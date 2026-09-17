using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class DriftChallengeMarkerVisual : MonoBehaviour
    {
        private DriftChallenge challenge;
        private ActivityManager activityManager;
        private Vector3 basePosition;
        private Vector3 baseScale;
        private Renderer markerRenderer;

        public void Bind(DriftChallenge target, ActivityManager manager)
        {
            challenge = target;
            activityManager = manager;

            if (challenge != null)
                transform.position = challenge.ZoneCenter + Vector3.up * 0.09f;

            basePosition = transform.position;
            baseScale = transform.localScale;
            markerRenderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (challenge == null) return;

            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive("drift");

            if (markerRenderer != null)
                markerRenderer.enabled = visible;

            if (!visible) return;

            float pulse = 1f + Mathf.Sin(Time.time * 3.6f) * 0.07f;
            transform.localScale = new Vector3(
                baseScale.x * pulse,
                baseScale.y,
                baseScale.z * pulse);

            transform.position =
                basePosition +
                Vector3.up * (Mathf.Sin(Time.time * 2.4f) * 0.035f);

            if (markerRenderer != null)
            {
                Color target = challenge.IsActive
                    ? new Color(1f, 0.38f, 0.05f, 0.42f)
                    : new Color(1f, 0.48f, 0.06f, 1f);

                markerRenderer.material.color =
                    Color.Lerp(
                        markerRenderer.material.color,
                        target,
                        Time.deltaTime * 6f);
            }
        }
    }
}
