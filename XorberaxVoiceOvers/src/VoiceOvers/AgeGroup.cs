using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace VoiceOvers
{
    public enum AgeGroup
    {
        Child,
        Teen,
        Adult,
        Elder,
    }

    public static class AgeGroupExtensions
    {
        public static AgeGroup GetAgeGroup(this Agent agent)
        {
            var age = agent.Age;
            if (age < 12)
            {
                return AgeGroup.Child;
            }
            if (age < 18)
            {
                return AgeGroup.Teen;
            }
            if (age < 50)
            {
                return AgeGroup.Adult;
            }
            return AgeGroup.Elder;
        }
    }
}
