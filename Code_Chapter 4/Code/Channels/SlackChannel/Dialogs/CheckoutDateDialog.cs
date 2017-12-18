namespace SlackChannel.Dialogs
{
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;


    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.Bot.Builder.Dialogs.IDialog{System.Int32}" />
    [Serializable]
    public class CheckoutDateDialog : IDialog<DateTime>
    {
        private DateTime checkInDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckoutDateDialog"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public CheckoutDateDialog(DateTime name)
        {
            this.checkInDate = name;
        }

        /// <summary>
        /// The start of the code that represents the conversational dialog.
        /// </summary>
        /// <param name="context">The dialog context.</param>
        /// <returns>
        /// A task that represents the dialog start.
        /// </returns>
        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync($"Checkin Date is { this.checkInDate.ToLongDateString() }, what is your checkout date?");

            context.Wait(this.MessageReceivedAsync);
        }

        /// <summary>
        /// Messages the received asynchronous.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            DateTime checkoutDate;

            if (DateTime.TryParse(message.Text, out checkoutDate) && (checkoutDate > this.checkInDate))
            {
                context.Done(checkoutDate);
            }
        }
    }
}