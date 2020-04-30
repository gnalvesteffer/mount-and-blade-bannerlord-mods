using System;
using System.IO;
using CSCore;
using CSCore.Codecs.OGG;
using CSCore.SoundOut;
using TaleWorlds.Core;

namespace VoiceOvers
{
    public static class DialogHandler
    {
        private static ISoundOut _soundOut;
        private static IWaveSource _waveSource;

        public static void SayDialog(string sentenceId, CultureCode characterCultureCode, bool isCharacterFemale, AgeGroup ageGroup)
        {
            var absoluteFilePath = VoiceOverFilePathResolver.GetVoiceOverFilePath(sentenceId, characterCultureCode, isCharacterFemale, ageGroup).absoluteFilePath;
            if (!File.Exists(absoluteFilePath))
            {
                return;
            }
            PlayVoiceOverSafe(absoluteFilePath);
        }

        public static void StopDialog()
        {
            _soundOut?.Stop();
        }

        private static void PlayVoiceOverSafe(string voiceOverFilePath)
        {
            try
            {
                _waveSource = new OggSource(new MemoryStream(File.ReadAllBytes(voiceOverFilePath))).ToWaveSource();
                _soundOut?.Stop();
                _soundOut = new WasapiOut();
                _soundOut.Initialize(_waveSource);
                _soundOut.Play();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception.Message);
            }
        }
    }
}
