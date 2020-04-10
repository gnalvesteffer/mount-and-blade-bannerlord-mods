# Xorberax's Shoulder Cam
Changes the third person camera to an over-the-shoulder style.

## Configuration
Certain aspects of this mod can be tweaked to your liking.
Inside of `\XorberaxShoulderCam\bin\Win64_Shipping_Client` lives a
`config.json` file with settings that can be edited in any text editor.

### Settings

#### onFootPositionXOffset
The distance in meters to offset the camera on the X-Axis when on foot.

**Accepts:** decimal number

#### onFootPositionYOffset
The distance in meters to offset the camera on the Y-Axis when on foot.

**Accepts:** decimal number

#### onFootPositionZOffset
The distance in meters to offset the camera on the Z-Axis when on foot.

**Accepts:** decimal number

#### mountedPositionXOffset
The distance in meters to offset the camera on the X-Axis when on horse.

**Accepts:** decimal number

#### mountedPositionYOffset
The distance in meters to offset the camera on the Y-Axis when on horse.

**Accepts:** decimal number

#### mountedPositionZOffset
The distance in meters to offset the camera on the Z-Axis when on horse.

**Accepts:** decimal number

#### bearingOffset
Angle in radians to rotate the camera left/right (yaw).

**Accepts:** decimal number

#### elevationOffset
Angle in radians to rotate the camera up/down (pitch).

**Accepts:** decimal number

#### mountedDistanceOffset
The distance in meters to offset the camera away from the player when
riding a horse. 

**Accepts:** decimal number

#### thirdPersonFieldOfView
The angle in degrees to set the third person camera's field of view.

**Accepts:** decimal number

#### shoulderCamRangedMode
The behavior of the camera when using a ranged weapon, such as a bow.

**Accepts one of the following values:** 
- `"noRevert"` -- does not revert/reposition the camera when aiming or
equipping a ranged weapon (causes aiming to be inaccurate).
- `"revertWhenAiming"` -- reverts/repositions the camera when aiming with 
a ranged weapon.
- `"revertWhenEquipped"` -- reverts/repositions the camera when a ranged
weapon is equipped.

#### shoulderCamMountedMode
The behavior of the camera when riding on a horse.

**Accepts one of the following values:** 
- `"noRevert"` -- does not revert/reposition the camera when riding a
horse.
- `"revertWhenMounted"` -- reverts/repositions the camera when riding
a horse.

#### shoulderSwitchMode
Specifies the shoulder switching behavior.

**Accepts one of the following values:** 
- `"noSwitching"` -- the camera will remain locked to a single shoulder.
- `"matchAttackAndBlockDirection"` -- the camera will switch shoulders
depending on the attack/block direction.
- `"temporarilyMatchAttackAndBlockDirection"` -- the camera will switch
shoulders depending on the attack/block direction, and then revert back
to the original shoulder upon release of the attack/block
*(see `temporaryShoulderSwitchDuration`)*.

#### temporaryShoulderSwitchDuration
The number of seconds that the camera should remain switched to the
alternate shoulder when `shoulderSwitchMode` is set to
`temporarilyMatchAttackAndBlockDirection`.

**Accepts:** decimal number

#### minimumPlayerHitCamShake
The strength of the camera shake when the player is hit.

**Accepts:** decimal number

#### playerHitCamShakeMultiplier
The strength of additional camera shake when the player is hit, scaled
to the amount of damage received.

**Accepts:** decimal number

#### playerHitCamShakeDuration
The duration of the camera shake in seconds when the player is hit.

**Accepts:** decimal number

#### minimumEnemyHitCamShakeAmount
The strength of the camera shake when the player hits an enemy.

**Accepts:** decimal number

#### enemyHitCamShakeMultiplier
The strength of additional camera shake when the player hits an enemy,
scaled to the amount of damage delivered.

**Accepts:** decimal number

#### enemyHitCamShakeDuration
The duration of the camera shake in seconds when the player hits an 
enemy.

**Accepts:** decimal number

#### enableLiveConfigUpdates
When enabled, changes to the config will be applied instantly in-game.

**Accepts:** `true` or `false`