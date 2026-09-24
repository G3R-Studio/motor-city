using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class CircuitRaceMarkerVisual : MonoBehaviour
    {
        private const string CircuitVfxResourcePath =
            "MotorCity/Markers/CircuitMarkerVfx";

        private CircuitRaceActivity race;
        private ActivityManager activityManager;
        private Renderer[] legacyRenderers;
        private GameObject circuitVfx;
        private CheckpointBeaconVisual fallbackBeacon;
        private bool visibilityInitialized;
        private bool lastVisible;

        public void Bind(
            CircuitRaceActivity activity,
            ActivityManager manager)
        {
            race =
                activity;

            activityManager =
                manager;

            legacyRenderers =
                GetComponentsInChildren<Renderer>(
                    true);

            HideLegacyVisuals();

            if (!TryCreateCircuitVfx())
            {
                CreateFallbackBeacon();
            }

            SnapToTarget();

            RefreshVisibility(
                true);
        }

        private void Update()
        {
            if (race == null)
                return;

            SnapToTarget();

            if (fallbackBeacon != null)
            {
                Vector3 nextTarget =
                    race.CurrentTarget;

                bool hasNext =
                    race.IsActive &&
                    race.TryGetNextTarget(
                        out nextTarget);

                fallbackBeacon.SetDirection(
                    race.CurrentTarget,
                    hasNext
                        ? nextTarget
                        : race.CurrentTarget,
                    hasNext);
            }

            RefreshVisibility(
                false);
        }

        private void SnapToTarget()
        {
            transform.position =
                race.CurrentTarget;
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

        private bool TryCreateCircuitVfx()
        {
            GameObject prefab =
                Resources.Load<GameObject>(
                    CircuitVfxResourcePath);

            if (prefab == null)
                return false;

            circuitVfx =
                Instantiate(
                    prefab,
                    transform);

            circuitVfx.name =
                "Circuit Marker VFX Runtime";

            circuitVfx.transform.localPosition =
                Vector3.zero;

            circuitVfx.transform.localRotation =
                Quaternion.identity;

            circuitVfx.transform.localScale =
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
                    0.08f,
                    0.9f,
                    1f),
                true,
                CheckpointBeaconStyle.Circuit);
        }

        private void RefreshVisibility(
            bool force)
        {
            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive(
                    "circuit");

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

            if (circuitVfx != null)
            {
                circuitVfx.SetActive(
                    visible);
            }

            fallbackBeacon?.SetVisible(
                visible);
        }
    }
}
