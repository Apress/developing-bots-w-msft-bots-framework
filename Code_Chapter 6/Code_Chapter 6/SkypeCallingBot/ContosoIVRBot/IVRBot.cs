namespace ContosoIVRBot
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Bot.Builder.Calling;
    using Microsoft.Bot.Builder.Calling.Events;
    using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
    using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;

    /// <summary>
    /// IVR Bot class
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="Microsoft.Bot.Builder.Calling.ICallingBot" />
    public class IVRBot : IDisposable, ICallingBot
    {
        /// <summary>
        /// The call state map
        /// </summary>
        private IDictionary<string, CallState> callStateMap = new Dictionary<string, CallState>();

        /// <summary>
        /// The telemetry client
        /// </summary>
        private TelemetryClient telemetryClient = new TelemetryClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="IVRBot"/> class.
        /// </summary>
        /// <param name="callingBotService">The calling bot service.</param>
        public IVRBot(ICallingBotService callingBotService)
        {
            if (callingBotService == null)
            {
                throw new ArgumentNullException(nameof(callingBotService));
            }

            this.CallingBotService = callingBotService;

            // Registering Call events
            this.CallingBotService.OnIncomingCallReceived += OnIncomingCallReceived;
            this.CallingBotService.OnPlayPromptCompleted += OnPlayPromptCompleted;
            this.CallingBotService.OnRecordCompleted += OnRecordCompletedAsync;
            this.CallingBotService.OnRecognizeCompleted += OnRecognizeCompleted;
            this.CallingBotService.OnHangupCompleted += OnHangupCompleted;
        }

        /// <summary>
        /// Gets the calling bot service.
        /// </summary>
        /// <value>
        /// The calling bot service.
        /// </value>
        public ICallingBotService CallingBotService
        {
            get; private set;
        }

        /// <summary>
        /// Called when [incoming call received].
        /// </summary>
        /// <param name="incomingCallEvent">The argument.</param>
        /// <returns></returns>
        private Task OnIncomingCallReceived(IncomingCallEvent incomingCallEvent)
        {
            this.callStateMap[incomingCallEvent.IncomingCall.Id] = new CallState(incomingCallEvent.IncomingCall.Participants);
            telemetryClient.TrackTrace($"IncomingCallReceived - {incomingCallEvent.IncomingCall.Id}");
            incomingCallEvent.ResultingWorkflow.Actions = new List<ActionBase>
            {
                new Answer { OperationId = Guid.NewGuid().ToString() },
                GetPromptForText(IVROptions.WelcomeMessage)
            };
            return Task.FromResult(true);
        }

        /// <summary>
        /// Called when [play prompt completed].
        /// </summary>
        /// <param name="playPromptOutcomeEvent">The play prompt outcome event.</param>
        /// <returns></returns>
        private Task OnPlayPromptCompleted(PlayPromptOutcomeEvent playPromptOutcomeEvent)
        {
            // Add Choices for the caller
            telemetryClient.TrackTrace($"PlayPromptCompleted - {playPromptOutcomeEvent.ConversationResult.Id}");
            Recognize recognize = SetupInitialMenu();
            playPromptOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase> { recognize };
            return Task.FromResult(true);
        }


        /// <summary>
        /// Called when [hangup completed].
        /// </summary>
        /// <param name="hangupOutcomeEvent">The hangup outcome event.</param>
        /// <returns></returns>
        private Task OnHangupCompleted(HangupOutcomeEvent hangupOutcomeEvent)
        {
            telemetryClient.TrackTrace($"HangupCompleted - {hangupOutcomeEvent.ConversationResult.Id}");
            hangupOutcomeEvent.ResultingWorkflow = null;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Called when [record completed].
        /// </summary>
        /// <param name="recordOutcomeEvent">The record outcome event.</param>
        /// <returns></returns>
        private async Task<bool> OnRecordCompletedAsync(RecordOutcomeEvent recordOutcomeEvent)
        {
            telemetryClient.TrackTrace($"RecordCompleted - {recordOutcomeEvent.ConversationResult.Id}");
            recordOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                {
                    GetPromptForText(IVROptions.Ending),
                    new Hangup { OperationId = Guid.NewGuid().ToString() }
                };

            // Message from User as stream of bytes format
            var recordedContent = recordOutcomeEvent.RecordedContent.Result;
            this.callStateMap[recordOutcomeEvent.ConversationResult.Id].RecordedContent = recordedContent;

            // save call state
            var azureStorgeContext = new AzureStorageContext();
            await azureStorgeContext.SaveCallStateAsync(this.callStateMap[recordOutcomeEvent.ConversationResult.Id]);

            recordOutcomeEvent.ResultingWorkflow.Links = null;
            this.callStateMap.Remove(recordOutcomeEvent.ConversationResult.Id);
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Called when [recognize completed].
        /// </summary>
        /// <param name="recognizeOutcomeEvent">The recognize outcome event.</param>
        /// <returns></returns>
        private Task OnRecognizeCompleted(RecognizeOutcomeEvent recognizeOutcomeEvent)
        {
            if (recognizeOutcomeEvent.RecognizeOutcome.Outcome != Outcome.Success)
            {
                telemetryClient.TrackTrace($"RecognizeFailed - {recognizeOutcomeEvent.ConversationResult.Id}");
                var unsupported = GetPromptForText(IVROptions.OptionMenuNotSupportedMessage);
                var recognize = SetupInitialMenu();
                recognizeOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase> { unsupported, recognize };
                return Task.FromResult(true);
            }

            // outcome success
            telemetryClient.TrackTrace($"RecognizeCompleted - {recognizeOutcomeEvent.ConversationResult.Id}");
            var prompt = GetPromptForText(IVROptions.RecordMessage);
            var record = new Record
            {
                OperationId = Guid.NewGuid().ToString(),
                PlayPrompt = prompt,
                MaxDurationInSeconds = 60,
                InitialSilenceTimeoutInSeconds = 5,
                MaxSilenceTimeoutInSeconds = 2,
                RecordingFormat = RecordingFormat.Wav,
                PlayBeep = true,
                StopTones = new List<char> { '#' }
            };
            this.callStateMap[recognizeOutcomeEvent.ConversationResult.Id].ChosenMenuOption = recognizeOutcomeEvent.RecognizeOutcome.ChoiceOutcome.ChoiceName.ToString();
            this.callStateMap[recognizeOutcomeEvent.ConversationResult.Id].Id = recognizeOutcomeEvent.ConversationResult.Id;
            recognizeOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase> { record };
            return Task.FromResult(true);
        }


        #region Dispose Implementation

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            if (CallingBotService != null)
            {
                CallingBotService.OnIncomingCallReceived -= OnIncomingCallReceived;
                CallingBotService.OnPlayPromptCompleted -= OnPlayPromptCompleted;
                CallingBotService.OnRecordCompleted -= OnRecordCompletedAsync;
                CallingBotService.OnRecognizeCompleted -= OnRecognizeCompleted;
                CallingBotService.OnHangupCompleted -= OnHangupCompleted;
            }
        }

        #endregion

        #region Helper Functions
        /// <summary>
        /// Setups the initial menu.
        /// </summary>
        /// <returns></returns>
        private Recognize SetupInitialMenu()
        {
            var callerChoices = new List<RecognitionOption>()
            {
                new RecognitionOption() { Name = "1", DtmfVariation = '1' },
                new RecognitionOption() { Name = "2", DtmfVariation = '2'},
                new RecognitionOption() { Name = "3", DtmfVariation = '3'},
                new RecognitionOption() { Name = "#", DtmfVariation = '#'} // for navigating back
            };

            // create recognize action for caller
            var recognize = new Recognize
            {
                OperationId = Guid.NewGuid().ToString(),
                PlayPrompt = GetPromptForText(IVROptions.MainMenuPrompt),
                BargeInAllowed = true,
                InitialSilenceTimeoutInSeconds = 10,
                Choices = callerChoices
            };
            return recognize;
        }

        /// <summary>
        /// Gets the prompt for text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        private PlayPrompt GetPromptForText(string text)
        {
            var prompt = new Prompt { Value = text, Voice = VoiceGender.Female,  };
            return new PlayPrompt { OperationId = Guid.NewGuid().ToString(), Prompts = new List<Prompt> { prompt } };
        }

        #endregion
        
    }

    /// <summary>
    /// Call State
    /// </summary>
    public class CallState
    {
        public CallState(IEnumerable<Participant> participants)
        {
            this.Participants = participants;
        }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the chosen menu option.
        /// </summary>
        /// <value>
        /// The chosen menu option.
        /// </value>
        public string ChosenMenuOption { get; set; }

        /// <summary>
        /// Gets the participants.
        /// </summary>
        /// <value>
        /// The participants.
        /// </value>
        public IEnumerable<Participant> Participants { get; }

        /// <summary>
        /// Gets or sets the content of the recorded.
        /// </summary>
        /// <value>
        /// The content of the recorded.
        /// </value>
        public Stream RecordedContent { get; set; }
    }
}