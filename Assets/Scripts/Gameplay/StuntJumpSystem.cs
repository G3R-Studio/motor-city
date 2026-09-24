using MotorCity.Localization;
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
        private const float RampRoadsideOffsetMeters = 4.25f;
        private const float RooftopMinimumHeightMeters = 3.5f;
        private const float RooftopMaximumHeightMeters = 32f;
        private const float RooftopCollectRadius = 4.5f;
        private const int RooftopRewardCredits = 750;

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
                    MotorCityLocalization.Text("world.north_jump"),
                    new Vector3(450f, 0f, 360f),
                    new Vector3(0f, 0f, 1f),
                    -1f,
                    12f,
                    22f,
                    34f),

                CreateJump(
                    "highway",
                    MotorCityLocalization.Text("world.highway"),
                    new Vector3(-450f, 0f, 360f),
                    new Vector3(0f, 0f, -1f),
                    1f,
                    18f,
                    32f,
                    48f),

                CreateJump(
                    "remote",
                    MotorCityLocalization.Text("world.remote_district"),
                    new Vector3(150f, 0f, 450f),
                    new Vector3(1f, 0f, 0f),
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

            Vector3 roadForward =
                rotation *
                Vector3.forward;

            roadForward.y = 0f;

            if (roadForward.sqrMagnitude < 0.001f)
                roadForward = preferredForward;

            roadForward.Normalize();

            Vector3 roadsideRight =
                rotation *
                Vector3.right;

            roadsideRight.y = 0f;

            if (roadsideRight.sqrMagnitude < 0.001f)
                roadsideRight = Vector3.right;

            roadsideRight.Normalize();

            float side =
                Mathf.Sign(
                    Mathf.Approximately(
                        roadsideSide,
                        0f)
                        ? 1f
                        : roadsideSide);

            Vector3 rampPosition =
                carPosition -
                Vector3.up * 1.10f +
                roadsideRight *
                RampRoadsideOffsetMeters *
                side;

            // Aim diagonally out of the road grid, GTA-style: the player gets
            // a visible run-up, hits the ramp at speed and lands on a roof /
            // raised platform instead of jumping down an arbitrary street.
            Vector3 desiredLaunchDirection =
                (
                    roadForward +
                    roadsideRight *
                    side *
                    0.72f
                ).normalized;

            bool hasLandingTarget =
                TryResolveRooftopTarget(
                    rampPosition,
                    desiredLaunchDirection,
                    out Vector3 landingPosition);

            Vector3 launchDirection =
                hasLandingTarget
                    ? Flat(
                        landingPosition -
                        rampPosition)
                    : desiredLaunchDirection;

            if (launchDirection.sqrMagnitude < 0.001f)
                launchDirection = roadForward;

            launchDirection.Normalize();

            Quaternion launchRotation =
                Quaternion.LookRotation(
                    launchDirection,
                    Vector3.up);

            string key =
                "MotorCity.StuntJump." +
                id;

            return new Jump
            {
                Id = id,
                DisplayName = displayName,
                Position =
                    rampPosition,
                Rotation = launchRotation,
                HasLandingTarget = hasLandingTarget,
                LandingPosition = landingPosition,
                RecommendedSpeedKph =
                    Mathf.Clamp(
                        54f +
                        HorizontalDistance(
                            rampPosition,
                            landingPosition) *
                        0.45f,
                        58f,
                        92f),
                RoofRewardCollected =
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        key + ".RoofReward",
                        0) != 0,
                BronzeDistance = bronzeDistance,
                SilverDistance = silverDistance,
                GoldDistance = goldDistance,
                BestDistance = Mathf.Max(
                    0f,
                    MotorCity.Persistence.MotorCitySaveService.GetFloat(
                        key + ".Best",
                        0f)),
                HighestMedal = Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
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

            UpdateRooftopRewards();

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
                Vector3 armedDelta =
                    carPosition -
                    Flat(armedJump.Position);

                if (armedDelta.sqrMagnitude >
                    DisarmRadius * DisarmRadius)
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
                        MotorCityLocalization.Format("stunt.airborne", activeJump.DisplayName);
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

                Vector3 jumpDelta =
                    carPosition -
                    Flat(jump.Position);

                if (jumpDelta.sqrMagnitude >
                    ArmRadius * ArmRadius)
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
                    MotorCityLocalization.Format(
                        "stunt.prompt",
                        jump.DisplayName,
                        jump.BronzeDistance,
                        jump.SilverDistance,
                        jump.GoldDistance);

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
                MotorCityLocalization.Format(
                    "stunt.live",
                    activeJump.DisplayName,
                    liveDistance,
                    airborneSeconds);

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

                MotorCity.Persistence.MotorCitySaveService.SetFloat(
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

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    jump.KeyPrefix + ".Medal",
                    jump.HighestMedal);

                wallet?.AddCredits(
                    credits);

                reputation?.AddReputation(
                    rep);
            }

            MotorCity.Persistence.MotorCitySaveService.Save();

            string recordText =
                newBest
                    ? MotorCityLocalization.Text("challenge.new_record")
                    : jump.BestDistance > 0f
                        ? MotorCityLocalization.Format("challenge.best_distance", jump.BestDistance)
                        : string.Empty;

            string rewardText =
                credits > 0 ||
                rep > 0
                    ? MotorCityLocalization.Format("challenge.reward", credits, rep)
                    : string.Empty;

            StatusText =
                MotorCityLocalization.Format(
                    "stunt.result",
                    jump.DisplayName,
                    distance,
                    airTime,
                    MedalName(medal),
                    recordText,
                    rewardText);

            messageTimer =
                MessageSeconds;

            if (medal > 0)
            {
                activityManager?.ReportCompletion(
                    "stuntjump");
            }
        }

        private void UpdateRooftopRewards()
        {
            if (car.GroundedWheels < 2)
                return;

            Vector3 carPosition =
                car.transform.position;

            for (int i = 0;
                 i < jumps.Length;
                 i++)
            {
                Jump jump = jumps[i];

                if (!jump.HasLandingTarget ||
                    jump.RoofRewardCollected)
                {
                    continue;
                }

                float horizontalDistance =
                    HorizontalDistance(
                        carPosition,
                        jump.LandingPosition);

                float heightDifference =
                    Mathf.Abs(
                        carPosition.y -
                        jump.LandingPosition.y);

                if (horizontalDistance >
                        RooftopCollectRadius ||
                    heightDifference > 3.8f)
                {
                    continue;
                }

                jump.RoofRewardCollected =
                    true;

                if (jump.RewardMarker != null)
                {
                    jump.RewardMarker.SetActive(
                        false);
                }

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    jump.KeyPrefix + ".RoofReward",
                    1);

                wallet?.AddCredits(
                    RooftopRewardCredits);

                MotorCity.Persistence.MotorCitySaveService.Save();

                StatusText =
                    jump.DisplayName +
                    ": +" +
                    RooftopRewardCredits +
                    " КР за метку на крыше";

                messageTimer =
                    MessageSeconds;

                activityManager?.ReportCompletion(
                    "stuntjump");

                break;
            }
        }

        private static bool TryResolveRooftopTarget(
            Vector3 rampPosition,
            Vector3 desiredDirection,
            out Vector3 landingPosition)
        {
            landingPosition =
                rampPosition +
                desiredDirection *
                34f;

            desiredDirection.y = 0f;

            if (desiredDirection.sqrMagnitude <
                0.001f)
            {
                return false;
            }

            desiredDirection.Normalize();

            Vector3 lateral =
                Vector3.Cross(
                    Vector3.up,
                    desiredDirection);

            float bestScore =
                float.PositiveInfinity;

            bool found =
                false;

            float[] distances =
            {
                26f,
                32f,
                38f,
                44f,
                50f,
                58f
            };

            float[] lateralOffsets =
            {
                0f,
                -6f,
                6f,
                -12f,
                12f
            };

            for (int d = 0;
                 d < distances.Length;
                 d++)
            {
                for (int l = 0;
                     l < lateralOffsets.Length;
                     l++)
                {
                    Vector3 sample =
                        rampPosition +
                        desiredDirection *
                        distances[d] +
                        lateral *
                        lateralOffsets[l];

                    Vector3 origin =
                        new Vector3(
                            sample.x,
                            rampPosition.y + 120f,
                            sample.z);

                    if (!Physics.Raycast(
                            origin,
                            Vector3.down,
                            out RaycastHit hit,
                            180f,
                            Physics.DefaultRaycastLayers,
                            QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    float height =
                        hit.point.y -
                        rampPosition.y;

                    if (height <
                            RooftopMinimumHeightMeters ||
                        height >
                            RooftopMaximumHeightMeters ||
                        hit.normal.y < 0.72f)
                    {
                        continue;
                    }

                    float flatDistance =
                        HorizontalDistance(
                            rampPosition,
                            hit.point);

                    if (flatDistance < 22f)
                        continue;

                    // Prefer a medium-length, broad-looking landing surface.
                    // Collider bounds are only a hint because FCG can use
                    // combined mesh colliders for whole blocks.
                    Bounds bounds =
                        hit.collider.bounds;

                    float surfaceBonus =
                        Mathf.Min(
                            bounds.size.x,
                            bounds.size.z);

                    float score =
                        Mathf.Abs(
                            flatDistance -
                            40f) +
                        Mathf.Abs(
                            height -
                            11f) *
                        0.7f -
                        Mathf.Min(
                            surfaceBonus,
                            18f) *
                        0.12f;

                    if (score >= bestScore)
                        continue;

                    bestScore =
                        score;

                    landingPosition =
                        hit.point +
                        Vector3.up *
                        1.15f;

                    found =
                        true;
                }
            }

            return found;
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
                3 => MotorCityLocalization.Text("medal.gold"),
                2 => MotorCityLocalization.Text("medal.silver"),
                1 => MotorCityLocalization.Text("medal.bronze"),
                _ => MotorCityLocalization.Text("medal.none")
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

        public bool HasLandingTarget(
            int index)
        {
            return Valid(index) &&
                jumps[index].HasLandingTarget;
        }

        public Vector3 GetLandingPosition(
            int index)
        {
            return Valid(index)
                ? jumps[index].LandingPosition
                : Vector3.zero;
        }

        public float GetRecommendedSpeedKph(
            int index)
        {
            return Valid(index)
                ? jumps[index].RecommendedSpeedKph
                : 0f;
        }

        public bool IsRoofRewardCollected(
            int index)
        {
            return Valid(index) &&
                jumps[index].RoofRewardCollected;
        }

        public void RegisterRewardMarker(
            int index,
            GameObject marker)
        {
            if (!Valid(index) ||
                marker == null)
            {
                return;
            }

            Jump jump =
                jumps[index];

            jump.RewardMarker =
                marker;

            marker.SetActive(
                jump.HasLandingTarget &&
                !jump.RoofRewardCollected);
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
            public bool HasLandingTarget;
            public Vector3 LandingPosition;
            public float RecommendedSpeedKph;
            public bool RoofRewardCollected;
            public GameObject RewardMarker;
            public float BronzeDistance;
            public float SilverDistance;
            public float GoldDistance;
            public float BestDistance;
            public int HighestMedal;
            public string KeyPrefix;
        }
    }
}
