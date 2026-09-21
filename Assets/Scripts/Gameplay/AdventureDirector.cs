using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class AdventureDirector : MonoBehaviour
    {
        private ActivityManager activityManager;
        private CareerProgressionSystem career;
        private CityContractSystem contracts;
        private CityLiveEventSystem liveEvents;
        private CityLegendSystem legends;
        private UndergroundSceneSystem nightClub;
        private CityRiskSystem cityRisk;
        private TurboPetSystem turbo;
        private FirstSessionOnboardingSystem onboarding;
        private DailyAdventureSystem dailyAdventures;
        private StoryMissionSystem story;
        private SeasonSystem season;

        private float refreshTimer;

        public MissionDefinition CurrentMission { get; private set; }

        public string ObjectiveLine =>
            CurrentMission == null
                ? string.Empty
                : CurrentMission.ObjectiveText;

        public bool HasMission =>
            CurrentMission != null;

        public void Initialize(
            ActivityManager manager,
            CareerProgressionSystem careerSystem,
            CityContractSystem contractSystem,
            CityLiveEventSystem liveEventSystem,
            CityLegendSystem legendSystem,
            UndergroundSceneSystem nightClubSystem,
            CityRiskSystem riskSystem,
            TurboPetSystem turboSystem,
            FirstSessionOnboardingSystem onboardingSystem,
            DailyAdventureSystem dailyAdventureSystem,
            StoryMissionSystem storySystem,
            SeasonSystem seasonSystem)
        {
            activityManager =
                manager;
            career =
                careerSystem;
            contracts =
                contractSystem;
            liveEvents =
                liveEventSystem;
            legends =
                legendSystem;
            nightClub =
                nightClubSystem;
            cityRisk =
                riskSystem;
            turbo =
                turboSystem;
            onboarding =
                onboardingSystem;
            dailyAdventures =
                dailyAdventureSystem;
            story =
                storySystem;
            season =
                seasonSystem;

            RefreshMission();
        }

        private void Update()
        {
            refreshTimer -=
                Time.unscaledDeltaTime;

            if (refreshTimer > 0f)
                return;

            refreshTimer = 0.2f;
            RefreshMission();
        }

        public void RefreshMission()
        {
            CurrentMission =
                ResolveMission();
        }

        private MissionDefinition ResolveMission()
        {
            MissionDefinition activity =
                BuildActiveActivityMission();

            if (activity != null)
                return activity;

            MissionDefinition onboardingMission =
                BuildOnboardingMission();

            if (onboardingMission != null)
                return onboardingMission;

            MissionDefinition storyMission =
                BuildStoryMission();

            if (storyMission != null)
                return storyMission;

            MissionDefinition seasonMission =
                BuildSeasonMission();

            if (seasonMission != null)
                return seasonMission;

            MissionDefinition inspector =
                BuildInspectorMission();

            if (inspector != null)
                return inspector;

            MissionDefinition night =
                BuildNightClubMission();

            if (night != null)
                return night;

            MissionDefinition legend =
                BuildLegendMission();

            if (legend != null)
                return legend;

            MissionDefinition live =
                BuildLiveEventMission();

            if (live != null)
                return live;

            MissionDefinition daily =
                BuildDailyAdventureMission();

            if (daily != null)
                return daily;

            MissionDefinition turboDaily =
                BuildTurboDailyMission();

            if (turboDaily != null)
                return turboDaily;

            MissionDefinition contract =
                BuildContractMission();

            if (contract != null)
                return contract;

            return
                BuildCareerMission();
        }

        private MissionDefinition BuildActiveActivityMission()
        {
            if (activityManager == null ||
                !activityManager.IsBusy)
            {
                return null;
            }

            string id =
                activityManager.ActiveId;

            MissionStepType stepType =
                id switch
                {
                    "delivery" =>
                        MissionStepType.Delivery,
                    "drift" =>
                        MissionStepType.DriftScore,
                    "sprint" =>
                        MissionStepType.RaceResult,
                    "circuit" =>
                        MissionStepType.RaceResult,
                    "underground" =>
                        MissionStepType.Checkpoints,
                    _ =>
                        MissionStepType.GoToPoint
                };

            string title =
                string.IsNullOrWhiteSpace(
                    activityManager.ActiveName)
                    ? id
                    : activityManager.ActiveName;

            string objective =
                MotorCityLocalization.Format(
                    "hud.target",
                    title);

            return
                new MissionDefinition(
                    "activity." + id,
                    title,
                    objective,
                    AdventureMissionSource.Activity,
                    1000)
                .AddStep(
                    new MissionStepDefinition(
                        stepType,
                        id,
                        objective));
        }

        private MissionDefinition BuildOnboardingMission()
        {
            if (onboarding == null ||
                onboarding.IsComplete)
            {
                return null;
            }

            string objective =
                onboarding.ObjectiveLine;

            return
                new MissionDefinition(
                    "source.onboarding",
                    MotorCityLocalization.Text(
                        "onboarding.title"),
                    objective,
                    AdventureMissionSource.Story,
                    990)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.GoToPoint,
                        string.Empty,
                        objective));
        }

        private MissionDefinition BuildStoryMission()
        {
            if (story == null ||
                story.IsComplete)
            {
                return null;
            }

            string objective =
                story.ObjectiveLine;

            if (string.IsNullOrWhiteSpace(
                    objective))
            {
                return null;
            }

            return
                new MissionDefinition(
                    "source.story",
                    MotorCityLocalization.Text(
                        "story.title"),
                    objective,
                    AdventureMissionSource.Story,
                    965)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.RaceResult,
                        string.Empty,
                        objective));
        }

        private MissionDefinition BuildSeasonMission()
        {
            if (season == null ||
                season.IsComplete ||
                !season.IsSeasonOneActive)
            {
                return null;
            }

            string objective =
                season.ObjectiveLine;

            if (string.IsNullOrWhiteSpace(
                    objective))
            {
                return null;
            }

            return
                new MissionDefinition(
                    "source.season1",
                    MotorCityLocalization.Text(
                        "season1.title"),
                    objective,
                    AdventureMissionSource.Story,
                    940)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.RaceResult,
                        string.Empty,
                        objective));
        }

        private MissionDefinition BuildInspectorMission()
        {
            if (cityRisk == null ||
                !cityRisk.PursuitActive)
            {
                return null;
            }

            string objective =
                cityRisk.HudLine;

            return
                new MissionDefinition(
                    "source.inspector",
                    MotorCityLocalization.Text(
                        "adventure.inspector"),
                    objective,
                    AdventureMissionSource.Inspector,
                    950)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.Parking,
                        string.Empty,
                        objective));
        }

        private MissionDefinition BuildNightClubMission()
        {
            if (nightClub == null ||
                !nightClub.HasActiveInvitation)
            {
                return null;
            }

            string objective =
                nightClub.HudLine;

            return
                new MissionDefinition(
                    "source.night_club",
                    MotorCityLocalization.Text(
                        "nightclub.title"),
                    objective,
                    AdventureMissionSource.NightClub,
                    900)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.GoToPoint,
                        "underground",
                        objective,
                        nightClub.CurrentTarget))
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.Checkpoints,
                        "underground",
                        objective));
        }

        private MissionDefinition BuildLegendMission()
        {
            if (legends == null ||
                legends.AllLegendsDefeated)
            {
                return null;
            }

            string objective =
                legends.HudLine;

            return
                new MissionDefinition(
                    "source.legend",
                    MotorCityLocalization.Text(
                        "activity.legend"),
                    objective,
                    AdventureMissionSource.Legend,
                    800)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.RaceResult,
                        string.Empty,
                        objective));
        }

        private MissionDefinition BuildLiveEventMission()
        {
            if (liveEvents == null)
                return null;

            string objective =
                liveEvents.HudLine;

            if (string.IsNullOrWhiteSpace(
                    objective))
            {
                return null;
            }

            return
                new MissionDefinition(
                    "source.live_event",
                    MotorCityLocalization.Text(
                        "adventure.city_event"),
                    objective,
                    AdventureMissionSource.LiveEvent,
                    700)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.RaceResult,
                        string.Empty,
                        objective));
        }

        private MissionDefinition BuildDailyAdventureMission()
        {
            if (dailyAdventures == null)
                return null;

            string objective =
                dailyAdventures.ObjectiveLine;

            if (string.IsNullOrWhiteSpace(
                    objective))
            {
                return null;
            }

            return
                new MissionDefinition(
                    "source.daily_adventure",
                    MotorCityLocalization.Text(
                        "daily.title"),
                    objective,
                    AdventureMissionSource.Daily,
                    675)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.RaceResult,
                        string.Empty,
                        objective));
        }

        private MissionDefinition BuildTurboDailyMission()
        {
            if (turbo == null)
                return null;

            string objective =
                turbo.DailyObjectiveLine;

            if (string.IsNullOrWhiteSpace(
                    objective))
            {
                return null;
            }

            return
                new MissionDefinition(
                    "source.turbo_daily",
                    MotorCityLocalization.Text(
                        "turbo.title"),
                    objective,
                    AdventureMissionSource.Daily,
                    650)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.RaceResult,
                        string.Empty,
                        objective));
        }

        private MissionDefinition BuildContractMission()
        {
            if (contracts == null)
                return null;

            string objective =
                contracts.HudLine;

            if (string.IsNullOrWhiteSpace(
                    objective))
            {
                return null;
            }

            return
                new MissionDefinition(
                    "source.contract",
                    MotorCityLocalization.Text(
                        "adventure.contract"),
                    objective,
                    AdventureMissionSource.Contract,
                    600)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.RaceResult,
                        string.Empty,
                        objective));
        }

        private MissionDefinition BuildCareerMission()
        {
            if (career == null)
                return null;

            string objective =
                career.HudLine;

            if (string.IsNullOrWhiteSpace(
                    objective))
            {
                return null;
            }

            return
                new MissionDefinition(
                    "source.career",
                    MotorCityLocalization.Text(
                        "adventure.career"),
                    objective,
                    AdventureMissionSource.Career,
                    500)
                .AddStep(
                    new MissionStepDefinition(
                        MissionStepType.RaceResult,
                        string.Empty,
                        objective));
        }
    }
}
