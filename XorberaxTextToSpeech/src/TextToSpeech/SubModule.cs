﻿using System.IO;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using TaleWorlds.MountAndBlade;

namespace TextToSpeech
{
    internal class SubModule : MBSubModuleBase
    {
        private static readonly string ConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
        public static Config Config { get; private set; }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            LoadConfig();
            new Harmony("xorberax.texttospeech").PatchAll();
        }

        public static void LoadConfig()
        {
            if (!File.Exists(ConfigFilePath))
            {
                return;
            }
            try
            {
                Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigFilePath));
            }
            catch
            {
            }
        }
    }
}
