using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.Vehicle
{
    public sealed class CartoonSportsCarRuntimeInstaller : MonoBehaviour
    {
        private const float TargetLength = 4.2f;
        private const float TargetWheelCenterLocalY = 0.36f;
        private static readonly string[] ExactWheelNames = { "tyre003", "tyre004", "tyre1", "tyre2" };
        private static readonly HashSet<string> FallbackVisualNames = new()
        {
            "LowerBody", "UpperBody", "Cabin", "Hood", "FrontBumper", "RearBumper", "Spoiler",
            "HeadlightL", "HeadlightR", "TailLightL", "TailLightR",
            "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR",
            "Wheel_FL_Rim", "Wheel_FR_Rim", "Wheel_RL_Rim", "Wheel_RR_Rim"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ScheduleInstall()
        {
            GameObject runner = new("Cartoon Sports Car Installer");
            DontDestroyOnLoad(runner);
            runner.AddComponent<CartoonSportsCarRuntimeInstaller>();
        }

        private void Start()
        {
            ArcadeCarController car = Object.FindAnyObjectByType<ArcadeCarController>();
            if (car != null) TryInstallNow(car);
            Destroy(gameObject);
        }

        public static bool TryInstallNow(ArcadeCarController car)
        {
            if (car == null) return false;
            if (car.transform.Find("CartoonSportsCarVisual_Runtime") != null) return true;

            GameObject prefab = Resources.Load<GameObject>("MotorCity/PlayerCarVisual");
            if (prefab == null) return false;

            return Install(car, prefab);
        }

        private static bool Install(ArcadeCarController car, GameObject prefab)
        {
            Transform carTransform = car.transform;
            GameObject visual = Instantiate(prefab, carTransform);
            visual.name = "CartoonSportsCarVisual_Runtime";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            StripPhysics(visual);
            NormalizeHorizontalScaleAndRotation(visual.transform);

            List<Transform> wheelAnchors = FindExactWheels(visual.transform);
            if (wheelAnchors.Count < 4) wheelAnchors = FindWheelMeshesFallback(visual.transform);

            if (wheelAnchors.Count < 4)
            {
                Debug.LogWarning("Motor City: CARRERA loaded, but four wheel anchors were not found. Keeping fallback vehicle visual.");
                Object.Destroy(visual);
                return false;
            }

            AlignBodyToWheelCenters(visual.transform, carTransform, wheelAnchors);
            UpgradeMaterialsForCurrentPipeline(visual);

            Transform[] ordered = OrderWheels(carTransform, wheelAnchors);
            Vector3[] centerWorld = new Vector3[4];
            Vector3[] centerLocal = new Vector3[4];
            float radiusSum = 0f;

            for (int i = 0; i < 4; i++)
            {
                Bounds bounds = RendererBounds(ordered[i]);
                centerWorld[i] = bounds.center;
                centerLocal[i] = carTransform.InverseTransformPoint(bounds.center);
                radiusSum += Mathf.Clamp(Mathf.Max(bounds.extents.y, bounds.extents.z), 0.29f, 0.40f);
            }

            float measuredRadius = radiusSum / 4f;
            Transform[] spinRoots = new Transform[4];
            Transform[] brakeRoots = new Transform[4];

            for (int i = 0; i < 4; i++)
            {
                spinRoots[i] = CreateWheelRoot(carTransform, $"CarreraWheelSpin_{i}", centerWorld[i]);
                brakeRoots[i] = CreateWheelRoot(carTransform, $"CarreraWheelBrake_{i}", centerWorld[i]);
            }

            BuildWheelVisualGroups(visual.transform, centerWorld, measuredRadius, spinRoots, brakeRoots);

            HideOnlyPrimitiveFallback(carTransform);
            car.ConfigureExternalWheelRig(spinRoots, brakeRoots, centerLocal, measuredRadius);
            Debug.Log("Motor City: CARRERA connected to transform-based CarController movement with grouped visual wheels and original texture atlas.");
            return true;
        }

        private static Transform CreateWheelRoot(Transform car, string name, Vector3 worldPosition)
        {
            GameObject rootObject = new(name);
            Transform root = rootObject.transform;
            root.SetParent(car, true);
            root.position = worldPosition;
            root.rotation = car.rotation;
            root.localScale = Vector3.one;
            return root;
        }

        private static void BuildWheelVisualGroups(
            Transform visual,
            Vector3[] wheelCenters,
            float wheelRadius,
            Transform[] spinRoots,
            Transform[] brakeRoots)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            var spinParts = new List<Transform>[4];
            var brakeParts = new List<Transform>[4];
            for (int i = 0; i < 4; i++)
            {
                spinParts[i] = new List<Transform>();
                brakeParts[i] = new List<Transform>();
            }

            float maxDistance = Mathf.Max(0.6f, wheelRadius * 1.9f);
            float maxDistanceSq = maxDistance * maxDistance;

            foreach (Renderer renderer in renderers)
            {
                Transform part = renderer.transform;
                string lower = part.name.ToLowerInvariant();

                bool brakePart = lower.Contains("brake");
                bool spinPart = lower.Contains("tyre") || lower.Contains("tire") || lower.Contains("rim") || lower.Contains("wheel");
                if (!brakePart && !spinPart) continue;

                int nearest = NearestWheel(renderer.bounds.center, wheelCenters, out float distanceSq);
                if (nearest < 0 || distanceSq > maxDistanceSq) continue;

                if (brakePart) brakeParts[nearest].Add(part);
                else spinParts[nearest].Add(part);
            }

            for (int i = 0; i < 4; i++)
            {
                foreach (Transform part in TopLevelParts(spinParts[i]))
                    part.SetParent(spinRoots[i], true);

                foreach (Transform part in TopLevelParts(brakeParts[i]))
                    part.SetParent(brakeRoots[i], true);
            }
        }

        private static IEnumerable<Transform> TopLevelParts(List<Transform> parts)
        {
            HashSet<Transform> set = parts.ToHashSet();
            foreach (Transform part in parts)
            {
                bool nested = false;
                Transform parent = part.parent;
                while (parent != null)
                {
                    if (set.Contains(parent))
                    {
                        nested = true;
                        break;
                    }
                    parent = parent.parent;
                }

                if (!nested) yield return part;
            }
        }

        private static int NearestWheel(Vector3 point, Vector3[] centers, out float distanceSq)
        {
            int best = -1;
            distanceSq = float.MaxValue;
            for (int i = 0; i < centers.Length; i++)
            {
                float candidate = (centers[i] - point).sqrMagnitude;
                if (candidate >= distanceSq) continue;
                distanceSq = candidate;
                best = i;
            }
            return best;
        }

        private static void NormalizeHorizontalScaleAndRotation(Transform visual)
        {
            Bounds bounds = RendererBounds(visual);
            if (bounds.size.x > bounds.size.z * 1.15f)
            {
                visual.localRotation = Quaternion.Euler(0f, 90f, 0f);
                bounds = RendererBounds(visual);
            }

            float length = Mathf.Max(bounds.size.x, bounds.size.z);
            if (length < 0.01f) return;

            visual.localScale *= TargetLength / length;
            bounds = RendererBounds(visual);
            Transform parent = visual.parent;
            Vector3 centerLocal = parent.InverseTransformPoint(bounds.center);
            visual.localPosition -= new Vector3(centerLocal.x, 0f, centerLocal.z);
        }

        private static void AlignBodyToWheelCenters(Transform visual, Transform carRoot, List<Transform> wheels)
        {
            float sumY = 0f;
            int count = 0;

            foreach (Transform wheel in wheels)
            {
                Bounds bounds = RendererBounds(wheel);
                sumY += carRoot.InverseTransformPoint(bounds.center).y;
                count++;
            }

            if (count == 0) return;
            float currentAverageY = sumY / count;
            visual.localPosition += Vector3.up * (TargetWheelCenterLocalY - currentAverageY);
        }

        private static void UpgradeMaterialsForCurrentPipeline(GameObject root)
        {
            if (GraphicsSettings.currentRenderPipeline == null) return;
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            var cache = new Dictionary<Material, Material>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] source = renderer.sharedMaterials;
                Material[] upgraded = new Material[source.Length];

                for (int i = 0; i < source.Length; i++)
                {
                    Material old = source[i];
                    if (old == null)
                    {
                        upgraded[i] = null;
                        continue;
                    }

                    if (old.shader != null && old.shader.name.StartsWith("Universal Render Pipeline/"))
                    {
                        upgraded[i] = old;
                        continue;
                    }

                    if (cache.TryGetValue(old, out Material cached))
                    {
                        upgraded[i] = cached;
                        continue;
                    }

                    string lower = old.name.ToLowerInvariant();
                    bool glass = lower.Contains("glass");
                    bool mirror = lower.Contains("mirror");
                    bool matte = lower.Contains("matte");

                    Texture baseTexture = old.HasProperty("_MainTex") ? old.GetTexture("_MainTex") : null;
                    Color oldColor = old.HasProperty("_Color") ? old.GetColor("_Color") : Color.white;
                    float metallic = old.HasProperty("_Metallic") ? old.GetFloat("_Metallic") : 0f;
                    float smoothness = old.HasProperty("_Glossiness") ? old.GetFloat("_Glossiness") : 0.4f;

                    Material material = new(urpLit)
                    {
                        name = old.name + "_URP",
                        enableInstancing = true
                    };

                    if (baseTexture != null && material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", baseTexture);
                        if (old.HasProperty("_MainTex"))
                        {
                            material.SetTextureScale("_BaseMap", old.GetTextureScale("_MainTex"));
                            material.SetTextureOffset("_BaseMap", old.GetTextureOffset("_MainTex"));
                        }
                    }

                    Color baseColor = baseTexture != null && !glass ? Color.white : oldColor;
                    if (glass)
                        baseColor = new Color(0.18f, 0.22f, 0.27f, 0.38f);

                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
                    if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", mirror ? 0.92f : metallic);
                    if (material.HasProperty("_Smoothness"))
                        material.SetFloat("_Smoothness", mirror ? 0.92f : (matte ? 0.24f : smoothness));

                    if (glass)
                    {
                        material.SetFloat("_Surface", 1f);
                        material.SetFloat("_Blend", 0f);
                        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                        material.SetFloat("_ZWrite", 0f);
                        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        material.renderQueue = (int)RenderQueue.Transparent;
                    }

                    cache.Add(old, material);
                    upgraded[i] = material;
                }

                renderer.sharedMaterials = upgraded;
            }
        }

        private static List<Transform> FindExactWheels(Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            var result = new List<Transform>(4);
            foreach (string exactName in ExactWheelNames)
            {
                Transform found = all.FirstOrDefault(t => t.name.Equals(exactName, System.StringComparison.OrdinalIgnoreCase));
                if (found != null && found.GetComponentInChildren<Renderer>(true) != null) result.Add(found);
            }
            return result;
        }

        private static List<Transform> FindWheelMeshesFallback(Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            var candidates = new List<Transform>();
            foreach (Transform item in all)
            {
                if (item == root) continue;
                string n = item.name.ToLowerInvariant();
                if (!(n.Contains("tyre") || n.Contains("tire"))) continue;
                if (item.GetComponentInChildren<Renderer>(true) == null) continue;
                bool childOfExisting = candidates.Any(c => item.IsChildOf(c));
                if (!childOfExisting) candidates.Add(item);
            }
            if (candidates.Count <= 4) return candidates;
            return candidates.OrderByDescending(t => RendererBounds(t).size.sqrMagnitude).Take(4).ToList();
        }

        private static Transform[] OrderWheels(Transform car, List<Transform> wheels)
        {
            return wheels.OrderByDescending(t => car.InverseTransformPoint(RendererBounds(t).center).z)
                .ThenBy(t => car.InverseTransformPoint(RendererBounds(t).center).x)
                .Take(4)
                .ToArray();
        }

        private static Bounds RendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.one * 0.1f);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void StripPhysics(GameObject visual)
        {
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                Object.Destroy(collider);
            }

            foreach (Rigidbody rigidbody in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                Object.Destroy(rigidbody);
            }
        }

        private static void HideOnlyPrimitiveFallback(Transform car)
        {
            foreach (Renderer renderer in car.GetComponentsInChildren<Renderer>(true))
            {
                if (FallbackVisualNames.Contains(renderer.transform.name)) renderer.enabled = false;
            }
        }
    }
}
