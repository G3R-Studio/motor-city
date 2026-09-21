using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CityLegendSystem : MonoBehaviour
    {
        private const string LegendIndexKey =
            "MotorCity.Legends.Index";

        private const string RacingProgressKey =
            "MotorCity.Legends.RacingProgress";

        private const string DriftProgressKey =
            "MotorCity.Legends.DriftProgress";

        private const string DeliveryProgressKey =
            "MotorCity.Legends.DeliveryProgress";

        private const string NightProgressKey =
            "MotorCity.Legends.NightProgress";

        private const int LegendCount = 4;
        private const float MessageSeconds = 6f;

        private ActivityManager activityManager;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private DisciplineReputationSystem disciplines;
        private VehicleMasterySystem mastery;
        private DayNightCycleController dayNight;

        private int legendIndex;
        private int racingProgress;
        private int driftProgress;
        private int deliveryProgress;
        private int nightProgress;
        private float messageTimer;
        private bool unlockAnnounced;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; } =
            string.Empty;

        public bool AllLegendsDefeated =>
            legendIndex >=
            LegendCount;

        public string HudLine
        {
            get
            {
                if (AllLegendsDefeated)
                    return "ЛЕГЕНДЫ ГОРОДА • ВСЕ ПОБЕЖДЕНЫ";

                LegendDefinition current =
                    CurrentDefinition();

                if (!IsUnlocked(
                        current))
                {
                    return
                        $"ЛЕГЕНДА — {current.Name} • " +
                        UnlockRequirements(
                            current);
                }

                return
                    $"ЛЕГЕНДА — {current.Name} • " +
                    ProgressText(
                        current);
            }
        }

        public string AdminLine =>
            AllLegendsDefeated
                ? "ЛЕГЕНДЫ ГОРОДА • 4/4"
                : $"ЛЕГЕНДА {legendIndex + 1}/{LegendCount} • " +
                  $"{CurrentDefinition().Name} • " +
                  (IsUnlocked(CurrentDefinition())
                      ? ProgressText(CurrentDefinition())
                      : UnlockRequirements(CurrentDefinition()));

        public void Initialize(
            ActivityManager manager,
            PlayerWallet playerWallet,
            PlayerReputation playerReputation,
            DisciplineReputationSystem disciplineSystem,
            VehicleMasterySystem masterySystem)
        {
            activityManager =
                manager;

            wallet =
                playerWallet;

            reputation =
                playerReputation;

            disciplines =
                disciplineSystem;

            mastery =
                masterySystem;

            legendIndex =
                Mathf.Clamp(
                    PlayerPrefs.GetInt(
                        LegendIndexKey,
                        0),
                    0,
                    LegendCount);

            racingProgress =
                LoadProgress(
                    RacingProgressKey);

            driftProgress =
                LoadProgress(
                    DriftProgressKey);

            deliveryProgress =
                LoadProgress(
                    DeliveryProgressKey);

            nightProgress =
                LoadProgress(
                    NightProgressKey);

            if (activityManager != null)
            {
                activityManager.ActivityResultShown +=
                    HandleActivityResult;
            }
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.deltaTime);
            }

            if (AllLegendsDefeated ||
                unlockAnnounced)
            {
                return;
            }

            LegendDefinition current =
                CurrentDefinition();

            if (!IsUnlocked(
                    current))
            {
                return;
            }

            unlockAnnounced = true;

            StatusText =
                $"НОВАЯ ЛЕГЕНДА — {current.Name}   " +
                current.Intro;

            messageTimer =
                MessageSeconds;
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityResultShown -=
                    HandleActivityResult;
            }

            Save();
        }

        public void CompleteCurrentForTesting()
        {
            if (AllLegendsDefeated)
                return;

            LegendDefinition current =
                CurrentDefinition();

            racingProgress =
                current.RacingRequired;

            driftProgress =
                current.DriftRequired;

            deliveryProgress =
                current.DeliveryRequired;

            nightProgress =
                current.NightRequired;

            CompleteLegend(
                current);
        }

        public void UnlockCurrentForTesting()
        {
            if (AllLegendsDefeated)
                return;

            LegendDefinition current =
                CurrentDefinition();

            disciplines?.SetLevelForTesting(
                DisciplineType.Racing,
                Mathf.Max(
                    current.RacingLevelRequired,
                    disciplines.RacingLevel));

            disciplines?.SetLevelForTesting(
                DisciplineType.Drift,
                Mathf.Max(
                    current.DriftLevelRequired,
                    disciplines.DriftLevel));

            disciplines?.SetLevelForTesting(
                DisciplineType.Delivery,
                Mathf.Max(
                    current.DeliveryLevelRequired,
                    disciplines.DeliveryLevel));

            mastery?.SetCurrentLevelForTesting(
                Mathf.Max(
                    current.MasteryRequired,
                    mastery.CurrentLevel));

            unlockAnnounced = false;
        }

        public void ResetForTesting()
        {
            legendIndex = 0;
            unlockAnnounced = false;
            ClearProgress();
            Save();

            StatusText =
                "ЛЕГЕНДЫ ГОРОДА СБРОШЕНЫ";

            messageTimer =
                MessageSeconds;
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success ||
                AllLegendsDefeated)
            {
                return;
            }

            LegendDefinition current =
                CurrentDefinition();

            if (!IsUnlocked(
                    current))
            {
                return;
            }

            bool counted = false;

            switch (activityId)
            {
                case "sprint":
                case "circuit":
                    if (current.RacingRequired > 0)
                    {
                        racingProgress++;
                        counted = true;
                    }
                    break;

                case "drift":
                    if (current.DriftRequired > 0)
                    {
                        driftProgress++;
                        counted = true;
                    }
                    break;

                case "delivery":
                    if (current.DeliveryRequired > 0)
                    {
                        deliveryProgress++;
                        counted = true;
                    }
                    break;
            }

            if (counted &&
                current.NightRequired > 0 &&
                IsNight())
            {
                nightProgress++;
            }

            if (!counted)
                return;

            if (IsComplete(
                    current))
            {
                CompleteLegend(
                    current);
                return;
            }

            Save();

            StatusText =
                $"{current.Name} — " +
                ProgressText(
                    current);

            messageTimer =
                3f;
        }

        private void CompleteLegend(
            LegendDefinition current)
        {
            wallet?.AddCredits(
                current.Credits);

            reputation?.AddReputation(
                current.Reputation);

            string defeated =
                current.Name;

            legendIndex =
                Mathf.Min(
                    LegendCount,
                    legendIndex + 1);

            ClearProgress();
            unlockAnnounced = false;
            Save();

            StatusText =
                legendIndex >=
                LegendCount
                    ? $"ЛЕГЕНДЫ ГОРОДА ПОБЕЖДЕНЫ   " +
                      $"+{current.Credits:N0} КР   +{current.Reputation:N0} РЕП"
                    : $"{defeated} ПОБЕЖДЁН   " +
                      $"+{current.Credits:N0} КР   +{current.Reputation:N0} РЕП   " +
                      $"СЛЕДУЮЩИЙ: {CurrentDefinition().Name}";

            messageTimer =
                MessageSeconds;
        }

        private bool IsUnlocked(
            LegendDefinition current)
        {
            if (disciplines == null ||
                mastery == null)
            {
                return false;
            }

            return
                disciplines.RacingLevel >=
                    current.RacingLevelRequired &&
                disciplines.DriftLevel >=
                    current.DriftLevelRequired &&
                disciplines.DeliveryLevel >=
                    current.DeliveryLevelRequired &&
                mastery.CurrentLevel >=
                    current.MasteryRequired;
        }

        private bool IsComplete(
            LegendDefinition current)
        {
            return
                racingProgress >=
                    current.RacingRequired &&
                driftProgress >=
                    current.DriftRequired &&
                deliveryProgress >=
                    current.DeliveryRequired &&
                nightProgress >=
                    current.NightRequired;
        }

        private bool IsNight()
        {
            if (dayNight == null)
            {
                dayNight =
                    Object.FindAnyObjectByType<DayNightCycleController>();
            }

            return
                dayNight != null &&
                dayNight.IsNight;
        }

        private LegendDefinition CurrentDefinition()
        {
            return legendIndex switch
            {
                0 => new LegendDefinition(
                    "ПРИЗРАК",
                    "Ночной гонщик заметил тебя",
                    5,
                    1,
                    1,
                    4,
                    3,
                    0,
                    0,
                    3,
                    4500,
                    350),

                1 => new LegendDefinition(
                    "СКОЛЬЗЯЩИЙ",
                    "Король городского дрифта принимает вызов",
                    1,
                    5,
                    1,
                    4,
                    0,
                    4,
                    0,
                    0,
                    4200,
                    340),

                2 => new LegendDefinition(
                    "НОЛЬ",
                    "Самый быстрый курьер города оставил маршрут",
                    1,
                    1,
                    5,
                    4,
                    0,
                    0,
                    4,
                    2,
                    4000,
                    320),

                _ => new LegendDefinition(
                    "КОРОНА",
                    "Финальный вызов требует владения всеми стилями",
                    7,
                    7,
                    7,
                    6,
                    2,
                    2,
                    2,
                    3,
                    9000,
                    750)
            };
        }

        private static string UnlockRequirements(
            LegendDefinition current)
        {
            string result =
                $"НУЖНО МАСТ {current.MasteryRequired}";

            if (current.RacingLevelRequired > 1)
            {
                result +=
                    $" • ГОНКИ {current.RacingLevelRequired}";
            }

            if (current.DriftLevelRequired > 1)
            {
                result +=
                    $" • ДРИФТ {current.DriftLevelRequired}";
            }

            if (current.DeliveryLevelRequired > 1)
            {
                result +=
                    $" • ДОСТАВКА {current.DeliveryLevelRequired}";
            }

            return result;
        }

        private string ProgressText(
            LegendDefinition current)
        {
            string result =
                string.Empty;

            AppendProgress(
                ref result,
                "R",
                racingProgress,
                current.RacingRequired);

            AppendProgress(
                ref result,
                "D",
                driftProgress,
                current.DriftRequired);

            AppendProgress(
                ref result,
                "ДОСТ",
                deliveryProgress,
                current.DeliveryRequired);

            AppendProgress(
                ref result,
                "НОЧЬ",
                nightProgress,
                current.NightRequired);

            return
                string.IsNullOrEmpty(
                    result)
                    ? "ГОТОВО"
                    : result;
        }

        private static void AppendProgress(
            ref string text,
            string label,
            int progress,
            int required)
        {
            if (required <= 0)
                return;

            if (!string.IsNullOrEmpty(
                    text))
            {
                text += "  ";
            }

            text +=
                $"{label} {Mathf.Min(progress, required)}/{required}";
        }

        private void ClearProgress()
        {
            racingProgress = 0;
            driftProgress = 0;
            deliveryProgress = 0;
            nightProgress = 0;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(
                LegendIndexKey,
                legendIndex);

            PlayerPrefs.SetInt(
                RacingProgressKey,
                racingProgress);

            PlayerPrefs.SetInt(
                DriftProgressKey,
                driftProgress);

            PlayerPrefs.SetInt(
                DeliveryProgressKey,
                deliveryProgress);

            PlayerPrefs.SetInt(
                NightProgressKey,
                nightProgress);

            PlayerPrefs.Save();
        }

        private static int LoadProgress(
            string key)
        {
            return
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        key,
                        0));
        }

        private readonly struct LegendDefinition
        {
            public readonly string Name;
            public readonly string Intro;
            public readonly int RacingLevelRequired;
            public readonly int DriftLevelRequired;
            public readonly int DeliveryLevelRequired;
            public readonly int MasteryRequired;
            public readonly int RacingRequired;
            public readonly int DriftRequired;
            public readonly int DeliveryRequired;
            public readonly int NightRequired;
            public readonly int Credits;
            public readonly int Reputation;

            public LegendDefinition(
                string name,
                string intro,
                int racingLevelRequired,
                int driftLevelRequired,
                int deliveryLevelRequired,
                int masteryRequired,
                int racingRequired,
                int driftRequired,
                int deliveryRequired,
                int nightRequired,
                int credits,
                int reputation)
            {
                Name = name;
                Intro = intro;
                RacingLevelRequired =
                    racingLevelRequired;
                DriftLevelRequired =
                    driftLevelRequired;
                DeliveryLevelRequired =
                    deliveryLevelRequired;
                MasteryRequired =
                    masteryRequired;
                RacingRequired =
                    racingRequired;
                DriftRequired =
                    driftRequired;
                DeliveryRequired =
                    deliveryRequired;
                NightRequired =
                    nightRequired;
                Credits =
                    credits;
                Reputation =
                    reputation;
            }
        }
    }
}
