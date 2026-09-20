using System;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class VehicleMasterySystem : MonoBehaviour
    {
        private const int MaxLevel = 10;
        private const float MessageSeconds = 5f;

        private static readonly int[] LevelThresholds =
        {
            0,
            100,
            240,
            420,
            650,
            930,
            1260,
            1640,
            2070,
            2550
        };

        private ActivityManager activityManager;
        private VehicleRosterSystem roster;
        private ArcadeCarController car;

        private int currentXp;
        private float messageTimer;

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentXp => currentXp;
        public int NextLevelXp =>
            CurrentLevel >= MaxLevel
                ? LevelThresholds[MaxLevel - 1]
                : LevelThresholds[CurrentLevel];

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; } =
            string.Empty;

        public void Initialize(
            ActivityManager manager,
            VehicleRosterSystem vehicleRoster,
            ArcadeCarController targetCar)
        {
            activityManager =
                manager;

            roster =
                vehicleRoster;

            car =
                targetCar;

            if (activityManager != null)
            {
                activityManager.ActivityResultShown +=
                    HandleActivityResult;
            }

            if (roster != null)
            {
                roster.VehicleChanged +=
                    HandleVehicleChanged;
            }

            LoadCurrentVehicle();
        }

        private void Update()
        {
            if (messageTimer <= 0f)
                return;

            messageTimer =
                Mathf.Max(
                    0f,
                    messageTimer -
                    Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityResultShown -=
                    HandleActivityResult;
            }

            if (roster != null)
            {
                roster.VehicleChanged -=
                    HandleVehicleChanged;
            }
        }

        public int GetLevelForVehicle(
            string vehicleId)
        {
            if (string.IsNullOrEmpty(
                    vehicleId))
            {
                return 1;
            }

            int xp =
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        XpKey(
                            vehicleId),
                        0));

            int level = 1;

            for (int i = 1;
                 i < LevelThresholds.Length;
                 i++)
            {
                if (xp <
                    LevelThresholds[i])
                {
                    break;
                }

                level =
                    i + 1;
            }

            return
                Mathf.Clamp(
                    level,
                    1,
                    MaxLevel);
        }

        public void AddBonusXp(
            int amount)
        {
            if (amount <= 0 ||
                roster == null)
            {
                return;
            }

            currentXp +=
                amount;

            SaveCurrentVehicle();
            ResolveLevel();
        }

        public void SetCurrentLevelForTesting(
            int level)
        {
            int safeLevel =
                Mathf.Clamp(
                    level,
                    1,
                    MaxLevel);

            currentXp =
                LevelThresholds[safeLevel - 1];

            SaveCurrentVehicle();
            ResolveLevel();
        }

        public void SetAllVehicleLevelsForTesting(
            int level)
        {
            int safeLevel =
                Mathf.Clamp(
                    level,
                    1,
                    MaxLevel);

            int xp =
                LevelThresholds[safeLevel - 1];

            string[] vehicleIds =
            {
                "street",
                "club",
                "muscle",
                "gt",
                "apex"
            };

            foreach (string vehicleId in vehicleIds)
            {
                PlayerPrefs.SetInt(
                    XpKey(
                        vehicleId),
                    xp);
            }

            PlayerPrefs.Save();
            LoadCurrentVehicle();
        }

        private void HandleVehicleChanged()
        {
            LoadCurrentVehicle();
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success ||
                roster == null)
            {
                return;
            }

            int earnedXp =
                activityId switch
                {
                    "delivery" => 65,
                    "drift" => 80,
                    "sprint" => 90,
                    "circuit" => 110,
                    _ => 0
                };

            if (earnedXp <= 0)
                return;

            int previousLevel =
                CurrentLevel;

            currentXp +=
                earnedXp;

            SaveCurrentVehicle();
            ResolveLevel();

            if (CurrentLevel >
                previousLevel)
            {
                StatusText =
                    $"{roster.SelectedName}: МАСТЕРСТВО УР. {CurrentLevel}/10   " +
                    $"+{earnedXp} XP";

                messageTimer =
                    MessageSeconds;
            }
        }

        private void LoadCurrentVehicle()
        {
            if (roster == null)
                return;

            currentXp =
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        XpKey(
                            roster.SelectedId),
                        0));

            ResolveLevel();
        }

        private void ResolveLevel()
        {
            int level =
                1;

            for (int i = 1;
                 i < LevelThresholds.Length;
                 i++)
            {
                if (currentXp <
                    LevelThresholds[i])
                {
                    break;
                }

                level =
                    i + 1;
            }

            CurrentLevel =
                Mathf.Clamp(
                    level,
                    1,
                    MaxLevel);

            car?.ApplyMasteryLevel(
                CurrentLevel);

            roster?.SetMasteryDisplay(
                CurrentLevel,
                currentXp,
                NextLevelXp);
        }

        private void SaveCurrentVehicle()
        {
            if (roster == null)
                return;

            PlayerPrefs.SetInt(
                XpKey(
                    roster.SelectedId),
                currentXp);

            PlayerPrefs.Save();
        }

        private static string XpKey(
            string vehicleId)
        {
            return
                $"MotorCity.VehicleMastery.{vehicleId}.Xp";
        }
    }
}
