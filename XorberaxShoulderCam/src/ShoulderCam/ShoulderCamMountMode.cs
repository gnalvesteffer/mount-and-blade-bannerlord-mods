using System.Runtime.Serialization;

namespace ShoulderCam
{
    public enum ShoulderCamMountedMode
    {
        [EnumMember(Value = "noRevert")] NoRevert,
        [EnumMember(Value = "revertWhenMounted")] RevertWhenMounted,
    }
}
