using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace LivelyTowns
{
    public class LivelyTownsMissionBehavior : MissionLogic
    {
        private readonly Mission _mission;

        public LivelyTownsMissionBehavior(Mission mission)
        {
            _mission = mission;
        }

        public override void AfterStart()
        {
            base.AfterStart();
            foreach (var characterObjectTemplate in CharacterObject.Templates)
            {
                characterObjectTemplate.Name = characterObjectTemplate.Name ?? new TextObject("NPC");
                var agent = _mission.SpawnAgent(new AgentBuildData(characterObjectTemplate));
                agent.TeleportToPosition(_mission.MainAgent.Position);
            }
        }

        private void Log(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage($"LivelyTowns: {message}"));
        }
    }
}
