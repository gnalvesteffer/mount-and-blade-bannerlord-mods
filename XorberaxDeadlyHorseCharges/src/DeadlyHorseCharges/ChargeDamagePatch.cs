using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DeadlyHorseCharges
{
    [HarmonyPatch(typeof(Mission))]
    internal static class ChargeDamagePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("ComputeBlowMagnitudeFromHorseCharge")]
        private static void ComputeBlowMagnitudeFromHorseChargePostfix(
            ref AttackCollisionData acd,
            Vec3 attackerAgentMovementDirection,
            Vec3 attackerAgentVelocity,
            float agentMountChargeDamageProperty,
            Vec3 victimAgentVelocity,
            Vec3 victimAgentPosition,
            ref float baseMagnitude,
            ref float specialMagnitude
        )
        {
            specialMagnitude *= SubModule.Config.ChargeDamageMultiplier;
        }
    }
}
