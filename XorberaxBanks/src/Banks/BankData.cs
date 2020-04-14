using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Banks
{
    [SaveableClass(42069247)]
    internal class BankData
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
        public float InterestRate { get; set; } = -1.0f;

        [SaveableProperty(6)]
        public int RemainingUnpaidLoan { get; set; }

        [SaveableProperty(7)]
        public CampaignTime LoanStartDate { get; set; } = CampaignTime.Never;

        public CampaignTime LoanEndDate => CampaignTime.Days((int)LoanStartDate.ToDays) + CampaignTime.Days(SubModule.Config.DaysToRepayLoan);

        [SaveableProperty(8)]
        public CampaignTime LastBankUpdateDate { get; set; } = CampaignTime.Never;

        [SaveableProperty(9)]
        public bool HasBankPerformedInitialRetaliationForUnpaidLoan { get; set; }

        [SaveableProperty(10)]
        public float LoanLateFeeInterestRate { get; set; }

        [SaveableProperty(11)]
        public LoanQuest LoanQuest { get; set; }

        [SaveableProperty(12)]
        public Hero Banker { get; set; }

        [SaveableProperty(13)]
        public int OriginalLoanAmount { get; set; }

        public int AccruedLoanLateFees => RemainingUnpaidLoan - OriginalLoanAmount;
        
        [SaveableProperty(14)]
        public CampaignTime LastLoanRecurringRetaliationDate { get; set; }
    }
}
