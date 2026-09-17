using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MotorCity.Gameplay
{
    public sealed class GarageUpgradeSystem : MonoBehaviour
    {
        private const string EngineKey = "MotorCity.Upgrade.Engine";
        private const string GripKey = "MotorCity.Upgrade.Grip";
        private const string StabilityKey = "MotorCity.Upgrade.Stability";
        private const string ActivityId = "garage";
        private const int MaxLevel = 3;

        [SerializeField] private Vector3 garageCenter = new(-42f, 0f, -42f);
        [SerializeField] private float interactRadius = 7f;
        [SerializeField] private float maxOpenSpeedKph = 8f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private DeliveryActivity delivery;
        private DriftChallenge driftChallenge;
        private StreetSprintActivity streetSprint;

        private GUIStyle titleStyle;
        private GUIStyle itemStyle;
        private GUIStyle hintStyle;

        public int EngineLevel { get; private set; }
        public int GripLevel { get; private set; }
        public int StabilityLevel { get; private set; }
        public bool IsOpen { get; private set; }
        public Vector3 GarageCenter => garageCenter;
        public bool IsNearGarage { get; private set; }
        public string StatusText { get; private set; } = "Фиолетовый маркер: гараж";

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager manager,
            DeliveryActivity deliveryActivity,
            DriftChallenge challenge,
            StreetSprintActivity sprint)
        {
            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            delivery = deliveryActivity;
            driftChallenge = challenge;
            streetSprint = sprint;

            EngineLevel = Mathf.Clamp(PlayerPrefs.GetInt(EngineKey, 0), 0, MaxLevel);
            GripLevel = Mathf.Clamp(PlayerPrefs.GetInt(GripKey, 0), 0, MaxLevel);
            StabilityLevel = Mathf.Clamp(PlayerPrefs.GetInt(StabilityKey, 0), 0, MaxLevel);

            ApplyUpgrades();
        }

        private void Update()
        {
            if (car == null || wallet == null || activityManager == null) return;

            float distance = Vector3.Distance(Flat(car.transform.position), Flat(garageCenter));
            IsNearGarage = distance <= interactRadius;

            if (IsOpen && (!IsNearGarage || car.SpeedKph > maxOpenSpeedKph))
                CloseGarage();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (IsNearGarage && keyboard.eKey.wasPressedThisFrame)
            {
                if (IsOpen)
                {
                    CloseGarage();
                }
                else if (car.SpeedKph <= maxOpenSpeedKph)
                {
                    OpenGarage();
                }
                else
                {
                    StatusText = "Гараж: сначала полностью останови машину";
                }
            }

            if (!IsOpen)
            {
                StatusText = IsNearGarage
                    ? (activityManager.IsBusy
                        ? $"E — открыть гараж и отменить «{activityManager.ActiveName}»"
                        : "ГАРАЖ — остановись и нажми E")
                    : "Фиолетовый маркер: гараж";
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                TryBuy(UpgradeType.Engine);
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                TryBuy(UpgradeType.Grip);
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                TryBuy(UpgradeType.Stability);
            if (keyboard.escapeKey.wasPressedThisFrame)
                CloseGarage();
        }

        private void OpenGarage()
        {
            CancelActiveMission();

            if (!activityManager.TryBegin(ActivityId, "Гараж"))
                return;

            IsOpen = true;
            car.SetDrivingEnabled(false);
            StatusText = "ГАРАЖ ОТКРЫТ";
        }

        private void CancelActiveMission()
        {
            delivery?.CancelActivity();
            driftChallenge?.CancelActivity();
            streetSprint?.CancelActivity();
        }

        private void CloseGarage()
        {
            IsOpen = false;
            car.SetDrivingEnabled(true);
            activityManager.End(ActivityId);
            StatusText = IsNearGarage ? "ГАРАЖ — нажми E" : "Фиолетовый маркер: гараж";
        }

        private void TryBuy(UpgradeType type)
        {
            int level = GetLevel(type);
            if (level >= MaxLevel)
            {
                StatusText = $"{Name(type)} уже улучшен до максимума";
                return;
            }

            int price = Price(type, level);
            if (!wallet.TrySpendCredits(price))
            {
                StatusText = $"Для «{Name(type)}» нужно {price:N0} CR";
                return;
            }

            level++;
            SetLevel(type, level);
            Save();
            ApplyUpgrades();
            StatusText = $"{Name(type)} улучшен до уровня {level}";
        }

        private void ApplyUpgrades()
        {
            if (car != null)
                car.ApplyUpgradeLevels(EngineLevel, GripLevel, StabilityLevel);
        }

        private int GetLevel(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Engine => EngineLevel,
                UpgradeType.Grip => GripLevel,
                _ => StabilityLevel
            };
        }

        private void SetLevel(UpgradeType type, int level)
        {
            switch (type)
            {
                case UpgradeType.Engine: EngineLevel = level; break;
                case UpgradeType.Grip: GripLevel = level; break;
                case UpgradeType.Stability: StabilityLevel = level; break;
            }
        }

        private static int Price(UpgradeType type, int currentLevel)
        {
            int basePrice = type switch
            {
                UpgradeType.Engine => 700,
                UpgradeType.Grip => 650,
                _ => 600
            };
            return basePrice * (currentLevel + 1);
        }

        private static string Name(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.Engine => "Двигатель",
                UpgradeType.Grip => "Сцепление",
                _ => "Стабильность"
            };
        }

        private void Save()
        {
            PlayerPrefs.SetInt(EngineKey, EngineLevel);
            PlayerPrefs.SetInt(GripKey, GripLevel);
            PlayerPrefs.SetInt(StabilityKey, StabilityLevel);
            PlayerPrefs.Save();
        }

        private void OnGUI()
        {
            if (!IsOpen || wallet == null) return;
            EnsureStyles();

            float width = Mathf.Min(700f, Screen.width - 40f);
            float x = (Screen.width - width) * 0.5f;
            float y = Mathf.Max(30f, Screen.height * 0.17f);

            GUI.Box(new Rect(x, y, width, 310f), string.Empty);
            GUI.Label(new Rect(x + 24f, y + 18f, width - 48f, 44f), "ГАРАЖ MOTOR CITY", titleStyle);
            GUI.Label(new Rect(x + 24f, y + 62f, width - 48f, 32f), $"{wallet.Credits:N0} CR", titleStyle);

            DrawUpgrade(x, y + 108f, UpgradeType.Engine, EngineLevel, "+12% тяги и +10% отклика за уровень");
            DrawUpgrade(x, y + 158f, UpgradeType.Grip, GripLevel, "+10% бокового сцепления за уровень");
            DrawUpgrade(x, y + 208f, UpgradeType.Stability, StabilityLevel, "+16% anti-roll и +12% демпфирования за уровень");

            GUI.Label(new Rect(x + 24f, y + 267f, width - 48f, 28f), "1 / 2 / 3 — купить     E или Esc — закрыть", hintStyle);
        }

        private void DrawUpgrade(float x, float y, UpgradeType type, int level, string description)
        {
            string levelText = level >= MaxLevel ? "МАКС" : $"УР. {level}/{MaxLevel}";
            string priceText = level >= MaxLevel ? string.Empty : $"   {Price(type, level):N0} CR";
            GUI.Label(
                new Rect(x + 24f, y, 650f, 32f),
                $"[{(int)type + 1}] {Name(type)}   {levelText}{priceText}   — {description}",
                itemStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = Color.white;

            itemStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            itemStyle.normal.textColor = Color.white;

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft
            };
            hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.72f);
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private enum UpgradeType
        {
            Engine = 0,
            Grip = 1,
            Stability = 2
        }
    }
}
