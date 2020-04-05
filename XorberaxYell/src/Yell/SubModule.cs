using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace Yell
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            var mission = Mission.Current;
            if (mission == null)
            {
                return;
            }
            var agent = mission.MainAgent;
            var shouldYell = InputKey.V.IsPressed() && agent?.IsPlayerControlled == true;
            if (shouldYell)
            {
                agent.SetWantsToYell();
            }
        }
    }
}
