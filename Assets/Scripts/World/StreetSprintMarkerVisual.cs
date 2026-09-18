using System.Collections.Generic;
using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class StreetSprintMarkerVisual : MonoBehaviour
    {
        private StreetSprintActivity sprint;
        private ActivityManager activityManager;
        private Renderer[] markerRenderers;
        private Material[] markerMaterials;
        private Vector3 baseScale;

        public void Bind(StreetSprintActivity activity, ActivityManager manager)
        {
            sprint = activity;
            activityManager = manager;
            baseScale = transform.localScale;
            CacheVisuals();
            SnapToTarget();
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
            if (sprint == null) return;

            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive("sprint");

            SetVisible(visible);
            if (!visible) return;

            SnapToTarget();

            float pulse =
                1f + Mathf.Sin(Time.time * 4.2f) * 0.045f;

            transform.localScale =
                baseScale * pulse;

            Tint(
                sprint.IsActive
                    ? new Color(0.12f, 1f, 0.48f)
                    : new Color(0.22f, 1f, 0.34f));
        }

        private void SnapToTarget()
        {
            Vector3 target = sprint.CurrentTarget;
            transform.position =
                target +
                Vector3.up *
                (0.12f +
                 Mathf.Sin(Time.time * 2.8f) * 0.04f);
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
