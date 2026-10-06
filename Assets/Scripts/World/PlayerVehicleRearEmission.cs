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

        // Do not duplicate complete vehicle meshes just to mask lamp polygons.
        // On several imported cars those overlays cover body/wheel submeshes
        // and render magenta. Existing authored lamp materials still work.
        private static readonly bool EnableMeshLampOverlays = false;

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
            public bool SuppressOnly;
        }

        private sealed class StarterLampOverlay
        {
            public GameObject Root;
            public Material RearMaterial;
            public Material FrontMaterial;
        }

        private readonly List<LampBinding> lampBindings =
            new();

        private readonly List<Material> runtimeMaterials =
            new();

        private readonly List<StarterLampOverlay> starterLampOverlays =
            new();

        private ArcadeCarController car;
        private DayNightCycleController dayNight;
        private Transform currentVisual;
        private Shader maskedRearShader;
        private Shader starterLampShader;
        private string vehicleId = "street";
        private float dayNightResolveTimer;
        private float lastNight = -1f;
        private bool lastBraking;
        private bool rearEmissionStateInitialized;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();

            maskedRearShader =
                Resources.Load<Shader>(
                    "MotorCity/Shaders/RearLampEmission");

            starterLampShader =
                Resources.Load<Shader>(
                    "MotorCity/Shaders/StarterLampEmission");
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

            if (rearEmissionStateInitialized &&
                braking ==
                    lastBraking &&
                Mathf.Abs(
                    night -
                    lastNight) <
                    0.01f)
            {
                return;
            }

            rearEmissionStateInitialized =
                true;

            lastBraking =
                braking;

            lastNight =
                night;

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

                if (binding.SuppressOnly)
                {
                    binding.Material.SetColor(
                        "_EmissionColor",
                        Color.black);

                    continue;
                }

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
            }

            float frontIntensity =
                Mathf.SmoothStep(
                    0f,
                    3.6f,
                    Mathf.InverseLerp(
                        0.30f,
                        0.70f,
                        night));

            float rearIntensity =
                braking
                    ? Mathf.Lerp(
                        2.2f,
                        4.2f,
                        night)
                    : 0f;

            foreach (StarterLampOverlay overlay in
                     starterLampOverlays)
            {
                if (overlay?.FrontMaterial != null)
                {
                    overlay.FrontMaterial.SetFloat(
                        "_Intensity",
                        frontIntensity);
                }

                if (overlay?.RearMaterial != null)
                {
                    overlay.RearMaterial.SetFloat(
                        "_Intensity",
                        rearIntensity);
                }
            }
        }

        private void OnDestroy()
        {
            ClearStarterLampOverlays();
            RestoreAndClearBindings();
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
            rearEmissionStateInitialized =
                false;
            lastNight =
                -1f;

            RestoreAndClearBindings();
            ClearStarterLampOverlays();

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
                    VehicleLampMaterialUtility.IsWheelRenderer(
                        renderer.transform) ||
                    IsMirrorRenderer(
                        renderer.transform))
                {
                    continue;
                }

                if (vehicleId == "beatall" && BindBeatallBrakeEmission(renderer))
                    continue;

                if (vehicleId == "street" &&
                    BindStarterLampMaterials(
                        renderer))
                {
                    continue;
                }

                if (vehicleId == "delorean" &&
                    BindDeloreanRearEmission(
                        renderer))
                {
                    continue;
                }

                if (vehicleId == "amggt" &&
                    BindAmgRearEmission(
                        renderer))
                {
                    continue;
                }

                if (vehicleId == "porsche996" &&
                    BindPorscheInnerRearEmission(
                        renderer))
                {
                    continue;
                }

                BindExistingLampMaterials(
                    renderer);
            }

            if (lampBindings.Count == 0 &&
                starterLampOverlays.Count == 0)
            {
                foreach (Renderer renderer in
                         renderers)
                {
                    if (renderer == null ||
                        VehicleLampMaterialUtility.IsWheelRenderer(
                            renderer.transform))
                    {
                        continue;
                    }

                    CreateTexturedRearLampOverlay(
                        renderer);
                }
            }
        }

        private bool BindStarterLampMaterials(
            Renderer renderer)
        {
            if (renderer == null ||
                starterLampShader == null)
            {
                return false;
            }

            MeshFilter filter =
                renderer.GetComponent<MeshFilter>();

            if (filter == null ||
                filter.sharedMesh == null)
            {
                return false;
            }

            Material[] sourceMaterials =
                renderer.sharedMaterials;

            bool found = false;
            Material[] assigned = null;

            for (int i = 0;
                 i < sourceMaterials.Length;
                 i++)
            {
                Material source =
                    sourceMaterials[i];

                if (!IsStarterSharedEmission(
                        source))
                {
                    continue;
                }

                found = true;

                Material muted =
                    new(source)
                    {
                        name =
                            source.name +
                            "_MotorCityMuted"
                    };

                if (muted.HasProperty(
                        "_EmissionColor"))
                {
                    muted.SetColor(
                        "_EmissionColor",
                        Color.black);
                }

                muted.EnableKeyword(
                    "_EMISSION");

                assigned ??=
                    (Material[])sourceMaterials.Clone();

                assigned[i] =
                    muted;

                runtimeMaterials.Add(
                    muted);

                lampBindings.Add(
                    new LampBinding
                    {
                        Renderer = renderer,
                        MaterialIndex = i,
                        Material = muted,
                        OriginalMaterial = source,
                        BaseEmission = Color.black,
                        RearSpecific = false,
                        SuppressOnly = true
                    });

                CreateStarterLampOverlay(
                    renderer,
                    filter.sharedMesh,
                    source,
                    i,
                    sourceMaterials.Length);
            }

            if (assigned != null)
            {
                renderer.sharedMaterials =
                    assigned;
            }

            return found;
        }

        private static bool IsStarterSharedEmission(
            Material material)
        {
            if (material == null)
                return false;

            string name =
                material.name
                    .ToLowerInvariant();

            return
                name.Contains("afrc_emission") ||
                name.Contains("afrc emission");
        }

        private void CreateStarterLampOverlay(
            Renderer sourceRenderer,
            Mesh mesh,
            Material sourceMaterial,
            int materialIndex,
            int materialCount)
        {
            if (!EnableMeshLampOverlays)
                return;
            Texture texture =
                ResolveEmissionTexture(
                    sourceMaterial,
                    false);

            if (texture == null)
                return;

            Vector3 forwardAxis =
                sourceRenderer.transform
                    .InverseTransformDirection(
                        transform.forward)
                    .normalized;

            VehicleLampMaterialUtility.ResolveProjectionRange(
                mesh.bounds,
                forwardAxis,
                out float minimum,
                out float maximum);

            float cutoff =
                (minimum + maximum) *
                0.5f;

            float softness =
                Mathf.Max(
                    0.03f,
                    (maximum - minimum) *
                    0.04f);

            Material rear =
                CreateStarterOverlayMaterial(
                    sourceMaterial,
                    texture,
                    forwardAxis,
                    cutoff,
                    softness,
                    false);

            Material front =
                CreateStarterOverlayMaterial(
                    sourceMaterial,
                    texture,
                    forwardAxis,
                    cutoff,
                    softness,
                    true);

            Material[] rearSlots =
                new Material[materialCount];

            Material[] frontSlots =
                new Material[materialCount];

            rearSlots[materialIndex] = rear;
            frontSlots[materialIndex] = front;

            GameObject root =
                new(
                    "MotorCityStarterLampOverlays");

            root.transform.SetParent(
                sourceRenderer.transform.parent,
                false);

            root.transform.localPosition =
                sourceRenderer.transform.localPosition;

            root.transform.localRotation =
                sourceRenderer.transform.localRotation;

            root.transform.localScale =
                sourceRenderer.transform.localScale;

            GameObject rearObject =
                new(
                    "Rear Brake Lamps",
                    typeof(MeshFilter),
                    typeof(MeshRenderer));

            rearObject.transform.SetParent(
                root.transform,
                false);

            rearObject.GetComponent<MeshFilter>()
                .sharedMesh = mesh;

            rearObject.GetComponent<MeshRenderer>()
                .sharedMaterials = rearSlots;

            GameObject frontObject =
                new(
                    "Front Headlamps",
                    typeof(MeshFilter),
                    typeof(MeshRenderer));

            frontObject.transform.SetParent(
                root.transform,
                false);

            frontObject.GetComponent<MeshFilter>()
                .sharedMesh = mesh;

            frontObject.GetComponent<MeshRenderer>()
                .sharedMaterials = frontSlots;

            runtimeMaterials.Add(
                rear);

            runtimeMaterials.Add(
                front);

            starterLampOverlays.Add(
                new StarterLampOverlay
                {
                    Root = root,
                    RearMaterial = rear,
                    FrontMaterial = front
                });
        }

        private Material CreateStarterOverlayMaterial(
            Material source,
            Texture texture,
            Vector3 axis,
            float cutoff,
            float softness,
            bool front)
        {
            Material material =
                new(starterLampShader)
                {
                    name =
                        front
                            ? "MotorCity_StarterFrontLamp_Runtime"
                            : "MotorCity_StarterRearBrake_Runtime"
                };

            material.SetTexture(
                "_BaseMap",
                texture);

            VehicleLampMaterialUtility.CopyTextureTransform(
                source,
                material);

            material.SetVector(
                "_AxisOS",
                new Vector4(
                    axis.x,
                    axis.y,
                    axis.z,
                    0f));

            material.SetFloat(
                "_Cutoff",
                cutoff);

            material.SetFloat(
                "_Softness",
                softness);

            material.SetFloat(
                "_Mode",
                front ? 1f : 0f);

            material.SetColor(
                "_EmissionColor",
                front
                    ? new Color(
                        0.92f,
                        0.96f,
                        1f,
                        1f)
                    : new Color(
                        1f,
                        0.025f,
                        0.012f,
                        1f));

            material.SetFloat(
                "_Intensity",
                0f);

            return material;
        }

        private bool BindBeatallBrakeEmission(Renderer renderer)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || starterLampShader == null || !starterLampShader.isSupported)
                return false;
            Material[] materials = renderer.sharedMaterials;
            bool found = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null || !source.name.ToLowerInvariant().Contains("beatallemission")) continue;
                Texture texture = VehicleLampMaterialUtility.ResolveBaseTexture(source);
                if (texture == null) continue;
                // Shared atlas contains both front and rear lamps. The overlay
                // selects red pixels in the rear half and starts with intensity zero.
                if (source.HasProperty("_EmissionColor")) source.SetColor("_EmissionColor", Color.black);
                source.DisableKeyword("_EMISSION");
                Vector3 axis = renderer.transform.InverseTransformDirection(transform.forward).normalized;
                VehicleLampMaterialUtility.ResolveProjectionRange(filter.sharedMesh.bounds, axis, out float min, out float max);
                Material rear = CreateStarterOverlayMaterial(source, texture, axis,
                    (min + max) * .5f, Mathf.Max(.02f, (max-min)*.025f), false);
                GameObject root = new("MotorCityBeatallBrakeOverlay");
                root.transform.SetParent(renderer.transform.parent, false);
                root.transform.localPosition = renderer.transform.localPosition;
                root.transform.localRotation = renderer.transform.localRotation;
                root.transform.localScale = renderer.transform.localScale;
                root.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                Material[] slots = new Material[materials.Length];
                slots[i] = rear;
                MeshRenderer overlayRenderer = root.AddComponent<MeshRenderer>();
                overlayRenderer.sharedMaterials = slots;
                overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                overlayRenderer.receiveShadows = false;
                runtimeMaterials.Add(rear);
                starterLampOverlays.Add(new StarterLampOverlay {Root = root, RearMaterial = rear, FrontMaterial = null});
                found = true;
            }
            return found;
        }
        private bool BindDeloreanRearEmission(
            Renderer renderer)
        {
            if (!EnableMeshLampOverlays)
                return false;
            if (renderer == null ||
                starterLampShader == null ||
                !starterLampShader.isSupported)
            {
                return false;
            }

            MeshFilter filter =
                renderer.GetComponent<MeshFilter>();

            if (filter == null ||
                filter.sharedMesh == null)
            {
                return false;
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
                        "deloreanemission") &&
                    !materialName.Contains(
                        "gradientemmisive") &&
                    !materialName.Contains(
                        "gradientemissive"))
                {
                    continue;
                }

                Texture texture =
                    VehicleLampMaterialUtility.ResolveBaseTexture(
                        source);

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

                Material rear =
                    CreateStarterOverlayMaterial(
                        source,
                        texture,
                        forwardAxis,
                        Mathf.Lerp(
                            minimum,
                            maximum,
                            0.50f),
                        Mathf.Max(
                            0.02f,
                            (maximum - minimum) *
                            0.025f),
                        false);

                // Delorean's brake lamp polygons are authored in the shared
                // emissive mesh and sample this exact palette/atlas UV.
                // Select the UV island directly, then keep only the rear half
                // of that emissive submesh.
                rear.SetFloat(
                    "_Mode",
                    2f);

                rear.SetFloat(
                    "_UvMask",
                    1f);

                rear.SetVector(
                    "_UvCenter",
                    new Vector4(
                        0.381f,
                        0.696f,
                        0f,
                        0f));

                rear.SetVector(
                    "_UvCenter2",
                    new Vector4(
                        0.381f,
                        0.696f,
                        0f,
                        0f));

                rear.SetVector(
                    "_UvTolerance",
                    new Vector4(
                        0.006f,
                        0.006f,
                        0f,
                        0f));

                rear.SetColor(
                    "_EmissionColor",
                    new Color(
                        1.25f,
                        0.018f,
                        0.008f,
                        1f));

                Material[] slots =
                    new Material[
                        materials.Length];

                slots[i] =
                    rear;

                GameObject root =
                    new(
                        "MotorCityDeloreanRearLampOverlay");

                root.transform.SetParent(
                    renderer.transform.parent,
                    false);

                root.transform.localPosition =
                    renderer.transform.localPosition;

                root.transform.localRotation =
                    renderer.transform.localRotation;

                root.transform.localScale =
                    renderer.transform.localScale;

                root.AddComponent<MeshFilter>()
                    .sharedMesh =
                    filter.sharedMesh;

                MeshRenderer overlayRenderer =
                    root.AddComponent<MeshRenderer>();

                overlayRenderer.sharedMaterials =
                    slots;

                overlayRenderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;

                overlayRenderer.receiveShadows =
                    false;

                runtimeMaterials.Add(
                    rear);

                starterLampOverlays.Add(
                    new StarterLampOverlay
                    {
                        Root = root,
                        RearMaterial = rear,
                        FrontMaterial = null
                    });

                return true;
            }

            return false;
        }

        private bool BindAmgRearEmission(
            Renderer renderer)
        {
            if (!EnableMeshLampOverlays)
                return false;
            if (renderer == null ||
                starterLampShader == null ||
                !starterLampShader.isSupported)
            {
                return false;
            }

            MeshFilter filter =
                renderer.GetComponent<MeshFilter>();

            if (filter == null ||
                filter.sharedMesh == null)
            {
                return false;
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
                        "amggtemission") &&
                    !materialName.Contains(
                        "gradientemmisive") &&
                    !materialName.Contains(
                        "gradientemissive"))
                {
                    continue;
                }

                Texture texture =
                    VehicleLampMaterialUtility.ResolveBaseTexture(
                        source);

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

                Material rear =
                    CreateStarterOverlayMaterial(
                        source,
                        texture,
                        forwardAxis,
                        Mathf.Lerp(
                            minimum,
                            maximum,
                            0.50f),
                        Mathf.Max(
                            0.02f,
                            (maximum - minimum) *
                            0.025f),
                        false);

                // AMG's shared emissive texture contains both front and rear
                // lamps. The rear lamp texels are not reliably "red enough"
                // for the generic chroma test, so use geometry (rear half of
                // the emissive submesh) as the authoritative mask.
                rear.SetFloat(
                    "_Mode",
                    2f);

                rear.SetColor(
                    "_EmissionColor",
                    new Color(
                        1.25f,
                        0.018f,
                        0.008f,
                        1f));

                Material[] slots =
                    new Material[
                        materials.Length];

                slots[i] =
                    rear;

                GameObject root =
                    new(
                        "MotorCityAmgRearLampOverlay");

                root.transform.SetParent(
                    renderer.transform.parent,
                    false);

                root.transform.localPosition =
                    renderer.transform.localPosition;

                root.transform.localRotation =
                    renderer.transform.localRotation;

                root.transform.localScale =
                    renderer.transform.localScale;

                root.AddComponent<MeshFilter>()
                    .sharedMesh =
                    filter.sharedMesh;

                MeshRenderer overlayRenderer =
                    root.AddComponent<MeshRenderer>();

                overlayRenderer.sharedMaterials =
                    slots;

                overlayRenderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;

                overlayRenderer.receiveShadows =
                    false;

                runtimeMaterials.Add(
                    rear);

                starterLampOverlays.Add(
                    new StarterLampOverlay
                    {
                        Root = root,
                        RearMaterial = rear,
                        FrontMaterial = null
                    });

                return true;
            }

            return false;
        }

        private bool BindPorscheInnerRearEmission(
            Renderer renderer)
        {
            if (!EnableMeshLampOverlays)
                return false;
            if (renderer == null ||
                starterLampShader == null ||
                !starterLampShader.isSupported)
            {
                return false;
            }

            MeshFilter filter =
                renderer.GetComponent<MeshFilter>();

            if (filter == null ||
                filter.sharedMesh == null)
            {
                return false;
            }

            Material[] materials =
                renderer.sharedMaterials;

            bool found = false;

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

                // In this Porsche mesh the two inner rear lamp polygons the
                // user wants are authored under "indicators". The outer red
                // polygons are "rearLights". Use only the REAR indicator faces
                // and recolor their emission red; the front indicator faces
                // are discarded by the rear-side mask.
                if (!materialName.Contains(
                        "porsche996indicators") &&
                    !materialName.Contains(
                        "indicators"))
                {
                    continue;
                }

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

                Material rear =
                    CreateStarterOverlayMaterial(
                        source,
                        Texture2D.whiteTexture,
                        forwardAxis,
                        Mathf.Lerp(
                            minimum,
                            maximum,
                            0.50f),
                        Mathf.Max(
                            0.02f,
                            (maximum - minimum) *
                            0.025f),
                        false);

                rear.SetFloat(
                    "_Mode",
                    2f);

                rear.SetColor(
                    "_EmissionColor",
                    new Color(
                        1.35f,
                        0.018f,
                        0.008f,
                        1f));

                Material[] slots =
                    new Material[
                        materials.Length];

                slots[i] =
                    rear;

                GameObject root =
                    new(
                        "MotorCityPorscheInnerRearLampOverlay");

                root.transform.SetParent(
                    renderer.transform.parent,
                    false);

                root.transform.localPosition =
                    renderer.transform.localPosition;

                root.transform.localRotation =
                    renderer.transform.localRotation;

                root.transform.localScale =
                    renderer.transform.localScale;

                root.AddComponent<MeshFilter>()
                    .sharedMesh =
                    filter.sharedMesh;

                MeshRenderer overlayRenderer =
                    root.AddComponent<MeshRenderer>();

                overlayRenderer.sharedMaterials =
                    slots;

                overlayRenderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;

                overlayRenderer.receiveShadows =
                    false;

                runtimeMaterials.Add(
                    rear);

                starterLampOverlays.Add(
                    new StarterLampOverlay
                    {
                        Root = root,
                        RearMaterial = rear,
                        FrontMaterial = null
                    });

                found = true;
            }

            return found;
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

                // Turn indicators are not brake/reverse lamps. Some imported
                // cars keep them in the same rear cluster, so explicitly
                // exclude them from the rear-emission controller.
                if (materialName.Contains("indicator") ||
                    materialName.Contains("indicators") ||
                    materialName.Contains("turnsignal") ||
                    materialName.Contains("turn_signal") ||
                    materialName.Contains("turn signal") ||
                    materialName.Contains("amber"))
                {
                    if (source.HasProperty("_EmissionColor"))
                    {
                        source.SetColor(
                            "_EmissionColor",
                            Color.black);
                    }

                    source.DisableKeyword(
                        "_EMISSION");

                    continue;
                }

                if (vehicleId == "porsche996" &&
                    materialName.Contains("rearlight"))
                {
                    if (source.HasProperty("_EmissionColor"))
                    {
                        source.SetColor(
                            "_EmissionColor",
                            Color.black);
                    }

                    source.DisableKeyword(
                        "_EMISSION");

                    continue;
                }

                // Hybrid's red rear lamp polygons have an exported numeric name.
                bool hybridRearLamp = vehicleId == "hybrid" &&
                    materialName.Replace(" (instance)", "").Replace(" (clone)", "").Trim() == "material.004";
                bool materialRearSpecific = hybridRearLamp || LooksLikeRearLampName(materialName);

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

                // This component controls rear/brake emission only.
                // Do not bind front lamps or generic emissive/color materials
                // such as "Light Blue Paint"; those must keep their authored
                // appearance and are handled elsewhere if needed.
                if (!rearSpecific)
                    continue;

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

            bool explicitLampName =
                materialName.Contains("lamp") ||
                materialName.Contains("headlight") ||
                materialName.Contains("head_light") ||
                materialName.Contains("head light") ||
                materialName.Contains("taillight") ||
                materialName.Contains("tail_light") ||
                materialName.Contains("tail light") ||
                (materialName.Contains("head") &&
                 materialName.Contains("light")) ||
                (materialName.Contains("tail") &&
                 materialName.Contains("light")) ||
                (materialName.Contains("rear") &&
                 materialName.Contains("light")) ||
                (materialName.Contains("brake") &&
                 materialName.Contains("light")) ||
                (materialName.Contains("stop") &&
                 materialName.Contains("light"));

            if (explicitLampName)
                return true;

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

            VehicleLampMaterialUtility.ResolveProjectionRange(
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

            bool spatialLampMask =
                false;

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
                    VehicleLampMaterialUtility.ResolveBaseTexture(
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

                VehicleLampMaterialUtility.CopyTextureTransform(
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
                    spatialLampMask
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
            if (vehicleId == "amggt")
            {
                rearCutoffFraction = 0.58f;
                rearSoftnessFraction = 0.045f;
                chromaLow = 0.10f;
                chromaHigh = 0.28f;
                redLow = 0.34f;
                redHigh = 0.66f;
                greenLow = 0.24f;
                greenHigh = 0.52f;
                blueLow = 0.22f;
                blueHigh = 0.48f;
                return;
            }

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
                    VehicleLampMaterialUtility.IsWheelRenderer(
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

        private void ClearStarterLampOverlays()
        {
            foreach (StarterLampOverlay overlay in
                     starterLampOverlays)
            {
                if (overlay?.Root != null)
                {
                    Destroy(
                        overlay.Root);
                }
            }

            starterLampOverlays.Clear();
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

    }
}
