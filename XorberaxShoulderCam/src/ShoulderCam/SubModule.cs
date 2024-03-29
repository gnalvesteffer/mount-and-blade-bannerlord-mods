﻿using System.IO;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using TaleWorlds.MountAndBlade;

namespace ShoulderCam
{
    public class SubModule : MBSubModuleBase
    {
        private static readonly string ConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
        public static Config Config { get; private set; }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            LoadConfig();
            var harmony = new Harmony("xorberax.shouldercam");
            harmony.PatchAll();
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            if (Config.AreLiveConfigUpdatesEnabled)
            {
                LoadConfig();
            }
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            mission.MissionBehaviors.Add(new ShoulderCamMissionLogic());
        }

        private static void LoadConfig()
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
