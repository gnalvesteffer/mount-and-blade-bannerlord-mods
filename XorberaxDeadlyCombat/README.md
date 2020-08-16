# Xorberax's Deadly Combat
Causes strong hits to vital body parts such as the torso and head to
bleed-out units, or even kill them.

## Configuration
Certain aspects of this mod can be tweaked to your liking.
Inside of `\XorberaxDeadlyCombat\bin\Win64_Shipping_Client` lives 
a `config.json` file with settings that can be edited in any text
editor.

### Settings

#### percentageOfDamageRequiredToKillUnit
The percentage of damage an attack must inflict in comparison to the
victim's total health, e.g. a value of `0.25` means that an attack must
inflict at least 25% of the victim's total health to cause the victim to
be instantly killed.

**Accepts:** decimal number between `0.0` and `1.0`

#### unitHealthPercentageToCauseBleedout
The percentage of a unit's health that will trigger them to bleed-out.

**Accepts:** decimal number between `0.0` and `1.0`

#### unitSpeedReductionRateDuringBleedout
The percentage to reduce a unit's speed each second during bleed-out.

**Accepts:** decimal number between `0.0` and `1.0`

#### shouldAutoReloadConfig
When enabled, will automatically apply changes made to the config
while in-game.

**Accepts:** `true` or `false`
