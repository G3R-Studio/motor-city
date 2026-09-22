using MotorCity.Localization;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CityRiskSystem : MonoBehaviour
    {
        private const string AttentionKey =
            "MotorCity.Police.Attention";

        private const float MessageSeconds = 5f;
        private const float PursuitStart = 40f;
        private const float PursuitEnd = 18f;

        private ArcadeCarController car;
        private ActivityManager activityManager;
        private GarageUpgradeSystem garage;
        private VehicleRosterSystem roster;
        private UndergroundSceneSystem underground;

        private float attention;
        private float messageTimer;
        private int lastShownLevel;
        private bool pursuitActive;
        private float saveTimer;

        public bool ShowMessage =>
            messageTimer > 0f;

        public bool PursuitActive =>
            pursuitActive;

        public int RiskLevel =>
            Mathf.Clamp(
                Mathf.CeilToInt(
                    attention / 20f),
                0,
                5);

        public float Attention =>
            attention;

        public string StatusText { get; private set; } =
            string.Empty;

        public string HudLine =>
            pursuitActive
                ? MotorCityLocalization.Format("risk.hud", RiskLevel)
                : string.Empty;

        public string AdminLine =>
            MotorCityLocalization.Format(
                "risk.admin",
                attention,
                RiskLevel,
                pursuitActive
                    ? MotorCityLocalization.Text("risk.active")
                    : MotorCityLocalization.Text("risk.calm"));

        public void Initialize(
            ArcadeCarController targetCar,
            ActivityManager manager,
            GarageUpgradeSystem garageSystem,
            VehicleRosterSystem vehicleRoster,
            UndergroundSceneSystem undergroundSystem)
        {
            car = targetCar;
            activityManager = manager;
            garage = garageSystem;
            roster = vehicleRoster;
            underground = undergroundSystem;

            attention =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetFloat(
                        AttentionKey,
                        0f),
                    0f,
                    100f);

            pursuitActive =
                attention >=
                PursuitStart;

            lastShownLevel =
                RiskLevel;

            if (activityManager != null)
            {
                activityManager.ActivityResultShown +=
                    HandleActivityResult;
            }

            if (roster != null)
            {
                roster.VehicleChanged +=
                    HandleVehicleChanged;
            }

            if (underground != null)
            {
                underground.PhysicalRunCompleted +=
                    HandleUndergroundCompleted;
            }
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

            if (car == null)
                return;

            bool garageSafe =
                garage != null &&
                garage.IsOpen;

            bool illegalActivity =
                activityManager != null &&
                activityManager.IsBusy &&
                (activityManager.ActiveId == "sprint" ||
                 activityManager.ActiveId == "drift" ||
                 activityManager.ActiveId == "underground");

            float speed =
                car.SpeedKph;

            if (garageSafe)
            {
                attention -=
                    12f *
                    Time.deltaTime;
            }
            else
            {
                if (speed > 120f)
                {
                    float excess =
                        Mathf.Clamp(
                            (speed - 120f) /
                            80f,
                            0f,
                            1.5f);

                    attention +=
                        (0.75f +
                         excess * 1.5f) *
                        Time.deltaTime;
                }

                if (illegalActivity &&
                    speed > 65f)
                {
                    attention +=
                        0.42f *
                        Time.deltaTime;
                }

                if (!illegalActivity &&
                    speed < 45f)
                {
                    float decay =
                        speed < 12f
                            ? 5.2f
                            : 2.8f;

                    if (pursuitActive)
                    {
                        decay *=
                            0.72f;
                    }

                    attention -=
                        decay *
                        Time.deltaTime;
                }
            }

            attention =
                Mathf.Clamp(
                    attention,
                    0f,
                    100f);

            UpdatePursuitState();
            UpdateLevelMessage();

            saveTimer +=
                Time.deltaTime;

            if (saveTimer >= 2f)
            {
                saveTimer = 0f;
                MarkSaveDirty();
            }
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityResultShown -=
                    HandleActivityResult;
            }

            if (roster != null)
            {
                roster.VehicleChanged -=
                    HandleVehicleChanged;
            }

            if (underground != null)
            {
                underground.PhysicalRunCompleted -=
                    HandleUndergroundCompleted;
            }

            Save();
        }

        public void AddAttentionForTesting(
            float amount)
        {
            AddAttention(
                amount,
                MotorCityLocalization.Text("risk.test"));
        }

        public void ClearForTesting()
        {
            attention = 0f;
            pursuitActive = false;
            lastShownLevel = 0;

            StatusText =
                MotorCityLocalization.Text("risk.cleared");

            messageTimer =
                MessageSeconds;

            Save();
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success)
                return;

            switch (activityId)
            {
                case "sprint":
                    AddAttention(
                        14f,
                        MotorCityLocalization.Text("risk.sprint"));
                    break;

                case "drift":
                    AddAttention(
                        10f,
                        MotorCityLocalization.Text("risk.drift"));
                    break;
            }
        }

        private void HandleUndergroundCompleted()
        {
            AddAttention(
                24f,
                MotorCityLocalization.Text("risk.night"));
        }

        private void HandleVehicleChanged()
        {
            if (attention <= 0f)
                return;

            attention =
                Mathf.Max(
                    0f,
                    attention -
                    25f);

            StatusText =
                MotorCityLocalization.Text("risk.vehicle_changed");

            messageTimer =
                MessageSeconds;

            UpdatePursuitState();
            lastShownLevel =
                RiskLevel;

            Save();
        }

        private void AddAttention(
            float amount,
            string reason)
        {
            if (amount <= 0f)
                return;

            attention =
                Mathf.Clamp(
                    attention +
                    amount,
                    0f,
                    100f);

            StatusText =
                MotorCityLocalization.Format(
                    "risk.reason_level",
                    reason,
                    RiskLevel);

            messageTimer =
                MessageSeconds;

            UpdatePursuitState();
            lastShownLevel =
                RiskLevel;

            Save();
        }

        private void UpdatePursuitState()
        {
            if (!pursuitActive &&
                attention >=
                PursuitStart)
            {
                pursuitActive = true;

                StatusText =
                    MotorCityLocalization.Text("risk.started");

                messageTimer =
                    MessageSeconds;

                return;
            }

            if (pursuitActive &&
                attention <=
                PursuitEnd)
            {
                pursuitActive = false;

                StatusText =
                    MotorCityLocalization.Text("risk.escaped");

                messageTimer =
                    MessageSeconds;
            }
        }

        private void UpdateLevelMessage()
        {
            int level =
                RiskLevel;

            if (level ==
                lastShownLevel)
            {
                return;
            }

            lastShownLevel =
                level;

            if (level <= 0)
                return;

            StatusText =
                MotorCityLocalization.Format(
                    "risk.level",
                    level);

            messageTimer =
                3.5f;
        }

        private void MarkSaveDirty()
        {
            MotorCity.Persistence.MotorCitySaveService.SetFloat(
                AttentionKey,
                attention);
        }

        private void Save()
        {
            MarkSaveDirty();

            MotorCity.Persistence.MotorCitySaveService.Save();
        }
    }
}
