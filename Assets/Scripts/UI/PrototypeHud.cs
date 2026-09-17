using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private ArcadeCarController car;
        private GUIStyle speedStyle;
        private GUIStyle hintStyle;

        public void Bind(ArcadeCarController controller) => car = controller;

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

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft
            };
            hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.82f);
        }

        private void OnGUI()
        {
            EnsureStyles();
            float speed = car == null ? 0f : car.SpeedKph;

            GUI.Label(new Rect(Screen.width - 280, Screen.height - 100, 240, 50), $"{speed:000} km/h", speedStyle);
            GUI.Label(new Rect(24, 20, 460, 110), "MOTOR CITY — prototype\nW/S — gas/reverse   A/D — steer\nSPACE — handbrake   R — reset car", hintStyle);
        }
    }
}
