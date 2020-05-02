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

        public static void SayDialog(string npcId, string sentenceId, CultureCode characterCultureCode, bool isCharacterFemale, AgeGroup ageGroup)
        {
            var voiceOverFileData = VoiceOverFilePathResolver.GetVoiceOverFileData(npcId, sentenceId, characterCultureCode, isCharacterFemale, ageGroup);
            if (File.Exists(voiceOverFileData.npcAbsoluteFilePath))
            {
                PlayVoiceOverSafe(voiceOverFileData.npcAbsoluteFilePath);
            }
            else if (File.Exists(voiceOverFileData.genericAbsoluteFilePath))
            {
                PlayVoiceOverSafe(voiceOverFileData.genericAbsoluteFilePath);
            }
        }

        public static void StopDialog()
        {
            _soundOut?.Stop();
        }

        private static void PlayVoiceOverSafe(string voiceOverFilePath)
        {
            try
            {
                StopDialog();
                _waveSource = new OggSource(new MemoryStream(File.ReadAllBytes(voiceOverFilePath))).ToWaveSource();
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
