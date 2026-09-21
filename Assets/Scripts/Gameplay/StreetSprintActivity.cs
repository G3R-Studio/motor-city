using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class StreetSprintActivity : MonoBehaviour
    {
        private const string ActivityId = "sprint";
        private const string BestTimeKey =
            "MotorCity.Sprint.BestTime";
        private const int EliteRequiredLevel = 3;

        [Header("Награда")]
        [SerializeField] private int baseRewardCredits = 550;
        [SerializeField] private int maximumTimeBonusCredits = 450;

        [Header("Пороги времени")]
        [SerializeField] private float goldTimeSeconds = 45f;
        [SerializeField] private float silverTimeSeconds = 60f;
        [SerializeField] private float bronzeTimeSeconds = 80f;

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
        private bool armed = true;
        private bool isCountingDown;
        private float countdownRemaining;
        private bool eliteMode;

        public bool IsActive { get; private set; }
        public bool IsCountingDown => isCountingDown;
        public bool IsNearStart { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public float BestTimeSeconds { get; private set; }
        public int CheckpointIndex => checkpointIndex;
        public int CheckpointCount => route?.Length ?? 0;

        public Vector3 CurrentTarget =>
            route == null || route.Length == 0
                ? Vector3.zero
                : route[Mathf.Clamp(
                    checkpointIndex,
                    0,
                    route.Length - 1)];

        public string StatusText { get; private set; } =
            MotorCityLocalization.Text("activity.marker.sprint");

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager manager)
        {
            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            route = CityAssetRuntimeInstaller.SprintRoute;

            BestTimeSeconds =
                Mathf.Max(
                    0f,
                    MotorCity.Persistence.MotorCitySaveService.GetFloat(
                        BestTimeKey,
                        0f));
        }

        private void Update()
        {
            if (car == null ||
                wallet == null ||
                activityManager == null ||
                route == null ||
                route.Length < 2)
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

                UpdateActiveSprint();
                return;
            }

            checkpointIndex = 0;

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
                        MotorCityLocalization.Text("activity.marker.sprint");
                }

                return;
            }

            if (!IsNearStart)
            {
                StatusText =
                    MotorCityLocalization.Text("activity.marker.sprint");
                return;
            }

            if (activityManager.IsBusy &&
                !activityManager.IsActive(ActivityId))
            {
                StatusText =
                    MotorCityLocalization.Format("activity.busy", MotorCityLocalization.Text("activity.sprint"), activityManager.ActiveName);
                return;
            }

            if (car.SpeedKph >
                maxStartSpeedKph)
            {
                StatusText =
                    MotorCityLocalization.Format("activity.stop", MotorCityLocalization.Text("hud.sprint"), maxStartSpeedKph);
                return;
            }

            string best =
                BestTimeSeconds > 0f
                    ? MotorCityLocalization.Format("activity.best_short", BestTimeSeconds)
                    : string.Empty;

            bool eliteUnlocked =
                activityManager.HasDisciplineLevel(
                    DisciplineType.Racing,
                    EliteRequiredLevel);

            string eliteHint =
                eliteUnlocked
                    ? MotorCityLocalization.Text("activity.elite_hint")
                    : MotorCityLocalization.Format("activity.elite_locked", MotorCityLocalization.Text("discipline.racing"), EliteRequiredLevel);

            StatusText =
                MotorCityLocalization.Format(
                    "activity.start_time",
                    MotorCityLocalization.Text("hud.sprint"),
                    goldTimeSeconds,
                    best,
                    eliteHint);

            if (MotorCityInput.InteractPressed)
            {
                bool wantsElite =
                    MotorCityInput.EliteModifierHeld;

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
                    MotorCityLocalization.Text("activity.sprint")))
                return;

            isCountingDown = true;
            armed = false;

            countdownRemaining =
                Mathf.Max(
                    0.1f,
                    countdownSeconds);

            ElapsedSeconds = 0f;
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
                MotorCityLocalization.Format(
                    "activity.countdown",
                    eliteMode
                        ? MotorCityLocalization.Text("activity.elite_sprint")
                        : MotorCityLocalization.Text("hud.sprint"),
                    shown);
        }

        private void UpdateActiveSprint()
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

            checkpointIndex++;

            if (checkpointIndex >=
                route.Length)
            {
                CompleteSprint();
                return;
            }

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            StatusText =
                MotorCityLocalization.Format(
                    "activity.checkpoint",
                    eliteMode
                        ? MotorCityLocalization.Text("activity.elite_sprint")
                        : MotorCityLocalization.Text("hud.sprint"),
                    checkpointIndex + 1,
                    route.Length,
                    ElapsedSeconds,
                    CurrentTierHint());
        }

        private string CurrentTierHint()
        {
            float gold =
                eliteMode ? 39f : goldTimeSeconds;
            float silver =
                eliteMode ? 52f : silverTimeSeconds;
            float bronze =
                eliteMode ? 70f : bronzeTimeSeconds;

            if (ElapsedSeconds <= gold)
                return MotorCityLocalization.Format("activity.tier_time", MotorCityLocalization.Text("medal.gold"), gold);

            if (ElapsedSeconds <= silver)
                return MotorCityLocalization.Format("activity.tier_time", MotorCityLocalization.Text("medal.silver"), silver);

            if (ElapsedSeconds <= bronze)
                return MotorCityLocalization.Format("activity.tier_time", MotorCityLocalization.Text("medal.bronze"), bronze);

            return MotorCityLocalization.Text("activity.finish_now");
        }

        private void CompleteSprint()
        {
            int bonus =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        maximumTimeBonusCredits,
                        0f,
                        Mathf.InverseLerp(
                            38f,
                            80f,
                            ElapsedSeconds)));

            int reward =
                baseRewardCredits +
                bonus;

            if (eliteMode)
            {
                reward =
                    Mathf.RoundToInt(
                        reward * 1.6f);
            }

            float gold =
                eliteMode ? 39f : goldTimeSeconds;
            float silver =
                eliteMode ? 52f : silverTimeSeconds;
            float bronze =
                eliteMode ? 70f : bronzeTimeSeconds;

            string tier =
                ElapsedSeconds <= gold
                    ? MotorCityLocalization.Text("medal.gold")
                    : ElapsedSeconds <= silver
                        ? MotorCityLocalization.Text("medal.silver")
                        : ElapsedSeconds <= bronze
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

            car.SetDrivingEnabled(false);

            string record =
                newBest
                    ? MotorCityLocalization.Text("activity.new_record_inline")
                    : BestTimeSeconds > 0f
                        ? MotorCityLocalization.Format("activity.record_inline", BestTimeSeconds)
                        : string.Empty;

            activityManager.ShowResult(
                ActivityId,
                eliteMode
                    ? MotorCityLocalization.Text("activity.elite_sprint")
                    : MotorCityLocalization.Text("activity.sprint"),
                tier,
                MotorCityLocalization.Format("activity.result_time_bonus", ElapsedSeconds, bonus, record),
                reward,
                true);

            StatusText =
                MotorCityLocalization.Format("activity.status_reward", MotorCityLocalization.Text("hud.sprint"), tier, reward);
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
            ElapsedSeconds = 0f;

            car?.SetDrivingEnabled(true);
            activityManager?.End(ActivityId);

            StatusText =
                MotorCityLocalization.Text("activity.sprint_cancelled");
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
