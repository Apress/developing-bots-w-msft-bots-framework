namespace ReceiveAttachmentBot
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Microsoft.ProjectOxford.Vision;
    using Microsoft.ProjectOxford.Vision.Contract;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using System.Net.Http.Formatting;
    using System.Threading;
    using Newtonsoft.Json.Linq;

    [Serializable]
    internal class ReceiveAttachmentDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(this.MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            string _apiUrlBase = "https://westus.api.cognitive.microsoft.com/vision/v1.0/ocr";
            var message = await argument;
            string ret = "";
            if (message.Attachments != null && message.Attachments.Any())
            {
                var attachment = message.Attachments.First();
                using (HttpClient httpClient = new HttpClient())
                {
                    // Skype & MS Teams attachment URLs are secured by a JwtToken, so we need to pass the token from our bot.
                    if ((message.ChannelId.Equals("skype", StringComparison.InvariantCultureIgnoreCase) || message.ChannelId.Equals("msteams", StringComparison.InvariantCultureIgnoreCase))
                        && new Uri(attachment.ContentUrl).Host.EndsWith("skype.com"))
                    {
                        var token = await new MicrosoftAppCredentials().GetTokenAsync();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    var responseMessage = await httpClient.GetAsync(attachment.ContentUrl);
                    string temp = "";

                    using (var httpClient1 = new HttpClient())
                    {
                        //setup HttpClient
                        httpClient1.BaseAddress = new Uri(_apiUrlBase);
                        httpClient1.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "");

                        HttpContent content = responseMessage.Content;
                        content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");
                        var response = await httpClient1.PostAsync(_apiUrlBase, content);

                        var responseContent = await response.Content.ReadAsByteArrayAsync();
                        ret = Encoding.ASCII.GetString(responseContent, 0, responseContent.Length);
                        dynamic image = JsonConvert.DeserializeObject<object>(ret);
                        foreach (var regs in image.regions)
                        {
                            foreach (var lns in regs.lines)
                            {
                                foreach (var wds in lns.words)
                                {
                                    temp += wds.text + " ";
                                }
                            }
                        }

                    }

                    // Azure search index endpoint
                    string _searchAPIBaseURI = "https://mcs-ocr.search.windows.net/indexes/ocrindex/docs/index?api-version=2016-09-01";

                    // request body for inserting data into Azure Search
                    JObject postData = new JObject();
                    dynamic indexData = new JObject();
                    indexData["@search.action"] = "upload";
                    indexData.id = Guid.NewGuid().ToString();
                    indexData.value = temp;
                    postData["value"] = new JArray(indexData);

                    using (var httpClient2 = new HttpClient())
                    {
                        httpClient2.BaseAddress = new Uri(_searchAPIBaseURI);
                        httpClient2.DefaultRequestHeaders.Add("api-key", "");
                        var response = await httpClient2.PostAsJsonAsync<JObject>(_searchAPIBaseURI, postData);
                    }

                    await context.PostAsync($"The text found is  '{temp}' ");

                }
            }
            else
            {
                await context.PostAsync("Hi there! I'm a bot created to show you how I can receive message attachments, but no attachment was sent to me. Please, try again sending a new message including an attachment.");
            }

            context.Wait(this.MessageReceivedAsync);
        }
    }
}



