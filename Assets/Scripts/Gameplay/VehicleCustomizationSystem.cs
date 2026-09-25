using System;
using System.Collections;
using MotorCity.Localization;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.Gameplay
{
    public sealed class VehicleCustomizationSystem : MonoBehaviour
    {
        private const string RuntimeVisualName =
            "MotorCityVehicleVisual_Runtime";

        private const string CosmeticsRootName =
            "MotorCityCosmetics_Runtime";

        private static readonly Color[] StreetBodyColors =
        {
            // Street uses its existing authored StarterPaint_0..4 materials.
            // These values are only a fallback if an authored material is missing.
            new(0.95f, 0.70f, 0.08f, 1f),
            new(0.08f, 0.42f, 0.95f, 1f),
            new(0.86f, 0.10f, 0.12f, 1f),
            new(0.42f, 0.44f, 0.48f, 1f),
            new(0.58f, 0.18f, 0.92f, 1f)
        };

        private static readonly Color[] DesignersoupBodyColors =
        {
            new(0.78f, 0.035f, 0.045f, 1f), // Rallye Red
            new(1.00f, 0.64f, 0.035f, 1f), // Pheonix Yellow
            new(0.055f, 0.30f, 0.78f, 1f), // Boost Pearl Blue
            new(0.018f, 0.022f, 0.028f, 1f), // Black Pearl
            new(0.33f, 0.36f, 0.39f, 1f), // Sonic Grey Pearl
            new(0.93f, 0.93f, 0.90f, 1f)  // Championship White
        };

        private static readonly Color[] MuscleBodyColors =
        {
            new(0.68f, 0.055f, 0.04f, 1f),
            new(0.92f, 0.30f, 0.035f, 1f),
            new(0.055f, 0.22f, 0.64f, 1f),
            new(0.025f, 0.028f, 0.035f, 1f),
            new(0.72f, 0.74f, 0.76f, 1f),
            new(0.94f, 0.93f, 0.89f, 1f)
        };

        private static readonly Color[] HybridBodyColors =
        {
            new(0.025f, 0.028f, 0.035f, 1f),
            new(0.045f, 0.28f, 0.80f, 1f),
            new(0.85f, 0.56f, 0.08f, 1f),
            new(0.06f, 0.52f, 0.20f, 1f),
            new(0.12f, 0.62f, 0.92f, 1f),
            new(0.46f, 0.12f, 0.72f, 1f),
            new(0.78f, 0.045f, 0.04f, 1f),
            new(0.62f, 0.65f, 0.70f, 1f),
            new(0.94f, 0.94f, 0.92f, 1f),
            new(0.96f, 0.66f, 0.04f, 1f)
        };

        private static readonly Color[] TristarBodyColors =
        {
            new(0.80f, 0.035f, 0.035f, 1f),
            new(0.96f, 0.62f, 0.025f, 1f),
            new(0.03f, 0.32f, 0.86f, 1f),
            new(0.018f, 0.022f, 0.03f, 1f),
            new(0.36f, 0.39f, 0.44f, 1f),
            new(0.94f, 0.94f, 0.92f, 1f)
        };

        private static readonly Color[] VanBodyColors =
        {
            new(0.93f, 0.93f, 0.90f, 1f),
            new(0.055f, 0.34f, 0.75f, 1f),
            new(0.72f, 0.045f, 0.035f, 1f),
            new(0.95f, 0.63f, 0.035f, 1f),
            new(0.23f, 0.25f, 0.28f, 1f),
            new(0.20f, 0.66f, 0.50f, 1f)
        };

        private static readonly Color[] DocLoreanBodyColors =
        {
            new(0.58f, 0.61f, 0.64f, 1f),
            new(0.018f, 0.022f, 0.03f, 1f),
            new(0.025f, 0.34f, 0.82f, 1f),
            new(0.72f, 0.045f, 0.035f, 1f),
            new(0.94f, 0.94f, 0.92f, 1f)
        };

        private static readonly Color[] AccentColors =
        {
            new(0.12f, 0.70f, 1f, 1f),
            new(1f, 0.45f, 0.12f, 1f),
            new(0.75f, 0.22f, 1f, 1f),
            new(0.18f, 1f, 0.48f, 1f),
            new(1f, 0.82f, 0.12f, 1f)
        };

        private ArcadeCarController car;
        private VehicleRosterSystem roster;
        private MaterialPropertyBlock block;

        private GameObject cosmeticsRoot;
        private Material flatMaterial;
        private Material neonMaterial;
        private bool photoInProgress;

        public event Action CustomizationChanged;
        public event Action PhotoTaken;

        public int SelectedColorIndex { get; private set; }
        public int SelectedStickerIndex { get; private set; }
        public int SelectedVinylIndex { get; private set; }
        public int SelectedWheelStyleIndex { get; private set; }
        public int SelectedNeonIndex { get; private set; }
        public int SelectedPlateIndex { get; private set; }
        public int SelectedPresetSlot { get; private set; }

        public int PresetSlotNumber =>
            SelectedPresetSlot + 1;

        public string PresetLine =>
            MotorCityLocalization.Format(
                "customization.preset_slot",
                PresetSlotNumber);

        public string GarageLine =>
            MotorCityLocalization.Format(
                "customization.simple_summary",
                MotorCityLocalization.Text(ColorNameKey(SelectedColorIndex)),
                MotorCityLocalization.Text(WheelNameKey(SelectedWheelStyleIndex)),
                MotorCityLocalization.Text(NeonNameKey(SelectedNeonIndex)));

        public void Initialize(
            ArcadeCarController targetCar,
            VehicleRosterSystem vehicleRoster)
        {
            car = targetCar;
            roster = vehicleRoster;
            block ??= new MaterialPropertyBlock();

            if (roster != null)
                roster.VehicleChanged += OnVehicleChanged;

            BuildSharedMaterials();
            LoadForSelectedVehicle();
            ApplyAll();
        }

        private void OnDestroy()
        {
            if (roster != null)
                roster.VehicleChanged -= OnVehicleChanged;

            if (flatMaterial != null)
                Destroy(flatMaterial);

            if (neonMaterial != null)
                Destroy(neonMaterial);
        }

        public void CycleBodyColor()
        {
            SelectedColorIndex =
                (SelectedColorIndex + 1) %
                BodyColorCountForCurrentVehicle();

            Changed();
        }

        public void CycleSticker()
        {
            SelectedStickerIndex =
                (SelectedStickerIndex + 1) % 4;

            Changed();
        }

        public void CycleVinyl()
        {
            SelectedVinylIndex =
                (SelectedVinylIndex + 1) % 4;

            Changed();
        }

        public void CycleWheelStyle()
        {
            SelectedWheelStyleIndex =
                (SelectedWheelStyleIndex + 1) % 4;

            Changed();
        }

        public void CycleNeon()
        {
            SelectedNeonIndex =
                (SelectedNeonIndex + 1) %
                (AccentColors.Length + 1);

            Changed();
        }

        public void CyclePlate()
        {
            SelectedPlateIndex =
                (SelectedPlateIndex + 1) % 6;

            Changed();
        }

        public void SelectPresetSlot(
            int slot)
        {
            SelectedPresetSlot =
                Mathf.Clamp(
                    slot,
                    0,
                    2);
        }

        public void SaveSelectedPreset()
        {
            SavePreset(
                SelectedPresetSlot);
        }

        public bool LoadSelectedPreset()
        {
            return
                LoadPreset(
                    SelectedPresetSlot);
        }

        public void SavePreset(
            int slot)
        {
            slot =
                Mathf.Clamp(
                    slot,
                    0,
                    2);

            string prefix =
                PresetPrefix(
                    slot);

            MotorCity.Persistence.MotorCitySaveService.SetInt(prefix + ".Color", SelectedColorIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(prefix + ".Wheels", SelectedWheelStyleIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(prefix + ".Neon", SelectedNeonIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(prefix + ".Exists", 1);
            MotorCity.Persistence.MotorCitySaveService.Save();

            SelectedPresetSlot =
                slot;

            CustomizationChanged?.Invoke();
        }

        public bool LoadPreset(
            int slot)
        {
            slot =
                Mathf.Clamp(
                    slot,
                    0,
                    2);

            string prefix =
                PresetPrefix(
                    slot);

            if (MotorCity.Persistence.MotorCitySaveService.GetInt(
                    prefix + ".Exists",
                    0) == 0)
            {
                return false;
            }

            SelectedColorIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(prefix + ".Color", 0),
                    0,
                    BodyColorCountForCurrentVehicle() - 1);

            SelectedStickerIndex = 0;
            SelectedVinylIndex = 0;

            SelectedWheelStyleIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(prefix + ".Wheels", 0),
                    0,
                    3);

            SelectedNeonIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(prefix + ".Neon", 0),
                    0,
                    AccentColors.Length);

            SelectedPlateIndex = 0;

            SelectedPresetSlot =
                slot;

            SaveForSelectedVehicle();
            ApplyAll();
            CustomizationChanged?.Invoke();
            return true;
        }

        public void CapturePhoto()
        {
            if (photoInProgress)
                return;

            StartCoroutine(
                CapturePhotoRoutine());
        }

        private IEnumerator CapturePhotoRoutine()
        {
            photoInProgress = true;

            yield return
                new WaitForEndOfFrame();

            PhotoTaken?.Invoke();


            photoInProgress = false;
        }

        private void Changed()
        {
            SaveForSelectedVehicle();
            ApplyAll();
            CustomizationChanged?.Invoke();
        }

        private void OnVehicleChanged()
        {
            LoadForSelectedVehicle();
            ApplyAll();
        }

        private void LoadForSelectedVehicle()
        {
            string id =
                VehicleId();

            SelectedColorIndex =
                GetInt(
                    id,
                    "Color",
                    0,
                    BodyColorCountForCurrentVehicle() - 1);

            SelectedStickerIndex = 0;
            SelectedVinylIndex = 0;

            SelectedWheelStyleIndex =
                GetInt(id, "Wheels", 0, 3);

            SelectedNeonIndex =
                GetInt(id, "Neon", 0, AccentColors.Length);

            SelectedPlateIndex = 0;
        }

        private int BodyColorCountForCurrentVehicle()
        {
            return
                BodyColorsForCurrentVehicle()
                    .Length;
        }

        private Color[] BodyColorsForCurrentVehicle()
        {
            return
                VehicleId() switch
                {
                    "street" =>
                        StreetBodyColors,

                    "tois08" or
                    "toro86" or
                    "stuttgart996" =>
                        DesignersoupBodyColors,

                    "muscle10" =>
                        MuscleBodyColors,

                    "hybrid" =>
                        HybridBodyColors,

                    "tristar" =>
                        TristarBodyColors,

                    "van" =>
                        VanBodyColors,

                    "doclorean" =>
                        DocLoreanBodyColors,

                    _ =>
                        DesignersoupBodyColors
                };
        }

        private int GetInt(
            string vehicleId,
            string suffix,
            int min,
            int max)
        {
            return
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        Key(
                            vehicleId,
                            suffix),
                        0),
                    min,
                    max);
        }

        private void SaveForSelectedVehicle()
        {
            string id =
                VehicleId();

            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Color"), SelectedColorIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Wheels"), SelectedWheelStyleIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Neon"), SelectedNeonIndex);
            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private void ApplyAll()
        {
            if (car == null)
                return;

            ClearCosmetics();
            ApplyBodyColor();
            ApplyWheelStyle();
            BuildCosmeticGeometry();
        }

        private void ApplyBodyColor()
        {
            Transform visual =
                FindVisualRoot();

            if (visual == null)
                return;

            if (VehicleId() == "street" &&
                ApplyAuthoredStarterPaint(
                    visual))
            {
                return;
            }

            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(
                    true);

            Color[] bodyColors =
                BodyColorsForCurrentVehicle();

            Color color =
                bodyColors[
                    Mathf.Clamp(
                        SelectedColorIndex,
                        0,
                        bodyColors.Length - 1)];

            int paintedSlots =
                0;

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    IsWheelLike(
                        renderer.transform.name))
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                for (int index = 0;
                     index < materials.Length;
                     index++)
                {
                    Material material =
                        materials[index];

                    if (!LooksPaintable(
                            renderer.transform.name,
                            material))
                    {
                        continue;
                    }

                    ApplyColorBlock(
                        renderer,
                        material,
                        index,
                        color);

                    paintedSlots++;
                }
            }

            if (paintedSlots > 0)
                return;

            Renderer fallback =
                FindLargestBodyRenderer(
                    renderers);

            if (fallback == null)
                return;

            Material[] fallbackMaterials =
                fallback.sharedMaterials;

            for (int i = 0;
                 i < fallbackMaterials.Length;
                 i++)
            {
                Material material =
                    fallbackMaterials[i];

                if (material == null ||
                    IsExcludedMaterial(
                        material.name))
                {
                    continue;
                }

                ApplyColorBlock(
                    fallback,
                    material,
                    i,
                    color);
            }
        }

        private bool ApplyAuthoredStarterPaint(
            Transform visual)
        {
            Material paint =
                Resources.Load<Material>(
                    "MotorCity/VehiclePaints/StarterPaint_" +
                    (SelectedColorIndex % 5));

            if (paint == null)
                return false;

            bool applied = false;

            foreach (Renderer renderer in
                     visual.GetComponentsInChildren<Renderer>(
                         true))
            {
                if (renderer == null ||
                    IsWheelLike(
                        renderer.transform.name))
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                bool changed = false;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material material =
                        materials[i];

                    if (!IsStarterPaintMaterial(
                            material))
                    {
                        continue;
                    }

                    materials[i] =
                        paint;

                    changed = true;
                    applied = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials =
                        materials;

                    // Remove any old tint left by the previous generic paint
                    // implementation so the authored texture is shown exactly.
                    renderer.SetPropertyBlock(
                        null);
                }
            }

            return applied;
        }

        private static bool IsStarterPaintMaterial(
            Material material)
        {
            if (material == null)
                return false;

            string name =
                material.name
                    .ToLowerInvariant();

            if (name.Contains("emission") ||
                name.Contains("emissive") ||
                name.Contains("env") ||
                name.Contains("glass") ||
                name.Contains("window") ||
                name.Contains("mirror"))
            {
                return false;
            }

            return
                name.Contains("afrc_mat") ||
                name.Contains("starterpaint") ||
                name.Contains("col1") ||
                name.Contains("col2") ||
                name.Contains("col3") ||
                name.Contains("col4") ||
                name.Contains("col5");
        }

        private void ApplyWheelStyle()
        {
            Transform visual =
                FindVisualRoot();

            if (visual == null)
                return;

            Renderer[] renderers =
                car.GetComponentsInChildren<Renderer>(
                    true);

            Color wheelColor =
                SelectedWheelStyleIndex switch
                {
                    1 =>
                        new Color(
                            0.85f,
                            0.87f,
                            0.92f,
                            1f),
                    2 =>
                        new Color(
                            0.07f,
                            0.08f,
                            0.10f,
                            1f),
                    3 =>
                        new Color(
                            0.95f,
                            0.68f,
                            0.12f,
                            1f),
                    _ =>
                        new Color(
                            0.34f,
                            0.36f,
                            0.40f,
                            1f)
                };

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    renderer is TrailRenderer ||
                    renderer is ParticleSystemRenderer ||
                    !IsWheelLike(
                        renderer.transform.name))
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

                    if (material == null)
                        continue;

                    ApplyColorBlock(
                        renderer,
                        material,
                        i,
                        wheelColor);
                }
            }
        }

        private void BuildCosmeticGeometry()
        {
            Bounds bounds =
                ResolveCarBounds();

            cosmeticsRoot =
                new GameObject(
                    CosmeticsRootName);

            cosmeticsRoot.transform.SetParent(
                car.transform,
                false);

            if (SelectedNeonIndex > 0)
                BuildNeon(
                    bounds);
        }

        private void BuildSticker(
            Bounds bounds)
        {
            Color color =
                AccentColors[
                    (SelectedStickerIndex - 1) %
                    AccentColors.Length];

            float sideX =
                Mathf.Max(
                    0.55f,
                    bounds.extents.x * 0.90f);

            float centerY =
                bounds.center.y +
                bounds.extents.y * 0.02f;

            float centerZ =
                bounds.center.z -
                bounds.extents.z * 0.15f;

            float length =
                Mathf.Clamp(
                    bounds.size.z * 0.22f,
                    0.45f,
                    0.82f);

            float height =
                Mathf.Clamp(
                    bounds.size.y * 0.18f,
                    0.12f,
                    0.24f);

            CreateSideAccent(
                "Sticker L",
                -sideX,
                centerY,
                centerZ,
                height,
                length,
                color,
                true);

            CreateSideAccent(
                "Sticker R",
                sideX,
                centerY,
                centerZ,
                height,
                length,
                color,
                false);
        }

        private void BuildVinyl(
            Bounds bounds)
        {
            Color color =
                AccentColors[
                    (SelectedVinylIndex + 1) %
                    AccentColors.Length];

            float stripeWidth =
                Mathf.Clamp(
                    bounds.size.x * 0.045f,
                    0.055f,
                    0.11f);

            int stripes =
                SelectedVinylIndex == 3
                    ? 2
                    : 1;

            float gap =
                stripeWidth * 1.55f;

            for (int i = 0;
                 i < stripes;
                 i++)
            {
                float x =
                    bounds.center.x +
                    (i - (stripes - 1) * 0.5f) *
                    gap;

                CreateTopAccent(
                    "Vinyl Hood " + i,
                    new Vector3(
                        x,
                        bounds.max.y + 0.004f,
                        bounds.center.z +
                        bounds.extents.z * 0.31f),
                    new Vector3(
                        stripeWidth,
                        0.006f,
                        bounds.size.z * 0.20f),
                    color);

                CreateTopAccent(
                    "Vinyl Roof " + i,
                    new Vector3(
                        x,
                        bounds.max.y + 0.006f,
                        bounds.center.z -
                        bounds.extents.z * 0.02f),
                    new Vector3(
                        stripeWidth,
                        0.006f,
                        bounds.size.z * 0.22f),
                    color);

                CreateTopAccent(
                    "Vinyl Trunk " + i,
                    new Vector3(
                        x,
                        bounds.max.y + 0.004f,
                        bounds.center.z -
                        bounds.extents.z * 0.32f),
                    new Vector3(
                        stripeWidth,
                        0.006f,
                        bounds.size.z * 0.13f),
                    color);
            }
        }

        private void BuildNeon(
            Bounds bounds)
        {
            Color color =
                AccentColors[
                    SelectedNeonIndex - 1];

            GameObject lightObject =
                new(
                    "Underglow Light");

            lightObject.transform.SetParent(
                cosmeticsRoot.transform,
                false);

            lightObject.transform.localPosition =
                new Vector3(
                    bounds.center.x,
                    bounds.min.y + 0.05f,
                    bounds.center.z);

            Light light =
                lightObject.AddComponent<Light>();

            light.type =
                LightType.Point;
            light.color =
                color;
            light.range =
                Mathf.Clamp(
                    bounds.size.z * 0.72f,
                    2.2f,
                    4.2f);
            light.intensity =
                2.4f;
            light.shadows =
                LightShadows.None;
            light.renderMode =
                LightRenderMode.ForcePixel;
        }

        private void BuildPlate(
            Bounds bounds)
        {
            Vector3 platePosition =
                new(
                    bounds.center.x,
                    bounds.center.y -
                    bounds.extents.y * 0.08f,
                    bounds.min.z - 0.018f);

            GameObject plate =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            plate.name =
                "Motor City Plate";

            plate.transform.SetParent(
                cosmeticsRoot.transform,
                false);

            plate.transform.localPosition =
                platePosition;

            plate.transform.localScale =
                new Vector3(
                    Mathf.Clamp(
                        bounds.size.x * 0.28f,
                        0.42f,
                        0.62f),
                    0.16f,
                    0.018f);

            Collider collider =
                plate.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            Renderer renderer =
                plate.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    flatMaterial;

                renderer.GetPropertyBlock(
                    block);

                Color plateColor =
                    new(
                        0.92f,
                        0.94f,
                        0.90f,
                        1f);

                block.SetColor(
                    "_BaseColor",
                    plateColor);
                block.SetColor(
                    "_Color",
                    plateColor);

                renderer.SetPropertyBlock(
                    block);
                block.Clear();
            }

            GameObject textObject =
                new(
                    "Plate Text");

            textObject.transform.SetParent(
                cosmeticsRoot.transform,
                false);

            textObject.transform.localPosition =
                platePosition +
                new Vector3(
                    0f,
                    0f,
                    -0.016f);

            textObject.transform.localRotation =
                Quaternion.identity;

            TextMesh mesh =
                textObject.AddComponent<TextMesh>();

            mesh.text =
                PlateText();
            mesh.anchor =
                TextAnchor.MiddleCenter;
            mesh.alignment =
                TextAlignment.Center;
            mesh.fontSize =
                48;
            mesh.characterSize =
                0.055f;
            mesh.color =
                new Color(
                    0.05f,
                    0.06f,
                    0.07f,
                    1f);
        }

        private void CreateSideAccent(
            string name,
            float x,
            float y,
            float z,
            float height,
            float length,
            Color color,
            bool left)
        {
            GameObject accent =
                CreateAccentPart(
                    name,
                    new Vector3(
                        x,
                        y,
                        z),
                    new Vector3(
                        0.008f,
                        height,
                        length),
                    color);

            accent.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    left
                        ? -12f
                        : 12f);
        }

        private void CreateTopAccent(
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            CreateAccentPart(
                name,
                localPosition,
                localScale,
                color);
        }

        private GameObject CreateAccentPart(
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            GameObject part =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            part.name =
                name;

            part.transform.SetParent(
                cosmeticsRoot.transform,
                false);

            part.transform.localPosition =
                localPosition;

            part.transform.localScale =
                localScale;

            Collider collider =
                part.GetComponent<Collider>();

            if (collider != null)
                Destroy(collider);

            Renderer renderer =
                part.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    flatMaterial;

                renderer.GetPropertyBlock(
                    block);

                block.SetColor(
                    "_BaseColor",
                    color);

                block.SetColor(
                    "_Color",
                    color);

                renderer.SetPropertyBlock(
                    block);

                block.Clear();
            }

            return part;
        }

        private void BuildSharedMaterials()
        {
            Shader lit =
                Shader.Find(
                    "Universal Render Pipeline/Lit") ??
                Shader.Find(
                    "Standard");

            flatMaterial =
                new Material(
                    lit)
                {
                    name =
                        "MotorCity Cosmetic"
                };

            Shader unlit =
                Shader.Find(
                    "Universal Render Pipeline/Unlit") ??
                Shader.Find(
                    "Unlit/Color") ??
                lit;

            neonMaterial =
                new Material(
                    unlit)
                {
                    name =
                        "MotorCity Neon"
                };

            if (neonMaterial.HasProperty(
                    "_EmissionColor"))
            {
                neonMaterial.EnableKeyword(
                    "_EMISSION");
            }
        }

        private void ClearCosmetics()
        {
            Transform existing =
                car.transform.Find(
                    CosmeticsRootName);

            if (existing != null)
                Destroy(existing.gameObject);

            cosmeticsRoot =
                null;
        }

        private Transform FindVisualRoot()
        {
            return
                car.transform.Find(
                    RuntimeVisualName) ??
                car.transform;
        }

        private Bounds ResolveCarBounds()
        {
            Renderer[] renderers =
                FindVisualRoot()
                    .GetComponentsInChildren<Renderer>(
                        true);

            bool initialized =
                false;

            Bounds local =
                new(
                    Vector3.zero,
                    new Vector3(
                        1.8f,
                        1.3f,
                        4.2f));

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    renderer.transform.name.Contains(
                        "Wheel",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Bounds world =
                    renderer.bounds;

                Vector3 center =
                    car.transform.InverseTransformPoint(
                        world.center);

                Vector3 size =
                    car.transform.InverseTransformVector(
                        world.size);

                size =
                    new Vector3(
                        Mathf.Abs(size.x),
                        Mathf.Abs(size.y),
                        Mathf.Abs(size.z));

                Bounds item =
                    new(
                        center,
                        size);

                if (!initialized)
                {
                    local = item;
                    initialized = true;
                }
                else
                {
                    local.Encapsulate(
                        item);
                }
            }

            return local;
        }

        private void ApplyColorBlock(
            Renderer renderer,
            Material material,
            int materialIndex,
            Color color)
        {
            renderer.GetPropertyBlock(
                block,
                materialIndex);

            if (material.HasProperty(
                    "_BaseColor"))
            {
                block.SetColor(
                    "_BaseColor",
                    color);
            }

            if (material.HasProperty(
                    "_Color"))
            {
                block.SetColor(
                    "_Color",
                    color);
            }

            renderer.SetPropertyBlock(
                block,
                materialIndex);

            block.Clear();
        }

        private static bool LooksPaintable(
            string objectName,
            Material material)
        {
            string objectLower =
                (objectName ?? string.Empty)
                    .ToLowerInvariant();

            string materialLower =
                material != null
                    ? material.name.ToLowerInvariant()
                    : string.Empty;

            if (IsWheelLike(
                    objectLower) ||
                IsExcludedMaterial(
                    materialLower))
            {
                return false;
            }

            return
                objectLower.Contains("body") ||
                objectLower.Contains("hood") ||
                objectLower.Contains("bonnet") ||
                objectLower.Contains("bumper") ||
                objectLower.Contains("door") ||
                objectLower.Contains("fender") ||
                objectLower.Contains("spoiler") ||
                materialLower.Contains("body") ||
                materialLower.Contains("paint") ||
                materialLower.Contains("carpaint") ||
                materialLower.Contains("vehicle");
        }

        private static bool IsWheelLike(
            string name)
        {
            string lower =
                (name ?? string.Empty)
                    .ToLowerInvariant();

            return
                lower.Contains("wheel") ||
                lower.Contains("tire") ||
                lower.Contains("tyre") ||
                lower.Contains("rim");
        }

        private static bool IsExcludedMaterial(
            string name)
        {
            string lower =
                (name ?? string.Empty)
                    .ToLowerInvariant();

            return
                lower.Contains("glass") ||
                lower.Contains("window") ||
                lower.Contains("chrome") ||
                lower.Contains("light") ||
                lower.Contains("lamp") ||
                lower.Contains("interior");
        }

        private static Renderer FindLargestBodyRenderer(
            Renderer[] renderers)
        {
            Renderer best = null;
            float bestVolume = 0f;

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    IsWheelLike(
                        renderer.transform.name))
                {
                    continue;
                }

                Vector3 size =
                    renderer.bounds.size;

                float volume =
                    size.x *
                    size.y *
                    size.z;

                if (volume <= bestVolume)
                    continue;

                best = renderer;
                bestVolume = volume;
            }

            return best;
        }

        private string VehicleId()
        {
            return
                roster != null
                    ? roster.SelectedId
                    : "street";
        }

        private static string Key(
            string vehicleId,
            string suffix)
        {
            return
                "MotorCity.Customization." +
                vehicleId +
                "." +
                suffix;
        }

        private string PresetPrefix(
            int slot)
        {
            return
                "MotorCity.Customization." +
                VehicleId() +
                ".Preset." +
                slot;
        }

        private string PlateText()
        {
            string[] plates =
            {
                "MC 01",
                "TURBO",
                "CITY",
                "DRIFT",
                "NIKA",
                "7-14"
            };

            return
                plates[
                    Mathf.Clamp(
                        SelectedPlateIndex,
                        0,
                        plates.Length - 1)];
        }

        private string ColorNameKey(
            int index)
        {
            string vehicleId =
                VehicleId();

            if (vehicleId == "street")
            {
                return
                    index switch
                    {
                        0 => "customization.color_yellow",
                        1 => "customization.color_blue",
                        2 => "customization.color_red",
                        3 => "customization.color_gray",
                        _ => "customization.color_purple"
                    };
            }

            if (vehicleId == "tois08" ||
                vehicleId == "toro86" ||
                vehicleId == "stuttgart996")
            {
                return
                    "customization.designersoup." +
                    Mathf.Clamp(
                        index,
                        0,
                        DesignersoupBodyColors.Length - 1);
            }

            string prefix =
                vehicleId switch
                {
                    "muscle10" =>
                        "customization.muscle.",

                    "hybrid" =>
                        "customization.hybrid.",

                    "tristar" =>
                        "customization.tristar.",

                    "van" =>
                        "customization.van.",

                    "doclorean" =>
                        "customization.doclorean.",

                    _ =>
                        "customization.designersoup."
                };

            return
                prefix +
                Mathf.Clamp(
                    index,
                    0,
                    BodyColorCountForCurrentVehicle() - 1);
        }

        private static string StickerNameKey(
            int index)
        {
            return
                "customization.sticker." +
                Mathf.Clamp(
                    index,
                    0,
                    3);
        }

        private static string VinylNameKey(
            int index)
        {
            return
                "customization.vinyl." +
                Mathf.Clamp(
                    index,
                    0,
                    3);
        }

        private static string WheelNameKey(
            int index)
        {
            return
                "customization.wheel." +
                Mathf.Clamp(
                    index,
                    0,
                    3);
        }

        private static string NeonNameKey(
            int index)
        {
            return
                Mathf.Clamp(
                    index,
                    0,
                    AccentColors.Length) switch
                {
                    1 => "customization.neon.blue",
                    2 => "customization.neon.orange",
                    3 => "customization.neon.purple",
                    4 => "customization.neon.green",
                    5 => "customization.neon.yellow",
                    _ => "customization.neon.0"
                };
        }
    }
}
