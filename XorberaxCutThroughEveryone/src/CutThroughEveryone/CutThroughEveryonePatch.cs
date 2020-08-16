using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace CutThroughEveryone
{
    [HarmonyPatch(typeof(Mission))]
    internal static class CutThroughEveryonePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("DecideWeaponCollisionReaction")]
        private static void DecideWeaponCollisionReactionPostfix(
            ref AttackCollisionData collisionData,
            Agent attacker,
            Agent defender,
            ref MeleeCollisionReaction colReaction
        )
        {
            if (SliceLogic.ShouldCutThrough(collisionData, attacker, defender))
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
            float momentumRemainingToComputeDamage,
            ref float inOutMomentumRemaining
        )
        {
            var totalDamage = collisionData.InflictedDamage + collisionData.AbsorbedByArmor;
            if (totalDamage >= 1 && SliceLogic.ShouldCutThrough(collisionData, attacker, victim))
            {
                var normalizedDamageInflicted = (float)collisionData.InflictedDamage / totalDamage;
                inOutMomentumRemaining =
                    momentumRemainingToComputeDamage *
                    normalizedDamageInflicted *
                    SubModule.Config.DamageAmountRetainedPerCut;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("MeleeHitCallback")]
        private static void MeleeHitCallbackPrefix(ref AttackCollisionData collisionData, Agent attacker)
        {
            if (
                !SubModule.Config.CanCutThroughShields ||
                SubModule.Config.OnlyPlayerCanCutThroughShields && !attacker.IsMainAgent ||
                collisionData.CollisionResult != CombatCollisionResult.Blocked
            )
            {
                return;
            }

            collisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose( // have to clone it and set the blocked flag to false since the properties are readonly
                false,
                collisionData.CorrectSideShieldBlock,
                collisionData.IsAlternativeAttack,
                collisionData.IsColliderAgent,
                collisionData.CollidedWithShieldOnBack,
                collisionData.IsMissile,
                collisionData.MissileHasPhysics,
                collisionData.EntityExists,
                collisionData.ThrustTipHit,
                collisionData.MissileGoneUnderWater,
                collisionData.CollisionResult,
                collisionData.CurrentUsageIndex,
                collisionData.AffectorWeaponKind,
                collisionData.StrikeType,
                collisionData.DamageType,
                collisionData.CollisionBoneIndex,
                collisionData.VictimHitBodyPart,
                collisionData.AttackBoneIndex,
                collisionData.AttackDirection,
                collisionData.PhysicsMaterialIndex,
                collisionData.CollisionHitResultFlags,
                collisionData.AttackProgress,
                collisionData.CollisionDistanceOnWeapon,
                collisionData.AttackerStunPeriod,
                collisionData.DefenderStunPeriod,
                collisionData.CurrentWeaponTipSpeed,
                collisionData.MissileTotalDamage,
                collisionData.MissileStartingBaseSpeed,
                collisionData.ChargeVelocity,
                collisionData.FallSpeed,
                collisionData.WeaponRotUp,
                collisionData.WeaponBlowDir,
                collisionData.CollisionGlobalPosition,
                collisionData.MissileVelocity,
                collisionData.MissileStartingPosition,
                collisionData.VictimAgentCurVelocity,
                collisionData.CollisionGlobalNormal
            );
        }
    }
}
