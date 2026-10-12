using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DriftSpotSystem : MonoBehaviour
    {
        private const float EnterRadius = 44f;
        private const float RearmRadius = 52f;
        private const float MessageSeconds = 3.2f;

        private ArcadeCarController car;
        private DriftTracker drift;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private ActivityManager activityManager;
        private Spot[] spots;
        private Spot activeSpot;
        private int scoreAtEntry;
        private bool spotHasDrifted;
        private int lastLiveScore = int.MinValue;
        private float messageTimer;

        public bool ShowMessage => messageTimer > 0f;
        public string StatusText { get; private set; }
        private const int BronzeCreditReward = 140;
        private const int SilverCreditReward = 210;
        private const int GoldCreditReward = 350;
        private const int BronzeReputationReward = 20;
        private const int SilverReputationReward = 35;
        private const int GoldReputationReward = 55;

        public int GetCatalogMedalCreditReward(int tier)
        {
            return tier == 1 ? BronzeCreditReward :
                tier == 2 ? SilverCreditReward :
                tier == 3 ? GoldCreditReward : 0;
        }

        public int GetCatalogMedalReputationReward(int tier)
        {
            return tier == 1 ? BronzeReputationReward :
                tier == 2 ? SilverReputationReward :
                tier == 3 ? GoldReputationReward : 0;
        }

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
                    MotorCityLocalization.Text("world.west_turn"),
                    new Vector3(-450f, 0f, 150f),
                    650,
                    1200,
                    2000),

                CreateSpot(
                    "north_grid",
                    MotorCityLocalization.Text("world.north_quarter"),
                    new Vector3(150f, 0f, 450f),
                    800,
                    1500,
                    2400),

                CreateSpot(
                    "remote_corner",
                    MotorCityLocalization.Text("world.central_avenue"),
                    new Vector3(450f, 0f, -100f),
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
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        $"MotorCity.DriftSpot.{id}.Medal",
                        0),
                    0,
                    3),
                BestScore = Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
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

            Vector3 carPosition = Flat(car.transform.position);
            // Re-arm only after leaving the original start trigger.
            foreach (Spot spot in spots)
            {
                if (!spot.Armed &&
                    (carPosition - Flat(spot.Position)).sqrMagnitude >
                    RearmRadius * RearmRadius)
                    spot.Armed = true;
            }

            if (activeSpot != null)
            {
                int liveScore = Mathf.Max(0, drift.TotalScore - scoreAtEntry);
                if (drift.IsDrifting && liveScore > 0)
                    spotHasDrifted = true;

                if (spotHasDrifted && !drift.ComboInProgress)
                {
                    // Bank the result at the end of the drift combination,
                    // regardless of distance from the original zone.
                    FinishSpot(activeSpot, liveScore);
                    activeSpot = null;
                    lastLiveScore = int.MinValue;
                    spotHasDrifted = false;
                    return;
                }

                if (!spotHasDrifted &&
                    (carPosition - Flat(activeSpot.Position)).sqrMagnitude >
                    RearmRadius * RearmRadius)
                {
                    activeSpot = null;
                    return;
                }

                if (liveScore != lastLiveScore)
                {
                    lastLiveScore = liveScore;
                    StatusText = MotorCityLocalization.Format(
                        "driftspot.live", activeSpot.DisplayName, liveScore,
                        activeSpot.BronzeScore, activeSpot.SilverScore,
                        activeSpot.GoldScore);
                }
                return;
            }

            foreach (Spot spot in spots)
            {
                if (!spot.Armed ||
                    (carPosition - Flat(spot.Position)).sqrMagnitude >
                    EnterRadius * EnterRadius)
                    continue;

                spot.Armed = false;
                activeSpot = spot;
                scoreAtEntry = drift.TotalScore;
                spotHasDrifted = false;
                lastLiveScore = int.MinValue;
                StatusText = MotorCityLocalization.Format(
                    "driftspot.prompt", spot.DisplayName,
                    spot.BronzeScore, spot.SilverScore, spot.GoldScore);
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
                MotorCity.Persistence.MotorCitySaveService.SetInt(
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
                            credits += BronzeCreditReward;
                            rep += BronzeReputationReward;
                            break;
                        case 2:
                            credits += SilverCreditReward;
                            rep += SilverReputationReward;
                            break;
                        case 3:
                            credits += GoldCreditReward;
                            rep += GoldReputationReward;
                            break;
                    }
                }

                spot.HighestMedal = medal;

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    $"MotorCity.DriftSpot.{spot.Id}.Medal",
                    spot.HighestMedal);

                wallet?.AddCredits(credits);
                reputation?.AddReputation(rep);
            }

            MotorCity.Persistence.MotorCitySaveService.Save();

            string record =
                newBest
                    ? MotorCityLocalization.Text("challenge.new_record")
                    : spot.BestScore > 0
                        ? MotorCityLocalization.Format("challenge.best_score", spot.BestScore)
                        : string.Empty;

            string reward =
                credits > 0 || rep > 0
                    ? MotorCityLocalization.Format("challenge.reward", credits, rep)
                    : string.Empty;

            StatusText =
                MotorCityLocalization.Format(
                    "driftspot.result",
                    spot.DisplayName,
                    score,
                    MedalName(medal),
                    record,
                    reward);

            messageTimer = MessageSeconds;

            if (medal > 0)
            {
                activityManager?.ReportCompletion(
                    "driftspot");
            }
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
                3 => MotorCityLocalization.Text("medal.gold"),
                2 => MotorCityLocalization.Text("medal.silver"),
                1 => MotorCityLocalization.Text("medal.bronze"),
                _ => MotorCityLocalization.Text("medal.none")
            };
        }

#if UNITY_EDITOR || DEBUG
        public void CompleteGoldForTesting(
            int index)
        {
            if (!Valid(
                    index))
            {
                return;
            }

            Spot spot =
                spots[index];

            FinishSpot(
                spot,
                spot.GoldScore +
                250);
        }

        public void ResetForTesting()
        {
            if (spots == null)
                return;

            foreach (Spot spot in
                     spots)
            {
                if (spot == null)
                    continue;

                spot.BestScore =
                    0;

                spot.HighestMedal =
                    0;

                MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                    $"MotorCity.DriftSpot.{spot.Id}.Best");

                MotorCity.Persistence.MotorCitySaveService.DeleteKey(
                    $"MotorCity.DriftSpot.{spot.Id}.Medal");
            }

            MotorCity.Persistence.MotorCitySaveService.Save();

            activeSpot =
                null;

            StatusText =
                string.Empty;

            messageTimer =
                0f;
        }

#endif

        public Vector3 GetSpotPosition(int index)
        {
            return Valid(index)
                ? spots[index].Position
                : Vector3.zero;
        }

        public int GetSpotHighestMedal(int index)
        {
            return Valid(index) ? spots[index].HighestMedal : 0;
        }

        public int GetSpotBronzeTarget(int index)
        {
            return Valid(index) ? spots[index].BronzeScore : 0;
        }

        public int GetSpotSilverTarget(int index)
        {
            return Valid(index) ? spots[index].SilverScore : 0;
        }

        public int GetSpotGoldTarget(int index)
        {
            return Valid(index) ? spots[index].GoldScore : 0;
        }

        public int GetSpotBestScore(int index)
        {
            return Valid(index) ? spots[index].BestScore : 0;
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
            public bool Armed = true;
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
