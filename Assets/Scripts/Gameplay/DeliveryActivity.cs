using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class DeliveryActivity : MonoBehaviour
    {
        private const string ActivityId = "delivery";
        private const string BestTimeKey =
            "MotorCity.Delivery.BestTime";
        private const int EliteRequiredLevel = 3;

        [Header("Route")]
        [SerializeField] private float checkpointRadius = 12f;
        [SerializeField] private float startRadius = 14f;
        [SerializeField] private float maxStartSpeedKph = 8f;
        [SerializeField] private float countdownSeconds = 3f;

        [Header("Time tiers")]
        [SerializeField] private float goldTimeSeconds = 60f;
        [SerializeField] private float silverTimeSeconds = 85f;
        [SerializeField] private float bronzeTimeSeconds = 115f;

        [Header("Tier rewards")]
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
            "Синий маркер: доставка";

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
                route.Length < 2)
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
                    "Синий маркер: доставка";
                return;
            }

            if (activityManager.IsBusy &&
                !activityManager.IsActive(ActivityId))
            {
                StatusText =
                    $"Доставка недоступна: активно «{activityManager.ActiveName}»";
                return;
            }

            if (car.SpeedKph >
                maxStartSpeedKph)
            {
                StatusText =
                    $"ДОСТАВКА — остановись до {maxStartSpeedKph:0} км/ч";
                return;
            }

            string best =
                BestTimeSeconds > 0f
                    ? $"   РЕК {BestTimeSeconds:0.0}с"
                    : string.Empty;

            bool eliteUnlocked =
                activityManager.HasDisciplineLevel(
                    DisciplineType.Delivery,
                    EliteRequiredLevel);

            string eliteHint =
                eliteUnlocked
                    ? "   SHIFT+E — ПРЕМИУМ"
                    : $"   ПРЕМИУМ: ДОСТАВКА {EliteRequiredLevel}";

            StatusText =
                $"ДОСТАВКА   E — НАЧАТЬ   " +
                $"ЗОЛОТО ≤ {goldTimeSeconds:0}с{best}" +
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
                    "Доставка"))
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
                $"{(eliteMode ? "PREMIUM ДОСТАВКА" : "ДОСТАВКА")}   " +
                $"СТАРТ ЧЕРЕЗ {shown}   ESC — ОТМЕНА";
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
                $"{(eliteMode ? "PREMIUM ДОСТАВКА" : "ДОСТАВКА")}  " +
                $"ТОЧКА {checkpointIndex + 1}/{route.Length}   " +
                $"{ElapsedSeconds:0.0}с   {CurrentTierHint()}   ESC — ОТМЕНА";
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
                return $"ЗОЛОТО ≤ {gold:0}с";

            if (ElapsedSeconds <= silver)
                return $"СЕРЕБРО ≤ {silver:0}с";

            if (ElapsedSeconds <= bronze)
                return $"БРОНЗА ≤ {bronze:0}с";

            return "ДОСТАВЬ ГРУЗ";
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
                tier = "ЗОЛОТО";
                reward =
                    goldRewardCredits;
            }
            else if (ElapsedSeconds <=
                     silver)
            {
                tier = "СЕРЕБРО";
                reward =
                    silverRewardCredits;
            }
            else if (ElapsedSeconds <=
                     bronze)
            {
                tier = "БРОНЗА";
                reward =
                    bronzeRewardCredits;
            }
            else
            {
                tier = "ДОСТАВЛЕНО";
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

                PlayerPrefs.SetFloat(
                    BestTimeKey,
                    BestTimeSeconds);

                PlayerPrefs.Save();
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
                    ? "   •   НОВЫЙ РЕКОРД"
                    : BestTimeSeconds > 0f
                        ? $"   •   Рекорд: {BestTimeSeconds:0.0}с"
                        : string.Empty;

            activityManager.ShowResult(
                ActivityId,
                eliteMode
                    ? "ПРЕМИУМ-ДОСТАВКА"
                    : "ДОСТАВКА",
                tier,
                $"Время: {ElapsedSeconds:0.0}с{record}",
                reward,
                true);

            StatusText =
                $"Доставка: {tier}  +{reward} КР";
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
                "Доставка отменена";
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
