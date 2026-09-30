using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class ResultNextGoalResolver : MonoBehaviour
    {
        private FirstSessionOnboardingSystem onboarding;
        private StoryMissionSystem story;
        private DailyAdventureSystem daily;
        private SeasonSystem season;

        public void Initialize(
            FirstSessionOnboardingSystem onboardingSystem,
            StoryMissionSystem storySystem,
            DailyAdventureSystem dailySystem,
            SeasonSystem seasonSystem)
        {
            onboarding =
                onboardingSystem;

            story =
                storySystem;

            daily =
                dailySystem;

            season =
                seasonSystem;
        }

        public string Resolve()
        {
            if (onboarding != null &&
                !onboarding.IsComplete)
            {
                return
                    MotorCityLocalization.Format(
                        "hud.result_next_rookie",
                        onboarding.ObjectiveLine);
            }

            if (story != null &&
                !story.IsComplete)
            {
                string goal =
                    !string.IsNullOrWhiteSpace(
                        story.CurrentMissionTitle)
                        ? story.CurrentMissionTitle
                        : story.ObjectiveLine;

                if (!string.IsNullOrWhiteSpace(
                        goal))
                {
                    return
                        MotorCityLocalization.Format(
                            "hud.result_next_story",
                            goal);
                }
            }

            if (daily != null &&
                !daily.DayCompleted)
            {
                int remaining =
                    Mathf.Max(
                        0,
                        3 -
                        daily.CompletedTasks);

                if (remaining > 0)
                {
                    return
                        MotorCityLocalization.Format(
                            "hud.result_next_daily",
                            remaining);
                }
            }

            if (season != null &&
                !season.IsComplete &&
                season.IsSeasonOneActive)
            {
                int remaining =
                    Mathf.Max(
                        0,
                        season.CurrentMissionTarget -
                        season.CurrentMissionProgress);

                string activity =
                    ActivityName(
                        season.RequiredActivityId);

                if (remaining > 0 &&
                    !string.IsNullOrWhiteSpace(
                        activity))
                {
                    return
                        MotorCityLocalization.Format(
                            "hud.result_next_season",
                            remaining,
                            activity);
                }
            }

            return
                string.Empty;
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
                _ =>
                    string.Empty
            };
        }
    }
}
