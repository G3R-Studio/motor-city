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
        private float dayNightResolveTimer;

        private void Awake()
        {
            car =
                GetComponent<ArcadeCarController>();
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

            float runningMultiplier =
                Mathf.Lerp(
                    0.28f,
                    1.15f,
                    night);

            float brakeMultiplier =
                Mathf.Lerp(
                    1.6f,
                    2.65f,
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

                // If a model has one shared emissive material for both front
                // and rear lamps (the starter ARCADE car does), keep the
                // original texture colours and only apply night-time glow.
                // Brake boosting is limited to materials/renderers that are
                // explicitly identifiable as rear lamps.
                float multiplier =
                    braking &&
                    binding.RearSpecific
                        ? brakeMultiplier
                        : runningMultiplier;

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
                        renderer.transform))
                {
                    continue;
                }

                BindExistingLampMaterials(
                    renderer);
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
