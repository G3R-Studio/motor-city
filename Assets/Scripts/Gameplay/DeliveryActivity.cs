using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DeliveryActivity : MonoBehaviour
    {
        private const string ActivityId = "delivery";
        private const string BestTimeKey =
            "MotorCity.Delivery.BestTime";
        private const int EliteRequiredLevel = 3;

        [Header("Маршрут")]
        [SerializeField] private float checkpointRadius = 12f;
        [SerializeField] private float startRadius = 14f;
        [SerializeField] private float maxStartSpeedKph = 8f;
        [SerializeField] private float countdownSeconds = 3f;

        [Header("Пороги времени")]
        [SerializeField] private float goldTimeSeconds = 60f;
        [SerializeField] private float silverTimeSeconds = 85f;
        [SerializeField] private float bronzeTimeSeconds = 115f;

        [Header("Награды по уровням")]
        [SerializeField] private int goldRewardCredits = 800;
        [SerializeField] private int silverRewardCredits = 650;
        [SerializeField] private int bronzeRewardCredits = 500;
        [SerializeField] private int completionRewardCredits = 350;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private Vector3[] route;
        private int checkpointIndex;
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
        public int RewardCredits => goldRewardCredits;

        public Vector3 CurrentTarget =>
            route == null || route.Length == 0
                ? Vector3.zero
                : route[Mathf.Clamp(
                    checkpointIndex,
                    0,
                    route.Length - 1)];

        public string StatusText { get; private set; } =
            MotorCityLocalization.Text("activity.marker.delivery");

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager manager)
        {
            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            route = CityAssetRuntimeInstaller.DeliveryRoute;

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

                UpdateActiveDelivery();
                return;
            }

            checkpointIndex = 0;

            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(route[0]));

            IsNearStart =
                distance <= startRadius;

            if (!IsNearStart)
            {
                StatusText =
                    MotorCityLocalization.Text("activity.marker.delivery");
                return;
            }

            if (activityManager.IsBusy &&
                !activityManager.IsActive(ActivityId))
            {
                StatusText =
                    MotorCityLocalization.Format("activity.busy", MotorCityLocalization.Text("activity.delivery"), activityManager.ActiveName);
                return;
            }

            if (car.SpeedKph >
                maxStartSpeedKph)
            {
                StatusText =
                    MotorCityLocalization.Format("activity.stop", MotorCityLocalization.Text("activity.delivery"), maxStartSpeedKph);
                return;
            }

            string best =
                BestTimeSeconds > 0f
                    ? MotorCityLocalization.Format("activity.best_short", BestTimeSeconds)
                    : string.Empty;

            bool eliteUnlocked =
                activityManager.HasDisciplineLevel(
                    DisciplineType.Delivery,
                    EliteRequiredLevel);

            string eliteHint =
                eliteUnlocked
                    ? MotorCityLocalization.Text("activity.premium_hint")
                    : MotorCityLocalization.Format("activity.premium_locked", MotorCityLocalization.Text("discipline.delivery"), EliteRequiredLevel);

            StatusText =
                MotorCityLocalization.Format(
                    "activity.start_time",
                    MotorCityLocalization.Text("activity.delivery"),
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
                    MotorCityLocalization.Text("activity.delivery")))
                return;

            isCountingDown = true;
            countdownRemaining =
                Mathf.Max(
                    0.1f,
                    countdownSeconds);

            checkpointIndex = 0;
            ElapsedSeconds = 0f;

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
            checkpointIndex = 1;
            ElapsedSeconds = 0f;

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
                        ? MotorCityLocalization.Text("activity.premium_delivery")
                        : MotorCityLocalization.Text("activity.delivery"),
                    shown);
        }

        private void UpdateActiveDelivery()
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
                CompleteDelivery();
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
                        ? MotorCityLocalization.Text("activity.premium_delivery")
                        : MotorCityLocalization.Text("activity.delivery"),
                    checkpointIndex + 1,
                    route.Length,
                    ElapsedSeconds,
                    CurrentTierHint());
        }

        private string CurrentTierHint()
        {
            float gold =
                eliteMode ? 52f : goldTimeSeconds;
            float silver =
                eliteMode ? 72f : silverTimeSeconds;
            float bronze =
                eliteMode ? 100f : bronzeTimeSeconds;

            if (ElapsedSeconds <= gold)
                return MotorCityLocalization.Format("activity.tier_time", MotorCityLocalization.Text("medal.gold"), gold);

            if (ElapsedSeconds <= silver)
                return MotorCityLocalization.Format("activity.tier_time", MotorCityLocalization.Text("medal.silver"), silver);

            if (ElapsedSeconds <= bronze)
                return MotorCityLocalization.Format("activity.tier_time", MotorCityLocalization.Text("medal.bronze"), bronze);

            return MotorCityLocalization.Text("activity.deliver_cargo");
        }

        private void CompleteDelivery()
        {
            string tier;
            int reward;

            float gold =
                eliteMode ? 52f : goldTimeSeconds;
            float silver =
                eliteMode ? 72f : silverTimeSeconds;
            float bronze =
                eliteMode ? 100f : bronzeTimeSeconds;

            if (ElapsedSeconds <=
                gold)
            {
                tier = MotorCityLocalization.Text("medal.gold");
                reward =
                    goldRewardCredits;
            }
            else if (ElapsedSeconds <=
                     silver)
            {
                tier = MotorCityLocalization.Text("medal.silver");
                reward =
                    silverRewardCredits;
            }
            else if (ElapsedSeconds <=
                     bronze)
            {
                tier = MotorCityLocalization.Text("medal.bronze");
                reward =
                    bronzeRewardCredits;
            }
            else
            {
                tier = MotorCityLocalization.Text("activity.delivered");
                reward =
                    completionRewardCredits;
            }

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

            if (eliteMode)
            {
                reward =
                    Mathf.RoundToInt(
                        reward * 1.6f);
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
                    ? MotorCityLocalization.Text("activity.premium_delivery")
                    : MotorCityLocalization.Text("activity.delivery"),
                tier,
                MotorCityLocalization.Format("activity.result_time", ElapsedSeconds, record),
                reward,
                true);

            StatusText =
                MotorCityLocalization.Format("activity.status_reward", MotorCityLocalization.Text("activity.delivery"), tier, reward);
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

            BeginCountdown();
        }

        public void CancelActivity()
        {
            if (!IsActive &&
                !isCountingDown)
                return;

            IsActive = false;
            isCountingDown = false;
            countdownRemaining = 0f;
            checkpointIndex = 0;
            ElapsedSeconds = 0f;

            car?.SetDrivingEnabled(true);
            activityManager?.End(ActivityId);

            StatusText =
                MotorCityLocalization.Text("activity.delivery_cancelled");
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
