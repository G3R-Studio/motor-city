using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class StreetSprintActivity : MonoBehaviour
    {
        private const string ActivityId = "sprint";

        [SerializeField] private int baseRewardCredits = 550;
        [SerializeField] private int maximumTimeBonusCredits = 450;
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

        public bool IsActive { get; private set; }
        public bool IsCountingDown => isCountingDown;
        public float ElapsedSeconds { get; private set; }
        public int CheckpointIndex => checkpointIndex;
        public int CheckpointCount => route?.Length ?? 0;
        public Vector3 CurrentTarget =>
            route == null || route.Length == 0
                ? Vector3.zero
                : route[Mathf.Clamp(checkpointIndex, 0, route.Length - 1)];
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
        }

        private void Update()
        {
            if (car == null ||
                wallet == null ||
                activityManager == null ||
                route == null ||
                route.Length < 2)
                return;

            Keyboard keyboard = Keyboard.current;

            if (isCountingDown)
            {
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

            if (!armed)
            {
                if (distance > startRadius + 4f)
                {
                    armed = true;
                    StatusText =
                        "Зелёный маркер: уличный спринт";
                }

                return;
            }

            if (distance > startRadius)
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

            if (car.SpeedKph > maxStartSpeedKph)
            {
                StatusText =
                    $"СПРИНТ — остановись до {maxStartSpeedKph:0} км/ч";
                return;
            }

            StatusText =
                "СПРИНТ   E — НАЧАТЬ";

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
                    "Уличный спринт"))
                return;

            isCountingDown = true;
            armed = false;
            countdownRemaining =
                Mathf.Max(0.1f, countdownSeconds);
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
                    countdownRemaining - Time.deltaTime);

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
                    Mathf.CeilToInt(countdownRemaining));

            StatusText =
                $"СПРИНТ   СТАРТ ЧЕРЕЗ {shown}   ESC — ОТМЕНА";
        }

        private void UpdateActiveSprint()
        {
            ElapsedSeconds += Time.deltaTime;

            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(CurrentTarget));

            if (distance > checkpointRadius)
                return;

            checkpointIndex++;

            if (checkpointIndex >= route.Length)
            {
                CompleteSprint();
                return;
            }

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            StatusText =
                $"СПРИНТ  ТОЧКА {checkpointIndex + 1}/{route.Length}   " +
                $"{ElapsedSeconds:0.0}с   ESC — ОТМЕНА";
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

            wallet.AddCredits(reward);

            IsActive = false;
            activityManager.End(ActivityId);
            checkpointIndex = 0;

            StatusText =
                $"Спринт завершён за {ElapsedSeconds:0.0}с  +{reward} КР";
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

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}