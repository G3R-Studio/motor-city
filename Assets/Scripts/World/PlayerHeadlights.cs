using System.Collections.Generic;
using MotorCity.Platform;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class PlayerHeadlights : MonoBehaviour
    {
        private DayNightCycleController dayNight;
        private Light left;
        private Light right;
        private const string RuntimeVisualName =
            "MotorCityVehicleVisual_Runtime";

        // Mesh-duplicating lamp overlays are disabled for now. Several imported
        // vehicle meshes share lamp materials with large body/wheel submeshes,
        // causing the overlay shader to render the whole mesh magenta.
        // Real Light components and direct material-emission bindings remain.
        private const bool EnableMeshLampOverlays = false;

        private float dayNightResolveTimer;
        private ArcadeCarController car;
        private Transform currentVisual;
        private bool anchorsDirty = true;
        private string vehicleId = "street";
        private float lastLightAmount = -1f;
        private float lastLightSpeed01 = -1f;
        private float lastEmissionIntensity = -1f;

        private const string DeloreanOverlayName =
            "MotorCityDeloreanNightEmissionOverlay";

        private sealed class NightEmissionOverlay
        {
            public GameObject Root;
            public Material Material;
        }

        private sealed class FrontLampMaterialBinding
        {
            public Material Material;
        }

        private readonly List<NightEmissionOverlay>
            nightEmissionOverlays = new();

        private readonly List<FrontLampMaterialBinding>
            frontLampMaterials = new();

        private Shader deloreanEmissionShader;
        private Shader starterLampShader;

        private void Awake()
        {
            deloreanEmissionShader =
                Resources.Load<Shader>(
                    "MotorCity/Shaders/DeloreanNightEmission");

            starterLampShader =
                Resources.Load<Shader>(
                    "MotorCity/Shaders/StarterLampEmission");

            car =
                GetComponent<ArcadeCarController>();

            MotorCityQualityRuntime.PresetChanged +=
                HandleQualityPresetChanged;

            dayNight =
                Object.FindAnyObjectByType<DayNightCycleController>();

            left =
                CreateHeadlight(
                    "Headlight Left",
                    new Vector3(
                        -0.62f,
                        0.62f,
                        1.88f));

            right =
                CreateHeadlight(
                    "Headlight Right",
                    new Vector3(
                        0.62f,
                        0.62f,
                        1.88f));

            ApplyLights(
                0f);
        }

        private void Update()
        {
            if (dayNight == null)
            {
                dayNightResolveTimer -=
                    Time.unscaledDeltaTime;

                if (dayNightResolveTimer <= 0f)
                {
                    dayNightResolveTimer = 1f;

                    dayNight =
                        Object.FindAnyObjectByType<DayNightCycleController>();
                }
            }

            RefreshAnchorsIfNeeded();

            float night =
                dayNight != null
                    ? dayNight.NightAmount
                    : 0f;

            float amount =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(
                        0.34f,
                        0.72f,
                        night));

            ApplyLights(
                amount);

            ApplyNightVisualEmission(
                night);
        }

        public void SetVehicleId(
            string id)
        {
            vehicleId =
                string.IsNullOrWhiteSpace(id)
                    ? "street"
                    : id.ToLowerInvariant();

            // Positioning stays geometry-based for every vehicle, while some
            // authored visuals (currently Delorean) also expose emissive
            // headlamp/neon geometry that is driven by the night cycle.
            anchorsDirty = true;
            lastLightAmount = -1f;
            lastLightSpeed01 = -1f;
            lastEmissionIntensity = -1f;
            RefreshAnchorsIfNeeded();
            if (currentVisual == null)
                RefreshNightEmissionBindings();
        }

        private void RefreshAnchorsIfNeeded()
        {
            Transform visual =
                transform.Find(
                    RuntimeVisualName);

            if (visual == currentVisual &&
                !anchorsDirty)
            {
                return;
            }

            currentVisual =
                visual;
            anchorsDirty =
                false;

            if (currentVisual == null ||
                left == null ||
                right == null)
            {
                return;
            }

            Renderer[] renderers =
                currentVisual.GetComponentsInChildren<Renderer>(
                    true);

            bool initialized =
                false;

            Bounds localBounds =
                new(
                    Vector3.zero,
                    Vector3.zero);

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    VehicleLampMaterialUtility.IsWheelRenderer(
                        renderer.transform))
                {
                    continue;
                }

                Bounds world =
                    renderer.bounds;

                Vector3 min =
                    world.min;
                Vector3 max =
                    world.max;

                for (int x = 0;
                     x < 2;
                     x++)
                {
                    for (int y = 0;
                         y < 2;
                         y++)
                    {
                        for (int z = 0;
                             z < 2;
                             z++)
                        {
                            Vector3 corner =
                                new(
                                    x == 0
                                        ? min.x
                                        : max.x,
                                    y == 0
                                        ? min.y
                                        : max.y,
                                    z == 0
                                        ? min.z
                                        : max.z);

                            Vector3 local =
                                transform.InverseTransformPoint(
                                    corner);

                            if (!initialized)
                            {
                                localBounds =
                                    new Bounds(
                                        local,
                                        Vector3.zero);

                                initialized =
                                    true;
                            }
                            else
                            {
                                localBounds.Encapsulate(
                                    local);
                            }
                        }
                    }
                }
            }

            if (!initialized)
                return;

            float halfWidth =
                Mathf.Max(
                    0.45f,
                    localBounds.extents.x);

            float xOffset =
                Mathf.Clamp(
                    halfWidth * 0.58f,
                    0.52f,
                    0.92f);

            float lightY =
                Mathf.Lerp(
                    localBounds.min.y,
                    localBounds.max.y,
                    0.34f);

            // Renderer bounds end at the outermost front body surface
            // (usually the bumper). Placing a Spot Light at max.z or beyond it
            // makes the source visibly float in front of the car. Keep both
            // lamps slightly inside the front fascia instead. Scaling the inset
            // with body length works for compact cars, the six-wheel Apex and
            // the longer bus without per-vehicle magic numbers.
            float frontInset =
                Mathf.Clamp(
                    localBounds.size.z * 0.10f,
                    0.20f,
                    0.48f);

            float lightZ =
                localBounds.max.z -
                frontInset;

            left.transform.localPosition =
                new Vector3(
                    -xOffset,
                    lightY,
                    lightZ);

            right.transform.localPosition =
                new Vector3(
                    xOffset,
                    lightY,
                    lightZ);

            RefreshNightEmissionBindings();
        }

        private static bool IsDedicatedVehicleLamp(string id, string materialName)
        {
            string name = materialName.Replace(" (instance)", "").Replace(" (clone)", "").Trim();
            // Hybrid exports its white lamp polygons as Material.005.
            // Beatall has a dedicated colored lamp atlas for front and rear.
            return (id == "hybrid" && name == "material.005") ||
                (id == "beatall" && name == "beatallemission");
        }
        private void RefreshNightEmissionBindings()
        {
            ClearNightEmissionOverlays();
            frontLampMaterials.Clear();

            if (currentVisual == null)
                return;

            Renderer[] renderers =
                currentVisual.GetComponentsInChildren<Renderer>(
                    true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    renderer.gameObject.name ==
                        DeloreanOverlayName ||
                    VehicleLampMaterialUtility.IsWheelRenderer(
                        renderer.transform))
                {
                    continue;
                }

                MeshFilter filter =
                    renderer.GetComponent<MeshFilter>();

                Material[] materials =
                    renderer.sharedMaterials;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material source =
                        materials[i];

                    if (source == null)
                        continue;

                    string materialName =
                        source.name
                            .ToLowerInvariant();

                    bool hybridWhiteLamp = vehicleId == "hybrid" &&
                        IsDedicatedVehicleLamp(vehicleId, materialName);
                    bool beatallLamp = vehicleId == "beatall" && IsDedicatedVehicleLamp(vehicleId, materialName);
                    if ((hybridWhiteLamp || beatallLamp) && source.HasProperty("_EmissionColor"))
                    {
                        // Material.005 is shared by front and rear white strips.
                        // Only the front overlay may emit; the base stays unlit.
                        source.SetColor("_EmissionColor", Color.black);
                        source.DisableKeyword("_EMISSION");
                    }

                    // Porsche and Peugeot expose dedicated headlight
                    // materials. Drive their real material emission at night
                    // instead of relying only on invisible Spot Lights.
                    if ((!hybridWhiteLamp && !beatallLamp && IsDedicatedVehicleLamp(vehicleId, materialName)) ||
                        materialName.Contains("headlight") ||
                        materialName.Contains("headlamp"))
                    {
                        if (source.HasProperty("_EmissionColor"))
                        {
                            source.SetColor(
                                "_EmissionColor",
                                Color.black);

                            source.EnableKeyword(
                                "_EMISSION");

                            frontLampMaterials.Add(
                                new FrontLampMaterialBinding
                                {
                                    Material = source
                                });
                        }

                        continue;
                    }

                    if (!EnableMeshLampOverlays)
                        continue;

                    if (filter == null ||
                        filter.sharedMesh == null ||
                        starterLampShader == null ||
                        !starterLampShader.isSupported)
                    {
                        continue;
                    }

                    bool useMaskedFrontOverlay = hybridWhiteLamp || beatallLamp ||
                        (vehicleId == "amggt" &&
                         (materialName.Contains("amggtemission") ||
                          materialName.Contains("gradientemmisive") ||
                          materialName.Contains("gradientemissive"))) ||
                        (vehicleId == "camaro" &&
                         (materialName.Contains("camarobloom") ||
                          materialName.Contains("color_bloom") ||
                          materialName.Contains("bloom"))) ||
                        (vehicleId == "bus" &&
                         materialName.Contains("busatlas"));

                    if (!useMaskedFrontOverlay)
                        continue;

                    Texture texture =
                        VehicleLampMaterialUtility.ResolveBaseTexture(
                            source);

                    if (hybridWhiteLamp) texture = Texture2D.whiteTexture;
                    if (texture == null)
                        continue;

                    Vector3 forwardAxis =
                        renderer.transform
                            .InverseTransformDirection(
                                transform.forward)
                            .normalized;

                    VehicleLampMaterialUtility.ResolveProjectionRange(
                        filter.sharedMesh.bounds,
                        forwardAxis,
                        out float minimum,
                        out float maximum);

                    float cutoffFraction =
                        vehicleId == "bus"
                            ? 0.70f
                            : vehicleId == "camaro"
                                ? 0.72f
                                : 0.62f;

                    float cutoff =
                        Mathf.Lerp(
                            minimum,
                            maximum,
                            cutoffFraction);

                    float softness =
                        Mathf.Max(
                            0.02f,
                            (maximum - minimum) *
                            0.035f);

                    Vector3 lateralAxis =
                        renderer.transform
                            .InverseTransformDirection(
                                transform.right)
                            .normalized;

                    Vector3 upAxis =
                        renderer.transform
                            .InverseTransformDirection(
                                transform.up)
                            .normalized;

                    VehicleLampMaterialUtility.ResolveProjectionRange(
                        filter.sharedMesh.bounds,
                        lateralAxis,
                        out float lateralMinimum,
                        out float lateralMaximum);

                    VehicleLampMaterialUtility.ResolveProjectionRange(
                        filter.sharedMesh.bounds,
                        upAxis,
                        out float upMinimum,
                        out float upMaximum);

                    float lateralMaxAbs =
                        Mathf.Max(
                            Mathf.Abs(lateralMinimum),
                            Mathf.Abs(lateralMaximum));

                    float upSpan =
                        Mathf.Max(
                            0.001f,
                            upMaximum - upMinimum);

                    Material overlayMaterial =
                        new Material(
                            starterLampShader)
                        {
                            name =
                                "MotorCity_FrontLampEmission_Runtime"
                        };

                    overlayMaterial.SetTexture(
                        "_BaseMap",
                        texture);

                    VehicleLampMaterialUtility.CopyTextureTransform(
                        source,
                        overlayMaterial);

                    overlayMaterial.SetVector(
                        "_AxisOS",
                        new Vector4(
                            forwardAxis.x,
                            forwardAxis.y,
                            forwardAxis.z,
                            0f));

                    overlayMaterial.SetFloat(
                        "_Cutoff",
                        cutoff);

                    overlayMaterial.SetFloat(
                        "_Softness",
                        softness);

                    overlayMaterial.SetVector(
                        "_LateralAxisOS",
                        new Vector4(
                            lateralAxis.x,
                            lateralAxis.y,
                            lateralAxis.z,
                            0f));

                    overlayMaterial.SetVector(
                        "_UpAxisOS",
                        new Vector4(
                            upAxis.x,
                            upAxis.y,
                            upAxis.z,
                            0f));

                    bool useSpatialMask =
                        false;

                    overlayMaterial.SetFloat(
                        "_SpatialMask",
                        useSpatialMask
                            ? 1f
                            : 0f);

                    bool useBusUvMask =
                        vehicleId == "bus";

                    overlayMaterial.SetFloat(
                        "_UvMask",
                        useBusUvMask
                            ? 1f
                            : 0f);

                    if (useBusUvMask)
                    {
                        // bus.obj has dedicated headlamp polygons even though
                        // the whole body shares one Material.001 palette
                        // material. Those two polygons both sample exactly
                        // this palette coordinate, so select that UV island
                        // directly instead of approximating them by body bounds.
                        overlayMaterial.SetVector(
                            "_UvCenter",
                            new Vector4(
                                0.474802f,
                                0.524206f,
                                0f,
                                0f));

                        overlayMaterial.SetVector(
                            "_UvCenter2",
                            new Vector4(
                                0.482f,
                                0.222f,
                                0f,
                                0f));

                        overlayMaterial.SetVector(
                            "_UvTolerance",
                            new Vector4(
                                0.006f,
                                0.006f,
                                0f,
                                0f));
                    }

                    overlayMaterial.SetFloat(
                        "_Mode",
                        1f);

                    overlayMaterial.SetColor(
                        "_EmissionColor",
                        new Color(
                            0.92f,
                            0.96f,
                            1f,
                            1f));

                    overlayMaterial.SetFloat(
                        "_Intensity",
                        0f);

                    GameObject overlay =
                        new GameObject(
                            "MotorCityFrontLampEmission",
                            typeof(MeshFilter),
                            typeof(MeshRenderer));

                    overlay.transform.SetParent(
                        renderer.transform.parent,
                        false);

                    overlay.transform.localPosition =
                        renderer.transform.localPosition;

                    overlay.transform.localRotation =
                        renderer.transform.localRotation;

                    overlay.transform.localScale =
                        renderer.transform.localScale;

                    overlay.GetComponent<MeshFilter>()
                        .sharedMesh =
                        filter.sharedMesh;

                    Material[] overlayMaterials =
                        new Material[
                            materials.Length];

                    overlayMaterials[i] =
                        overlayMaterial;

                    MeshRenderer overlayRenderer =
                        overlay.GetComponent<MeshRenderer>();

                    overlayRenderer.sharedMaterials =
                        overlayMaterials;

                    overlayRenderer.shadowCastingMode =
                        UnityEngine.Rendering.ShadowCastingMode.Off;

                    overlayRenderer.receiveShadows =
                        false;

                    nightEmissionOverlays.Add(
                        new NightEmissionOverlay
                        {
                            Root = overlay,
                            Material = overlayMaterial
                        });
                }
            }

            if (!EnableMeshLampOverlays ||
                vehicleId != "delorean" ||
                deloreanEmissionShader == null ||
                !deloreanEmissionShader.isSupported)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    renderer.gameObject.name ==
                        DeloreanOverlayName ||
                    VehicleLampMaterialUtility.IsWheelRenderer(
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

                Material[] materials =
                    renderer.sharedMaterials;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material source =
                        materials[i];

                    if (source == null)
                        continue;

                    string materialName =
                        source.name
                            .ToLowerInvariant();

                    if (!materialName.Contains(
                            "deloreanemission"))
                    {
                        continue;
                    }

                    Texture texture =
                        VehicleLampMaterialUtility.ResolveBaseTexture(
                            source);

                    if (texture == null)
                        continue;

                    Material overlayMaterial =
                        new Material(
                            deloreanEmissionShader)
                        {
                            name =
                                "MotorCity_DeloreanNightEmission_Runtime"
                        };

                    overlayMaterial.SetTexture(
                        "_BaseMap",
                        texture);

                    VehicleLampMaterialUtility.CopyTextureTransform(
                        source,
                        overlayMaterial);

                    overlayMaterial.SetFloat(
                        "_Intensity",
                        0f);

                    GameObject overlay =
                        new GameObject(
                            DeloreanOverlayName,
                            typeof(MeshFilter),
                            typeof(MeshRenderer));

                    Transform sourceTransform =
                        renderer.transform;

                    overlay.transform.SetParent(
                        sourceTransform.parent,
                        false);

                    overlay.transform.localPosition =
                        sourceTransform.localPosition;

                    overlay.transform.localRotation =
                        sourceTransform.localRotation;

                    overlay.transform.localScale =
                        sourceTransform.localScale;

                    overlay.GetComponent<MeshFilter>()
                        .sharedMesh =
                        filter.sharedMesh;

                    Material[] overlayMaterials =
                        new Material[
                            materials.Length];

                    overlayMaterials[i] =
                        overlayMaterial;

                    overlay.GetComponent<MeshRenderer>()
                        .sharedMaterials =
                        overlayMaterials;

                    nightEmissionOverlays.Add(
                        new NightEmissionOverlay
                        {
                            Root = overlay,
                            Material = overlayMaterial
                        });
                }
            }
        }

        private void ApplyNightVisualEmission(
            float night)
        {
            float intensity =
                Mathf.SmoothStep(
                    0f,
                    2.8f,
                    Mathf.InverseLerp(
                        0.24f,
                        0.66f,
                        night));

            if (Mathf.Abs(
                    intensity -
                    lastEmissionIntensity) <
                    0.01f)
            {
                return;
            }

            lastEmissionIntensity =
                intensity;

            for (int i = 0;
                 i < frontLampMaterials.Count;
                 i++)
            {
                Material material =
                    frontLampMaterials[i]?.Material;

                if (material == null ||
                    !material.HasProperty(
                        "_EmissionColor"))
                {
                    continue;
                }

                material.SetColor(
                    "_EmissionColor",
                    new Color(
                        0.92f,
                        0.96f,
                        1f,
                        1f) *
                    intensity);
            }

            for (int i = 0;
                 i < nightEmissionOverlays.Count;
                 i++)
            {
                NightEmissionOverlay overlay =
                    nightEmissionOverlays[i];

                if (overlay?.Material == null)
                    continue;

                overlay.Material.SetFloat(
                    "_Intensity",
                    intensity);
            }
        }

        private void ClearNightEmissionOverlays()
        {
            for (int i = 0;
                 i < frontLampMaterials.Count;
                 i++)
            {
                Material material =
                    frontLampMaterials[i]?.Material;

                if (material != null &&
                    material.HasProperty(
                        "_EmissionColor"))
                {
                    material.SetColor(
                        "_EmissionColor",
                        Color.black);
                }
            }

            frontLampMaterials.Clear();

            for (int i = 0;
                 i < nightEmissionOverlays.Count;
                 i++)
            {
                NightEmissionOverlay overlay =
                    nightEmissionOverlays[i];

                if (overlay == null)
                    continue;

                if (overlay.Root != null)
                {
                    Destroy(
                        overlay.Root);
                }

                if (overlay.Material != null)
                {
                    Destroy(
                        overlay.Material);
                }
            }

            nightEmissionOverlays.Clear();
        }

        private void OnDestroy()
        {
            MotorCityQualityRuntime.PresetChanged -=
                HandleQualityPresetChanged;

            ClearNightEmissionOverlays();
        }

        private Light CreateHeadlight(
            string lightName,
            Vector3 localPosition)
        {
            GameObject lightObject =
                new(lightName);

            lightObject.transform.SetParent(
                transform,
                false);

            lightObject.transform.localPosition =
                localPosition;

            lightObject.transform.localRotation =
                Quaternion.Euler(
                    5f,
                    0f,
                    0f);

            Light light =
                lightObject.AddComponent<Light>();

            light.type =
                LightType.Spot;

            light.shadows =
                LightShadows.None;

            light.color =
                new Color(
                    0.92f,
                    0.95f,
                    1f);

            light.range =
                54f;

            light.spotAngle =
                50f;

            light.innerSpotAngle =
                30f;

            light.bounceIntensity =
                0f;

            light.cullingMask =
                ~0;

            light.enabled =
                false;

            return light;
        }

        private void HandleQualityPresetChanged()
        {
            lastLightAmount =
                -1f;

            lastLightSpeed01 =
                -1f;

            lastEmissionIntensity =
                -1f;
        }

        private void ApplyLights(
            float amount)
        {
            float speed01 =
                car == null
                    ? 0f
                    : Mathf.InverseLerp(
                        25f,
                        180f,
                        car.SpeedKph);

            if (Mathf.Abs(
                    amount -
                    lastLightAmount) <
                    0.01f &&
                Mathf.Abs(
                    speed01 -
                    lastLightSpeed01) <
                    0.015f)
            {
                return;
            }

            lastLightAmount =
                amount;

            lastLightSpeed01 =
                speed01;

            Configure(
                left,
                amount,
                speed01);

            Configure(
                right,
                amount,
                speed01);
        }

        private static void Configure(
            Light light,
            float amount,
            float speed01)
        {
            if (light == null)
                return;

            light.enabled =
                amount >
                0.02f;

            float qualityIntensity =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low =>
                        0.72f,

                    MotorCityQualityPreset.High =>
                        1.08f,

                    _ =>
                        0.90f
                };

            float qualityRange =
                MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCityQualityPreset.Low =>
                        0.78f,

                    MotorCityQualityPreset.High =>
                        1.08f,

                    _ =>
                        0.92f
                };

            light.intensity =
                Mathf.Lerp(
                    10.0f,
                    13.0f,
                    speed01) *
                amount *
                qualityIntensity;

            light.range =
                Mathf.Lerp(
                    54f,
                    78f,
                    speed01) *
                qualityRange;

            light.spotAngle =
                Mathf.Lerp(
                    50f,
                    42f,
                    speed01);

            light.innerSpotAngle =
                Mathf.Lerp(
                    30f,
                    24f,
                    speed01);

            bool shadowed =
                light.enabled &&
                MotorCityQualityRuntime.CurrentPreset ==
                    MotorCityQualityPreset.High;

            light.shadows =
                shadowed
                    ? LightShadows.Hard
                    : LightShadows.None;

            if (shadowed)
            {
                light.shadowStrength =
                    0.42f;
            }
        }
    }
}
