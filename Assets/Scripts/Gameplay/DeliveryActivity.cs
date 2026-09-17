using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DeliveryActivity : MonoBehaviour
    {
        private const string ActivityId = "delivery";

        [SerializeField] private int rewardCredits = 450;
        [SerializeField] private float checkpointRadius = 5f;
        [SerializeField] private float startRadius = 7f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private Vector3[] route;
        private int checkpointIndex;

        public bool IsActive { get; private set; }
        public int CheckpointIndex => checkpointIndex;
        public int CheckpointCount => route?.Length ?? 0;
        public int RewardCredits => rewardCredits;
        public Vector3 CurrentTarget => route == null || route.Length == 0 ? Vector3.zero : route[Mathf.Clamp(checkpointIndex, 0, route.Length - 1)];
        public string StatusText { get; private set; } = "Blue marker: delivery";

        public void Initialize(ArcadeCarController targetCar, PlayerWallet targetWallet, ActivityManager manager)
        {
            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            route = new[]
            {
                new Vector3(42f, 0f, -42f),
                new Vector3(84f, 0f, 0f),
                new Vector3(42f, 0f, 84f),
                new Vector3(-42f, 0f, 84f),
                new Vector3(-84f, 0f, 42f)
            };
        }

        private void Update()
        {
            if (car == null || wallet == null || activityManager == null || route == null || route.Length == 0) return;

            float distance = Vector3.Distance(Flat(car.transform.position), Flat(CurrentTarget));

            if (!IsActive)
            {
                checkpointIndex = 0;
                if (distance <= startRadius)
                {
                    if (!activityManager.TryBegin(ActivityId, "Delivery"))
                    {
                        StatusText = $"Delivery unavailable during {activityManager.ActiveName}";
                        return;
                    }

                    IsActive = true;
                    checkpointIndex = 1;
                    StatusText = $"DELIVERY  CP {checkpointIndex + 1}/{route.Length}";
                }
                else
                {
                    StatusText = "Blue marker: delivery";
                }
                return;
            }

            if (distance > checkpointRadius) return;

            checkpointIndex++;
            if (checkpointIndex >= route.Length)
            {
                wallet.AddCredits(rewardCredits);
                IsActive = false;
                activityManager.End(ActivityId);
                checkpointIndex = 0;
                StatusText = $"Delivery complete +{rewardCredits} CR";
                return;
            }

            StatusText = $"DELIVERY  CP {checkpointIndex + 1}/{route.Length}";
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
