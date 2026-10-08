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
                    if (original == null)
                        continue;

                    // Prefer the five explicit material names. Some imported
                    // prefabs replace those names with a generated/converted
                    // material; the authored submesh bounds then identify the
                    // exact same polygons without sampling texture colors.
                    if (!TryGetLampKind(original.name, out LampKind kind) &&
                        !TryGetKindFromAuthoredSubmesh(renderer, slot, out kind))
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
                // Include actual material names to distinguish stale prefab
                // imports from Unity renaming/merging the material slots.
                List<string> observed = new();
                foreach (Renderer renderer in
                         currentVisual.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null ||
                        VehicleLampMaterialUtility.IsWheelRenderer(renderer.transform))
                        continue;

                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null && observed.Count < 16)
                            observed.Add(renderer.name + ": " + material.name);
                    }
                }

                Debug.LogWarning(
                    "Motor City: DeLorean MC lamp polygons could not be bound. " +
                    "Imported slots: [" + string.Join("; ", observed) + "]. " +
                    "Use Motor City > Vehicles > Rebuild Delorean, then check " +
                    "the delorean.obj Model Importer material settings.");
            }
        }

        // Exact local-space bounds measured from the committed
        // Delorean/delorean.obj MC_* polygon groups. Used only when Unity
        // drops/replaces a material's authored name during prefab import.
        // Never classify using pixel color or a broad front/rear cutoff.
        private static bool TryGetKindFromAuthoredSubmesh(
            Renderer renderer, int slot, out LampKind kind)
        {
            kind = default;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return false;

            Mesh mesh = filter.sharedMesh;
            if (slot >= mesh.subMeshCount)
                return false;

            // This geometry exists only on DeLorean's body_misc mesh,
            // not the wheels or body paint.
            string rendererName = renderer.name.ToLowerInvariant();
            string meshName = mesh.name.ToLowerInvariant();
            if (!rendererName.Contains("body_misc") &&
                !meshName.Contains("body_misc"))
                return false;

            // SubMeshDescriptor.bounds is available for imported meshes
            // without accessing the non-readable vertex buffer.
            Bounds bounds = mesh.GetSubMesh(slot).bounds;
            if (MatchesBounds(bounds,
                    new Vector3(-0.754994f, 0.678464f, 2.291579f),
                    new Vector3(-0.381872f, 0.786028f, 2.333040f)) ||
                MatchesBounds(bounds,
                    new Vector3(0.381852f, 0.678464f, 2.291592f),
                    new Vector3(0.754973f, 0.786028f, 2.333049f)))
            {
                kind = LampKind.Headlight;
                return true;
            }

            if (MatchesBounds(bounds,
                    new Vector3(0.359479f, 0.811863f, -2.166042f),
                    new Vector3(0.572289f, 0.905231f, -2.110843f)) ||
                MatchesBounds(bounds,
                    new Vector3(-0.572206f, 0.811863f, -2.166055f),
                    new Vector3(-0.359398f, 0.905231f, -2.110856f)))
            {
                kind = LampKind.Brake;
                return true;
            }

            if (MatchesBounds(bounds,
                    new Vector3(-0.774788f, 0.333617f, -1.531649f),
                    new Vector3(0.774849f, 1.142584f, 1.974952f)))
            {
                kind = LampKind.Cyan;
                return true;
            }

            return false;
        }

        private static bool MatchesBounds(Bounds actual, Vector3 min, Vector3 max)
        {
            const float tolerance = 0.015f;
            return (actual.min - min).sqrMagnitude <= tolerance * tolerance &&
                (actual.max - max).sqrMagnitude <= tolerance * tolerance;
        }

        private static bool TryGetLampKind(string materialName, out LampKind kind)
        {
            // Runtime model conversion appends _URP to imported material
            // names, and Unity can append (Instance)/(Clone).
            string name = materialName.Trim();
            // The importer can order the instance/clone and URP suffixes
            // differently (e.g. MC_Cyan (Instance)_URP).
            for (int i = 0; i < 3; i++)
            {
                if (name.EndsWith("_URP", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 4).Trim();

                if (name.EndsWith(" (Instance)", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 11).Trim();

                if (name.EndsWith(" (Clone)", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 8).Trim();
            }

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
