using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class DriftChallenge : MonoBehaviour
    {
        private const string ActivityId = "drift";
        private const int EliteRequiredLevel = 3;

        private Vector3 zoneCenter;
        [SerializeField] private float startRadius = 15f;
        [SerializeField] private float activityHalfExtent = 52f;
        [SerializeField] private float outsideGraceSeconds = 3.5f;
        [SerializeField] private float durationSeconds = 42f;

        [Header("Пороги очков")]
        [SerializeField] private int bronzeScore = 1800;
        [SerializeField] private int silverScore = 3000;
        [SerializeField] private int goldScore = 4500;
        [SerializeField] private int legendaryScore = 6000;

        [Header("Награды по уровням")]
        [SerializeField] private int bronzeRewardCredits = 650;
        [SerializeField] private int silverRewardCredits = 800;
        [SerializeField] private int goldRewardCredits = 1050;
        [SerializeField] private int legendaryRewardCredits = 1350;

        [SerializeField] private float maxStartSpeedKph = 8f;
        [SerializeField] private float countdownSeconds = 3f;

        private ArcadeCarController car;
        private DriftTracker drift;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private int scoreAtStart;
        private bool armed = true;
        private bool isCountingDown;
        private float countdownRemaining;
        private float outsideTimer;
        private bool eliteMode;

        public bool IsActive { get; private set; }
        public bool IsCountingDown => isCountingDown;
        public bool IsNearStart { get; private set; }
        public float TimeRemaining { get; private set; }

        public int CurrentScore =>
            drift == null
                ? 0
                : Mathf.Max(
                    0,
                    drift.TotalScore - scoreAtStart);

        public int TargetScore => bronzeScore;
        public int RewardCredits => bronzeRewardCredits;
        public Vector3 ZoneCenter => zoneCenter;

        public string StatusText { get; private set; } =
            "Оранжевая зона: дрифт-заезд";

        public void Initialize(
            ArcadeCarController targetCar,
            DriftTracker driftTracker,
            PlayerWallet targetWallet,
            ActivityManager manager)
        {
            car = targetCar;
            drift = driftTracker;
            wallet = targetWallet;
            activityManager = manager;

            zoneCenter =
                MotorCity.World.CityAssetRuntimeInstaller.DriftChallengePoint;

            TimeRemaining = durationSeconds;
        }

        private void Update()
        {
            if (car == null ||
                drift == null ||
                wallet == null ||
                activityManager == null)
                return;

            Keyboard keyboard =
                Keyboard.current;

            if (isCountingDown)
            {
                IsNearStart = false;

                if (keyboard != null &&
                    keyboard.escapeKey.wasPressedThisFrame)
                {
                    CancelActivity();
                    return;
                }

                UpdateCountdown();
                return;
            }

            if (IsActive)
            {
                IsNearStart = false;

                if (keyboard != null &&
                    keyboard.escapeKey.wasPressedThisFrame)
                {
                    CancelActivity();
                    return;
                }

                UpdateActiveChallenge();
                return;
            }

            Vector3 flatPosition =
                Flat(car.transform.position);

            float distance =
                Vector3.Distance(
                    flatPosition,
                    Flat(zoneCenter));

            IsNearStart =
                distance <= startRadius;

            if (!armed)
            {
                IsNearStart = false;

                if (distance >
                    startRadius + 4f)
                {
                    armed = true;
                    StatusText =
                        "Оранжевая зона: дрифт-заезд";
                }

                return;
            }

            if (!IsNearStart)
            {
                StatusText =
                    "Оранжевая зона: дрифт-заезд";
                return;
            }

            if (activityManager.IsBusy &&
                !activityManager.IsActive(ActivityId))
            {
                StatusText =
                    $"Дрифт-заезд недоступен: активно «{activityManager.ActiveName}»";
                return;
            }

            if (car.SpeedKph >
                maxStartSpeedKph)
            {
                StatusText =
                    $"ДРИФТ — остановись до {maxStartSpeedKph:0} км/ч";
                return;
            }

            bool eliteUnlocked =
                activityManager.HasDisciplineLevel(
                    DisciplineType.Drift,
                    EliteRequiredLevel);

            string eliteHint =
                eliteUnlocked
                    ? "   SHIFT+E — ЭЛИТА"
                    : $"   ЭЛИТА: ДРИФТ {EliteRequiredLevel}";

            StatusText =
                $"ДРИФТ-ЗАЕЗД   E — НАЧАТЬ   " +
                $"БРОНЗА {bronzeScore:N0}   ЛЕГЕНДА {legendaryScore:N0}" +
                eliteHint;

            if (keyboard != null &&
                keyboard.eKey.wasPressedThisFrame)
            {
                bool wantsElite =
                    keyboard.leftShiftKey.isPressed ||
                    keyboard.rightShiftKey.isPressed;

                eliteMode =
                    wantsElite &&
                    eliteUnlocked;

                BeginCountdown();
            }
        }

        private void BeginCountdown()
        {
            if (!activityManager.TryBegin(
                    ActivityId,
                    "Дрифт-заезд"))
                return;

            isCountingDown = true;
            armed = false;

            countdownRemaining =
                Mathf.Max(
                    0.1f,
                    countdownSeconds);

            TimeRemaining =
                eliteMode
                    ? 48f
                    : durationSeconds;

            outsideTimer = 0f;

            car.SetDrivingEnabled(false);

            UpdateCountdownStatus();
        }

        private void UpdateCountdown()
        {
            countdownRemaining =
                Mathf.Max(
                    0f,
                    countdownRemaining -
                    Time.deltaTime);

            if (countdownRemaining > 0f)
            {
                UpdateCountdownStatus();
                return;
            }

            isCountingDown = false;
            IsActive = true;
            TimeRemaining =
                eliteMode
                    ? 48f
                    : durationSeconds;

            scoreAtStart = drift.TotalScore;
            outsideTimer = 0f;

            car.SetDrivingEnabled(true);

            UpdateActiveStatus();
        }

        private void UpdateCountdownStatus()
        {
            int shown =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        countdownRemaining));

            StatusText =
                $"{(eliteMode ? "ЭЛИТНЫЙ ДРИФТ" : "ДРИФТ")}   " +
                $"СТАРТ ЧЕРЕЗ {shown}   ESC — ОТМЕНА";
        }

        private void UpdateActiveChallenge()
        {
            Vector3 flatPosition =
                Flat(car.transform.position);

            TimeRemaining =
                Mathf.Max(
                    0f,
                    TimeRemaining -
                    Time.deltaTime);

            Vector3 local =
                flatPosition -
                Flat(zoneCenter);

            bool insideChallengeArea =
                Mathf.Abs(local.x) <=
                    activityHalfExtent &&
                Mathf.Abs(local.z) <=
                    activityHalfExtent;

            if (!insideChallengeArea)
            {
                outsideTimer +=
                    Time.deltaTime;

                float remainingGrace =
                    Mathf.Max(
                        0f,
                        outsideGraceSeconds -
                        outsideTimer);

                if (outsideTimer >=
                    outsideGraceSeconds)
                {
                    FailChallenge(
                        "СЛИШКОМ ДАЛЕКО ОТ ПЛОЩАДКИ");
                    return;
                }

                StatusText =
                    $"ДРИФТ  {CurrentScore:N0}   " +
                    $"{TimeRemaining:0.0}с   " +
                    $"ВЕРНИСЬ {remainingGrace:0.0}с";
                return;
            }

            outsideTimer = 0f;

            if (TimeRemaining <= 0f)
            {
                FinishByScore();
                return;
            }

            UpdateActiveStatus();
        }

        private void UpdateActiveStatus()
        {
            StatusText =
                $"ДРИФТ  {CurrentScore:N0}   " +
                $"{CurrentTierProgress()}   " +
                $"{TimeRemaining:0.0}с   ESC — ОТМЕНА";
        }

        private string CurrentTierProgress()
        {
            int score =
                CurrentScore;

            int bronze =
                eliteMode ? 3000 : bronzeScore;
            int silver =
                eliteMode ? 4800 : silverScore;
            int gold =
                eliteMode ? 6800 : goldScore;
            int legendary =
                eliteMode ? 9000 : legendaryScore;

            if (score < bronze)
                return $"БРОНЗА {bronze:N0}";

            if (score < silver)
                return $"СЕРЕБРО {silver:N0}";

            if (score < gold)
                return $"ЗОЛОТО {gold:N0}";

            if (score < legendary)
                return $"ЛЕГЕНДА {legendary:N0}";

            return "ЛЕГЕНДА ДОСТИГНУТА";
        }

        private void FinishByScore()
        {
            int finalScore =
                CurrentScore;

            int bronze =
                eliteMode ? 3000 : bronzeScore;
            int silver =
                eliteMode ? 4800 : silverScore;
            int gold =
                eliteMode ? 6800 : goldScore;
            int legendary =
                eliteMode ? 9000 : legendaryScore;

            if (finalScore <
                bronze)
            {
                FailChallenge(
                    $"НЕ ХВАТИЛО ОЧКОВ: {finalScore:N0}/{bronze:N0}");
                return;
            }

            string tier;
            int reward;

            if (finalScore >=
                legendary)
            {
                tier = "ЛЕГЕНДА";
                reward =
                    legendaryRewardCredits;
            }
            else if (finalScore >=
                     gold)
            {
                tier = "ЗОЛОТО";
                reward =
                    goldRewardCredits;
            }
            else if (finalScore >=
                     silver)
            {
                tier = "СЕРЕБРО";
                reward =
                    silverRewardCredits;
            }
            else
            {
                tier = "БРОНЗА";
                reward =
                    bronzeRewardCredits;
            }

            if (eliteMode)
            {
                reward =
                    Mathf.RoundToInt(
                        reward * 1.6f);
            }

            wallet.AddCredits(
                reward);

            IsActive = false;
            isCountingDown = false;
            TimeRemaining = 0f;
            outsideTimer = 0f;

            car.SetDrivingEnabled(false);

            activityManager.ShowResult(
                ActivityId,
                eliteMode
                    ? "ЭЛИТНЫЙ ДРИФТ"
                    : "ДРИФТ-ЗАЕЗД",
                tier,
                $"Очки: {finalScore:N0}   •   Время: {(eliteMode ? 48f : durationSeconds):0}с",
                reward,
                true);

            StatusText =
                $"Дрифт-заезд: {tier}  +{reward} КР";
        }

        private void FailChallenge(
            string reason)
        {
            int finalScore =
                CurrentScore;

            IsActive = false;
            isCountingDown = false;
            TimeRemaining = 0f;
            outsideTimer = 0f;

            car.SetDrivingEnabled(false);

            activityManager.ShowResult(
                ActivityId,
                "ДРИФТ-ЗАЕЗД",
                "ПРОВАЛ",
                $"{reason}   •   Очки: {finalScore:N0}",
                0,
                false);

            StatusText =
                "Дрифт-заезд провален";
        }

        public void RestartFromResult()
        {
            if (activityManager == null ||
                !activityManager.HasResult ||
                activityManager.ResultActivityId !=
                    ActivityId ||
                car == null)
                return;

            activityManager.DismissResult();

            Quaternion rotation =
                Quaternion.Euler(
                    0f,
                    car.transform.eulerAngles.y,
                    0f);

            car.TeleportTo(
                zoneCenter +
                Vector3.up * 1.1f,
                rotation);

            armed = true;

            BeginCountdown();
        }

        public void CancelActivity()
        {
            if (!IsActive &&
                !isCountingDown)
                return;

            IsActive = false;
            isCountingDown = false;
            armed = false;
            countdownRemaining = 0f;
            TimeRemaining = 0f;
            outsideTimer = 0f;

            car?.SetDrivingEnabled(true);
            activityManager?.End(ActivityId);

            StatusText =
                "Дрифт-заезд отменён. Покинь зону, чтобы повторить.";
        }

        private void OnDisable()
        {
            car?.SetDrivingEnabled(true);
        }

        private static Vector3 Flat(
            Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
