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
                ? $"ПОЛИЦИЯ • РОЗЫСК {RiskLevel}/5 • СБРОСЬ СКОРОСТЬ И СКРОЙСЯ"
                : string.Empty;

        public string AdminLine =>
            $"ВНИМАНИЕ {attention:0}/100 • УРОВЕНЬ {RiskLevel}/5 • " +
            (pursuitActive
                ? "ПОГОНЯ"
                : "СПОКОЙНО");

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
                    PlayerPrefs.GetFloat(
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
                Save();
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
                "ТЕСТ: ВНИМАНИЕ ПОЛИЦИИ");
        }

        public void ClearForTesting()
        {
            attention = 0f;
            pursuitActive = false;
            lastShownLevel = 0;

            StatusText =
                "ПОЛИЦИЯ — ВНИМАНИЕ СБРОШЕНО";

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
                        "ПОЛИЦИЯ ЗАМЕТИЛА УЛИЧНЫЙ СПРИНТ");
                    break;

                case "drift":
                    AddAttention(
                        10f,
                        "ПОЛИЦИЯ ЗАМЕТИЛА НЕЛЕГАЛЬНЫЙ ДРИФТ");
                    break;
            }
        }

        private void HandleUndergroundCompleted()
        {
            AddAttention(
                24f,
                "ПОЛИЦИЯ ЗАСЕКЛА ПОДПОЛЬНЫЙ ЗАЕЗД");
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
                "СМЕНА МАШИНЫ СНИЗИЛА ВНИМАНИЕ ПОЛИЦИИ";

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
                $"{reason}   •   РОЗЫСК {RiskLevel}/5";

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
                    "ПОЛИЦИЯ — НАЧАЛАСЬ ПОГОНЯ   СБРОСЬ СКОРОСТЬ, СКРОЙСЯ ИЛИ СМЕНИ МАШИНУ";

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
                    "ПОЛИЦИЯ — ТЫ ОТОРВАЛСЯ ОТ ПОГОНИ";

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
                $"ВНИМАНИЕ ПОЛИЦИИ — УРОВЕНЬ {level}/5";

            messageTimer =
                3.5f;
        }

        private void Save()
        {
            PlayerPrefs.SetFloat(
                AttentionKey,
                attention);

            PlayerPrefs.Save();
        }
    }
}
