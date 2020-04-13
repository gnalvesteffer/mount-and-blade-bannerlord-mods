using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.TwoDimension;

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
                var hasWeekPassedSinceLastBankDataUpdate = (CampaignTime.Now - bankData.LastBankUpdateDate).ToWeeks >= 1;
                if (hasWeekPassedSinceLastBankDataUpdate)
                {
                    if (bankData.RemainingUnpaidLoan == 0)
                    {
                        bankData.Balance += (int)(bankData.Balance * bankData.InterestRate);
                    }
                    bankData.LastBankUpdateDate = CampaignTime.Now;
                }
                if (IsLoanOverdueAtSettlement(settlement))
                {
                    if (bankData.HasBankRetaliatedForUnpaidLoan)
                    {
                        if (hasWeekPassedSinceLastBankDataUpdate)
                        {
                            ApplyWeeklyBankRetaliationForUnpaidLoan(settlement);
                        }
                    }
                    else
                    {
                        ApplyInitialBankRetaliationForUnpaidLoan(settlement);
                    }
                }
            }
        }

        private float CalculateLoanLateFeeInterestAtSettlement(Settlement settlement)
        {
            return settlement.Prosperity * SubModule.Config.LoanLateFeeInterestRatePerSettlementProsperityFactor;
        }

        private void ApplyInitialBankRetaliationForUnpaidLoan(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            bankData.HasBankRetaliatedForUnpaidLoan = true;
            ChangeCrimeRatingAction.Apply(settlement.MapFaction, SubModule.Config.CrimeRatingIncreaseForUnpaidLoan);
            Hero.MainHero.Clan.Renown -= SubModule.Config.RenownLossForUnpaidLoan;
            InformationManager.DisplayMessage(new InformationMessage($"You failed to repay your loan. Lost {SubModule.Config.RenownLossForUnpaidLoan} renown."));
            InformationManager.ShowInquiry(
                new InquiryData(
                    "Loan Payment Overdue",
                    $"You have failed to repay your loan with the {settlement.Name} bank. The {settlement.MapFaction.InformalName} will treat you as an outlaw if you do not repay your debts.",
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

        private void ApplyWeeklyBankRetaliationForUnpaidLoan(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            bankData.RemainingUnpaidLoan += (int)(bankData.RemainingUnpaidLoan * bankData.LoanLateFeeInterestRate);
            ChangeCrimeRatingAction.Apply(settlement.MapFaction, SubModule.Config.RecurringCrimeRatingIncreaseForUnpaidLoan);
        }

        private bool IsLoanOverdueAtSettlement(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            return bankData.RemainingUnpaidLoan > 0 && (CampaignTime.Now - bankData.LoanEndDate).ToDays >= 0;
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
                    var (canPlayerAccessBank, reasonMessage) = CanPlayerAccessBankAtSettlement(Settlement.CurrentSettlement);
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.IsEnabled = canPlayerAccessBank;
                    args.Tooltip = canPlayerAccessBank ? TextObject.Empty : new TextObject(reasonMessage);
                    return true;
                },
                args => GameMenu.SwitchToMenu("bank_account"),
                false,
                1
            );

            // Bank Account
            campaignGameStarter.AddGameMenu(
                "bank_account",
                "{=bank_account_info}{XORBERAX_BANKS_BANK_ACCOUNT_INFO}",
                args => UpdateBankMenuTextVariables(),
                GameOverlays.MenuOverlayType.SettlementWithBoth
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_account",
                "bank_account_open_account",
                "{=bank_account_open_account}Open Account ({XORBERAX_BANKS_BANK_ACCOUNT_OPENING_COST}{GOLD_ICON})",
                args =>
                {
                    if (DoesPlayerHaveAccountAtSettlementBank(Settlement.CurrentSettlement))
                    {
                        return false;
                    }
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
                "bank_account",
                "bank_account_deposit",
                "{=bank_account_deposit}Deposit",
                args =>
                {
                    if (!DoesPlayerHaveAccountAtSettlementBank(Settlement.CurrentSettlement))
                    {
                        return false;
                    }
                    var isLoanOverdueAtSettlement = IsLoanOverdueAtSettlement(Settlement.CurrentSettlement);
                    args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;
                    args.IsEnabled = !isLoanOverdueAtSettlement;
                    args.Tooltip = isLoanOverdueAtSettlement ? new TextObject("You must repay your loan.") : TextObject.Empty;
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
                    if (!DoesPlayerHaveAccountAtSettlementBank(Settlement.CurrentSettlement))
                    {
                        return false;
                    }
                    var isLoanOverdueAtSettlement = IsLoanOverdueAtSettlement(Settlement.CurrentSettlement);
                    args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;
                    args.IsEnabled = !isLoanOverdueAtSettlement;
                    args.Tooltip = isLoanOverdueAtSettlement ? new TextObject("You must repay your loan.") : TextObject.Empty;
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
                    var doesPlayerHaveEnoughRenownToTakeOutLoan = DoesPlayerHaveEnoughRenownToTakeOutLoan();
                    var canLoan = MaxAvailableLoanAtSettlement(Settlement.CurrentSettlement) > 0;
                    args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;
                    args.IsEnabled = canLoan;
                    if (doesPlayerHaveEnoughRenownToTakeOutLoan)
                    {
                        args.Tooltip = canLoan ? TextObject.Empty : new TextObject("{XORBERAX_BANKS_SETTLEMENT_NAME}'s bank is unable to offer loans at this time.");
                    }
                    else
                    {
                        args.Tooltip = new TextObject($"You must have at least {SubModule.Config.MinimumRenownRequiredToTakeOutLoan} renown to take out a loan.");
                    }
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
                    args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;
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

        private bool DoesPlayerHaveAccountAtSettlementBank(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            return bankData.HasAccount;
        }

        private static bool DoesPlayerHaveEnoughRenownToTakeOutLoan()
        {
            return (int)Hero.MainHero.Clan.Renown >= SubModule.Config.MinimumRenownRequiredToTakeOutLoan;
        }

        private (bool CanAccess, string ReasonMessage) CanPlayerAccessBankAtSettlement(Settlement settlement)
        {
            if (!CampaignTime.Now.IsDayTime)
            {
                return (false, "The bank is closed at night.");
            }
            var isPlayerEnemyOfFaction = FactionManager.IsAtWarAgainstFaction(settlement.MapFaction, Hero.MainHero.MapFaction);
            return (!isPlayerEnemyOfFaction, isPlayerEnemyOfFaction ? "You cannot access the bank with your current reputation." : string.Empty);
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
            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, settlement, remainingUnpaidLoanAmount, true);
            InformationManager.DisplayMessage(new InformationMessage($"You have paid off your loan of {remainingUnpaidLoanAmount}<img src=\"Icons\\Coin@2x\">.", "event:/ui/notification/coins_negative"));
            bankData.RemainingUnpaidLoan = 0;
            bankData.HasBankRetaliatedForUnpaidLoan = false;
            GameMenu.SwitchToMenu("bank_account");
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

        private void OnOpenBankAccountAtSettlement(Settlement settlement)
        {
            if (TryOpenBankAccountAtSettlement(settlement))
            {
                GameMenu.SwitchToMenu("bank_account");
            }
        }

        private void UpdateBankMenuTextVariables()
        {
            MBTextManager.SetTextVariable("XORBERAX_BANKS_SETTLEMENT_NAME", Settlement.CurrentSettlement.Name);
            MBTextManager.SetTextVariable("XORBERAX_BANKS_BALANCE", GetBalanceAtSettlement(Settlement.CurrentSettlement));
            MBTextManager.SetTextVariable("XORBERAX_BANKS_INTEREST_RATE", $"{GetInterestRateAtSettlement(Settlement.CurrentSettlement):0.0000}");
            MBTextManager.SetTextVariable("XORBERAX_BANKS_BANK_ACCOUNT_OPENING_COST", GetBankAccountOpeningCostAtSettlement(Settlement.CurrentSettlement));
            MBTextManager.SetTextVariable("XORBERAX_BANKS_REMAINING_UNPAID_LOAN", GetRemainingUnpaidLoanAtSettlement(Settlement.CurrentSettlement));
            MBTextManager.SetTextVariable("XORBERAX_BANKS_LOAN_INFO", BuildLoanInfoText(Settlement.CurrentSettlement));
            MBTextManager.SetTextVariable("XORBERAX_BANKS_BANK_ACCOUNT_INFO", BuildBankAccountInfoText(Settlement.CurrentSettlement));
        }

        private string BuildBankAccountInfoText(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            return bankData.HasAccount
                ? "You are at the {XORBERAX_BANKS_SETTLEMENT_NAME} bank.\nYour balance is {XORBERAX_BANKS_BALANCE}{GOLD_ICON} with a weekly interest rate of {XORBERAX_BANKS_INTEREST_RATE}%. {XORBERAX_BANKS_LOAN_INFO}"
                : "You are at the {XORBERAX_BANKS_SETTLEMENT_NAME} bank. You can open an account with a weekly interest rate of {XORBERAX_BANKS_INTEREST_RATE}%. {XORBERAX_BANKS_LOAN_INFO}";
        }

        private string BuildLoanInfoText(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            if (bankData.RemainingUnpaidLoan > 0)
            {
                var isLoanOverdue = IsLoanOverdueAtSettlement(settlement);
                return $"You {(isLoanOverdue ? "had" : "have")} a loan of {bankData.RemainingUnpaidLoan}{{GOLD_ICON}} due on {GetLoanRepayDueDateAtSettlement(settlement)}.";
            }
            return string.Empty;
        }

        private CampaignTime GetLoanRepayDueDateAtSettlement(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            return bankData.LoanEndDate;
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
                    "Enter the amount to deposit:",
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
                    "Enter the amount to withdraw:",
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
                    $"You can take out a loan for up to {MaxAvailableLoanAtSettlement(settlement)}<img src=\"Icons\\Coin@2x\">. The amount must be repaid within {SubModule.Config.DaysToRepayLoan} days.\nEnter the amount to take out:",
                    true,
                    true,
                    "Take Out Loan",
                    "Cancel",
                    amountText =>
                    {
                        var (isValid, amount) = TryParseLoanAmount(amountText, settlement);
                        if (isValid)
                        {
                            SubModule.ExecuteActionOnNextTick(() => PromptLoanConfirmation(amount, settlement)); // HACK: there's a bug that prevents a child inquiry from invoking the affirmative/negative actions.
                        }
                    },
                    () => { InformationManager.HideInquiry(); },
                    false,
                    amountText => TryParseLoanAmount(amountText, settlement).IsValid
                )
            );
        }

        private void PromptLoanConfirmation(int amount, Settlement settlement)
        {
            InformationManager.ShowInquiry(
                new InquiryData(
                    "Loan",
                    $"Are you sure you want to take out a loan of {amount}<img src=\"Icons\\Coin@2x\">? You will lose {CalculateRenownCostForLoanAmount(amount)} renown.",
                    true,
                    true,
                    "Yes",
                    "No",
                    () => { TakeOutLoan(amount, settlement); },
                    () => InformationManager.HideInquiry()
                )
            );
        }

        private int CalculateRenownCostForLoanAmount(int loanAmount)
        {
            return (int)Mathf.Max(loanAmount / SubModule.Config.AvailableLoanAmountDivisor * SubModule.Config.RenownCostPerLoanAmountDivisor, 1);
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
            InformationManager.DisplayMessage(new InformationMessage($"You deposited {amount}<img src=\"Icons\\Coin@2x\">.", "event:/ui/notification/coins_positive"));
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
            InformationManager.DisplayMessage(new InformationMessage($"You withdrew {amount}<img src=\"Icons\\Coin@2x\">.", "event:/ui/notification/coins_positive"));
            UpdateBankMenuTextVariables();
        }

        private void TakeOutLoan(int amount, Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            var settlementComponent = settlement.GetSettlementComponent();
            if (amount > settlementComponent.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage($"This bank does not have enough funds to loan to you. Only {settlementComponent.Gold}<img src=\"Icons\\Coin@2x\"> are available."));
                return;
            }
            bankData.LoanStartDate = CampaignTime.Now;
            bankData.RemainingUnpaidLoan = amount;
            bankData.LoanLateFeeInterestRate = CalculateLoanLateFeeInterestAtSettlement(settlement);
            var loanRenownCost = CalculateRenownCostForLoanAmount(amount);
            Hero.MainHero.Clan.Renown -= loanRenownCost;
            GiveGoldAction.ApplyForSettlementToCharacter(settlement, Hero.MainHero, amount, true);
            InformationManager.DisplayMessage(new InformationMessage($"You took out a loan for {amount}<img src=\"Icons\\Coin@2x\">. Lost {loanRenownCost} renown.", "event:/ui/notification/coins_positive"));
            UpdateBankMenuTextVariables();
            GameMenu.SwitchToMenu("bank_account");
        }

        private int MaxAvailableLoanAtSettlement(Settlement settlement)
        {
            var availableLoanAmount =
                (int)(MathF.Clamp(
                          Hero.MainHero.Clan.Renown * SubModule.Config.AvailableLoanAmountPerRenown,
                          0,
                          settlement.GetSettlementComponent().Gold
                      ) / SubModule.Config.AvailableLoanAmountDivisor
                ) * SubModule.Config.AvailableLoanAmountDivisor;
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
