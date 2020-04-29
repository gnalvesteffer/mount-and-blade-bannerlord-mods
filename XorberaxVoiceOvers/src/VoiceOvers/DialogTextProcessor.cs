using System.Text.RegularExpressions;

namespace VoiceOvers
{
    public static class DialogTextProcessor
    {
        private static string StripTags(this string text)
        {
            return Regex.Replace(text, "<.*?>", string.Empty);
        }
    }
}
