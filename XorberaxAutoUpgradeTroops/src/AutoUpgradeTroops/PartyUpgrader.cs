using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment.Managers;
using TaleWorlds.Core;

namespace AutoUpgradeTroops
{
    internal class PartyUpgrader
    {
        // TODO: track total cost, and only upgrade if player can afford X number of this upgrade to prevent spending all their money.
        public static void UpgradeParty(PartyBase party)
        {
            var memberRoster = party.MemberRoster;
            var troopUpgradeMetadataCollection = new List<TroopUpgradeMetadata>();
            for (var memberIndex = 0; memberIndex < memberRoster.Count; ++memberIndex)
            {
                var elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(memberIndex);
                if (!elementCopyAtIndex.HasHigherTierToUpgradeTo())
                {
                    continue;
                }

                var upgradeXpCostPerUnit = elementCopyAtIndex.Character.UpgradeXpCost;
                var totalUnitsReadyToUpgradeInElement = elementCopyAtIndex.NumberReadyToUpgrade;
                for (var upgradeTargetIndex = 0; upgradeTargetIndex < elementCopyAtIndex.Character.UpgradeTargets.Length; ++upgradeTargetIndex)
                {
                    var totalUnitsToUpgradeInElement = totalUnitsReadyToUpgradeInElement;
                    var upgradeTarget = elementCopyAtIndex.Character.UpgradeTargets[upgradeTargetIndex]; // the higher tier unit type to upgrade to
                    var costToUpgradeToHigherTierPerUnit = elementCopyAtIndex.Character.UpgradeCost(party, upgradeTargetIndex);
                    var totalCostToUpgradeToHigherTier = totalUnitsToUpgradeInElement * costToUpgradeToHigherTierPerUnit;
                    if (party.LeaderHero != null && costToUpgradeToHigherTierPerUnit != 0 && totalCostToUpgradeToHigherTier > party.LeaderHero.Gold)
                    {
                        totalUnitsToUpgradeInElement = party.LeaderHero.Gold / costToUpgradeToHigherTierPerUnit;
                    }
                    var canUpgrade = true;
                    if (elementCopyAtIndex.Character.UpgradeTargets[upgradeTargetIndex].UpgradeRequiresItemFromCategory != null)
                    {
                        canUpgrade = false;
                        var totalItemsAvailableToSupplyTroopUpgrade = 0;
                        foreach (var itemRosterElement in party.ItemRoster)
                        {
                            if (itemRosterElement.EquipmentElement.Item.ItemCategory == upgradeTarget.UpgradeRequiresItemFromCategory)
                            {
                                totalItemsAvailableToSupplyTroopUpgrade += itemRosterElement.Amount;
                                canUpgrade = true;
                                if (totalItemsAvailableToSupplyTroopUpgrade >= totalUnitsToUpgradeInElement)
                                {
                                    break;
                                }
                            }
                        }
                        if (canUpgrade)
                        {
                            totalUnitsToUpgradeInElement = Math.Min(totalItemsAvailableToSupplyTroopUpgrade, totalUnitsToUpgradeInElement);
                        }
                    }
                    if (party.Culture.IsBandit)
                    {
                        canUpgrade = elementCopyAtIndex.Character.UpgradeTargets[upgradeTargetIndex].Culture.IsBandit;
                    }
                    if (canUpgrade && totalUnitsToUpgradeInElement > 0)
                    {
                        troopUpgradeMetadataCollection.Add(
                            new TroopUpgradeMetadata(
                                memberIndex,
                                elementCopyAtIndex.Character.UpgradeTargets[upgradeTargetIndex],
                                totalUnitsToUpgradeInElement,
                                costToUpgradeToHigherTierPerUnit
                            )
                        );
                    }
                }
                if (troopUpgradeMetadataCollection.Any())
                {
                    var randomElement = troopUpgradeMetadataCollection.GetRandomElement();
                    var upgradedTierCharacterObject = randomElement.CharacterUpgradeTarget;
                    var totalUnitsToUpgradeInElement = randomElement.TotalUnitsToUpgradeInElement;
                    var costToUpgradeToHigherTierPerUnit = randomElement.CostToUpgradeToHigherTierPerUnit;
                    var totalUpgradeXpCost = upgradeXpCostPerUnit * totalUnitsToUpgradeInElement;
                    memberRoster.SetElementXp(memberIndex, memberRoster.GetElementXp(memberIndex) - totalUpgradeXpCost);
                    memberRoster.AddToCounts(elementCopyAtIndex.Character, -totalUnitsToUpgradeInElement);
                    memberRoster.AddToCounts(upgradedTierCharacterObject, totalUnitsToUpgradeInElement);
                    if (upgradedTierCharacterObject.UpgradeRequiresItemFromCategory != null)
                    {
                        var newTotalUnitsToUpgradeInElement = totalUnitsToUpgradeInElement;
                        foreach (var itemRosterElement in party.ItemRoster)
                        {
                            if (itemRosterElement.EquipmentElement.Item.ItemCategory == upgradedTierCharacterObject.UpgradeRequiresItemFromCategory)
                            {
                                var totalUnitsToUpgradeInElementDueToItemRequirements = Math.Min(newTotalUnitsToUpgradeInElement, itemRosterElement.Amount);
                                party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement.Item, -totalUnitsToUpgradeInElementDueToItemRequirements);
                                newTotalUnitsToUpgradeInElement -= totalUnitsToUpgradeInElementDueToItemRequirements;
                                if (newTotalUnitsToUpgradeInElement == 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    var totalCostToUpgradeUnitsInElement = costToUpgradeToHigherTierPerUnit * totalUnitsToUpgradeInElement;
                    if (party.Owner.Gold < totalCostToUpgradeUnitsInElement)
                    {
                        totalUnitsToUpgradeInElement = party.Owner.Gold / costToUpgradeToHigherTierPerUnit;
                    }
                    if (totalUnitsToUpgradeInElement > 0)
                    {
                        if (party.Owner != null)
                        {
                            SkillLevelingManager.OnUpgradeTroops(party, upgradedTierCharacterObject, totalUnitsToUpgradeInElement);
                            GiveGoldAction.ApplyBetweenCharacters(party.Owner, null, totalCostToUpgradeUnitsInElement, true);
                        }
                        else if (party.LeaderHero != null)
                        {
                            SkillLevelingManager.OnUpgradeTroops(party, upgradedTierCharacterObject, totalUnitsToUpgradeInElement);
                            GiveGoldAction.ApplyBetweenCharacters(party.LeaderHero, null, totalCostToUpgradeUnitsInElement, true);
                        }
                    }
                }
            }
        }
    }
}
