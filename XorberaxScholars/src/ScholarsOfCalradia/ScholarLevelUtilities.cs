using System.Collections.Generic;
using TaleWorlds.Core;

namespace ScholarsOfCalradia
{
    public static class ScholarLevelUtilities
    {
        private static readonly List<ScholarLevelInfo> ScholarLevelInfo = new List<ScholarLevelInfo>
        {
            new ScholarLevelInfo
            {
                ScholarLevelName = "Novice",
                ExperienceGainPerAttendee = SubModule.Config.NoviceScholarExperienceGain,
                CostPerAttendee = SubModule.Config.NoviceScholarCostPerAttendee,
                LectureDurationInHours = SubModule.Config.NoviceLectureDurationInHours
            },
            new ScholarLevelInfo
            {
                ScholarLevelName = "Intermediate",
                ExperienceGainPerAttendee = SubModule.Config.IntermediateScholarExperienceGain,
                CostPerAttendee = SubModule.Config.IntermediateScholarCostPerAttendee,
                LectureDurationInHours = SubModule.Config.IntermediateLectureDurationInHours
            },
            new ScholarLevelInfo
            {
                ScholarLevelName = "Advanced",
                ExperienceGainPerAttendee = SubModule.Config.AdvancedScholarExperienceGain,
                CostPerAttendee = SubModule.Config.AdvancedScholarCostPerAttendee,
                LectureDurationInHours = SubModule.Config.AdvancedLectureDurationInHours
            },
            new ScholarLevelInfo
            {
                ScholarLevelName = "Expert",
                ExperienceGainPerAttendee = SubModule.Config.ExpertScholarExperienceGain,
                CostPerAttendee = SubModule.Config.ExpertScholarCostPerAttendee,
                LectureDurationInHours = SubModule.Config.ExpertLectureDurationInHours
            }
        };

        public static ScholarLevelInfo GetScholarLevelInfo(int scholarSeed)
        {
            return ScholarLevelInfo[scholarSeed % ScholarLevelInfo.Count];
        }
    }
}
