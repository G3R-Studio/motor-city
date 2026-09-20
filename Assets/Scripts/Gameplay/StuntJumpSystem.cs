using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class StuntJumpSystem : MonoBehaviour
    {
        private const float ArmRadius = 16f;
        private const float DisarmRadius = 34f;
        private const float MinimumArmSpeedKph = 38f;
        private const float MinimumAirTime = 0.24f;
        private const float MessageSeconds = 3.5f;
        private const float RoadsideOffsetMeters = 9.5f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private ActivityManager activityManager;
        private Jump[] jumps;

        private Jump armedJump;
        private Jump activeJump;
        private Vector3 takeoffPosition;
        private float airborneSeconds;
        private float messageTimer;

        public bool ShowMessage => messageTimer > 0f;
        public bool IsAttemptActive => activeJump != null;
        public string StatusText { get; private set; }
        public int JumpCount => jumps?.Length ?? 0;

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

            jumps = new[]
            {
                CreateJump(
                    "main_north",
                    "СЕВЕРНЫЙ ТРАМПЛИН",
                    new Vector3(450f, 0f, 360f),
                    new Vector3(0f, 0f, 1f),
                    1f,
                    12f,
                    22f,
                    34f),

                CreateJump(
                    "highway",
                    "ШОССЕ",
                    new Vector3(-300f, 0f, -1400f),
                    new Vector3(0f, 0f, -1f),
                    -1f,
                    18f,
                    32f,
                    48f),

                CreateJump(
                    "remote",
                    "ДАЛЬНИЙ РАЙОН",
                    new Vector3(-300f, 0f, -2010f),
                    new Vector3(0f, 0f, 1f),
                    1f,
                    15f,
                    28f,
                    42f)
            };
        }

        private Jump CreateJump(
            string id,
            string displayName,
            Vector3 preferred,
            Vector3 preferredForward,
            float roadsideSide,
            float bronzeDistance,
            float silverDistance,
            float goldDistance)
        {
            CityAssetRuntimeInstaller.ResolveNearestRoadResetPose(
                preferred,
                preferredForward,
                out Vector3 carPosition,
                out Quaternion rotation);

            Vector3 roadsideRight =
                rotation *
                Vector3.right;

            roadsideRight.y = 0f;

            if (roadsideRight.sqrMagnitude < 0.001f)
                roadsideRight = Vector3.right;

            roadsideRight.Normalize();

            Vector3 roadsidePosition =
                carPosition -
                Vector3.up * 1.10f +
                roadsideRight *
                RoadsideOffsetMeters *
                Mathf.Sign(
                    Mathf.Approximately(
                        roadsideSide,
                        0f)
                        ? 1f
                        : roadsideSide);

            string key =
                "MotorCity.StuntJump." +
                id;

            return new Jump
            {
                Id = id,
                DisplayName = displayName,
                Position =
                    roadsidePosition,
                Rotation = rotation,
                BronzeDistance = bronzeDistance,
                SilverDistance = silverDistance,
                GoldDistance = goldDistance,
                BestDistance = Mathf.Max(
                    0f,
                    PlayerPrefs.GetFloat(
                        key + ".Best",
                        0f)),
                HighestMedal = Mathf.Clamp(
                    PlayerPrefs.GetInt(
                        key + ".Medal",
                        0),
                    0,
                    3),
                KeyPrefix = key
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
                jumps == null)
                return;

            if (activityManager != null &&
                activityManager.IsBusy)
            {
                CancelAttempt();
                return;
            }

            if (activeJump != null)
            {
                UpdateActiveJump();
                return;
            }

            Vector3 carPosition =
                Flat(
                    car.transform.position);

            if (armedJump != null)
            {
                float distance =
                    Vector3.Distance(
                        carPosition,
                        Flat(armedJump.Position));

                if (distance >
                    DisarmRadius)
                {
                    armedJump = null;
                    return;
                }

                if (car.GroundedWheels <= 1)
                {
                    activeJump = armedJump;
                    armedJump = null;
                    takeoffPosition =
                        car.transform.position;
                    airborneSeconds = 0f;

                    StatusText =
                        $"ПРЫЖОК — {activeJump.DisplayName}   В ВОЗДУХЕ";
                }

                return;
            }

            if (car.SpeedKph <
                    MinimumArmSpeedKph ||
                car.GroundedWheels < 2)
                return;

            for (int i = 0;
                 i < jumps.Length;
                 i++)
            {
                Jump jump = jumps[i];

                float distance =
                    Vector3.Distance(
                        carPosition,
                        Flat(jump.Position));

                if (distance >
                    ArmRadius)
                    continue;

                Vector3 forward =
                    jump.Rotation *
                    Vector3.forward;

                Vector3 toCar =
                    carPosition -
                    Flat(jump.Position);

                float approach =
                    Vector3.Dot(
                        Flat(car.transform.forward),
                        Flat(forward));

                if (approach < 0.35f)
                    continue;

                armedJump = jump;

                StatusText =
                    $"ТРАМПЛИН — {jump.DisplayName}   " +
                    $"Б {jump.BronzeDistance:0}м  " +
                    $"С {jump.SilverDistance:0}м  " +
                    $"З {jump.GoldDistance:0}м";

                return;
            }
        }

        private void UpdateActiveJump()
        {
            airborneSeconds +=
                Time.deltaTime;

            float liveDistance =
                HorizontalDistance(
                    takeoffPosition,
                    car.transform.position);

            StatusText =
                $"ПРЫЖОК — {activeJump.DisplayName}   " +
                $"{liveDistance:0.0} М   " +
                $"{airborneSeconds:0.00} С";

            if (airborneSeconds <
                    MinimumAirTime ||
                car.GroundedWheels < 2)
                return;

            FinishJump(
                activeJump,
                liveDistance,
                airborneSeconds);

            activeJump = null;
        }

        private void FinishJump(
            Jump jump,
            float distance,
            float airTime)
        {
            bool newBest =
                distance >
                jump.BestDistance + 0.05f;

            if (newBest)
            {
                jump.BestDistance =
                    distance;

                PlayerPrefs.SetFloat(
                    jump.KeyPrefix + ".Best",
                    jump.BestDistance);
            }

            int medal =
                ResolveMedal(
                    jump,
                    distance);

            int credits = 0;
            int rep = 0;

            if (medal >
                jump.HighestMedal)
            {
                for (int tier =
                         jump.HighestMedal + 1;
                     tier <= medal;
                     tier++)
                {
                    switch (tier)
                    {
                        case 1:
                            credits += 180;
                            rep += 25;
                            break;

                        case 2:
                            credits += 260;
                            rep += 40;
                            break;

                        case 3:
                            credits += 420;
                            rep += 65;
                            break;
                    }
                }

                jump.HighestMedal =
                    medal;

                PlayerPrefs.SetInt(
                    jump.KeyPrefix + ".Medal",
                    jump.HighestMedal);

                wallet?.AddCredits(
                    credits);

                reputation?.AddReputation(
                    rep);
            }

            PlayerPrefs.Save();

            string recordText =
                newBest
                    ? "   НОВЫЙ РЕКОРД"
                    : jump.BestDistance > 0f
                        ? $"   РЕК {jump.BestDistance:0.0} М"
                        : string.Empty;

            string rewardText =
                credits > 0 ||
                rep > 0
                    ? $"   +{credits:N0} КР   +{rep:N0} REP"
                    : string.Empty;

            StatusText =
                $"ПРЫЖОК — {jump.DisplayName}   " +
                $"{distance:0.0} М   {airTime:0.00} С   " +
                $"{MedalName(medal)}" +
                recordText +
                rewardText;

            messageTimer =
                MessageSeconds;
        }

        private void CancelAttempt()
        {
            armedJump = null;
            activeJump = null;
            airborneSeconds = 0f;
        }

        private static int ResolveMedal(
            Jump jump,
            float distance)
        {
            if (distance >=
                jump.GoldDistance)
                return 3;

            if (distance >=
                jump.SilverDistance)
                return 2;

            if (distance >=
                jump.BronzeDistance)
                return 1;

            return 0;
        }

        private static string MedalName(
            int medal)
        {
            return medal switch
            {
                3 => "ЗОЛОТО",
                2 => "СЕРЕБРО",
                1 => "БРОНЗА",
                _ => "БЕЗ МЕДАЛИ"
            };
        }

        public Vector3 GetJumpPosition(
            int index)
        {
            return Valid(index)
                ? jumps[index].Position
                : Vector3.zero;
        }

        public Quaternion GetJumpRotation(
            int index)
        {
            return Valid(index)
                ? jumps[index].Rotation
                : Quaternion.identity;
        }

        public string GetJumpName(
            int index)
        {
            return Valid(index)
                ? jumps[index].DisplayName
                : string.Empty;
        }

        public float GetBestDistance(
            int index)
        {
            return Valid(index)
                ? jumps[index].BestDistance
                : 0f;
        }

        private bool Valid(
            int index)
        {
            return jumps != null &&
                index >= 0 &&
                index < jumps.Length;
        }

        private static float HorizontalDistance(
            Vector3 a,
            Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(
                a,
                b);
        }

        private static Vector3 Flat(
            Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private sealed class Jump
        {
            public string Id;
            public string DisplayName;
            public Vector3 Position;
            public Quaternion Rotation;
            public float BronzeDistance;
            public float SilverDistance;
            public float GoldDistance;
            public float BestDistance;
            public int HighestMedal;
            public string KeyPrefix;
        }
    }
}
