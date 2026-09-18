using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.Vehicle
{
    [RequireComponent(typeof(ArcadeCarController))]
    public sealed class DriftEffects : MonoBehaviour
    {
        private const int RearWheelCount = 2;

        private ArcadeCarController car;
        private readonly TrailRenderer[] trails =
            new TrailRenderer[RearWheelCount];
        private readonly ParticleSystem[] smoke =
            new ParticleSystem[RearWheelCount];

        private Material trailMaterial;
        private Material smokeMaterial;
        private Vector3 previousCarPosition;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
            previousCarPosition = transform.position;

            trailMaterial = CreateTrailMaterial();
            smokeMaterial = CreateSmokeMaterial();

            for (int i = 0; i < RearWheelCount; i++)
            {
                trails[i] =
                    CreateTrail(
                        i == 0
                            ? "Rear Tire Mark L"
                            : "Rear Tire Mark R");

                smoke[i] =
                    CreateSmoke(
                        i == 0
                            ? "Rear Tire Smoke L"
                            : "Rear Tire Smoke R");
            }
        }

        private void LateUpdate()
        {
            if (car == null)
                return;

            if ((transform.position - previousCarPosition).sqrMagnitude > 225f)
                ClearTrails();

            previousCarPosition = transform.position;

            for (int i = 0; i < RearWheelCount; i++)
            {
                bool grounded =
                    car.TryGetRearGroundHit(
                        i,
                        out WheelHit hit);

                bool sliding =
                    grounded &&
                    car.IsSliding &&
                    Mathf.Abs(hit.sidewaysSlip) >= 0.10f;

                UpdateTrail(i, hit, grounded, sliding);
                UpdateSmoke(i, hit, grounded, sliding);
            }
        }

        public void ClearTrails()
        {
            foreach (TrailRenderer trail in trails)
            {
                if (trail == null)
                    continue;

                trail.emitting = false;
                trail.Clear();
            }
        }

        private void UpdateTrail(
            int index,
            WheelHit hit,
            bool grounded,
            bool sliding)
        {
            TrailRenderer trail = trails[index];
            if (trail == null)
                return;

            if (grounded)
            {
                trail.transform.position =
                    hit.point +
                    hit.normal * 0.055f;

                Vector3 forward =
                    Vector3.ProjectOnPlane(
                        transform.forward,
                        hit.normal);

                if (forward.sqrMagnitude > 0.001f)
                    trail.transform.rotation =
                        Quaternion.LookRotation(
                            hit.normal,
                            forward.normalized);
            }

            trail.emitting =
                grounded &&
                sliding;
        }

        private void UpdateSmoke(
            int index,
            WheelHit hit,
            bool grounded,
            bool sliding)
        {
            ParticleSystem particles = smoke[index];
            if (particles == null)
                return;

            if (grounded)
            {
                particles.transform.position =
                    hit.point +
                    hit.normal * 0.08f;
                particles.transform.rotation =
                    Quaternion.LookRotation(
                        hit.normal,
                        transform.forward);
            }

            ParticleSystem.EmissionModule emission =
                particles.emission;

            emission.rateOverTime =
                sliding
                    ? Mathf.Lerp(
                        10f,
                        48f,
                        car.DriftIntensity)
                    : 0f;
        }

        private TrailRenderer CreateTrail(string name)
        {
            GameObject go = new(name);
            go.transform.SetParent(transform, true);

            TrailRenderer trail =
                go.AddComponent<TrailRenderer>();

            trail.material = trailMaterial;
            trail.time = 5f;
            trail.startWidth = 0.19f;
            trail.endWidth = 0.17f;
            trail.minVertexDistance = 0.14f;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.alignment =
                LineAlignment.TransformZ;
            trail.shadowCastingMode =
                ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            trail.textureMode =
                LineTextureMode.Stretch;

            Gradient color = new();
            color.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(0.055f, 0.055f, 0.06f),
                        0f),
                    new GradientColorKey(
                        new Color(0.085f, 0.085f, 0.09f),
                        1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.88f, 0f),
                    new GradientAlphaKey(0.62f, 1f)
                });

            trail.colorGradient = color;
            return trail;
        }

        private ParticleSystem CreateSmoke(string name)
        {
            GameObject go = new(name);
            go.transform.SetParent(transform, true);

            ParticleSystem particles =
                go.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main =
                particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace =
                ParticleSystemSimulationSpace.World;
            main.startLifetime =
                new ParticleSystem.MinMaxCurve(
                    0.75f,
                    1.35f);
            main.startSpeed =
                new ParticleSystem.MinMaxCurve(
                    0.25f,
                    1.15f);
            main.startSize =
                new ParticleSystem.MinMaxCurve(
                    0.32f,
                    0.72f);
            main.startRotation =
                new ParticleSystem.MinMaxCurve(
                    0f,
                    Mathf.PI * 2f);
            main.startColor =
                new ParticleSystem.MinMaxGradient(
                    new Color(
                        0.68f,
                        0.69f,
                        0.7f,
                        0.48f),
                    new Color(
                        0.9f,
                        0.9f,
                        0.9f,
                        0.34f));
            main.gravityModifier = -0.035f;
            main.maxParticles = 180;

            ParticleSystem.EmissionModule emission =
                particles.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape =
                particles.shape;
            shape.shapeType =
                ParticleSystemShapeType.Cone;
            shape.angle = 16f;
            shape.radius = 0.12f;

            ParticleSystem.ColorOverLifetimeModule color =
                particles.colorOverLifetime;
            color.enabled = true;

            Gradient smokeGradient = new();
            smokeGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(
                        new Color(0.72f, 0.73f, 0.74f),
                        0f),
                    new GradientColorKey(
                        new Color(0.82f, 0.82f, 0.82f),
                        1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.45f, 0f),
                    new GradientAlphaKey(0.18f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });

            color.color =
                new ParticleSystem.MinMaxGradient(
                    smokeGradient);

            ParticleSystem.SizeOverLifetimeModule size =
                particles.sizeOverLifetime;
            size.enabled = true;
            size.size =
                new ParticleSystem.MinMaxCurve(
                    1f,
                    AnimationCurve.EaseInOut(
                        0f,
                        0.45f,
                        1f,
                        1.45f));

            ParticleSystemRenderer renderer =
                particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode =
                ParticleSystemRenderMode.Billboard;
            renderer.material = smokeMaterial;
            renderer.shadowCastingMode =
                ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            particles.Play();
            return particles;
        }

        private static Material CreateTrailMaterial()
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            Material material =
                new(shader);

            Color color =
                new(0.025f, 0.025f, 0.028f, 0.88f);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);

            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat(
                    "_Cull",
                    (float)CullMode.Off);
            }

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");

            material.SetOverrideTag(
                "RenderType",
                "Transparent");

            material.renderQueue =
                (int)RenderQueue.Transparent + 1;

            return material;
        }

        private static Material CreateSmokeMaterial()
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Particles/Unlit");

            if (shader == null)
                shader =
                    Shader.Find(
                        "Particles/Standard Unlit");

            if (shader == null)
                shader =
                    Shader.Find(
                        "Sprites/Default");

            Material material =
                new(shader);

            Texture2D texture =
                CreateSoftParticleTexture();

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.renderQueue = 3000;
            return material;
        }

        private static Texture2D CreateSoftParticleTexture()
        {
            const int size = 32;
            Texture2D texture =
                new(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false);

            texture.name =
                "MotorCity_RuntimeSmoke";
            texture.wrapMode =
                TextureWrapMode.Clamp;
            texture.filterMode =
                FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx =
                        (x + 0.5f) / size * 2f - 1f;
                    float ny =
                        (y + 0.5f) / size * 2f - 1f;
                    float distance =
                        Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha =
                        Mathf.Clamp01(1f - distance);
                    alpha =
                        alpha * alpha *
                        (3f - 2f * alpha);

                    texture.SetPixel(
                        x,
                        y,
                        new Color(
                            1f,
                            1f,
                            1f,
                            alpha));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
