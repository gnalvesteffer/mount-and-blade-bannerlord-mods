using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Core;

namespace FriendlyFire
{
    public class Config
    {
        [JsonProperty("friendlyFireEnabledMissionModes")]
        public HashSet<MissionMode> FriendlyFireEnabledMissionModes { get; set; }

        [JsonProperty("logFriendlyFire")]
        public bool ShouldLogFriendlyFire { get; set; }

        [JsonProperty("friendlyFireLogMessageColorHex")]
        public string FriendlyFireLogMessageColorHex { get; set; }
    }
}
