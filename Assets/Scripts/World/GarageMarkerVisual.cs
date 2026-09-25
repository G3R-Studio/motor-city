using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class GarageMarkerVisual : MonoBehaviour
    {
        private const string GarageMarkerResourcePath =
            "MotorCity/Markers/ProfessionMarkerVfx";

        private static readonly Color GarageColor =
            new(
                0.72f,
                0.20f,
                1f,
                1f);

        private static readonly Color GarageOpenColor =
            new(
                0.95f,
                0.55f,
                1f,
                1f);

        private const float MarkerRootHeight =
            0.12f;

        private const float MaximumVisibleDistance =
            230f;

        private GarageUpgradeSystem garage;
        private GameObject markerVfx;
        private SpriteRenderer[] markerSprites;
        private ParticleSystem[] markerParticles;
        private Transform observer;
        private float observerResolveTimer;

        public void Bind(
            GarageUpgradeSystem target)
        {
            garage =
                target;

            BuildMarker();

            SnapToGarage();

            RefreshVisuals();
        }

        private void BuildMarker()
        {
            GameObject prefab =
                Resources.Load<GameObject>(
                    GarageMarkerResourcePath);

            if (prefab == null)
            {
                Debug.LogWarning(
                    "[MotorCity][GarageMarker] Missing " +
                    GarageMarkerResourcePath);

                return;
            }

            markerVfx =
                Instantiate(
                    prefab,
                    transform);

            markerVfx.name =
                "Garage Marker VFX Runtime";

            markerVfx.transform.localPosition =
                Vector3.zero;

            markerVfx.transform.localRotation =
                Quaternion.identity;

            markerVfx.transform.localScale =
                Vector3.one;

            markerSprites =
                markerVfx.GetComponentsInChildren<SpriteRenderer>(
                    true);

            markerParticles =
                markerVfx.GetComponentsInChildren<ParticleSystem>(
                    true);

            ApplyGarageColor(
                false);
        }

        private void SnapToGarage()
        {
            if (garage == null)
                return;

            transform.position =
                garage.GarageCenter +
                Vector3.up *
                MarkerRootHeight;
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

            Camera mainCamera =
                Camera.main;

            if (mainCamera != null)
            {
                observer =
                    mainCamera.transform;
            }
        }

        private void Update()
        {
            if (garage == null)
                return;

            SnapToGarage();

            ResolveObserver();

            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (markerVfx == null)
                return;

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

            if (markerVfx.activeSelf !=
                visible)
            {
                markerVfx.SetActive(
                    visible);
            }

            if (!visible)
                return;

            ApplyGarageColor(
                garage != null &&
                garage.IsOpen);
        }

        private void ApplyGarageColor(
            bool open)
        {
            Color coreColor =
                open
                    ? GarageOpenColor
                    : GarageColor;

            if (markerSprites != null)
            {
                foreach (SpriteRenderer renderer in
                         markerSprites)
                {
                    if (renderer == null)
                        continue;

                    bool glow =
                        renderer.name.IndexOf(
                            "Glow",
                            System.StringComparison.OrdinalIgnoreCase) >=
                        0;

                    renderer.color =
                        glow
                            ? new Color(
                                coreColor.r,
                                coreColor.g,
                                coreColor.b,
                                0.22f)
                            : coreColor;
                }
            }

            if (markerParticles != null)
            {
                foreach (ParticleSystem particles in
                         markerParticles)
                {
                    if (particles == null)
                        continue;

                    ParticleSystem.MainModule main =
                        particles.main;

                    main.startColor =
                        new Color(
                            coreColor.r,
                            coreColor.g,
                            coreColor.b,
                            0.86f);
                }
            }
        }
    }
}
