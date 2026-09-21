using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DiscoverySystem : MonoBehaviour
    {
        private const float DiscoverRadius = 16f;
        private const float MessageSeconds = 3.5f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private ActivityManager activityManager;
        private Discovery[] discoveries;
        private float messageTimer;

        public bool ShowMessage => messageTimer > 0f;
        public string StatusText { get; private set; }
        public int DiscoveryCount => discoveries?.Length ?? 0;

        public int FoundCount
        {
            get
            {
                if (discoveries == null)
                    return 0;

                int count = 0;
                foreach (Discovery item in discoveries)
                {
                    if (item.Found)
                        count++;
                }

                return count;
            }
        }

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            PlayerReputation targetReputation,
            ActivityManager manager)
        {
            car = targetCar;
            wallet = targetWallet;
            reputation = targetReputation;
            activityManager = manager;

            discoveries = new[]
            {
                CreateDiscovery(
                    "main_north",
                    "СЕВЕР ГЛАВНОГО РАЙОНА",
                    new Vector3(450f, 0f, 430f)),

                CreateDiscovery(
                    "main_west",
                    "ЗАПАДНАЯ ОКРАИНА",
                    new Vector3(-450f, 0f, 360f)),

                CreateDiscovery(
                    "highway_mid",
                    "СЕРЕДИНА ШОССЕ",
                    new Vector3(-300f, 0f, -1000f)),

                CreateDiscovery(
                    "remote_east",
                    "ВОСТОК ДАЛЬНЕГО РАЙОНА",
                    new Vector3(20f, 0f, -1832f)),

                CreateDiscovery(
                    "remote_south",
                    "ЮГ ДАЛЬНЕГО РАЙОНА",
                    new Vector3(-300f, 0f, -2010f))
            };
        }

        private Discovery CreateDiscovery(
            string id,
            string name,
            Vector3 preferred)
        {
            CityAssetRuntimeInstaller.ResolveNearestRoadResetPose(
                preferred,
                Vector3.forward,
                out Vector3 carPosition,
                out _);

            return new Discovery
            {
                Id = id,
                DisplayName = name,
                Position = carPosition - Vector3.up * 1.10f,
                Found = PlayerPrefs.GetInt(
                    $"MotorCity.Discovery.{id}",
                    0) != 0
            };
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer = Mathf.Max(
                    0f,
                    messageTimer - Time.deltaTime);
            }

            if (car == null ||
                discoveries == null)
                return;

            if (activityManager != null &&
                activityManager.IsBusy)
                return;

            Vector3 carPosition = Flat(
                car.transform.position);

            for (int i = 0;
                 i < discoveries.Length;
                 i++)
            {
                Discovery item = discoveries[i];

                if (item.Found)
                    continue;

                float distance = Vector3.Distance(
                    carPosition,
                    Flat(item.Position));

                if (distance > DiscoverRadius)
                    continue;

                Discover(item);
                return;
            }
        }

        private void Discover(
            Discovery item)
        {
            item.Found = true;

            PlayerPrefs.SetInt(
                $"MotorCity.Discovery.{item.Id}",
                1);

            PlayerPrefs.Save();

            const int credits = 90;
            const int rep = 35;

            wallet?.AddCredits(credits);
            reputation?.AddReputation(rep);

            StatusText =
                $"ОТКРЫТИЕ — {item.DisplayName}   " +
                $"+{credits} КР   +{rep} РЕП   " +
                $"{FoundCount}/{DiscoveryCount}";

            messageTimer = MessageSeconds;
        }

        public Vector3 GetDiscoveryPosition(int index)
        {
            return Valid(index)
                ? discoveries[index].Position
                : Vector3.zero;
        }

        public string GetDiscoveryName(int index)
        {
            return Valid(index)
                ? discoveries[index].DisplayName
                : string.Empty;
        }

        public bool IsFound(int index)
        {
            return Valid(index) &&
                discoveries[index].Found;
        }

        private bool Valid(int index)
        {
            return discoveries != null &&
                index >= 0 &&
                index < discoveries.Length;
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private sealed class Discovery
        {
            public string Id;
            public string DisplayName;
            public Vector3 Position;
            public bool Found;
        }
    }
}
