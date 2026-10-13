using System;
using MotorCity.UI;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class ActivityMarkerVfxAnimator : MonoBehaviour
    {
        [SerializeField] private Transform iconRoot;
        [SerializeField] private float bobAmplitude = 0.10f;
        [SerializeField] private float bobSpeed = 1.45f;
        [SerializeField] private float pulseAmplitude = 0.015f;
        [SerializeField] private float pulseSpeed = 1.65f;

        private float iconBaseHeight;
        private Vector3 iconBaseScale;
        private Camera mainCamera;
        private float cameraResolveTimer;

        public void Configure(
            Transform targetIcon)
        {
            iconRoot =
                targetIcon;

            CaptureBasePose();
        }

        private void Awake()
        {
            if (iconRoot == null)
            {
                iconRoot =
                    transform.Find(
                        "Mission Icon");
            }

            // Keep the marker's core and glow aligned with its current
            // Tabler icon. Generic profession prefabs can change icon IDs
            // at runtime depending on the selected job.
            ApplyTablerIcon(ResolveMarkerId(gameObject.name));

            ResolveCamera(
                true);

            CaptureBasePose();
        }

        // Used by the garage, whose marker is based on the generic
        // Profession VFX prefab but needs a dedicated garage pictogram.
        public void ApplyTablerIcon(string markerId)
        {
            if (iconRoot == null || string.IsNullOrEmpty(markerId))
                return;

            Sprite sprite = MotorCityIconLibrary.ForWorldMarker(markerId);
            if (sprite == null)
                return;

            foreach (SpriteRenderer renderer in
                     iconRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null)
                    continue;

                if (renderer.name.IndexOf("Icon Core",
                        StringComparison.OrdinalIgnoreCase) < 0 &&
                    renderer.name.IndexOf("Icon Glow",
                        StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // Keep the authored world-space footprint if a generic
                // Tabler marker switches to another Tabler pictogram.
                Sprite oldSprite = renderer.sprite;
                if (oldSprite != null && oldSprite != sprite)
                {
                    float oldUnits = Mathf.Max(oldSprite.bounds.size.x,
                        oldSprite.bounds.size.y);
                    float newUnits = Mathf.Max(sprite.bounds.size.x,
                        sprite.bounds.size.y);
                    if (oldUnits > 0.001f && newUnits > 0.001f)
                        renderer.transform.localScale *= oldUnits / newUnits;
                }

                renderer.sprite = sprite;
            }
        }

        private static string ResolveMarkerId(string markerName)
        {
            if (string.IsNullOrEmpty(markerName))
                return null;

            string name = markerName.ToLowerInvariant();
            if (name.Contains("carwash")) return "carwash";
            if (name.Contains("delivery")) return "delivery";
            if (name.Contains("drift")) return "drift";
            if (name.Contains("sprint")) return "sprint";
            if (name.Contains("circuit")) return "circuit";
            if (name.Contains("discovery")) return "discovery";
            if (name.Contains("underground")) return "underground";
            if (name.Contains("profession")) return "profession";
            if (name.Contains("tow")) return "tow";
            return null;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            if (iconRoot == null)
                return;

            float time =
                Time.unscaledTime;

            Vector3 position =
                iconRoot.localPosition;

            position.y =
                iconBaseHeight +
                Mathf.Sin(
                    time *
                    bobSpeed) *
                bobAmplitude;

            iconRoot.localPosition =
                position;

            float pulse =
                1f +
                Mathf.Sin(
                    time *
                    pulseSpeed) *
                pulseAmplitude;

            iconRoot.localScale =
                iconBaseScale *
                pulse;
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f)
                return;

            if (iconRoot == null)
                return;

            ResolveCamera(
                false);

            if (mainCamera == null)
                return;

            Vector3 toCamera =
                mainCamera.transform.position -
                iconRoot.position;

            if (toCamera.sqrMagnitude <
                0.0001f)
            {
                return;
            }

            iconRoot.rotation =
                Quaternion.LookRotation(
                    toCamera.normalized,
                    mainCamera.transform.up);
        }

        private void ResolveCamera(
            bool force)
        {
            if (mainCamera != null)
                return;

            if (!force)
            {
                cameraResolveTimer -=
                    Time.unscaledDeltaTime;

                if (cameraResolveTimer > 0f)
                    return;
            }

            cameraResolveTimer =
                0.5f;

            mainCamera =
                Camera.main;
        }

        private void CaptureBasePose()
        {
            if (iconRoot == null)
                return;

            iconBaseHeight =
                iconRoot.localPosition.y;

            iconBaseScale =
                iconRoot.localScale;
        }
    }
}
