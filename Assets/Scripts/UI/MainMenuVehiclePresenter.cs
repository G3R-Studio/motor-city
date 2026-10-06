using System;
using MotorCity.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    /// <summary>
    /// Lightweight presentation-only vehicle used by the front-end.
    /// It reads the saved roster/customization directly so the selected car
    /// can be shown before gameplay systems and the city are constructed.
    /// </summary>
    public sealed class MainMenuVehiclePresenter :
        MonoBehaviour
    {
        private static readonly Vector3 PreviewOrigin =
            new(0f, -4000f, 0f);

        private const float TargetVehicleLength = 4.8f;
        private const float BaseVehicleYaw = 24f;

        private RawImage targetImage;
        private RenderTexture renderTexture;
        private Camera previewCamera;
        private GameObject stageRoot;
        private Transform vehicleAnchor;
        private GameObject vehicleInstance;
        private Material platformMaterial;
        private Material neonSurfaceMaterial;
        private MaterialPropertyBlock propertyBlock;

        private string loadedVehicleId = string.Empty;
        private int loadedColorIndex = -1;
        private int loadedWheelIndex = -1;
        private int loadedNeonIndex = -1;
        private bool visibleRequested;

        public void Initialize(
            RawImage image)
        {
            targetImage =
                image;

            propertyBlock =
                new MaterialPropertyBlock();

            EnsureStage();
            EnsureRenderTexture();

            if (targetImage != null)
            {
                targetImage.texture =
                    renderTexture;

                targetImage.color =
                    Color.white;

                targetImage.raycastTarget =
                    false;
            }

            SetVisible(
                true);

            Refresh();
        }

        public void SetVisible(
            bool visible)
        {
            visibleRequested =
                visible;

            if (targetImage != null)
            {
                targetImage.enabled =
                    visible;
            }

            if (previewCamera != null)
            {
                previewCamera.enabled =
                    visible &&
                    targetImage != null &&
                    targetImage.gameObject.activeInHierarchy;
            }
        }

        public void Refresh()
        {
            if (!VehicleRosterSystem.TryResolveSavedPresentationVehicle(
                    out string vehicleId,
                    out string resourcePath))
            {
                ClearVehicle();
                return;
            }

            int colorIndex =
                VehicleCustomizationSystem.LoadSavedColorIndex(
                    vehicleId);

            int wheelIndex =
                VehicleCustomizationSystem.LoadSavedWheelStyleIndex(
                    vehicleId);

            int neonIndex =
                VehicleCustomizationSystem.LoadSavedNeonIndex(
                    vehicleId);

            bool unchanged =
                vehicleInstance != null &&
                loadedVehicleId == vehicleId &&
                loadedColorIndex == colorIndex &&
                loadedWheelIndex == wheelIndex &&
                loadedNeonIndex == neonIndex;

            if (unchanged)
                return;

            GameObject prefab =
                Resources.Load<GameObject>(
                    resourcePath);

            if (prefab == null)
            {
                ClearVehicle();
                return;
            }

            ClearVehicle();

            loadedVehicleId =
                vehicleId;

            loadedColorIndex =
                colorIndex;

            loadedWheelIndex =
                wheelIndex;

            loadedNeonIndex =
                neonIndex;

            vehicleInstance =
                Instantiate(
                    prefab,
                    vehicleAnchor,
                    false);

            vehicleInstance.name =
                "MainMenuVehicle_" +
                vehicleId;

            SanitizePresentationVehicle(
                vehicleInstance);

            ApplySavedCosmetics(
                vehicleInstance,
                vehicleId,
                colorIndex,
                wheelIndex);

            FitVehicleToStage(
                vehicleInstance.transform);

            BuildSavedNeon(
                neonIndex);
        }

        private void Update()
        {
            if (previewCamera != null)
            {
                previewCamera.enabled =
                    visibleRequested &&
                    targetImage != null &&
                    targetImage.gameObject.activeInHierarchy;
            }

            if (!visibleRequested ||
                vehicleAnchor == null ||
                vehicleInstance == null)
            {
                return;
            }

            float idleYaw =
                BaseVehicleYaw +
                Mathf.Sin(
                    Time.unscaledTime * 0.42f) *
                3.5f;

            vehicleAnchor.localRotation =
                Quaternion.Euler(
                    0f,
                    idleYaw,
                    0f);
        }

        private void EnsureStage()
        {
            if (stageRoot != null)
                return;

            stageRoot =
                new GameObject(
                    "Main Menu Vehicle Stage");

            stageRoot.transform.SetParent(
                transform,
                false);

            stageRoot.transform.position =
                PreviewOrigin;

            GameObject anchor =
                new(
                    "Vehicle Anchor");

            anchor.transform.SetParent(
                stageRoot.transform,
                false);

            anchor.transform.localPosition =
                Vector3.zero;

            anchor.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    BaseVehicleYaw,
                    0f);

            vehicleAnchor =
                anchor.transform;

            CreatePlatform();
            CreateStageLights();
            CreatePreviewCamera();
        }

        private void CreatePlatform()
        {
            GameObject platform =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder);

            platform.name =
                "Presentation Platform";

            platform.transform.SetParent(
                stageRoot.transform,
                false);

            platform.transform.localPosition =
                new Vector3(
                    0f,
                    -0.09f,
                    0f);

            platform.transform.localScale =
                new Vector3(
                    3.45f,
                    0.07f,
                    3.45f);

            Collider collider =
                platform.GetComponent<Collider>();

            if (collider != null)
            {
                collider.enabled =
                    false;
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit") ??
                Shader.Find(
                    "Standard");

            if (shader == null)
                return;

            platformMaterial =
                new Material(
                    shader)
                {
                    name =
                        "MainMenuPlatform_Runtime"
                };

            SetMaterialColor(
                platformMaterial,
                new Color(
                    0.035f,
                    0.045f,
                    0.075f,
                    1f));

            SetMaterialFloat(
                platformMaterial,
                "_Metallic",
                0.25f);

            SetMaterialFloat(
                platformMaterial,
                "_Smoothness",
                0.58f);

            Renderer renderer =
                platform.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    platformMaterial;
            }
        }

        private void CreateStageLights()
        {
            GameObject keyObject =
                new(
                    "Presentation Key Light");

            keyObject.transform.SetParent(
                stageRoot.transform,
                false);

            keyObject.transform.localPosition =
                new Vector3(
                    -3.5f,
                    4.8f,
                    -3.8f);

            keyObject.transform.LookAt(
                stageRoot.transform.position +
                new Vector3(
                    0f,
                    0.8f,
                    0f));

            Light key =
                keyObject.AddComponent<Light>();

            key.type =
                LightType.Spot;

            key.color =
                new Color(
                    0.86f,
                    0.92f,
                    1f,
                    1f);

            key.intensity =
                5.2f;

            key.range =
                18f;

            key.spotAngle =
                72f;

            key.innerSpotAngle =
                48f;

            key.shadows =
                LightShadows.None;

            GameObject rimObject =
                new(
                    "Presentation Rim Light");

            rimObject.transform.SetParent(
                stageRoot.transform,
                false);

            rimObject.transform.localPosition =
                new Vector3(
                    3.8f,
                    2.8f,
                    3.4f);

            rimObject.transform.LookAt(
                stageRoot.transform.position +
                new Vector3(
                    0f,
                    0.9f,
                    0f));

            Light rim =
                rimObject.AddComponent<Light>();

            rim.type =
                LightType.Spot;

            rim.color =
                new Color(
                    0.34f,
                    0.62f,
                    1f,
                    1f);

            rim.intensity =
                3.2f;

            rim.range =
                16f;

            rim.spotAngle =
                82f;

            rim.innerSpotAngle =
                54f;

            rim.shadows =
                LightShadows.None;
        }

        private void CreatePreviewCamera()
        {
            GameObject cameraObject =
                new(
                    "Main Menu Vehicle Camera");

            cameraObject.transform.SetParent(
                stageRoot.transform,
                false);

            cameraObject.transform.localPosition =
                new Vector3(
                    6.7f,
                    2.85f,
                    -7.6f);

            cameraObject.transform.LookAt(
                stageRoot.transform.position +
                new Vector3(
                    0f,
                    1.02f,
                    0f));

            previewCamera =
                cameraObject.AddComponent<Camera>();

            previewCamera.clearFlags =
                CameraClearFlags.SolidColor;

            previewCamera.backgroundColor =
                new Color(
                    0f,
                    0f,
                    0f,
                    0f);

            previewCamera.fieldOfView =
                34f;

            previewCamera.nearClipPlane =
                0.1f;

            previewCamera.farClipPlane =
                40f;

            previewCamera.allowHDR =
                false;

            previewCamera.allowMSAA =
                false;
        }

        private void EnsureRenderTexture()
        {
            if (renderTexture != null)
                return;

            renderTexture =
                new RenderTexture(
                    896,
                    896,
                    24,
                    RenderTextureFormat.ARGB32)
                {
                    name =
                        "MainMenuVehiclePreview_Runtime",
                    antiAliasing =
                        1,
                    useMipMap =
                        false,
                    autoGenerateMips =
                        false
                };

            renderTexture.Create();

            if (previewCamera != null)
            {
                previewCamera.targetTexture =
                    renderTexture;
            }

            if (targetImage != null)
            {
                targetImage.texture =
                    renderTexture;

                targetImage.color =
                    Color.white;

                targetImage.raycastTarget =
                    false;
            }
        }

        private void FitVehicleToStage(
            Transform visual)
        {
            if (!TryGetRendererBounds(
                    visual,
                    out Bounds bounds))
            {
                return;
            }

            float horizontalSize =
                Mathf.Max(
                    bounds.size.x,
                    bounds.size.z);

            if (horizontalSize > 0.001f)
            {
                float scale =
                    TargetVehicleLength /
                    horizontalSize;

                scale =
                    Mathf.Clamp(
                        scale,
                        0.04f,
                        12f);

                visual.localScale *=
                    scale;
            }

            if (!TryGetRendererBounds(
                    visual,
                    out bounds))
            {
                return;
            }

            Vector3 targetCenter =
                stageRoot.transform.position;

            Vector3 offset =
                new(
                    targetCenter.x -
                    bounds.center.x,
                    targetCenter.y +
                    0.08f -
                    bounds.min.y,
                    targetCenter.z -
                    bounds.center.z);

            visual.position +=
                offset;
        }

        private void ApplySavedCosmetics(
            GameObject visual,
            string vehicleId,
            int colorIndex,
            int wheelIndex)
        {
            Renderer[] renderers =
                visual.GetComponentsInChildren<
                    Renderer>(
                    true);

            bool streetPaintApplied =
                vehicleId == "street" &&
                ApplyStreetPaint(
                    renderers,
                    colorIndex);

            if (!streetPaintApplied)
            {
                ApplyBodyColor(
                    renderers,
                    VehicleCustomizationSystem
                        .ResolveBodyColorForPresentation(
                            vehicleId,
                            colorIndex));
            }

            ApplyWheelColor(
                renderers,
                VehicleCustomizationSystem
                    .ResolveWheelColorForPresentation(
                        wheelIndex));
        }

        private bool ApplyStreetPaint(
            Renderer[] renderers,
            int colorIndex)
        {
            Material paint =
                Resources.Load<Material>(
                    "MotorCity/VehiclePaints/StarterPaint_" +
                    (colorIndex % 5));

            if (paint == null)
                return false;

            bool applied =
                false;

            foreach (Renderer renderer in
                     renderers)
            {
                if (!IsUsableRenderer(
                        renderer) ||
                    IsWheelHierarchy(
                        renderer.transform))
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                bool changed =
                    false;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    if (!IsStarterPaintMaterial(
                            materials[i]))
                    {
                        continue;
                    }

                    materials[i] =
                        paint;

                    changed =
                        true;

                    applied =
                        true;
                }

                if (changed)
                {
                    renderer.sharedMaterials =
                        materials;
                }
            }

            return applied;
        }

        private void ApplyBodyColor(
            Renderer[] renderers,
            Color color)
        {
            bool hasNamedBody =
                Array.Exists(
                    renderers,
                    renderer =>
                        renderer != null &&
                        IsPrimaryBodyRenderer(
                            renderer.transform));

            foreach (Renderer renderer in
                     renderers)
            {
                if (!IsUsableRenderer(
                        renderer))
                {
                    continue;
                }

                bool bodyCandidate =
                    IsPrimaryBodyRenderer(
                        renderer.transform);

                if (!bodyCandidate &&
                    !hasNamedBody)
                {
                    bodyCandidate =
                        IsFallbackBodyRenderer(
                            renderer);
                }

                if (!bodyCandidate)
                    continue;

                Material[] materials =
                    renderer.sharedMaterials;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material material =
                        materials[i];

                    if (material == null ||
                        IsGlassOrEmissionMaterial(
                            material))
                    {
                        continue;
                    }

                    ApplyColorBlock(
                        renderer,
                        material,
                        i,
                        color);
                }
            }
        }

        private void ApplyWheelColor(
            Renderer[] renderers,
            Color color)
        {
            bool hasNamedWheelPaint =
                Array.Exists(
                    renderers,
                    IsNamedWheelPaintRenderer);

            foreach (Renderer renderer in
                     renderers)
            {
                if (!IsUsableRenderer(
                        renderer))
                {
                    continue;
                }

                bool hierarchyWheel =
                    IsWheelHierarchy(
                        renderer.transform);

                bool wheelRenderer =
                    hasNamedWheelPaint
                        ? IsNamedWheelPaintRenderer(
                            renderer)
                        : hierarchyWheel ||
                          IsRimMaterialSet(
                              renderer.sharedMaterials);

                if (!wheelRenderer ||
                    IsPrimaryBodyRenderer(
                        renderer.transform))
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material material =
                        materials[i];

                    if (material == null ||
                        IsRubberMaterial(
                            material))
                    {
                        continue;
                    }

                    if (!hasNamedWheelPaint &&
                        !hierarchyWheel &&
                        !IsRimMaterial(
                            material))
                    {
                        continue;
                    }

                    ApplyColorBlock(
                        renderer,
                        material,
                        i,
                        color);
                }
            }
        }

        private void BuildSavedNeon(
            int neonIndex)
        {
            if (!VehicleCustomizationSystem
                    .TryResolveNeonColorForPresentation(
                        neonIndex,
                        out Color color) ||
                vehicleInstance == null ||
                !TryGetRendererBounds(
                    vehicleInstance.transform,
                    out Bounds bounds))
            {
                return;
            }

            GameObject neonRoot =
                new(
                    "Main Menu Underglow");

            neonRoot.transform.SetParent(
                vehicleAnchor,
                false);

            Vector3 centerLocal =
                vehicleAnchor.InverseTransformPoint(
                    bounds.center);

            float undersideWorld =
                bounds.min.y +
                0.16f;

            Vector3 undersideLocal =
                vehicleAnchor.InverseTransformPoint(
                    new Vector3(
                        bounds.center.x,
                        undersideWorld,
                        bounds.center.z));

            float span =
                Mathf.Max(
                    2.4f,
                    bounds.size.z * 0.62f);

            for (int i = -2;
                 i <= 2;
                 i++)
            {
                GameObject lightObject =
                    new(
                        "Underglow " +
                        i);

                lightObject.transform.SetParent(
                    neonRoot.transform,
                    false);

                lightObject.transform.localPosition =
                    new Vector3(
                        centerLocal.x,
                        undersideLocal.y,
                        centerLocal.z +
                        i * span * 0.23f);

                lightObject.transform.localRotation =
                    Quaternion.Euler(
                        90f,
                        0f,
                        0f);

                Light light =
                    lightObject.AddComponent<Light>();

                light.type =
                    LightType.Spot;

                light.color =
                    color;

                light.intensity =
                    i == 0
                        ? 1.1f
                        : 1.6f;

                light.range =
                    6f;

                light.spotAngle =
                    158f;

                light.innerSpotAngle =
                    132f;

                light.shadows =
                    LightShadows.None;
            }

            CreateNeonSurface(
                neonRoot.transform,
                centerLocal,
                undersideLocal.y -
                    0.04f,
                bounds,
                color);
        }

        private void CreateNeonSurface(
            Transform parent,
            Vector3 centerLocal,
            float localY,
            Bounds bounds,
            Color color)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit") ??
                Shader.Find(
                    "Universal Render Pipeline/Lit") ??
                Shader.Find(
                    "Standard");

            if (shader == null)
                return;

            GameObject surface =
                GameObject.CreatePrimitive(
                    PrimitiveType.Quad);

            surface.name =
                "Underglow Surface";

            surface.transform.SetParent(
                parent,
                false);

            surface.transform.localPosition =
                new Vector3(
                    centerLocal.x,
                    localY,
                    centerLocal.z);

            surface.transform.localRotation =
                Quaternion.Euler(
                    90f,
                    0f,
                    0f);

            surface.transform.localScale =
                new Vector3(
                    Mathf.Clamp(
                        bounds.size.x * 0.72f,
                        1.2f,
                        3.2f),
                    Mathf.Clamp(
                        bounds.size.z * 0.66f,
                        2.4f,
                        4.8f),
                    1f);

            Collider collider =
                surface.GetComponent<Collider>();

            if (collider != null)
            {
                collider.enabled =
                    false;
            }

            neonSurfaceMaterial =
                new Material(
                    shader)
                {
                    name =
                        "MainMenuNeon_Runtime"
                };

            Color surfaceColor =
                new(
                    color.r * 0.42f,
                    color.g * 0.42f,
                    color.b * 0.42f,
                    1f);

            SetMaterialColor(
                neonSurfaceMaterial,
                surfaceColor);

            if (neonSurfaceMaterial.HasProperty(
                    "_EmissionColor"))
            {
                neonSurfaceMaterial.EnableKeyword(
                    "_EMISSION");

                neonSurfaceMaterial.SetColor(
                    "_EmissionColor",
                    color * 2.2f);
            }

            Renderer renderer =
                surface.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    neonSurfaceMaterial;
            }
        }

        private void ApplyColorBlock(
            Renderer renderer,
            Material material,
            int materialIndex,
            Color color)
        {
            propertyBlock.Clear();

            renderer.GetPropertyBlock(
                propertyBlock,
                materialIndex);

            if (material.HasProperty(
                    "_BaseColor"))
            {
                propertyBlock.SetColor(
                    "_BaseColor",
                    color);
            }

            if (material.HasProperty(
                    "_Color"))
            {
                propertyBlock.SetColor(
                    "_Color",
                    color);
            }

            if (material.HasProperty(
                    "_Smoothness"))
            {
                propertyBlock.SetFloat(
                    "_Smoothness",
                    Mathf.Max(
                        0.46f,
                        material.GetFloat(
                            "_Smoothness")));
            }

            renderer.SetPropertyBlock(
                propertyBlock,
                materialIndex);

            propertyBlock.Clear();
        }

        private static void SanitizePresentationVehicle(
            GameObject visual)
        {
            foreach (Behaviour behaviour in
                     visual.GetComponentsInChildren<
                         Behaviour>(
                         true))
            {
                if (behaviour != null)
                {
                    behaviour.enabled =
                        false;
                }
            }

            foreach (Collider collider in
                     visual.GetComponentsInChildren<
                         Collider>(
                         true))
            {
                if (collider != null)
                {
                    collider.enabled =
                        false;
                }
            }

            foreach (Rigidbody body in
                     visual.GetComponentsInChildren<
                         Rigidbody>(
                         true))
            {
                if (body == null)
                    continue;

                body.isKinematic =
                    true;

                body.detectCollisions =
                    false;

                body.linearVelocity =
                    Vector3.zero;

                body.angularVelocity =
                    Vector3.zero;
            }

            foreach (ParticleSystem particle in
                     visual.GetComponentsInChildren<
                         ParticleSystem>(
                         true))
            {
                particle.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear);
            }

            foreach (TrailRenderer trail in
                     visual.GetComponentsInChildren<
                         TrailRenderer>(
                         true))
            {
                trail.enabled =
                    false;
            }
        }

        private static bool TryGetRendererBounds(
            Transform root,
            out Bounds bounds)
        {
            Renderer[] renderers =
                root.GetComponentsInChildren<
                    Renderer>(
                    true);

            bool initialized =
                false;

            bounds =
                new Bounds(
                    root.position,
                    Vector3.one);

            foreach (Renderer renderer in
                     renderers)
            {
                if (!IsUsableRenderer(
                        renderer))
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds =
                        renderer.bounds;

                    initialized =
                        true;
                }
                else
                {
                    bounds.Encapsulate(
                        renderer.bounds);
                }
            }

            return initialized;
        }

        private static bool IsUsableRenderer(
            Renderer renderer)
        {
            return
                renderer != null &&
                renderer is not TrailRenderer &&
                renderer is not ParticleSystemRenderer;
        }

        private static bool IsPrimaryBodyRenderer(
            Transform transform)
        {
            if (transform == null)
                return false;

            string meshName =
                RendererMeshName(
                    transform);

            string normalized =
                VehiclePaintMeshNames.Normalize(
                    meshName);

            if (normalized == "body_misc" ||
                normalized.StartsWith(
                    "body_misc_",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return
                VehiclePaintMeshNames.IsBody(
                    meshName) ||
                VehiclePaintMeshNames.IsBody(
                    transform.name);
        }

        private static bool IsNamedWheelPaintRenderer(
            Renderer renderer)
        {
            return
                renderer != null &&
                (VehiclePaintMeshNames.IsWheelPaint(
                     RendererMeshName(
                         renderer.transform)) ||
                 VehiclePaintMeshNames.IsWheelPaint(
                     renderer.transform.name));
        }

        private static string RendererMeshName(
            Transform transform)
        {
            MeshFilter filter =
                transform.GetComponent<MeshFilter>();

            if (filter != null &&
                filter.sharedMesh != null)
            {
                return
                    filter.sharedMesh.name;
            }

            SkinnedMeshRenderer skinned =
                transform.GetComponent<
                    SkinnedMeshRenderer>();

            return
                skinned != null &&
                skinned.sharedMesh != null
                    ? skinned.sharedMesh.name
                    : string.Empty;
        }

        private static bool IsWheelHierarchy(
            Transform transform)
        {
            Transform current =
                transform;

            int depth =
                0;

            while (current != null &&
                   depth++ < 8)
            {
                string name =
                    (current.name ??
                     string.Empty)
                    .ToLowerInvariant();

                if (name.Contains(
                        "wheel") ||
                    name.Contains(
                        "tire") ||
                    name.Contains(
                        "tyre") ||
                    name.Contains(
                        "rim") ||
                    name.Contains(
                        "alloy"))
                {
                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static bool IsFallbackBodyRenderer(
            Renderer renderer)
        {
            if (renderer == null ||
                IsWheelHierarchy(
                    renderer.transform))
            {
                return false;
            }

            Material[] materials =
                renderer.sharedMaterials;

            foreach (Material material in
                     materials)
            {
                if (material == null ||
                    IsGlassOrEmissionMaterial(
                        material))
                {
                    continue;
                }

                string lower =
                    material.name
                        .ToLowerInvariant();

                if (lower.Contains(
                        "body") ||
                    lower.Contains(
                        "paint") ||
                    lower.Contains(
                        "col1") ||
                    lower.Contains(
                        "col2") ||
                    lower.Contains(
                        "col3") ||
                    lower.Contains(
                        "col4") ||
                    lower.Contains(
                        "col5"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsStarterPaintMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string name =
                material.name
                    .ToLowerInvariant();

            if (name.Contains(
                    "emission") ||
                name.Contains(
                    "emissive") ||
                name.Contains(
                    "env") ||
                name.Contains(
                    "glass") ||
                name.Contains(
                    "window") ||
                name.Contains(
                    "mirror"))
            {
                return false;
            }

            return
                name.Contains(
                    "afrc_mat") ||
                name.Contains(
                    "starterpaint") ||
                name.Contains(
                    "col1") ||
                name.Contains(
                    "col2") ||
                name.Contains(
                    "col3") ||
                name.Contains(
                    "col4") ||
                name.Contains(
                    "col5");
        }

        private static bool IsGlassOrEmissionMaterial(
            Material material)
        {
            string lower =
                material.name
                    .ToLowerInvariant();

            return
                lower.Contains(
                    "glass") ||
                lower.Contains(
                    "window") ||
                lower.Contains(
                    "mirror") ||
                lower.Contains(
                    "emission") ||
                lower.Contains(
                    "emissive") ||
                lower.Contains(
                    "light") ||
                lower.Contains(
                    "lamp");
        }

        private static bool IsRimMaterialSet(
            Material[] materials)
        {
            foreach (Material material in
                     materials)
            {
                if (IsRimMaterial(
                        material))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRimMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string lower =
                material.name
                    .Replace(
                        " (Instance)",
                        string.Empty)
                    .ToLowerInvariant();

            return
                lower.Contains(
                    "rim") ||
                lower.Contains(
                    "wheel") ||
                lower.Contains(
                    "alloy") ||
                lower.Contains(
                    "disc") ||
                lower.Contains(
                    "disk");
        }

        private static bool IsRubberMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string lower =
                material.name
                    .Replace(
                        " (Instance)",
                        string.Empty)
                    .ToLowerInvariant();

            return
                lower.Contains(
                    "tire") ||
                lower.Contains(
                    "tyre") ||
                lower.Contains(
                    "rubber");
        }

        private static void SetMaterialColor(
            Material material,
            Color color)
        {
            if (material.HasProperty(
                    "_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    color);
            }

            if (material.HasProperty(
                    "_Color"))
            {
                material.SetColor(
                    "_Color",
                    color);
            }
        }

        private static void SetMaterialFloat(
            Material material,
            string propertyName,
            float value)
        {
            if (material.HasProperty(
                    propertyName))
            {
                material.SetFloat(
                    propertyName,
                    value);
            }
        }

        private void ClearVehicle()
        {
            if (vehicleInstance != null)
            {
                Destroy(
                    vehicleInstance);

                vehicleInstance =
                    null;
            }

            if (vehicleAnchor != null)
            {
                Transform existing =
                    vehicleAnchor.Find(
                        "Main Menu Underglow");

                if (existing != null)
                {
                    Destroy(
                        existing.gameObject);
                }
            }

            if (neonSurfaceMaterial != null)
            {
                Destroy(
                    neonSurfaceMaterial);

                neonSurfaceMaterial =
                    null;
            }

            loadedVehicleId =
                string.Empty;

            loadedColorIndex =
                -1;

            loadedWheelIndex =
                -1;

            loadedNeonIndex =
                -1;
        }

        private void OnDestroy()
        {
            ClearVehicle();

            if (previewCamera != null)
            {
                previewCamera.targetTexture =
                    null;
            }

            if (renderTexture != null)
            {
                renderTexture.Release();

                Destroy(
                    renderTexture);

                renderTexture =
                    null;
            }

            if (platformMaterial != null)
            {
                Destroy(
                    platformMaterial);

                platformMaterial =
                    null;
            }

            if (stageRoot != null)
            {
                Destroy(
                    stageRoot);

                stageRoot =
                    null;
            }
        }
    }
}
