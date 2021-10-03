using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;

namespace TextToSpeech
{
    [HarmonyPatch(typeof(MissionConversationVM))]
    internal static class MissionConversationVMPatch
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

            var text = conversationManager.CurrentSentenceText;
            TextToSpeech.Say(text ?? string.Empty, character);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnFinalize")]
        private static void OnFinalizePostfix()
        {
            TextToSpeech.StopSpeech();
        }
    }
}