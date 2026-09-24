using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class DriftChallengeMarkerVisual : MonoBehaviour
    {
        private const string DriftVfxResourcePath =
            "MotorCity/Markers/DriftMarkerVfx";

        private DriftChallenge challenge;
        private ActivityManager activityManager;
        private Vector3 basePosition;
        private Renderer[] legacyRenderers;
        private GameObject driftVfx;
        private CheckpointBeaconVisual fallbackBeacon;
        private bool visibilityInitialized;
        private bool lastVisible;

        public void Bind(
            DriftChallenge target,
            ActivityManager manager)
        {
            challenge =
                target;

            activityManager =
                manager;

            if (challenge != null)
            {
                transform.position =
                    challenge.ZoneCenter +
                    Vector3.up *
                    0.09f;
            }

            basePosition =
                transform.position;

            CacheAndHideLegacyVisuals();

            if (!TryCreateDriftVfx())
            {
                CreateFallbackBeacon();
            }

            RefreshVisibility(
                true);
        }

        private void Update()
        {
            if (challenge == null)
                return;

            transform.position =
                basePosition;

            RefreshVisibility(
                false);
        }

        private bool TryCreateDriftVfx()
        {
            GameObject prefab =
                Resources.Load<GameObject>(
                    DriftVfxResourcePath);

            if (prefab == null)
                return false;

            driftVfx =
                Instantiate(
                    prefab,
                    transform);

            driftVfx.name =
                "Drift Marker VFX Runtime";

            driftVfx.transform.localPosition =
                Vector3.zero;

            driftVfx.transform.localRotation =
                Quaternion.identity;

            driftVfx.transform.localScale =
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
                    1f,
                    0.48f,
                    0.06f),
                false,
                CheckpointBeaconStyle.Drift);
        }

        private void CacheAndHideLegacyVisuals()
        {
            legacyRenderers =
                GetComponentsInChildren<Renderer>(
                    true);

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

        private void RefreshVisibility(
            bool force)
        {
            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive(
                    "drift");

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

            if (driftVfx != null)
            {
                driftVfx.SetActive(
                    visible);
            }

            fallbackBeacon?.SetVisible(
                visible);
        }
    }
}
