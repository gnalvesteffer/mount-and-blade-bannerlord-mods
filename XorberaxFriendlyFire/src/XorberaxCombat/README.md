# Xorberax's Friendly Fire
Enables friendly fire for all units, e.g. you can hurt your troops,
they can hurt each other, and your enemies can do the same.

## Configuration
Certain aspects of this mod can be tweaked to your liking.
Inside of `XorberaxFriendlyFire\bin\Win64_Shipping_Client` lives a
`config.json` file with settings that can be edited in any text editor.


### Settings
#### logFriendlyFire
This can be set to either `true` or `false`. 
Setting this to `true` will log a message to the chat log whenever a 
friendly fire incident occurs on your team.

#### friendlyFireLogMessageColorHex
This can be set to a hexadecimal color code value. This color will be
used for the friendly fire message that appears when the
`logFriendlyFire` setting is set to `true`.

#### friendlyFireEnabledMissionModes
This can be set to an array of mission modes, specifying which types of
missions friendly fire should be enabled for. 

Here's the setting with all possible values
*(note: `"StartUp"` allows you to slaughter villagers!)*:
```
"friendlyFireEnabledMissionModes": [
  "StartUp",
  "Conversation",
  "Battle",
  "Duel",
  "Stealth",
  "Barter",
  "Deployment",
  "Tournament",
  "Replay"
]
```