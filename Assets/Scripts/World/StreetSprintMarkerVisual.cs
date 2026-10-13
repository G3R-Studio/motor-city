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


            RefreshVisibility(
                false);
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
                false,
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
