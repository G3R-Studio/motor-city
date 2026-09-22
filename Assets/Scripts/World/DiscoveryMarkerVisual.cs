using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class DiscoveryMarkerVisual : MonoBehaviour
    {
        private DiscoverySystem discoveries;
        private int discoveryIndex;
        private CheckpointBeaconVisual beacon;
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

            beacon =
                gameObject.AddComponent<CheckpointBeaconVisual>();

            beacon.Initialize(
                new Color(
                    0.72f,
                    0.28f,
                    1f),
                false,
                CheckpointBeaconStyle.Discovery);

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

            beacon?.SetVisible(
                visible);
        }
    }
}
