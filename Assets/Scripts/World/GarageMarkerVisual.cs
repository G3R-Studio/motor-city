using MotorCity.Gameplay;
using MotorCity.UI;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class GarageMarkerVisual : MonoBehaviour
    {
        private GarageUpgradeSystem garage;
        private SpriteRenderer marker;
        private SpriteRenderer markerShadow;
        private SpriteRenderer markerHighlight;
        private SpriteRenderer markerGlow;
        private Vector3 baseScale;
        private Camera mainCamera;
        private Transform observer;
        private float observerResolveTimer;

        private const float TargetMarkerSize = 0.82f;
        private const float MarkerHeight = 1.55f;
        private const float FullScaleDistance = 38f;
        private const float FarScaleDistance = 160f;
        private const float MaximumVisibleDistance = 230f;

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

            markerGlow =
                CreateLayer(
                    visual.transform,
                    "Garage Marker Glow",
                    sprite,
                    new Color(
                        0.78f,
                        0.28f,
                        1f,
                        0.18f),
                    new Vector3(
                        0f,
                        -0.015f,
                        0.03f),
                    1.22f,
                    197);

            markerShadow =
                CreateLayer(
                    visual.transform,
                    "Garage Marker Shadow",
                    sprite,
                    new Color(
                        0.12f,
                        0.035f,
                        0.18f,
                        0.82f),
                    new Vector3(
                        0.045f,
                        -0.055f,
                        0.02f),
                    1.02f,
                    198);

            marker =
                CreateLayer(
                    visual.transform,
                    "Garage Marker Core",
                    sprite,
                    new Color(
                        0.72f,
                        0.20f,
                        1f,
                        1f),
                    Vector3.zero,
                    1f,
                    200);

            markerHighlight =
                CreateLayer(
                    visual.transform,
                    "Garage Marker Highlight",
                    sprite,
                    new Color(
                        1f,
                        0.72f,
                        1f,
                        0.22f),
                    new Vector3(
                        -0.025f,
                        0.035f,
                        -0.01f),
                    0.98f,
                    201);

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

        private static SpriteRenderer CreateLayer(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            Vector3 localPosition,
            float scale,
            int sortingOrder)
        {
            GameObject layer =
                new(name);

            layer.transform.SetParent(
                parent,
                false);

            layer.transform.localPosition =
                localPosition;

            layer.transform.localRotation =
                Quaternion.identity;

            layer.transform.localScale =
                Vector3.one * scale;

            SpriteRenderer renderer =
                layer.AddComponent<SpriteRenderer>();

            renderer.sprite =
                sprite;

            renderer.color =
                color;

            renderer.sortingOrder =
                sortingOrder;

            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            renderer.receiveShadows =
                false;

            return renderer;
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

            if (markerShadow != null)
                markerShadow.enabled =
                    visible;

            if (markerHighlight != null)
                markerHighlight.enabled =
                    visible;

            if (markerGlow != null)
                markerGlow.enabled =
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

            bool open =
                garage.IsOpen;

            marker.color =
                open
                    ? new Color(
                        0.95f,
                        0.55f,
                        1f,
                        1f)
                    : new Color(
                        0.72f,
                        0.20f,
                        1f,
                        1f);

            if (markerShadow != null)
            {
                markerShadow.color =
                    open
                        ? new Color(
                            0.24f,
                            0.08f,
                            0.30f,
                            0.88f)
                        : new Color(
                            0.12f,
                            0.035f,
                            0.18f,
                            0.82f);
            }

            if (markerHighlight != null)
            {
                markerHighlight.color =
                    open
                        ? new Color(
                            1f,
                            0.86f,
                            1f,
                            0.30f)
                        : new Color(
                            1f,
                            0.72f,
                            1f,
                            0.22f);
            }

            if (markerGlow != null)
            {
                float glowPulse =
                    0.16f +
                    (Mathf.Sin(
                        Time.time * 3.1f) +
                     1f) *
                    0.045f;

                markerGlow.color =
                    open
                        ? new Color(
                            0.98f,
                            0.55f,
                            1f,
                            glowPulse + 0.07f)
                        : new Color(
                            0.78f,
                            0.28f,
                            1f,
                            glowPulse);
            }
        }
    }
}
