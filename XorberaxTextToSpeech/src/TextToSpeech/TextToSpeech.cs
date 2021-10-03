using System.IO;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TextToSpeech
{
    internal static class TextToSpeech
    {
        private static readonly SpeechSynthesizer SpeechSynthesizer = new SpeechSynthesizer();

        static TextToSpeech()
        {
            if (SubModule.Config.IsDeveloperMode)
            {
                File.WriteAllText(
                    Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "installed-voices.txt"
                    ),
                    string.Join(
                        "\n",
                        SpeechSynthesizer.GetInstalledVoices().Select(voice => voice.VoiceInfo.Name)
                    )
                );
            }
        }

        public static void Say(string text, CharacterObject character)
        {
            if (SubModule.Config.IsDeveloperMode)
            {
                SubModule.LoadConfig(); // Reloads config, allowing voices to be tweaked in-game.
                Clipboard.SetText(text);
            }

            var voice = GetVoiceForCharacter(character);
            try
            {
                SpeechSynthesizer.SelectVoice(voice);
            }
            catch
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        $@"ERROR: The voice ""{voice}"" is not available.",
                        Colors.Red
                    )
                );
            }

            SpeechSynthesizer.SpeakAsyncCancelAll();
            SpeechSynthesizer.Rate = SubModule.Config.SpeechRate;
            SpeechSynthesizer.SpeakAsync(ProcessConversationTextForSpeech(text));
        }

        private static string GetFallbackVoiceNameForGender(VoiceGender gender)
        {
            var installedVoices = SpeechSynthesizer.GetInstalledVoices();
            return installedVoices.FirstOrDefault(voice => voice.VoiceInfo.Gender == gender)?.VoiceInfo.Name;
        }

        private static string GetVoiceForCharacter(CharacterObject character)
        {
            if (SubModule.Config.CultureVoices.TryGetValue(character.Culture.StringId, out var cultureVoiceDefinition))
            {
                return character.IsFemale ? cultureVoiceDefinition.Female : cultureVoiceDefinition.Male;
            }

            return GetFallbackVoiceNameForGender(character.IsFemale ? VoiceGender.Female : VoiceGender.Male);
        }

        private static string ProcessConversationTextForSpeech(string text)
        {
            foreach (var dialogReplacements in SubModule.Config.DialogReplacements)
            {
                text = text.Replace(dialogReplacements.Key, dialogReplacements.Value);
            }

            return Regex.Replace(text, "<.*?>", string.Empty); // remove any tags from the text.
        }

        public static void StopSpeech()
        {
            SpeechSynthesizer.SpeakAsyncCancelAll();
        }
    }
}