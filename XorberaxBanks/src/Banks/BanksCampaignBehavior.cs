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
                dataStore.SyncData("settlementBankData", ref _settlementBankDataBySettlementId);
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
            // Bank Setup
            campaignGameStarter.AddGameMenu(
                "bank_setup",
                "{=bank_setup}You are at the {SETTLEMENT_NAME} Bank. You can open an account with an interest rate of {INTEREST_RATE}% per month.",
                args => UpdateBankMenuTextVariables(),
                GameOverlays.MenuOverlayType.SettlementWithBoth
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_setup",
                "bank_setup_open_account",
                "{=bank_deposit}Open Account ({BANK_ACCOUNT_COST}{GOLD_ICON})",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Default;
                    return true;
                },
                args => { OnOpenBankAccountAtSettlement(Settlement.CurrentSettlement); }
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_setup",
                "bank_setup_open_account",
                "{=bank_deposit}Open Account",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Default;
                    return true;
                },
                args => { OnOpenBankAccountAtSettlement(Settlement.CurrentSettlement); }
            );

            // Bank Account
            campaignGameStarter.AddGameMenu(
                "bank_account",
                "{=bank_account}You are at the {SETTLEMENT_NAME} Bank. Your balance is {BALANCE}{GOLD_ICON} with an interest rate of {INTEREST_RATE}% per month.",
                args => UpdateBankMenuTextVariables(),
                GameOverlays.MenuOverlayType.SettlementWithBoth
            );
            campaignGameStarter.AddGameMenuOption(
                "bank_account",
                "bank_account_deposit",
                "{=bank_account_deposit}Deposit",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Default;
                    return true;
                },
                args => { PromptDepositAmount(); }
            );

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
                4
            );
        }

        private string GetBankMenuId(Settlement settlement)
        {
            var bankData = GetPlayerBankDataAtSettlement(settlement);
            return bankData.HasAccount ? "bank_account" : "bank_setup";
        }

        private void OnOpenBankAccountAtSettlement(Settlement settlement)
        {
            OpenBankAccountAtSettlement(settlement);
            GameMenu.SwitchToMenu("");
        }

        private void UpdateBankMenuTextVariables()
        {
            MBTextManager.SetTextVariable("SETTLEMENT_NAME", Settlement.CurrentSettlement.Name);
            MBTextManager.SetTextVariable("BALANCE", GetBalanceAtSettlement(Settlement.CurrentSettlement));
            MBTextManager.SetTextVariable("INTEREST_RATE", $"{GetInterestRateAtSettlement(Settlement.CurrentSettlement):0.000}");
            MBTextManager.SetTextVariable("BANK_ACCOUNT_COST", GetBankAccountOpeningCost(Settlement.CurrentSettlement));
        }

        private int GetBankAccountOpeningCost(Settlement settlement)
        {
            var settlementProsperityFactor = settlement.Prosperity % SubModule.Config.BankAccountOpeningCostSettlementProsperityDivisor;
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

        private void Deposit(int amount, Settlement settlement)
        {
            if (amount > Hero.MainHero.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough Denars to deposit."));
                return;
            }
            var settlementId = settlement.Id;
            if (_settlementBankDataBySettlementId.ContainsKey(settlementId))
            {
                _settlementBankDataBySettlementId[settlementId].Balance += amount;
                GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, settlement, amount);
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

        private void OpenBankAccountAtSettlement(Settlement settlement)
        {
            var bankData = GetPlayerBankDataAtSettlement(settlement);
            bankData.AccountOpenDate = CampaignTime.Now;
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

        private static (bool IsValid, int Amount) TryParseDepositAmount(string amountText)
        {
            if (int.TryParse(amountText, out var amount))
            {
                return (amount > 0 && amount <= GetPlayerMoneyOnPerson(), amount);
            }
            return (false, -1);
        }

        private static int GetPlayerMoneyOnPerson()
        {
            return Hero.MainHero.Gold;
        }
    }
}
