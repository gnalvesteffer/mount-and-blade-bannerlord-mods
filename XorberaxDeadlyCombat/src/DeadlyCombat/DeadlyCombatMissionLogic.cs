using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DeadlyCombat
{
    public class DeadlyCombatMissionLogic : MissionLogic
    {
        private class AgentInfo
        {
            public float InitialHitTimestamp { get; set; }
            public Blow InitialBlow { get; set; }
            public float BleedOutRate { get; set; }
            public float LifeSpanSinceInitialHit { get; set; }
        }

        private static readonly ActionIndexCache BleedOutActionIndexCache = ActionIndexCache.Create("Bleed Out");

        private static readonly HashSet<BoneBodyPartType> VitalBodyParts = new HashSet<BoneBodyPartType>
        {
            BoneBodyPartType.Head,
            BoneBodyPartType.Neck,
            BoneBodyPartType.Chest,
            BoneBodyPartType.Abdomen
        };

        private Dictionary<Agent, AgentInfo> _agentsThatAreBleedingOut = new Dictionary<Agent, AgentInfo>();

        public override void OnRegisterBlow(
            Agent attacker,
            Agent victim,
            GameEntity realHitEntity,
            Blow blow,
            ref AttackCollisionData collisionData,
            in MissionWeapon attackerWeapon
        )
        {
            base.OnRegisterBlow(attacker, victim, realHitEntity, blow, ref collisionData, attackerWeapon);

            if (
                victim != null &&
                blow.InflictedDamage > blow.AbsorbedByArmor &&
                VitalBodyParts.Contains(blow.VictimBodyPart)
            )
            {
                if (blow.InflictedDamage >= victim.HealthLimit * SubModule.Config.PercentageOfDamageRequiredToKillUnit)
                {
                    victim.Die(blow);
                    _agentsThatAreBleedingOut.Remove(victim);
                }
                else if (
                    !_agentsThatAreBleedingOut.ContainsKey(victim) &&
                    victim.Health / victim.HealthLimit <= SubModule.Config.UnitHealthPercentageToCauseBleedout
                )
                {
                    _agentsThatAreBleedingOut.Add(
                        victim,
                        new AgentInfo
                        {
                            InitialHitTimestamp = Mission.Time,
                            InitialBlow = blow,
                            BleedOutRate = blow.InflictedDamage
                        }
                    );
                }
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            var agentsToRemoveFromBleedOut = new List<Agent>();
            foreach (var agentInfoPair in _agentsThatAreBleedingOut)
            {
                var agent = agentInfoPair.Key;
                var agentInfo = agentInfoPair.Value;
                var initialTime = agentInfo.LifeSpanSinceInitialHit;
                agentInfo.LifeSpanSinceInitialHit += dt;
                var hasSecondElapsed = MathF.Floor(agentInfo.LifeSpanSinceInitialHit) - MathF.Floor(initialTime) >= 1;
                if (hasSecondElapsed)
                {
                    agent.SetMaximumSpeedLimit(SubModule.Config.UnitSpeedReductionRateDuringBleedout, true);
                    agent.Health -= agentInfo.BleedOutRate;
                }

                if (agent.Health <= 0)
                {
                    agent.Die(
                        agentInfo.InitialBlow.IsMissile
                            ? new Blow(agentInfo.InitialBlow.OwnerId)
                            : agentInfo.InitialBlow
                    );
                    agentsToRemoveFromBleedOut.Add(agent);
                }
            }

            foreach (var agent in agentsToRemoveFromBleedOut)
            {
                _agentsThatAreBleedingOut.Remove(agent);
            }
        }
    }
}