using System.Collections.Generic;
using MotorCity.Input;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.World
{
    // Hybrid only: explicit Blender-authored lamp positions. No material-name
    // or pixel-color detection. Other cars retain their existing lighting.
    public sealed class HybridCoordinateLights : MonoBehaviour
    {
        private const string VisualName = "MotorCityVehicleVisual_Runtime";
        private const string RootName = "MotorCityHybridCoordinateLights";
        private static readonly Vector3[] Front =
        {
            new(-0.4199f, 0.4040f, 1.6630f),
            new( 0.4199f, 0.4040f, 1.6630f)
        };
        private static readonly Vector3[] Rear =
        {
            new(-0.3842f, 0.5957f, -1.0700f),
            new( 0.3842f, 0.5957f, -1.0700f)
        };
        private static readonly Vector3[] Accent =
        {
            new(-0.1937f, 0.3907f, 1.8150f),
            new( 0.1937f, 0.3907f, 1.8150f),
            new(-0.3916f, 0.3734f, -1.1540f),
            new( 0.3916f, 0.3734f, -1.1540f)
        };

        private readonly List<Renderer> frontRenderers = new();
        private readonly List<Renderer> rearRenderers = new();
        private readonly List<Material> ownedMaterials = new();
        private Transform currentVisual;
        private GameObject lightsRoot;
        private DayNightCycleController dayNight;
        private ArcadeCarController car;
        private bool hybrid;
        private bool lastFront;
        private bool lastRear;
        private bool initialized;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
        }

        public void SetVehicleId(string vehicleId)
        {
            hybrid = vehicleId == VehicleIds.Hybrid;
            Rebuild();
        }

        private void Update()
        {
            if (!hybrid)
                return;

            Transform active = transform.Find(VisualName);
            if (active != currentVisual || lightsRoot == null)
                Rebuild();

            if (lightsRoot == null)
                return;

            if (dayNight == null)
                dayNight = FindAnyObjectByType<DayNightCycleController>();

            bool night = dayNight != null && dayNight.NightAmount > 0.50f;
            bool brake = MotorCityInput.ReverseHeld ||
                (car != null && car.HandbrakeInputHeld);

            if (initialized && night == lastFront && brake == lastRear)
                return;

            initialized = true;
            lastFront = night;
            lastRear = brake;
            SetActive(frontRenderers, night);
            SetActive(rearRenderers, brake);
        }

        private void Rebuild()
        {
            Cleanup();
            currentVisual = hybrid ? transform.Find(VisualName) : null;
            if (currentVisual == null)
                return;

            // Put local anchors on the instantiated visual so its authored
            // transform, scaling and vehicle motion are inherited.
            lightsRoot = new GameObject(RootName);
            lightsRoot.transform.SetParent(currentVisual, false);
            AddPoints(Front, "Front", new Color(0.94f, 0.97f, 1f), 0.075f, frontRenderers);
            AddPoints(Rear, "Brake", new Color(1f, 0.025f, 0.01f), 0.09f, rearRenderers);
            AddPoints(Accent, "Cyan Accent", new Color(0.01f, 0.67f, 1f), 0.032f, null);
            initialized = false;
            SetActive(frontRenderers, false);
            SetActive(rearRenderers, false);
        }

        private void AddPoints(Vector3[] points, string prefix, Color color,
            float diameter, List<Renderer> controlled)
        {
            for (int i = 0; i < points.Length; i++)
            {
                GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.name = prefix + " " + (i + 1);
                point.transform.SetParent(lightsRoot.transform, false);
                point.transform.localPosition = points[i];
                point.transform.localScale = Vector3.one * diameter;
                Collider collider = point.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                Renderer renderer = point.GetComponent<Renderer>();
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                if (shader == null)
                {
                    renderer.enabled = false;
                    continue;
                }

                Material material = new Material(shader);
                material.name = "MotorCity Hybrid " + prefix + " Runtime";
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                ownedMaterials.Add(material);
                controlled?.Add(renderer);
            }
        }

        private static void SetActive(List<Renderer> renderers, bool active)
        {
            foreach (Renderer renderer in renderers)
                if (renderer != null)
                    renderer.enabled = active;
        }

        private void Cleanup()
        {
            frontRenderers.Clear();
            rearRenderers.Clear();
            if (lightsRoot != null)
            {
                lightsRoot.SetActive(false);
                Destroy(lightsRoot);
                lightsRoot = null;
            }
            foreach (Material material in ownedMaterials)
                if (material != null)
                    Destroy(material);
            ownedMaterials.Clear();
            initialized = false;
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
