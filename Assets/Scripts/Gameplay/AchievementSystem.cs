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
        private readonly bool[] unlocked =
            new bool[9];
        private int unlockedCount;
        private float checkTimer;
        private float messageTimer;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public int UnlockedCount =>
            unlockedCount;

        // Read-only UX inspection. Never call TryUnlock or re-evaluate
        // achievements when opening a progress panel.
        private static readonly string[] AchievementNameKeys =
        {
            "achievement.first_drive",
            "achievement.racer",
            "achievement.drifter",
            "achievement.worker",
            "achievement.explorer",
            "achievement.photographer",
            "achievement.collector",
            "achievement.story",
            "achievement.season"
        };

        // Single source of truth for the one-time rewards displayed in
        // Player Experience and granted by EvaluateAll/TryUnlock.
        private static readonly int[] AchievementCreditRewards =
        {
            250, 500, 500, 650, 700, 700, 900, 1200, 1600
        };

        private static readonly int[] AchievementRepRewards =
        {
            25, 50, 50, 60, 70, 70, 90, 120, 150
        };

        public int TotalCount => unlocked.Length;

        public void GetAchievementReward(
            int index, out int credits, out int rep)
        {
            credits = index >= 0 && index < AchievementCreditRewards.Length
                ? AchievementCreditRewards[index] : 0;
            rep = index >= 0 && index < AchievementRepRewards.Length
                ? AchievementRepRewards[index] : 0;
        }

        public void GetAchievementProgress(
            int index, out int current, out int target)
        {
            target = index == 0 || index >= 7 ? 1 :
                index == 6 ? 3 : 5;
            current = index switch
            {
                0 => activityWins,
                1 => raceWins,
                2 => driftWins,
                3 => Mathf.Max(professionWins,
                    professions == null ? 0 : professions.TotalCompleted),
                4 => discoveries == null ? 0 : discoveries.FoundCount,
                5 => photoHunt == null ? 0 : photoHunt.TotalCaptured,
                6 => roster == null ? 0 : roster.GetOwnedVehicleCount(),
                7 => story != null && story.IsComplete ? 1 : 0,
                8 => season != null && season.IsComplete ? 1 : 0,
                _ => 0
            };

            // A completed achievement remains complete even when its
            // underlying daily/seasonal counter changes in the future.
            if (IsAchievementUnlocked(index))
                current = Mathf.Max(current, target);
            current = Mathf.Clamp(current, 0, target);
        }

        public bool IsAchievementUnlocked(int index)
        {
            return index >= 0 &&
                   index < unlocked.Length &&
                   unlocked[index];
        }

        public string GetAchievementCondition(int index)
        {
            string key = index switch
            {
                0 => "ux4.achievement.condition.first",
                1 => "ux4.achievement.condition.racer",
                2 => "ux4.achievement.condition.drifter",
                3 => "ux4.achievement.condition.worker",
                4 => "ux4.achievement.condition.explorer",
                5 => "ux4.achievement.condition.photo",
                6 => "ux4.achievement.condition.collector",
                7 => "ux4.achievement.condition.story",
                8 => "ux4.achievement.condition.season",
                _ => null
            };
            return key == null ? string.Empty : MotorCityLocalization.Text(key);
        }

        public string GetAchievementName(int index)
        {
            return index >= 0 && index < AchievementNameKeys.Length
                ? MotorCityLocalization.Text(AchievementNameKeys[index])
                : string.Empty;
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

            unlockedCount = 0;

            for (int i = 0;
                 i < unlocked.Length;
                 i++)
            {
                unlocked[i] =
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        UnlockKey(i),
                        0) != 0;

                if (unlocked[i])
                {
                    unlockedCount++;
                }
            }

            if (activities != null)
                activities.ActivityCompleted += OnActivityCompleted;

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

            if (unlockedCount >=
                unlocked.Length)
            {
                return;
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
                activities.ActivityCompleted -= OnActivityCompleted;
        }

        private void OnActivityCompleted(
            string activityId)
        {
            if (activities != null &&
                !activities.SecondaryProgressionAllowed)
            {
                return;
            }

            if (unlockedCount >=
                    unlocked.Length)
            {
                return;
            }

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

#if UNITY_EDITOR || DEBUG
        public void UnlockNextForTesting()
        {
            string[] keys =
            {
                "achievement.first_drive",
                "achievement.racer",
                "achievement.drifter",
                "achievement.worker",
                "achievement.explorer",
                "achievement.photographer",
                "achievement.collector",
                "achievement.story",
                "achievement.season"
            };

            for (int i = 0;
                 i < unlocked.Length;
                 i++)
            {
                if (unlocked[i])
                    continue;

                TryUnlock(
                    i,
                    true,
                    keys[i],
                    0,
                    0);

                return;
            }
        }

        public void UnlockAllForTesting()
        {
            int guard =
                0;

            while (unlockedCount <
                       unlocked.Length &&
                   guard <
                       unlocked.Length +
                       2)
            {
                UnlockNextForTesting();
                guard++;
            }
        }

        public void ResetForTesting()
        {
            activityWins = 0;
            raceWins = 0;
            driftWins = 0;
            professionWins = 0;
            unlockedCount = 0;

            SaveCounter(
                "Activities",
                0);

            SaveCounter(
                "Races",
                0);

            SaveCounter(
                "Drifts",
                0);

            SaveCounter(
                "Professions",
                0);

            for (int i = 0;
                 i < unlocked.Length;
                 i++)
            {
                unlocked[i] =
                    false;

                MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                    UnlockKey(
                        i));
            }

            MotorCity.Persistence.MotorCitySaveService.Save();

            StatusText =
                string.Empty;

            messageTimer =
                0f;
        }

#endif

        private void EvaluateAll()
        {
            if (activities != null &&
                !activities.SecondaryProgressionAllowed)
            {
                return;
            }

            if (unlockedCount >=
                unlocked.Length)
            {
                return;
            }

            TryUnlock(
                0,
                activityWins >= 1,
                "achievement.first_drive",
                AchievementCreditRewards[0],
                AchievementRepRewards[0]);

            TryUnlock(
                1,
                raceWins >= 5,
                "achievement.racer",
                AchievementCreditRewards[1],
                AchievementRepRewards[1]);

            TryUnlock(
                2,
                driftWins >= 5,
                "achievement.drifter",
                AchievementCreditRewards[2],
                AchievementRepRewards[2]);

            TryUnlock(
                3,
                professionWins >= 5 ||
                (professions != null &&
                 professions.TotalCompleted >= 5),
                "achievement.worker",
                AchievementCreditRewards[3],
                AchievementRepRewards[3]);

            TryUnlock(
                4,
                discoveries != null &&
                discoveries.FoundCount >= 5,
                "achievement.explorer",
                AchievementCreditRewards[4],
                AchievementRepRewards[4]);

            TryUnlock(
                5,
                photoHunt != null &&
                photoHunt.TotalCaptured >= 5,
                "achievement.photographer",
                AchievementCreditRewards[5],
                AchievementRepRewards[5]);

            TryUnlock(
                6,
                roster != null &&
                roster.GetOwnedVehicleCount() >= 3,
                "achievement.collector",
                AchievementCreditRewards[6],
                AchievementRepRewards[6]);

            TryUnlock(
                7,
                story != null &&
                story.IsComplete,
                "achievement.story",
                AchievementCreditRewards[7],
                AchievementRepRewards[7]);

            TryUnlock(
                8,
                season != null &&
                season.IsComplete,
                "achievement.season",
                AchievementCreditRewards[8],
                AchievementRepRewards[8]);
        }

        private void TryUnlock(
            int index,
            bool condition,
            string nameKey,
            int credits,
            int rep)
        {
            if (!condition ||
                index < 0 ||
                index >= unlocked.Length ||
                unlocked[index])
            {
                return;
            }

            unlocked[index] =
                true;

            unlockedCount++;

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
