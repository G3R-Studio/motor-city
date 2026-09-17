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
            StreetSprintActivity sprint)
        {
            car = controller;
            wallet = playerWallet;
            drift = driftTracker;
            delivery = deliveryActivity;
            driftChallenge = challenge;
            streetSprint = sprint;
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

            GUI.Label(new Rect(Screen.width - 300, Screen.height - 104, 260, 52), $"{speed:000} km/h", speedStyle);
            GUI.Label(new Rect(24, 18, 420, 34), $"MOTOR CITY    {credits:N0} CR", primaryStyle);
            GUI.Label(new Rect(24, 52, 620, 86), "W/S — gas/reverse   A/D — steer   SPACE — handbrake\nR — reset   RMB — camera   Wheel — zoom", hintStyle);

            if (streetSprint != null)
            {
                GUI.Label(new Rect(24, Screen.height - 146, 820, 32), streetSprint.StatusText, primaryStyle);
            }

            if (driftChallenge != null)
            {
                GUI.Label(new Rect(24, Screen.height - 112, 820, 32), driftChallenge.StatusText, primaryStyle);
            }

            if (delivery != null)
            {
                GUI.Label(new Rect(24, Screen.height - 78, 820, 32), delivery.StatusText, primaryStyle);
            }

            if (drift != null && (drift.IsDrifting || drift.CurrentScore > 0))
            {
                string combo = drift.Combo > 1.05f ? $"  x{drift.Combo:0.0}" : string.Empty;
                GUI.Label(new Rect(Screen.width * 0.5f - 180f, 34f, 360f, 46f), $"DRIFT {drift.CurrentScore:N0}{combo}", driftStyle);
            }
        }
    }
}
