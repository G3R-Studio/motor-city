using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class DiscoveryMarkerVisual : MonoBehaviour
    {
        private const string ResourcePath =
            "MotorCity/Markers/DiscoveryMarkerVfx";

        private DiscoverySystem discoveries;
        private int discoveryIndex;
        private GameObject markerVfx;
        private CheckpointBeaconVisual fallbackBeacon;
        private bool visibilityInitialized;
        private bool lastVisible;

        public void Bind(
            DiscoverySystem system,
            int index)
        {
            discoveries =
                system;

            discoveryIndex =
                index;

            if (discoveries != null)
            {
                transform.position =
                    discoveries.GetDiscoveryPosition(
                        discoveryIndex);
            }

            if (!TryCreateVfx())
            {
                fallbackBeacon =
                    gameObject.AddComponent<
                        CheckpointBeaconVisual>();

                fallbackBeacon.Initialize(
                    new Color(
                        0.72f,
                        0.28f,
                        1f),
                    false,
                    CheckpointBeaconStyle.Discovery);
            }

            RefreshVisibility(
                true);
        }

        private void Update()
        {
            if (discoveries == null)
                return;

            transform.position =
                discoveries.GetDiscoveryPosition(
                    discoveryIndex);

            RefreshVisibility(
                false);
        }

        private bool TryCreateVfx()
        {
            GameObject prefab =
                Resources.Load<GameObject>(
                    ResourcePath);

            if (prefab == null)
                return false;

            markerVfx =
                Instantiate(
                    prefab,
                    transform);

            markerVfx.name =
                "Discovery Marker VFX Runtime";

            markerVfx.transform.localPosition =
                Vector3.zero;

            markerVfx.transform.localRotation =
                Quaternion.identity;

            markerVfx.transform.localScale =
                Vector3.one;

            return true;
        }

        private void RefreshVisibility(
            bool force)
        {
            bool visible =
                discoveries != null &&
                !discoveries.IsFound(
                    discoveryIndex);

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
