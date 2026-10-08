using System;
using System.Collections.Generic;
using MotorCity.Input;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Lights only DeLorean's explicitly assigned MC_* material polygons.
    /// No extra meshes, spheres, color-keying or duplicated overlays.
    /// </summary>
    public sealed class DeloreanAuthoredLights : MonoBehaviour
    {
        private const string RuntimeVisualName = "MotorCityVehicleVisual_Runtime";

        private enum LampKind
        {
            Cyan,
            Brake,
            Headlight
        }

        private sealed class Binding
        {
            public Renderer Renderer;
            public int Slot;
            public Material Original;
            public Material Runtime;
            public LampKind Kind;
        }

        private static readonly Color CyanEmission =
            new Color(0.025f, 0.45f, 1f) * 0.90f;
        private static readonly Color BrakeEmission =
            new Color(1f, 0.018f, 0.006f) * 3.2f;
        private static readonly Color HeadlightEmission =
            new Color(0.95f, 0.97f, 1f) * 3.0f;

        private readonly List<Binding> bindings = new();
        private Transform currentVisual;
        private ArcadeCarController car;
        private DayNightCycleController dayNight;
        private string vehicleId = VehicleIds.Street;
        private float dayNightResolveTimer;
        private float previousHeadlight = -1f;
        private bool previousBrake;
        private bool stateApplied;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
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
            if (vehicleId != VehicleIds.Delorean)
                return;

            Transform visual = transform.Find(RuntimeVisualName);
            if (visual != currentVisual)
                Rebuild();

            if (currentVisual == null)
                return;

            if (dayNight == null)
            {
                dayNightResolveTimer -= Time.unscaledDeltaTime;
                if (dayNightResolveTimer <= 0f)
                {
                    dayNightResolveTimer = 1f;
                    dayNight = FindAnyObjectByType<DayNightCycleController>();
                }
            }

            float night = dayNight == null ? 0f : dayNight.NightAmount;
            float headlightAmount = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(0.34f, 0.72f, night));
            bool braking = MotorCityInput.ReverseHeld ||
                (car != null && car.HandbrakeInputHeld);

            if (stateApplied && braking == previousBrake &&
                Mathf.Abs(headlightAmount - previousHeadlight) < 0.01f)
                return;

            previousBrake = braking;
            previousHeadlight = headlightAmount;
            stateApplied = true;

            foreach (Binding binding in bindings)
            {
                if (binding.Runtime == null)
                    continue;

                Color emission = binding.Kind switch
                {
                    LampKind.Cyan => CyanEmission,
                    LampKind.Brake => braking ? BrakeEmission : Color.black,
                    _ => HeadlightEmission * headlightAmount
                };

                binding.Runtime.SetColor("_EmissionColor", emission);
            }
        }

        private void Rebuild()
        {
            RestoreBindings();

            currentVisual = vehicleId == VehicleIds.Delorean
                ? transform.Find(RuntimeVisualName)
                : null;

            if (currentVisual == null)
                return;

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
                    if (original == null ||
                        !TryGetLampKind(original.name, out LampKind kind))
                        continue;

                    // An imported material may also be used by a cached car
                    // or by the garage. Never change its shared emission state.
                    Material instance = new Material(original)
                    {
                        name = original.name + " (DeLorean Lamp Runtime)"
                    };

                    if (!instance.HasProperty("_EmissionColor"))
                    {
                        Debug.LogWarning(
                            "Motor City: DeLorean lamp material has no emission property: " +
                            original.name);
                        Destroy(instance);
                        continue;
                    }

                    // OBJ exports can provide a shared atlas as map_Ke.
                    // The lens still uses its original base texture, but its
                    // illumination should be uniform over the selected faces.
                    if (instance.HasProperty("_EmissionMap"))
                        instance.SetTexture("_EmissionMap", Texture2D.whiteTexture);

                    instance.EnableKeyword("_EMISSION");
                    instance.SetColor("_EmissionColor",
                        kind == LampKind.Cyan ? CyanEmission : Color.black);

                    slots[slot] = instance;
                    changed = true;
                    bindings.Add(new Binding
                    {
                        Renderer = renderer,
                        Slot = slot,
                        Original = original,
                        Runtime = instance,
                        Kind = kind
                    });
                }

                if (changed)
                    renderer.sharedMaterials = slots;
            }

            stateApplied = false;
            previousHeadlight = -1f;

            if (bindings.Count == 0)
            {
                Debug.LogWarning(
                    "Motor City: no MC_* DeLorean lamp material slots found. " +
                    "Rebuild the DeLorean prefab from its updated OBJ.");
            }
        }

        private static bool TryGetLampKind(string materialName, out LampKind kind)
        {
            // Runtime model conversion appends _URP to imported material
            // names, and Unity can append (Instance)/(Clone).
            string name = materialName.Trim();
            name = name.Replace(" (Instance)", "")
                .Replace(" (Clone)", "")
                .Trim();

            if (name.EndsWith("_URP", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - 4);

            if (name.Equals("MC_Cyan", StringComparison.OrdinalIgnoreCase))
            {
                kind = LampKind.Cyan;
                return true;
            }

            if (name.Equals("MC_Brake_L", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("MC_Brake_R", StringComparison.OrdinalIgnoreCase))
            {
                kind = LampKind.Brake;
                return true;
            }

            if (name.Equals("MC_Headlight_L", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("MC_Headlight_R", StringComparison.OrdinalIgnoreCase))
            {
                kind = LampKind.Headlight;
                return true;
            }

            kind = default;
            return false;
        }

        private void RestoreBindings()
        {
            foreach (Binding binding in bindings)
            {
                if (binding.Renderer != null)
                {
                    Material[] slots = binding.Renderer.sharedMaterials;
                    if (binding.Slot < slots.Length &&
                        slots[binding.Slot] == binding.Runtime)
                    {
                        slots[binding.Slot] = binding.Original;
                        binding.Renderer.sharedMaterials = slots;
                    }
                }

                if (binding.Runtime != null)
                    Destroy(binding.Runtime);
            }

            bindings.Clear();
            stateApplied = false;
            previousHeadlight = -1f;
        }

        private void OnDestroy()
        {
            RestoreBindings();
        }
    }
}
