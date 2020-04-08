using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace FriendlyFire
{
    [HarmonyPatch(typeof(Mission), "CancelsDamageAndBlocksAttackBecauseOfNonEnemyCase")]
    internal static class FriendlyFirePatch
    {
        private static bool Prefix(Mission __instance, ref bool __result, Agent attacker, Agent victim)
        {
            if (SubModule.Config.FriendlyFireEnabledMissionModes.Contains(__instance.Mode))
            {
                if (SubModule.Config.ShouldLogFriendlyFire && attacker.Team == victim.Team && attacker.Team == __instance.PlayerTeam)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"\"{attacker.Name}\" hit friendly troop \"${victim.Name}\".",
                        Color.ConvertStringToColor(SubModule.Config.FriendlyFireLogMessageColorHex))
                    );
                }
                __result = false;
                return false;
            }
            return true;
        }
    }
}
