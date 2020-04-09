using System.Runtime.Serialization;

namespace ShoulderCam
{
    public enum ShoulderSwitchMode
    {
        [EnumMember(Value = "noSwitching")] NoSwitching,
        [EnumMember(Value = "matchAttackAndBlockDirection")] MatchAttackAndBlockDirection,
        [EnumMember(Value = "temporarilyMatchAttackAndBlockDirection")] TemporarilyMatchAttackAndBlockDirection,
    }
}
