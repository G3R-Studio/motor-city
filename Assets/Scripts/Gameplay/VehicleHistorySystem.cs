using MotorCity.Localization;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class VehicleHistorySystem : MonoBehaviour
    {
        private const float SaveIntervalSeconds = 10f;
        private const float MaximumTrackedStepMeters = 80f;

        private ArcadeCarController car;
        private VehicleRosterSystem roster;
        private ActivityManager activityManager;

        private string currentVehicleId;
        private float distanceMeters;
        private int victories;
        private int earnedCredits;
        private int racingVictories;
        private int driftVictories;
        private int deliveryVictories;

        private Vector3 lastPosition;
        private float saveTimer;
        private bool positionReady;
        private bool dirty;

        public float DistanceKm => distanceMeters / 1000f;
        public int Victories => victories;
        public int EarnedCredits => earnedCredits;
        public int RacingVictories => racingVictories;
        public int DriftVictories => driftVictories;
        public int DeliveryVictories => deliveryVictories;

        public string PassportSummary =>
            MotorCityLocalization.Format(
                "history.passport_summary",
                DistanceKm,
                victories,
                earnedCredits,
                LegacyStatus);

        public string PassportDisciplines =>
            MotorCityLocalization.Format(
                "history.passport_disciplines",
                racingVictories,
                driftVictories,
                deliveryVictories,
                FavoriteDiscipline);

        public string GarageLine
        {
            get
            {
                return
                    MotorCityLocalization.Format(
                        "history.garage_line",
                        DistanceKm,
                        victories,
                        earnedCredits,
                        LegacyStatus,
                        FavoriteDiscipline);
            }
        }

        public void Initialize(
            ArcadeCarController targetCar,
            VehicleRosterSystem vehicleRoster,
            ActivityManager manager)
        {
            car = targetCar;
            roster = vehicleRoster;
            activityManager = manager;

            currentVehicleId =
                roster == null
                    ? "street"
                    : roster.SelectedId;

            LoadCurrentVehicle();
            ResetPositionSample();

            if (roster != null)
                roster.VehicleChanged += HandleVehicleChanged;

            if (activityManager != null)
                activityManager.ActivityResultShown += HandleActivityResult;
        }

        private void Update()
        {
            TrackDistance();

            if (!dirty)
                return;

            saveTimer += Time.deltaTime;

            if (saveTimer >= SaveIntervalSeconds)
                SaveCurrentVehicle();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveCurrentVehicle();
        }

        private void OnApplicationQuit()
        {
            SaveCurrentVehicle();
        }

        private void OnDestroy()
        {
            if (roster != null)
                roster.VehicleChanged -= HandleVehicleChanged;

            if (activityManager != null)
                activityManager.ActivityResultShown -= HandleActivityResult;

            SaveCurrentVehicle();
        }

        public int GetLegacyTierForVehicle(
            string vehicleId)
        {
            if (string.IsNullOrEmpty(
                    vehicleId))
            {
                return 0;
            }

            float km =
                Mathf.Max(
                    0f,
                    MotorCity.Persistence.MotorCitySaveService.GetFloat(
                        Key(
                            vehicleId,
                            "DistanceMeters"),
                        0f)) /
                1000f;

            int wins =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        Key(
                            vehicleId,
                            "Victories"),
                        0));

            if (km >= 400f ||
                wins >= 80)
                return 4;

            if (km >= 150f ||
                wins >= 35)
                return 3;

            if (km >= 50f ||
                wins >= 15)
                return 2;

            if (km >= 10f ||
                wins >= 4)
                return 1;

            return 0;
        }

        public void ResetAllForTesting()
        {
            SaveCurrentVehicle();

            string[] vehicleIds =
            {
                "street",
                "club",
                "muscle",
                "gt",
                "apex"
            };

            foreach (string vehicleId in vehicleIds)
            {
                MotorCity.Persistence.MotorCitySaveService.DeleteKey(Key(vehicleId, "DistanceMeters"));
                MotorCity.Persistence.MotorCitySaveService.DeleteKey(Key(vehicleId, "Victories"));
                MotorCity.Persistence.MotorCitySaveService.DeleteKey(Key(vehicleId, "EarnedCredits"));
                MotorCity.Persistence.MotorCitySaveService.DeleteKey(Key(vehicleId, "RacingVictories"));
                MotorCity.Persistence.MotorCitySaveService.DeleteKey(Key(vehicleId, "DriftVictories"));
                MotorCity.Persistence.MotorCitySaveService.DeleteKey(Key(vehicleId, "DeliveryVictories"));
            }

            MotorCity.Persistence.MotorCitySaveService.Save();

            currentVehicleId =
                roster == null
                    ? "street"
                    : roster.SelectedId;

            LoadCurrentVehicle();
            ResetPositionSample();
        }

        public void SetAllLegendaryForTesting()
        {
            SaveCurrentVehicle();

            string[] vehicleIds =
            {
                "street",
                "club",
                "muscle",
                "gt",
                "apex"
            };

            foreach (string vehicleId in vehicleIds)
            {
                MotorCity.Persistence.MotorCitySaveService.SetFloat(Key(vehicleId, "DistanceMeters"), 500000f);
                MotorCity.Persistence.MotorCitySaveService.SetInt(Key(vehicleId, "Victories"), 100);
                MotorCity.Persistence.MotorCitySaveService.SetInt(Key(vehicleId, "EarnedCredits"), 1000000);
                MotorCity.Persistence.MotorCitySaveService.SetInt(Key(vehicleId, "RacingVictories"), 40);
                MotorCity.Persistence.MotorCitySaveService.SetInt(Key(vehicleId, "DriftVictories"), 35);
                MotorCity.Persistence.MotorCitySaveService.SetInt(Key(vehicleId, "DeliveryVictories"), 25);
            }

            MotorCity.Persistence.MotorCitySaveService.Save();

            currentVehicleId =
                roster == null
                    ? "street"
                    : roster.SelectedId;

            LoadCurrentVehicle();
            ResetPositionSample();
        }

        private string LegacyStatus
        {
            get
            {
                float km = DistanceKm;

                if (km >= 400f || victories >= 80)
                    return MotorCityLocalization.Text("history.legend");

                if (km >= 150f || victories >= 35)
                    return MotorCityLocalization.Text("history.cult");

                if (km >= 50f || victories >= 15)
                    return MotorCityLocalization.Text("history.proven");

                if (km >= 10f || victories >= 4)
                    return MotorCityLocalization.Text("history.broken_in");

                return MotorCityLocalization.Text("history.new");
            }
        }

        private string FavoriteDiscipline
        {
            get
            {
                if (racingVictories <= 0 &&
                    driftVictories <= 0 &&
                    deliveryVictories <= 0)
                {
                    return MotorCityLocalization.Text("history.style_none");
                }

                if (racingVictories >= driftVictories &&
                    racingVictories >= deliveryVictories)
                {
                    return MotorCityLocalization.Text("history.style_racing");
                }

                if (driftVictories >= deliveryVictories)
                    return MotorCityLocalization.Text("history.style_drift");

                return MotorCityLocalization.Text("history.style_delivery");
            }
        }

        private void TrackDistance()
        {
            if (car == null)
                return;

            Vector3 current = car.transform.position;

            if (!positionReady)
            {
                lastPosition = current;
                positionReady = true;
                return;
            }

            Vector3 delta = current - lastPosition;
            delta.y = 0f;
            lastPosition = current;

            float step = delta.magnitude;

            if (step <= 0.01f ||
                step > MaximumTrackedStepMeters ||
                car.SpeedKph < 1f)
            {
                return;
            }

            distanceMeters += step;
            dirty = true;
        }

        private void HandleVehicleChanged()
        {
            SaveCurrentVehicle();

            currentVehicleId =
                roster == null
                    ? "street"
                    : roster.SelectedId;

            LoadCurrentVehicle();
            ResetPositionSample();
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success)
                return;

            victories++;

            if (activityManager != null)
            {
                earnedCredits +=
                    Mathf.Max(
                        0,
                        activityManager.ResultRewardCredits);
            }

            switch (activityId)
            {
                case "delivery":
                    deliveryVictories++;
                    break;

                case "drift":
                    driftVictories++;
                    break;

                case "sprint":
                case "circuit":
                    racingVictories++;
                    break;
            }

            dirty = true;
            SaveCurrentVehicle();
        }

        private void LoadCurrentVehicle()
        {
            distanceMeters =
                Mathf.Max(
                    0f,
                    MotorCity.Persistence.MotorCitySaveService.GetFloat(
                        Key(currentVehicleId, "DistanceMeters"),
                        0f));

            victories =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        Key(currentVehicleId, "Victories"),
                        0));

            earnedCredits =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        Key(currentVehicleId, "EarnedCredits"),
                        0));

            racingVictories =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        Key(currentVehicleId, "RacingVictories"),
                        0));

            driftVictories =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        Key(currentVehicleId, "DriftVictories"),
                        0));

            deliveryVictories =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        Key(currentVehicleId, "DeliveryVictories"),
                        0));

            dirty = false;
            saveTimer = 0f;
        }

        private void SaveCurrentVehicle()
        {
            if (string.IsNullOrEmpty(currentVehicleId))
                return;

            MotorCity.Persistence.MotorCitySaveService.SetFloat(
                Key(currentVehicleId, "DistanceMeters"),
                distanceMeters);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                Key(currentVehicleId, "Victories"),
                victories);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                Key(currentVehicleId, "EarnedCredits"),
                earnedCredits);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                Key(currentVehicleId, "RacingVictories"),
                racingVictories);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                Key(currentVehicleId, "DriftVictories"),
                driftVictories);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                Key(currentVehicleId, "DeliveryVictories"),
                deliveryVictories);

            MotorCity.Persistence.MotorCitySaveService.Save();

            dirty = false;
            saveTimer = 0f;
        }

        private void ResetPositionSample()
        {
            positionReady = false;

            if (car != null)
            {
                lastPosition = car.transform.position;
                positionReady = true;
            }
        }

        private static string Key(
            string vehicleId,
            string value)
        {
            return
                $"MotorCity.VehicleHistory.{vehicleId}.{value}";
        }
    }
}
