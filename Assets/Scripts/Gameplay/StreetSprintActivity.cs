using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class StreetSprintActivity : MonoBehaviour
    {
        private const string ActivityId = "sprint";
        private const string BestTimeKey =
            "MotorCity.Sprint.BestTime";
        private const int EliteRequiredLevel = 3;

        [Header("Reward")]
        [SerializeField] private int baseRewardCredits = 550;
        [SerializeField] private int maximumTimeBonusCredits = 450;

        [Header("Time tiers")]
        [SerializeField] private float goldTimeSeconds = 45f;
        [SerializeField] private float silverTimeSeconds = 60f;
        [SerializeField] private float bronzeTimeSeconds = 80f;

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
            "Зелёный маркер: уличный спринт";

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
                        "Зелёный маркер: уличный спринт";
                }

                return;
            }

            if (!IsNearStart)
            {
                StatusText =
                    "Зелёный маркер: уличный спринт";
                return;
            }

            if (activityManager.IsBusy &&
                !activityManager.IsActive(ActivityId))
            {
                StatusText =
                    $"Спринт недоступен: активно «{activityManager.ActiveName}»";
                return;
            }

            if (car.SpeedKph >
                maxStartSpeedKph)
            {
                StatusText =
                    $"СПРИНТ — остановись до {maxStartSpeedKph:0} км/ч";
                return;
            }

            string best =
                BestTimeSeconds > 0f
                    ? $"   РЕК {BestTimeSeconds:0.0}с"
                    : string.Empty;

            bool eliteUnlocked =
                activityManager.HasDisciplineLevel(
                    DisciplineType.Racing,
                    EliteRequiredLevel);

            string eliteHint =
                eliteUnlocked
                    ? "   SHIFT+E — ЭЛИТА"
                    : $"   ЭЛИТА: ГОНКИ {EliteRequiredLevel}";

            StatusText =
                $"СПРИНТ   E — НАЧАТЬ   " +
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
                    "Уличный спринт"))
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
                $"{(eliteMode ? "ELITE СПРИНТ" : "СПРИНТ")}   " +
                $"СТАРТ ЧЕРЕЗ {shown}   ESC — ОТМЕНА";
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
                $"{(eliteMode ? "ELITE СПРИНТ" : "СПРИНТ")}  " +
                $"ТОЧКА {checkpointIndex + 1}/{route.Length}   " +
                $"{ElapsedSeconds:0.0}с   {CurrentTierHint()}   ESC — ОТМЕНА";
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
                return $"ЗОЛОТО ≤ {gold:0}с";

            if (ElapsedSeconds <= silver)
                return $"СЕРЕБРО ≤ {silver:0}с";

            if (ElapsedSeconds <= bronze)
                return $"БРОНЗА ≤ {bronze:0}с";

            return "ФИНИШИРУЙ";
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
                    ? "ЗОЛОТО"
                    : ElapsedSeconds <= silver
                        ? "СЕРЕБРО"
                        : ElapsedSeconds <= bronze
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
                    ? "ЭЛИТНЫЙ УЛИЧНЫЙ СПРИНТ"
                    : "УЛИЧНЫЙ СПРИНТ",
                tier,
                $"Время: {ElapsedSeconds:0.0}с   •   Бонус: {bonus:N0} КР{record}",
                reward,
                true);

            StatusText =
                $"Спринт: {tier}  +{reward} КР";
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
                "Спринт отменён. Отъедь от старта, чтобы повторить.";
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
