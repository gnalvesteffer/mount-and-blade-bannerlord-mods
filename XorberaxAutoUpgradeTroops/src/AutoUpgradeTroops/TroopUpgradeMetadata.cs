using TaleWorlds.CampaignSystem;

namespace AutoUpgradeTroops
{
    public class TroopUpgradeMetadata
    {
        public int MemberIndex { get; }
        public CharacterObject CharacterUpgradeTarget { get; }
        public int TotalUnitsToUpgradeInElement { get; }
        public int CostToUpgradeToHigherTierPerUnit { get; }

        public TroopUpgradeMetadata(
            int memberIndex,
            CharacterObject characterUpgradeTarget,
            int totalUnitsToUpgradeInElement,
            int costToUpgradeToHigherTierPerUnit
        )
        {
            MemberIndex = memberIndex;
            CharacterUpgradeTarget = characterUpgradeTarget;
            TotalUnitsToUpgradeInElement = totalUnitsToUpgradeInElement;
            CostToUpgradeToHigherTierPerUnit = costToUpgradeToHigherTierPerUnit;
        }
    }
}
