using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

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
            var agent = conversationManager.OneToOneConversationAgent as Agent;
            var character = conversationManager.OneToOneConversationCharacter;
            if (character == null)
            {
                return;
            }
            var sentenceTextObject = conversationManager.GetFieldValue("_currentSentenceText") as TextObject;
            var sentenceId = sentenceTextObject?.GetID();
            if (sentenceId == DialogTextProcessor.RumorSentenceId)
            {
                sentenceId = DialogTextProcessor.GetUnderlyingRumorSentenceId(sentenceTextObject) ?? sentenceId;
            }
            if (SubModule.Config.IsDevMode)
            {
                var fileData = VoiceOverFilePathResolver.GetVoiceOverFileData(character.StringId, sentenceId, character.Culture.GetCultureCode(), character.IsFemale, agent.GetAgeGroup());
                Clipboard.SetText($"NPC Voice-Over File Name: {fileData.npcFileName}\nGeneric Voice-Over File Name: {fileData.genericFileName}\nSentence ID: {sentenceId}\nCulture: {character.Culture.GetCultureCode()}\nGender: {(character.IsFemale ? "Female" : "Male")}\nNPC Name: {character.Name}\nNPC ID: {character.StringId}\nText: {conversationManager.CurrentSentenceText}");
                Logger.LogInfo($"Copied voice-over info to clipboard: {sentenceId}");
            }
            DialogHandler.SayDialog(character.StringId, sentenceId, character.Culture.GetCultureCode(), character.IsFemale, agent.GetAgeGroup());
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnFinalize")]
        private static void OnFinalizePostfix()
        {
            DialogHandler.StopDialog();
        }
    }
}
