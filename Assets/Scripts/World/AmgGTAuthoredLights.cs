using System;
using System.Collections.Generic;
using MotorCity.Input;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Controls the two dedicated AMG GT OBJ materials on their actual
    /// polygons. Does not add light meshes or modify shared source materials.
    /// Road-illuminating Spot Lights are owned by PlayerHeadlights.
    /// </summary>
    public sealed class AmgGTAuthoredLights : MonoBehaviour
    {
        private const string RuntimeVisualName = "MotorCityVehicleVisual_Runtime";

        private enum LampKind
        {
            Front,
            Brake
        }

        private sealed class Binding
        {
            public Renderer Renderer;
            public int Slot;
            public Material Original;
            public Material Runtime;
            public LampKind Kind;
        }

        private static readonly Color FrontEmission =
            new Color(0.95f, 0.97f, 1f) * 3.0f;
        private static readonly Color BrakeEmission =
            new Color(1f, 0.015f, 0.005f) * 3.2f;

        private readonly List<Binding> bindings = new();
        private ArcadeCarController car;
        private DayNightCycleController dayNight;
        private Transform currentVisual;
        private string vehicleId = VehicleIds.Street;
        private float nightLookupTimer;
        private float lastHeadlight = -1f;
        private bool lastBrake;
        private bool initialized;

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
            if (vehicleId != VehicleIds.AmgGT)
                return;

            Transform activeVisual = FindActiveVisual();
            if (activeVisual != currentVisual)
                Rebuild();

            if (currentVisual == null)
                return;

            if (dayNight == null)
            {
                nightLookupTimer -= Time.unscaledDeltaTime;
                if (nightLookupTimer <= 0f)
                {
                    nightLookupTimer = 1f;
                    dayNight = FindAnyObjectByType<DayNightCycleController>();
                }
            }

            float night = dayNight == null ? 0f : dayNight.NightAmount;
            float front = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(0.34f, 0.72f, night));
            bool brake = MotorCityInput.ReverseHeld ||
                (car != null && car.HandbrakeInputHeld);

            if (initialized && Mathf.Abs(lastHeadlight - front) < 0.01f &&
                lastBrake == brake)
                return;

            initialized = true;
            lastHeadlight = front;
            lastBrake = brake;

            foreach (Binding binding in bindings)
            {
                if (binding.Runtime == null)
                    continue;

                Color emission = binding.Kind == LampKind.Front
                    ? FrontEmission * front
                    : brake ? BrakeEmission : Color.black;
                binding.Runtime.SetColor("_EmissionColor", emission);
            }
        }

        private Transform FindActiveVisual()
        {
            // The initial ARCADE prototype is destroyed at the end of the
            // frame, and cached inactive visuals can remain under this car.
            // Bind only the active replacement.
            for (int i = transform.childCount - 1; i >= 0; i--)
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

            currentVisual = vehicleId == VehicleIds.AmgGT
                ? FindActiveVisual()
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
                    Material source = slots[slot];
                    if (source == null)
                        continue;

                    LampKind kind;
                    if (!TryResolveKind(source.name, out kind) &&
                        !MatchesAuthoredSubmesh(renderer, slot, out kind))
                        continue;

                    Material runtime = new Material(source)
                    {
                        name = source.name + " (AMG GT Lamp Runtime)"
                    };

                    if (!runtime.HasProperty("_EmissionColor"))
                    {
                        Debug.LogWarning("Motor City: AMG GT material has no emission support: " +
                            source.name);
                        Destroy(runtime);
                        continue;
                    }

                    // The OBJ material library can use the complete texture
                    // atlas as its emission map. Use a uniform mask only on
                    // these specific submeshes, never the painted body.
                    if (runtime.HasProperty("_EmissionMap"))
                        runtime.SetTexture("_EmissionMap", Texture2D.whiteTexture);
                    runtime.EnableKeyword("_EMISSION");
                    runtime.SetColor("_EmissionColor", Color.black);

                    bindings.Add(new Binding
                    {
                        Renderer = renderer,
                        Slot = slot,
                        Original = source,
                        Runtime = runtime,
                        Kind = kind
                    });
                    slots[slot] = runtime;
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = slots;
            }

            initialized = false;
            lastHeadlight = -1f;

            if (bindings.Count == 0)
            {
                List<string> slotsFound = new();
                foreach (Renderer renderer in
                         currentVisual.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null ||
                        VehicleLampMaterialUtility.IsWheelRenderer(renderer.transform))
                        continue;

                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null && slotsFound.Count < 14)
                            slotsFound.Add(renderer.name + ": " + material.name);
                    }
                }

                Debug.LogWarning("Motor City: AMG GT MC_* material slots not found on the " +
                    "active model. Imported: [" + string.Join("; ", slotsFound) +
                    "]. Rebuild AMG GT via Motor City > Vehicles > Rebuild AmgGT.");
            }
        }

        private static bool TryResolveKind(string rawName, out LampKind kind)
        {
            string name = rawName.Trim();
            // The runtime URP converter appends _URP, while Unity may
            // append (Instance) or (Clone), sometimes in varying order.
            for (int i = 0; i < 3; i++)
            {
                if (name.EndsWith("_URP", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 4).Trim();
                if (name.EndsWith(" (Instance)", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 11).Trim();
                if (name.EndsWith(" (Clone)", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 8).Trim();
            }

            if (name.Equals("MC_Headlight", StringComparison.OrdinalIgnoreCase))
            {
                kind = LampKind.Front;
                return true;
            }

            if (name.Equals("MC_Brake", StringComparison.OrdinalIgnoreCase))
            {
                kind = LampKind.Brake;
                return true;
            }

            kind = default;
            return false;
        }

        private static bool MatchesAuthoredSubmesh(
            Renderer renderer, int slot, out LampKind kind)
        {
            kind = default;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return false;

            Mesh mesh = filter.sharedMesh;
            if (slot >= mesh.subMeshCount)
                return false;

            string objectName = renderer.name.ToLowerInvariant();
            string meshName = mesh.name.ToLowerInvariant();
            if (!objectName.Contains("body_misc") &&
                !meshName.Contains("body_misc"))
                return false;

            Bounds bounds = mesh.GetSubMesh(slot).bounds;
            if (SameBounds(bounds,
                    new Vector3(-0.793886f, 0.401690f, 1.476099f),
                    new Vector3(0.793885f, 0.601209f, 1.842311f)))
            {
                kind = LampKind.Front;
                return true;
            }

            if (SameBounds(bounds,
                    new Vector3(-0.753717f, 0.562708f, -1.949013f),
                    new Vector3(0.753717f, 0.668690f, -1.585606f)))
            {
                kind = LampKind.Brake;
                return true;
            }

            return false;
        }

        private static bool SameBounds(Bounds actual, Vector3 min, Vector3 max)
        {
            const float tolerance = 0.015f;
            return (actual.min - min).sqrMagnitude < tolerance * tolerance &&
                (actual.max - max).sqrMagnitude < tolerance * tolerance;
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
            initialized = false;
            lastHeadlight = -1f;
        }

        private void OnDestroy()
        {
            RestoreBindings();
        }
    }
}
