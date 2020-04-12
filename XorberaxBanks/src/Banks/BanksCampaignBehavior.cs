using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Banks
{
    public class BanksCampaignBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, BankData> _settlementBankDataBySettlementId = new Dictionary<string, BankData>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                dataStore.SyncData(nameof(_settlementBankDataBySettlementId), ref _settlementBankDataBySettlementId);
            }
            catch
            {
            }
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            AddMenus(campaignGameStarter);
        }

        private void OnDailyTick()
        {
            UpdateBankData();
        }

        private void UpdateBankData()
        {
            foreach (var settlementId in _settlementBankDataBySettlementId.Keys)
            {
                var settlement = Settlement.Find(settlementId);
                var bankData = GetBankDataAtSettlement(settlement);
                if ((CampaignTime.Now - bankData.LastBankUpdateDate).ToWeeks > 0)
                {
                    bankData.Balance += (int)(bankData.Balance * bankData.InterestRate);
                    bankData.LastBankUpdateDate = CampaignTime.Now;
                    if (IsLoanOverDueAtSettlement(settlement) && !bankData.HasBankRetaliatedForUnpaidLoan)
                    {
                        ApplyBankRetaliationForUnpaidLoan(settlement);
                    }
                }
            }
        }

        private void ApplyBankRetaliationForUnpaidLoan(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            FactionManager.DeclareWar(settlement.MapFaction, Hero.MainHero.MapFaction);
            bankData.HasBankRetaliatedForUnpaidLoan = true;
            InformationManager.ShowInquiry(
                new InquiryData(
                    "Loan Payment Overdue",
                    $"You have failed to repay your loan with the {settlement.Name} bank. {settlement.Name} now recognizes you as a criminal.",
                    true,
                    false,
                    "OK",
                    "",
                    () => InformationManager.HideInquiry(),
                    () => { }
                ),
                true
            );
        }

        private bool IsLoanOverDueAtSettlement(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            return bankData.RemainingUnpaidLoan > 0 && (CampaignTime.Now - bankData.LoanStartDate).ToDays > SubModule.Config.DaysToRepayLoan;
        }

        private void AddMenus(CampaignGameStarter campaignGameStarter)
        {
            // Town Menu
            campaignGameStarter.AddGameMenuOption(
                "town",
                "town_go_to_bank",
                "{=town_bank}Go to the bank",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return true;
                },
                args => GameMenu.SwitchToMenu(GetBankMenuId(Settlement.CurrentSettlement)),
                false,
                1
            );

            // Bank Setup
            campaignGameStarter.AddGameMenu(
                "bank_setup",
                "{=bank_setup}You are at the {XORBERAX_SETTLEMENT_NAME} bank. You can open an account with a weekly interest rate of {XORBERAX_BANKS_INTEREST_RATE}%.",
                args => UpdateBankMenuTextVariables(),
                GameOverlays.MenuOverlayType.SettlementWithBoth
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_setup",
                "bank_setup_open_account",
                "{=bank_setup_open_account}Open Account ({XORBERAX_BANKS_BANK_ACCOUNT_OPENING_COST}{GOLD_ICON})",
                args =>
                {
                    var canPlayerAffordToOpenBankAccountAtSettlement = CanPlayerAffordToOpenBankAccountAtSettlement(Settlement.CurrentSettlement);
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    args.IsEnabled = canPlayerAffordToOpenBankAccountAtSettlement;
                    args.Tooltip = canPlayerAffordToOpenBankAccountAtSettlement
                        ? TextObject.Empty
                        : new TextObject("You cannot afford to open a bank account here.");
                    return true;
                },
                args => OnOpenBankAccountAtSettlement(Settlement.CurrentSettlement)
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_setup",
                "bank_setup_leave",
                "{=bank_setup_leave}Leave bank",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => GameMenu.SwitchToMenu("town")
            );

            // Bank Account
            campaignGameStarter.AddGameMenu(
                "bank_account",
                "{=bank_account}You are at the {XORBERAX_SETTLEMENT_NAME} bank.\nYour balance is {XORBERAX_BANKS_BALANCE}{GOLD_ICON} with a weekly interest rate of {XORBERAX_BANKS_INTEREST_RATE}%. {XORBERAX_BANKS_LOAN_INFO}",
                args => UpdateBankMenuTextVariables(),
                GameOverlays.MenuOverlayType.SettlementWithBoth
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_account",
                "bank_account_deposit",
                "{=bank_account_deposit}Deposit",
                args =>
                {
                    var isLoanOverDueAtSettlement = IsLoanOverDueAtSettlement(Settlement.CurrentSettlement);
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    args.IsEnabled = !isLoanOverDueAtSettlement;
                    args.Tooltip = isLoanOverDueAtSettlement ? new TextObject("You must repay your loan.") : TextObject.Empty;
                    return true;
                },
                args => { PromptDepositAmount(Settlement.CurrentSettlement); }
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_account",
                "bank_account_withdraw",
                "{=bank_account_withdraw}Withdraw",
                args =>
                {
                    var isLoanOverDueAtSettlement = IsLoanOverDueAtSettlement(Settlement.CurrentSettlement);
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    args.IsEnabled = !isLoanOverDueAtSettlement;
                    args.Tooltip = isLoanOverDueAtSettlement ? new TextObject("You must repay your loan.") : TextObject.Empty;
                    return true;
                },
                args => { PromptWithdrawAmount(Settlement.CurrentSettlement); }
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_account",
                "bank_account_take_out_loan",
                "{=bank_account_take_out_loan}Take out a loan",
                args =>
                {
                    var doesPlayerHaveActiveLoanAtSettlement = DoesPlayerHaveActiveLoanAtSettlement(Settlement.CurrentSettlement);
                    if (doesPlayerHaveActiveLoanAtSettlement)
                    {
                        return false;
                    }
                    var canLoan = MaxAvailableLoanAtSettlement(Settlement.CurrentSettlement) > 0;
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    args.IsEnabled = canLoan;
                    args.Tooltip = canLoan ? TextObject.Empty : new TextObject("{XORBERAX_SETTLEMENT_NAME}'s bank is unable to offer loans at this time.");
                    return true;
                },
                args => { PromptLoanAmount(Settlement.CurrentSettlement); }
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_account",
                "bank_account_repay_loan",
                "{=bank_account_repay_loan}Repay loan ({XORBERAX_BANKS_REMAINING_UNPAID_LOAN}{GOLD_ICON})",
                args =>
                {
                    var doesPlayerHaveActiveLoanAtSettlement = DoesPlayerHaveActiveLoanAtSettlement(Settlement.CurrentSettlement);
                    if (!doesPlayerHaveActiveLoanAtSettlement)
                    {
                        return false;
                    }
                    var doesPlayerHaveEnoughMoneyToRepayLoanAtSettlement = DoesPlayerHaveEnoughMoneyToRepayLoanAtSettlement(Settlement.CurrentSettlement);
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    args.IsEnabled = doesPlayerHaveEnoughMoneyToRepayLoanAtSettlement;
                    args.Tooltip = doesPlayerHaveEnoughMoneyToRepayLoanAtSettlement
                        ? TextObject.Empty
                        : new TextObject("You do not have enough money on your person to repay this loan.");
                    return true;
                },
                args => { RepayLoanAtSettlement(Settlement.CurrentSettlement); }
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_account",
                "bank_account_leave",
                "{=bank_account_leave}Leave bank",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => GameMenu.SwitchToMenu("town")
            );
        }

        private void RepayLoanAtSettlement(Settlement settlement)
        {
            var remainingUnpaidLoanAmount = GetRemainingUnpaidLoanAtSettlement(settlement);
            if (remainingUnpaidLoanAmount > GetPlayerMoneyOnPerson())
            {
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough money to repay this loan."));
                return;
            }
            var bankData = GetBankDataAtSettlement(settlement);
            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, settlement, remainingUnpaidLoanAmount);
            bankData.RemainingUnpaidLoan = 0;
            bankData.HasBankRetaliatedForUnpaidLoan = false;
            InformationManager.DisplayMessage(new InformationMessage("You have paid off your loan."));
            GameMenu.SwitchToMenu(GetBankMenuId(settlement));
        }

        private bool CanPlayerAffordToOpenBankAccountAtSettlement(Settlement settlement)
        {
            return GetPlayerMoneyOnPerson() >= GetBankAccountOpeningCostAtSettlement(settlement);
        }

        private bool DoesPlayerHaveEnoughMoneyToRepayLoanAtSettlement(Settlement settlement)
        {
            return GetPlayerMoneyOnPerson() >= GetRemainingUnpaidLoanAtSettlement(settlement);
        }

        private bool DoesPlayerHaveActiveLoanAtSettlement(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            return bankData.RemainingUnpaidLoan > 0;
        }

        private string GetBankMenuId(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            return bankData.HasAccount ? "bank_account" : "bank_setup";
        }

        private void OnOpenBankAccountAtSettlement(Settlement settlement)
        {
            if (TryOpenBankAccountAtSettlement(settlement))
            {
                GameMenu.SwitchToMenu("bank_account");
            }
        }

        private void UpdateBankMenuTextVariables()
        {
            MBTextManager.SetTextVariable("XORBERAX_SETTLEMENT_NAME", Settlement.CurrentSettlement.Name);
            MBTextManager.SetTextVariable("XORBERAX_BANKS_BALANCE", GetBalanceAtSettlement(Settlement.CurrentSettlement));
            MBTextManager.SetTextVariable("XORBERAX_BANKS_INTEREST_RATE", $"{GetInterestRateAtSettlement(Settlement.CurrentSettlement):0.0000}");
            MBTextManager.SetTextVariable("XORBERAX_BANKS_BANK_ACCOUNT_OPENING_COST", GetBankAccountOpeningCostAtSettlement(Settlement.CurrentSettlement));
            MBTextManager.SetTextVariable("XORBERAX_BANKS_REMAINING_UNPAID_LOAN", GetRemainingUnpaidLoanAtSettlement(Settlement.CurrentSettlement));
            MBTextManager.SetTextVariable("XORBERAX_BANKS_LOAN_INFO", BuildLoanInfoText(Settlement.CurrentSettlement));
        }

        private string BuildLoanInfoText(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            if (bankData.RemainingUnpaidLoan > 0)
            {
                return $"You have a loan of {bankData.RemainingUnpaidLoan}{{GOLD_ICON}} due on {GetLoanRepayDueDateAtSettlement(settlement)}.";
            }
            return string.Empty;
        }

        private CampaignTime GetLoanRepayDueDateAtSettlement(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            return CampaignTime.Days((int)(bankData.LoanStartDate.ToDays + SubModule.Config.DaysToRepayLoan));
        }

        private int GetRemainingUnpaidLoanAtSettlement(Settlement settlement)
        {
            return GetBankDataAtSettlement(settlement).RemainingUnpaidLoan;
        }

        private int GetBankAccountOpeningCostAtSettlement(Settlement settlement)
        {
            var settlementProsperityFactor = settlement.Prosperity >= SubModule.Config.BankAccountOpeningCostSettlementProsperityDivisor
                ? (int)(settlement.Prosperity / SubModule.Config.BankAccountOpeningCostSettlementProsperityDivisor)
                : 1;
            return SubModule.Config.BankAccountOpeningCostBase * settlementProsperityFactor;
        }

        private void PromptDepositAmount(Settlement settlement)
        {
            InformationManager.ShowTextInquiry(
                new TextInquiryData(
                    "Deposit",
                    "Amount",
                    true,
                    true,
                    "Deposit",
                    "Cancel",
                    amountText =>
                    {
                        var (isValid, amount) = TryParseDepositAmount(amountText);
                        if (isValid)
                        {
                            Deposit(amount, settlement);
                        }
                    },
                    () => { InformationManager.HideInquiry(); },
                    false,
                    amountText => TryParseDepositAmount(amountText).IsValid
                )
            );
        }

        private void PromptWithdrawAmount(Settlement settlement)
        {
            InformationManager.ShowTextInquiry(
                new TextInquiryData(
                    "Withdraw",
                    "Amount",
                    true,
                    true,
                    "Withdraw",
                    "Cancel",
                    amountText =>
                    {
                        var (isValid, amount) = TryParseWithdrawAmount(amountText, settlement);
                        if (isValid)
                        {
                            Withdraw(amount, settlement);
                        }
                    },
                    () => { InformationManager.HideInquiry(); },
                    false,
                    amountText => TryParseWithdrawAmount(amountText, settlement).IsValid
                )
            );
        }

        private void PromptLoanAmount(Settlement settlement)
        {
            InformationManager.ShowTextInquiry(
                new TextInquiryData(
                    "Loan",
                    $"You can take out a loan for up to {MaxAvailableLoanAtSettlement(settlement)} Denars. The amount must be repaid within {SubModule.Config.DaysToRepayLoan} days.",
                    true,
                    true,
                    "Take Out Loan",
                    "Cancel",
                    amountText =>
                    {
                        var (isValid, amount) = TryParseLoanAmount(amountText, settlement);
                        if (isValid)
                        {
                            TakeOutLoan(amount, settlement);
                        }
                    },
                    () => { InformationManager.HideInquiry(); },
                    false,
                    amountText => TryParseLoanAmount(amountText, settlement).IsValid
                )
            );
        }

        private void Deposit(int amount, Settlement settlement)
        {
            if (amount > Hero.MainHero.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough Denars to deposit."));
                return;
            }
            var bankData = GetBankDataAtSettlement(settlement);
            bankData.Balance += amount;
            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, settlement, amount, true);
            InformationManager.DisplayMessage(new InformationMessage($"Deposited {amount}{{GOLD_ICON}}.", "event:/ui/notification/coins_positive"));
            UpdateBankMenuTextVariables();
        }

        private void Withdraw(int amount, Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            if (amount > bankData.Balance)
            {
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough Denars to withdraw."));
                return;
            }
            var settlementComponent = settlement.GetSettlementComponent();
            if (amount > settlementComponent.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage($"The bank does not have enough funds available for you to withdraw. Only {settlementComponent.Gold} Denars are available."));
                return;
            }
            bankData.Balance -= amount;
            GiveGoldAction.ApplyForSettlementToCharacter(settlement, Hero.MainHero, amount, true);
            InformationManager.DisplayMessage(new InformationMessage($"Withdrew {amount}{{GOLD_ICON}}.", "event:/ui/notification/coins_positive"));
            UpdateBankMenuTextVariables();
        }

        private void TakeOutLoan(int amount, Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            var settlementComponent = settlement.GetSettlementComponent();
            if (amount > settlementComponent.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage($"This bank does not have enough funds to loan to you. Only {settlementComponent.Gold} Denars are available."));
                return;
            }
            bankData.LoanStartDate = CampaignTime.Now;
            bankData.RemainingUnpaidLoan = amount;
            GiveGoldAction.ApplyForSettlementToCharacter(settlement, Hero.MainHero, amount);
            UpdateBankMenuTextVariables();
            GameMenu.SwitchToMenu(GetBankMenuId(settlement));
        }

        private int MaxAvailableLoanAtSettlement(Settlement settlement)
        {
            var availableLoanAmount =
                (int)(MathF.Clamp(
                          Hero.MainHero.Clan.Renown * SubModule.Config.AvailableLoanAmountPerRenown,
                          0,
                          settlement.Prosperity
                      ) / SubModule.Config.availableLoanAmountDivisor
                ) * SubModule.Config.availableLoanAmountDivisor;
            return availableLoanAmount;
        }

        private float GetInterestRateAtSettlement(Settlement settlement)
        {
            return GetBankDataAtSettlement(settlement).InterestRate;
        }

        private int GetBalanceAtSettlement(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            if (bankData != null)
            {
                return bankData.Balance;
            }
            return 0;
        }

        private BankData GetBankDataAtSettlement(Settlement settlement)
        {
            if (_settlementBankDataBySettlementId.ContainsKey(settlement.StringId))
            {
                return _settlementBankDataBySettlementId[settlement.StringId];
            }
            return _settlementBankDataBySettlementId[settlement.StringId] = InitializeBankDataAtSettlement(settlement);
        }

        private bool TryOpenBankAccountAtSettlement(Settlement settlement)
        {
            var openingCost = GetBankAccountOpeningCostAtSettlement(settlement);
            if (openingCost > Hero.MainHero.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage("You cannot afford to open an account here."));
                return false;
            }
            var bankData = GetBankDataAtSettlement(settlement);
            bankData.HasAccount = true;
            bankData.AccountOpenDate = CampaignTime.Now;
            bankData.LastBankUpdateDate = CampaignTime.Now;
            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, settlement, openingCost);
            return true;
        }

        private BankData InitializeBankDataAtSettlement(Settlement settlement)
        {
            return new BankData
            {
                InterestRate = CalculateSettlementInterestRate(settlement)
            };
        }

        private float CalculateSettlementInterestRate(Settlement settlement)
        {
            return
                (settlement.Prosperity + settlement.GetSettlementComponent().Gold) *
                SubModule.Config.InterestRatePerSettlementProsperityFactor;
        }

        private (bool IsValid, int Amount) TryParseDepositAmount(string amountText)
        {
            if (int.TryParse(amountText, out var amount))
            {
                return (amount > 0 && amount <= GetPlayerMoneyOnPerson(), amount);
            }
            return (false, -1);
        }

        private (bool IsValid, int Amount) TryParseWithdrawAmount(string amountText, Settlement settlement)
        {
            if (int.TryParse(amountText, out var amount))
            {
                return (amount > 0 && amount <= GetBalanceAtSettlement(settlement), amount);
            }
            return (false, -1);
        }

        private (bool IsValid, int Amount) TryParseLoanAmount(string amountText, Settlement settlement)
        {
            if (int.TryParse(amountText, out var amount))
            {
                return (amount > 0 && amount <= MaxAvailableLoanAtSettlement(settlement), amount);
            }
            return (false, -1);
        }

        private static int GetPlayerMoneyOnPerson()
        {
            return Hero.MainHero.Gold;
        }
    }
}
