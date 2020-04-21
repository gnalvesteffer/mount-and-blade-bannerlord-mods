using System.Linq;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;

namespace TextToSpeech
{
    internal static class TextToSpeech
    {
        private static readonly SpeechSynthesizer GameSpeechSynthesizer = new SpeechSynthesizer();

        private static string GetVoiceNameForGender(this SpeechSynthesizer speechSynthesizer, VoiceGender gender)
        {
            var installedVoices = speechSynthesizer.GetInstalledVoices();
            return installedVoices.FirstOrDefault(voice => voice.VoiceInfo.Gender == gender)?.VoiceInfo.Name;
        }

        private static void Say(string text, VoiceGender voiceGender, VoiceAge voiceAge)
        {
            GameSpeechSynthesizer.SpeakAsyncCancelAll();
            GameSpeechSynthesizer.SelectVoiceByHints(voiceGender, voiceAge);
            GameSpeechSynthesizer.SpeakAsync(text);
        }

        private static VoiceAge GetVoiceAge(int age)
        {
            if (age < 12)
            {
                return VoiceAge.Child;
            }
            if (age < 18)
            {
                return VoiceAge.Teen;
            }
            if (age < 50)
            {
                return VoiceAge.Adult;
            }
            return VoiceAge.Senior;
        }

        private static string StripTags(this string text)
        {
            return Regex.Replace(text, "<.*?>", string.Empty);
        }

        [HarmonyPatch(typeof(MissionConversationVM))]
        internal static class Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch("Refresh")]
            private static void RefreshPostfix()
            {
                var conversationManager = Campaign.Current.ConversationManager;
                var character = conversationManager.OneToOneConversationCharacter;
                if (character == null)
                {
                    return;
                }
                var voiceGender = character.IsFemale
                    ? VoiceGender.Female
                    : VoiceGender.Male;
                var text = conversationManager.CurrentSentenceText;
                Say(text?.StripTags() ?? string.Empty, voiceGender, GetVoiceAge((int)character.Age));
            }

            [HarmonyPostfix]
            [HarmonyPatch("OnFinalize")]
            private static void OnFinalizePostfix()
            {
                GameSpeechSynthesizer.SpeakAsyncCancelAll();
            }
        }
    }
}
