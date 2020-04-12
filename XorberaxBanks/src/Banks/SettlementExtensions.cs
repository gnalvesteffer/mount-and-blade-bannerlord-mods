using TaleWorlds.CampaignSystem;

namespace Banks
{
    internal static class SettlementExtensions
    {
        public static SettlementComponent GetSettlementComponent(this Settlement settlement) => settlement.GetComponent<SettlementComponent>();
    }
}
