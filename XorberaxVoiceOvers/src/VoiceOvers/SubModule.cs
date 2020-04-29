using System.IO;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using TaleWorlds.MountAndBlade;

namespace VoiceOvers
{
    internal class SubModule : MBSubModuleBase
    {
        internal static readonly string ExecutingAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string ConfigFilePath = Path.Combine(ExecutingAssemblyPath, "config.json");
        public static Config Config { get; private set; }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            LoadConfig();
            new Harmony("xorberax.voiceovers").PatchAll();
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            if (Config.IsDevMode)
            {
                LoadConfig();
            }
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
