using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class RouteMarkerVisual : MonoBehaviour
    {
        private const string DeliveryVfxResourcePath =
            "MotorCity/Markers/DeliveryMarkerVfx";

        private DeliveryActivity activity;
        private ActivityManager activityManager;
        private Renderer[] legacyRenderers;
        private GameObject deliveryVfx;
        private CheckpointBeaconVisual fallbackBeacon;
        private bool visibilityInitialized;
        private bool lastVisible;

        public void Bind(
            DeliveryActivity targetActivity,
            ActivityManager manager)
        {
            activity =
                targetActivity;

            activityManager =
                manager;

            legacyRenderers =
                GetComponentsInChildren<Renderer>(
                    true);

            HideLegacyVisuals();

            if (!TryCreateDeliveryVfx())
            {
                CreateFallbackBeacon();
            }

            RefreshVisibility(
                true);
        }

        private void Update()
        {
            if (activity == null)
                return;

            transform.position =
                activity.CurrentTarget;

            if (fallbackBeacon != null)
            {
                Vector3 nextTarget =
                    activity.CurrentTarget;

                bool hasNext =
                    activity.IsActive &&
                    activity.TryGetNextTarget(
                        out nextTarget);

                fallbackBeacon.SetDirection(
                    activity.CurrentTarget,
                    hasNext
                        ? nextTarget
                        : activity.CurrentTarget,
                    hasNext);
            }

            RefreshVisibility(
                false);
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

        private bool TryCreateDeliveryVfx()
        {
            GameObject prefab =
                Resources.Load<GameObject>(
                    DeliveryVfxResourcePath);

            if (prefab == null)
                return false;

            deliveryVfx =
                Instantiate(
                    prefab,
                    transform);

            deliveryVfx.name =
                "Delivery Marker VFX Runtime";

            deliveryVfx.transform.localPosition =
                Vector3.zero;

            deliveryVfx.transform.localRotation =
                Quaternion.identity;

            deliveryVfx.transform.localScale =
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
                    0.12f,
                    0.58f,
                    1f),
                true,
                CheckpointBeaconStyle.Delivery);
        }

        private void RefreshVisibility(
            bool force)
        {
            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive(
                    "delivery");

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

            if (deliveryVfx != null)
            {
                deliveryVfx.SetActive(
                    visible);
            }

            fallbackBeacon?.SetVisible(
                visible);
        }
    }
}
