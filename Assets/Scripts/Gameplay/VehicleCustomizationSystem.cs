using System;
using System.Collections;
using System.IO;
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

        private static readonly Color[] BodyColors =
        {
            new(0.86f, 0.10f, 0.12f, 1f),
            new(0.08f, 0.42f, 0.95f, 1f),
            new(0.95f, 0.70f, 0.08f, 1f),
            new(0.10f, 0.72f, 0.34f, 1f),
            new(0.58f, 0.18f, 0.92f, 1f),
            new(0.96f, 0.96f, 0.98f, 1f),
            new(0.08f, 0.09f, 0.11f, 1f)
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
        private readonly MaterialPropertyBlock block =
            new();

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
                "customization.summary",
                MotorCityLocalization.Text(ColorNameKey(SelectedColorIndex)),
                MotorCityLocalization.Text(StickerNameKey(SelectedStickerIndex)),
                MotorCityLocalization.Text(VinylNameKey(SelectedVinylIndex)),
                MotorCityLocalization.Text(WheelNameKey(SelectedWheelStyleIndex)),
                MotorCityLocalization.Text(NeonNameKey(SelectedNeonIndex)),
                PlateText());

        public void Initialize(
            ArcadeCarController targetCar,
            VehicleRosterSystem vehicleRoster)
        {
            car = targetCar;
            roster = vehicleRoster;

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
                BodyColors.Length;

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
            MotorCity.Persistence.MotorCitySaveService.SetInt(prefix + ".Sticker", SelectedStickerIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(prefix + ".Vinyl", SelectedVinylIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(prefix + ".Wheels", SelectedWheelStyleIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(prefix + ".Neon", SelectedNeonIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(prefix + ".Plate", SelectedPlateIndex);
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
                    BodyColors.Length - 1);

            SelectedStickerIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(prefix + ".Sticker", 0),
                    0,
                    3);

            SelectedVinylIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(prefix + ".Vinyl", 0),
                    0,
                    3);

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

            SelectedPlateIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(prefix + ".Plate", 0),
                    0,
                    5);

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

#if !UNITY_WEBGL || UNITY_EDITOR
            string directory =
                Path.Combine(
                    Application.persistentDataPath,
                    "MotorCityPhotos");

            Directory.CreateDirectory(
                directory);

            string fileName =
                "MotorCity_" +
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss") +
                ".png";

            string path =
                Path.Combine(
                    directory,
                    fileName);

            Texture2D screenshot =
                new Texture2D(
                    Screen.width,
                    Screen.height,
                    TextureFormat.RGB24,
                    false);

            screenshot.ReadPixels(
                new Rect(
                    0f,
                    0f,
                    Screen.width,
                    Screen.height),
                0,
                0);

            screenshot.Apply();
            File.WriteAllBytes(
                path,
                screenshot.EncodeToPNG());

            UnityEngine.Object.Destroy(
                screenshot);
#endif

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
                GetInt(id, "Color", 0, BodyColors.Length - 1);

            SelectedStickerIndex =
                GetInt(id, "Sticker", 0, 3);

            SelectedVinylIndex =
                GetInt(id, "Vinyl", 0, 3);

            SelectedWheelStyleIndex =
                GetInt(id, "Wheels", 0, 3);

            SelectedNeonIndex =
                GetInt(id, "Neon", 0, AccentColors.Length);

            SelectedPlateIndex =
                GetInt(id, "Plate", 0, 5);
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
            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Sticker"), SelectedStickerIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Vinyl"), SelectedVinylIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Wheels"), SelectedWheelStyleIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Neon"), SelectedNeonIndex);
            MotorCity.Persistence.MotorCitySaveService.SetInt(Key(id, "Plate"), SelectedPlateIndex);
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

            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(
                    true);

            Color color =
                BodyColors[
                    SelectedColorIndex];

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

        private void ApplyWheelStyle()
        {
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

            if (SelectedStickerIndex > 0)
                BuildSticker(
                    bounds);

            if (SelectedVinylIndex > 0)
                BuildVinyl(
                    bounds);

            if (SelectedNeonIndex > 0)
                BuildNeon(
                    bounds);

            BuildPlate(
                bounds);
        }

        private void BuildSticker(
            Bounds bounds)
        {
            Color color =
                AccentColors[
                    (SelectedStickerIndex - 1) %
                    AccentColors.Length];

            float side =
                bounds.extents.x +
                0.015f;

            float z =
                bounds.center.z +
                bounds.extents.z *
                0.15f;

            GameObject left =
                CreateFlatPart(
                    "Sticker L",
                    new Vector3(
                        -side,
                        bounds.center.y +
                        bounds.extents.y *
                        0.08f,
                        z),
                    new Vector3(
                        0.02f,
                        0.34f,
                        0.62f),
                    color);

            left.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    SelectedStickerIndex *
                    18f);

            GameObject right =
                CreateFlatPart(
                    "Sticker R",
                    new Vector3(
                        side,
                        bounds.center.y +
                        bounds.extents.y *
                        0.08f,
                        z),
                    new Vector3(
                        0.02f,
                        0.34f,
                        0.62f),
                    color);

            right.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    -SelectedStickerIndex *
                    18f);
        }

        private void BuildVinyl(
            Bounds bounds)
        {
            Color color =
                AccentColors[
                    (SelectedVinylIndex + 1) %
                    AccentColors.Length];

            float width =
                Mathf.Max(
                    0.10f,
                    bounds.size.x *
                    0.08f);

            int stripes =
                SelectedVinylIndex == 3
                    ? 3
                    : SelectedVinylIndex;

            for (int i = 0;
                 i < stripes;
                 i++)
            {
                float offset =
                    (i -
                     (stripes - 1) *
                     0.5f) *
                    width *
                    1.8f;

                CreateFlatPart(
                    "Vinyl " + i,
                    new Vector3(
                        bounds.center.x +
                        offset,
                        bounds.max.y +
                        0.012f,
                        bounds.center.z),
                    new Vector3(
                        width,
                        0.018f,
                        bounds.size.z *
                        0.78f),
                    color);
            }
        }

        private void BuildNeon(
            Bounds bounds)
        {
            Color color =
                AccentColors[
                    SelectedNeonIndex - 1];

            CreateNeonPart(
                new Vector3(
                    0f,
                    bounds.min.y -
                    0.035f,
                    bounds.center.z),
                new Vector3(
                    bounds.size.x *
                    0.72f,
                    0.025f,
                    bounds.size.z *
                    0.68f),
                color);
        }

        private void BuildPlate(
            Bounds bounds)
        {
            GameObject plate =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            plate.name =
                "Motor City Plate";

            plate.transform.SetParent(
                cosmeticsRoot.transform,
                false);

            plate.transform.localPosition =
                new Vector3(
                    bounds.center.x,
                    bounds.center.y -
                    bounds.extents.y *
                    0.10f,
                    bounds.min.z -
                    0.025f);

            plate.transform.localScale =
                new Vector3(
                    0.58f,
                    0.18f,
                    0.025f);

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
            }

            GameObject textObject =
                new(
                    "Plate Text");

            textObject.transform.SetParent(
                plate.transform,
                false);

            textObject.transform.localPosition =
                new Vector3(
                    0f,
                    0f,
                    -0.56f);

            textObject.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    180f,
                    0f);

            TextMesh mesh =
                textObject.AddComponent<TextMesh>();

            mesh.text =
                PlateText();

            mesh.anchor =
                TextAnchor.MiddleCenter;

            mesh.alignment =
                TextAlignment.Center;

            mesh.fontSize = 42;
            mesh.characterSize = 0.12f;
            mesh.color = Color.black;
        }

        private GameObject CreateFlatPart(
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

        private void CreateNeonPart(
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            GameObject part =
                CreateFlatPart(
                    "Neon",
                    localPosition,
                    localScale,
                    color);

            Renderer renderer =
                part.GetComponent<Renderer>();

            if (renderer == null)
                return;

            renderer.sharedMaterial =
                neonMaterial;

            renderer.GetPropertyBlock(
                block);

            block.SetColor(
                "_BaseColor",
                color);

            block.SetColor(
                "_Color",
                color);

            block.SetColor(
                "_EmissionColor",
                color * 2.2f);

            renderer.SetPropertyBlock(
                block);

            block.Clear();
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

        private static string ColorNameKey(
            int index)
        {
            return
                index switch
                {
                    1 => "customization.color_blue",
                    2 => "customization.color_yellow",
                    3 => "customization.color_green",
                    4 => "customization.color_purple",
                    5 => "customization.color_white",
                    6 => "customization.color_black",
                    _ => "customization.color_red"
                };
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
                index == 0
                    ? "customization.neon.0"
                    : "customization.neon.on";
        }
    }
}
