using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TrainingField
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            new Harmony("xorberax.trainingfield").PatchAll();
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
            campaignGameStarter.AddBehavior(new TrainingFieldCampaignBehavior());
        }
    }
}
