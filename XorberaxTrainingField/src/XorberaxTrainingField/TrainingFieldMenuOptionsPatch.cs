using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;

namespace TrainingField
{
    [HarmonyPatch(typeof(StoryMode.Behaviors.TrainingFieldCampaignBehavior))]
    [HarmonyPatch("OnSessionLaunched")]
    internal static class TrainingFieldMenuOptionsPatch
    {
        private static void Postfix(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption(
                "training_field_menu",
                "training_field_train_troops",
                "{=training_field_train_troops}Train your troops.",
                menuCallbackArgs =>
                {
                    menuCallbackArgs.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    return TrainingFieldCampaignBehavior.Current.HasTroopsToTrain;
                },
                menuCallbackArgs => TrainingFieldCampaignBehavior.Current.BeginTraining(),
                false,
                1
            );
            campaignGameStarter.AddWaitGameMenu(
                "training_field_train_troops_wait",
                "{=training_field_train_troops_wait}Your troops train tirelessly throughout the day, increasing their combat effectiveness.",
                null,
                args =>
                {
                    args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(TrainingFieldCampaignBehavior.Current.MaximumNumberOfHoursToTrain, 0);
                    args.MenuContext.GameMenu.AllowWaitingAutomatically();
                    return true;
                },
                null,
                (args, dt) =>
                {
                    args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(TrainingFieldCampaignBehavior.Current.TrainingProgress);
                },
                GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption
            );
            campaignGameStarter.AddGameMenuOption(
                "training_field_train_troops_wait",
                "training_field_train_troops_wait_end_training",
                "{=training_field_train_troops_wait_end_training}End training early.",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => TrainingFieldCampaignBehavior.Current.CancelTraining()
            );
        }
    }
}
