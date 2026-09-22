using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class SpeedTrapSystem : MonoBehaviour
    {
        private const float TriggerRadius = 8f;
        private const float MessageSeconds = 3.2f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private ActivityManager activityManager;
        private Trap[] traps;
        private float messageTimer;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public int TrapCount =>
            traps?.Length ?? 0;

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

            traps =
                new[]
                {
                    CreateTrap(
                        "main_avenue",
                        MotorCityLocalization.Text("world.central_avenue"),
                        new Vector3(
                            150f,
                            0f,
                            -100f),
                        115f,
                        150f,
                        185f),

                    CreateTrap(
                        "highway",
                        MotorCityLocalization.Text("world.main_north"),
                        new Vector3(
                            450f,
                            0f,
                            360f),
                        150f,
                        190f,
                        225f),

                    CreateTrap(
                        "remote_district",
                        MotorCityLocalization.Text("world.main_west"),
                        new Vector3(
                            -450f,
                            0f,
                            -100f),
                        125f,
                        165f,
                        200f)
                };
        }

        private Trap CreateTrap(
            string id,
            string displayName,
            Vector3 preferredPosition,
            float bronzeSpeed,
            float silverSpeed,
            float goldSpeed)
        {
            CityAssetRuntimeInstaller.ResolveNearestRoadResetPose(
                preferredPosition,
                Vector3.forward,
                out Vector3 carPosition,
                out Quaternion rotation);

            Vector3 roadPosition =
                carPosition -
                Vector3.up * 1.10f;

            string keyPrefix =
                "MotorCity.SpeedTrap." +
                id;

            return new Trap
            {
                Id = id,
                DisplayName = displayName,
                Position = roadPosition,
                Rotation = rotation,
                BronzeSpeed = bronzeSpeed,
                SilverSpeed = silverSpeed,
                GoldSpeed = goldSpeed,
                BestSpeed =
                    Mathf.Max(
                        0f,
                        MotorCity.Persistence.MotorCitySaveService.GetFloat(
                            keyPrefix + ".Best",
                            0f)),
                HighestMedal =
                    Mathf.Clamp(
                        MotorCity.Persistence.MotorCitySaveService.GetInt(
                            keyPrefix + ".Medal",
                            0),
                        0,
                        3),
                KeyPrefix = keyPrefix
            };
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.deltaTime);
            }

            if (car == null ||
                traps == null)
                return;

            bool blocked =
                activityManager != null &&
                activityManager.IsBusy;

            Vector3 carPosition =
                Flat(
                    car.transform.position);

            for (int i = 0;
                 i < traps.Length;
                 i++)
            {
                Trap trap =
                    traps[i];

                bool inside =
                    Vector3.Distance(
                        carPosition,
                        Flat(trap.Position)) <=
                    TriggerRadius;

                if (blocked)
                {
                    trap.WasInside =
                        inside;
                    continue;
                }

                if (inside &&
                    !trap.WasInside)
                {
                    EvaluateTrap(
                        trap);
                }

                trap.WasInside =
                    inside;
            }
        }

        private void EvaluateTrap(
            Trap trap)
        {
            float speed =
                car.SpeedKph;

            if (speed < 25f)
                return;

            bool newBest =
                speed >
                trap.BestSpeed + 0.1f;

            if (newBest)
            {
                trap.BestSpeed =
                    speed;

                MotorCity.Persistence.MotorCitySaveService.SetFloat(
                    trap.KeyPrefix + ".Best",
                    trap.BestSpeed);
            }

            int medal =
                ResolveMedal(
                    trap,
                    speed);

            int credits = 0;
            int rep = 0;

            if (medal >
                trap.HighestMedal)
            {
                for (int tier =
                         trap.HighestMedal + 1;
                     tier <= medal;
                     tier++)
                {
                    switch (tier)
                    {
                        case 1:
                            credits += 120;
                            rep += 20;
                            break;

                        case 2:
                            credits += 180;
                            rep += 30;
                            break;

                        case 3:
                            credits += 300;
                            rep += 50;
                            break;
                    }
                }

                trap.HighestMedal =
                    medal;

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    trap.KeyPrefix + ".Medal",
                    trap.HighestMedal);

                wallet?.AddCredits(
                    credits);

                reputation?.AddReputation(
                    rep);
            }

            MotorCity.Persistence.MotorCitySaveService.Save();

            string medalText =
                MedalName(
                    medal);

            string rewardText =
                credits > 0 ||
                rep > 0
                    ? MotorCityLocalization.Format("challenge.reward", credits, rep)
                    : string.Empty;

            string recordText =
                newBest
                    ? MotorCityLocalization.Text("challenge.new_record")
                    : trap.BestSpeed > 0f
                        ? MotorCityLocalization.Format("challenge.best_speed", trap.BestSpeed, MotorCityLocalization.Text("common.kmh"))
                        : string.Empty;

            StatusText =
                MotorCityLocalization.Format(
                    "speedtrap.status",
                    trap.DisplayName,
                    speed,
                    MotorCityLocalization.Text("common.kmh"),
                    medalText,
                    recordText,
                    rewardText);

            messageTimer =
                MessageSeconds;
        }

        private static int ResolveMedal(
            Trap trap,
            float speed)
        {
            if (speed >=
                trap.GoldSpeed)
                return 3;

            if (speed >=
                trap.SilverSpeed)
                return 2;

            if (speed >=
                trap.BronzeSpeed)
                return 1;

            return 0;
        }

        private static string MedalName(
            int medal)
        {
            return medal switch
            {
                3 => MotorCityLocalization.Text("medal.gold"),
                2 => MotorCityLocalization.Text("medal.silver"),
                1 => MotorCityLocalization.Text("medal.bronze"),
                _ => MotorCityLocalization.Text("medal.none")
            };
        }

        public Vector3 GetTrapPosition(
            int index)
        {
            return Valid(index)
                ? traps[index].Position
                : Vector3.zero;
        }

        public Quaternion GetTrapRotation(
            int index)
        {
            return Valid(index)
                ? traps[index].Rotation
                : Quaternion.identity;
        }

        public string GetTrapName(
            int index)
        {
            return Valid(index)
                ? traps[index].DisplayName
                : string.Empty;
        }

        public string GetTrapGoalText(
            int index)
        {
            if (!Valid(index))
                return string.Empty;

            Trap trap =
                traps[index];

            return
                MotorCityLocalization.Format(
                    "challenge.short_goals",
                    trap.BronzeSpeed.ToString("0"),
                    trap.SilverSpeed.ToString("0"),
                    trap.GoldSpeed.ToString("0"));
        }

        public float GetBestSpeed(
            int index)
        {
            return Valid(index)
                ? traps[index].BestSpeed
                : 0f;
        }

        private bool Valid(
            int index)
        {
            return traps != null &&
                index >= 0 &&
                index < traps.Length;
        }

        private static Vector3 Flat(
            Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private sealed class Trap
        {
            public string Id;
            public string DisplayName;
            public Vector3 Position;
            public Quaternion Rotation;
            public float BronzeSpeed;
            public float SilverSpeed;
            public float GoldSpeed;
            public float BestSpeed;
            public int HighestMedal;
            public string KeyPrefix;
            public bool WasInside;
        }
    }
}
