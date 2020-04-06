using TaleWorlds.CampaignSystem;

namespace Arena
{
    public class ArenaCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, campaignGameStarter =>
            {
                campaignGameStarter.AddGameMenuOption(
                    "town_arena",
                    "town_arena_slaughter_criminals",
                    "{=town_arena_slaughter_criminals}Slaughter criminals",
                    args => true,
                    args =>
                    {
                        Campaign.Current.GameMenuManager.NextLocation = LocationComplex.Current.GetLocationWithId("arena");
                        Campaign.Current.GameMenuManager.PreviousLocation = LocationComplex.Current.GetLocationWithId("center");
                        PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(Campaign.Current.GameMenuManager.NextLocation);
                        Campaign.Current.GameMenuManager.NextLocation = null;
                        Campaign.Current.GameMenuManager.PreviousLocation = null;
                    },
                    true,
                    5
                );
            });
        }

        public override void SyncData(IDataStore dataStore)
        {
            return; // can persist info here
        }
    }
}
