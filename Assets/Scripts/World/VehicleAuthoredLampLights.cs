using System;
using System.Collections.Generic;
using MotorCity.Input;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Drives dedicated OBJ lamp material polygons on Peugeot 306, Porsche
    /// 996, Toyota AE86, Camaro, Bus and Beatall. This class never creates
    /// proxy/overlay meshes and never changes shared imported materials.
    /// </summary>
    public sealed class VehicleAuthoredLampLights : MonoBehaviour
    {
        private const string RuntimeVisualName = "MotorCityVehicleVisual_Runtime";

        private enum LampKind { Front, Brake }

        private sealed class Binding
        {
            public Renderer Renderer;
            public int Slot;
            public Material Original;
            public Material Runtime;
            public LampKind Kind;
        }

        private readonly List<Binding> bindings = new();
        private ArcadeCarController car;
        private DayNightCycleController dayNight;
        private Transform currentVisual;
        private string vehicleId = VehicleIds.Street;
        private float dayNightLookupTimer;
        private float lastNight = -1f;
        private bool lastBrake;
        private bool applied;

        private static readonly Color HeadlightEmission =
            new Color(0.95f, 0.97f, 1f) * 3.0f;

        private static readonly Color BrakeEmission =
            new Color(1f, 0.016f, 0.006f) * 3.2f;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
        }

        public static bool HandlesVehicle(string id)
        {
            return id == VehicleIds.Peugeot306 ||
                id == VehicleIds.Porsche996 ||
                id == VehicleIds.ToyotaAE86 ||
                id == VehicleIds.Camaro ||
                id == VehicleIds.Bus ||
                id == VehicleIds.Beatall;
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
            if (!HandlesVehicle(vehicleId))
                return;

            Transform active = FindActiveVisual();
            if (active != currentVisual)
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
            float headlight = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(0.34f, 0.72f, nightAmount));

            // AE86's pop-up headlamps are hidden in the source mesh. The
            // dedicated lens polygons do not exist yet; keep beams off until
            // a proper deployment system and real front lenses are authored.
            if (vehicleId == VehicleIds.ToyotaAE86)
                headlight = 0f;

            bool braking = MotorCityInput.ReverseHeld ||
                (car != null && car.HandbrakeInputHeld);

            if (applied && braking == lastBrake &&
                Mathf.Abs(headlight - lastNight) < 0.01f)
                return;

            applied = true;
            lastBrake = braking;
            lastNight = headlight;

            foreach (Binding binding in bindings)
            {
                if (binding.Runtime == null)
                    continue;

                Color emission = binding.Kind == LampKind.Front
                    ? HeadlightEmission * headlight
                    : braking ? BrakeEmission : Color.black;

                binding.Runtime.SetColor("_EmissionColor", emission);
            }
        }

        private Transform FindActiveVisual()
        {
            // The prior AFRC placeholder can survive until end-of-frame
            // after Destroy(). Inactive cached visuals also remain in the
            // hierarchy: never bind their material slots.
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

            currentVisual = HandlesVehicle(vehicleId)
                ? FindActiveVisual()
                : null;

            if (currentVisual == null)
                return;

            int frontFound = 0;
            int brakeFound = 0;

            foreach (Renderer renderer in
                     currentVisual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null ||
                    VehicleLampMaterialUtility.IsWheelRenderer(renderer.transform))
                    continue;

                VehicleVisualRoles explicitRoles = renderer.GetComponent<VehicleVisualRoles>();
                Material[] slots = renderer.sharedMaterials;
                bool changed = false;

                for (int slot = 0; slot < slots.Length; ++slot)
                {
                    Material source = slots[slot];
                    if (source == null)
                        continue;
                    LampKind kind;
                    if (explicitRoles != null &&
                        explicitRoles.RolesAt(slot) != VehicleMaterialRole.None)
                    {
                        VehicleMaterialRole role = explicitRoles.RolesAt(slot);
                        if ((role & VehicleMaterialRole.FrontLamp) != 0)
                            kind = LampKind.Front;
                        else if ((role & VehicleMaterialRole.RearLamp) != 0)
                            kind = LampKind.Brake;
                        else
                            continue;
                    }
                    else if (!TryResolveKind(vehicleId, source.name, out kind))
                        continue;

                    Material runtime = new Material(source)
                    {
                        name = source.name + " (Vehicle Lamp Runtime)"
                    };

                    if (!runtime.HasProperty("_EmissionColor"))
                    {
                        Debug.LogWarning("Motor City: lamp material has no _EmissionColor: " +
                            source.name);
                        Destroy(runtime);
                        continue;
                    }

                    // Only the dedicated lamp submeshes receive emission.
                    // A shared atlas may also be exported as map_Ke; never
                    // use it as the emission mask for these specific lenses.
                    if (runtime.HasProperty("_EmissionMap"))
                        runtime.SetTexture("_EmissionMap", Texture2D.whiteTexture);

                    runtime.EnableKeyword("_EMISSION");
                    runtime.SetColor("_EmissionColor", Color.black);

                    slots[slot] = runtime;
                    changed = true;
                    bindings.Add(new Binding
                    {
                        Renderer = renderer,
                        Slot = slot,
                        Original = source,
                        Runtime = runtime,
                        Kind = kind
                    });

                    if (kind == LampKind.Front)
                        ++frontFound;
                    else
                        ++brakeFound;
                }

                if (changed)
                    renderer.sharedMaterials = slots;
            }

            applied = false;
            lastNight = -1f;

            if (brakeFound == 0 ||
                (vehicleId != VehicleIds.ToyotaAE86 && frontFound == 0))
            {
                List<string> imported = new();
                foreach (Renderer renderer in
                         currentVisual.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null ||
                        VehicleLampMaterialUtility.IsWheelRenderer(renderer.transform))
                        continue;

                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null && imported.Count < 18)
                            imported.Add(renderer.name + ": " + material.name);
                    }
                }

                Debug.LogWarning("Motor City: " + vehicleId +
                    " has missing authored lamp slots (front " + frontFound +
                    ", brake " + brakeFound + "). Actual materials: [" +
                    string.Join("; ", imported) +
                    "]. Check the updated vehicle prefab and OBJ material slots.");
            }
        }

        private static bool TryResolveKind(string id, string sourceName, out LampKind kind)
        {
            VehicleMaterialRole role = VehicleMaterialRoleCatalog.Resolve(sourceName);
            if (role == VehicleMaterialRole.RearLamp)
            {
                kind = LampKind.Brake;
                return true;
            }
            kind = LampKind.Front;
            return role == VehicleMaterialRole.FrontLamp && id != VehicleIds.ToyotaAE86;
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
            applied = false;
            lastNight = -1f;
        }

        private void OnDestroy()
        {
            RestoreBindings();
        }
    }
}
