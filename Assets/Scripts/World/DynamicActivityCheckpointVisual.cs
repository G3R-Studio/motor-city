using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class DynamicActivityCheckpointVisual : MonoBehaviour
    {
        private CityProfessionSystem profession;
        private TowTruckJobSystem towTruck;
        private UndergroundSceneSystem underground;
        private GameObject markerVfx;
        private CheckpointBeaconVisual fallbackBeacon;
        private bool visibilityInitialized;
        private bool lastVisible;

        public void BindProfession(
            CityProfessionSystem system)
        {
            profession =
                system;

            Setup(
                "MotorCity/Markers/ProfessionMarkerVfx",
                new Color(
                    1f,
                    0.68f,
                    0.10f),
                CheckpointBeaconStyle.Profession);
        }

        public void BindTowTruck(
            TowTruckJobSystem system)
        {
            towTruck =
                system;

            Setup(
                "MotorCity/Markers/TowMarkerVfx",
                new Color(
                    1f,
                    0.56f,
                    0.06f),
                CheckpointBeaconStyle.Tow);
        }

        public void BindUnderground(
            UndergroundSceneSystem system)
        {
            underground =
                system;

            Setup(
                "MotorCity/Markers/UndergroundMarkerVfx",
                new Color(
                    0.78f,
                    0.18f,
                    1f),
                CheckpointBeaconStyle.Underground);
        }

        private void Setup(
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
                    "Dynamic Marker VFX Runtime";

                markerVfx.transform.localPosition =
                    Vector3.zero;

                markerVfx.transform.localRotation =
                    Quaternion.identity;

                markerVfx.transform.localScale =
                    Vector3.one;

                markerVfx.SetActive(
                    false);

                return;
            }

            fallbackBeacon =
                gameObject.AddComponent<
                    CheckpointBeaconVisual>();

            fallbackBeacon.Initialize(
                fallbackColor,
                true,
                fallbackStyle);

            fallbackBeacon.SetVisible(
                false);
        }

        private void Update()
        {
            bool visible;
            Vector3 target;
            Vector3 nextTarget =
                Vector3.zero;

            bool hasNext;

            if (profession != null)
            {
                visible =
                    profession.IsActive;

                target =
                    profession.CurrentTarget;

                hasNext =
                    visible &&
                    profession.TryGetNextTarget(
                        out nextTarget);
            }
            else if (towTruck != null)
            {
                visible =
                    towTruck.IsActive;

                target =
                    towTruck.CurrentTarget;

                hasNext =
                    visible &&
                    towTruck.TryGetNextTarget(
                        out nextTarget);
            }
            else if (underground != null)
            {
                visible =
                    underground.IsActive ||
                    underground.IsCountingDown;

                target =
                    underground.CurrentTarget;

                hasNext =
                    visible &&
                    underground.TryGetNextTarget(
                        out nextTarget);
            }
            else
            {
                visible =
                    false;

                target =
                    transform.position;

                nextTarget =
                    target;

                hasNext =
                    false;
            }

            if (!visibilityInitialized ||
                visible != lastVisible)
            {
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

            if (!visible)
                return;

            transform.position =
                target;

            fallbackBeacon?.SetDirection(
                target,
                hasNext
                    ? nextTarget
                    : target,
                hasNext);
        }
    }
}
