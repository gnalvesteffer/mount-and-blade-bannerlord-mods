using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace CutThroughEveryone
{
    [HarmonyPatch(typeof(Mission))]
    internal static class CutThroughEveryonePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("DecideWeaponCollisionReaction")]
        private static void DecideWeaponCollisionReactionPostfix(
            Mission __instance,
            Blow registeredBlow,
            ref AttackCollisionData collisionData,
            Agent attacker,
            Agent defender,
            bool isFatalHit,
            bool isShruggedOff,
            ref MeleeCollisionReaction colReaction
        )
        {
            if (SliceLogic.ShouldSliceThrough(collisionData, attacker, defender))
            {
                colReaction = MeleeCollisionReaction.SlicedThrough;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("MeleeHitCallback")]
        private static void MeleeHitCallbackPostfix(
            ref AttackCollisionData collisionData,
            Agent attacker,
            Agent victim,
            GameEntity realHitEntity,
            float momentumRemainingToComputeDamage,
            ref float inOutMomentumRemaining,
            ref MeleeCollisionReaction colReaction,
            CrushThroughState cts,
            Vec3 blowDir,
            Vec3 swingDir,
            bool crushedThroughWithoutAgentCollision
        )
        {
            var totalDamage = collisionData.InflictedDamage + collisionData.AbsorbedByArmor;
            if (totalDamage >= 1 && SliceLogic.ShouldSliceThrough(collisionData, attacker, victim))
            {
                var normalizedDamageInflicted = (float)collisionData.InflictedDamage / totalDamage;
                inOutMomentumRemaining = momentumRemainingToComputeDamage * normalizedDamageInflicted;
            }
        }
    }
}
