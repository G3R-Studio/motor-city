using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class StaticActivityMarkerVisual : MonoBehaviour
    {
        private GameObject markerVfx;
        private CheckpointBeaconVisual fallbackBeacon;
        private ActivityManager activityManager;
        private string markerActivityId;
        private bool? lastVisibility;

        public void Bind(
            string resourcePath,
            Color fallbackColor,
            CheckpointBeaconStyle fallbackStyle,
            ActivityManager manager,
            string activityId)
        {
            activityManager = manager;
            markerActivityId = activityId;

            GameObject prefab =
                Resources.Load<GameObject>(
                    resourcePath);

            if (prefab != null)
            {
                markerVfx =
                    Instantiate(
                        prefab,
                        transform);

                markerVfx.name =
                    "Activity Marker VFX Runtime";

                markerVfx.transform.localPosition =
                    Vector3.zero;

                markerVfx.transform.localRotation =
                    Quaternion.identity;

                markerVfx.transform.localScale =
                    Vector3.one;

                RefreshVisibility();
                return;
            }

            fallbackBeacon =
                gameObject.AddComponent<
                    CheckpointBeaconVisual>();

            fallbackBeacon.Initialize(
                fallbackColor,
                false,
                fallbackStyle);

            RefreshVisibility();
        }

        private void Update()
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            // The active activity owns the world presentation. All other
            // job start markers return when ActivityManager.End() clears it.
            bool visible =
                activityManager == null ||
                !activityManager.IsBusy ||
                activityManager.IsActive(markerActivityId);

            if (!lastVisibility.HasValue ||
                lastVisibility.Value != visible)
            {
                SetVisible(visible);
            }
        }

        public void SetVisible(
            bool visible)
        {
            lastVisibility = visible;

            if (markerVfx != null)
            {
                markerVfx.SetActive(
                    visible);
            }

            fallbackBeacon?.SetVisible(
                visible);
        }
    }
}
