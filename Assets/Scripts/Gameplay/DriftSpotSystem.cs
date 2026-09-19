using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DriftSpotSystem : MonoBehaviour
    {
        private const float EnterRadius = 44f;
        private const float ExitRadius = 52f;
        private const float MessageSeconds = 3.2f;

        private ArcadeCarController car;
        private DriftTracker drift;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private ActivityManager activityManager;
        private Spot[] spots;
        private Spot activeSpot;
        private int scoreAtEntry;
        private float messageTimer;

        public bool ShowMessage => messageTimer > 0f;
        public string StatusText { get; private set; }
        public int SpotCount => spots?.Length ?? 0;

        public void Initialize(
            ArcadeCarController targetCar,
            DriftTracker driftTracker,
            PlayerWallet targetWallet,
            PlayerReputation targetReputation,
            ActivityManager manager)
        {
            car = targetCar;
            drift = driftTracker;
            wallet = targetWallet;
            reputation = targetReputation;
            activityManager = manager;

            spots = new[]
            {
                CreateSpot(
                    "west_roundabout",
                    "ЗАПАДНЫЙ ПОВОРОТ",
                    new Vector3(-450f, 0f, 150f),
                    650,
                    1200,
                    2000),

                CreateSpot(
                    "north_grid",
                    "СЕВЕРНЫЙ КВАРТАЛ",
                    new Vector3(150f, 0f, 450f),
                    800,
                    1500,
                    2400),

                CreateSpot(
                    "remote_corner",
                    "ДАЛЬНИЙ УГОЛ",
                    new Vector3(-630f, 0f, -1832f),
                    900,
                    1700,
                    2700)
            };
        }

        private Spot CreateSpot(
            string id,
            string name,
            Vector3 preferred,
            int bronze,
            int silver,
            int gold)
        {
            CityAssetRuntimeInstaller.ResolveNearestRoadResetPose(
                preferred,
                Vector3.forward,
                out Vector3 carPosition,
                out _);

            return new Spot
            {
                Id = id,
                DisplayName = name,
                Position = carPosition - Vector3.up * 1.10f,
                BronzeScore = bronze,
                SilverScore = silver,
                GoldScore = gold,
                HighestMedal = Mathf.Clamp(
                    PlayerPrefs.GetInt(
                        $"MotorCity.DriftSpot.{id}.Medal",
                        0),
                    0,
                    3),
                BestScore = Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        $"MotorCity.DriftSpot.{id}.Best",
                        0))
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
                drift == null ||
                spots == null)
                return;

            if (activityManager != null &&
                activityManager.IsBusy)
            {
                activeSpot = null;
                return;
            }

            Vector3 carPosition = Flat(
                car.transform.position);

            if (activeSpot != null)
            {
                float distance = Vector3.Distance(
                    carPosition,
                    Flat(activeSpot.Position));

                int liveScore = Mathf.Max(
                    0,
                    drift.TotalScore - scoreAtEntry);

                if (distance <= ExitRadius)
                {
                    StatusText =
                        $"DRIFT SPOT — {activeSpot.DisplayName}   " +
                        $"{liveScore:N0}   " +
                        $"Б {activeSpot.BronzeScore:N0}  " +
                        $"С {activeSpot.SilverScore:N0}  " +
                        $"З {activeSpot.GoldScore:N0}";
                    return;
                }

                FinishSpot(
                    activeSpot,
                    liveScore);

                activeSpot = null;
                return;
            }

            for (int i = 0; i < spots.Length; i++)
            {
                Spot spot = spots[i];

                float distance = Vector3.Distance(
                    carPosition,
                    Flat(spot.Position));

                if (distance > EnterRadius)
                    continue;

                activeSpot = spot;
                scoreAtEntry = drift.TotalScore;
                StatusText =
                    $"DRIFT SPOT — {spot.DisplayName}   " +
                    $"Б {spot.BronzeScore:N0}  " +
                    $"С {spot.SilverScore:N0}  " +
                    $"З {spot.GoldScore:N0}";
                return;
            }
        }

        private void FinishSpot(
            Spot spot,
            int score)
        {
            bool newBest =
                score > spot.BestScore;

            if (newBest)
            {
                spot.BestScore = score;
                PlayerPrefs.SetInt(
                    $"MotorCity.DriftSpot.{spot.Id}.Best",
                    spot.BestScore);
            }

            int medal = ResolveMedal(
                spot,
                score);

            int credits = 0;
            int rep = 0;

            if (medal > spot.HighestMedal)
            {
                for (int tier =
                         spot.HighestMedal + 1;
                     tier <= medal;
                     tier++)
                {
                    switch (tier)
                    {
                        case 1:
                            credits += 140;
                            rep += 20;
                            break;
                        case 2:
                            credits += 210;
                            rep += 35;
                            break;
                        case 3:
                            credits += 350;
                            rep += 55;
                            break;
                    }
                }

                spot.HighestMedal = medal;

                PlayerPrefs.SetInt(
                    $"MotorCity.DriftSpot.{spot.Id}.Medal",
                    spot.HighestMedal);

                wallet?.AddCredits(credits);
                reputation?.AddReputation(rep);
            }

            PlayerPrefs.Save();

            string record =
                newBest
                    ? "   НОВЫЙ РЕКОРД"
                    : spot.BestScore > 0
                        ? $"   РЕК {spot.BestScore:N0}"
                        : string.Empty;

            string reward =
                credits > 0 || rep > 0
                    ? $"   +{credits:N0} КР   +{rep:N0} REP"
                    : string.Empty;

            StatusText =
                $"DRIFT SPOT — {spot.DisplayName}   " +
                $"{score:N0}   {MedalName(medal)}" +
                record +
                reward;

            messageTimer = MessageSeconds;
        }

        private static int ResolveMedal(
            Spot spot,
            int score)
        {
            if (score >= spot.GoldScore)
                return 3;
            if (score >= spot.SilverScore)
                return 2;
            if (score >= spot.BronzeScore)
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

        public Vector3 GetSpotPosition(int index)
        {
            return Valid(index)
                ? spots[index].Position
                : Vector3.zero;
        }

        public string GetSpotName(int index)
        {
            return Valid(index)
                ? spots[index].DisplayName
                : string.Empty;
        }

        private bool Valid(int index)
        {
            return spots != null &&
                index >= 0 &&
                index < spots.Length;
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private sealed class Spot
        {
            public string Id;
            public string DisplayName;
            public Vector3 Position;
            public int BronzeScore;
            public int SilverScore;
            public int GoldScore;
            public int BestScore;
            public int HighestMedal;
        }
    }
}
