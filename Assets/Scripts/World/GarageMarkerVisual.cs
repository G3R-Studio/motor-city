using MotorCity.Gameplay;
using MotorCity.UI;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class GarageMarkerVisual : MonoBehaviour
    {
        private GarageUpgradeSystem garage;
        private SpriteRenderer marker;
        private Vector3 baseScale;
        private Camera mainCamera;
        private Transform observer;
        private float observerResolveTimer;

        private const float TargetMarkerSize = 1.85f;
        private const float MarkerHeight = 3.35f;
        private const float FullScaleDistance = 70f;
        private const float FarScaleDistance = 280f;
        private const float MaximumVisibleDistance = 420f;

        public void Bind(GarageUpgradeSystem target)
        {
            garage = target;
            BuildMarker();

            if (garage != null)
                transform.position =
                    garage.GarageCenter + Vector3.up * MarkerHeight;
        }

        private void BuildMarker()
        {
            Sprite sprite =
                MotorCityIconLibrary.Garage;

            if (sprite == null)
            {
                sprite =
                    Resources.Load<Sprite>(
                        "MotorCity/Markers/flag");
            }

            GameObject visual =
                new("Garage Marker Icon");
            visual.transform.SetParent(
                transform,
                false);

            marker =
                visual.AddComponent<SpriteRenderer>();

            if (sprite == null)
            {
                sprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);

                sprite.name =
                    "MotorCity_GarageMarkerFallback";

                visual.transform.localRotation =
                    Quaternion.Euler(0f, 0f, 45f);
            }

            marker.sprite = sprite;
            marker.color =
                new Color(0.72f, 0.2f, 1f, 1f);
            marker.sortingOrder = 200;

            float spriteSize =
                Mathf.Max(
                    sprite.bounds.size.x,
                    sprite.bounds.size.y);

            float normalizedScale =
                TargetMarkerSize /
                Mathf.Max(spriteSize, 0.01f);

            visual.transform.localScale =
                Vector3.one * normalizedScale;

            baseScale =
                visual.transform.localScale;

            mainCamera = Camera.main;
        }

        private void ResolveObserver()
        {
            if (observer != null)
                return;

            observerResolveTimer -=
                Time.unscaledDeltaTime;

            if (observerResolveTimer > 0f)
                return;

            observerResolveTimer =
                1f;

            MotorCity.Vehicle.ArcadeCarController car =
                Object.FindAnyObjectByType<
                    MotorCity.Vehicle.ArcadeCarController>();

            if (car != null)
            {
                observer =
                    car.transform;

                return;
            }

            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null)
            {
                observer =
                    mainCamera.transform;
            }
        }

        private void Update()
        {
            if (garage == null || marker == null)
                return;

            transform.position =
                garage.GarageCenter +
                Vector3.up *
                (MarkerHeight + Mathf.Sin(Time.time * 2.2f) * 0.12f);

            ResolveObserver();

            float distance =
                observer == null
                    ? 0f
                    : Vector3.Distance(
                        observer.position,
                        transform.position);

            bool visible =
                observer == null ||
                distance <=
                MaximumVisibleDistance;

            marker.enabled =
                visible;

            if (!visible)
                return;

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

            float farT =
                observer == null
                    ? 0f
                    : Mathf.InverseLerp(
                        FullScaleDistance,
                        FarScaleDistance,
                        distance);

            float distanceScale =
                Mathf.Lerp(
                    1.08f,
                    0.68f,
                    farT);

            float pulse =
                1f +
                Mathf.Sin(Time.time * 2.8f) *
                0.035f;

            marker.transform.localScale =
                baseScale *
                distanceScale *
                pulse;

            marker.color =
                garage.IsOpen
                    ? new Color(0.95f, 0.55f, 1f, 1f)
                    : new Color(0.72f, 0.2f, 1f, 1f);
        }
    }
}
