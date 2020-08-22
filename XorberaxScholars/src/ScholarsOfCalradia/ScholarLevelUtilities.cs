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
                ScholarLevelPrefix = "a",
                ScholarLevelName = "Novice",
                ExperienceGainPerAttendee = SubModule.Config.NoviceScholarExperienceGain,
                CostPerAttendee = SubModule.Config.NoviceScholarCostPerAttendee,
                LectureDurationInHours = 4
            },
            new ScholarLevelInfo
            {
                ScholarLevelPrefix = "an",
                ScholarLevelName = "Intermediate",
                ExperienceGainPerAttendee = SubModule.Config.IntermediateScholarExperienceGain,
                CostPerAttendee = SubModule.Config.IntermediateScholarCostPerAttendee,
                LectureDurationInHours = 3
            },
            new ScholarLevelInfo
            {
                ScholarLevelPrefix = "an",
                ScholarLevelName = "Advanced",
                ExperienceGainPerAttendee = SubModule.Config.AdvancedScholarExperienceGain,
                CostPerAttendee = SubModule.Config.AdvancedScholarCostPerAttendee,
                LectureDurationInHours = 2
            },
            new ScholarLevelInfo
            {
                ScholarLevelPrefix = "an",
                ScholarLevelName = "Expert",
                ExperienceGainPerAttendee = SubModule.Config.ExpertScholarExperienceGain,
                CostPerAttendee = SubModule.Config.ExpertScholarCostPerAttendee,
                LectureDurationInHours = 1
            }
        };

        public static ScholarLevelInfo GetScholarLevelInfo(int scholarSeed)
        {
            return ScholarLevelInfo[scholarSeed % ScholarLevelInfo.Count];
        }
    }
}
