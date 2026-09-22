using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class DynamicActivityCheckpointVisual : MonoBehaviour
    {
        private CityProfessionSystem profession;
        private TowTruckJobSystem towTruck;
        private UndergroundSceneSystem underground;
        private CheckpointBeaconVisual beacon;

        private bool visibilityInitialized;
        private bool lastVisible;

        public void BindProfession(
            CityProfessionSystem system)
        {
            profession =
                system;

            Setup(
                new Color(
                    1f,
                    0.72f,
                    0.16f));
        }

        public void BindTowTruck(
            TowTruckJobSystem system)
        {
            towTruck =
                system;

            Setup(
                new Color(
                    1f,
                    0.58f,
                    0.08f));
        }

        public void BindUnderground(
            UndergroundSceneSystem system)
        {
            underground =
                system;

            Setup(
                new Color(
                    0.72f,
                    0.28f,
                    1f));
        }

        private void Setup(
            Color color)
        {
            beacon =
                gameObject.AddComponent<CheckpointBeaconVisual>();

            beacon.Initialize(
                color);

            beacon.SetVisible(
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

                beacon?.SetVisible(
                    visible);
            }

            if (!visible)
                return;

            transform.position =
                target;

            beacon?.SetDirection(
                target,
                hasNext
                    ? nextTarget
                    : target,
                hasNext);
        }
    }
}
