using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class LeaderboardSyncSystem : MonoBehaviour
    {
        private const string WinsKey = "MotorCity.Leaderboard.ActivityWins";

        private PlayerReputation reputation;
        private CollectionProgressionSystem collection;
        private ActivityManager activities;
        private int activityWins;
        private int lastRep = -1;
        private int lastCollection = -1;
        private int lastWins = -1;
        private float submitTimer;
        private bool submissionPending;

        public void Initialize(
            PlayerReputation playerReputation,
            CollectionProgressionSystem collectionSystem,
            ActivityManager activityManager)
        {
            reputation = playerReputation;
            collection = collectionSystem;
            activities = activityManager;

            activityWins = Mathf.Max(
                0,
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    WinsKey,
                    0));

            if (activities != null)
                activities.ActivityResultShown += OnActivityResult;

            submitTimer = 2f;
        }

        private void Update()
        {
            submitTimer -= Time.unscaledDeltaTime;

            if (submitTimer > 0f)
                return;

            submitTimer = 20f;
            SubmitChanged();
        }

        private void OnDestroy()
        {
            if (activities != null)
                activities.ActivityResultShown -= OnActivityResult;
        }

        private void OnActivityResult(string activityId, bool success)
        {
            if (!success)
                return;

            activityWins++;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                WinsKey,
                activityWins);

            MotorCity.Persistence.MotorCitySaveService.Save();

            submitTimer = Mathf.Min(submitTimer, 1.5f);
        }

        private void SubmitChanged()
        {
            if (submissionPending ||
                !MotorCityPlatform.SupportsLeaderboards)
            {
                return;
            }

            int rep =
                reputation != null
                    ? reputation.Reputation
                    : 0;

            int collectionRating =
                collection != null
                    ? collection.Rating
                    : 0;

            if (rep != lastRep)
            {
                int submittedRep =
                    rep;

                submissionPending = true;

                MotorCityPlatform.SubmitLeaderboard(
                    MotorCityRemoteConfigRuntime.GetString(
                        "leaderboard_rep_id",
                        "motor_city_rep"),
                    submittedRep,
                    success =>
                    {
                        submissionPending = false;

                        if (success)
                        {
                            lastRep =
                                submittedRep;
                        }

                        submitTimer =
                            Mathf.Min(
                                submitTimer,
                                0.25f);
                    });

                return;
            }

            if (collectionRating != lastCollection)
            {
                int submittedCollection =
                    collectionRating;

                submissionPending = true;

                MotorCityPlatform.SubmitLeaderboard(
                    MotorCityRemoteConfigRuntime.GetString(
                        "leaderboard_collection_id",
                        "motor_city_collection"),
                    submittedCollection,
                    success =>
                    {
                        submissionPending = false;

                        if (success)
                        {
                            lastCollection =
                                submittedCollection;
                        }

                        submitTimer =
                            Mathf.Min(
                                submitTimer,
                                0.25f);
                    });

                return;
            }

            if (activityWins != lastWins)
            {
                int submittedWins =
                    activityWins;

                submissionPending = true;

                MotorCityPlatform.SubmitLeaderboard(
                    MotorCityRemoteConfigRuntime.GetString(
                        "leaderboard_activities_id",
                        "motor_city_activities"),
                    submittedWins,
                    success =>
                    {
                        submissionPending = false;

                        if (success)
                        {
                            lastWins =
                                submittedWins;
                        }

                        submitTimer =
                            Mathf.Min(
                                submitTimer,
                                0.25f);
                    });
            }
        }
    }
}
