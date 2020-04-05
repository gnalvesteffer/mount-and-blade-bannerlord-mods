using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace ShoulderCam
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            var harmony = new Harmony("xorberax.shouldercam");
            harmony.PatchAll();
        }
    }
}
