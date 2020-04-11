using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.KillFeed;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.KillFeed.Personal;

namespace KillFeedDisabler
{
    [HarmonyPatch]
    [HarmonyPatch(typeof(SPKillFeedVM))]
    internal static class SPKillFeedVMPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnAgentRemoved")]
        private static bool OnAgentRemovedPrefix(
            ref SPKillFeedVM __instance,
            Agent affectedAgent,
            Agent affectorAgent
        )
        {
            var assistedAgent = (Agent)__instance.Call("GetAssistedAgent", affectedAgent, affectorAgent);
            if (SubModule.Config.EnablePersonalKillFeed)
            {
                if (Agent.Main != null && assistedAgent == Agent.Main)
                {
                    __instance.PersonalFeed.OnPersonalAssist(affectedAgent.Name);
                }
            }
            if (SubModule.Config.EnableGeneralKillFeed)
            {
                __instance.GeneralCasualty.OnAgentRemoved(affectedAgent, affectorAgent, assistedAgent);
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnPersonalDamage")]
        private static bool OnPersonalDamagePrefix(
            ref SPKillFeedVM __instance,
            int totalDamage,
            bool isFatalDamage,
            bool isVictimAgentMount,
            bool isFriendlyFire,
            string victimAgentName
        )
        {
            if (SubModule.Config.EnablePersonalKillFeed)
            {
                __instance.PersonalFeed.OnPersonalHit(totalDamage, isFatalDamage, isVictimAgentMount, isFriendlyFire, victimAgentName);
            }
            return false;
        }
    }
}
