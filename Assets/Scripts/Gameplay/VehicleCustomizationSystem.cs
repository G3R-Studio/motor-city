using System;
using MotorCity.Localization;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class VehicleCustomizationSystem :
        MonoBehaviour
    {
        private const string RuntimeVisualName =
            "MotorCityVehicleVisual_Runtime";

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

        private ArcadeCarController car;
        private VehicleRosterSystem roster;
        private readonly MaterialPropertyBlock block =
            new();

        public event Action CustomizationChanged;

        public int SelectedColorIndex { get; private set; }

        public string CurrentColorName =>
            MotorCityLocalization.Text(
                ColorNameKey(
                    SelectedColorIndex));

        public string GarageLine =>
            MotorCityLocalization.Format(
                "customization.color_line",
                CurrentColorName);

        public void Initialize(
            ArcadeCarController targetCar,
            VehicleRosterSystem vehicleRoster)
        {
            car =
                targetCar;
            roster =
                vehicleRoster;

            if (roster != null)
            {
                roster.VehicleChanged +=
                    OnVehicleChanged;
            }

            LoadForSelectedVehicle();
            ApplyCurrentColor();
        }

        private void OnDestroy()
        {
            if (roster != null)
            {
                roster.VehicleChanged -=
                    OnVehicleChanged;
            }
        }

        public void CycleBodyColor()
        {
            if (roster == null ||
                car == null)
            {
                return;
            }

            SelectedColorIndex =
                (SelectedColorIndex + 1) %
                BodyColors.Length;

            SaveForSelectedVehicle();
            ApplyCurrentColor();

            CustomizationChanged?.Invoke();
        }

        private void OnVehicleChanged()
        {
            LoadForSelectedVehicle();

            // The vehicle visual is replaced synchronously by VehicleRoster.
            ApplyCurrentColor();
        }

        private void LoadForSelectedVehicle()
        {
            string vehicleId =
                roster != null
                    ? roster.SelectedId
                    : "street";

            SelectedColorIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        ColorKey(
                            vehicleId),
                        0),
                    0,
                    BodyColors.Length - 1);
        }

        private void SaveForSelectedVehicle()
        {
            string vehicleId =
                roster != null
                    ? roster.SelectedId
                    : "street";

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                ColorKey(
                    vehicleId),
                SelectedColorIndex);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private void ApplyCurrentColor()
        {
            if (car == null)
                return;

            Transform visual =
                car.transform.Find(
                    RuntimeVisualName);

            if (visual == null)
            {
                visual =
                    car.transform;
            }

            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(
                    true);

            Color color =
                BodyColors[
                    Mathf.Clamp(
                        SelectedColorIndex,
                        0,
                        BodyColors.Length - 1)];

            int paintedSlots =
                0;

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    ShouldSkipRenderer(
                        renderer.transform))
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                if (materials == null ||
                    materials.Length == 0)
                {
                    continue;
                }

                for (int index = 0;
                     index < materials.Length;
                     index++)
                {
                    Material material =
                        materials[index];

                    if (!LooksPaintable(
                            renderer.transform,
                            material))
                    {
                        continue;
                    }

                    renderer.GetPropertyBlock(
                        block,
                        index);

                    if (material != null &&
                        material.HasProperty(
                            "_BaseColor"))
                    {
                        block.SetColor(
                            "_BaseColor",
                            color);
                    }

                    if (material != null &&
                        material.HasProperty(
                            "_Color"))
                    {
                        block.SetColor(
                            "_Color",
                            color);
                    }

                    renderer.SetPropertyBlock(
                        block,
                        index);

                    block.Clear();
                    paintedSlots++;
                }
            }

            if (paintedSlots > 0)
                return;

            // Fallback for unusually named imported cars: tint the largest
            // non-wheel renderer, never shared materials themselves.
            Renderer fallback =
                FindLargestBodyRenderer(
                    renderers);

            if (fallback == null)
                return;

            Material[] fallbackMaterials =
                fallback.sharedMaterials;

            for (int index = 0;
                 index < fallbackMaterials.Length;
                 index++)
            {
                Material material =
                    fallbackMaterials[index];

                if (material == null ||
                    IsExcludedMaterial(
                        material.name))
                {
                    continue;
                }

                fallback.GetPropertyBlock(
                    block,
                    index);

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

                fallback.SetPropertyBlock(
                    block,
                    index);

                block.Clear();
            }
        }

        private static bool LooksPaintable(
            Transform rendererTransform,
            Material material)
        {
            string objectName =
                rendererTransform != null
                    ? rendererTransform.name.ToLowerInvariant()
                    : string.Empty;

            string materialName =
                material != null
                    ? material.name.ToLowerInvariant()
                    : string.Empty;

            if (ShouldSkipName(
                    objectName) ||
                IsExcludedMaterial(
                    materialName))
            {
                return false;
            }

            return
                objectName.Contains("body") ||
                objectName.Contains("hood") ||
                objectName.Contains("bonnet") ||
                objectName.Contains("bumper") ||
                objectName.Contains("door") ||
                objectName.Contains("fender") ||
                objectName.Contains("spoiler") ||
                materialName.Contains("body") ||
                materialName.Contains("paint") ||
                materialName.Contains("carpaint") ||
                materialName.Contains("vehicle");
        }

        private static bool ShouldSkipRenderer(
            Transform item)
        {
            if (item == null)
                return true;

            return
                ShouldSkipName(
                    item.name.ToLowerInvariant());
        }

        private static bool ShouldSkipName(
            string name)
        {
            return
                name.Contains("wheel") ||
                name.Contains("tire") ||
                name.Contains("tyre") ||
                name.Contains("rim") ||
                name.Contains("glass") ||
                name.Contains("window") ||
                name.Contains("light") ||
                name.Contains("lamp") ||
                name.Contains("interior") ||
                name.Contains("seat");
        }

        private static bool IsExcludedMaterial(
            string name)
        {
            if (string.IsNullOrWhiteSpace(
                    name))
            {
                return false;
            }

            string normalized =
                name.ToLowerInvariant();

            return
                normalized.Contains("glass") ||
                normalized.Contains("window") ||
                normalized.Contains("wheel") ||
                normalized.Contains("tire") ||
                normalized.Contains("tyre") ||
                normalized.Contains("rim") ||
                normalized.Contains("chrome") ||
                normalized.Contains("light") ||
                normalized.Contains("lamp") ||
                normalized.Contains("interior");
        }

        private static Renderer FindLargestBodyRenderer(
            Renderer[] renderers)
        {
            Renderer best =
                null;

            float bestVolume =
                0f;

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    ShouldSkipRenderer(
                        renderer.transform))
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

                best =
                    renderer;

                bestVolume =
                    volume;
            }

            return best;
        }

        private static string ColorKey(
            string vehicleId)
        {
            return
                "MotorCity.Customization.Color." +
                (string.IsNullOrWhiteSpace(
                    vehicleId)
                    ? "street"
                    : vehicleId);
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
    }
}
