using TaleWorlds.CampaignSystem;

namespace AutoUpgradeTroops
{
    internal class AutoUpgradeTroopsCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnBattleEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnBattleEnded(MapEvent mapEvent)
        {
            PartyUpgrader.UpgradeParty(PartyBase.MainParty);
        }
    }
}
