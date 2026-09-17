using MotorCity.Gameplay;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private ArcadeCarController car;
        private PlayerWallet wallet;
        private DriftTracker drift;
        private DeliveryActivity delivery;
        private DriftChallenge driftChallenge;
        private StreetSprintActivity streetSprint;
        private ActivityManager activityManager;
        private GarageUpgradeSystem garage;

        private GUIStyle speedStyle;
        private GUIStyle primaryStyle;
        private GUIStyle hintStyle;
        private GUIStyle driftStyle;

        public void Bind(
            ArcadeCarController controller,
            PlayerWallet playerWallet,
            DriftTracker driftTracker,
            DeliveryActivity deliveryActivity,
            DriftChallenge challenge,
            StreetSprintActivity sprint,
            ActivityManager manager,
            GarageUpgradeSystem garageSystem)
        {
            car = controller;
            wallet = playerWallet;
            drift = driftTracker;
            delivery = deliveryActivity;
            driftChallenge = challenge;
            streetSprint = sprint;
            activityManager = manager;
            garage = garageSystem;
        }

        private void EnsureStyles()
        {
            if (speedStyle != null) return;

            speedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            speedStyle.normal.textColor = Color.white;

            primaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            primaryStyle.normal.textColor = Color.white;

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.UpperLeft
            };
            hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.76f);

            driftStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            driftStyle.normal.textColor = new Color(1f, 0.72f, 0.16f);
        }

        private void OnGUI()
        {
            EnsureStyles();

            float speed = car == null ? 0f : car.SpeedKph;
            int credits = wallet == null ? 0 : wallet.Credits;

            GUI.Label(
                new Rect(Screen.width - 300, Screen.height - 104, 260, 52),
                $"{speed:000} км/ч",
                speedStyle);

            string upgrades = garage == null
                ? string.Empty
                : $"    ДВ{garage.EngineLevel}  СЦ{garage.GripLevel}  СТ{garage.StabilityLevel}";

            GUI.Label(
                new Rect(24, 18, 700, 34),
                $"MOTOR CITY    {credits:N0} CR{upgrades}",
                primaryStyle);

            GUI.Label(
                new Rect(24, 52, 900, 86),
                "W/S — газ/реверс   A/D — руль   SPACE — ручник   E — гараж\nR — сброс   ПКМ — камера   Колесо — приближение",
                hintStyle);

            string status = ResolveStatus();
            if (!string.IsNullOrEmpty(status))
            {
                GUI.Label(
                    new Rect(24, Screen.height - 78, Mathf.Min(1100f, Screen.width - 48f), 34),
                    status,
                    primaryStyle);
            }

            if (drift != null && (drift.IsDrifting || drift.CurrentScore > 0))
            {
                string combo = drift.Combo > 1.05f ? $"  x{drift.Combo:0.0}" : string.Empty;
                GUI.Label(
                    new Rect(Screen.width * 0.5f - 220f, 34f, 440f, 46f),
                    $"ДРИФТ {drift.CurrentScore:N0}{combo}",
                    driftStyle);
            }
            else if (drift != null && drift.ShowRewardMessage)
            {
                GUI.Label(
                    new Rect(Screen.width * 0.5f - 220f, 34f, 440f, 46f),
                    $"ДРИФТ ЗАВЕРШЁН  +{drift.LastBankedCredits:N0} CR",
                    driftStyle);
            }
        }

        private string ResolveStatus()
        {
            if (activityManager != null && activityManager.IsBusy)
            {
                return activityManager.ActiveId switch
                {
                    "delivery" => delivery?.StatusText,
                    "drift" => driftChallenge?.StatusText,
                    "sprint" => streetSprint?.StatusText,
                    "garage" => garage?.StatusText,
                    _ => activityManager.ActiveName
                };
            }

            if (garage != null && garage.IsNearGarage)
                return garage.StatusText;

            return "СИНИЙ — доставка   •   ОРАНЖЕВЫЙ — дрифт   •   ЗЕЛЁНЫЙ — спринт   •   ФИОЛЕТОВЫЙ — гараж";
        }
    }
}
