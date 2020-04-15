using TaleWorlds.CampaignSystem;

namespace AutoUpgradeTroops
{
    internal static class TroopRosterElementExtensions
    {
        public static bool HasHigherTierToUpgradeTo(this TroopRosterElement element)
        {
            return !element.Character.IsHero &&
                   element.Character.UpgradeTargets != null &&
                   element.Character.UpgradeTargets.Length != 0;
        }
    }
}
