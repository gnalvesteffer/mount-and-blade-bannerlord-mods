using System.IO;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screen;

namespace ShoulderCam.Patches
{
    [HarmonyPatch(typeof(MissionScreen))]
    [HarmonyPatch("UpdateCamera")]
    internal static class ShoulderCamPatch
    {
        private static readonly string ConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
        private static bool _areLiveConfigUpdatesEnabled = false;
        private static float _positionXOffset = 0.35f;
        private static float _positionYOffset = 0.0f;
        private static float _positionZOffset = -0.5f;
        private static float _bearingOffset = 0.0f;
        private static float _elevationOffset = 0.0f;
        private static float _mountedDistanceOffset = 0.0f;
        private static float _thirdPersonFieldOfView = 65.0f;
        private static ShoulderCamRangedMode _shoulderCamRangedMode = ShoulderCamRangedMode.RevertWhenAiming;
        private static ShoulderCamMountedMode _shoulderCamMountedMode = ShoulderCamMountedMode.NoRevert;

        static ShoulderCamPatch()
        {
            LoadConfig();
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
            if (_areLiveConfigUpdatesEnabled)
            {
                LoadConfig();
            }

            if (!ShouldApplyCameraTransformation(__instance))
            {
                var isFreeLooking = InputKey.Tilde.IsDown();
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
            ____cameraSpecialTargetFOV = _thirdPersonFieldOfView;
            ____cameraSpecialTargetDistanceToAdd = _positionYOffset + (mainAgent.MountAgent == null ? 0.0f : _mountedDistanceOffset);
            ____cameraSpecialTargetAddedBearing = _bearingOffset;
            ____cameraSpecialTargetAddedElevation = _elevationOffset;
        }

        private static void Postfix(
            ref MissionScreen __instance,
            ref Vec3 ____cameraSpecialTargetPositionToAdd
        )
        {
            if (!ShouldApplyCameraTransformation(__instance))
            {
                return;
            }

            var mainAgent = __instance.Mission.MainAgent;
            var directionBoneIndex = mainAgent.Monster.HeadLookDirectionBoneIndex;
            var boneEntitialFrame = mainAgent.AgentVisuals.GetSkeleton().GetBoneEntitialFrame(directionBoneIndex);
            boneEntitialFrame.origin = boneEntitialFrame.TransformToParent(mainAgent.Monster.FirstPersonCameraOffsetWrtHead);
            boneEntitialFrame.origin.x += _positionXOffset;
            var frame = mainAgent.AgentVisuals.GetFrame();
            var parent = frame.TransformToParent(boneEntitialFrame);
            ____cameraSpecialTargetPositionToAdd = new Vec3(
                parent.origin.x - mainAgent.Position.x,
                parent.origin.y - mainAgent.Position.y,
                _positionZOffset
            );
        }

        private static bool ShouldApplyCameraTransformation(MissionScreen missionScreen)
        {
            var mainAgent = missionScreen.Mission.MainAgent;
            var missionMode = missionScreen.Mission.Mode;
            var isFirstPerson = missionScreen.Mission.CameraIsFirstPerson;
            var isMainAgentPresent = mainAgent != null;
            var isCompatibleMissionMode = missionMode != MissionMode.Conversation;
            var isFreeLooking = InputKey.Tilde.IsDown();
            return isMainAgentPresent &&
                   isCompatibleMissionMode &&
                   !isFreeLooking &&
                   !isFirstPerson &&
                   !mainAgent.ShouldRevertCameraForRangedMode() &&
                   !mainAgent.ShouldRevertCameraForMountMode();
        }

        private static bool ShouldRevertCameraForRangedMode(this Agent agent)
        {
            if (_shoulderCamRangedMode == ShoulderCamRangedMode.NoRevert)
            {
                return false;
            }
            if (_shoulderCamRangedMode == ShoulderCamRangedMode.RevertWhenAiming && !InputKey.LeftMouseButton.IsDown())
            {
                return false;
            }
            var wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (wieldedItemIndex == EquipmentIndex.None)
            {
                return false;
            }
            var equippedItem = agent.Equipment[wieldedItemIndex];
            foreach (var weaponComponentData in equippedItem.Weapons)
            {
                if (weaponComponentData != null && weaponComponentData.IsRangedWeapon)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ShouldRevertCameraForMountMode(this Agent agent)
        {
            if (_shoulderCamMountedMode == ShoulderCamMountedMode.NoRevert)
            {
                return false;
            }
            if (_shoulderCamMountedMode == ShoulderCamMountedMode.RevertWhenMounted && agent.MountAgent != null)
            {
                return true;
            }
            return false;
        }

        private static void LoadConfig()
        {
            if (!File.Exists(ConfigFilePath))
            {
                return;
            }
            try
            {
                var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigFilePath));
                _areLiveConfigUpdatesEnabled = config.AreLiveConfigUpdatesEnabled;
                _positionXOffset = config.PositionXOffset;
                _positionYOffset = config.PositionYOffset;
                _positionZOffset = config.PositionZOffset;
                _bearingOffset = config.BearingOffset;
                _elevationOffset = config.ElevationOffset;
                _mountedDistanceOffset = config.MountedDistanceOffset;
                _shoulderCamRangedMode = config.ShoulderCamRangedMode;
                _shoulderCamMountedMode = config.ShoulderCamMountedMode;
                _thirdPersonFieldOfView = config.ThirdPersonFieldOfView;
            }
            catch
            {
            }
        }
    }
}
