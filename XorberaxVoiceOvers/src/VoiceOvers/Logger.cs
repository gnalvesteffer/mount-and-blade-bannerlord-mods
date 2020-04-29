using TaleWorlds.Core;
using TaleWorlds.Library;

namespace VoiceOvers
{
    public static class Logger
    {
        public static void LogInfo(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage($"[VOICE FRAMEWORK - INFO] {message}", Colors.Cyan));
        }

        public static void LogError(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage($"[VOICE FRAMEWORK - ERROR] {message}", Colors.Red));
        }
    }
}
