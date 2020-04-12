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
        private Dictionary<MBGUID, BankData> _settlementBankDataBySettlementId = new Dictionary<MBGUID, BankData>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
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
                args => { GameMenu.SwitchToMenu(GetBankMenuId(Settlement.CurrentSettlement)); },
                false,
                1
            );

            // Bank Setup
            campaignGameStarter.AddGameMenu(
                "bank_setup",
                "{=bank_setup}You are at the {XORBERAX_SETTLEMENT_NAME} bank. You can open an account with an interest rate of {XORBERAX_BANKS_INTEREST_RATE}% per month.",
                args => UpdateBankMenuTextVariables(),
                GameOverlays.MenuOverlayType.SettlementWithBoth
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_setup",
                "bank_setup_open_account",
                "{=bank_setup_open_account}Open Account ({XORBERAX_BANKS_BANK_ACCOUNT_OPENING_COST}{GOLD_ICON})",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
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
                "{=bank_account}You are at the {XORBERAX_SETTLEMENT_NAME} bank.\nYour balance is {XORBERAX_BANKS_BALANCE}{GOLD_ICON} with an interest rate of {XORBERAX_BANKS_INTEREST_RATE}% per month.",
                args => UpdateBankMenuTextVariables(),
                GameOverlays.MenuOverlayType.SettlementWithBoth
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_account",
                "bank_account_deposit",
                "{=bank_account_deposit}Deposit",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    return true;
                },
                args => { PromptDepositAmount(); }
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_account",
                "bank_account_withdraw",
                "{=bank_account_deposit}Withdraw",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    return true;
                },
                args => { PromptWithdrawAmount(); }
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

        private string GetBankMenuId(Settlement settlement)
        {
            var bankData = GetPlayerBankDataAtSettlement(settlement);
            return bankData.HasAccount ? "bank_account" : "bank_setup";
        }

        private void OnOpenBankAccountAtSettlement(Settlement settlement)
        {
            GameMenu.SwitchToMenu(TryOpenBankAccountAtSettlement(settlement) ? "bank_account" : "town");
        }

        private void UpdateBankMenuTextVariables()
        {
            MBTextManager.SetTextVariable("XORBERAX_SETTLEMENT_NAME", Settlement.CurrentSettlement.Name);
            MBTextManager.SetTextVariable("XORBERAX_BANKS_BALANCE", GetBalanceAtSettlement(Settlement.CurrentSettlement));
            MBTextManager.SetTextVariable("XORBERAX_BANKS_INTEREST_RATE", $"{GetInterestRateAtSettlement(Settlement.CurrentSettlement):0.000}");
            MBTextManager.SetTextVariable("XORBERAX_BANKS_BANK_ACCOUNT_OPENING_COST", GetBankAccountOpeningCost(Settlement.CurrentSettlement));
        }

        private int GetBankAccountOpeningCost(Settlement settlement)
        {
            var settlementProsperityFactor = settlement.Prosperity >= SubModule.Config.BankAccountOpeningCostSettlementProsperityDivisor
                ? settlement.Prosperity % SubModule.Config.BankAccountOpeningCostSettlementProsperityDivisor
                : 1;
            return (int)(SubModule.Config.BankAccountOpeningCostBase * settlementProsperityFactor);
        }

        private void PromptDepositAmount()
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
                            Deposit(amount, Settlement.CurrentSettlement);
                        }
                    },
                    () => { InformationManager.HideInquiry(); },
                    false,
                    amountText => TryParseDepositAmount(amountText).IsValid
                )
            );
        }

        private void PromptWithdrawAmount()
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
                        var (isValid, amount) = TryParseWithdrawAmount(amountText, Settlement.CurrentSettlement);
                        if (isValid)
                        {
                            Withdraw(amount, Settlement.CurrentSettlement);
                        }
                    },
                    () => { InformationManager.HideInquiry(); },
                    false,
                    amountText => TryParseWithdrawAmount(amountText, Settlement.CurrentSettlement).IsValid
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
            if (_settlementBankDataBySettlementId.ContainsKey(settlement.Id))
            {
                _settlementBankDataBySettlementId[settlement.Id].Balance += amount;
                GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, settlement, amount);
            }
            UpdateBankMenuTextVariables();
        }

        private void Withdraw(int amount, Settlement settlement)
        {
            if (amount > Hero.MainHero.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough Denars to withdraw."));
                return;
            }
            if (_settlementBankDataBySettlementId.ContainsKey(settlement.Id))
            {
                _settlementBankDataBySettlementId[settlement.Id].Balance -= amount;
                GiveGoldAction.ApplyForSettlementToCharacter(settlement, Hero.MainHero, amount);
            }
            UpdateBankMenuTextVariables();
        }

        private int MaxAvailableLoanAtSettlement(Settlement settlement)
        {
            return (int)MathF.Clamp(
                Hero.MainHero.Clan.Renown * SubModule.Config.AvailableLoanAmountPerRenown,
                0,
                settlement.Prosperity
            );
        }

        private float GetInterestRateAtSettlement(Settlement settlement)
        {
            return GetPlayerBankDataAtSettlement(settlement).InterestRate;
        }

        private int GetBalanceAtSettlement(Settlement settlement)
        {
            var bankData = GetPlayerBankDataAtSettlement(settlement);
            if (bankData != null)
            {
                return bankData.Balance;
            }
            return 0;
        }

        private BankData GetPlayerBankDataAtSettlement(Settlement settlement)
        {
            if (_settlementBankDataBySettlementId.ContainsKey(settlement.Id))
            {
                return _settlementBankDataBySettlementId[settlement.Id];
            }
            return _settlementBankDataBySettlementId[settlement.Id] = InitializeBankDataAtSettlement(settlement);
        }

        private bool TryOpenBankAccountAtSettlement(Settlement settlement)
        {
            var openingCost = GetBankAccountOpeningCost(settlement);
            if (openingCost > Hero.MainHero.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage("You cannot afford to open an account here."));
                return false;
            }
            var bankData = GetPlayerBankDataAtSettlement(settlement);
            bankData.HasAccount = true;
            bankData.AccountOpenDate = CampaignTime.Now;
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
            return settlement.Prosperity * SubModule.Config.InterestRatePerSettlementProsperityFactor;
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

        private static int GetPlayerMoneyOnPerson()
        {
            return Hero.MainHero.Gold;
        }
    }
}
