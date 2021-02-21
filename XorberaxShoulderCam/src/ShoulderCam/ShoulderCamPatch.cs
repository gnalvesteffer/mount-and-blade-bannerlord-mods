using System;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screen;
using TaleWorlds.TwoDimension;

namespace ShoulderCam
{
    [HarmonyPatch(typeof(MissionScreen))]
    [HarmonyPatch("UpdateCamera")]
    internal static class ShoulderCamPatch
    {
        private static ShoulderPosition _focusedShoulderPosition = ShoulderPosition.Right;
        private static float _alternateShoulderSwitchTimestamp;
        private static float _revertRangedModeEndTimestamp;
        private static float _camShakeAmount;
        private static float _camShakeEndTimestamp;

        public static void ShakeCamera(float amount, float duration)
        {
            _camShakeAmount = Mathf.Clamp(amount, 0.0f, SubModule.Config.MaxCamShakeAmount);
            _camShakeEndTimestamp = Mission.Current.Time + duration;
        }

        private static void Prefix(
            ref MissionScreen __instance,
            ref float ____cameraSpecialTargetFOV,
            ref float ____cameraSpecialTargetDistanceToAdd,
            ref float ____cameraSpecialTargetAddedBearing,
            ref float ____cameraSpecialTargetAddedElevation,
            ref Vec3 ____cameraSpecialTargetPositionToAdd
        )
        {
            if (!ShouldApplyCameraTransformation(__instance))
            {
                var isFreeLooking = __instance.InputManager.IsGameKeyDown(CombatHotKeyCategory.ViewCharacter);
                if (!isFreeLooking && __instance.Mission.Mode != MissionMode.Conversation)
                {
                    ____cameraSpecialTargetFOV = 65;
                    ____cameraSpecialTargetDistanceToAdd = 0;
                    ____cameraSpecialTargetAddedBearing = 0;
                    ____cameraSpecialTargetAddedElevation = 0;
                    ____cameraSpecialTargetPositionToAdd = Vec3.Zero;
                }
                return;
            }

            var mainAgent = __instance.Mission.MainAgent;
            var torsoBone = mainAgent.AgentVisuals.GetSkeleton().GetBoneEntitialFrame((int)HumanBone.Spine2);
            var camShakeVector = GetCamShakeVector();
            ____cameraSpecialTargetFOV = SubModule.Config.ThirdPersonFieldOfView;
            ____cameraSpecialTargetDistanceToAdd = mainAgent.MountAgent == null ? SubModule.Config.OnFootPositionYOffset : SubModule.Config.MountedPositionYOffset;
            ____cameraSpecialTargetAddedBearing = SubModule.Config.BearingOffset + (torsoBone.rotation.f.z * SubModule.Config.TorsoTrackedCameraSwayAmount) + camShakeVector.z;
            ____cameraSpecialTargetAddedElevation = SubModule.Config.ElevationOffset + (torsoBone.rotation.f.x * SubModule.Config.TorsoTrackedCameraSwayAmount) + camShakeVector.x;
        }

        private static void Postfix(
            ref MissionScreen __instance,
            ref Vec3 ____cameraSpecialTargetPositionToAdd
        )
        {
            if (!ShouldApplyCameraTransformation(__instance))
            {
                ____cameraSpecialTargetPositionToAdd = Vec3.Zero;
                return;
            }

            var mainAgent = __instance.Mission.MainAgent;
            UpdateFocusedShoulderPosition(__instance, mainAgent);
            var directionBoneIndex = mainAgent.Monster.HeadLookDirectionBoneIndex;
            var boneEntitialFrame = mainAgent.AgentVisuals.GetSkeleton().GetBoneEntitialFrame(directionBoneIndex);
            boneEntitialFrame.origin = boneEntitialFrame.TransformToParent(mainAgent.Monster.FirstPersonCameraOffsetWrtHead);
            boneEntitialFrame.origin.x += (mainAgent.MountAgent == null ? SubModule.Config.OnFootPositionXOffset : SubModule.Config.MountedPositionXOffset) * _focusedShoulderPosition.GetOffsetValue();
            var frame = mainAgent.AgentVisuals.GetFrame();
            var parent = frame.TransformToParent(boneEntitialFrame);
            ____cameraSpecialTargetPositionToAdd = new Vec3(
                parent.origin.x - mainAgent.Position.x,
                parent.origin.y - mainAgent.Position.y,
                mainAgent.MountAgent == null ? SubModule.Config.OnFootPositionZOffset : SubModule.Config.MountedPositionZOffset
            );
        }

        private static void UpdateFocusedShoulderPosition(MissionScreen missionScreen, Agent mainAgent)
        {
            var isShoulderSwitchModeDynamic =
                SubModule.Config.ShoulderSwitchMode == ShoulderSwitchMode.MatchAttackAndBlockDirection ||
                SubModule.Config.ShoulderSwitchMode == ShoulderSwitchMode.TemporarilyMatchAttackAndBlockDirection;
            if (!isShoulderSwitchModeDynamic)
            {
                return;
            }

            var actionDirection = mainAgent.GetCurrentActionDirection(1);
            if (missionScreen.InputManager.IsGameKeyDown(CombatHotKeyCategory.Attack))
            {
                switch (actionDirection)
                {
                    case Agent.UsageDirection.AttackLeft:
                        _focusedShoulderPosition = ShoulderPosition.Left;
                        _alternateShoulderSwitchTimestamp = missionScreen.Mission.Time;
                        return;
                    case Agent.UsageDirection.AttackRight:
                        _focusedShoulderPosition = ShoulderPosition.Right;
                        return;
                }
            }
            else if (missionScreen.InputManager.IsGameKeyDown(CombatHotKeyCategory.Defend))
            {
                switch (actionDirection)
                {
                    case Agent.UsageDirection.DefendLeft:
                        _focusedShoulderPosition = ShoulderPosition.Left;
                        _alternateShoulderSwitchTimestamp = missionScreen.Mission.Time;
                        return;
                    case Agent.UsageDirection.DefendRight:
                        _focusedShoulderPosition = ShoulderPosition.Right;
                        return;
                }
            }
            else if (ShouldReturnFocusToOriginalShoulder(missionScreen))
            {
                _focusedShoulderPosition = ShoulderPosition.Right;
            }
        }

        private static bool ShouldReturnFocusToOriginalShoulder(MissionScreen missionScreen)
        {
            var returnFocusTimestamp = _alternateShoulderSwitchTimestamp + SubModule.Config.TemporaryShoulderSwitchDuration;
            return
                SubModule.Config.ShoulderSwitchMode == ShoulderSwitchMode.TemporarilyMatchAttackAndBlockDirection &&
                missionScreen.Mission.Time > returnFocusTimestamp;
        }

        private static bool ShouldApplyCameraTransformation(MissionScreen missionScreen)
        {
            var mainAgent = missionScreen.Mission.MainAgent;
            var missionMode = missionScreen.Mission.Mode;
            var isFirstPerson = missionScreen.Mission.CameraIsFirstPerson;
            var isMainAgentPresent = mainAgent != null;
            var isCompatibleMissionMode = missionMode != MissionMode.Conversation;
            var isFreeLooking = missionScreen.InputManager.IsGameKeyDown(CombatHotKeyCategory.ViewCharacter);
            return isMainAgentPresent &&
                   isCompatibleMissionMode &&
                   !isFreeLooking &&
                   !isFirstPerson &&
                   !mainAgent.ShouldRevertCameraForRangedMode(missionScreen) &&
                   !mainAgent.ShouldRevertCameraForMountMode();
        }

        private static bool ShouldRevertCameraForRangedMode(this Agent agent, MissionScreen missionScreen)
        {
            if (SubModule.Config.ShoulderCamRangedMode == ShoulderCamRangedMode.NoRevert)
            {
                return false;
            }
            if (SubModule.Config.ShoulderCamRangedMode == ShoulderCamRangedMode.RevertWhenAiming && !missionScreen.InputManager.IsGameKeyDown(CombatHotKeyCategory.Attack) && Mission.Current.Time > _revertRangedModeEndTimestamp)
            {
                return false;
            }
            var wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (wieldedItemIndex == EquipmentIndex.None)
            {
                return false;
            }
            var equippedItem = agent.Equipment[wieldedItemIndex];
            var weaponComponentData = equippedItem.GetWeaponComponentDataForUsage(equippedItem.CurrentUsageIndex);
            if (weaponComponentData != null && weaponComponentData.IsRangedWeapon)
            {
                if (missionScreen.InputManager.IsGameKeyDown(CombatHotKeyCategory.Attack))
                {
                    _revertRangedModeEndTimestamp = Mission.Current.Time + SubModule.Config.RevertWhenAimingReturnDelay;
                }
                return true;
            }
            return false;
        }

        private static bool ShouldRevertCameraForMountMode(this Agent agent)
        {
            if (SubModule.Config.ShoulderCamMountedMode == ShoulderCamMountedMode.NoRevert)
            {
                return false;
            }
            if (SubModule.Config.ShoulderCamMountedMode == ShoulderCamMountedMode.RevertWhenMounted && agent.MountAgent != null)
            {
                return true;
            }
            return false;
        }

        private static Vec3 GetCamShakeVector()
        {
            var strength = Math.Max(_camShakeEndTimestamp - Mission.Current.Time, 0);
            return new Vec3(
                MBRandom.RandomFloatNormal * _camShakeAmount * strength,
                MBRandom.RandomFloatNormal * _camShakeAmount * strength,
                MBRandom.RandomFloatNormal * _camShakeAmount * strength
            );
        }
    }
}
