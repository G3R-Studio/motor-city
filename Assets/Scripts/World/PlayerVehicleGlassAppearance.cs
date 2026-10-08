using System;
using System.Collections.Generic;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    /// <summary>
    /// Adds neutral, glossy URP glass to the actual window submeshes of
    /// imported player vehicles. The Street/AFRC car keeps its authored
    /// baked-glass appearance completely unchanged.
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

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("Motor City: URP Lit shader unavailable for vehicle glass.");
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

                    Material glass = new Material(shader)
                    {
                        name = "MotorCity_" + vehicleId + "_Glass_Runtime",
                        hideFlags = HideFlags.DontSave,
                        enableInstancing = true
                    };

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
                    // Transparent car windows should not cast black opaque
                    // shadows over the passenger cabin or surrounding paint.
                    // Only disable shadows for dedicated all-glass renderers.
                    // On mixed body_misc renderers, leave renderer-wide
                    // settings unchanged.
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
            // A neutral charcoal/steel tint follows the Street car's
            // understated, reflective windows. Do not preserve the imported
            // pure-black Kd or palette texture: both made the old glass look
            // like painted plastic or bright, flat blue.
            Color tint = new Color(0.24f, 0.255f, 0.27f, 0.72f);
            glass.SetColor("_BaseColor", tint);
            if (glass.HasProperty("_BaseMap"))
                glass.SetTexture("_BaseMap", Texture2D.whiteTexture);

            glass.SetFloat("_Metallic", 0.13f);
            glass.SetFloat("_Smoothness", 0.94f);
            glass.SetFloat("_EnvironmentReflections", 1f);
            glass.SetFloat("_SpecularHighlights", 1f);

            // URP Lit transparent, premultiplied: preserves clear Fresnel/
            // sky-probe highlights over the slightly see-through dark tint.
            // Must set both properties AND shader keywords; changing the
            // alpha property alone still renders an opaque URP material.
            glass.SetFloat("_Surface", 1f);
            glass.SetFloat("_Blend", 1f);
            glass.SetFloat("_BlendModePreserveSpecular", 1f);
            glass.SetFloat("_SrcBlend", (float)BlendMode.One);
            glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            glass.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite", 0f);
            glass.SetFloat("_Cull", (float)CullMode.Back);
            glass.SetFloat("_AlphaClip", 0f);
            glass.SetFloat("_ReceiveShadows", 0f);
            glass.SetColor("_EmissionColor", Color.black);

            glass.DisableKeyword("_EMISSION");
            glass.DisableKeyword("_ALPHATEST_ON");
            glass.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            glass.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            glass.DisableKeyword("_RECEIVE_SHADOWS_OFF");
            glass.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            glass.SetOverrideTag("RenderType", "Transparent");
            glass.renderQueue = (int)RenderQueue.Transparent;
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
