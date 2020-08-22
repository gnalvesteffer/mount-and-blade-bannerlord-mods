using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ScholarsOfCalradia
{
    public class ScholarCampaignBehavior : CampaignBehaviorBase
    {
        private static SkillObject[] _skills;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            _skills = DefaultSkills.GetAllSkills().ToArray();
            AddMenus(campaignGameStarter);
        }

        private void AddMenus(CampaignGameStarter campaignGameStarter)
        {
            // Town Menu
            campaignGameStarter.AddGameMenuOption(
                "town",
                "town_go_to_scholar",
                "{=town_scholar}Visit the local scholar",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.IsEnabled = true;
                    return true;
                },
                args => GameMenu.SwitchToMenu("scholar"),
                false,
                1
            );

            // College
            campaignGameStarter.AddGameMenu(
                "scholar",
                "{=scholar}You visit the town's scholar and see that he has an upcoming lecture about {XORBERAX_SCHOLAR_LECTURE_SKILL_NAME}. You and your companions can improve their {XORBERAX_SCHOLAR_LECTURE_SKILL_NAME} skill by partaking in today's lecture for {XORBERAX_SCHOLAR_LECTURE_COST_PER_ATTENDEE}{GOLD_ICON} per attendee.",
                args => UpdateMenuTextVariables(),
                GameOverlays.MenuOverlayType.SettlementWithBoth
            );
            campaignGameStarter.AddGameMenuOption(
                "scholar",
                "scholar_attend_lecture",
                "{=scholar_attend_lecture}Attend Lecture",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    return true;
                },
                args => { ShowAttendeeSelectionList(); }
            );
            campaignGameStarter.AddGameMenuOption(
                "scholar",
                "scholar_leave",
                "{=scholar_leave}Leave",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => GameMenu.SwitchToMenu("town")
            );
            // ToDo: Add wait menu for in-progress lecture.
            // campaignGameStarter.AddWaitGameMenu(
            //     "scholar_lecture_attend_wait",
            //     "{XORBERAX_SCHOLAR_LECTURE_IN_PROGRESS_DESCRIPTION}",
            //     null,
            //     args => { },
            //     null,
            //     (args, dt) => { },
            //     GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption
            // );
        }

        private void UpdateMenuTextVariables()
        {
            var currentSettlement = Settlement.CurrentSettlement;
            var lectureInfo = GetCurrentLectureInfoAtSettlement(currentSettlement);

            MBTextManager.SetTextVariable("XORBERAX_SCHOLAR_SETTLEMENT_NAME", currentSettlement);
            MBTextManager.SetTextVariable("XORBERAX_SCHOLAR_LECTURE_SKILL_NAME", lectureInfo.Skill.Name);
            MBTextManager.SetTextVariable("XORBERAX_SCHOLAR_LECTURE_COST_PER_ATTENDEE", lectureInfo.CostPerAttendee);
            MBTextManager.SetTextVariable("XORBERAX_SCHOLAR_LECTURE_IN_PROGRESS_DESCRIPTION", $""); // ToDo: add in-progress lecture info, include skill being lectured, and number of attendees.
        }

        private void ShowAttendeeSelectionList()
        {
            InformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    "Lecture Attendees",
                    "Select heroes to attend lecture:",
                    Hero.MainHero.PartyBelongedTo.MemberRoster
                        .Where(troopRosterElement => troopRosterElement.Character.IsHero)
                        .Select(troopRosterElement => troopRosterElement)
                        .Select(heroRosterElement => new InquiryElement(heroRosterElement.Character.HeroObject, heroRosterElement.Character.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(heroRosterElement.Character))))
                        .ToList(),
                    true,
                    -1,
                    "Continue",
                    null,
                    selectedHeroElements =>
                    {
                        if (selectedHeroElements?.Any() == false)
                        {
                            return;
                        }
                        InformationManager.HideInquiry();
                        SubModule.ExecuteActionOnNextTick(() =>
                        {
                            var lectureInfo = GetCurrentLectureInfoAtSettlement(Settlement.CurrentSettlement);
                            var totalLectureCost = selectedHeroElements.Count * lectureInfo.CostPerAttendee;
                            InformationManager.ShowInquiry(
                                new InquiryData(
                                    "Attend Lecture?",
                                    $"Attending this lecture will cost a total of {totalLectureCost}<img src=\"Icons\\Coin@2x\"> with {selectedHeroElements.Count} attendee(s). Do you wish to continue?",
                                    true,
                                    true,
                                    "Yes",
                                    "No",
                                    () => AttendLecture(lectureInfo, selectedHeroElements.Select(element => element.Identifier as Hero)),
                                    () => InformationManager.HideInquiry()
                                )
                            );
                        });
                    },
                    null
                )
            );
        }

        private LectureInfo GetCurrentLectureInfoAtSettlement(Settlement settlement)
        {
            return new LectureInfo
            {
                Skill = _skills[(settlement.Id.GetHashCode() + CampaignTime.Now.GetDayOfYear) % _skills.Length],
                ExperienceGainPerAttendee = 50, // ToDo: make this configurable or scale to something (maybe some sort of scholar quality?)
                CostPerAttendee = 500, // ToDo: make this configurable and scale to experience gain or settlement prosperity
            };
        }

        private void AttendLecture(LectureInfo lectureInfo, IEnumerable<Hero> attendees)
        {
            var totalLectureCost = lectureInfo.CostPerAttendee * attendees.Count();

            if (totalLectureCost > Hero.MainHero.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage("You cannot afford to attend this lecture."));
                return;
            }

            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, totalLectureCost);

            // ToDo: show wait menu, and then distribute skill XP after waiting completes.

            foreach (var attendee in attendees)
            {
                attendee.AddSkillXp(lectureInfo.Skill, lectureInfo.ExperienceGainPerAttendee);
            }
        }
    }
}
