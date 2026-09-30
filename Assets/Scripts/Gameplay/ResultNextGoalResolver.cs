using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class ResultNextGoalResolver :
        MonoBehaviour
    {
        public enum GoalType
        {
            None,
            RookiePath,
            Story,
            VehicleUnlock,
            Daily,
            Season,
            WeekendEvent,
            Navigator
        }

        public readonly struct Goal
        {
            public Goal(
                GoalType type,
                int priority,
                string text)
            {
                Type =
                    type;

                Priority =
                    priority;

                Text =
                    text ?? string.Empty;
            }

            public GoalType Type { get; }
            public int Priority { get; }
            public string Text { get; }
            public bool IsValid =>
                Type != GoalType.None &&
                !string.IsNullOrWhiteSpace(
                    Text);
        }

        private FirstSessionOnboardingSystem onboarding;
        private StoryMissionSystem story;
        private VehicleRosterSystem vehicles;
        private PlayerReputation reputation;
        private DailyAdventureSystem daily;
        private SeasonSystem season;
        private CityLiveEventSystem liveEvents;
        private AdventureDirector adventureDirector;

        public Goal CurrentGoal =>
            ResolveGoal();

        public string AdminLine
        {
            get
            {
                Goal goal =
                    ResolveGoal();

                return goal.IsValid
                    ? goal.Priority +
                      " | " +
                      goal.Type +
                      " | " +
                      goal.Text
                    : "нет цели";
            }
        }

        public void Initialize(
            FirstSessionOnboardingSystem onboardingSystem,
            StoryMissionSystem storySystem,
            VehicleRosterSystem vehicleSystem,
            PlayerReputation reputationSystem,
            DailyAdventureSystem dailySystem,
            SeasonSystem seasonSystem,
            CityLiveEventSystem liveEventSystem,
            AdventureDirector director)
        {
            onboarding =
                onboardingSystem;

            story =
                storySystem;

            vehicles =
                vehicleSystem;

            reputation =
                reputationSystem;

            daily =
                dailySystem;

            season =
                seasonSystem;

            liveEvents =
                liveEventSystem;

            adventureDirector =
                director;
        }

        public string Resolve()
        {
            return
                ResolveGoal().Text;
        }

        public Goal ResolveGoal()
        {
            Goal rookie =
                ResolveRookie();

            if (rookie.IsValid)
                return rookie;

            Goal storyGoal =
                ResolveStory();

            if (storyGoal.IsValid)
                return storyGoal;

            Goal vehicle =
                ResolveVehicleUnlock();

            if (vehicle.IsValid)
                return vehicle;

            Goal dailyGoal =
                ResolveDaily();

            if (dailyGoal.IsValid)
                return dailyGoal;

            Goal seasonGoal =
                ResolveSeason();

            if (seasonGoal.IsValid)
                return seasonGoal;

            Goal weekend =
                ResolveWeekendEvent();

            if (weekend.IsValid)
                return weekend;

            return
                ResolveNavigator();
        }

        private Goal ResolveRookie()
        {
            if (onboarding == null ||
                onboarding.IsComplete ||
                string.IsNullOrWhiteSpace(
                    onboarding.ObjectiveLine))
            {
                return
                    default;
            }

            return
                new Goal(
                    GoalType.RookiePath,
                    1,
                    MotorCityLocalization.Format(
                        "hud.result_next_rookie",
                        onboarding.ObjectiveLine));
        }

        private Goal ResolveStory()
        {
            if (story == null ||
                story.IsComplete)
            {
                return
                    default;
            }

            string goal =
                !string.IsNullOrWhiteSpace(
                    story.CurrentMissionTitle)
                    ? story.CurrentMissionTitle
                    : story.ObjectiveLine;

            if (string.IsNullOrWhiteSpace(
                    goal))
            {
                return
                    default;
            }

            return
                new Goal(
                    GoalType.Story,
                    2,
                    MotorCityLocalization.Format(
                        "hud.result_next_story",
                        goal));
        }

        private Goal ResolveVehicleUnlock()
        {
            // Stage 42 owns the exact "close to unlock" threshold.
            // Keeping the priority slot here prevents other UI from
            // implementing a second, conflicting vehicle-goal policy.
            if (vehicles == null ||
                reputation == null ||
                !vehicles.HasNextVehicle ||
                vehicles.NextVehicleUnlocked)
            {
                return
                    default;
            }

            return
                default;
        }

        private Goal ResolveDaily()
        {
            if (daily == null ||
                daily.DayCompleted)
            {
                return
                    default;
            }

            int remaining =
                Mathf.Max(
                    0,
                    3 -
                    daily.CompletedTasks);

            if (remaining <= 0)
                return default;

            return
                new Goal(
                    GoalType.Daily,
                    4,
                    MotorCityLocalization.Format(
                        "hud.result_next_daily",
                        remaining));
        }

        private Goal ResolveSeason()
        {
            if (season == null ||
                season.IsComplete ||
                !season.IsSeasonOneActive)
            {
                return
                    default;
            }

            int remaining =
                Mathf.Max(
                    0,
                    season.CurrentMissionTarget -
                    season.CurrentMissionProgress);

            string activity =
                ActivityName(
                    season.RequiredActivityId);

            if (remaining <= 0 ||
                string.IsNullOrWhiteSpace(
                    activity))
            {
                return
                    default;
            }

            return
                new Goal(
                    GoalType.Season,
                    5,
                    MotorCityLocalization.Format(
                        "hud.result_next_season",
                        remaining,
                        activity));
        }

        private Goal ResolveWeekendEvent()
        {
            if (liveEvents == null ||
                string.IsNullOrWhiteSpace(
                    liveEvents.HudLine))
            {
                return
                    default;
            }

            return
                new Goal(
                    GoalType.WeekendEvent,
                    6,
                    MotorCityLocalization.Format(
                        "hud.result_next_event",
                        liveEvents.HudLine));
        }

        private Goal ResolveNavigator()
        {
            if (adventureDirector == null ||
                string.IsNullOrWhiteSpace(
                    adventureDirector.ObjectiveLine))
            {
                return
                    default;
            }

            return
                new Goal(
                    GoalType.Navigator,
                    7,
                    MotorCityLocalization.Format(
                        "hud.result_next_navigator",
                        adventureDirector.ObjectiveLine));
        }

        private static string ActivityName(
            string activityId)
        {
            return activityId switch
            {
                "delivery" =>
                    MotorCityLocalization.Text(
                        "activity.delivery"),
                "sprint" =>
                    MotorCityLocalization.Text(
                        "activity.sprint"),
                "circuit" =>
                    MotorCityLocalization.Text(
                        "activity.circuit"),
                "drift" =>
                    MotorCityLocalization.Text(
                        "activity.drift_challenge"),
                "profession_pizza" =>
                    MotorCityLocalization.Text(
                        "profession.pizza"),
                "profession_taxi" =>
                    MotorCityLocalization.Text(
                        "profession.taxi"),
                "profession_mail" =>
                    MotorCityLocalization.Text(
                        "profession.mail"),
                "profession_icecream" =>
                    MotorCityLocalization.Text(
                        "profession.icecream"),
                "profession_carwash" =>
                    MotorCityLocalization.Text(
                        "carwash.title"),
                "profession_tow" =>
                    MotorCityLocalization.Text(
                        "tow.title"),
                "underground" =>
                    MotorCityLocalization.Text(
                        "nightclub.title"),
                _ =>
                    string.Empty
            };
        }
    }
}
