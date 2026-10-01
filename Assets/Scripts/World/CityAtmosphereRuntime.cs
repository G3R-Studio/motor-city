using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class CityAtmosphereRuntime : MonoBehaviour
    {
        private DayNightCycleController dayNight;
        private Transform observer;
        private ParticleSystem haze;
        private ParticleSystemRenderer hazeRenderer;
        private Material hazeMaterial;
        private float resolveTimer;

        public void Initialize(DayNightCycleController cycle)
        {
            dayNight = cycle;
            // Particle haze is intentionally disabled. The imported fog particle
            // material renders as visible billboards in the current URP/WebGL setup.
            // Distance fog provides the atmospheric depth without transparent overdraw.
        }

        private void OnDestroy()
        {
            if (hazeMaterial != null)
                Destroy(hazeMaterial);
        }

        private void LateUpdate()
        {
            ResolveObserver();
            if (observer != null)
            {
                Vector3 p = observer.position;
                transform.position = new Vector3(p.x, p.y + 1.2f, p.z);
            }

            ApplyAtmosphere();
        }

        private void ResolveObserver()
        {
            if (observer != null)
                return;

            resolveTimer -= Time.unscaledDeltaTime;
            if (resolveTimer > 0f)
                return;
            resolveTimer = 1f;

            Camera camera = Camera.main;
            if (camera != null)
                observer = camera.transform;
        }

        private void ApplyAtmosphere()
        {
            // Unity's per-material fog is deliberately disabled for the FCG city.
            // The city mixes converted URP materials with legacy/custom content,
            // so shader fog variants produce bright unfogged islands at distance.
            // Keep the frame clean until atmosphere is applied as a uniform
            // screen-space/depth pass instead of per material.
            RenderSettings.fog = false;
        }

        private void BuildHaze()
        {
            GameObject go = new("Motor City Local Haze");
            go.transform.SetParent(transform, false);

            haze = go.AddComponent<ParticleSystem>();
            hazeRenderer = go.GetComponent<ParticleSystemRenderer>();

            ParticleSystem.MainModule main = haze.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(12f, 18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(18f, 34f);
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = haze.emission;
            emission.rateOverTime = 1.2f;

            ParticleSystem.ShapeModule shape = haze.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(90f, 7f, 90f);

            ParticleSystem.VelocityOverLifetimeModule velocity = haze.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.06f);

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            if (shader != null)
            {
                hazeMaterial = new Material(shader);
                hazeMaterial.name = "MotorCity Runtime Haze";
                hazeMaterial.color = new Color(0.82f, 0.86f, 0.88f, 0.045f);
                hazeRenderer.sharedMaterial = hazeMaterial;
            }

            hazeRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            hazeRenderer.sortMode = ParticleSystemSortMode.Distance;
            haze.Play();
        }

        private void ApplyQuality()
        {
            if (haze == null)
                return;

            MotorCityQualityPreset preset = MotorCityQualityRuntime.CurrentPreset;
            ParticleSystem.EmissionModule emission = haze.emission;
            ParticleSystem.MainModule main = haze.main;

            switch (preset)
            {
                case MotorCityQualityPreset.Low:
                    emission.enabled = false;
                    main.maxParticles = 0;
                    haze.Clear();
                    break;
                case MotorCityQualityPreset.High:
                    emission.enabled = true;
                    emission.rateOverTime = 1.4f;
                    main.maxParticles = 32;
                    if (!haze.isPlaying) haze.Play();
                    break;
                default:
                    emission.enabled = true;
                    emission.rateOverTime = 0.8f;
                    main.maxParticles = 20;
                    if (!haze.isPlaying) haze.Play();
                    break;
            }
        }
    }
}
