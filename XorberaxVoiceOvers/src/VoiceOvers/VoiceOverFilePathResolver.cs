using System.IO;
using TaleWorlds.Core;

namespace VoiceOvers
{
    public static class VoiceOverFilePathResolver
    {
        public static (string absoluteFilePath, string fileName) GetVoiceOverFilePath(string sentenceId, CultureCode characterCultureCode, bool isCharacterFemale, float characterAge)
        {
            var fileName = $"{sentenceId}_{characterCultureCode.ToString().ToLowerInvariant()}_{(isCharacterFemale ? "female" : "male")}.ogg";
            return (Path.GetFullPath(Path.Combine(SubModule.ExecutingAssemblyPath, $"../../data/{fileName}")), fileName);
        }
    }
}
