using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TrainingField
{
    internal static class MobilePartyExtensions
    {
        public static int GetHighestSkillValueInParty(this MobileParty party, SkillObject skill)
        {
            var highestSkillValue = 0;
            for (var memberIndex = 0; memberIndex < party.MemberRoster.Count; ++memberIndex)
            {
                var characterAtIndex = party.MemberRoster.GetCharacterAtIndex(memberIndex);
                if (!characterAtIndex.IsHero || characterAtIndex.HeroObject.IsWounded)
                {
                    continue;
                }
                var skillValue = characterAtIndex.GetSkillValue(skill);
                if (skillValue > highestSkillValue)
                {
                    highestSkillValue = skillValue;
                }
            }
            return highestSkillValue;
        }
    }
}
