using System.Collections.Generic;
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
        private Renderer[] markerRenderers;
        private Material[] markerMaterials;

        public void Bind(DriftChallenge target, ActivityManager manager)
        {
            challenge = target;
            activityManager = manager;

            if (challenge != null)
                transform.position =
                    challenge.ZoneCenter + Vector3.up * 0.09f;

            basePosition = transform.position;
            baseScale = transform.localScale;
            CacheVisuals();
        }

        private void CacheVisuals()
        {
            markerRenderers = GetComponentsInChildren<Renderer>(true);
            List<Material> materials = new();

            foreach (Renderer renderer in markerRenderers)
            {
                if (renderer == null) continue;
                materials.AddRange(renderer.materials);
            }

            markerMaterials = materials.ToArray();
        }

        private void Update()
        {
            if (challenge == null) return;

            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive("drift");

            SetVisible(visible);
            if (!visible) return;

            float pulse =
                1f + Mathf.Sin(Time.time * 3.6f) * 0.055f;

            transform.localScale = new Vector3(
                baseScale.x * pulse,
                baseScale.y * pulse,
                baseScale.z * pulse);

            transform.position =
                basePosition +
                Vector3.up *
                (Mathf.Sin(Time.time * 2.4f) * 0.035f);

            Color tint = challenge.IsActive
                ? new Color(1f, 0.36f, 0.04f)
                : new Color(1f, 0.55f, 0.08f);

            Tint(tint);
        }

        private void SetVisible(bool visible)
        {
            if (markerRenderers == null) return;
            foreach (Renderer renderer in markerRenderers)
                if (renderer != null)
                    renderer.enabled = visible;
        }

        private void Tint(Color color)
        {
            if (markerMaterials == null) return;

            foreach (Material material in markerMaterials)
            {
                if (material == null) continue;
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color"))
                    material.color = color;
            }
        }
    }
}
