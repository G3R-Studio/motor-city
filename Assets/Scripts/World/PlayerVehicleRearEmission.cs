using System.Collections.Generic;
using MotorCity.Input;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    public sealed class PlayerVehicleRearEmission :
        MonoBehaviour
    {
        private const string RuntimeVisualName =
            "MotorCityVehicleVisual_Runtime";

        private const string OverlayName =
            "MotorCityRearLampEmission";

        private readonly List<Material> runtimeMaterials =
            new();

        private ArcadeCarController car;
        private DayNightCycleController dayNight;
        private Transform currentVisual;
        private Shader emissionShader;
        private float dayNightResolveTimer;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();

            emissionShader =
                Resources.Load<Shader>(
                    "MotorCity/Shaders/RearLampEmission");
        }

        private void Start()
        {
            RefreshVisual();
        }

        private void Update()
        {
            Transform visual =
                transform.Find(
                    RuntimeVisualName);

            if (visual != currentVisual)
            {
                RefreshVisual();
            }

            ResolveDayNight();

            float night =
                dayNight == null
                    ? 0f
                    : dayNight.NightAmount;

            bool braking =
                MotorCityInput.ReverseHeld ||
                (car != null &&
                 car.HandbrakeInputHeld);

            float runningIntensity =
                Mathf.Lerp(
                    0.14f,
                    0.62f,
                    night);

            float targetIntensity =
                braking
                    ? Mathf.Lerp(
                        2.4f,
                        3.6f,
                        night)
                    : runningIntensity;

            for (int i = 0;
                 i < runtimeMaterials.Count;
                 i++)
            {
                Material material =
                    runtimeMaterials[i];

                if (material == null)
                    continue;

                material.SetFloat(
                    "_Intensity",
                    targetIntensity);
            }
        }

        private void OnDestroy()
        {
            ClearRuntimeMaterials();
        }

        public void RefreshVisual()
        {
            ClearRuntimeMaterials();

            currentVisual =
                transform.Find(
                    RuntimeVisualName);

            if (currentVisual == null ||
                emissionShader == null)
            {
                return;
            }

            RemoveExistingOverlays();

            MeshRenderer[] renderers =
                currentVisual.GetComponentsInChildren<
                    MeshRenderer>(
                    true);

            foreach (MeshRenderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    IsWheelRenderer(
                        renderer.transform))
                {
                    continue;
                }

                MeshFilter filter =
                    renderer.GetComponent<MeshFilter>();

                if (filter == null ||
                    filter.sharedMesh == null)
                {
                    continue;
                }

                CreateEmissionOverlay(
                    renderer,
                    filter.sharedMesh);
            }
        }

        private void CreateEmissionOverlay(
            MeshRenderer sourceRenderer,
            Mesh mesh)
        {
            Material[] sourceMaterials =
                sourceRenderer.sharedMaterials;

            if (sourceMaterials == null ||
                sourceMaterials.Length == 0)
            {
                return;
            }

            GameObject overlay =
                new(
                    OverlayName,
                    typeof(MeshFilter),
                    typeof(MeshRenderer));

            overlay.transform.SetParent(
                sourceRenderer.transform,
                false);

            overlay.transform.localPosition =
                Vector3.zero;

            overlay.transform.localRotation =
                Quaternion.identity;

            overlay.transform.localScale =
                Vector3.one;

            MeshFilter overlayFilter =
                overlay.GetComponent<MeshFilter>();

            overlayFilter.sharedMesh =
                mesh;

            MeshRenderer overlayRenderer =
                overlay.GetComponent<MeshRenderer>();

            overlayRenderer.shadowCastingMode =
                ShadowCastingMode.Off;

            overlayRenderer.receiveShadows =
                false;

            overlayRenderer.lightProbeUsage =
                LightProbeUsage.Off;

            overlayRenderer.reflectionProbeUsage =
                ReflectionProbeUsage.Off;

            overlayRenderer.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;

            Vector3 rearAxis =
                sourceRenderer.transform
                    .InverseTransformDirection(
                        -transform.forward)
                    .normalized;

            ResolveProjectionRange(
                mesh.bounds,
                rearAxis,
                out float minimum,
                out float maximum);

            float span =
                Mathf.Max(
                    0.001f,
                    maximum -
                    minimum);

            float cutoff =
                Mathf.Lerp(
                    minimum,
                    maximum,
                    0.64f);

            float softness =
                Mathf.Max(
                    0.015f,
                    span *
                    0.045f);

            Material[] overlayMaterials =
                new Material[
                    sourceMaterials.Length];

            for (int i = 0;
                 i < sourceMaterials.Length;
                 i++)
            {
                Material source =
                    sourceMaterials[i];

                Material material =
                    new(
                        emissionShader)
                    {
                        name =
                            "MotorCity_RearLampEmission_Runtime"
                    };

                Texture texture =
                    ResolveBaseTexture(
                        source);

                if (texture != null)
                {
                    material.SetTexture(
                        "_BaseMap",
                        texture);

                    CopyTextureTransform(
                        source,
                        material);
                }

                material.SetVector(
                    "_RearAxisOS",
                    new Vector4(
                        rearAxis.x,
                        rearAxis.y,
                        rearAxis.z,
                        0f));

                material.SetFloat(
                    "_RearCutoff",
                    cutoff);

                material.SetFloat(
                    "_RearSoftness",
                    softness);

                material.SetColor(
                    "_EmissionColor",
                    new Color(
                        1f,
                        0.025f,
                        0.012f,
                        1f));

                material.SetFloat(
                    "_Intensity",
                    0.2f);

                runtimeMaterials.Add(
                    material);

                overlayMaterials[i] =
                    material;
            }

            overlayRenderer.sharedMaterials =
                overlayMaterials;
        }

        private void RemoveExistingOverlays()
        {
            if (currentVisual == null)
                return;

            Transform[] transforms =
                currentVisual.GetComponentsInChildren<
                    Transform>(
                    true);

            foreach (Transform item in
                     transforms)
            {
                if (item == null ||
                    item == currentVisual ||
                    item.name !=
                    OverlayName)
                {
                    continue;
                }

                item.gameObject.SetActive(
                    false);

                Destroy(
                    item.gameObject);
            }
        }

        private void ClearRuntimeMaterials()
        {
            for (int i = 0;
                 i < runtimeMaterials.Count;
                 i++)
            {
                Material material =
                    runtimeMaterials[i];

                if (material != null)
                {
                    Destroy(
                        material);
                }
            }

            runtimeMaterials.Clear();
        }

        private void ResolveDayNight()
        {
            if (dayNight != null)
                return;

            dayNightResolveTimer -=
                Time.unscaledDeltaTime;

            if (dayNightResolveTimer > 0f)
                return;

            dayNightResolveTimer =
                1f;

            dayNight =
                Object.FindAnyObjectByType<
                    DayNightCycleController>();
        }

        private static Texture ResolveBaseTexture(
            Material material)
        {
            if (material == null)
                return null;

            if (material.HasProperty(
                    "_BaseMap"))
            {
                Texture baseMap =
                    material.GetTexture(
                        "_BaseMap");

                if (baseMap != null)
                    return baseMap;
            }

            if (material.HasProperty(
                    "_MainTex"))
            {
                return
                    material.GetTexture(
                        "_MainTex");
            }

            return null;
        }

        private static void CopyTextureTransform(
            Material source,
            Material destination)
        {
            if (source == null ||
                destination == null)
            {
                return;
            }

            string property =
                source.HasProperty(
                    "_BaseMap")
                    ? "_BaseMap"
                    : "_MainTex";

            if (!source.HasProperty(
                    property))
            {
                return;
            }

            destination.SetTextureScale(
                "_BaseMap",
                source.GetTextureScale(
                    property));

            destination.SetTextureOffset(
                "_BaseMap",
                source.GetTextureOffset(
                    property));
        }

        private static bool IsWheelRenderer(
            Transform item)
        {
            Transform cursor =
                item;

            while (cursor != null)
            {
                string name =
                    cursor.name
                        .ToLowerInvariant();

                if (name.Contains("wheel") ||
                    name.Contains("tire") ||
                    name.Contains("tyre") ||
                    name.Contains("rim"))
                {
                    return true;
                }

                cursor =
                    cursor.parent;
            }

            return false;
        }

        private static void ResolveProjectionRange(
            Bounds bounds,
            Vector3 axis,
            out float minimum,
            out float maximum)
        {
            minimum =
                float.PositiveInfinity;

            maximum =
                float.NegativeInfinity;

            Vector3 center =
                bounds.center;

            Vector3 extents =
                bounds.extents;

            for (int x = -1;
                 x <= 1;
                 x += 2)
            {
                for (int y = -1;
                     y <= 1;
                     y += 2)
                {
                    for (int z = -1;
                         z <= 1;
                         z += 2)
                    {
                        Vector3 corner =
                            center +
                            Vector3.Scale(
                                extents,
                                new Vector3(
                                    x,
                                    y,
                                    z));

                        float projection =
                            Vector3.Dot(
                                corner,
                                axis);

                        minimum =
                            Mathf.Min(
                                minimum,
                                projection);

                        maximum =
                            Mathf.Max(
                                maximum,
                                projection);
                    }
                }
            }
        }
    }
}
