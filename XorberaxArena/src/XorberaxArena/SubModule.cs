using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Arena
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            var harmony = new Harmony("xorberax.arena");
            harmony.PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            var campaign = game.GameType as Campaign;
            if (campaign == null)
            {
                return;
            }
            var campaignGameStarter = (CampaignGameStarter)gameStarterObject;
            campaignGameStarter.AddBehavior(new ArenaCampaignBehavior());
        }

        public override void OnMissionBehaviourInitialize(Mission mission)
        {
            base.OnMissionBehaviourInitialize(mission);
            MBEditor.EnterEditMode(mission.SceneView, mission.CombatCamera.Frame, this.CameraElevation, this.CameraBearing);
        }
    }
}
