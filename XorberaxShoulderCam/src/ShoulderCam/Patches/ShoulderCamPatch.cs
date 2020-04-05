using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Screen;

namespace ShoulderCam.Patches
{
    [HarmonyPatch(typeof(MissionScreen))]
    [HarmonyPatch("UpdateCamera")]
    public class ShoulderCamPatch
    {
        private static float _time;

        public static void Postfix(
            float dt,
            ref MissionScreen __instance,
            ref bool ____cameraApplySpecialMovementsInstantly,
            ref Vec3 ____cameraSpecialTargetPositionToAdd
        )
        {
            _time += dt;
            var mainAgent = __instance.Mission.MainAgent;
            if (mainAgent == null)
            {
                return;
            }
            var directionBoneIndex = mainAgent.Monster.HeadLookDirectionBoneIndex;
            var boneEntitialFrame = mainAgent.AgentVisuals.GetSkeleton().GetBoneEntitialFrame(directionBoneIndex);
            boneEntitialFrame.origin = boneEntitialFrame.TransformToParent(mainAgent.Monster.FirstPersonCameraOffsetWrtHead);
            boneEntitialFrame.origin.x += 0.35f;
            var frame = mainAgent.AgentVisuals.GetFrame();
            var parent = frame.TransformToParent(boneEntitialFrame);
            ____cameraApplySpecialMovementsInstantly = true;
            ____cameraSpecialTargetPositionToAdd = new Vec3(
                parent.origin.x - mainAgent.Position.x,
                parent.origin.y - mainAgent.Position.y,
                -0.5f
            );
        }
    }
}
