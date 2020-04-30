using TaleWorlds.CampaignSystem;

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
        public static AgeGroup GetAgeGroup(this CharacterObject character)
        {
            var age = character.Age;
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
