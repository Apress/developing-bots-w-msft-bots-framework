using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using SlackChannel.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace SlackChannel.Dialogs
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.Dialogs.IDialog{System.Object}" />
    [Serializable]
    public class SlackDialog : IDialog<string>
    {
        /// <summary>
        /// The destination
        /// </summary>
        private string destination;
        /// <summary>
        /// The name
        /// </summary>
        private string name;
        /// <summary>
        /// The age
        /// </summary>
        private int age;
        /// <summary>
        /// The startdate
        /// </summary>
        private DateTime checkInDate;
        /// <summary>
        /// The enddate
        /// </summary>
        private DateTime checkOutDate;

        /// <summary>
        /// The start of the code that represents the conversational dialog.
        /// </summary>
        /// <param name="context">The dialog context.</param>
        /// <returns>
        /// A task that represents the dialog start.
        /// </returns>
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Messages the received asynchronous.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var validDestinations = new List<string> { "palazzo", "bellagio", "mirage" };

            var activity = await result as Activity;

            var slackChannelData = JObject.FromObject(activity.ChannelData);

            var destination = slackChannelData.SelectToken("Payload.actions[0].value")?.ToString();

            if (!string.IsNullOrEmpty(destination) && validDestinations.Contains(destination))
            {
                this.destination = destination;
                context.Call(new NameDialog(), this.NameDialogResumeAfter);
            }
            else
            {
                var reply = context.MakeMessage();

                reply.ChannelData = new SlackMessage
                {
                    Text = "Hi Welcome to *Vegas tours Bot*, _book you Las Vegas Stay from anywhere using Slack_ :hotel:",
                    Attachments = new System.Collections.Generic.List<Models.Attachment>
                {
                    new Models.Attachment()
                    {
                        Title = "Which hotel do you want to stay?",
                        Text = "Choose a Hotel",
                        Color = "#3AA3E3",
                        Callback = "wopr_hotel",
                        Actions = new List<Models.Action>()
                        {
                            new Models.Action()
                            {
                                 Text = "The Palazzo (min. $350 per night)",
                                 Name = "destination",
                                 Type = "button",
                                 Value = "palazzo"
                            },
                             new Models.Action()
                            {
                                 Text = "Bellagio Hotel and Casino (min. $300 per night)",
                                 Name = "destination",
                                 Type = "button",
                                 Value = "bellagio"
                            },
                              new Models.Action()
                            {
                                 Text = "The Mirage (min. $280 per night)",
                                 Name = "destination",
                                 Type = "button",
                                 Value = "mirage",
                                 Style = "Danger",
                                 Confirm = new JObject(
                                 new JProperty("confirm",
                                                 new JObject(
                                                     new JProperty("title", "Are you sure, you may want to check the events at Palazzo?"),
                                                     new JProperty("text", "You may want to check events, restaurants at the Palazzo?"),
                                                     new JProperty("ok_text", "Yes"),
                                                     new JProperty("dismiss_text", "No")
                                               ))) }
                        }
                    }
                }
                };

                // return our reply to the user
                await context.PostAsync(reply);

                context.Wait(MessageReceivedAsync);
            }
        }

        /// <summary>
        /// Names the dialog resume after.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private async Task NameDialogResumeAfter(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                this.name = await result;

                context.Call(new AgeDialog(this.name), this.AgeDialogResumeAfter);
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
            }
        }

        /// <summary>
        /// Ages the dialog resume after.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private async Task AgeDialogResumeAfter(IDialogContext context, IAwaitable<int> result)
        {
            try
            {
                this.age = await result;

                context.Call(new CheckinDateDialog(this.name), this.CheckinDateDialogResumeAfter);
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
            }
        }

        /// <summary>
        /// Checkins the date dialog resume after.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private async Task CheckinDateDialogResumeAfter(IDialogContext context, IAwaitable<DateTime> result)
        {
            try
            {
                this.checkInDate = await result;

                context.Call(new CheckoutDateDialog(this.checkInDate), this.CheckoutDateDialogResumeAfter);
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
            }
        }

        /// <summary>
        /// Checkouts the date dialog resume after.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private async Task CheckoutDateDialogResumeAfter(IDialogContext context, IAwaitable<DateTime> result)
        {
            try
            {
                // Sample Image embedded in Slack Message
                string imageUrl = "https://www.palazzo.com/content/dam/palazzo/Suites/bella/Palazzo-Bella1-med_900x600.jpg.resize.0.0.474.316.jpg";
                // Sample Title URL 
                string titleURL = "https://www.vegas.com/tours/";
                this.checkOutDate = await result;
                var reply = context.MakeMessage();
                reply.ChannelData = new SlackMessage
                {
                    Text = $"Hi *{name}*, your stay is confirmed from \n Your stay is from *{checkInDate.ToLongDateString()} to {checkOutDate.ToLongDateString()}*. \n Have a pleasant stay :smiley:",
                    Attachments = new System.Collections.Generic.List<Models.Attachment>
                  {
                    new Models.Attachment()
                    {
                        Title = "Here are few things you can do at Vegas !! :thumbsup_all:",
                        Text = "Hotel Pics",
                        ImageUrl = imageUrl,
                        TitleLink = titleURL
                    }
                  }
                };
                await context.PostAsync(reply);
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
            }
        }
    }
}
