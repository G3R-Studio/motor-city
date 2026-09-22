using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CircuitRaceActivity : MonoBehaviour
    {
        private const string ActivityId = "circuit";
        private const string BestTimeKey =
            "MotorCity.Circuit.BestTime";
        private const string BestLapKey =
            "MotorCity.Circuit.BestLap";

        [Header("Гонка")]
        [SerializeField] private int lapCount = 2;
        [SerializeField] private int baseRewardCredits = 800;
        [SerializeField] private int maximumTimeBonusCredits = 700;

        [Header("Пороги времени")]
        [SerializeField] private float goldTimeSeconds = 105f;
        [SerializeField] private float silverTimeSeconds = 130f;
        [SerializeField] private float bronzeTimeSeconds = 160f;

        [Header("Старт")]
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

        public bool TryGetNextTarget(
            out Vector3 target)
        {
            target =
                CurrentTarget;

            if (route == null ||
                route.Length < 2)
            {
                return false;
            }

            int currentIndex =
                Mathf.Clamp(
                    checkpointIndex,
                    0,
                    route.Length - 1);

            int nextIndex =
                (currentIndex + 1) %
                route.Length;

            target =
                route[nextIndex];

            return true;
        }

        public string StatusText { get; private set; } =
            MotorCityLocalization.Text("activity.marker.circuit");

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
                    MotorCity.Persistence.MotorCitySaveService.GetFloat(
                        BestTimeKey,
                        0f));

            BestLapSeconds =
                Mathf.Max(
                    0f,
                    MotorCity.Persistence.MotorCitySaveService.GetFloat(
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

            if (isCountingDown)
            {
                IsNearStart = false;

                if (MotorCityInput.CancelPressed)
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

                if (MotorCityInput.CancelPressed)
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
                        MotorCityLocalization.Text("activity.marker.circuit");
                }

                return;
            }

            if (!IsNearStart)
            {
                StatusText =
                    MotorCityLocalization.Text("activity.marker.circuit");
                return;
            }

            if (activityManager.IsBusy &&
                !activityManager.IsActive(ActivityId))
            {
                StatusText =
                    MotorCityLocalization.Format("activity.busy", MotorCityLocalization.Text("activity.circuit"), activityManager.ActiveName);
                return;
            }

            if (car.SpeedKph >
                maxStartSpeedKph)
            {
                StatusText =
                    MotorCityLocalization.Format("activity.stop", MotorCityLocalization.Text("hud.circuit"), maxStartSpeedKph);
                return;
            }

            string best =
                BestTimeSeconds > 0f
                    ? MotorCityLocalization.Format("activity.best_short", BestTimeSeconds)
                    : string.Empty;

            StatusText =
                MotorCityLocalization.Format(
                    "activity.start_time",
                    MotorCityLocalization.Text("hud.circuit"),
                    goldTimeSeconds,
                    best,
                    string.Empty);

            if (MotorCityInput.InteractPressed)
            {
                BeginCountdown();
            }
        }

        private void BeginCountdown()
        {
            if (!activityManager.TryBegin(
                    ActivityId,
                    MotorCityLocalization.Text("activity.circuit")))
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
                MotorCityLocalization.Format("activity.countdown", MotorCityLocalization.Text("hud.circuit"), shown);
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

                MotorCity.Persistence.MotorCitySaveService.SetFloat(
                    BestLapKey,
                    BestLapSeconds);

                MotorCity.Persistence.MotorCitySaveService.Save();
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
                    ? MotorCityLocalization.Text("medal.gold")
                    : ElapsedSeconds <= silverTimeSeconds
                        ? MotorCityLocalization.Text("medal.silver")
                        : ElapsedSeconds <= bronzeTimeSeconds
                            ? MotorCityLocalization.Text("medal.bronze")
                            : MotorCityLocalization.Text("common.finish");

            bool newBest =
                BestTimeSeconds <= 0f ||
                ElapsedSeconds <
                BestTimeSeconds;

            if (newBest)
            {
                BestTimeSeconds =
                    ElapsedSeconds;

                MotorCity.Persistence.MotorCitySaveService.SetFloat(
                    BestTimeKey,
                    BestTimeSeconds);

                MotorCity.Persistence.MotorCitySaveService.Save();
            }

            wallet.AddCredits(
                reward);

            IsActive = false;
            checkpointIndex = 0;
            currentLap = 1;

            car.SetDrivingEnabled(false);

            string record =
                newBest
                    ? MotorCityLocalization.Text("common.new_record")
                    : BestTimeSeconds > 0f
                        ? MotorCityLocalization.Format("common.record", BestTimeSeconds)
                        : string.Empty;

            string lap =
                sessionBestLapSeconds > 0f
                    ? MotorCityLocalization.Format("activity.best_lap", sessionBestLapSeconds)
                    : string.Empty;

            activityManager.ShowResult(
                ActivityId,
                MotorCityLocalization.Text("activity.circuit"),
                tier,
                MotorCityLocalization.Format("activity.circuit_result", ElapsedSeconds, record, lap, bonus),
                reward,
                true);

            StatusText =
                MotorCityLocalization.Format("activity.status_reward", MotorCityLocalization.Text("hud.circuit"), tier, reward);
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
                MotorCityLocalization.Text("activity.circuit_cancelled");
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
                    ? MotorCityLocalization.Format("activity.best_lap_short", BestLapSeconds)
                    : string.Empty;

            StatusText =
                MotorCityLocalization.Format(
                    "activity.circuit_status",
                    currentLap,
                    lapCount,
                    shownCheckpoint,
                    total,
                    currentLapTime,
                    ElapsedSeconds,
                    lapBest);
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
