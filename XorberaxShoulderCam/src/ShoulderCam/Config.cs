using Newtonsoft.Json;

namespace ShoulderCam
{
    public class Config
    {
        [JsonProperty("onFootPositionXOffset")]
        public float OnFootPositionXOffset { get; set; }

        [JsonProperty("onFootPositionYOffset")]
        public float OnFootPositionYOffset { get; set; }

        [JsonProperty("onFootPositionZOffset")]
        public float OnFootPositionZOffset { get; set; }

        [JsonProperty("mountedPositionXOffset")]
        public float MountedPositionXOffset { get; set; }

        [JsonProperty("mountedPositionYOffset")]
        public float MountedPositionYOffset { get; set; }

        [JsonProperty("mountedPositionZOffset")]
        public float MountedPositionZOffset { get; set; }

        [JsonProperty("bearingOffset")]
        public float BearingOffset { get; set; }

        [JsonProperty("elevationOffset")]
        public float ElevationOffset { get; set; }

        [JsonProperty("thirdPersonFieldOfView")]
        public float ThirdPersonFieldOfView { get; set; }

        [JsonProperty("shoulderCamRangedMode")]
        public ShoulderCamRangedMode ShoulderCamRangedMode { get; set; }

        [JsonProperty("shoulderCamMountedMode")]
        public ShoulderCamMountedMode ShoulderCamMountedMode { get; set; }

        [JsonProperty("shoulderSwitchMode")]
        public ShoulderSwitchMode ShoulderSwitchMode { get; set; }

        [JsonProperty("temporaryShoulderSwitchDuration")]
        public float TemporaryShoulderSwitchDuration { get; set; }

        [JsonProperty("minimumPlayerHitCamShake")]
        public float MinimumPlayerHitCamShake { get; set; }

        [JsonProperty("playerHitCamShakeMultiplier")]
        public float PlayerHitCamShakeMultiplier { get; set; }

        [JsonProperty("playerHitCamShakeDuration")]
        public float PlayerHitCamShakeDuration { get; set; }

        [JsonProperty("minimumEnemyHitCamShakeAmount")]
        public float MinimumEnemyHitCamShakeAmount { get; set; }

        [JsonProperty("enemyHitCamShakeMultiplier")]
        public float EnemyHitCamShakeMultiplier { get; set; }

        [JsonProperty("enemyHitCamShakeDuration")]
        public float EnemyHitCamShakeDuration { get; set; }

        [JsonProperty("enableLiveConfigUpdates")]
        public bool AreLiveConfigUpdatesEnabled { get; set; }
    }
}
