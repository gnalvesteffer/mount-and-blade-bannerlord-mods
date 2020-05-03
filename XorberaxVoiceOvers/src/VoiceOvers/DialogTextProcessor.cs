using System.Collections.Generic;
using System.Text.RegularExpressions;
using TaleWorlds.Localization;
using TaleWorlds.Localization.TextProcessor;

namespace VoiceOvers
{
    public static class DialogTextProcessor
    {
        public static readonly string RumorSentenceId = "bHzPJfRb";

        private static string StripTags(this string text)
        {
            return Regex.Replace(text, "<.*?>", string.Empty);
        }

        public static string GetUnderlyingRumorSentenceId(TextObject sentenceTextObject)
        {
            var textContext = AccessExtensions.GetField(typeof(MBTextManager), "TextContext") as TextProcessingContext;
            var textVariables = textContext.GetFieldValue("_variables") as Dictionary<string, TextObject>;
            if (textVariables?.ContainsKey("CONVERSATION_SCRAP") == true)
            {
                return textVariables["CONVERSATION_SCRAP"].GetID();
            }
            return null;
        }
    }
}
