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
    internal class BanksCampaignBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, BankData> _settlementBankDataBySettlementId = new Dictionary<string, BankData>();

        internal static BanksCampaignBehavior Current { get; private set; }

        public BanksCampaignBehavior()
        {
            Current = this;
        }

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
                var hasMonthPassedSinceLastBankDataUpdate = (CampaignTime.Now - bankData.LastBankUpdateDate).ToDays >= CampaignTime.DaysInSeason;
                if (hasMonthPassedSinceLastBankDataUpdate)
                {
                    if (bankData.RemainingUnpaidLoan == 0)
                    {
                        var oldInterestRate = bankData.InterestRate;
                        var newInterestRate = CalculateSettlementInterestRate(settlement);
                        var moneyGainedFromInterest = (int)(bankData.Balance * oldInterestRate);
                        bankData.Balance += moneyGainedFromInterest;
                        bankData.InterestRate = newInterestRate;
                        InformationManager.DisplayMessage(new InformationMessage($"Your balance at the {settlement.Name} bank has gained {moneyGainedFromInterest}<img src=\"Icons\\Coin@2x\"> from interest.${(Mathf.Abs(newInterestRate - oldInterestRate) > 0.0001 ? $" Your interest rate has changed from {oldInterestRate:0.0000}% to {newInterestRate:0.0000}%." : string.Empty)}", "event:/ui/notification/coins_positive"));
                    }
                    bankData.LastBankUpdateDate = CampaignTime.Now;
                }
                if (IsLoanOverdueAtSettlement(settlement) && bankData.HasBankPerformedInitialRetaliationForUnpaidLoan && HasWeekElapsedSinceLastBankRetaliation(bankData))
                {
                    ApplyRecurringBankRetaliationForUnpaidLoan(settlement);
                }
            }
        }

        private static bool HasWeekElapsedSinceLastBankRetaliation(BankData bankData)
        {
            return (CampaignTime.Now - bankData.LastLoanRecurringRetaliationDate).ToDays >= CampaignTime.DaysInWeek;
        }

        private float CalculateLoanLateFeeInterestAtSettlement(Settlement settlement)
        {
            return settlement.Prosperity * SubModule.Config.LoanLateFeeInterestRatePerSettlementProsperityFactor;
        }

        internal void ApplyInitialBankRetaliationForUnpaidLoan(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            bankData.HasBankPerformedInitialRetaliationForUnpaidLoan = true;
            ChangeCrimeRatingAction.Apply(settlement.MapFaction, SubModule.Config.CrimeRatingIncreaseForUnpaidLoan);
            Hero.MainHero.Clan.Renown -= SubModule.Config.RenownLossForUnpaidLoan;
            InformationManager.DisplayMessage(new InformationMessage($"You failed to repay your loan. Lost {SubModule.Config.RenownLossForUnpaidLoan} renown."));
            InformationManager.ShowInquiry(
                new InquiryData(
                    "Loan Payment Overdue",
                    $"You failed to repay your loan on time with the {settlement.Name} bank. The {settlement.MapFaction.Name} will treat you as an outlaw if you do not repay your debts.",
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

        private void ApplyRecurringBankRetaliationForUnpaidLoan(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            bankData.LastLoanRecurringRetaliationDate = CampaignTime.Now;
            bankData.RemainingUnpaidLoan += (int)(bankData.RemainingUnpaidLoan * bankData.LoanLateFeeInterestRate);
            if (settlement.MapFaction.MainHeroCrimeRating < SubModule.Config.MaxCriminalRatingForUnpaidLoan)
            {
                var criminalRatingToApply =
                    settlement.MapFaction.MainHeroCrimeRating + SubModule.Config.RecurringCrimeRatingIncreaseForUnpaidLoan > SubModule.Config.MaxCriminalRatingForUnpaidLoan
                        ? SubModule.Config.MaxCriminalRatingForUnpaidLoan - settlement.MapFaction.MainHeroCrimeRating
                        : SubModule.Config.RecurringCrimeRatingIncreaseForUnpaidLoan;
                ChangeCrimeRatingAction.Apply(settlement.MapFaction, criminalRatingToApply);
                InformationManager.DisplayMessage(new InformationMessage($"Your criminal rating with the {settlement.MapFaction.Name} has increased by {criminalRatingToApply} due to an unpaid loan at {settlement.Name}.", Colors.Red));
            }
        }

        private bool IsLoanOverdueAtSettlement(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            return bankData.RemainingUnpaidLoan > 0 && (CampaignTime.Now - bankData.LoanEndDate).ToDays > 0;
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
                    args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;
                    if (IsLoanOverdueAtSettlement(Settlement.CurrentSettlement))
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("You must repay your loan.");
                    }
                    else if (GetBalanceAtSettlement(Settlement.CurrentSettlement) <= 0)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("You do not have any money to withdraw.");
                    }
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
                    var canLoan = doesPlayerHaveEnoughRenownToTakeOutLoan && MaxAvailableLoanAtSettlement(Settlement.CurrentSettlement) > 0;
                    args.optionLeaveType = GameMenuOption.LeaveType.RansomAndBribe;
                    args.IsEnabled = canLoan;
                    if (!canLoan)
                    {
                        args.Tooltip = new TextObject(
                            doesPlayerHaveEnoughRenownToTakeOutLoan
                                ? "{XORBERAX_BANKS_SETTLEMENT_NAME}'s bank is unable to offer loans at this time."
                                : $"You must have at least {SubModule.Config.MinimumRenownRequiredToTakeOutLoan} renown to take out a loan."
                        );
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
            InformationManager.DisplayMessage(new InformationMessage($"You repaid the loan of {remainingUnpaidLoanAmount}<img src=\"Icons\\Coin@2x\"> from {settlement.Name}.", "event:/ui/notification/coins_negative"));
            if (bankData.LoanQuest?.IsOngoing == true)
            {
                bankData.LoanQuest.OnLoanRepaidOnTime();
            }
            bankData.LoanQuest = null;
            bankData.LastLoanRecurringRetaliationDate = CampaignTime.Never;
            bankData.OriginalLoanAmount = 0;
            bankData.RemainingUnpaidLoan = 0;
            bankData.HasBankPerformedInitialRetaliationForUnpaidLoan = false;
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
                ? "You are at the {XORBERAX_BANKS_SETTLEMENT_NAME} bank.\nYour balance is {XORBERAX_BANKS_BALANCE}{GOLD_ICON} with a monthly interest rate of {XORBERAX_BANKS_INTEREST_RATE}%. {XORBERAX_BANKS_LOAN_INFO}"
                : "You are at the {XORBERAX_BANKS_SETTLEMENT_NAME} bank. You can open an account with a monthly interest rate of {XORBERAX_BANKS_INTEREST_RATE}%. {XORBERAX_BANKS_LOAN_INFO}";
        }

        private string BuildLoanInfoText(Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            if (bankData.RemainingUnpaidLoan > 0)
            {
                var isLoanOverdue = IsLoanOverdueAtSettlement(settlement);
                return $"You {(isLoanOverdue ? "had" : "have")} a loan of {bankData.OriginalLoanAmount}{{GOLD_ICON}} due on {GetLoanRepayDueDateAtSettlement(settlement)}{(bankData.AccruedLoanLateFees > 0 ? $", which has accrued {bankData.AccruedLoanLateFees}{{GOLD_ICON}} in late fees." : ".")}";
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
                    $"Enter the amount to deposit (1 - {GetPlayerMoneyOnPerson()}):",
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
                    $"Enter the amount to withdraw (1 - {GetBalanceAtSettlement(settlement)}):",
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
            var maxAvailableLoanAtSettlement = MaxAvailableLoanAtSettlement(settlement);
            InformationManager.ShowTextInquiry(
                new TextInquiryData(
                    "Loan",
                    $"You can take out a loan of up to {maxAvailableLoanAtSettlement}<img src=\"Icons\\Coin@2x\">. The amount must be repaid within {SubModule.Config.DaysToRepayLoan} days.\nEnter the amount to take out ({SubModule.Config.AvailableLoanAmountDivisor} - {maxAvailableLoanAtSettlement}):",
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
                    $"Are you sure you want to take out a loan of {amount}<img src=\"Icons\\Coin@2x\">? You will lose {CalculateRenownCostForLoanAmount(amount):0.00} renown.",
                    true,
                    true,
                    "Yes",
                    "No",
                    () => { TakeOutLoan(amount, settlement); },
                    () => InformationManager.HideInquiry()
                )
            );
        }

        private float CalculateRenownCostForLoanAmount(int loanAmount)
        {
            return Mathf.Max((float)loanAmount / SubModule.Config.AvailableLoanAmountPerRenown * SubModule.Config.RenownCostPerLoanAmountDivisor, 1);
        }

        private void Deposit(int amount, Settlement settlement)
        {
            if (amount > Hero.MainHero.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough money to deposit."));
                return;
            }
            var bankData = GetBankDataAtSettlement(settlement);
            bankData.Balance += amount;
            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, settlement, amount, true);
            InformationManager.DisplayMessage(new InformationMessage($"You deposited {amount}<img src=\"Icons\\Coin@2x\">.", "event:/ui/notification/coins_positive"));
            UpdateBankMenuTextVariables();
            GameMenu.SwitchToMenu("bank_account");
        }

        private void Withdraw(int amount, Settlement settlement)
        {
            var bankData = GetBankDataAtSettlement(settlement);
            if (amount > bankData.Balance)
            {
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough money to withdraw."));
                return;
            }
            var settlementComponent = settlement.GetSettlementComponent();
            if (amount > settlementComponent.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage($"The bank does not have enough funds available for you to withdraw. Only {settlementComponent.Gold}<img src=\"Icons\\Coin@2x\"> are available."));
                return;
            }
            bankData.Balance -= amount;
            GiveGoldAction.ApplyForSettlementToCharacter(settlement, Hero.MainHero, amount, true);
            InformationManager.DisplayMessage(new InformationMessage($"You withdrew {amount}<img src=\"Icons\\Coin@2x\">.", "event:/ui/notification/coins_positive"));
            UpdateBankMenuTextVariables();
            GameMenu.SwitchToMenu("bank_account");
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
            bankData.LastLoanRecurringRetaliationDate = CampaignTime.Never;
            bankData.OriginalLoanAmount = amount;
            bankData.RemainingUnpaidLoan = amount;
            bankData.LoanLateFeeInterestRate = CalculateLoanLateFeeInterestAtSettlement(settlement);
            bankData.LoanQuest = LoanQuest.Start(settlement, bankData.Banker, amount, bankData.LoanStartDate, bankData.LoanEndDate);
            var loanRenownCost = CalculateRenownCostForLoanAmount(amount);
            Hero.MainHero.Clan.Renown -= loanRenownCost;
            GiveGoldAction.ApplyForSettlementToCharacter(settlement, Hero.MainHero, amount, true);
            InformationManager.DisplayMessage(new InformationMessage($"You took out a loan for {amount}<img src=\"Icons\\Coin@2x\">. Lost {loanRenownCost:0.00} renown.", "event:/ui/notification/coins_positive"));
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
            if (!_settlementBankDataBySettlementId.ContainsKey(settlement.StringId))
            {
                _settlementBankDataBySettlementId[settlement.StringId] = new BankData();
            }
            var bankData = _settlementBankDataBySettlementId[settlement.StringId];
            if (bankData.InterestRate < 0)
            {
                bankData.InterestRate = CalculateSettlementInterestRate(settlement);
            }
            if (bankData.Banker == null) // v0.2.0 data migration
            {
                bankData.Banker = new Hero();
                bankData.Banker.Call("SetCharacterObject", settlement.Culture.Merchant);
                bankData.Banker.Name = new TextObject("Banker");
                bankData.Banker.Clan = new Clan
                {
                    Name = new TextObject($"Bank of {settlement.Name}"),
                };
                bankData.Banker.Clan.Call("SetLeader", bankData.Banker);
            }
            if (bankData.OriginalLoanAmount == 0 && bankData.RemainingUnpaidLoan > 0) // v0.2.0 data migration
            {
                bankData.OriginalLoanAmount = bankData.RemainingUnpaidLoan;
            }
            return bankData;
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
                return (amount >= SubModule.Config.AvailableLoanAmountDivisor && amount <= MaxAvailableLoanAtSettlement(settlement), amount);
            }
            return (false, -1);
        }

        private static int GetPlayerMoneyOnPerson()
        {
            return Hero.MainHero.Gold;
        }
    }
}
