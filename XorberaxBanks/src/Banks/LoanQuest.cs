using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Banks
{
    internal class LoanQuest : QuestBase
    {
        [SaveableField(1)] private Settlement _settlement;

        public sealed override TextObject Title
        {
            get
            {
                var textObject = new TextObject("Repay Loan at {BANK_SETTLEMENT_NAME}");
                textObject.SetTextVariable("BANK_SETTLEMENT_NAME", this._settlement.Name);
                return textObject;
            }
        }

        public override bool IsRemainingTimeHidden { get; } = false;

        public static LoanQuest Start(Settlement settlement, int loanAmount, CampaignTime loanStartDate, CampaignTime loanEndDate)
        {
            var loanQuest = new LoanQuest(
                $"{settlement.StringId}_{Guid.NewGuid()}",
                settlement,
                loanAmount,
                loanStartDate,
                loanEndDate
            );
            loanQuest.StartQuest();
            loanQuest.AddLog(new TextObject($"You took out a loan of {loanAmount}<img src=\"Icons\\Coin@2x\"> from the bank of {settlement.Name} on {loanStartDate}. You must repay the loan by {loanEndDate}."), true);
            return loanQuest;
        }

        private LoanQuest(string questId, Settlement settlement, int loanAmount, CampaignTime loanStartDate, CampaignTime loanEndDate) : base(questId, Hero.MainHero, loanEndDate, 0)
        {
            _settlement = settlement;
        }

        public void OnLoanRepaidOnTime()
        {
            CompleteQuestWithSuccess();
            this.AddLog(new TextObject("You repaid the loan."), true);
        }

        public void OnLoanUnpaid()
        {
            CompleteQuestWithTimeOut();
            this.AddLog(new TextObject("You failed to repay the loan on time."), true);
        }

        protected override void SetDialogs()
        {
        }

        protected override void InitializeQuestOnGameLoad()
        {
        }
    }
}
