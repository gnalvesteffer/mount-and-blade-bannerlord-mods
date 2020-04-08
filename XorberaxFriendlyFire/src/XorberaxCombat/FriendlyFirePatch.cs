using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace FriendlyFire
{
    [HarmonyPatch(typeof(Mission), "CancelsDamageAndBlocksAttackBecauseOfNonEnemyCase")]
    internal static class FriendlyFirePatch
    {
        private static bool Prefix(Mission __instance, ref bool __result)
        {
            if (SubModule.Config.FriendlyFireEnabledMissionModes.Contains(__instance.Mode))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
