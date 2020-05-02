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
                var fileData = VoiceOverFilePathResolver.GetVoiceOverFileData(character.StringId, sentenceId, character.Culture.GetCultureCode(), character.IsFemale, character.GetAgeGroup());
                Clipboard.SetText($"NPC Voice-Over File Name: {fileData.npcFileName}\nGeneric Voice-Over File Name: {fileData.genericFileName}\nCulture: {character.Culture.GetCultureCode()}\nGender: {(character.IsFemale ? "Female" : "Male")}\nNPC Name: {character.Name}\nNPC ID: {character.StringId}\nText: {conversationManager.CurrentSentenceText}");
                Logger.LogInfo("Copied voice-over info to clipboard");
            }
            DialogHandler.SayDialog(character.StringId, sentenceId, character.Culture.GetCultureCode(), character.IsFemale, character.GetAgeGroup());
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnFinalize")]
        private static void OnFinalizePostfix()
        {
            DialogHandler.StopDialog();
        }
    }
}
