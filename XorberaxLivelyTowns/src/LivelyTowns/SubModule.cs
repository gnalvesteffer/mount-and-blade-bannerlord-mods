using TaleWorlds.MountAndBlade;

namespace LivelyTowns
{
    public class SubModule : MBSubModuleBase
    {
        public override void OnMissionBehaviourInitialize(Mission mission)
        {
            base.OnMissionBehaviourInitialize(mission);
            mission.AddMissionBehaviour(new LivelyTownsMissionBehavior(mission));
        }
    }
}
