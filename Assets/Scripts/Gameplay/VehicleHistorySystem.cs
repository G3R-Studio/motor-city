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

        public string GarageLine
        {
            get
            {
                return
                    $"ИСТОРИЯ: {DistanceKm:0.0} КМ   •   ПОБЕДЫ {victories}   •   " +
                    $"ЗАРАБОТАНО {earnedCredits:N0} КР   •   {LegacyStatus}   •   {FavoriteDiscipline}";
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
                PlayerPrefs.DeleteKey(Key(vehicleId, "DistanceMeters"));
                PlayerPrefs.DeleteKey(Key(vehicleId, "Victories"));
                PlayerPrefs.DeleteKey(Key(vehicleId, "EarnedCredits"));
                PlayerPrefs.DeleteKey(Key(vehicleId, "RacingVictories"));
                PlayerPrefs.DeleteKey(Key(vehicleId, "DriftVictories"));
                PlayerPrefs.DeleteKey(Key(vehicleId, "DeliveryVictories"));
            }

            PlayerPrefs.Save();

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
                PlayerPrefs.SetFloat(Key(vehicleId, "DistanceMeters"), 500000f);
                PlayerPrefs.SetInt(Key(vehicleId, "Victories"), 100);
                PlayerPrefs.SetInt(Key(vehicleId, "EarnedCredits"), 1000000);
                PlayerPrefs.SetInt(Key(vehicleId, "RacingVictories"), 40);
                PlayerPrefs.SetInt(Key(vehicleId, "DriftVictories"), 35);
                PlayerPrefs.SetInt(Key(vehicleId, "DeliveryVictories"), 25);
            }

            PlayerPrefs.Save();

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
                    return "ЛЕГЕНДА";

                if (km >= 150f || victories >= 35)
                    return "КУЛЬТОВАЯ";

                if (km >= 50f || victories >= 15)
                    return "ПРОВЕРЕНА";

                if (km >= 10f || victories >= 4)
                    return "ОБКАТАНА";

                return "НОВАЯ";
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
                    return "СТИЛЬ НЕ ОПРЕДЕЛЁН";
                }

                if (racingVictories >= driftVictories &&
                    racingVictories >= deliveryVictories)
                {
                    return "СТИЛЬ RACING";
                }

                if (driftVictories >= deliveryVictories)
                    return "СТИЛЬ DRIFT";

                return "СТИЛЬ DELIVERY";
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
                    PlayerPrefs.GetFloat(
                        Key(currentVehicleId, "DistanceMeters"),
                        0f));

            victories =
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        Key(currentVehicleId, "Victories"),
                        0));

            earnedCredits =
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        Key(currentVehicleId, "EarnedCredits"),
                        0));

            racingVictories =
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        Key(currentVehicleId, "RacingVictories"),
                        0));

            driftVictories =
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        Key(currentVehicleId, "DriftVictories"),
                        0));

            deliveryVictories =
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        Key(currentVehicleId, "DeliveryVictories"),
                        0));

            dirty = false;
            saveTimer = 0f;
        }

        private void SaveCurrentVehicle()
        {
            if (string.IsNullOrEmpty(currentVehicleId))
                return;

            PlayerPrefs.SetFloat(
                Key(currentVehicleId, "DistanceMeters"),
                distanceMeters);

            PlayerPrefs.SetInt(
                Key(currentVehicleId, "Victories"),
                victories);

            PlayerPrefs.SetInt(
                Key(currentVehicleId, "EarnedCredits"),
                earnedCredits);

            PlayerPrefs.SetInt(
                Key(currentVehicleId, "RacingVictories"),
                racingVictories);

            PlayerPrefs.SetInt(
                Key(currentVehicleId, "DriftVictories"),
                driftVictories);

            PlayerPrefs.SetInt(
                Key(currentVehicleId, "DeliveryVictories"),
                deliveryVictories);

            PlayerPrefs.Save();

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
