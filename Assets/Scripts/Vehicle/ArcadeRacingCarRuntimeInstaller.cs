using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.Vehicle
{
    public sealed class ArcadeRacingCarRuntimeInstaller : MonoBehaviour
    {
        private const float TargetLength = 4.35f;
        private const float TargetWheelCenterLocalY = 0.42f;

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
            GameObject runner = new("ARCADE Free Racing Car Installer");
            DontDestroyOnLoad(runner);
            runner.AddComponent<ArcadeRacingCarRuntimeInstaller>();
        }

        private void Start()
        {
            ArcadeCarController car = UnityEngine.Object.FindAnyObjectByType<ArcadeCarController>();
            if (car != null) TryInstallNow(car);
            UnityEngine.Object.Destroy(gameObject);
        }

        public static bool TryInstallNow(ArcadeCarController car)
        {
            if (car == null) return false;
            if (car.transform.Find("ArcadeFreeRacingCarVisual_Runtime") != null) return true;

            GameObject prefab = Resources.Load<GameObject>("MotorCity/PlayerCarVisual");
            if (prefab == null) return false;

            return Install(car, prefab);
        }

        private static bool Install(ArcadeCarController car, GameObject prefab)
        {
            Transform carTransform = car.transform;
            GameObject visual = Instantiate(prefab, carTransform);
            visual.name = "ArcadeFreeRacingCarVisual_Runtime";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            int importedBodyColliderCount = StripImportedPhysics(visual);
            NormalizeHorizontalScaleAndRotation(visual.transform);
            UpgradeMaterialsForCurrentPipeline(visual);

            List<Transform> wheelAnchors = FindWheelAnchors(visual.transform);
            if (wheelAnchors.Count < 4)
            {
                Debug.LogWarning(
                    "Motor City: ARCADE Free Racing Car loaded, but four separate wheel meshes were not found. " +
                    "Keeping the visual body with fallback wheel visuals.");
                HideFallbackBodyOnly(carTransform);
                return true;
            }

            AlignBodyToWheelCenters(visual.transform, carTransform, wheelAnchors);

            Transform[] ordered = OrderWheels(carTransform, wheelAnchors);
            Vector3[] centerWorld = new Vector3[4];
            Vector3[] centerLocal = new Vector3[4];
            float radiusSum = 0f;
            Transform[] spinRoots = new Transform[4];

            for (int i = 0; i < 4; i++)
            {
                Bounds bounds = RendererBounds(ordered[i]);
                centerWorld[i] = bounds.center;
                centerLocal[i] = carTransform.InverseTransformPoint(bounds.center);
                radiusSum += Mathf.Clamp(bounds.extents.y, 0.28f, 0.52f);

                spinRoots[i] = CreateWheelRoot(
                    carTransform,
                    $"ArcadeRacingWheelSpin_{i}",
                    centerWorld[i]);

                ordered[i].SetParent(spinRoots[i], true);
            }

            float measuredRadius = radiusSum / 4f;

            HidePrimitiveFallback(carTransform);

            if (importedBodyColliderCount > 0)
            {
                BoxCollider fallbackCollider = carTransform.GetComponent<BoxCollider>();
                if (fallbackCollider != null)
                {
                    fallbackCollider.enabled = false;
                    UnityEngine.Object.Destroy(fallbackCollider);
                }

                car.UseAutomaticMassProperties();
            }

            car.ConfigureExternalWheelRig(
                spinRoots,
                null,
                centerLocal,
                measuredRadius);

            Debug.Log(
                "Motor City: ARCADE Free Racing Car connected to Pro Drift Controller v1 WheelCollider physics.");

            return true;
        }

        private static List<Transform> FindWheelAnchors(Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            var named = new List<Transform>();

            foreach (Transform item in all)
            {
                if (item == root) continue;
                string n = item.name.ToLowerInvariant();
                if (!(n.Contains("wheel") || n.Contains("tire") || n.Contains("tyre"))) continue;
                if (item.GetComponentInChildren<Renderer>(true) == null) continue;

                bool nested = named.Any(existing => item.IsChildOf(existing));
                if (!nested) named.Add(item);
            }

            if (named.Count >= 4)
                return SelectFourCornerWheels(root, named);

            Bounds bodyBounds = RendererBounds(root);
            Vector3 bodyCenter = bodyBounds.center;
            Vector3 bodySize = bodyBounds.size;
            var geometric = new List<Transform>();

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Transform item = renderer.transform;
                Bounds bounds = renderer.bounds;
                Vector3 local = root.InverseTransformPoint(bounds.center);

                bool lowEnough =
                    bounds.center.y <= bodyCenter.y + bodySize.y * 0.05f;
                bool offCenter =
                    Mathf.Abs(local.x) >= bodySize.x * 0.18f &&
                    Mathf.Abs(local.z) >= bodySize.z * 0.15f;
                bool smallEnough =
                    Mathf.Max(bounds.size.x, bounds.size.z) <=
                    Mathf.Max(bodySize.x, bodySize.z) * 0.38f;

                if (lowEnough && offCenter && smallEnough)
                    geometric.Add(item);
            }

            return SelectFourCornerWheels(root, geometric);
        }

        private static List<Transform> SelectFourCornerWheels(
            Transform root,
            IEnumerable<Transform> source)
        {
            return source
                .Distinct()
                .Select(t => new
                {
                    Transform = t,
                    Local = root.InverseTransformPoint(RendererBounds(t).center),
                    Size = RendererBounds(t).size.sqrMagnitude
                })
                .OrderByDescending(x => Mathf.Abs(x.Local.x) + Mathf.Abs(x.Local.z))
                .ThenByDescending(x => x.Size)
                .Take(4)
                .Select(x => x.Transform)
                .ToList();
        }

        private static Transform[] OrderWheels(
            Transform car,
            List<Transform> wheels)
        {
            return wheels
                .OrderByDescending(
                    t => car.InverseTransformPoint(RendererBounds(t).center).z)
                .ThenBy(
                    t => car.InverseTransformPoint(RendererBounds(t).center).x)
                .Take(4)
                .ToArray();
        }

        private static Transform CreateWheelRoot(
            Transform car,
            string name,
            Vector3 worldPosition)
        {
            GameObject rootObject = new(name);
            Transform root = rootObject.transform;
            root.SetParent(car, true);
            root.position = worldPosition;
            root.rotation = car.rotation;
            root.localScale = Vector3.one;
            return root;
        }

        private static void NormalizeHorizontalScaleAndRotation(Transform visual)
        {
            Bounds bounds = RendererBounds(visual);

            if (bounds.size.x > bounds.size.z * 1.12f)
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
            visual.localPosition -=
                new Vector3(centerLocal.x, 0f, centerLocal.z);
        }

        private static void AlignBodyToWheelCenters(
            Transform visual,
            Transform carRoot,
            List<Transform> wheels)
        {
            float sumY = 0f;

            foreach (Transform wheel in wheels)
                sumY += carRoot.InverseTransformPoint(RendererBounds(wheel).center).y;

            float currentAverageY = sumY / wheels.Count;
            visual.localPosition +=
                Vector3.up *
                (TargetWheelCenterLocalY - currentAverageY);
        }

        private static Bounds RendererBounds(Transform root)
        {
            Renderer[] renderers =
                root.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
                return new Bounds(root.position, Vector3.one * 0.1f);

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private static int StripImportedPhysics(GameObject visual)
        {
            foreach (WheelCollider wheel in
                     visual.GetComponentsInChildren<WheelCollider>(true))
            {
                wheel.enabled = false;
                UnityEngine.Object.Destroy(wheel);
            }

            foreach (Rigidbody rigidbody in
                     visual.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
                UnityEngine.Object.Destroy(rigidbody);
            }

            int bodyColliderCount = 0;

            foreach (Collider collider in
                     visual.GetComponentsInChildren<Collider>(true))
            {
                if (collider is WheelCollider) continue;

                string lower = collider.transform.name.ToLowerInvariant();
                bool wheelPart =
                    lower.Contains("wheel") ||
                    lower.Contains("tire") ||
                    lower.Contains("tyre") ||
                    lower.Contains("rim");

                if (wheelPart)
                {
                    collider.enabled = false;
                    UnityEngine.Object.Destroy(collider);
                    continue;
                }

                collider.enabled = true;
                bodyColliderCount++;
            }

            return bodyColliderCount;
        }

        private static void UpgradeMaterialsForCurrentPipeline(GameObject root)
        {
            if (GraphicsSettings.currentRenderPipeline == null) return;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return;

            var cache = new Dictionary<Material, Material>();

            foreach (Renderer renderer in
                     root.GetComponentsInChildren<Renderer>(true))
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

                    if (old.shader != null &&
                        old.shader.name.StartsWith(
                            "Universal Render Pipeline/",
                            StringComparison.Ordinal))
                    {
                        upgraded[i] = old;
                        continue;
                    }

                    if (cache.TryGetValue(old, out Material cached))
                    {
                        upgraded[i] = cached;
                        continue;
                    }

                    Texture baseTexture =
                        old.HasProperty("_MainTex")
                            ? old.GetTexture("_MainTex")
                            : null;

                    Color oldColor =
                        old.HasProperty("_Color")
                            ? old.GetColor("_Color")
                            : Color.white;

                    float metallic =
                        old.HasProperty("_Metallic")
                            ? old.GetFloat("_Metallic")
                            : 0f;

                    float smoothness =
                        old.HasProperty("_Glossiness")
                            ? old.GetFloat("_Glossiness")
                            : 0.35f;

                    Material material = new(urpLit)
                    {
                        name = old.name + "_URP",
                        enableInstancing = true
                    };

                    if (baseTexture != null &&
                        material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", baseTexture);
                        material.SetTextureScale(
                            "_BaseMap",
                            old.GetTextureScale("_MainTex"));
                        material.SetTextureOffset(
                            "_BaseMap",
                            old.GetTextureOffset("_MainTex"));
                    }

                    if (material.HasProperty("_BaseColor"))
                        material.SetColor(
                            "_BaseColor",
                            baseTexture != null ? Color.white : oldColor);

                    if (material.HasProperty("_Metallic"))
                        material.SetFloat("_Metallic", metallic);

                    if (material.HasProperty("_Smoothness"))
                        material.SetFloat("_Smoothness", smoothness);

                    cache.Add(old, material);
                    upgraded[i] = material;
                }

                renderer.sharedMaterials = upgraded;
            }
        }

        private static void HidePrimitiveFallback(Transform car)
        {
            foreach (Renderer renderer in
                     car.GetComponentsInChildren<Renderer>(true))
            {
                if (FallbackVisualNames.Contains(renderer.transform.name))
                    renderer.enabled = false;
            }
        }

        private static void HideFallbackBodyOnly(Transform car)
        {
            foreach (Renderer renderer in
                     car.GetComponentsInChildren<Renderer>(true))
            {
                string n = renderer.transform.name;
                if (n.StartsWith("Wheel_", StringComparison.Ordinal)) continue;
                if (FallbackVisualNames.Contains(n))
                    renderer.enabled = false;
            }
        }
    }
}
