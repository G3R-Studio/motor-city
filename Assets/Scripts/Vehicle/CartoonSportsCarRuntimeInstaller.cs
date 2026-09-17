using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.Vehicle
{
    public sealed class CartoonSportsCarRuntimeInstaller : MonoBehaviour
    {
        private const float TargetLength = 4.2f;
        private static readonly string[] ExactWheelNames = { "tyre003", "tyre004", "tyre1", "tyre2" };
        private static readonly string[] ExactBrakeNames = { "brakes003", "brakes004", "brakes1", "brakes2" };
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

        private IEnumerator Start()
        {
            yield return null;
            yield return null;

            ArcadeCarController car = Object.FindAnyObjectByType<ArcadeCarController>();
            if (car == null)
            {
                Destroy(gameObject);
                yield break;
            }

            GameObject prefab = Resources.Load<GameObject>("MotorCity/PlayerCarVisual");
            if (prefab == null)
            {
                Destroy(gameObject);
                yield break;
            }

            Install(car, prefab);
            Destroy(gameObject);
        }

        private static void Install(ArcadeCarController car, GameObject prefab)
        {
            Transform carTransform = car.transform;
            GameObject visual = Instantiate(prefab, carTransform);
            visual.name = "CartoonSportsCarVisual_Runtime";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            DisablePhysics(visual);
            NormalizeVisualScale(visual.transform);
            UpgradeMaterialsForCurrentPipeline(visual);
            ApplyBodyPaint(visual);

            List<Transform> wheelMeshes = FindExactWheels(visual.transform);
            if (wheelMeshes.Count < 4) wheelMeshes = FindWheelMeshesFallback(visual.transform);

            if (wheelMeshes.Count < 4)
            {
                Debug.LogWarning("Motor City: full CARRERA model loaded, but four wheel transforms were not found. Keeping fallback suspension visuals.");
                visual.SetActive(false);
                return;
            }

            Transform[] ordered = OrderWheels(carTransform, wheelMeshes);
            List<Transform> brakes = FindExactBrakes(visual.transform);
            Transform[] carriers = new Transform[4];
            Transform[] spinPivots = new Transform[4];
            Vector3[] suspensionPoints = new Vector3[4];
            float measuredRadius = 0.33f;

            for (int i = 0; i < 4; i++)
            {
                Transform tyre = ordered[i];
                Bounds bounds = RendererBounds(tyre);
                measuredRadius = Mathf.Clamp(Mathf.Max(bounds.extents.y, bounds.extents.z), 0.27f, 0.42f);

                GameObject carrierObject = new($"AssetWheelCarrier_{i}");
                Transform carrier = carrierObject.transform;
                carrier.SetParent(carTransform, true);
                carrier.position = bounds.center;
                carrier.rotation = carTransform.rotation;

                GameObject spinObject = new($"AssetWheelSpin_{i}");
                Transform spin = spinObject.transform;
                spin.SetParent(carrier, false);
                spin.localPosition = Vector3.zero;
                spin.localRotation = Quaternion.identity;

                tyre.SetParent(spin, true);

                Transform nearestBrake = FindNearestBrake(bounds.center, brakes);
                if (nearestBrake != null)
                {
                    nearestBrake.SetParent(carrier, true);
                    brakes.Remove(nearestBrake);
                }

                carriers[i] = carrier;
                spinPivots[i] = spin;

                Vector3 local = carTransform.InverseTransformPoint(bounds.center);
                suspensionPoints[i] = new Vector3(local.x, 0.20f, local.z);
            }

            HideOnlyPrimitiveFallback(carTransform);
            car.ConfigureExternalWheelRig(carriers, spinPivots, suspensionPoints, measuredRadius);
            Debug.Log("Motor City: full CARRERA installed with visible asset wheels, brakes, interior and suspension rig.");
        }

        private static void NormalizeVisualScale(Transform visual)
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

            // Only center the body in X/Z. Do not vertically normalize from the tyre
            // bottoms: the suspension owns wheel height and the chassis should retain
            // the FBX's original relationship to its wheel arches.
            visual.localPosition -= new Vector3(centerLocal.x, 0f, centerLocal.z);
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

                    if (old.shader != null && old.shader.name.StartsWith("Universal Render Pipeline"))
                    {
                        upgraded[i] = old;
                        continue;
                    }

                    if (cache.TryGetValue(old, out Material cached))
                    {
                        upgraded[i] = cached;
                        continue;
                    }

                    Texture mainTexture = old.HasProperty("_MainTex") ? old.GetTexture("_MainTex") : null;
                    Color color = old.HasProperty("_Color") ? old.GetColor("_Color") : Color.white;
                    float metallic = old.HasProperty("_Metallic") ? old.GetFloat("_Metallic") : 0f;
                    float smoothness = old.HasProperty("_Glossiness") ? old.GetFloat("_Glossiness") : 0.35f;

                    Material material = new(urpLit) { name = old.name + "_URP" };
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    if (mainTexture != null && material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", mainTexture);
                    if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
                    cache.Add(old, material);
                    upgraded[i] = material;
                }

                renderer.sharedMaterials = upgraded;
            }
        }

        private static void ApplyBodyPaint(GameObject root)
        {
            Color paint = new(0.34f, 0.025f, 0.018f, 1f);
            string[] bodyTokens = { "carrera", "door", "hood", "spoiler", "bottom" };

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                string lower = renderer.name.ToLowerInvariant();
                bool bodyPart = bodyTokens.Any(token => lower.Contains(token));
                bool excluded = lower.Contains("glass") || lower.Contains("tyre") || lower.Contains("tire") ||
                                lower.Contains("rim") || lower.Contains("brake") || lower.Contains("lamp") ||
                                lower.Contains("inside") || lower.Contains("seat") || lower.Contains("dash") ||
                                lower.Contains("mirror");
                if (!bodyPart || excluded) continue;

                Material[] materials = renderer.materials;
                foreach (Material material in materials)
                {
                    if (material == null || !material.HasProperty("_BaseColor")) continue;
                    Color original = material.GetColor("_BaseColor");
                    material.SetColor("_BaseColor", new Color(
                        Mathf.Max(0.08f, original.r * paint.r * 2f),
                        Mathf.Max(0.01f, original.g * paint.g * 2f),
                        Mathf.Max(0.008f, original.b * paint.b * 2f),
                        original.a));
                    if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.45f);
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.68f);
                }
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

        private static List<Transform> FindExactBrakes(Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            var result = new List<Transform>(4);
            foreach (string exactName in ExactBrakeNames)
            {
                Transform found = all.FirstOrDefault(t => t.name.Equals(exactName, System.StringComparison.OrdinalIgnoreCase));
                if (found != null) result.Add(found);
            }
            return result;
        }

        private static Transform FindNearestBrake(Vector3 wheelCenter, List<Transform> brakes)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (Transform brake in brakes)
            {
                float distance = (RendererBounds(brake).center - wheelCenter).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = brake;
                }
            }
            return best;
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
                .ThenBy(t => car.InverseTransformPoint(RendererBounds(t).center).x).Take(4).ToArray();
        }

        private static Bounds RendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.one * 0.1f);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void DisablePhysics(GameObject visual)
        {
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Rigidbody rigidbody in visual.GetComponentsInChildren<Rigidbody>(true)) rigidbody.isKinematic = true;
        }

        private static void HideOnlyPrimitiveFallback(Transform car)
        {
            foreach (Renderer renderer in car.GetComponentsInChildren<Renderer>(true))
            {
                if (FallbackVisualNames.Contains(renderer.transform.name))
                    renderer.enabled = false;
            }
        }
    }
}
