using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class DeliveryActivity : MonoBehaviour
    {
        private const string ActivityId = "delivery";

        [SerializeField] private int rewardCredits = 450;
        [SerializeField] private float checkpointRadius = 12f;
        [SerializeField] private float startRadius = 14f;
        [SerializeField] private float maxStartSpeedKph = 8f;
        [SerializeField] private float countdownSeconds = 3f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private Vector3[] route;
        private int checkpointIndex;
        private bool isCountingDown;
        private float countdownRemaining;

        public bool IsActive { get; private set; }
        public bool IsCountingDown => isCountingDown;
        public int CheckpointIndex => checkpointIndex;
        public int CheckpointCount => route?.Length ?? 0;
        public int RewardCredits => rewardCredits;
        public Vector3 CurrentTarget =>
            route == null || route.Length == 0
                ? Vector3.zero
                : route[Mathf.Clamp(checkpointIndex, 0, route.Length - 1)];
        public string StatusText { get; private set; } = "Синий маркер: доставка";

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager manager)
        {
            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            route = CityAssetRuntimeInstaller.DeliveryRoute;
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

                UpdateActiveDelivery();
                return;
            }

            checkpointIndex = 0;
            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(route[0]));

            if (distance > startRadius)
            {
                StatusText = "Синий маркер: доставка";
                return;
            }

            if (activityManager.IsBusy &&
                !activityManager.IsActive(ActivityId))
            {
                StatusText =
                    $"Доставка недоступна: активно «{activityManager.ActiveName}»";
                return;
            }

            if (car.SpeedKph > maxStartSpeedKph)
            {
                StatusText =
                    $"ДОСТАВКА — остановись до {maxStartSpeedKph:0} км/ч";
                return;
            }

            StatusText =
                $"ДОСТАВКА   E — НАЧАТЬ   +{rewardCredits} КР";

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
                    "Доставка"))
                return;

            isCountingDown = true;
            countdownRemaining =
                Mathf.Max(0.1f, countdownSeconds);
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
            checkpointIndex = 1;
            car.SetDrivingEnabled(true);
            StatusText =
                $"ДОСТАВКА  ТОЧКА {checkpointIndex + 1}/{route.Length}";
        }

        private void UpdateCountdownStatus()
        {
            int shown =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(countdownRemaining));

            StatusText =
                $"ДОСТАВКА   СТАРТ ЧЕРЕЗ {shown}   ESC — ОТМЕНА";
        }

        private void UpdateActiveDelivery()
        {
            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(CurrentTarget));

            if (distance > checkpointRadius)
                return;

            checkpointIndex++;

            if (checkpointIndex >= route.Length)
            {
                wallet.AddCredits(rewardCredits);
                IsActive = false;
                activityManager.End(ActivityId);
                checkpointIndex = 0;
                StatusText =
                    $"Доставка завершена  +{rewardCredits} КР";
                return;
            }

            StatusText =
                $"ДОСТАВКА  ТОЧКА {checkpointIndex + 1}/{route.Length}   ESC — ОТМЕНА";
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
            car?.SetDrivingEnabled(true);
            activityManager?.End(ActivityId);
            StatusText = "Доставка отменена";
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