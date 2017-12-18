namespace SearchBot
{
    using Microsoft.Bot.Builder.Dialogs;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Threading.Tasks;
    using System.Net.Http;
    using Microsoft.Bot.Connector;
    using System.Text;
    using Newtonsoft.Json;

    [Serializable]
    public class SearchDialog : IDialog<Object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(this.MessageReceiveAsync);
        }

        public async Task MessageReceiveAsync(IDialogContext dialogContext, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            using (var httpClient = new HttpClient())
            {
                var queryString = HttpUtility.ParseQueryString(string.Empty);

                // Request headers
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "");
                // User agent for PC, this should be extracted from client's request to Bot application and 
                // forwarded to the Cognitive Services API
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows Phone 8.0; Trident/6.0; IEMobile/10.0; ARM; Touch; NOKIA; Lumia 822)");
                // Bing generated ID to identify the request
                httpClient.DefaultRequestHeaders.Add("X-Search-ClientIP", "999.999.999.999");
                // User's location 
                httpClient.DefaultRequestHeaders.Add("X-Search-Location", "lat:17.4761950,long:78.3813510,re:100");

                // Query parameters
                queryString["q"] = message.Text; // user's search query
                queryString["count"] = "3"; // number of results to include
                queryString["offset"] = "0"; // number of pages to skip
                queryString["mkt"] = "en-us"; // market to associate the request with
                queryString["safesearch"] = "Moderate"; // safe search other possible values are Off and Strict
                queryString["freshness"] = "Day"; // freshness of the content by Day, Week, Month
                queryString["answerCount"] = "2"; // 
                queryString["promote"] = "Video, News"; // freshness of the content by Day, Week, Month
                var uri = "https://api.cognitive.microsoft.com/bing/v7.0/search?" + queryString;

                var responseMessage = await httpClient.GetAsync(uri);
                var responseContent = await responseMessage.Content.ReadAsByteArrayAsync();
                var response = Encoding.ASCII.GetString(responseContent, 0, responseContent.Length);
                dynamic data = JsonConvert.DeserializeObject<object>(response);
                string webSearch = "";

                foreach (var webPage in data.webPages.value)
                {
                    string name = webPage.name;
                    string url = webPage.url;
                    string displayUrl = webPage.displayUrl;
                    string snippet = webPage.snippet;
                    webSearch += $"# {name} \n [{displayUrl}]({url})\n\n {snippet} \n\n";
                }

                await dialogContext.PostAsync($"# Web Search Result \n  {webSearch} ");
            }
        }
    }
}