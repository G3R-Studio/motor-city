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

        private float dayNightResolveTimer;
        private ArcadeCarController car;
        private Transform currentVisual;
        private bool anchorsDirty = true;
        private string vehicleId = "street";

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
            RefreshAnchorsIfNeeded();
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
                    IsWheelRenderer(
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
                    IsWheelRenderer(
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

                    // Porsche and Peugeot expose dedicated headlight
                    // materials. Drive their real material emission at night
                    // instead of relying only on invisible Spot Lights.
                    if (materialName.Contains("headlight") ||
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

                    if (filter == null ||
                        filter.sharedMesh == null ||
                        starterLampShader == null ||
                        !starterLampShader.isSupported)
                    {
                        continue;
                    }

                    bool useMaskedFrontOverlay =
                        (vehicleId == "amggt" &&
                         materialName.Contains("amggtemission")) ||
                        (vehicleId == "camaro" &&
                         (materialName.Contains("camarobloom") ||
                          materialName.Contains("color_bloom") ||
                          materialName.Contains("bloom"))) ||
                        (vehicleId == "bus" &&
                         materialName.Contains("busatlas"));

                    if (!useMaskedFrontOverlay)
                        continue;

                    Texture texture =
                        ResolveBaseTexture(
                            source);

                    if (texture == null)
                        continue;

                    Vector3 forwardAxis =
                        renderer.transform
                            .InverseTransformDirection(
                                transform.forward)
                            .normalized;

                    ResolveProjectionRange(
                        filter.sharedMesh.bounds,
                        forwardAxis,
                        out float minimum,
                        out float maximum);

                    float cutoffFraction =
                        vehicleId == "bus"
                            ? 0.93f
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

                    ResolveProjectionRange(
                        filter.sharedMesh.bounds,
                        lateralAxis,
                        out float lateralMinimum,
                        out float lateralMaximum);

                    ResolveProjectionRange(
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

                    CopyTextureTransform(
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
                        vehicleId == "bus";

                    overlayMaterial.SetFloat(
                        "_SpatialMask",
                        useSpatialMask
                            ? 1f
                            : 0f);

                    if (useSpatialMask)
                    {
                        // The bus uses one bright palette over the whole body.
                        // Restrict the overlay to the two low outer headlamp
                        // areas instead of letting the white fascia/roof glow.
                        overlayMaterial.SetFloat(
                            "_LateralMin",
                            lateralMaxAbs * 0.38f);

                        overlayMaterial.SetFloat(
                            "_LateralMax",
                            lateralMaxAbs * 0.68f);

                        overlayMaterial.SetFloat(
                            "_UpMin",
                            upMinimum +
                            upSpan * 0.16f);

                        overlayMaterial.SetFloat(
                            "_UpMax",
                            upMinimum +
                            upSpan * 0.31f);
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

            if (vehicleId != "delorean" ||
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
                        ResolveBaseTexture(
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

                    CopyTextureTransform(
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

                material.EnableKeyword(
                    "_EMISSION");
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

        private static Texture ResolveBaseTexture(
            Material material)
        {
            if (material == null)
                return null;

            if (material.HasProperty(
                    "_BaseMap"))
            {
                Texture texture =
                    material.GetTexture(
                        "_BaseMap");

                if (texture != null)
                    return texture;
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

        private static void ResolveProjectionRange(
            Bounds bounds,
            Vector3 axis,
            out float minimum,
            out float maximum)
        {
            minimum = float.PositiveInfinity;
            maximum = float.NegativeInfinity;

            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner =
                            center +
                            Vector3.Scale(
                                extents,
                                new Vector3(x, y, z));

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

        private void OnDestroy()
        {
            ClearNightEmissionOverlays();
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
                    7f,
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
                    0.88f,
                    0.93f,
                    1f);

            light.range =
                46f;

            light.spotAngle =
                54f;

            light.innerSpotAngle =
                28f;

            light.bounceIntensity =
                0f;

            light.cullingMask =
                ~0;

            light.enabled =
                false;

            return light;
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
                    8.0f,
                    10.2f,
                    speed01) *
                amount *
                qualityIntensity;

            light.range =
                Mathf.Lerp(
                    46f,
                    72f,
                    speed01) *
                qualityRange;

            light.spotAngle =
                Mathf.Lerp(
                    54f,
                    44f,
                    speed01);

            light.innerSpotAngle =
                Mathf.Lerp(
                    28f,
                    24f,
                    speed01);
        }
    }
}
