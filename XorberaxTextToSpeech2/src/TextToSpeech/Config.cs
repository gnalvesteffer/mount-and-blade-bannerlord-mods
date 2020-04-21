using Newtonsoft.Json;

namespace TextToSpeech
{
    internal class Config
    {
        [JsonProperty("watsonTextToSpeechUrl")]
        public string WatsonTextToSpeechUrl { get; set; }

        [JsonProperty("watsonTextToSpeechApiKey")]
        public string WatsonTextToSpeechApiKey { get; set; }

        [JsonProperty("watsonTextToSpeechFemaleVoice")]
        public string WatsonTextToSpeechFemaleVoice { get; set; }

        [JsonProperty("watsonTextToSpeechMaleVoice")]
        public string WatsonTextToSpeechMaleVoice { get; set; }
    }
}
