using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class StreetSprintMarkerVisual : MonoBehaviour
    {
        private StreetSprintActivity sprint;
        private ActivityManager activityManager;
        private Renderer markerRenderer;
        private Vector3 baseScale;

        public void Bind(StreetSprintActivity activity, ActivityManager manager)
        {
            sprint = activity;
            activityManager = manager;
            markerRenderer = GetComponent<Renderer>();
            baseScale = transform.localScale;
            SnapToTarget();
        }

        private void Update()
        {
            if (sprint == null) return;

            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive("sprint");

            if (markerRenderer != null)
                markerRenderer.enabled = visible;

            if (!visible) return;

            SnapToTarget();

            float pulse = 1f + Mathf.Sin(Time.time * 4.2f) * 0.08f;
            transform.localScale = new Vector3(
                baseScale.x * pulse,
                baseScale.y,
                baseScale.z * pulse);

            if (markerRenderer != null)
            {
                Color target = sprint.IsActive
                    ? new Color(0.1f, 0.95f, 0.45f, 1f)
                    : new Color(0.18f, 1f, 0.34f, 1f);

                markerRenderer.material.color =
                    Color.Lerp(
                        markerRenderer.material.color,
                        target,
                        Time.deltaTime * 7f);
            }
        }

        private void SnapToTarget()
        {
            Vector3 target = sprint.CurrentTarget;
            transform.position =
                target +
                Vector3.up *
                (0.12f + Mathf.Sin(Time.time * 2.8f) * 0.04f);
        }
    }
}
