using System.IO;
using System.Linq;
using System.Net;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using HarmonyLib;
using NAudio.Wave;
using RestSharp;
using RestSharp.Authenticators;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;

namespace TextToSpeech
{
    internal static class TextToSpeech
    {
        private static readonly RestClient RestClient;
        private static readonly WaveOutEvent WaveOutEvent = new WaveOutEvent();

        static TextToSpeech()
        {
            RestClient = new RestClient(SubModule.Config.WatsonTextToSpeechUrl);
            RestClient.Authenticator = new HttpBasicAuthenticator("apikey", SubModule.Config.WatsonTextToSpeechApiKey);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        private static string GetVoiceNameForGender(this SpeechSynthesizer speechSynthesizer, VoiceGender gender)
        {
            var installedVoices = speechSynthesizer.GetInstalledVoices();
            return installedVoices.FirstOrDefault(voice => voice.VoiceInfo.Gender == gender)?.VoiceInfo.Name;
        }

        private static string StripTags(this string text)
        {
            return Regex.Replace(text, "<.*?>", string.Empty);
        }

        private static void Say(string text, bool isFemale)
        {
            var request = new RestRequest("v1/synthesize", DataFormat.Json);
            request.AddJsonBody(new
            {
                text,
                voice = isFemale ? SubModule.Config.WatsonTextToSpeechFemaleVoice : SubModule.Config.WatsonTextToSpeechMaleVoice,
            });
            request.AddHeader("Accept", "audio/wav");
            RestClient.PostAsync(request, (response, handle) =>
            {
                if (!response.IsSuccessful)
                {
                    return;
                }
                WaveOutEvent.Stop();
                WaveOutEvent.Init(new RawSourceWaveStream(new MemoryStream(response.RawBytes), new WaveFormat(22050, 1)));
                WaveOutEvent.Play();
            });
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
                var text = conversationManager.CurrentSentenceText?.StripTags();
                if (text == null)
                {
                    return;
                }
                Say(text, character.IsFemale);
            }

            [HarmonyPostfix]
            [HarmonyPatch("OnFinalize")]
            private static void OnFinalizePostfix()
            {
                WaveOutEvent?.Stop();
            }
        }
    }
}
