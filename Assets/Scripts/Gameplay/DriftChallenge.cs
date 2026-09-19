using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class DriftChallenge : MonoBehaviour
    {
        private const string ActivityId = "drift";

        private Vector3 zoneCenter;
        [SerializeField] private float startRadius = 15f;
        [SerializeField] private float activityHalfExtent = 52f;
        [SerializeField] private float outsideGraceSeconds = 3.5f;
        [SerializeField] private float durationSeconds = 42f;
        [SerializeField] private int targetScore = 1800;
        [SerializeField] private int rewardCredits = 650;
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
        public int TargetScore => targetScore;
        public int RewardCredits => rewardCredits;
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

            Keyboard keyboard = Keyboard.current;

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

            IsNearStart = distance <= startRadius;

            if (!armed)
            {
                IsNearStart = false;
                if (distance > startRadius + 4f)
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

            if (car.SpeedKph > maxStartSpeedKph)
            {
                StatusText =
                    $"ДРИФТ — остановись до {maxStartSpeedKph:0} км/ч";
                return;
            }

            StatusText =
                $"ДРИФТ-ЗАЕЗД   E — НАЧАТЬ   ЦЕЛЬ {targetScore:N0}";

            if (keyboard != null &&
                keyboard.eKey.wasPressedThisFrame)
            {
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
                Mathf.Max(0.1f, countdownSeconds);
            TimeRemaining = durationSeconds;
            outsideTimer = 0f;
            car.SetDrivingEnabled(false);
            UpdateCountdownStatus();
        }

        private void UpdateCountdown()
        {
            countdownRemaining =
                Mathf.Max(
                    0f,
                    countdownRemaining - Time.deltaTime);

            if (countdownRemaining > 0f)
            {
                UpdateCountdownStatus();
                return;
            }

            isCountingDown = false;
            IsActive = true;
            TimeRemaining = durationSeconds;
            scoreAtStart = drift.TotalScore;
            outsideTimer = 0f;
            car.SetDrivingEnabled(true);

            StatusText =
                $"ДРИФТ  0/{targetScore:N0}   {TimeRemaining:0.0}с   ESC — ОТМЕНА";
        }

        private void UpdateCountdownStatus()
        {
            int shown =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(countdownRemaining));

            StatusText =
                $"ДРИФТ   СТАРТ ЧЕРЕЗ {shown}   ESC — ОТМЕНА";
        }

        private void UpdateActiveChallenge()
        {
            Vector3 flatPosition =
                Flat(car.transform.position);

            TimeRemaining =
                Mathf.Max(
                    0f,
                    TimeRemaining - Time.deltaTime);

            if (CurrentScore >= targetScore)
            {
                wallet.AddCredits(rewardCredits);
                EndChallenge(
                    $"Дрифт-заезд завершён  +{rewardCredits} КР");
                return;
            }

            Vector3 local =
                flatPosition -
                Flat(zoneCenter);

            bool insideChallengeArea =
                Mathf.Abs(local.x) <= activityHalfExtent &&
                Mathf.Abs(local.z) <= activityHalfExtent;

            if (!insideChallengeArea)
            {
                outsideTimer += Time.deltaTime;

                float remainingGrace =
                    Mathf.Max(
                        0f,
                        outsideGraceSeconds - outsideTimer);

                if (outsideTimer >= outsideGraceSeconds)
                {
                    EndChallenge(
                        "Провал: слишком далеко от площадки");
                    return;
                }

                StatusText =
                    $"ДРИФТ  {CurrentScore:N0}/{targetScore:N0}   " +
                    $"{TimeRemaining:0.0}с   ВЕРНИСЬ {remainingGrace:0.0}с";
                return;
            }

            outsideTimer = 0f;

            if (TimeRemaining <= 0f)
            {
                EndChallenge(
                    $"Провал: {CurrentScore:N0}/{targetScore:N0}");
                return;
            }

            StatusText =
                $"ДРИФТ  {CurrentScore:N0}/{targetScore:N0}   " +
                $"{TimeRemaining:0.0}с   ESC — ОТМЕНА";
        }

        private void EndChallenge(string message)
        {
            IsActive = false;
            isCountingDown = false;
            activityManager.End(ActivityId);
            TimeRemaining = 0f;
            outsideTimer = 0f;
            car.SetDrivingEnabled(true);

            StatusText =
                message +
                ". Покинь зону, чтобы повторить.";
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

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}