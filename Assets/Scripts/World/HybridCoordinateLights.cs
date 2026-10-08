using System.Collections.Generic;
using MotorCity.Input;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Drives Hybrid's authored lamp polygons, not positioned primitives.
    /// hybrid.obj splits the rear white faces out of Material.005 into
    /// Material.005_RearUnlit. No mesh copies, color searches or lamp overlays.
    /// </summary>
    public sealed class HybridCoordinateLights : MonoBehaviour
    {
        private const string RuntimeVisualName = "MotorCityVehicleVisual_Runtime";
        private const string AccentName = "Material.003";
        private const string BrakeName = "Material.004";
        private const string FrontName = "Material.005";

        private enum LampKind
        {
            Accent,
            Brake,
            Front
        }

        private sealed class Binding
        {
            public Renderer Renderer;
            public int Slot;
            public Material Original;
            public Material Instance;
            public LampKind Kind;
        }

        private readonly List<Binding> bindings = new();
        private Transform currentVisual;
        private ArcadeCarController car;
        private DayNightCycleController dayNight;
        private string vehicleId = VehicleIds.Street;
        private bool applied;
        private bool previousBrake;
        private float previousFront = -1f;
        private float dayNightLookupTimer;

        private static readonly Color AccentEmission =
            new Color(0.015f, 0.28f, 0.95f) * 1.15f;
        private static readonly Color BrakeEmission =
            new Color(1f, 0.015f, 0.005f) * 3.2f;
        private static readonly Color HeadlightEmission =
            new Color(0.94f, 0.97f, 1f) * 3.0f;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
        }

        public void SetVehicleId(string id)
        {
            vehicleId = string.IsNullOrEmpty(id)
                ? VehicleIds.Street
                : id.ToLowerInvariant();
            Rebuild();
        }

        private void Update()
        {
            if (vehicleId != VehicleIds.Hybrid)
                return;

            Transform visual = transform.Find(RuntimeVisualName);
            if (visual != currentVisual)
                Rebuild();

            if (currentVisual == null)
                return;

            if (dayNight == null)
            {
                dayNightLookupTimer -= Time.unscaledDeltaTime;
                if (dayNightLookupTimer <= 0f)
                {
                    dayNightLookupTimer = 1f;
                    dayNight = FindAnyObjectByType<DayNightCycleController>();
                }
            }

            float nightAmount = dayNight == null ? 0f : dayNight.NightAmount;
            float front = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(0.34f, 0.72f, nightAmount));
            bool braking = MotorCityInput.ReverseHeld ||
                (car != null && car.HandbrakeInputHeld);

            if (applied && braking == previousBrake &&
                Mathf.Abs(previousFront - front) < 0.01f)
                return;

            previousBrake = braking;
            previousFront = front;
            applied = true;

            foreach (Binding binding in bindings)
            {
                if (binding.Instance == null)
                    continue;

                Color emission = binding.Kind switch
                {
                    LampKind.Accent => AccentEmission,
                    LampKind.Brake => braking ? BrakeEmission : Color.black,
                    _ => HeadlightEmission * front
                };

                binding.Instance.SetColor("_EmissionColor", emission);
            }
        }

        private void Rebuild()
        {
            RestoreBindings();

            currentVisual = vehicleId == VehicleIds.Hybrid
                ? transform.Find(RuntimeVisualName)
                : null;

            if (currentVisual == null)
                return;

            // Remove any leftover roots from the previous point-sphere
            // prototype when a cached visual is reused.
            Transform legacyLights =
                currentVisual.Find("MotorCityHybridCoordinateLights");
            if (legacyLights != null)
            {
                legacyLights.gameObject.SetActive(false);
                Destroy(legacyLights.gameObject);
            }

            Renderer[] renderers =
                currentVisual.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    VehicleLampMaterialUtility.IsWheelRenderer(renderer.transform))
                    continue;

                Material[] slots = renderer.sharedMaterials;
                bool changed = false;

                for (int slot = 0; slot < slots.Length; slot++)
                {
                    Material original = slots[slot];
                    if (original == null)
                        continue;

                    LampKind kind;
                    string name = NormalizeMaterialName(original.name);

                    if (name == AccentName.ToLowerInvariant())
                        kind = LampKind.Accent;
                    else if (name == BrakeName.ToLowerInvariant())
                        kind = LampKind.Brake;
                    else if (name == FrontName.ToLowerInvariant())
                        kind = LampKind.Front;
                    else
                        continue; // Includes Material.005_RearUnlit and all body/glass.

                    // Never change imported or shared materials: other vehicles,
                    // cached prefabs and garage colors may refer to them.
                    Material instance = new Material(original)
                    {
                        name = original.name + " (Hybrid Lamp Runtime)"
                    };

                    if (!instance.HasProperty("_EmissionColor"))
                    {
                        Debug.LogWarning("Motor City: Hybrid lamp material " +
                            original.name + " has no _EmissionColor property.");
                        Destroy(instance);
                        continue;
                    }

                    instance.EnableKeyword("_EMISSION");
                    instance.SetColor("_EmissionColor",
                        kind == LampKind.Accent ? AccentEmission : Color.black);

                    bindings.Add(new Binding
                    {
                        Renderer = renderer,
                        Slot = slot,
                        Original = original,
                        Instance = instance,
                        Kind = kind
                    });

                    slots[slot] = instance;
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = slots;
            }

            // These are the actual authored faces; never add spheres, strips or
            // cloned whole-mesh renderers. Brake and front start disabled.
            applied = false;
            previousFront = -1f;
        }

        private static string NormalizeMaterialName(string name)
        {
            return name.Replace(" (Instance)", "")
                .Replace(" (Clone)", "")
                .Trim()
                .ToLowerInvariant();
        }

        private void RestoreBindings()
        {
            foreach (Binding binding in bindings)
            {
                if (binding.Renderer != null)
                {
                    Material[] slots = binding.Renderer.sharedMaterials;
                    if (binding.Slot < slots.Length &&
                        slots[binding.Slot] == binding.Instance)
                    {
                        slots[binding.Slot] = binding.Original;
                        binding.Renderer.sharedMaterials = slots;
                    }
                }

                if (binding.Instance != null)
                    Destroy(binding.Instance);
            }

            bindings.Clear();
            applied = false;
            previousFront = -1f;
        }

        private void OnDestroy()
        {
            RestoreBindings();
        }
    }
}
