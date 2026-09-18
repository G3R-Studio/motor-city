using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class GarageMarkerVisual : MonoBehaviour
    {
        private GarageUpgradeSystem garage;
        private SpriteRenderer marker;
        private Vector3 baseScale;
        private Camera mainCamera;

        public void Bind(GarageUpgradeSystem target)
        {
            garage = target;
            BuildMarker();

            if (garage != null)
                transform.position =
                    garage.GarageCenter + Vector3.up * 10f;
        }

        private void BuildMarker()
        {
            Sprite sprite =
                Resources.Load<Sprite>(
                    "MotorCity/Markers/flag");

            GameObject visual =
                new("Garage Marker Flag");
            visual.transform.SetParent(
                transform,
                false);

            marker =
                visual.AddComponent<SpriteRenderer>();
            marker.sprite = sprite;
            marker.color =
                new Color(0.72f, 0.2f, 1f, 1f);
            marker.sortingOrder = 200;

            visual.transform.localScale =
                Vector3.one * 6.5f;

            baseScale =
                visual.transform.localScale;

            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (garage == null || marker == null)
                return;

            transform.position =
                garage.GarageCenter +
                Vector3.up *
                (10f + Mathf.Sin(Time.time * 2.5f) * 0.45f);

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null)
            {
                Vector3 direction =
                    mainCamera.transform.position -
                    transform.position;

                if (direction.sqrMagnitude > 0.0001f)
                    transform.rotation =
                        Quaternion.LookRotation(direction.normalized);
            }

            float pulse =
                1f +
                Mathf.Sin(Time.time * 3.2f) *
                0.08f;

            marker.transform.localScale =
                baseScale * pulse;

            marker.color =
                garage.IsOpen
                    ? new Color(0.95f, 0.55f, 1f, 1f)
                    : new Color(0.72f, 0.2f, 1f, 1f);
        }
    }
}
