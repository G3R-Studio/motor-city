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

        public void Bind(DeliveryActivity targetActivity, ActivityManager manager)
        {
            activity = targetActivity;
            activityManager = manager;
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
            if (activity == null) return;

            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive("delivery");

            SetVisible(visible);
            if (!visible) return;

            Vector3 target = activity.CurrentTarget;
            transform.position = new Vector3(
                target.x,
                1.25f + Mathf.Sin(Time.time * 3f) * 0.18f,
                target.z);

            transform.Rotate(
                0f,
                55f * Time.deltaTime,
                0f,
                Space.World);

            Color tint = activity.IsActive
                ? new Color(1f, 0.72f, 0.18f)
                : new Color(0.22f, 0.62f, 1f);

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
