namespace EmailChannel.Dialogs
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Diagnostics;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.Dialogs.IDialog{System.Object}" />
    [Serializable]
    public class RootDialog : IDialog<object>
    {
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
            var activity = await result as Activity;
            var reply = context.MakeMessage();
            string location = (activity.Text ?? string.Empty);
            var weatherHTML = this.getWeatherDetails(location);
            var emailChannelData = new EmailChannelData()
            {
                htmlBody = $"<html><body style=\"font-family: Calibri; font-size: 11pt;\">{weatherHTML}</body></html>",
                subject = $"Weather at {location}",
                importance = "normal"
            };
            if (activity.ChannelId != "email")
            {
                reply.Text = emailChannelData.ToString();
            }
            else
            {
                reply.ChannelData = emailChannelData;
            }
            // return our reply to the user
            await context.PostAsync(reply);
            context.Wait(MessageReceivedAsync);
        }

        /// <summary>
        /// Gets the weather details.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        private string getWeatherDetails(string location)
        {
            string htmlResponse = string.Empty;
            string response = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    response = client.GetStringAsync($"http://api.openweathermap.org/data/2.5/weather?q={location}&appid=9aeafb54eb98a3b63804af5932066f8c&units=metric&mode=html").Result;
                    htmlResponse += response;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                htmlResponse = $"Invalid location, please only location in Email. For Ex: New Delhi, Current Location Passed: {location}, API Response: {response}";
            }
            return htmlResponse;
        }
    }

    [JsonObject]
    public class EmailChannelData
    {
        /// <summary>
        /// Gets or sets the HTML body.
        /// </summary>
        /// <value>
        /// The HTML body.
        /// </value>
        [JsonProperty("htmlBody")]
        public string htmlBody { get; set; }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        /// <value>
        /// The subject.
        /// </value>
        [JsonProperty("subject")]
        public string subject { get; set; }

        /// <summary>
        /// Gets or sets the importance.
        /// </summary>
        /// <value>
        /// The importance.
        /// </value>
        [JsonProperty("importance")]
        public string importance { get; set; }
    }
}