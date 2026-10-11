using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class StreetSprintMarkerVisual : MonoBehaviour
    {
        private const string SprintVfxResourcePath =
            "MotorCity/Markers/SprintMarkerVfx";

        private StreetSprintActivity sprint;
        private ActivityManager activityManager;
        private Renderer[] legacyRenderers;
        private GameObject sprintVfx;
        private CheckpointBeaconVisual fallbackBeacon;
        private SpriteRenderer[] sprintChevrons;
        private Camera directionCamera;
        private bool visibilityInitialized;
        private bool lastVisible;

        public void Bind(
            StreetSprintActivity activity,
            ActivityManager manager)
        {
            sprint =
                activity;

            activityManager =
                manager;

            legacyRenderers =
                GetComponentsInChildren<Renderer>(
                    true);

            HideLegacyVisuals();

            if (!TryCreateSprintVfx())
            {
                CreateFallbackBeacon();
            }

            SnapToTarget();

            RefreshVisibility(
                true);
        }

        private void Update()
        {
            if (sprint == null)
                return;

            SnapToTarget();

            if (fallbackBeacon != null)
            {
                Vector3 nextTarget =
                    sprint.CurrentTarget;

                bool hasNext =
                    sprint.IsActive &&
                    sprint.TryGetNextTarget(
                        out nextTarget);

                fallbackBeacon.SetDirection(
                    sprint.CurrentTarget,
                    hasNext
                        ? nextTarget
                        : sprint.CurrentTarget,
                    hasNext);
            }

            RefreshVisibility(
                false);
        }

        private void LateUpdate()
        {
            UpdateSprintChevronDirection();
        }

        // The SprintMarkerVfx fastForward sprite points right in its local
        // XY plane. Its parent billboard faces the camera, so yawing the
        // marker GameObject does NOT aim the chevrons down the race route.
        // Rotate both sprite planes within the billboard using the outgoing
        // checkpoint segment projected onto the camera's screen axes.
        private void UpdateSprintChevronDirection()
        {
            if (sprint == null ||
                sprintChevrons == null ||
                sprintChevrons.Length == 0)
                return;

            Vector3 nextTarget;
            bool hasNext =
                sprint.TryGetNextTarget(out nextTarget);

            Vector3 direction =
                nextTarget - sprint.CurrentTarget;
            direction.y = 0f;

            if (directionCamera == null)
                directionCamera = Camera.main;

            bool hasDirection =
                hasNext &&
                directionCamera != null &&
                direction.sqrMagnitude > 4f;

            float angle = 0f;
            if (hasDirection)
            {
                Transform cameraTransform =
                    directionCamera.transform;

                Vector2 projected =
                    new Vector2(
                        Vector3.Dot(
                            direction,
                            cameraTransform.right),
                        Vector3.Dot(
                            direction,
                            cameraTransform.up));

                hasDirection =
                    projected.sqrMagnitude > 0.0001f;

                if (hasDirection)
                {
                    angle = Mathf.Atan2(
                        projected.y,
                        projected.x) * Mathf.Rad2Deg;
                }
            }

            for (int i = 0;
                 i < sprintChevrons.Length;
                 i++)
            {
                SpriteRenderer chevron =
                    sprintChevrons[i];

                if (chevron == null)
                    continue;

                // The final checkpoint has no outgoing direction. Keep the
                // checkpoint ring but do not show a misleading right arrow.
                chevron.enabled = hasDirection;
                if (hasDirection)
                {
                    chevron.transform.localRotation =
                        Quaternion.Euler(0f, 0f, angle);
                }
            }
        }

        private void SnapToTarget()
        {
            transform.position =
                sprint.CurrentTarget;
        }

        private void HideLegacyVisuals()
        {
            if (legacyRenderers == null)
                return;

            foreach (Renderer renderer in
                     legacyRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled =
                        false;
                }
            }
        }

        private bool TryCreateSprintVfx()
        {
            GameObject prefab =
                Resources.Load<GameObject>(
                    SprintVfxResourcePath);

            if (prefab == null)
                return false;

            sprintVfx =
                Instantiate(
                    prefab,
                    transform);

            sprintVfx.name =
                "Sprint Marker VFX Runtime";

            sprintVfx.transform.localPosition =
                Vector3.zero;

            sprintVfx.transform.localRotation =
                Quaternion.identity;

            sprintVfx.transform.localScale =
                Vector3.one;

            SpriteRenderer[] renderers =
                sprintVfx.GetComponentsInChildren<
                    SpriteRenderer>(true);

            System.Collections.Generic.List<SpriteRenderer> chevrons =
                new();

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null &&
                    (renderer.name == "Sprint Icon Core" ||
                     renderer.name == "Sprint Icon Glow"))
                {
                    chevrons.Add(renderer);
                }
            }

            sprintChevrons = chevrons.ToArray();

            return
                true;
        }

        private void CreateFallbackBeacon()
        {
            fallbackBeacon =
                gameObject.AddComponent<
                    CheckpointBeaconVisual>();

            fallbackBeacon.Initialize(
                new Color(
                    0.18f,
                    1f,
                    0.34f),
                true,
                CheckpointBeaconStyle.Sprint);
        }

        private void RefreshVisibility(
            bool force)
        {
            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive(
                    "sprint");

            if (!force &&
                visibilityInitialized &&
                visible == lastVisible)
            {
                return;
            }

            visibilityInitialized =
                true;

            lastVisible =
                visible;

            if (sprintVfx != null)
            {
                sprintVfx.SetActive(
                    visible);
            }

            fallbackBeacon?.SetVisible(
                visible);
        }
    }
}
