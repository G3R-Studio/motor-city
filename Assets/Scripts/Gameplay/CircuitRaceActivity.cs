using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class CircuitRaceActivity : MonoBehaviour
    {
        private const string ActivityId = "circuit";
        private const string BestTimeKey = "MotorCity.Circuit.BestTime";

        [SerializeField] private int lapCount = 2;
        [SerializeField] private int baseRewardCredits = 800;
        [SerializeField] private int maximumTimeBonusCredits = 700;
        [SerializeField] private float startRadius = 14f;
        [SerializeField] private float checkpointRadius = 14f;
        [SerializeField] private float maxStartSpeedKph = 8f;
        [SerializeField] private float countdownSeconds = 3f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private Vector3[] route;
        private int checkpointIndex;
        private int currentLap = 1;
        private bool armed = true;
        private bool isCountingDown;
        private float countdownRemaining;

        public bool IsActive { get; private set; }
        public bool IsCountingDown => isCountingDown;
        public bool IsNearStart { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public int CurrentLap => currentLap;
        public int LapCount => lapCount;
        public int CheckpointIndex => checkpointIndex;
        public int CheckpointCount => route?.Length ?? 0;
        public float BestTimeSeconds { get; private set; }

        public Vector3 CurrentTarget =>
            route == null || route.Length == 0
                ? Vector3.zero
                : route[Mathf.Clamp(
                    checkpointIndex,
                    0,
                    route.Length - 1)];

        public string StatusText { get; private set; } =
            "Бирюзовый флаг: кольцевая гонка";

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager manager)
        {
            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            route = CityAssetRuntimeInstaller.CircuitRoute;

            BestTimeSeconds =
                Mathf.Max(
                    0f,
                    PlayerPrefs.GetFloat(
                        BestTimeKey,
                        0f));
        }

        private void Update()
        {
            if (car == null ||
                wallet == null ||
                activityManager == null ||
                route == null ||
                route.Length < 4)
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

                UpdateActiveRace();
                return;
            }

            checkpointIndex = 0;
            currentLap = 1;

            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(route[0]));

            IsNearStart = distance <= startRadius;

            if (!armed)
            {
                IsNearStart = false;
                if (distance > startRadius + 4f)
                {
                    armed = true;
                    StatusText =
                        "Бирюзовый флаг: кольцевая гонка";
                }

                return;
            }

            if (!IsNearStart)
            {
                StatusText =
                    "Бирюзовый флаг: кольцевая гонка";
                return;
            }

            if (activityManager.IsBusy &&
                !activityManager.IsActive(ActivityId))
            {
                StatusText =
                    $"Кольцо недоступно: активно «{activityManager.ActiveName}»";
                return;
            }

            if (car.SpeedKph > maxStartSpeedKph)
            {
                StatusText =
                    $"КОЛЬЦО — остановись до {maxStartSpeedKph:0} км/ч";
                return;
            }

            string best =
                BestTimeSeconds > 0f
                    ? $"   РЕК {BestTimeSeconds:0.0}с"
                    : string.Empty;

            StatusText =
                $"КОЛЬЦО   E — НАЧАТЬ{best}";

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
                    "Кольцевая гонка"))
                return;

            isCountingDown = true;
            armed = false;
            countdownRemaining =
                Mathf.Max(0.1f, countdownSeconds);
            ElapsedSeconds = 0f;
            currentLap = 1;
            checkpointIndex = 0;
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
            ElapsedSeconds = 0f;
            currentLap = 1;
            checkpointIndex = 1;
            car.SetDrivingEnabled(true);
            UpdateStatus();
        }

        private void UpdateCountdownStatus()
        {
            int shown =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(countdownRemaining));

            StatusText =
                $"КОЛЬЦО   СТАРТ ЧЕРЕЗ {shown}   ESC — ОТМЕНА";
        }

        private void UpdateActiveRace()
        {
            ElapsedSeconds += Time.deltaTime;

            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(CurrentTarget));

            if (distance > checkpointRadius)
                return;

            if (checkpointIndex == 0)
            {
                if (currentLap >= lapCount)
                {
                    CompleteRace();
                    return;
                }

                currentLap++;
                checkpointIndex = 1;
                UpdateStatus();
                return;
            }

            checkpointIndex++;

            if (checkpointIndex >= route.Length)
            {
                checkpointIndex = 0;
            }

            UpdateStatus();
        }

        private void CompleteRace()
        {
            int bonus =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        maximumTimeBonusCredits,
                        0f,
                        Mathf.InverseLerp(
                            92f,
                            170f,
                            ElapsedSeconds)));

            int reward =
                baseRewardCredits +
                bonus;

            bool newBest =
                BestTimeSeconds <= 0f ||
                ElapsedSeconds < BestTimeSeconds;

            if (newBest)
            {
                BestTimeSeconds =
                    ElapsedSeconds;

                PlayerPrefs.SetFloat(
                    BestTimeKey,
                    BestTimeSeconds);

                PlayerPrefs.Save();
            }

            wallet.AddCredits(reward);

            IsActive = false;
            activityManager.End(ActivityId);
            checkpointIndex = 0;
            currentLap = 1;

            string record =
                newBest
                    ? "   НОВЫЙ РЕКОРД!"
                    : BestTimeSeconds > 0f
                        ? $"   РЕКОРД {BestTimeSeconds:0.0}с"
                        : string.Empty;

            StatusText =
                $"Кольцо завершено за {ElapsedSeconds:0.0}с  +{reward} КР{record}";
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
            checkpointIndex = 0;
            currentLap = 1;
            ElapsedSeconds = 0f;
            car?.SetDrivingEnabled(true);
            activityManager?.End(ActivityId);

            StatusText =
                "Кольцевая гонка отменена. Отъедь от старта, чтобы повторить.";
        }

        private void UpdateStatus()
        {
            int shownCheckpoint =
                checkpointIndex == 0
                    ? route.Length
                    : checkpointIndex + 1;

            int total =
                route.Length;

            string best =
                BestTimeSeconds > 0f
                    ? $"   РЕК {BestTimeSeconds:0.0}с"
                    : string.Empty;

            StatusText =
                $"КОЛЬЦО  КРУГ {currentLap}/{lapCount}   " +
                $"ТОЧКА {shownCheckpoint}/{total}   " +
                $"{ElapsedSeconds:0.0}с{best}   ESC — ОТМЕНА";
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