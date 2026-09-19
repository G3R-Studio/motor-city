using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class CircuitRaceActivity : MonoBehaviour
    {
        private const string ActivityId = "circuit";
        private const string BestTimeKey =
            "MotorCity.Circuit.BestTime";
        private const string BestLapKey =
            "MotorCity.Circuit.BestLap";

        [Header("Race")]
        [SerializeField] private int lapCount = 2;
        [SerializeField] private int baseRewardCredits = 800;
        [SerializeField] private int maximumTimeBonusCredits = 700;

        [Header("Time tiers")]
        [SerializeField] private float goldTimeSeconds = 105f;
        [SerializeField] private float silverTimeSeconds = 130f;
        [SerializeField] private float bronzeTimeSeconds = 160f;

        [Header("Start")]
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
        private float lapStartElapsedSeconds;
        private float sessionBestLapSeconds;

        public bool IsActive { get; private set; }
        public bool IsCountingDown => isCountingDown;
        public bool IsNearStart { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public int CurrentLap => currentLap;
        public int LapCount => lapCount;
        public int CheckpointIndex => checkpointIndex;
        public int CheckpointCount => route?.Length ?? 0;
        public float BestTimeSeconds { get; private set; }
        public float BestLapSeconds { get; private set; }
        public float SessionBestLapSeconds => sessionBestLapSeconds;

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

            BestLapSeconds =
                Mathf.Max(
                    0f,
                    PlayerPrefs.GetFloat(
                        BestLapKey,
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

                UpdateActiveRace();
                return;
            }

            checkpointIndex = 0;
            currentLap = 1;

            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(route[0]));

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

            if (car.SpeedKph >
                maxStartSpeedKph)
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
                $"КОЛЬЦО   E — НАЧАТЬ   " +
                $"ЗОЛОТО ≤ {goldTimeSeconds:0}с{best}";

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
                Mathf.Max(
                    0.1f,
                    countdownSeconds);

            ElapsedSeconds = 0f;
            lapStartElapsedSeconds = 0f;
            sessionBestLapSeconds = 0f;
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
                    countdownRemaining -
                    Time.deltaTime);

            if (countdownRemaining > 0f)
            {
                UpdateCountdownStatus();
                return;
            }

            isCountingDown = false;
            IsActive = true;
            ElapsedSeconds = 0f;
            lapStartElapsedSeconds = 0f;
            sessionBestLapSeconds = 0f;
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
                    Mathf.CeilToInt(
                        countdownRemaining));

            StatusText =
                $"КОЛЬЦО   СТАРТ ЧЕРЕЗ {shown}   ESC — ОТМЕНА";
        }

        private void UpdateActiveRace()
        {
            ElapsedSeconds +=
                Time.deltaTime;

            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(CurrentTarget));

            if (distance >
                checkpointRadius)
            {
                UpdateStatus();
                return;
            }

            if (checkpointIndex == 0)
            {
                RecordLap();

                if (currentLap >=
                    lapCount)
                {
                    CompleteRace();
                    return;
                }

                currentLap++;
                lapStartElapsedSeconds =
                    ElapsedSeconds;
                checkpointIndex = 1;

                UpdateStatus();
                return;
            }

            checkpointIndex++;

            if (checkpointIndex >=
                route.Length)
            {
                checkpointIndex = 0;
            }

            UpdateStatus();
        }

        private void RecordLap()
        {
            float lapTime =
                Mathf.Max(
                    0f,
                    ElapsedSeconds -
                    lapStartElapsedSeconds);

            if (lapTime <= 0f)
                return;

            if (sessionBestLapSeconds <= 0f ||
                lapTime <
                sessionBestLapSeconds)
            {
                sessionBestLapSeconds =
                    lapTime;
            }

            if (BestLapSeconds <= 0f ||
                lapTime <
                BestLapSeconds)
            {
                BestLapSeconds =
                    lapTime;

                PlayerPrefs.SetFloat(
                    BestLapKey,
                    BestLapSeconds);

                PlayerPrefs.Save();
            }
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

            string tier =
                ElapsedSeconds <= goldTimeSeconds
                    ? "ЗОЛОТО"
                    : ElapsedSeconds <= silverTimeSeconds
                        ? "СЕРЕБРО"
                        : ElapsedSeconds <= bronzeTimeSeconds
                            ? "БРОНЗА"
                            : "ФИНИШ";

            bool newBest =
                BestTimeSeconds <= 0f ||
                ElapsedSeconds <
                BestTimeSeconds;

            if (newBest)
            {
                BestTimeSeconds =
                    ElapsedSeconds;

                PlayerPrefs.SetFloat(
                    BestTimeKey,
                    BestTimeSeconds);

                PlayerPrefs.Save();
            }

            wallet.AddCredits(
                reward);

            IsActive = false;
            checkpointIndex = 0;
            currentLap = 1;

            car.SetDrivingEnabled(false);

            string record =
                newBest
                    ? "НОВЫЙ РЕКОРД"
                    : BestTimeSeconds > 0f
                        ? $"Рекорд: {BestTimeSeconds:0.0}с"
                        : string.Empty;

            string lap =
                sessionBestLapSeconds > 0f
                    ? $"   •   Лучший круг: {sessionBestLapSeconds:0.0}с"
                    : string.Empty;

            activityManager.ShowResult(
                ActivityId,
                "КОЛЬЦЕВАЯ ГОНКА",
                tier,
                $"Время: {ElapsedSeconds:0.0}с   •   {record}{lap}   •   Бонус: {bonus:N0} КР",
                reward,
                true);

            StatusText =
                $"Кольцо: {tier}  +{reward} КР";
        }

        public void RestartFromResult()
        {
            if (activityManager == null ||
                !activityManager.HasResult ||
                activityManager.ResultActivityId !=
                    ActivityId ||
                route == null ||
                route.Length < 2 ||
                car == null)
                return;

            activityManager.DismissResult();

            Vector3 direction =
                Flat(
                    route[1] -
                    route[0]);

            Quaternion rotation =
                direction.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up)
                    : Quaternion.Euler(
                        0f,
                        car.transform.eulerAngles.y,
                        0f);

            car.TeleportTo(
                route[0] +
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
            checkpointIndex = 0;
            currentLap = 1;
            ElapsedSeconds = 0f;
            lapStartElapsedSeconds = 0f;
            sessionBestLapSeconds = 0f;

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

            float currentLapTime =
                Mathf.Max(
                    0f,
                    ElapsedSeconds -
                    lapStartElapsedSeconds);

            string lapBest =
                BestLapSeconds > 0f
                    ? $"   ЛУЧШ КРУГ {BestLapSeconds:0.0}с"
                    : string.Empty;

            StatusText =
                $"КОЛЬЦО  КРУГ {currentLap}/{lapCount}   " +
                $"ТОЧКА {shownCheckpoint}/{total}   " +
                $"КРУГ {currentLapTime:0.0}с   " +
                $"ОБЩ {ElapsedSeconds:0.0}с{lapBest}   ESC — ОТМЕНА";
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
