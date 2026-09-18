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
        private GUIStyle moneyStyle;
        private GUIStyle itemStyle;
        private GUIStyle priceStyle;
        private GUIStyle descriptionStyle;
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
                StatusText = $"Для «{Name(type)}» нужно {price:N0} КР";
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

            float width = Mathf.Clamp(Screen.width - 24f, 520f, 820f);
            float x = (Screen.width - width) * 0.5f;
            float y = Mathf.Max(12f, Screen.height * 0.08f);
            float panelHeight = 388f;

            GUI.Box(new Rect(x, y, width, panelHeight), string.Empty);
            GUI.Label(new Rect(x + 24f, y + 16f, width - 48f, 36f), "ГАРАЖ MOTOR CITY", titleStyle);
            GUI.Label(new Rect(x + 24f, y + 52f, width - 48f, 30f), $"{wallet.Credits:N0} КР", moneyStyle);

            DrawUpgrade(x, width, y + 96f, UpgradeType.Engine, EngineLevel, "+12% тяги и +10% отклика за уровень");
            DrawUpgrade(x, width, y + 166f, UpgradeType.Grip, GripLevel, "+10% бокового сцепления за уровень");
            DrawUpgrade(x, width, y + 236f, UpgradeType.Stability, StabilityLevel, "+16% стабилизации крена и +12% демпфирования за уровень");

            GUI.Label(
                new Rect(x + 24f, y + 334f, width - 48f, 28f),
                "1 / 2 / 3 — купить     E или Esc — закрыть",
                hintStyle);
        }

        private void DrawUpgrade(
            float x,
            float width,
            float y,
            UpgradeType type,
            int level,
            string description)
        {
            string levelText = level >= MaxLevel ? "МАКС" : $"УР. {level}/{MaxLevel}";
            string priceText = level >= MaxLevel ? "КУПЛЕНО" : $"{Price(type, level):N0} КР";

            GUI.Label(
                new Rect(x + 24f, y, width - 190f, 28f),
                $"[{(int)type + 1}] {Name(type)}   {levelText}",
                itemStyle);

            GUI.Label(
                new Rect(x + width - 178f, y, 154f, 28f),
                priceText,
                priceStyle);

            GUI.Label(
                new Rect(x + 42f, y + 30f, width - 66f, 24f),
                description,
                descriptionStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                titleStyle.normal.textColor = Color.white;
            }

            if (moneyStyle == null)
            {
                moneyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 23,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                moneyStyle.normal.textColor = Color.white;
            }

            if (itemStyle == null)
            {
                itemStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                itemStyle.normal.textColor = Color.white;
            }

            if (priceStyle == null)
            {
                priceStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight
                };
                priceStyle.normal.textColor = Color.white;
            }

            if (descriptionStyle == null)
            {
                descriptionStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft
                };
                descriptionStyle.normal.textColor = new Color(1f, 1f, 1f, 0.78f);
            }

            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleLeft
                };
                hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.72f);
            }
        }

        private void OnDisable()
        {
            RestoreDriving();
        }

        private void OnDestroy()
        {
            RestoreDriving();
        }

        private void RestoreDriving()
        {
            if (car != null)
                car.SetDrivingEnabled(true);

            if (activityManager != null)
                activityManager.End(ActivityId);

            IsOpen = false;
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
