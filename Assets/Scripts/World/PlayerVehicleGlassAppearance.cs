using System;
using System.Collections.Generic;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    /// <summary>
    /// Adds opaque, camera-angle-dependent reflective glass to the actual
    /// window polygons of imported player vehicles. The Street/AFRC car
    /// keeps its authored baked-glass appearance completely unchanged.
    ///
    /// No mesh overlays or geometry edits: each detected blackGlass slot
    /// receives its own runtime material. Cached visuals and source
    /// materials are restored when the selected vehicle changes.
    /// </summary>
    public sealed class PlayerVehicleGlassAppearance : MonoBehaviour
    {
        private const string RuntimeVisualName = "MotorCityVehicleVisual_Runtime";

        private sealed class Binding
        {
            public Renderer Renderer;
            public int Slot;
            public Material Original;
            public Material Glass;
        }

        private readonly List<Binding> bindings = new();
        private Transform currentVisual;
        private string vehicleId = VehicleIds.Street;

        private static bool SupportsGlass(string id)
        {
            return id == VehicleIds.Beatall ||
                id == VehicleIds.Peugeot306 ||
                id == VehicleIds.ToyotaAE86 ||
                id == VehicleIds.Hybrid ||
                id == VehicleIds.Porsche996 ||
                id == VehicleIds.AmgGT ||
                id == VehicleIds.Camaro ||
                id == VehicleIds.Delorean ||
                id == VehicleIds.Bus;
        }

        public void SetVehicleId(string id)
        {
            vehicleId = string.IsNullOrWhiteSpace(id)
                ? VehicleIds.Street
                : id.ToLowerInvariant();

            Rebuild();
        }

        private void Update()
        {
            if (!SupportsGlass(vehicleId))
                return;

            Transform active = FindActiveVisual();
            if (active != currentVisual)
                Rebuild();
        }

        private Transform FindActiveVisual()
        {
            // Destroy is deferred to the end of the frame. A retired
            // starter car or an inactive cached vehicle must never supply
            // glass renderers for the currently selected player vehicle.
            for (int i = transform.childCount - 1; i >= 0; --i)
            {
                Transform child = transform.GetChild(i);
                if (child.name == RuntimeVisualName &&
                    child.gameObject.activeInHierarchy)
                    return child;
            }

            return null;
        }

        private void Rebuild()
        {
            RestoreBindings();

            currentVisual = SupportsGlass(vehicleId)
                ? FindActiveVisual()
                : null;

            if (currentVisual == null)
                return;

            // Keep a material asset in Resources so the custom shader is
            // included in desktop and WebGL builds, not just in the Editor.
            Material template = Resources.Load<Material>("MotorCity/VehicleGlass");
            Shader shader = template != null
                ? template.shader
                : Shader.Find("MotorCity/VehicleGlass");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning(
                    "Motor City: MotorCity/VehicleGlass shader unavailable. " +
                    "Check Assets/Resources/MotorCity/VehicleGlass.mat.");
                return;
            }

            int found = 0;
            foreach (Renderer renderer in
                     currentVisual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null ||
                    VehicleLampMaterialUtility.IsWheelRenderer(renderer.transform))
                    continue;

                Material[] slots = renderer.sharedMaterials;
                bool changed = false;

                for (int slot = 0; slot < slots.Length; slot++)
                {
                    Material original = slots[slot];
                    if (original == null || !IsWindowMaterial(original.name))
                        continue;

                    Material glass = template != null
                        ? new Material(template)
                        : new Material(shader);

                    glass.name = "MotorCity_" + vehicleId + "_Glass_Runtime";
                    glass.hideFlags = HideFlags.DontSave;
                    glass.enableInstancing = true;

                    ConfigureGlass(glass);
                    slots[slot] = glass;
                    bindings.Add(new Binding
                    {
                        Renderer = renderer,
                        Slot = slot,
                        Original = original,
                        Glass = glass
                    });
                    changed = true;
                    found++;
                }

                if (changed)
                {
                    renderer.sharedMaterials = slots;
                    // Only the authored window material slot changes;
                    // other body_misc submeshes keep their original material,
                    // shadows, body colors, and light emission.
                }
            }

            if (found == 0)
            {
                Debug.LogWarning("Motor City: no blackGlass submesh material found for " +
                    vehicleId + " on the active vehicle visual.");
            }
        }

        private bool IsWindowMaterial(string originalName)
        {
            string name = (originalName ?? string.Empty).Trim();

            for (int i = 0; i < 4; i++)
            {
                if (name.EndsWith("_URP", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 4).Trim();

                if (name.EndsWith(" (Instance)", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 11).Trim();

                if (name.EndsWith(" (Clone)", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 8).Trim();
            }

            // Source OBJ contracts are blackGlass, blackGlass.001, etc.
            // Some older prefabs may refer to their own saved *Glass.mat.
            // Never match generic "glass" substrings in lamp or mirror names.
            if (name.Equals("blackGlass", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("blackGlass.", StringComparison.OrdinalIgnoreCase))
                return true;

            return name.Equals(vehicleId + "Glass", StringComparison.OrdinalIgnoreCase) ||
                (vehicleId == VehicleIds.Porsche996 &&
                 name.Equals("Porsche996Glass", StringComparison.OrdinalIgnoreCase)) ||
                (vehicleId == VehicleIds.Peugeot306 &&
                 name.Equals("Peugeot306Glass", StringComparison.OrdinalIgnoreCase)) ||
                (vehicleId == VehicleIds.ToyotaAE86 &&
                 name.Equals("ToyotaAE86Glass", StringComparison.OrdinalIgnoreCase)) ||
                (vehicleId == VehicleIds.AmgGT &&
                 name.Equals("AmgGTGlass", StringComparison.OrdinalIgnoreCase));
        }

        private static void ConfigureGlass(Material glass)
        {
            // Do not use alpha blending here. The previous 0.72 alpha made
            // the interior and whole street visible through the windows,
            // especially on the Bus. This deliberately writes opaque depth.
            //
            // The custom shader computes a Schlick-like Fresnel falloff in
            // world space, from near-black face-on to neutral grey at grazing
            // angles, plus a desaturated glossy probe/sky reflection.
            // These highlights move as the player's camera rotates.
            glass.SetColor("_BaseColor",
                new Color(0.024f, 0.028f, 0.035f, 1f));
            glass.SetColor("_EdgeColor",
                new Color(0.22f, 0.235f, 0.25f, 1f));
            glass.SetFloat("_FresnelPower", 2.8f);
            glass.SetFloat("_ReflectionStrength", 0.42f);
            glass.SetFloat("_Smoothness", 0.91f);
            glass.SetFloat("_SpecularStrength", 0.18f);

            // The shader's SubShader is Opaque, Cull Back, ZWrite On.
            // Keep the material's queue/tag consistent with the actual
            // shader so no old URP-transparent states can survive.
            glass.SetOverrideTag("RenderType", "Opaque");
            glass.renderQueue = (int)RenderQueue.Geometry;
        }

        private void RestoreBindings()
        {
            foreach (Binding binding in bindings)
            {
                if (binding.Renderer != null)
                {
                    Material[] slots = binding.Renderer.sharedMaterials;
                    if (binding.Slot < slots.Length &&
                        slots[binding.Slot] == binding.Glass)
                    {
                        slots[binding.Slot] = binding.Original;
                        binding.Renderer.sharedMaterials = slots;
                    }
                }

                if (binding.Glass != null)
                    Destroy(binding.Glass);
            }

            bindings.Clear();
        }

        private void OnDestroy()
        {
            RestoreBindings();
        }
    }
}
