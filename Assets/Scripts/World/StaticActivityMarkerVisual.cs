using UnityEngine;

namespace MotorCity.World
{
    public sealed class StaticActivityMarkerVisual : MonoBehaviour
    {
        private GameObject markerVfx;
        private CheckpointBeaconVisual fallbackBeacon;

        public void Bind(
            string resourcePath,
            Color fallbackColor,
            CheckpointBeaconStyle fallbackStyle)
        {
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

                return;
            }

            fallbackBeacon =
                gameObject.AddComponent<
                    CheckpointBeaconVisual>();

            fallbackBeacon.Initialize(
                fallbackColor,
                false,
                fallbackStyle);
        }

        public void SetVisible(
            bool visible)
        {
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
