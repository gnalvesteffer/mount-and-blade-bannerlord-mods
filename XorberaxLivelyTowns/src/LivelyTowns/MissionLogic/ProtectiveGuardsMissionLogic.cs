using System.Collections.Generic;
using System.Linq;
using SandBox;
using SandBox.Source.Missions.Handlers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace LivelyTowns.MissionLogic
{
    public class ProtectiveGuardsMissionLogic : TaleWorlds.MountAndBlade.MissionLogic
    {
        private static int MaxGuards = 20;

        private readonly Mission _mission;
        private readonly Settlement _settlement;

        private HashSet<Agent> Guards = new HashSet<Agent>();

        public ProtectiveGuardsMissionLogic(Mission mission)
        {
            _mission = mission;
            _settlement = Settlement.CurrentSettlement;
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, int damage, int weaponKind, int currentWeaponUsageIndex)
        {
            base.OnAgentHit(affectedAgent, affectorAgent, damage, weaponKind, currentWeaponUsageIndex);
            if (
                affectedAgent.Team.MBTeam.Index == -1 &&
                Guards.Count < MaxGuards &&
                !Guards.Any(guard => guard.Position.Distance(affectedAgent.Position) <= SubModule.Config.GuardAlertDistance)
            )
            {
                var guard = SpawnGuard();
                guard.TeleportToPosition(
                    _mission.GetRandomPositionAroundPoint(
                        affectedAgent.Position,
                        SubModule.Config.GuardMinSpawnDistance,
                        SubModule.Config.GuardMaxSpawnDistance
                    )
                );
                guard.SetTargetPositionAndDirection(affectedAgent.Position.AsVec2, (affectorAgent.Position - affectedAgent.Position).NormalizedCopy());
            }
            if (Guards.Contains(affectedAgent) && affectedAgent.Health < 0.1f) // a guard died
            {
                Guards.Remove(affectedAgent);
            }
        }

        private void DisplayMessage(string message, string hexColor)
        {
            InformationManager.DisplayMessage(new InformationMessage($"LivelyTowns: {message}", Color.ConvertStringToColor(hexColor)));
        }

        private Agent SpawnGuard()
        {
            var guard = _mission.SpawnAgent(new AgentBuildData(GetGuardCharacterObject()));
            Guards.Add(guard);
            return guard;
        }

        private CharacterObject GetGuardCharacterObject()
        {
            return _settlement.Culture.Guard;
        }
    }
}
