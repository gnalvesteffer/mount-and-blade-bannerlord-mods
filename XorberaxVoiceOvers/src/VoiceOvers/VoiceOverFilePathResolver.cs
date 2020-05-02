using System.IO;
using TaleWorlds.Core;

namespace VoiceOvers
{
    public static class VoiceOverFilePathResolver
    {
        private static readonly string DataDirectoryPath = Path.GetFullPath(Path.Combine(SubModule.ExecutingAssemblyPath, "../../data/"));

        public static (string genericAbsoluteFilePath, string genericFileName, string npcAbsoluteFilePath, string npcFileName) GetVoiceOverFileData(string npcId, string sentenceId, CultureCode characterCultureCode, bool isCharacterFemale, AgeGroup ageGroup)
        {
            var genericFileName = $"{sentenceId}_{characterCultureCode.ToString().ToLowerInvariant()}_{ageGroup.ToString().ToLowerInvariant()}_{(isCharacterFemale ? "female" : "male")}.ogg";
            var npcFileName = $"{npcId}_{sentenceId}.ogg";
            return (Path.Combine(DataDirectoryPath, genericFileName), genericFileName, Path.Combine(DataDirectoryPath, npcFileName), npcFileName);
        }
    }
}
