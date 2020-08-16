# Xorberax's Cut Through Everyone
Allows units (such as yourself) to cut through multiple people depending
on the weapon, and opponent's armor.

## Configuration
Certain aspects of this mod can be tweaked to your liking.
Inside of `\XorberaxCutThroughEveryone\bin\Win64_Shipping_Client` lives 
a `config.json` file with settings that can be edited in any text
editor.

### Settings

#### onlyCutThroughWhenUnitIsKilled
When enabled, cutting through only occurs when a unit is killed by
the hit. 

**Accepts:** `true` or `false`

#### damageRetainedPerCut
The amount of damage retained per person cut.
Recommended to use a value between 0.0 through 1.0.

**Accepts:** decimal number

#### percentageOfInflictedDamageRequiredToCutThroughArmor
The percentage of inflicted damage required to cut through armor.
e.g. if `percentageOfInflictedDamageRequiredToCutThroughArmor` is `0.8`,
and an attack has a total of 100 points of damage, and 20 points are absorbed 
by armor, then 80 points of damage will be inflicted, which means 80% of
the attack damage was inflicted, allowing the attack to cut through.
 
**Accepts:** decimal number

#### doFriendlyUnitsBlockCutThroughs
When enabled, prevents weapons from cutting through friendly units.

**Accepts:** `true` or `false`

#### onlyPlayerCanCutThrough
When enabled, only the player will be allowed to cut through units.

**Accepts:** `true` or `false`

#### canCutThroughShields
When enabled, units can cut through shields.

**Accepts:** `true` or `false`

#### onlyPlayerCanCutThroughShields
When enabled with `canCutThroughShields`, only the player will be able
to cut through shields.

**Accepts:** `true` or `false`

#### shouldAutoReloadConfig
When enabled, will automatically apply changes made to the config
while in-game.

**Accepts:** `true` or `false`
