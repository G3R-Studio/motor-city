using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class AchievementSystem : MonoBehaviour
    {
        private const float CheckInterval = 0.75f;
        private const float MessageSeconds = 5f;

        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private ActivityManager activities;
        private DiscoverySystem discoveries;
        private PhotoHuntSystem photoHunt;
        private VehicleRosterSystem roster;
        private StoryMissionSystem story;
        private SeasonSystem season;
        private CityProfessionSystem professions;

        private int activityWins;
        private int raceWins;
        private int driftWins;
        private int professionWins;
        private float checkTimer;
        private float messageTimer;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public int UnlockedCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < 9; i++)
                {
                    if (IsUnlocked(i))
                        count++;
                }

                return count;
            }
        }

        public void Initialize(
            PlayerWallet targetWallet,
            PlayerReputation targetReputation,
            ActivityManager activityManager,
            DiscoverySystem discoverySystem,
            PhotoHuntSystem photoHuntSystem,
            VehicleRosterSystem vehicleRoster,
            StoryMissionSystem storySystem,
            SeasonSystem seasonSystem,
            CityProfessionSystem professionSystem)
        {
            wallet = targetWallet;
            reputation = targetReputation;
            activities = activityManager;
            discoveries = discoverySystem;
            photoHunt = photoHuntSystem;
            roster = vehicleRoster;
            story = storySystem;
            season = seasonSystem;
            professions = professionSystem;

            activityWins = LoadCounter("Activities");
            raceWins = LoadCounter("Races");
            driftWins = LoadCounter("Drifts");
            professionWins = LoadCounter("Professions");

            if (activities != null)
                activities.ActivityResultShown += OnActivityResult;

            EvaluateAll();
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.unscaledDeltaTime);
            }

            checkTimer -=
                Time.unscaledDeltaTime;

            if (checkTimer > 0f)
                return;

            checkTimer =
                CheckInterval;

            EvaluateAll();
        }

        private void OnDestroy()
        {
            if (activities != null)
                activities.ActivityResultShown -= OnActivityResult;
        }

        private void OnActivityResult(
            string activityId,
            bool success)
        {
            if (!success)
                return;

            activityWins++;
            SaveCounter("Activities", activityWins);

            if (activityId == "sprint" ||
                activityId == "circuit")
            {
                raceWins++;
                SaveCounter("Races", raceWins);
            }

            if (activityId == "drift")
            {
                driftWins++;
                SaveCounter("Drifts", driftWins);
            }

            if (activityId != null &&
                activityId.StartsWith(
                    "profession_",
                    System.StringComparison.Ordinal))
            {
                professionWins++;
                SaveCounter("Professions", professionWins);
            }

            MotorCity.Persistence.MotorCitySaveService.Save();
            EvaluateAll();
        }

        private void EvaluateAll()
        {
            TryUnlock(
                0,
                activityWins >= 1,
                "achievement.first_drive",
                250,
                25);

            TryUnlock(
                1,
                raceWins >= 5,
                "achievement.racer",
                500,
                50);

            TryUnlock(
                2,
                driftWins >= 5,
                "achievement.drifter",
                500,
                50);

            TryUnlock(
                3,
                professionWins >= 5 ||
                (professions != null &&
                 professions.TotalCompleted >= 5),
                "achievement.worker",
                650,
                60);

            TryUnlock(
                4,
                discoveries != null &&
                discoveries.FoundCount >= 5,
                "achievement.explorer",
                700,
                70);

            TryUnlock(
                5,
                photoHunt != null &&
                photoHunt.TotalCaptured >= 5,
                "achievement.photographer",
                700,
                70);

            TryUnlock(
                6,
                roster != null &&
                roster.GetOwnedVehicleCount() >= 3,
                "achievement.collector",
                900,
                90);

            TryUnlock(
                7,
                story != null &&
                story.IsComplete,
                "achievement.story",
                1200,
                120);

            TryUnlock(
                8,
                season != null &&
                season.IsComplete,
                "achievement.season",
                1600,
                150);
        }

        private void TryUnlock(
            int index,
            bool condition,
            string nameKey,
            int credits,
            int rep)
        {
            if (!condition ||
                IsUnlocked(index))
            {
                return;
            }

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                UnlockKey(index),
                1);

            MotorCity.Persistence.MotorCitySaveService.Save();

            wallet?.AddCredits(credits);
            reputation?.AddReputation(rep);

            StatusText =
                MotorCityLocalization.Format(
                    "achievement.unlocked",
                    MotorCityLocalization.Text(nameKey),
                    credits,
                    rep,
                    UnlockedCount,
                    9);

            messageTimer =
                MessageSeconds;
        }

        private bool IsUnlocked(
            int index)
        {
            return
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    UnlockKey(index),
                    0) != 0;
        }

        private static int LoadCounter(
            string suffix)
        {
            return
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        "MotorCity.Achievement.Count." +
                        suffix,
                        0));
        }

        private static void SaveCounter(
            string suffix,
            int value)
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                "MotorCity.Achievement.Count." +
                suffix,
                Mathf.Max(
                    0,
                    value));
        }

        private static string UnlockKey(
            int index)
        {
            return
                "MotorCity.Achievement.Unlocked." +
                index;
        }
    }
}
