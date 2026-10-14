using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Geometry-based fallback for WebGL builds where URP's shader fog is absent.
    /// Three low-poly depth-tested shells; no renderer feature, depth texture,
    /// full-screen post-process, variant keywords, or changes to city materials.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class MotorCityDistanceFogShell : MonoBehaviour
    {
        private static readonly float[] Fractions = { 0.64f, 0.80f, 0.94f };
        private static readonly float[] Opacities = { 0.23f, 0.45f, 0.78f };
        private const int ShellLayer = 0;
        private readonly Transform[] shells = new Transform[3];
        private readonly Material[] materials = new Material[3];
        private Camera sourceCamera;
        private bool ready;

        public static bool Active { get; private set; }

        private void Awake()
        {
            sourceCamera = GetComponent<Camera>();
            Shader shader = Shader.Find("MotorCity/DistanceFogShell");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[MotorCity] Distance fog shell shader unavailable.");
                return;
            }

            for (int i = 0; i < shells.Length; i++)
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.name = "Motor City Distance Fog " + i;
                obj.layer = ShellLayer;
                obj.transform.SetParent(transform, false);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                Collider collider = obj.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);
                Material material = new Material(shader);
                material.name = "Motor City Fog Shell " + i;
                obj.GetComponent<Renderer>().sharedMaterial = material;
                shells[i] = obj.transform;
                materials[i] = material;
            }
            ready = true;
            Active = true;
        }

        private void LateUpdate()
        {
            if (!ready)
                return;

            // Reuse the same distance policy as the city camera. The shells
            // remain camera-relative and never affect collisions or culling.
            float far = sourceCamera.farClipPlane;
            Color fog = RenderSettings.fogColor;
            for (int i = 0; i < shells.Length; i++)
            {
                float diameter = far * Fractions[i] * 2f;
                shells[i].localScale = new Vector3(diameter, diameter, diameter);
                fog.a = Opacities[i];
                materials[i].SetColor("_FogColor", fog);
            }
        }

        private void OnDisable()
        {
            Active = false;
        }

        private void OnEnable()
        {
            if (ready)
                Active = true;
        }

        private void OnDestroy()
        {
            Active = false;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] != null)
                    Destroy(materials[i]);
        }
    }
}
