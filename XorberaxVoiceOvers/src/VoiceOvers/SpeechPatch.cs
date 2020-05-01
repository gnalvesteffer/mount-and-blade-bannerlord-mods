using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Localization;

namespace VoiceOvers
{
    [HarmonyPatch(typeof(MissionConversationVM))]
    internal static class SpeechPatch
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
            var sentenceId = (conversationManager.GetFieldValue("_currentSentenceText") as TextObject)?.GetID();
            if (SubModule.Config.IsDevMode)
            {
                var fileName = VoiceOverFilePathResolver.GetVoiceOverFilePath(sentenceId, character.Culture.GetCultureCode(), character.IsFemale, character.GetAgeGroup()).fileName;
                Clipboard.SetText($"File Name: {fileName}\nCulture: {character.Culture.GetCultureCode()}\nGender: {(character.IsFemale ? "Female" : "Male")}\nText: {conversationManager.CurrentSentenceText}");
                Logger.LogInfo($"Copied voice-over info to clipboard: {fileName}");
            }
            DialogHandler.SayDialog(sentenceId, character.Culture.GetCultureCode(), character.IsFemale, character.GetAgeGroup());
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnFinalize")]
        private static void OnFinalizePostfix()
        {
            DialogHandler.StopDialog();
        }
    }
}
