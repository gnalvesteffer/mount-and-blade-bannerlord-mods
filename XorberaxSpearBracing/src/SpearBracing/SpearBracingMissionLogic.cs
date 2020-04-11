using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SpearBracing
{
    public class SpearBracingMissionLogic : MissionLogic
    {
        private readonly Mission _mission;
        private readonly HashSet<Agent> _agentsToProcess = new HashSet<Agent>();
        private readonly HashSet<Agent> _agentsToStopProcessing = new HashSet<Agent>();

        public SpearBracingMissionLogic(Mission mission)
        {
            _mission = mission;
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            if (agent.IsHuman)
            {
                _agentsToProcess.Add(agent);
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
            _agentsToStopProcessing.Add(affectedAgent);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            foreach (var agent in _agentsToProcess)
            {
                if (IsAgentBracingPolearm(agent))
                {
                    HandleBracedPolearm(agent.WieldedWeapon.CurrentUsageItem, agent);
                }
            }
            foreach (var agentToStopProcessing in _agentsToStopProcessing)
            {
                _agentsToProcess.Remove(agentToStopProcessing);
            }
            _agentsToStopProcessing.Clear();
        }

        private static bool IsAgentBracingPolearm(Agent agent)
        {
            return
                agent.WieldedWeapon.CurrentUsageItem?.Item?.Type == ItemObject.ItemTypeEnum.Polearm &&
                (agent.AttackDirection == Agent.UsageDirection.AttackUp || agent.AttackDirection == Agent.UsageDirection.AttackDown);
        }

        private void HandleBracedPolearm(WeaponComponentData weapon, Agent wieldingAgent)
        {
            var weaponTipFrame = GetWeaponTipFrame(weapon, wieldingAgent);
            if (SubModule.Config.IsDebugMode)
            {
                MBDebug.RenderDebugDirectionArrow(weaponTipFrame.origin, weaponTipFrame.rotation.u);
                MBDebug.RenderDebugSphere(weaponTipFrame.origin, SubModule.Config.BoneCollisionRadius);
            }
            var agentsNearWeaponTip = _mission.GetNearbyAgents(weaponTipFrame.origin.AsVec2, 1.0f);
            foreach (var agentNearWeaponTip in agentsNearWeaponTip)
            {
                if (agentNearWeaponTip != wieldingAgent)
                {
                    HandleAgentNearWeaponTip(agentNearWeaponTip, wieldingAgent, weapon, weaponTipFrame);
                }
            }
        }

        private MatrixFrame GetWeaponTipFrame(WeaponComponentData weapon, Agent wieldingAgent)
        {
            var weaponLength = weapon.GetRealWeaponLength();
            var skeleton = wieldingAgent.AgentVisuals.GetSkeleton();
            var itemFrame = skeleton.GetBoneEntitialFrame((int)HumanBone.ItemR);
            var agentFrame = wieldingAgent.AgentVisuals.GetFrame();
            var transformedWeaponTipFrame = agentFrame.TransformToParent(itemFrame);
            var weaponTipOriginOffset = transformedWeaponTipFrame.rotation.u * weaponLength;
            transformedWeaponTipFrame.origin += weaponTipOriginOffset;
            return transformedWeaponTipFrame;
        }

        private void HandleAgentNearWeaponTip(
            Agent affectedAgent,
            Agent affectorAgent,
            WeaponComponentData weapon,
            MatrixFrame weaponTipFrame
        )
        {
            var impactedBone = GetImpactedBone(affectedAgent, weaponTipFrame);
            if (impactedBone == HumanBone.Invalid)
            {
                return;
            }
            var blow = CreateBlow(affectedAgent, impactedBone, affectorAgent, weapon, weaponTipFrame);
            affectedAgent.RegisterBlow(blow);
        }

        private Blow CreateBlow(
            Agent affectedAgent,
            HumanBone impactedBone,
            Agent affectorAgent,
            WeaponComponentData weapon,
            MatrixFrame weaponTipFrame
        )
        {
            var blow = new Blow
            {
                Position = weaponTipFrame.origin,
                Direction = weaponTipFrame.rotation.u,
                AttackType = AgentAttackType.Standard,
                InflictedDamage = (int)((weapon.ThrustDamage * affectedAgent.Velocity.Length) + (weapon.ThrustDamage * affectorAgent.Velocity.Length)),
                DamageType = DamageTypes.Pierce,
                VictimBodyPart = affectedAgent.AgentVisuals.GetBoneTypeDataList()[(int)impactedBone].BodyPartType,
                BaseMagnitude = 1.0f,
                StrikeType = StrikeType.Thrust,
            };
            return blow;
        }

        private static HumanBone GetImpactedBone(Agent affectedAgent, MatrixFrame weaponTipFrame)
        {
            var skeleton = affectedAgent.AgentVisuals.GetSkeleton();
            var agentFrame = affectedAgent.AgentVisuals.GetGlobalFrame();
            foreach (var bone in (HumanBone[])Enum.GetValues(typeof(HumanBone)))
            {
                if (bone == HumanBone.Invalid)
                {
                    continue;
                }
                var boneFrame = skeleton.GetBoneEntitialFrame((int)bone);
                var bonePosition = agentFrame.TransformToParent(boneFrame);
                if (bonePosition.origin.Distance(weaponTipFrame.origin) <= SubModule.Config.BoneCollisionRadius)
                {
                    return bone;
                }
            }
            return HumanBone.Invalid;
        }
    }
}
