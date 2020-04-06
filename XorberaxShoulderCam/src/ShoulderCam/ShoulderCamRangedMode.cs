using System.Runtime.Serialization;

namespace ShoulderCam
{
    public enum ShoulderCamRangedMode
    {
        [EnumMember(Value = "noRevert")] NoRevert,
        [EnumMember(Value = "revertWhenAiming")] RevertWhenAiming,
        [EnumMember(Value = "revertWhenEquipped")] RevertWhenEquipped,
    }
}
