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
        private bool visibilityInitialized;
        private bool lastVisible;
        private bool tintInitialized;
        private bool lastActive;
        private CheckpointBeaconVisual checkpointBeacon;

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

            checkpointBeacon =
                gameObject.AddComponent<CheckpointBeaconVisual>();

            checkpointBeacon.Initialize(
                new Color(
                    1f,
                    0.48f,
                    0.06f),
                false,
                CheckpointBeaconStyle.Drift);

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
            if (challenge == null) return;

            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive("drift");

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

            transform.localScale =
                baseScale;

            transform.position =
                basePosition;

            bool active =
                challenge.IsActive;

            if (!tintInitialized ||
                active != lastActive)
            {
                tintInitialized =
                    true;
                lastActive =
                    active;

                Color tint =
                    active
                        ? new Color(1f, 0.42f, 0.08f)
                        : new Color(0.95f, 0.5f, 0.1f);

                Tint(tint);
            }
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
