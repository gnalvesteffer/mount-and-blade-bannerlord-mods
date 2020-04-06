using Newtonsoft.Json;

namespace ShoulderCam
{
    public class Config
    {
        [JsonProperty("positionXOffset")]
        public float PositionXOffset { get; set; }

        [JsonProperty("positionZOffset")]
        public float PositionZOffset { get; set; }


        [JsonProperty("bearingOffset")]
        public float BearingOffset { get; set; }

        [JsonProperty("elevationOffset")]
        public float ElevationOffset { get; set; }

        [JsonProperty("mountedDistanceOffset")]
        public float MountedDistanceOffset { get; set; }

        [JsonProperty("shoulderCamRangedMode")]
        public ShoulderCamRangedMode ShoulderCamRangedMode { get; set; }
    }
}
