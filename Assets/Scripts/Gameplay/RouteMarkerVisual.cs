using System.Collections.Generic;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class RouteMarkerVisual : MonoBehaviour
    {
        private DeliveryActivity activity;
        private ActivityManager activityManager;
        private Renderer[] markerRenderers;
        private Material[] markerMaterials;
        private bool visibilityInitialized;
        private bool lastVisible;
        private bool tintInitialized;
        private bool lastActive;
        private Vector3 baseScale;
        private CheckpointBeaconVisual checkpointBeacon;

        public void Bind(DeliveryActivity targetActivity, ActivityManager manager)
        {
            activity = targetActivity;
            activityManager = manager;

            CacheVisuals();

            checkpointBeacon =
                gameObject.AddComponent<CheckpointBeaconVisual>();

            checkpointBeacon.Initialize(
                new Color(
                    0.12f,
                    0.58f,
                    1f),
                true,
                CheckpointBeaconStyle.Delivery);

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

                        if (!visibilityInitialized ||
                visible != lastVisible)
            {
                visibilityInitialized =
                    true;
                lastVisible =
                    visible;
                SetVisible(
                    visible);
            }
            if (!visible) return;

            Vector3 target =
                activity.CurrentTarget;

            transform.position =
                target;

            Vector3 nextTarget =
                activity.CurrentTarget;

            bool hasNext =
                activity.IsActive &&
                activity.TryGetNextTarget(
                    out nextTarget);

            checkpointBeacon?.SetDirection(
                target,
                hasNext
                    ? nextTarget
                    : target,
                hasNext);

            transform.localScale =
                baseScale;

            bool active =
                activity.IsActive;

            if (!tintInitialized ||
                active != lastActive)
            {
                tintInitialized =
                    true;
                lastActive =
                    active;

                Color tint =
                    active
                        ? new Color(0.18f, 0.78f, 1f)
                        : new Color(0.16f, 0.52f, 0.95f);

                Tint(tint);
            }
        }

        private void SetVisible(bool visible)
        {
            if (markerRenderers != null)
            {
                foreach (Renderer renderer in markerRenderers)
                {
                    if (renderer != null)
                        renderer.enabled = visible;
                }
            }

            checkpointBeacon?.SetVisible(
                visible);
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
