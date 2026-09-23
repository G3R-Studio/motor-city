using System;
using System.Collections.Generic;
using MotorCity.Input;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Drives emission on the lamp geometry/materials that already exist in the
    /// selected vehicle model. No extra quads, boxes or replacement lamp meshes
    /// are created.
    /// </summary>
    public sealed class PlayerVehicleRearEmission : MonoBehaviour
    {
        private const string RuntimeVisualName =
            "MotorCityVehicleVisual_Runtime";

        // Name used by the old implementation that projected red geometry over
        // the rear of every renderer. Keep this only so old runtime objects are
        // cleaned up when entering play mode after the fix.
        private const string LegacyOverlayName =
            "MotorCityRearLampEmission";

        private sealed class LampBinding
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public Material Material;
            public Material OriginalMaterial;
            public Color BaseEmission;
            public bool RearSpecific;
        }

        private readonly List<LampBinding> lampBindings =
            new();

        private readonly List<Material> runtimeMaterials =
            new();

        private ArcadeCarController car;
        private DayNightCycleController dayNight;
        private Transform currentVisual;
        private Shader maskedRearShader;
        private string vehicleId = "street";
        private float dayNightResolveTimer;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();

            maskedRearShader =
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

            float brakeMultiplier =
                Mathf.Lerp(
                    1.8f,
                    3.0f,
                    night);

            for (int i = 0;
                 i < lampBindings.Count;
                 i++)
            {
                LampBinding binding =
                    lampBindings[i];

                if (binding == null ||
                    binding.Material == null)
                {
                    continue;
                }

                // Rear lamps are brake lights: no permanent running glow.
                // This also fixes the starter ARCADE car, whose shared
                // emissive material previously made the tail lamps glow all
                // the time.
                float multiplier =
                    braking
                        ? brakeMultiplier
                        : 0f;

                Color emission =
                    binding.BaseEmission *
                    multiplier;

                binding.Material.SetColor(
                    "_EmissionColor",
                    emission);

                binding.Material.EnableKeyword(
                    "_EMISSION");
            }
        }

        private void OnDestroy()
        {
            ClearRuntimeMaterials();
        }

        public void SetVehicleId(
            string id)
        {
            vehicleId =
                string.IsNullOrWhiteSpace(id)
                    ? "street"
                    : id.ToLowerInvariant();

            RefreshVisual();
        }

        public void RefreshVisual()
        {
            RestoreAndClearBindings();

            currentVisual =
                transform.Find(
                    RuntimeVisualName);

            if (currentVisual == null)
                return;

            RemoveLegacyOverlays();

            Renderer[] renderers =
                currentVisual.GetComponentsInChildren<
                    Renderer>(
                    true);

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    IsWheelRenderer(
                        renderer.transform) ||
                    IsMirrorRenderer(
                        renderer.transform))
                {
                    continue;
                }

                BindExistingLampMaterials(
                    renderer);
            }

            // The starter ARCADE car already contains a proper emissive light
            // submesh/material. Never put the texture-mask fallback over any
            // of its other parts (mirrors, spoiler, body, etc.).
            //
            // PolyPack garage cars use one atlas/material for the whole body,
            // so only those cars reach this fallback path.
            if (lampBindings.Count == 0)
            {
                foreach (Renderer renderer in
                         renderers)
                {
                    if (renderer == null ||
                        IsWheelRenderer(
                            renderer.transform))
                    {
                        continue;
                    }

                    CreateTexturedRearLampOverlay(
                        renderer);
                }
            }
        }

        private void BindExistingLampMaterials(
            Renderer renderer)
        {
            Material[] sourceMaterials =
                renderer.sharedMaterials;

            if (sourceMaterials == null ||
                sourceMaterials.Length == 0)
            {
                return;
            }

            string rendererName =
                BuildHierarchyName(
                    renderer.transform,
                    currentVisual);

            bool rendererRearSpecific =
                LooksLikeRearLampName(
                    rendererName);

            Material[] assigned =
                null;

            for (int i = 0;
                 i < sourceMaterials.Length;
                 i++)
            {
                Material source =
                    sourceMaterials[i];

                if (source == null)
                    continue;

                string materialName =
                    source.name == null
                        ? string.Empty
                        : source.name.ToLowerInvariant();

                bool materialRearSpecific =
                    LooksLikeRearLampName(
                        materialName);

                bool genericLampMaterial =
                    LooksLikeLampMaterial(
                        materialName,
                        source);

                if (!rendererRearSpecific &&
                    !materialRearSpecific &&
                    !genericLampMaterial)
                {
                    continue;
                }

                bool rearSpecific =
                    rendererRearSpecific ||
                    materialRearSpecific;

                Material runtime =
                    new(source)
                    {
                        name =
                            source.name +
                            "_MotorCityLampRuntime"
                    };

                ConfigureEmission(
                    runtime,
                    source,
                    rearSpecific);

                if (assigned == null)
                {
                    assigned =
                        (Material[])sourceMaterials.Clone();
                }

                assigned[i] =
                    runtime;

                runtimeMaterials.Add(
                    runtime);

                lampBindings.Add(
                    new LampBinding
                    {
                        Renderer = renderer,
                        MaterialIndex = i,
                        Material = runtime,
                        OriginalMaterial = source,
                        BaseEmission =
                            ResolveBaseEmission(
                                runtime,
                                source),
                        RearSpecific =
                            rearSpecific
                    });
            }

            if (assigned != null)
            {
                renderer.sharedMaterials =
                    assigned;
            }
        }

        private static void ConfigureEmission(
            Material runtime,
            Material source,
            bool rearSpecific)
        {
            if (runtime == null ||
                source == null)
            {
                return;
            }

            Texture emissionMap =
                ResolveEmissionTexture(
                    source,
                    rearSpecific);

            if (emissionMap != null &&
                runtime.HasProperty(
                    "_EmissionMap"))
            {
                runtime.SetTexture(
                    "_EmissionMap",
                    emissionMap);
            }

            Color sourceEmission =
                ResolveSourceEmission(
                    source);

            if (sourceEmission.maxColorComponent <=
                0.001f)
            {
                sourceEmission =
                    rearSpecific
                        ? new Color(
                            1f,
                            0.08f,
                            0.035f,
                            1f)
                        : Color.white;
            }

            if (runtime.HasProperty(
                    "_EmissionColor"))
            {
                runtime.SetColor(
                    "_EmissionColor",
                    sourceEmission);
            }

            runtime.EnableKeyword(
                "_EMISSION");

            if (runtime.HasProperty(
                    "_Surface"))
            {
                // Existing lamp geometry should remain opaque. We only change
                // its emissive response, never its shape or placement.
                runtime.SetFloat(
                    "_Surface",
                    0f);
            }
        }

        private static Texture ResolveEmissionTexture(
            Material material,
            bool rearSpecific)
        {
            if (material == null)
                return null;

            if (material.HasProperty(
                    "_EmissionMap"))
            {
                Texture map =
                    material.GetTexture(
                        "_EmissionMap");

                if (map != null)
                    return map;
            }

            // Some imported car packs store lamp colour directly in the base
            // texture and only flag the material as emissive.
            if (material.HasProperty(
                    "_BaseMap"))
            {
                Texture map =
                    material.GetTexture(
                        "_BaseMap");

                if (map != null &&
                    (rearSpecific ||
                     LooksLikeEmissionMaterial(
                         material)))
                {
                    return map;
                }
            }

            if (material.HasProperty(
                    "_MainTex"))
            {
                Texture map =
                    material.GetTexture(
                        "_MainTex");

                if (map != null &&
                    (rearSpecific ||
                     LooksLikeEmissionMaterial(
                         material)))
                {
                    return map;
                }
            }

            return null;
        }

        private static Color ResolveBaseEmission(
            Material runtime,
            Material source)
        {
            Color emission =
                ResolveSourceEmission(
                    runtime);

            if (emission.maxColorComponent >
                0.001f)
            {
                return emission;
            }

            emission =
                ResolveSourceEmission(
                    source);

            return emission.maxColorComponent >
                   0.001f
                ? emission
                : Color.white;
        }

        private static Color ResolveSourceEmission(
            Material material)
        {
            if (material != null &&
                material.HasProperty(
                    "_EmissionColor"))
            {
                return
                    material.GetColor(
                        "_EmissionColor");
            }

            return Color.black;
        }

        private static bool LooksLikeLampMaterial(
            string materialName,
            Material material)
        {
            if (LooksLikeRearLampName(
                    materialName))
            {
                return true;
            }

            if (materialName.Contains(
                    "lamp") ||
                materialName.Contains(
                    "light"))
            {
                return true;
            }

            // Asset packs commonly use names such as AFRC_Emission for the
            // model's actual light submesh. Preserve that submesh instead of
            // drawing substitute rectangles.
            if (materialName.Contains(
                    "emission") ||
                materialName.Contains(
                    "emissive"))
            {
                return true;
            }

            return
                LooksLikeEmissionMaterial(
                    material);
        }

        private static bool LooksLikeEmissionMaterial(
            Material material)
        {
            if (material == null)
                return false;

            if (material.IsKeywordEnabled(
                    "_EMISSION"))
            {
                return true;
            }

            if (material.HasProperty(
                    "_EmissionMap") &&
                material.GetTexture(
                    "_EmissionMap") != null)
            {
                return true;
            }

            if (material.HasProperty(
                    "_EmissionColor"))
            {
                Color emission =
                    material.GetColor(
                        "_EmissionColor");

                return
                    emission.maxColorComponent >
                    0.05f;
            }

            return false;
        }

        private static bool LooksLikeRearLampName(
            string value)
        {
            if (string.IsNullOrEmpty(
                    value))
            {
                return false;
            }

            string name =
                value.ToLowerInvariant();

            bool rear =
                name.Contains("tail") ||
                name.Contains("rear") ||
                name.Contains("back") ||
                name.Contains("brake") ||
                name.Contains("stop");

            bool lamp =
                name.Contains("light") ||
                name.Contains("lamp") ||
                name.Contains("emission") ||
                name.Contains("emissive");

            return rear &&
                   lamp;
        }

        private static string BuildHierarchyName(
            Transform item,
            Transform stopAt)
        {
            if (item == null)
                return string.Empty;

            string value =
                item.name ?? string.Empty;

            Transform cursor =
                item.parent;

            while (cursor != null &&
                   cursor != stopAt)
            {
                value +=
                    "/" +
                    cursor.name;

                cursor =
                    cursor.parent;
            }

            return
                value.ToLowerInvariant();
        }


        private void CreateTexturedRearLampOverlay(
            Renderer sourceRenderer)
        {
            if (maskedRearShader == null ||
                !maskedRearShader.isSupported ||
                sourceRenderer == null ||
                !IsBodySizedRenderer(sourceRenderer))
            {
                return;
            }

            MeshFilter filter =
                sourceRenderer.GetComponent<MeshFilter>();

            if (filter == null ||
                filter.sharedMesh == null)
            {
                return;
            }

            Material[] sourceMaterials =
                sourceRenderer.sharedMaterials;

            if (sourceMaterials == null ||
                sourceMaterials.Length == 0)
            {
                return;
            }

            Material[] overlayMaterials =
                new Material[sourceMaterials.Length];

            bool any = false;

            Vector3 rearAxis =
                sourceRenderer.transform
                    .InverseTransformDirection(
                        -transform.forward)
                    .normalized;

            ResolveProjectionRange(
                filter.sharedMesh.bounds,
                rearAxis,
                out float minimum,
                out float maximum);

            Vector3 lateralAxis =
                sourceRenderer.transform
                    .InverseTransformDirection(
                        transform.right)
                    .normalized;

            Vector3 upAxis =
                sourceRenderer.transform
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

            float span =
                Mathf.Max(
                    0.001f,
                    maximum - minimum);

            ResolveRearMaskPreset(
                out float rearCutoffFraction,
                out float rearSoftnessFraction,
                out float chromaLow,
                out float chromaHigh,
                out float redLow,
                out float redHigh,
                out float greenLow,
                out float greenHigh,
                out float blueLow,
                out float blueHigh);

            float cutoff =
                Mathf.Lerp(
                    minimum,
                    maximum,
                    rearCutoffFraction);

            float softness =
                Mathf.Max(
                    0.008f,
                    span *
                    rearSoftnessFraction);

            bool clubSpatialMask =
                vehicleId == "club";

            float lateralMaxAbs =
                Mathf.Max(
                    Mathf.Abs(lateralMinimum),
                    Mathf.Abs(lateralMaximum));

            float upSpan =
                Mathf.Max(
                    0.001f,
                    upMaximum -
                    upMinimum);

            float lateralLampMin =
                lateralMaxAbs * 0.50f;

            float lateralLampMax =
                lateralMaxAbs * 1.02f;

            float upLampMin =
                upMinimum +
                upSpan * 0.34f;

            float upLampMax =
                upMinimum +
                upSpan * 0.72f;

            for (int i = 0;
                 i < sourceMaterials.Length;
                 i++)
            {
                Material source =
                    sourceMaterials[i];

                Texture texture =
                    ResolveBaseTexture(
                        source);

                Material overlay =
                    new(maskedRearShader)
                    {
                        name =
                            "MotorCity_ModelRearLampMask_Runtime"
                    };

                overlay.SetTexture(
                    "_BaseMap",
                    texture != null
                        ? texture
                        : Texture2D.blackTexture);

                CopyTextureTransform(
                    source,
                    overlay);

                overlay.SetVector(
                    "_RearAxisOS",
                    new Vector4(
                        rearAxis.x,
                        rearAxis.y,
                        rearAxis.z,
                        0f));

                overlay.SetVector(
                    "_LateralAxisOS",
                    new Vector4(
                        lateralAxis.x,
                        lateralAxis.y,
                        lateralAxis.z,
                        0f));

                overlay.SetVector(
                    "_UpAxisOS",
                    new Vector4(
                        upAxis.x,
                        upAxis.y,
                        upAxis.z,
                        0f));

                overlay.SetFloat(
                    "_RearCutoff",
                    cutoff);

                overlay.SetFloat(
                    "_RearSoftness",
                    softness);

                overlay.SetFloat(
                    "_ChromaLow",
                    chromaLow);
                overlay.SetFloat(
                    "_ChromaHigh",
                    chromaHigh);
                overlay.SetFloat(
                    "_RedLow",
                    redLow);
                overlay.SetFloat(
                    "_RedHigh",
                    redHigh);
                overlay.SetFloat(
                    "_GreenLow",
                    greenLow);
                overlay.SetFloat(
                    "_GreenHigh",
                    greenHigh);
                overlay.SetFloat(
                    "_BlueLow",
                    blueLow);
                overlay.SetFloat(
                    "_BlueHigh",
                    blueHigh);

                overlay.SetFloat(
                    "_SpatialMask",
                    clubSpatialMask
                        ? 1f
                        : 0f);
                overlay.SetFloat(
                    "_LateralMin",
                    lateralLampMin);
                overlay.SetFloat(
                    "_LateralMax",
                    lateralLampMax);
                overlay.SetFloat(
                    "_UpMin",
                    upLampMin);
                overlay.SetFloat(
                    "_UpMax",
                    upLampMax);

                overlay.SetColor(
                    "_EmissionColor",
                    new Color(
                        1f,
                        0.035f,
                        0.015f,
                        1f));

                overlay.SetFloat(
                    "_Intensity",
                    0.2f);

                overlayMaterials[i] =
                    overlay;

                runtimeMaterials.Add(
                    overlay);

                if (texture != null)
                {
                    lampBindings.Add(
                        new LampBinding
                        {
                            Renderer = null,
                            MaterialIndex = -1,
                            Material = overlay,
                            OriginalMaterial = null,
                            BaseEmission =
                                new Color(
                                    1f,
                                    0.035f,
                                    0.015f,
                                    1f),
                            RearSpecific = true
                        });

                    any = true;
                }
            }

            if (!any)
                return;

            GameObject overlayObject =
                new(
                    LegacyOverlayName,
                    typeof(MeshFilter),
                    typeof(MeshRenderer));

            overlayObject.transform.SetParent(
                sourceRenderer.transform,
                false);

            overlayObject.transform.localPosition =
                Vector3.zero;

            overlayObject.transform.localRotation =
                Quaternion.identity;

            overlayObject.transform.localScale =
                Vector3.one;

            MeshFilter overlayFilter =
                overlayObject.GetComponent<MeshFilter>();

            overlayFilter.sharedMesh =
                filter.sharedMesh;

            MeshRenderer overlayRenderer =
                overlayObject.GetComponent<MeshRenderer>();

            overlayRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            overlayRenderer.receiveShadows =
                false;

            overlayRenderer.lightProbeUsage =
                UnityEngine.Rendering.LightProbeUsage.Off;

            overlayRenderer.reflectionProbeUsage =
                UnityEngine.Rendering.ReflectionProbeUsage.Off;

            overlayRenderer.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;

            overlayRenderer.sharedMaterials =
                overlayMaterials;
        }

        private void ResolveRearMaskPreset(
            out float rearCutoffFraction,
            out float rearSoftnessFraction,
            out float chromaLow,
            out float chromaHigh,
            out float redLow,
            out float redHigh,
            out float greenLow,
            out float greenHigh,
            out float blueLow,
            out float blueHigh)
        {
            // Strict preset is known to work on the red MuscleCar without
            // lighting its painted body.
            rearCutoffFraction = 0.70f;
            rearSoftnessFraction = 0.035f;
            chromaLow = 0.22f;
            chromaHigh = 0.46f;
            redLow = 0.62f;
            redHigh = 0.90f;
            greenLow = 0.20f;
            greenHigh = 0.42f;
            blueLow = 0.18f;
            blueHigh = 0.38f;

            bool club =
                vehicleId == "club";

            bool apex =
                vehicleId == "apex";

            if (club)
            {
                // Swifto/Club stores its rear lamps as comparatively dark red
                // texels. Restrict the effect to the very back of the mesh so
                // we can safely use a looser colour mask without lighting the
                // whole red body.
                rearCutoffFraction = 0.82f;
                rearSoftnessFraction = 0.022f;
                chromaLow = 0.01f;
                chromaHigh = 0.10f;
                redLow = 0.10f;
                redHigh = 0.34f;
                greenLow = 0.34f;
                greenHigh = 0.72f;
                blueLow = 0.32f;
                blueHigh = 0.68f;
            }
            else if (apex)
            {
                // SuvV1/Apex lamps are also darker in the shared atlas.
                rearCutoffFraction = 0.70f;
                rearSoftnessFraction = 0.035f;
                chromaLow = 0.12f;
                chromaHigh = 0.32f;
                redLow = 0.42f;
                redHigh = 0.72f;
                greenLow = 0.26f;
                greenHigh = 0.54f;
                blueLow = 0.24f;
                blueHigh = 0.50f;
            }
        }

        private bool IsBodySizedRenderer(
            Renderer renderer)
        {
            if (renderer == null ||
                currentVisual == null)
            {
                return false;
            }

            Bounds full =
                new(
                    currentVisual.position,
                    Vector3.zero);

            bool initialized = false;

            foreach (Renderer item in
                     currentVisual.GetComponentsInChildren<Renderer>(
                         true))
            {
                if (item == null ||
                    IsWheelRenderer(
                        item.transform))
                {
                    continue;
                }

                if (!initialized)
                {
                    full =
                        item.bounds;

                    initialized = true;
                }
                else
                {
                    full.Encapsulate(
                        item.bounds);
                }
            }

            if (!initialized)
                return false;

            Bounds candidate =
                renderer.bounds;

            float fullHorizontal =
                Mathf.Max(
                    full.size.x,
                    full.size.z);

            float candidateHorizontal =
                Mathf.Max(
                    candidate.size.x,
                    candidate.size.z);

            float fullWidth =
                Mathf.Min(
                    full.size.x,
                    full.size.z);

            float candidateWidth =
                Mathf.Min(
                    candidate.size.x,
                    candidate.size.z);

            return
                candidateHorizontal >=
                    fullHorizontal * 0.58f &&
                candidateWidth >=
                    fullWidth * 0.45f;
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

        private void RemoveLegacyOverlays()
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
                    LegacyOverlayName)
                {
                    continue;
                }

                item.gameObject.SetActive(
                    false);

                Destroy(
                    item.gameObject);
            }
        }

        private void RestoreAndClearBindings()
        {
            for (int i = 0;
                 i < lampBindings.Count;
                 i++)
            {
                LampBinding binding =
                    lampBindings[i];

                if (binding == null ||
                    binding.Renderer == null)
                {
                    continue;
                }

                Material[] materials =
                    binding.Renderer.sharedMaterials;

                if (binding.MaterialIndex < 0 ||
                    binding.MaterialIndex >=
                    materials.Length)
                {
                    continue;
                }

                if (materials[
                        binding.MaterialIndex] ==
                    binding.Material)
                {
                    materials[
                        binding.MaterialIndex] =
                        binding.OriginalMaterial;

                    binding.Renderer.sharedMaterials =
                        materials;
                }
            }

            lampBindings.Clear();
            ClearRuntimeMaterials();
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
                UnityEngine.Object.FindAnyObjectByType<
                    DayNightCycleController>();
        }

        private static bool IsMirrorRenderer(
            Transform item)
        {
            Transform cursor =
                item;

            while (cursor != null)
            {
                string name =
                    cursor.name
                        .ToLowerInvariant();

                if (name.Contains("mirror") ||
                    name.Contains("rearview") ||
                    name.Contains("rear_view") ||
                    name.Contains("sideview") ||
                    name.Contains("side_view"))
                {
                    return true;
                }

                cursor =
                    cursor.parent;
            }

            return false;
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
    }
}
