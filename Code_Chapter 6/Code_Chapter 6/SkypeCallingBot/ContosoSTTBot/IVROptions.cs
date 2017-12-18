using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContosoSTTBot
{
    /// <summary>
    /// String constants for IVR Menu
    /// </summary>
    public static class IVROptions
    {

        /// <summary>
        /// The welcome message
        /// </summary>
        internal const string WelcomeMessage = "Hello you have reached Contoso Electronics Customer Service Centre";

        /// <summary>
        /// The main menu prompt
        /// </summary>
        internal const string MainMenuPrompt =
                    "If you are complaint is regarding Mobiles press 1, for TV press 2, for Refrigerator press 3";

        /// <summary>
        /// The record message
        /// </summary>
        internal const string RecordMessage =
            "Please leave your name and brief description of the complaint after the signal. You can press the hash key when finished. We will call you as soon as possible";

        /// <summary>
        /// The ending
        /// </summary>
        internal const string Ending = "Thank you for calling, goodbye";

        /// <summary>
        /// The option menu not supported message
        /// </summary>
        internal const string OptionMenuNotSupportedMessage = "The option you entered is not supported. Please try again.";
    }
}