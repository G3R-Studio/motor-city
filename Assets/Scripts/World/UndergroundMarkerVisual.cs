using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class UndergroundMarkerVisual : MonoBehaviour
    {
        private const string ResourcePath =
            "MotorCity/Markers/UndergroundMarkerVfx";

        private UndergroundSceneSystem underground;
        private GameObject markerVfx;
        private CheckpointBeaconVisual fallbackBeacon;
        private DayNightCycleController dayNight;
        private float dayNightResolveTimer;
        private bool visibilityInitialized;
        private bool lastVisible;

        public void Bind(
            UndergroundSceneSystem target)
        {
            underground =
                target;

            dayNight =
                Object.FindAnyObjectByType<
                    DayNightCycleController>();

            if (!TryCreateVfx())
            {
                fallbackBeacon =
                    gameObject.AddComponent<
                        CheckpointBeaconVisual>();

                fallbackBeacon.Initialize(
                    new Color(
                        0.78f,
                        0.18f,
                        1f),
                    false,
                    CheckpointBeaconStyle.Underground);
            }

            RefreshVisibility(
                true);
        }

        private void Update()
        {
            if (underground == null)
                return;

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
                "Underground Marker VFX Runtime";

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
                underground != null &&
                underground.HasActiveInvitation &&
                (underground.IsNearMeeting ||
                 underground.IsActive ||
                 underground.IsCountingDown ||
                 IsNight());

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

        private bool IsNight()
        {
            if (dayNight == null)
            {
                dayNightResolveTimer -=
                    Time.unscaledDeltaTime;

                if (dayNightResolveTimer <= 0f)
                {
                    dayNightResolveTimer =
                        1f;

                    dayNight =
                        Object.FindAnyObjectByType<
                            DayNightCycleController>();
                }
            }

            return
                dayNight != null &&
                dayNight.IsNight;
        }
    }
}
