using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.Core;
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
                inOutMomentumRemaining =
                    momentumRemainingToComputeDamage *
                    normalizedDamageInflicted *
                    SubModule.Config.DamageRetainedPerCut;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("MissileHitCallback")]
        private static bool MissileHitCallbackPrefix(
            Mission __instance,
            ref Dictionary<int, Mission.Missile> ____missiles,
            ref int hitParticleIndex,
            ref AttackCollisionData collisionData,
            int missileIndex,
            Vec3 missileStartingPosition,
            Vec3 missilePosition,
            Vec3 missileAngularVelocity,
            Vec3 movementVelocity,
            MatrixFrame attachGlobalFrame,
            MatrixFrame affectedShieldGlobalFrame,
            int numDamagedAgents,
            Agent attacker,
            Agent victim,
            GameEntity hitEntity,
            ref bool __result
        )
        {
            var missile = ____missiles[missileIndex];
            var weaponFlags1 = missile.Weapon.CurrentUsageItem.WeaponFlags;
            var momentumRemaining = 1f;
            WeaponComponentData shieldOnBack = null;
            if (collisionData.AttackBlockedWithShield && weaponFlags1.HasAnyFlag(WeaponFlags.CanPenetrateShield))
            {
                __instance.Call("GetAttackCollisionResults", attacker, victim, hitEntity, momentumRemaining, collisionData, false, false, shieldOnBack);
                var wieldedItemIndex = victim.GetWieldedItemIndex(Agent.HandIndex.OffHand);
                if (collisionData.InflictedDamage > ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.ShieldPenetrationOffset) + ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.ShieldPenetrationFactor) * (double)victim.Equipment[wieldedItemIndex].GetShieldArmorForCurrentUsage())
                {
                    AttackCollisionData.UpdateDataForShieldPenetration(ref collisionData);
                    momentumRemaining *= (float)(0.400000005960464 + MBRandom.RandomFloat * 0.200000002980232);
                }
            }
            hitParticleIndex = -1;
            var flag1 = !GameNetwork.IsSessionActive;
            var missileHasPhysics = collisionData.MissileHasPhysics;
            var fromIndex = PhysicsMaterial.GetFromIndex(collisionData.PhysicsMaterialIndex);
            var num1 = fromIndex.IsValid ? (int)fromIndex.GetFlags() : 0;
            var flag2 = (weaponFlags1 & WeaponFlags.AmmoSticksWhenShot) > 0;
            var flag3 = (num1 & 1) == 0;
            var flag4 = (uint)(num1 & 8) > 0U;
            MissionObject attachedMissionObject = null;
            if (victim == null && hitEntity != null)
            {
                var gameEntity = hitEntity;
                do
                {
                    attachedMissionObject = gameEntity.GetFirstScriptOfType<MissionObject>();
                    gameEntity = gameEntity.Parent;
                } while (attachedMissionObject == null && gameEntity != null);
                hitEntity = attachedMissionObject?.GameEntity;
            }
            var collisionReaction1 = !flag4 ? (!weaponFlags1.HasAnyFlag(WeaponFlags.Burning) ? (!flag3 || !flag2 ? Mission.MissileCollisionReaction.BounceBack : Mission.MissileCollisionReaction.Stick) : Mission.MissileCollisionReaction.BecomeInvisible) : Mission.MissileCollisionReaction.PassThrough;
            var isCanceled = false;
            Mission.MissileCollisionReaction collisionReaction2;
            if (collisionData.MissileGoneUnderWater)
            {
                collisionReaction2 = Mission.MissileCollisionReaction.BecomeInvisible;
                hitParticleIndex = 0;
            }
            else if (victim == null)
            {
                if (hitEntity != null)
                {
                    __instance.Call("GetAttackCollisionResults", attacker, victim, hitEntity, momentumRemaining, collisionData, false, false, shieldOnBack);
                    var missileBlow = __instance.Call("CreateMissileBlow", attacker, collisionData, missile, missilePosition, missileStartingPosition);
                    __instance.Call("RegisterBlow", attacker, (Agent)null, hitEntity, missileBlow, collisionData);
                }
                collisionReaction2 = collisionReaction1;
                hitParticleIndex = 0;
            }
            else if (collisionData.AttackBlockedWithShield)
            {
                __instance.Call("GetAttackCollisionResults", attacker, victim, hitEntity, momentumRemaining, collisionData, false, false, shieldOnBack);
                collisionReaction2 = collisionData.IsShieldBroken ? Mission.MissileCollisionReaction.BecomeInvisible : collisionReaction1;
                hitParticleIndex = 0;
            }
            else
            {
                if (attacker != null && attacker.IsFriendOf(victim))
                {
                    if (!missileHasPhysics)
                    {
                        if (flag1)
                        {
                            if (attacker.Controller == Agent.ControllerType.AI)
                                isCanceled = true;
                        }
                        else if (MultiplayerOptions.OptionType.FriendlyFireDamageRangedFriendPercent.GetIntValue() <= 0 && MultiplayerOptions.OptionType.FriendlyFireDamageRangedSelfPercent.GetIntValue() <= 0 || __instance.Mode == MissionMode.Duel)
                            isCanceled = true;
                    }
                }
                else if (victim.IsHuman && !attacker.IsEnemyOf(victim))
                    isCanceled = true;
                else if (flag1 && attacker != null && (attacker.Controller == Agent.ControllerType.AI && victim.RiderAgent != null) && attacker.IsFriendOf(victim.RiderAgent))
                    isCanceled = true;
                if (isCanceled)
                {
                    if (flag1 && attacker == Agent.Main && attacker.IsFriendOf(victim))
                        InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("ui_you_hit_a_friendly_troop").ToString(), Color.ConvertStringToColor("#D65252FF")));
                    collisionReaction2 = Mission.MissileCollisionReaction.BecomeInvisible;
                }
                else
                {
                    var flag5 = (weaponFlags1 & WeaponFlags.MultiplePenetration) > 0;
                    __instance.Call("GetAttackCollisionResults", attacker, victim, (GameEntity)null, momentumRemaining, collisionData, false, false, shieldOnBack);
                    var missileBlow = (Blow)__instance.Call("CreateMissileBlow", attacker, collisionData, missile, missilePosition, missileStartingPosition);
                    if (!collisionData.CollidedWithShieldOnBack & flag5 && numDamagedAgents > 0)
                    {
                        missileBlow.InflictedDamage /= numDamagedAgents;
                        missileBlow.SelfInflictedDamage /= numDamagedAgents;
                    }
                    var managedParameter1 = ManagedParameters.Instance.GetManagedParameter(missileBlow.DamageType != DamageTypes.Cut ? (missileBlow.DamageType != DamageTypes.Pierce ? ManagedParametersEnum.DamageInterruptAttackThresholdBlunt : ManagedParametersEnum.DamageInterruptAttackThresholdPierce) : ManagedParametersEnum.DamageInterruptAttackThresholdCut);
                    if (collisionData.InflictedDamage <= (double)managedParameter1)
                        missileBlow.BlowFlag |= BlowFlags.ShrugOff;
                    if (victim.State == AgentState.Active)
                        __instance.Call("RegisterBlow", attacker, victim, (GameEntity)null, missileBlow, collisionData);
                    hitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_enter");
                    if (flag5 && numDamagedAgents < 3)
                    {
                        collisionReaction2 = Mission.MissileCollisionReaction.PassThrough;
                    }
                    else
                    {
                        collisionReaction2 = collisionReaction1;
                        if (collisionReaction1 == Mission.MissileCollisionReaction.Stick && !collisionData.CollidedWithShieldOnBack)
                        {
                            var flag6 = __instance.CombatType == Mission.MissionCombatType.Combat;
                            if (flag6)
                            {
                                var flag7 = victim.IsHuman && collisionData.VictimHitBodyPart == BoneBodyPartType.Head;
                                flag6 = victim.State != AgentState.Active || !flag7;
                            }
                            if (flag6)
                            {
                                var managedParameter2 = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.MissileMinimumDamageToStick);
                                var num2 = 2f * managedParameter2;
                                if (!GameNetwork.IsClientOrReplay && missileBlow.InflictedDamage < (double)managedParameter2 && missileBlow.AbsorbedByArmor > (double)num2)
                                    collisionReaction2 = Mission.MissileCollisionReaction.BounceBack;
                            }
                            else
                                collisionReaction2 = Mission.MissileCollisionReaction.BecomeInvisible;
                        }
                    }
                }
            }
            if (collisionData.CollidedWithShieldOnBack && shieldOnBack != null && (victim != null && victim.IsMainAgent))
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("ui_hit_shield_on_back").ToString(), Color.ConvertStringToColor("#FFFFFFFF")));
            MatrixFrame attachLocalFrame;
            if (!collisionData.MissileHasPhysics && !collisionData.MissileGoneUnderWater)
            {
                var shouldMissilePenetrate = collisionReaction2 == Mission.MissileCollisionReaction.Stick;
                attachLocalFrame = (MatrixFrame)__instance.Call("CalculateAttachedLocalFrame", attachGlobalFrame, collisionData, missile.Weapon.CurrentUsageItem, victim, hitEntity, movementVelocity, missileAngularVelocity, affectedShieldGlobalFrame, shouldMissilePenetrate);
            }
            else
            {
                attachLocalFrame = attachGlobalFrame;
                attachedMissionObject = null;
            }
            var velocity = Vec3.Zero;
            var angularVelocity = Vec3.Zero;
            if (collisionReaction2 == Mission.MissileCollisionReaction.BounceBack)
            {
                var weaponFlags2 = weaponFlags1 & WeaponFlags.AmmoBreakOnBounceBackMask;
                if (weaponFlags2 == WeaponFlags.AmmoCanBreakOnBounceBack && collisionData.MissileVelocity.Length > (double)ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BreakableProjectileMinimumBreakSpeed) || weaponFlags2 == WeaponFlags.AmmoBreaksOnBounceBack)
                {
                    collisionReaction2 = Mission.MissileCollisionReaction.BecomeInvisible;
                    hitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_broken_arrow");
                }
                else
                    missile.CalculateBounceBackVelocity(missileAngularVelocity, collisionData, out velocity, out angularVelocity);
            }
            __instance.HandleMissileCollisionReaction(missileIndex, collisionReaction2, attachLocalFrame, attacker, victim, collisionData.AttackBlockedWithShield, collisionData.CollisionBoneIndex, attachedMissionObject, velocity, angularVelocity, -1);
            foreach (var missionBehaviour in __instance.MissionBehaviours)
                missionBehaviour.OnMissileHit(attacker, victim, isCanceled);
            __result = collisionReaction2 != Mission.MissileCollisionReaction.PassThrough;
            return false;
        }
    }
}
