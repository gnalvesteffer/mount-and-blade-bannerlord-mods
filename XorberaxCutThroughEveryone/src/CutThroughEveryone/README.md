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