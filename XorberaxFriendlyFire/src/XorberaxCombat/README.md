# Xorberax's Friendly Fire
Enables friendly fire for all units, e.g. you can hurt your troops, 
they can hurt each other, and your enemies can do the same.

## Configuration
Certain aspects of this mod can be tweaked to your liking.
Inside of `XorberaxFriendlyFire\bin\Win64_Shipping_Client` lives a
`config.json` file with settings that can be edited in any text editor.

### Settings

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