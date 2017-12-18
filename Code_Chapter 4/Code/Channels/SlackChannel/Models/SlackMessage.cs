using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SlackChannel.Models
{
    /// <summary>
    /// 
    /// </summary>
    [JsonObject]
    public class SlackMessage
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the attachments.
        /// </summary>
        /// <value>
        /// The attachments.
        /// </value>
        [JsonProperty("attachments")]
        public List<Attachment> Attachments { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [JsonObject]
    public class Attachment
    {
        public Attachment() { }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>
        /// The color.
        /// </value>
        [JsonProperty("color")]
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the fallback.
        /// </summary>
        /// <value>
        /// The fallback.
        /// </value>
        [JsonProperty("fallback")]
        public string Fallback { get; set; }

        /// <summary>
        /// Gets or sets the actions.
        /// </summary>
        /// <value>
        /// The actions.
        /// </value>
        [JsonProperty("actions")]
        public List<Action> Actions { get; set; }

        /// <summary>
        /// Gets or sets the callback.
        /// </summary>
        /// <value>
        /// The callback.
        /// </value>
        [JsonProperty("callback_id")]
        public string Callback { get; set; }

        /// <summary>
        /// Gets or sets the image URL.
        /// </summary>
        /// <value>
        /// The image URL.
        /// </value>
        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the title link.
        /// </summary>
        /// <value>
        /// The title link.
        /// </value>
        [JsonProperty("title_link")]
        public string TitleLink { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [JsonObject]
    public class Action
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonProperty("name")]
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        [JsonProperty("text")]
        public string Text { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("type")]
        public string Type { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        [JsonProperty("value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>
        /// The style.
        /// </value>
        [JsonProperty("style")]
        public string Style { get; set; }

        /// <summary>
        /// Gets or sets the confirm.
        /// </summary>
        /// <value>
        /// The confirm.
        /// </value>
        [JsonProperty("confirm")]
        public JObject Confirm { get; set; }
    }
}