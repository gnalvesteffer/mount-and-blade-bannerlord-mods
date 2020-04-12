using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Banks
{
    public class BankData
    {
        public MBGUID SettlementId { get; set; }
        public CampaignTime AccountOpenDate { get; set; }
        public bool HasAccount { get; set; }
        public int Balance { get; set; }
        public float InterestRate { get; set; }
        public int RemainingUnpaidLoan { get; set; }
        public CampaignTime? LoanStartDate { get; set; }
    }
}
