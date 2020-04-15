using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Banks
{
    internal class LoanQuest : QuestBase
    {
        [SaveableField(1)] private Settlement _settlement;
        [SaveableField(2)] private CampaignTime _loanEndDate;

        public sealed override TextObject Title
        {
            get
            {
                var textObject = new TextObject("Repay Loan at {BANK_SETTLEMENT_NAME} by {LOAN_DUE_DATE}");
                textObject.SetTextVariable("BANK_SETTLEMENT_NAME", this._settlement.Name);
                textObject.SetTextVariable("LOAN_DUE_DATE", this._loanEndDate.ToString());
                return textObject;
            }
        }

        public override bool IsRemainingTimeHidden { get; } = false;

        public static LoanQuest Start(
            Settlement settlement,
            Hero banker,
            int loanAmount,
            CampaignTime loanStartDate,
            CampaignTime loanEndDate
        )
        {
            var loanQuest = new LoanQuest(settlement, banker, loanEndDate);
            loanQuest.StartQuest();
            loanQuest.AddLog(new TextObject($"You took out a loan of {loanAmount}<img src=\"Icons\\Coin@2x\"> from the bank of {settlement.Name} on {loanStartDate}. You must repay the loan by {loanEndDate}."), true);
            return loanQuest;
        }

        private LoanQuest(
            Settlement settlement,
            Hero banker,
            CampaignTime loanEndDate
        ) : base(
            $"xorberax_banks_loan_{settlement.Id}_{CampaignTime.Now.ToMilliseconds}",
            banker,
            loanEndDate,
            0
        )
        {
            _settlement = settlement;
            _loanEndDate = loanEndDate;
        }

        public void OnLoanRepaidOnTime()
        {
            CompleteQuestWithSuccess();
            this.AddLog(new TextObject("You repaid the loan on time."), true);
        }

        protected override void OnTimedOut()
        {
            base.OnTimedOut();
            this.AddLog(new TextObject("You failed to repay the loan on time."), true);
            BanksCampaignBehavior.Current.ApplyInitialBankRetaliationForUnpaidLoan(this._settlement);
        }

        protected override void SetDialogs()
        {
        }

        protected override void InitializeQuestOnGameLoad()
        {
        }
    }
}
