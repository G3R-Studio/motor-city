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
        private bool visibilityInitialized;
        private bool lastVisible;
        private bool tintInitialized;
        private bool lastActive;
        private Vector3 baseScale;
        private Camera mainCamera;
        private CheckpointBeaconVisual checkpointBeacon;

        public void Bind(StreetSprintActivity activity, ActivityManager manager)
        {
            sprint = activity;
            activityManager = manager;
            baseScale = transform.localScale;

            CacheVisuals();

            checkpointBeacon =
                gameObject.AddComponent<CheckpointBeaconVisual>();

            checkpointBeacon.Initialize(
                new Color(
                    0.18f,
                    1f,
                    0.34f),
                true,
                CheckpointBeaconStyle.Sprint);

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

        private void HideLegacyMarkerRenderers()
        {
            if (markerRenderers == null)
                return;

            foreach (Renderer renderer in
                     markerRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled =
                        false;
                }
            }
        }

        private void Update()
        {
            if (sprint == null) return;

            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive("sprint");

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

            SnapToTarget();

            transform.localScale =
                baseScale;

            Vector3 nextTarget =
                sprint.CurrentTarget;

            bool hasNext =
                sprint.IsActive &&
                sprint.TryGetNextTarget(
                    out nextTarget);

            checkpointBeacon?.SetDirection(
                sprint.CurrentTarget,
                hasNext
                    ? nextTarget
                    : sprint.CurrentTarget,
                hasNext);

            bool active =
                sprint.IsActive;

            if (!tintInitialized ||
                active != lastActive)
            {
                tintInitialized =
                    true;
                lastActive =
                    active;

                Tint(
                    active
                        ? new Color(0.12f, 1f, 0.48f)
                        : new Color(0.22f, 1f, 0.34f));
            }
        }

        private void SnapToTarget()
        {
            Vector3 target = sprint.CurrentTarget;
            transform.position = target;
        }

        private void SetVisible(bool visible)
        {
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
