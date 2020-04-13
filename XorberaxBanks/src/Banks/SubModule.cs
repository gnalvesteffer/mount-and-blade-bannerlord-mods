using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Banks
{
    internal class SubModule : MBSubModuleBase
    {
        private static readonly string ConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
        private static readonly List<Action> ActionsToExecuteNextTick = new List<Action>();
        public static Config Config { get; private set; }

        public static void ExecuteActionOnNextTick(Action action)
        {
            if (action == null)
            {
                return;
            }
            ActionsToExecuteNextTick.Add(action);
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            LoadConfig();
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            foreach (var action in ActionsToExecuteNextTick)
            {
                action();
            }
            ActionsToExecuteNextTick.Clear();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);
            var campaign = game.GameType as Campaign;
            if (campaign == null)
            {
                return;
            }
            var campaignGameStarter = gameStarter as CampaignGameStarter;
            campaignGameStarter?.AddBehavior(new BanksCampaignBehavior());
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
