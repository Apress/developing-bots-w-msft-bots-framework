namespace ContosoSTTBot
{
    using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
    using Microsoft.CognitiveServices.SpeechRecognition;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;


    /// <summary>
    /// Bing Speech to text provider
    /// </summary>
    public class BingSpeech
    {
        private DataRecognitionClient dataClient;
        private Action<string> _callback;
        private Action<string> _failedCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="BingSpeech"/> class.
        /// </summary>
        /// <param name="conversationResult">The conversation result.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="failedCallback">The failed callback.</param>
        public BingSpeech(Action<string> callback, Action<string> failedCallback)
        {
            _callback = callback;
            _failedCallback = failedCallback;
        }

        /// <summary>
        /// Gets the default locale.
        /// </summary>
        /// <value>
        /// The default locale.
        /// </value>
        public string DefaultLocale { get; } = "en-US";

        /// <summary>
        /// Gets or sets the subscription key.
        /// </summary>
        /// <value>
        /// The subscription key.
        /// </value>
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Creates the data reco client.
        /// </summary>
        public void CreateDataRecoClient()
        {
            this.SubscriptionKey = ConfigurationManager.AppSettings["MicrosoftSpeechApiKey"].ToString();
            this.dataClient = SpeechRecognitionServiceFactory.CreateDataClient(
                SpeechRecognitionMode.ShortPhrase,
                this.DefaultLocale,// for example: ‘en-us’
                this.SubscriptionKey);
            this.dataClient.OnResponseReceived += this.OnResponseReceivedHandler;
            this.dataClient.OnConversationError += this.OnConversationError;
        }

        public void SendAudioHelper(Stream recordedStream)
        {
            // Note for wave files, we can just send data from the file right to the server.
            // In the case you are not an audio file in wave format, and instead you have just
            // raw data (for example audio coming over bluetooth), then before sending up any 
            // audio data, you must first send up an SpeechAudioFormat descriptor to describe 
            // the layout and format of your raw audio data via DataRecognitionClient's sendAudioFormat() method.
            int bytesRead = 0;
            byte[] buffer = new byte[1024];
            try
            {
                do
                {
                    // Get more Audio data to send into byte buffer.
                    bytesRead = recordedStream.Read(buffer, 0, buffer.Length);

                    // Send of audio data to service. 
                    this.dataClient.SendAudio(buffer, bytesRead);
                }
                while (bytesRead > 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception ------------ " + ex.Message);
            }
            finally
            {
                // We are done sending audio.  Final recognition results will arrive in OnResponseReceived event call.
                this.dataClient.EndAudio();
            }
        }

        /// <summary>
        /// Called when [response received handler].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.RecognitionSuccess)
            {
                // confidence high has order 1
                string phraseResponse = e.PhraseResponse.Results.OrderBy(r => r.Confidence).FirstOrDefault().DisplayText;
                this._callback.Invoke(phraseResponse.ToString());
            }
        }

        /// <summary>
        /// Called when [conversation error].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechErrorEventArgs"/> instance containing the event data.</param>
        private void OnConversationError(object sender, SpeechErrorEventArgs e)
        {
            Debug.WriteLine(e.SpeechErrorText);
            this._failedCallback.Invoke(e.SpeechErrorText);
        }
    }

}