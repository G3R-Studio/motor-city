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
            if (prefab == null)
                return ConfigureFallbackRig(car);

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

            StripImportedPhysics(visual);
            NormalizeHorizontalScaleAndRotation(visual.transform);
            UpgradeMaterialsForCurrentPipeline(visual);

            List<Transform> wheelAnchors = FindWheelAnchors(visual.transform);
            if (wheelAnchors.Count < 4)
            {
                Debug.LogWarning(
                    "Motor City: ARCADE Free Racing Car loaded, but four separate wheel meshes were not found. " +
                    "Keeping the visual body and using the fallback wheel rig.");
                HideFallbackBodyOnly(carTransform);
                return ConfigureFallbackRig(car);
            }

            AlignWheelbaseWithCarForward(
                visual.transform,
                carTransform,
                wheelAnchors);

            EnsureVisualNoseFacesPositiveZ(
                visual.transform,
                carTransform,
                wheelAnchors);

            CenterVisualHorizontally(
                visual.transform,
                carTransform);

            AlignBodyToWheelCenters(
                visual.transform,
                carTransform,
                wheelAnchors);

            // Re-evaluate the anchors after all visual rotations. The Prometeo
            // contract is explicit: indices 0/1 are the physical front axle
            // (+Z in PlayerCar local space), 2/3 are the rear axle.
            wheelAnchors =
                FindWheelAnchors(
                    visual.transform);

            Transform[] ordered =
                OrderWheels(
                    carTransform,
                    wheelAnchors);
            if (ordered.Length < 4)
            {
                Debug.LogError(
                    "Motor City: could not classify four ARCADE wheels into front/rear axles.");
                return ConfigureFallbackRig(car);
            }

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

            SymmetrizePhysicalWheelCenters(
                centerLocal);

            HidePrimitiveFallback(carTransform);

            BoxCollider chassis =
                carTransform.GetComponent<BoxCollider>();

            if (chassis != null)
            {
                chassis.enabled =
                    true;
            }

            car.ConfigurePrometeoRig(
                spinRoots,
                centerLocal,
                measuredRadius);

            Debug.Log(
                "Motor City: ARCADE Free Racing Car prepared for Prometeo Car Controller physics. " +
                $"FL={centerLocal[0]}, FR={centerLocal[1]}, " +
                $"RL={centerLocal[2]}, RR={centerLocal[3]}. " +
                $"Front axle average Z={(centerLocal[0].z + centerLocal[1].z) * 0.5f:0.###}, " +
                $"rear axle average Z={(centerLocal[2].z + centerLocal[3].z) * 0.5f:0.###}.");

            return true;
        }

        private static void SymmetrizePhysicalWheelCenters(
            Vector3[] centers)
        {
            if (centers == null ||
                centers.Length < 4)
                return;

            float frontHalfTrack =
                (Mathf.Abs(centers[FrontLeftIndex].x) +
                 Mathf.Abs(centers[FrontRightIndex].x)) *
                0.5f;

            float rearHalfTrack =
                (Mathf.Abs(centers[RearLeftIndex].x) +
                 Mathf.Abs(centers[RearRightIndex].x)) *
                0.5f;

            float frontZ =
                (centers[FrontLeftIndex].z +
                 centers[FrontRightIndex].z) *
                0.5f;

            float rearZ =
                (centers[RearLeftIndex].z +
                 centers[RearRightIndex].z) *
                0.5f;

            float frontY =
                (centers[FrontLeftIndex].y +
                 centers[FrontRightIndex].y) *
                0.5f;

            float rearY =
                (centers[RearLeftIndex].y +
                 centers[RearRightIndex].y) *
                0.5f;

            centers[FrontLeftIndex] =
                new Vector3(
                    -frontHalfTrack,
                    frontY,
                    frontZ);

            centers[FrontRightIndex] =
                new Vector3(
                    frontHalfTrack,
                    frontY,
                    frontZ);

            centers[RearLeftIndex] =
                new Vector3(
                    -rearHalfTrack,
                    rearY,
                    rearZ);

            centers[RearRightIndex] =
                new Vector3(
                    rearHalfTrack,
                    rearY,
                    rearZ);
        }

        private const int FrontLeftIndex = 0;
        private const int FrontRightIndex = 1;
        private const int RearLeftIndex = 2;
        private const int RearRightIndex = 3;

        private static bool ConfigureFallbackRig(ArcadeCarController car)
        {
            if (car == null)
                return false;

            Transform root = car.transform;
            string[] names =
            {
                "Wheel_FL",
                "Wheel_FR",
                "Wheel_RL",
                "Wheel_RR"
            };

            Transform[] wheelRoots = new Transform[4];
            Vector3[] wheelCenters = new Vector3[4];

            for (int i = 0; i < names.Length; i++)
            {
                Transform wheel = root.Find(names[i]);
                if (wheel == null)
                {
                    Debug.LogWarning(
                        $"Motor City: fallback wheel '{names[i]}' was not found, so the Prometeo rig could not be created.");
                    return false;
                }

                wheelRoots[i] = wheel;
                wheelCenters[i] = wheel.localPosition;
            }

            car.ConfigurePrometeoRig(
                wheelRoots,
                wheelCenters,
                0.36f);

            Debug.Log(
                "Motor City: using the built-in fallback wheel rig.");

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
            if (wheels == null ||
                wheels.Count < 4)
                return Array.Empty<Transform>();

            var positions =
                wheels
                    .Distinct()
                    .Select(
                        wheel => new
                        {
                            Wheel = wheel,
                            Local =
                                car.InverseTransformPoint(
                                    RendererBounds(wheel).center)
                        })
                    .OrderByDescending(item => item.Local.z)
                    .Take(4)
                    .ToArray();

            if (positions.Length < 4)
                return Array.Empty<Transform>();

            var front =
                positions
                    .Take(2)
                    .OrderBy(item => item.Local.x)
                    .ToArray();

            var rear =
                positions
                    .Skip(2)
                    .Take(2)
                    .OrderBy(item => item.Local.x)
                    .ToArray();

            return new[]
            {
                front[0].Wheel,
                front[1].Wheel,
                rear[0].Wheel,
                rear[1].Wheel
            };
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

        private static void AlignWheelbaseWithCarForward(
            Transform visual,
            Transform carRoot,
            List<Transform> wheels)
        {
            if (wheels == null ||
                wheels.Count < 4)
                return;

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;

            foreach (Transform wheel in wheels)
            {
                Vector3 center =
                    carRoot.InverseTransformPoint(
                        RendererBounds(wheel).center);

                minX = Mathf.Min(minX, center.x);
                maxX = Mathf.Max(maxX, center.x);
                minZ = Mathf.Min(minZ, center.z);
                maxZ = Mathf.Max(maxZ, center.z);
            }

            float xSpan = maxX - minX;
            float zSpan = maxZ - minZ;

            if (xSpan <= zSpan)
                return;

            visual.localRotation =
                visual.localRotation *
                Quaternion.Euler(0f, 90f, 0f);
        }

        private static void EnsureVisualNoseFacesPositiveZ(
            Transform visual,
            Transform carRoot,
            List<Transform> wheels)
        {
            if (visual == null ||
                carRoot == null)
                return;

            float frontZ = 0f;
            int frontCount = 0;
            float rearZ = 0f;
            int rearCount = 0;

            foreach (Transform item in
                     visual.GetComponentsInChildren<Transform>(true))
            {
                if (item == null ||
                    item == visual)
                    continue;

                string name =
                    item.name.ToLowerInvariant();

                Renderer renderer =
                    item.GetComponentInChildren<Renderer>(true);

                if (renderer == null)
                    continue;

                float z =
                    carRoot.InverseTransformPoint(
                        RendererBounds(item).center).z;

                if (LooksLikeFrontPart(name))
                {
                    frontZ += z;
                    frontCount++;
                }

                if (LooksLikeRearPart(name))
                {
                    rearZ += z;
                    rearCount++;
                }
            }

            // Wheel names are often more reliable than body-part names.
            if (wheels != null)
            {
                foreach (Transform wheel in wheels)
                {
                    if (wheel == null)
                        continue;

                    string name =
                        wheel.name.ToLowerInvariant();

                    float z =
                        carRoot.InverseTransformPoint(
                            RendererBounds(wheel).center).z;

                    if (LooksLikeFrontWheel(name))
                    {
                        frontZ += z * 2f;
                        frontCount += 2;
                    }

                    if (LooksLikeRearWheel(name))
                    {
                        rearZ += z * 2f;
                        rearCount += 2;
                    }
                }
            }

            bool shouldFlip;

            if (frontCount > 0 &&
                rearCount > 0)
            {
                float averageFront =
                    frontZ / frontCount;

                float averageRear =
                    rearZ / rearCount;

                shouldFlip =
                    averageFront <
                    averageRear;
            }
            else
            {
                Bounds bounds =
                    RendererBounds(visual);

                float centerZ =
                    carRoot.InverseTransformPoint(
                        bounds.center).z;

                if (frontCount > 0)
                {
                    shouldFlip =
                        frontZ / frontCount <
                        centerZ;
                }
                else if (rearCount > 0)
                {
                    shouldFlip =
                        rearZ / rearCount >
                        centerZ;
                }
                else
                {
                    // Known orientation of the current ARCADE Free Racing Car
                    // source: the visual nose is on local -Z.
                    shouldFlip =
                        true;
                }
            }

            if (shouldFlip)
            {
                visual.localRotation =
                    visual.localRotation *
                    Quaternion.Euler(
                        0f,
                        180f,
                        0f);
            }

            Debug.Log(
                "Motor City: ARCADE visual forward resolved without rotating " +
                $"the PlayerCar physics root. Visual flipped={shouldFlip}.");
        }

        private static bool LooksLikeFrontPart(
            string name)
        {
            return
                name.Contains("front") ||
                name.Contains("headlight") ||
                name.Contains("head_light") ||
                name.Contains("hood") ||
                name.Contains("bonnet");
        }

        private static bool LooksLikeRearPart(
            string name)
        {
            return
                name.Contains("rear") ||
                name.Contains("tail") ||
                name.Contains("back") ||
                name.Contains("spoiler") ||
                name.Contains("trunk") ||
                name.Contains("boot");
        }

        private static bool LooksLikeFrontWheel(
            string name)
        {
            return
                name.Contains("front") ||
                name.Contains("wheel_fl") ||
                name.Contains("wheel_fr") ||
                name.Contains("wheelfl") ||
                name.Contains("wheelfr") ||
                name.Contains("_fl") ||
                name.Contains("_fr");
        }

        private static bool LooksLikeRearWheel(
            string name)
        {
            return
                name.Contains("rear") ||
                name.Contains("back") ||
                name.Contains("wheel_rl") ||
                name.Contains("wheel_rr") ||
                name.Contains("wheelrl") ||
                name.Contains("wheelrr") ||
                name.Contains("_rl") ||
                name.Contains("_rr");
        }

        private static void CenterVisualHorizontally(
            Transform visual,
            Transform carRoot)
        {
            Bounds bounds =
                RendererBounds(visual);

            Vector3 centerLocal =
                carRoot.InverseTransformPoint(
                    bounds.center);

            visual.localPosition -=
                new Vector3(
                    centerLocal.x,
                    0f,
                    centerLocal.z);
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

        private static void StripImportedPhysics(
            GameObject visual)
        {
            foreach (WheelCollider wheel in
                     visual.GetComponentsInChildren<WheelCollider>(true))
            {
                wheel.enabled =
                    false;

                UnityEngine.Object.Destroy(
                    wheel);
            }

            foreach (Rigidbody rigidbody in
                     visual.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic =
                    true;

                rigidbody.detectCollisions =
                    false;

                UnityEngine.Object.Destroy(
                    rigidbody);
            }

            foreach (Collider collider in
                     visual.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null)
                    continue;

                collider.enabled =
                    false;

                UnityEngine.Object.Destroy(
                    collider);
            }
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
