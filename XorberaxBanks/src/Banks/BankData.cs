using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace Banks
{
    [SaveableClass(42069247)]
    public class BankData
    {
        [SaveableProperty(1)]
        public string SettlementId { get; set; }

        [SaveableProperty(2)]
        public bool HasAccount { get; set; }

        [SaveableProperty(3)]
        public CampaignTime AccountOpenDate { get; set; }

        [SaveableProperty(4)]
        public int Balance { get; set; }

        [SaveableProperty(5)]
        public float InterestRate { get; set; }

        [SaveableProperty(6)]
        public int RemainingUnpaidLoan { get; set; }

        [SaveableProperty(7)]
        public CampaignTime LoanStartDate { get; set; }
    }
}
