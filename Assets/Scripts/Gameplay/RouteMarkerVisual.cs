using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class RouteMarkerVisual : MonoBehaviour
    {
        private DeliveryActivity activity;
        private ActivityManager activityManager;
        private Renderer[] markerRenderers;
        private Material[] markerMaterials;
        private Vector3 baseScale;

        public void Bind(DeliveryActivity targetActivity, ActivityManager manager)
        {
            activity = targetActivity;
            activityManager = manager;
            CacheVisuals();
            baseScale = transform.localScale;
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
            if (activity == null) return;

            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive("delivery");

            SetVisible(visible);
            if (!visible) return;

            Vector3 target = activity.CurrentTarget;
            transform.position =
                new Vector3(target.x, 0f, target.z);

            float pulse =
                1f + Mathf.Sin(Time.time * 2.8f) * 0.015f;
            transform.localScale = baseScale * pulse;

            Color tint = activity.IsActive
                ? new Color(0.18f, 0.78f, 1f)
                : new Color(0.16f, 0.52f, 0.95f);

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
