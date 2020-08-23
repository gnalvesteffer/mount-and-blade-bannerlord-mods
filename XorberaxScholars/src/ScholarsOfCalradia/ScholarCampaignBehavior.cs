using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace ScholarsOfCalradia
{
    public class ScholarCampaignBehavior : CampaignBehaviorBase
    {
        private static readonly int MaxDailyRandomNumber = 100;
        private static SkillObject[] _skills;

        private int _dailyRandomNumber = 1;
        private List<string> _settlementIdsOfSettlementsLecturedAtToday = new List<string>();
        private List<string> _heroIdsOfLectureAttendees = new List<string>();
        private string _lectureSkillId;
        private int _lectureDurationInHours;
        private int _lectureExperienceGainPerAttendee;
        private CampaignTime _startTimeOfLecture;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_dailyRandomNumber", ref _dailyRandomNumber);
            dataStore.SyncData("_settlementIdsOfSettlementsLecturedAtToday", ref _settlementIdsOfSettlementsLecturedAtToday);
            dataStore.SyncData("_heroIdsOfLectureAttendees", ref _heroIdsOfLectureAttendees);
            dataStore.SyncData("_lectureSkillId", ref _lectureSkillId);
            dataStore.SyncData("_lectureDurationInHours", ref _lectureDurationInHours);
            dataStore.SyncData("_lectureExperiencePerAttendee", ref _lectureExperienceGainPerAttendee);
            dataStore.SyncData("_startTimeOfLecture", ref _startTimeOfLecture);
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            _skills = DefaultSkills.GetAllSkills().ToArray();
            AddMenus(campaignGameStarter);
        }

        private void OnDailyTick()
        {
            _dailyRandomNumber = MBRandom.RandomInt(1, MaxDailyRandomNumber);
            _settlementIdsOfSettlementsLecturedAtToday.Clear();
        }

        private void AddMenus(CampaignGameStarter campaignGameStarter)
        {
            // Town menu
            campaignGameStarter.AddGameMenuOption(
                "town",
                "town_go_to_scholar",
                "{=town_scholar}Go to the scholar",
                args =>
                {
                    var lectureInfo = GetCurrentLectureInfoAtSettlement(Settlement.CurrentSettlement);
                    if (lectureInfo == null)
                    {
                        return false;
                    }
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    args.Tooltip = new TextObject("A scholar is providing a lecture to this town.");
                    args.IsEnabled = true;
                    return true;
                },
                args => GameMenu.SwitchToMenu("scholar"),
                false,
                1
            );

            // Scholar menus
            campaignGameStarter.AddGameMenu(
                "scholar",
                "{=scholar}You visit the {XORBERAX_SCHOLAR_LEVEL} scholar and see that he has an upcoming lecture about {XORBERAX_SCHOLAR_LECTURE_SKILL_NAME}. You and your companions can improve their {XORBERAX_SCHOLAR_LECTURE_SKILL_NAME} skill by attending the lecture for {XORBERAX_SCHOLAR_LECTURE_COST_PER_ATTENDEE}{GOLD_ICON} per attendee.",
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
            campaignGameStarter.AddWaitGameMenu(
                "scholar_lecture_attend_wait",
                "{XORBERAX_SCHOLAR_LECTURE_IN_PROGRESS_DESCRIPTION}",
                args =>
                {
                    args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(_lectureDurationInHours, 0.0f);
                    args.MenuContext.GameMenu.AllowWaitingAutomatically();
                },
                args =>
                {
                    args.MenuContext.GameMenu.AllowWaitingAutomatically();
                    args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    return true;
                },
                args => OnLectureEnd(),
                (args, dt) =>
                {
                    args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(_startTimeOfLecture.ElapsedHoursUntilNow / _lectureDurationInHours);
                },
                GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption
            );
            campaignGameStarter.AddGameMenuOption(
                "scholar_lecture_attend_wait",
                "scholar_lecture_attend_wait_leave",
                "{=scholar_lecture_attend_wait_leave}Leave lecture (no refund)",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args =>
                {
                    InformationManager.DisplayMessage(new InformationMessage("You left the lecture."));
                    GameMenu.SwitchToMenu("town");
                }
            );
        }

        private void UpdateMenuTextVariables()
        {
            var currentSettlement = Settlement.CurrentSettlement;
            var lectureInfo = GetCurrentLectureInfoAtSettlement(currentSettlement);

            MBTextManager.SetTextVariable("XORBERAX_SCHOLAR_SETTLEMENT_NAME", currentSettlement);
            MBTextManager.SetTextVariable("XORBERAX_SCHOLAR_LECTURE_SKILL_NAME", lectureInfo?.Skill?.Name);
            MBTextManager.SetTextVariable("XORBERAX_SCHOLAR_LEVEL", lectureInfo?.ScholarLevelInfo?.ScholarLevelName.ToLowerInvariant());
            MBTextManager.SetTextVariable("XORBERAX_SCHOLAR_LECTURE_COST_PER_ATTENDEE", lectureInfo?.ScholarLevelInfo?.CostPerAttendee);
            MBTextManager.SetTextVariable("XORBERAX_SCHOLAR_LECTURE_IN_PROGRESS_DESCRIPTION", $"{_heroIdsOfLectureAttendees.Count} {(_heroIdsOfLectureAttendees.Count == 1 ? "person" : "people")} in your party {(_heroIdsOfLectureAttendees.Count == 1 ? "is" : "are")} attending a lecture about {lectureInfo?.Skill}.");
        }

        private void ShowAttendeeSelectionList()
        {
            InformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    "Lecture Attendees",
                    "Select people to attend lecture:",
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
                            var totalLectureCost = selectedHeroElements.Count * lectureInfo.ScholarLevelInfo.CostPerAttendee;
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
            var scholarSeed = settlement.Id.GetHashCode() + _dailyRandomNumber;
            if (scholarSeed % _dailyRandomNumber > MaxDailyRandomNumber * SubModule.Config.ScholarAppearanceProbability || HasAttendedLectureAtSettlementToday(settlement))
            {
                return null;
            }
            var scholarLevelInfo = ScholarLevelUtilities.GetScholarLevelInfo(scholarSeed);
            return new LectureInfo
            {
                ScholarLevelInfo = scholarLevelInfo,
                Skill = _skills[scholarSeed % _skills.Length]
            };
        }

        private void AttendLecture(LectureInfo lectureInfo, IEnumerable<Hero> attendees)
        {
            var totalLectureCost = lectureInfo.ScholarLevelInfo.CostPerAttendee * attendees.Count();

            if (totalLectureCost > Hero.MainHero.Gold)
            {
                InformationManager.DisplayMessage(new InformationMessage("You cannot afford to attend this lecture."));
                return;
            }
            if (HasAttendedLectureAtSettlementToday(Settlement.CurrentSettlement))
            {
                InformationManager.DisplayMessage(new InformationMessage("You've already attended this scholar's lecture today."));
                return;
            }

            _lectureSkillId = lectureInfo.Skill.StringId;
            _heroIdsOfLectureAttendees = attendees.Select(attendee => attendee.StringId).ToList();
            _lectureExperienceGainPerAttendee = lectureInfo.ScholarLevelInfo.ExperienceGainPerAttendee;
            _startTimeOfLecture = CampaignTime.Now;
            _lectureDurationInHours = lectureInfo.ScholarLevelInfo.LectureDurationInHours;

            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, totalLectureCost);
            UpdateMenuTextVariables();
            GameMenu.SwitchToMenu("scholar_lecture_attend_wait");
        }

        private void OnLectureEnd()
        {
            _settlementIdsOfSettlementsLecturedAtToday.Add(Settlement.CurrentSettlement.StringId);
            var attendees = Hero.FindAll(hero => _heroIdsOfLectureAttendees.Contains(hero.StringId));
            var lectureSkill = SkillObject.FindFirst(skill => skill.StringId == _lectureSkillId);
            foreach (var attendee in attendees)
            {
                attendee.AddSkillXp(lectureSkill, _lectureExperienceGainPerAttendee);
            }
            UpdateMenuTextVariables();
            GameMenu.SwitchToMenu("town");
        }

        private bool HasAttendedLectureAtSettlementToday(Settlement settlement)
        {
            return _settlementIdsOfSettlementsLecturedAtToday.Contains(settlement.StringId);
        }
    }
}
