using System.Collections.Generic;

namespace TextToSpeech
{
    internal class Config
    {
        public bool IsDeveloperMode { get; set; }
        public int SpeechRate { get; set; }
        public Dictionary<string, string> DialogReplacements { get; set; }
        public Dictionary<string, CultureVoiceDefinition> CultureVoices { get; set; }
    }
}