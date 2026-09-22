using System.Collections.Generic;
using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class CircuitRaceMarkerVisual : MonoBehaviour
    {
        private CircuitRaceActivity race;
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

        public void Bind(
            CircuitRaceActivity activity,
            ActivityManager manager)
        {
            race = activity;
            activityManager = manager;
            baseScale = transform.localScale;

            CacheVisuals();

            checkpointBeacon =
                gameObject.AddComponent<CheckpointBeaconVisual>();

            checkpointBeacon.Initialize(
                new Color(
                    0.08f,
                    0.9f,
                    1f),
                true,
                CheckpointBeaconStyle.Circuit);

            HideLegacyMarkerRenderers();


            SnapToTarget();
            mainCamera = Camera.main;
        }

        private void CacheVisuals()
        {
            markerRenderers =
                GetComponentsInChildren<Renderer>(true);

            List<Material> materials =
                new();

            foreach (Renderer renderer in markerRenderers)
            {
                if (renderer == null)
                    continue;

                materials.AddRange(
                    renderer.materials);
            }

            markerMaterials =
                materials.ToArray();
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
            if (race == null)
                return;

            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive("circuit");

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

            if (!visible)
                return;

            SnapToTarget();

            float pulse =
                1f +
                Mathf.Sin(
                    Time.time * 3.7f) *
                0.018f;

            transform.localScale =
                baseScale *
                pulse;

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null)
            {
                Vector3 direction =
                    mainCamera.transform.position -
                    transform.position;

                direction.y = 0f;

                if (direction.sqrMagnitude > 0.01f)
                {
                    transform.rotation =
                        Quaternion.LookRotation(
                            direction.normalized,
                            Vector3.up);
                }
            }

            Vector3 nextTarget =
                race.CurrentTarget;

            bool hasNext =
                race.IsActive &&
                race.TryGetNextTarget(
                    out nextTarget);

            checkpointBeacon?.SetDirection(
                race.CurrentTarget,
                hasNext
                    ? nextTarget
                    : race.CurrentTarget,
                hasNext);

            bool active =
                race.IsActive;

            if (!tintInitialized ||
                active != lastActive)
            {
                tintInitialized =
                    true;
                lastActive =
                    active;

                Tint(
                    active
                        ? new Color(0.08f, 1f, 0.92f)
                        : new Color(0.12f, 0.86f, 1f));
            }
        }

        private void SnapToTarget()
        {
            transform.position =
                race.CurrentTarget;
        }

        private void SetVisible(
            bool visible)
        {
            checkpointBeacon?.SetVisible(
                visible);
        }

        private void Tint(
            Color color)
        {
            if (markerMaterials == null)
                return;

            foreach (Material material in markerMaterials)
            {
                if (material == null)
                    continue;

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color"))
                    material.color = color;
            }
        }
    }
}
