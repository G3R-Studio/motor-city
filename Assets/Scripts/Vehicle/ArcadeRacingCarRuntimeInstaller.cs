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
        private const string RuntimeVisualName =
            "MotorCityVehicleVisual_Runtime";

        private static readonly HashSet<string> FallbackVisualNames = new()
        {
            "LowerBody", "UpperBody", "Cabin", "Hood", "FrontBumper", "RearBumper", "Spoiler",
            "HeadlightL", "HeadlightR", "TailLightL", "TailLightR",
            "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR",
            "Wheel_FL_Rim", "Wheel_FR_Rim", "Wheel_RL_Rim", "Wheel_RR_Rim"
        };

        // Runtime-created Materials are UnityEngine.Objects and are not reclaimed
        // by managed GC merely because an old vehicle visual is destroyed.
        // Keep one converted material per unique source material instead of
        // allocating a fresh copy on every vehicle switch.
        private static readonly Dictionary<Material, Material>
            RuntimeUrpMaterialCache = new();

        private static readonly Dictionary<Material, Material>
            RuntimeMirrorMaterialCache = new();

        private static Material runtimeNullMirrorMaterial;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeMaterialCaches()
        {
            RuntimeUrpMaterialCache.Clear();
            RuntimeMirrorMaterialCache.Clear();
            runtimeNullMirrorMaterial = null;
        }

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
            if (car == null)
                return false;

            if (car.transform.Find(RuntimeVisualName) != null ||
                car.transform.Find("ArcadeFreeRacingCarVisual_Runtime") != null)
                return true;

            GameObject prefab =
                Resources.Load<GameObject>(
                    "MotorCity/PlayerCarVisual");

            if (prefab == null)
                return ConfigureFallbackRig(car);

            return Install(
                car,
                prefab,
                false);
        }

        public static bool InstallVehicleVisual(
            ArcadeCarController car,
            string resourcePath,
            bool rotateLeft90 = false,
            float targetLength = TargetLength,
            bool flipYaw180 = false)
        {
            if (car == null ||
                string.IsNullOrWhiteSpace(
                    resourcePath))
                return false;

            GameObject prefab =
                Resources.Load<GameObject>(
                    resourcePath);

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Motor City: vehicle visual resource '{resourcePath}' was not found.");

                return false;
            }

            ClearRuntimeVisual(
                car.transform);

            return Install(
                car,
                prefab,
                rotateLeft90,
                targetLength,
                flipYaw180);
        }

        private static bool Install(
            ArcadeCarController car,
            GameObject prefab,
            bool rotateLeft90,
            float targetLength = TargetLength,
            bool flipYaw180 = false)
        {
            Transform carTransform = car.transform;

            GameObject visual = Instantiate(prefab, carTransform);
            visual.name = RuntimeVisualName;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            bool useAuthoredBusRig =
                flipYaw180;

            Transform[] authoredBusWheels =
                useAuthoredBusRig
                    ? FindAuthoredBusWheels(
                        visual.transform)
                    : Array.Empty<Transform>();

            Transform[] authoredSixWheelBusWheels =
                useAuthoredBusRig
                    ? FindAuthoredSixWheelBusWheels(
                        visual.transform)
                    : Array.Empty<Transform>();

            float authoredBusWheelRadius =
                useAuthoredBusRig
                    ? MeasureAuthoredBusWheelRadius(
                        visual.transform)
                    : 0f;

            if (useAuthoredBusRig)
            {
                StripImportedTrafficBehaviour(
                    visual);
            }

            StripImportedPhysics(visual);

            if (rotateLeft90)
            {
                NormalizeScaleOnly(
                    visual.transform,
                    targetLength);

                visual.transform.localRotation =
                    Quaternion.Euler(
                        0f,
                        -90f,
                        0f);
            }
            else if (useAuthoredBusRig)
            {
                // FCG buses are authored at the same real-world scale used by
                // city traffic. Preserve scale 1:1 so the player BusClimm has
                // exactly the same visual dimensions as traffic BusClimm.
                visual.transform.localRotation =
                    Quaternion.identity;
            }
            else
            {
                NormalizeHorizontalScaleAndRotation(
                    visual.transform,
                    targetLength);
            }

            UpgradeMaterialsForCurrentPipeline(visual);
            FixMirrorMaterialsForCurrentPipeline(visual);

            Transform[] authoredSixWheelPolyPackWheels =
                rotateLeft90
                    ? FindAuthoredSixWheelPolyPackWheels(
                        visual.transform)
                    : Array.Empty<Transform>();

            Transform[] authoredPolyPackWheels =
                rotateLeft90 &&
                authoredSixWheelPolyPackWheels.Length == 6
                    ? authoredSixWheelPolyPackWheels
                        .Take(4)
                        .ToArray()
                    : rotateLeft90
                        ? FindAuthoredPolyPackWheels(
                            visual.transform)
                        : Array.Empty<Transform>();

            Transform[] authoredSixWheelVisuals =
                authoredSixWheelBusWheels.Length == 6
                    ? authoredSixWheelBusWheels
                    : authoredSixWheelPolyPackWheels;

            List<Transform> wheelAnchors =
                useAuthoredBusRig &&
                authoredBusWheels.Length == 4
                    ? authoredBusWheels.ToList()
                    : rotateLeft90 &&
                      authoredPolyPackWheels.Length == 4
                        ? authoredPolyPackWheels.ToList()
                        : FindWheelAnchors(
                            visual.transform);

            if (wheelAnchors.Count < 4)
            {
                Debug.LogWarning(
                    "Motor City: selected vehicle does not expose four separable wheel meshes. " +
                    "The model cannot use the per-car animated wheel rig, so the fallback rig is used.");
                HideFallbackBodyOnly(carTransform);
                return ConfigureFallbackRig(car);
            }

            if (!rotateLeft90 &&
                !useAuthoredBusRig)
            {
                AlignWheelbaseWithCarForward(
                    visual.transform,
                    carTransform,
                    wheelAnchors);

                EnsureVisualNoseFacesPositiveZ(
                    visual.transform,
                    carTransform,
                    wheelAnchors);
            }

            CenterVisualHorizontally(
                visual.transform,
                carTransform);

            AlignBodyToWheelCenters(
                visual.transform,
                carTransform,
                wheelAnchors);

            // Re-evaluate normal cars after all visual transforms. For the
            // FCG bus keep the source FL/FR/BL/BR transforms explicitly; its
            // names do not contain "wheel" and geometric guessing is needless.
            if (!useAuthoredBusRig &&
                !(rotateLeft90 &&
                  authoredPolyPackWheels.Length == 4))
            {
                wheelAnchors =
                    FindWheelAnchors(
                        visual.transform);
            }

            Transform[] ordered =
                OrderWheels(
                    carTransform,
                    wheelAnchors);

            // All visual rigs are now in PlayerCar space before ordering.
            // PolyPack authored +X has already been rotated to +Z, while the
            // FCG bus is natively +Z, so Z-based axle ordering is reliable.

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
                radiusSum +=
                    MeasureWheelRadius(
                        bounds);

                spinRoots[i] = CreateWheelRoot(
                    carTransform,
                    $"ArcadeRacingWheelSpin_{i}",
                    centerWorld[i]);

                ordered[i].SetParent(spinRoots[i], true);
            }

            Transform[] additionalSpinRoots =
                Array.Empty<Transform>();

            Vector3[] additionalOffsetsLocal =
                Array.Empty<Vector3>();

            if (authoredSixWheelVisuals.Length == 6)
            {
                additionalSpinRoots =
                    new Transform[2];

                additionalOffsetsLocal =
                    new Vector3[2];

                for (int i = 0;
                     i < additionalSpinRoots.Length;
                     i++)
                {
                    Transform authoredWheel =
                        authoredSixWheelVisuals[4 + i];

                    Bounds bounds =
                        RendererBounds(
                            authoredWheel);

                    additionalSpinRoots[i] =
                        CreateWheelRoot(
                            carTransform,
                            $"ArcadeRacingWheelSpin_{4 + i}",
                            bounds.center);

                    int sourceWheelIndex =
                        i == 0
                            ? RearLeftIndex
                            : RearRightIndex;

                    additionalOffsetsLocal[i] =
                        carTransform.InverseTransformVector(
                            additionalSpinRoots[i].position -
                            spinRoots[sourceWheelIndex].position);

                    authoredWheel.SetParent(
                        additionalSpinRoots[i],
                        true);
                }
            }

            float measuredRadius;

            if (useAuthoredBusRig &&
                     authoredBusWheelRadius > 0.01f)
            {
                measuredRadius =
                    Mathf.Clamp(
                        authoredBusWheelRadius *
                        Mathf.Abs(
                            visual.transform.lossyScale.y),
                        0.26f,
                        0.58f);
            }
            else
            {
                measuredRadius =
                    radiusSum / 4f;
            }

            SymmetrizePhysicalWheelCenters(
                centerLocal);

            HidePrimitiveFallback(carTransform);

            BoxCollider chassis =
                carTransform.GetComponent<BoxCollider>();

            if (chassis != null)
            {
                if (rotateLeft90 ||
                    useAuthoredBusRig ||
                    targetLength >
                    TargetLength + 0.1f)
                {
                    ConfigureChassisFromVisual(
                        chassis,
                        carTransform,
                        visual.transform,
                        ordered,
                        measuredRadius,
                        targetLength);
                }

                chassis.enabled =
                    true;
            }

            bool needsExternalWheelSync =
                rotateLeft90 ||
                useAuthoredBusRig;

            car.ConfigurePrometeoRig(
                spinRoots,
                centerLocal,
                measuredRadius,
                needsExternalWheelSync);

            VehicleWheelVisualSync wheelSync =
                car.GetComponent<VehicleWheelVisualSync>();

            if (needsExternalWheelSync)
            {
                if (wheelSync == null)
                {
                    wheelSync =
                        car.gameObject.AddComponent<VehicleWheelVisualSync>();
                }

                wheelSync.Bind(
                    car,
                    spinRoots);

                if (additionalSpinRoots.Length == 2 &&
                    additionalOffsetsLocal.Length == 2)
                {
                    wheelSync.BindAdditionalVisual(
                        0,
                        additionalSpinRoots[0],
                        RearLeftIndex,
                        additionalOffsetsLocal[0]);

                    wheelSync.BindAdditionalVisual(
                        1,
                        additionalSpinRoots[1],
                        RearRightIndex,
                        additionalOffsetsLocal[1]);
                }
            }
            else if (wheelSync != null)
            {
                wheelSync.Clear();
            }

            return true;
        }

        private static void ClearRuntimeVisual(
            Transform carRoot)
        {
            if (carRoot == null)
                return;

            for (int i =
                     carRoot.childCount - 1;
                 i >= 0;
                 i--)
            {
                Transform child =
                    carRoot.GetChild(i);

                bool runtimeVisual =
                    child.name ==
                        RuntimeVisualName ||
                    child.name ==
                        "ArcadeFreeRacingCarVisual_Runtime";

                bool runtimeWheel =
                    child.name.StartsWith(
                        "ArcadeRacingWheelSpin_",
                        StringComparison.Ordinal);

                bool prometeoProxy =
                    child.name.StartsWith(
                        "PrometeoWheelProxy_",
                        StringComparison.Ordinal);

                if (!runtimeVisual &&
                    !runtimeWheel &&
                    !prometeoProxy)
                    continue;

                child.gameObject.SetActive(
                    false);

                UnityEngine.Object.Destroy(
                    child.gameObject);
            }
        }

        private static float MeasureWheelRadius(
            Bounds bounds)
        {
            float[] dimensions =
            {
                bounds.size.x,
                bounds.size.y,
                bounds.size.z
            };

            Array.Sort(
                dimensions);

            float diameter =
                dimensions[1];

            return Mathf.Clamp(
                diameter * 0.5f,
                0.26f,
                0.58f);
        }

        private static void ConfigureChassisFromVisual(
            BoxCollider chassis,
            Transform carRoot,
            Transform visualRoot,
            Transform[] wheels,
            float wheelRadius,
            float targetLength = TargetLength)
        {
            if (chassis == null ||
                carRoot == null ||
                visualRoot == null)
                return;

            Renderer[] renderers =
                visualRoot.GetComponentsInChildren<Renderer>(
                    true);

            bool hasBounds = false;
            Bounds localBounds =
                new(
                    Vector3.zero,
                    Vector3.zero);

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    IsWheelRenderer(
                        renderer.transform,
                        wheels))
                    continue;

                Bounds world =
                    renderer.bounds;

                Vector3 min =
                    world.min;

                Vector3 max =
                    world.max;

                for (int x = 0;
                     x < 2;
                     x++)
                {
                    for (int y = 0;
                         y < 2;
                         y++)
                    {
                        for (int z = 0;
                             z < 2;
                             z++)
                        {
                            Vector3 worldCorner =
                                new(
                                    x == 0
                                        ? min.x
                                        : max.x,
                                    y == 0
                                        ? min.y
                                        : max.y,
                                    z == 0
                                        ? min.z
                                        : max.z);

                            Vector3 local =
                                carRoot.InverseTransformPoint(
                                    worldCorner);

                            if (!hasBounds)
                            {
                                localBounds =
                                    new Bounds(
                                        local,
                                        Vector3.zero);

                                hasBounds =
                                    true;
                            }
                            else
                            {
                                localBounds.Encapsulate(
                                    local);
                            }
                        }
                    }
                }
            }

            if (!hasBounds)
                return;

            bool largeVehicle =
                targetLength >
                TargetLength + 0.1f;

            float width =
                largeVehicle
                    ? Mathf.Clamp(
                        localBounds.size.x * 0.94f,
                        1.8f,
                        2.75f)
                    : Mathf.Clamp(
                        localBounds.size.x * 0.88f,
                        1.35f,
                        2.35f);

            float length =
                largeVehicle
                    ? Mathf.Clamp(
                        localBounds.size.z * 0.96f,
                        targetLength * 0.82f,
                        targetLength * 1.02f)
                    : Mathf.Clamp(
                        localBounds.size.z * 0.88f,
                        2.7f,
                        4.75f);

            float bodyHeight =
                largeVehicle
                    ? Mathf.Clamp(
                        localBounds.size.y * 0.82f,
                        1.25f,
                        2.85f)
                    : Mathf.Clamp(
                        localBounds.size.y * 0.58f,
                        0.52f,
                        1.05f);

            float wheelBottom =
                TargetWheelCenterLocalY -
                wheelRadius;

            float desiredBottom =
                Mathf.Max(
                    wheelBottom + 0.055f,
                    localBounds.min.y + 0.025f);

            float desiredTop =
                Mathf.Min(
                    localBounds.max.y - 0.08f,
                    desiredBottom +
                    bodyHeight);

            if (desiredTop <=
                desiredBottom + 0.2f)
            {
                desiredTop =
                    desiredBottom +
                    bodyHeight;
            }

            chassis.size =
                new Vector3(
                    width,
                    desiredTop -
                    desiredBottom,
                    length);

            chassis.center =
                new Vector3(
                    localBounds.center.x,
                    (desiredBottom +
                     desiredTop) * 0.5f,
                    localBounds.center.z);
        }

        private static bool IsWheelRenderer(
            Transform candidate,
            Transform[] wheels)
        {
            if (candidate == null ||
                wheels == null)
                return false;

            foreach (Transform wheel in
                     wheels)
            {
                if (wheel == null)
                    continue;

                if (candidate == wheel ||
                    candidate.IsChildOf(
                        wheel))
                    return true;
            }

            return false;
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

        private static Transform[] FindAuthoredSixWheelPolyPackWheels(
            Transform root)
        {
            if (root == null)
                return Array.Empty<Transform>();

            // SuvV1 is a six-wheel PolyPack vehicle. Use the real authored
            // naming instead of geometric guessing:
            // FL/FR = steering axle, BL2/BR2 = physical rear axle,
            // BL1/BR1 = tandem visual axle that follows the rear colliders.
            string[] suffixes =
            {
                "_TireFL",
                "_TireFR",
                "_TireBL2",
                "_TireBR2",
                "_TireBL1",
                "_TireBR1"
            };

            Transform[] all =
                root.GetComponentsInChildren<Transform>(
                    true);

            Transform[] result =
                new Transform[suffixes.Length];

            for (int i = 0;
                 i < suffixes.Length;
                 i++)
            {
                result[i] =
                    all.FirstOrDefault(
                        item =>
                            item != null &&
                            item.name.EndsWith(
                                suffixes[i],
                                StringComparison.OrdinalIgnoreCase) &&
                            item.GetComponentInChildren<Renderer>(
                                true) != null);

                if (result[i] == null)
                    return Array.Empty<Transform>();
            }

            return result;
        }

        private static Transform[] FindAuthoredPolyPackWheels(
            Transform root)
        {
            if (root == null)
                return Array.Empty<Transform>();

            // Vehicles PolyPack 1.2 is authored lengthwise on local X.
            // Its deterministic suffixes identify the four visual wheels:
            // FL/FR are +X (front), BL/BR are -X (rear). The installer then
            // rotates the complete visual -90 degrees so authored +X maps to
            // Motor City's +Z forward axis.
            string[] suffixes =
            {
                "_TireFL",
                "_TireFR",
                "_TireBL",
                "_TireBR"
            };

            Transform[] all =
                root.GetComponentsInChildren<Transform>(
                    true);

            Transform[] result =
                new Transform[4];

            for (int i = 0;
                 i < suffixes.Length;
                 i++)
            {
                result[i] =
                    all.FirstOrDefault(
                        item =>
                            item != null &&
                            item.name.EndsWith(
                                suffixes[i],
                                StringComparison.OrdinalIgnoreCase) &&
                            item.GetComponentInChildren<Renderer>(
                                true) != null);

                if (result[i] == null)
                    return Array.Empty<Transform>();
            }

            return result;
        }

        private static Transform[] FindAuthoredBusWheels(
            Transform root)
        {
            if (root == null)
                return Array.Empty<Transform>();

            // Native FCG bus naming and axle contract:
            // FL/FR are the +Z front axle, BL/BR are the rearmost axle.
            string[] names =
            {
                "FL",
                "FR",
                "BL",
                "BR"
            };

            Transform[] result =
                new Transform[4];

            Transform[] all =
                root.GetComponentsInChildren<Transform>(
                    true);

            for (int i = 0;
                 i < names.Length;
                 i++)
            {
                result[i] =
                    all.FirstOrDefault(
                        item =>
                            item != null &&
                            string.Equals(
                                item.name,
                                names[i],
                                StringComparison.OrdinalIgnoreCase));

                if (result[i] == null ||
                    result[i].GetComponentInChildren<Renderer>(
                        true) == null)
                {
                    return Array.Empty<Transform>();
                }
            }

            return result;
        }

        private static Transform[] FindAuthoredSixWheelBusWheels(
            Transform root)
        {
            if (root == null)
                return Array.Empty<Transform>();

            // BusClimm uses a tandem rear axle. BL/BR remain the physical rear
            // axle while BL2/BR2 are synced as the additional visual axle.
            string[] names =
            {
                "FL",
                "FR",
                "BL",
                "BR",
                "BL2",
                "BR2"
            };

            Transform[] all =
                root.GetComponentsInChildren<Transform>(
                    true);

            Transform[] result =
                new Transform[names.Length];

            for (int i = 0;
                 i < names.Length;
                 i++)
            {
                result[i] =
                    all.FirstOrDefault(
                        item =>
                            item != null &&
                            string.Equals(
                                item.name,
                                names[i],
                                StringComparison.OrdinalIgnoreCase) &&
                            item.GetComponentInChildren<Renderer>(
                                true) != null);

                if (result[i] == null)
                    return Array.Empty<Transform>();
            }

            return result;
        }

        private static float MeasureAuthoredBusWheelRadius(
            Transform root)
        {
            if (root == null)
                return 0f;

            WheelCollider[] colliders =
                root.GetComponentsInChildren<WheelCollider>(
                    true);

            if (colliders == null ||
                colliders.Length < 4)
                return 0f;

            float sum = 0f;
            int count = 0;

            foreach (WheelCollider wheel in
                     colliders)
            {
                if (wheel == null ||
                    wheel.radius <= 0.01f)
                    continue;

                sum += wheel.radius;
                count++;
            }

            return count > 0
                ? sum / count
                : 0f;
        }

        private static List<Transform> FindWheelAnchors(
            Transform root)
        {
            Transform[] all =
                root.GetComponentsInChildren<Transform>(
                    true);

            var named =
                new List<Transform>();

            foreach (Transform item in all)
            {
                if (item == root)
                    continue;

                string name =
                    item.name.ToLowerInvariant();

                if (!(name.Contains("wheel") ||
                      name.Contains("tire") ||
                      name.Contains("tyre")))
                    continue;

                if (item.GetComponentInChildren<Renderer>(
                        true) == null)
                    continue;

                bool nested =
                    named.Any(
                        existing =>
                            item.IsChildOf(
                                existing));

                if (!nested)
                {
                    named.Add(
                        item);
                }
            }

            if (named.Count >= 4)
            {
                return SelectFourCornerWheels(
                    root,
                    named);
            }

            Renderer[] renderers =
                root.GetComponentsInChildren<Renderer>(
                    true);

            if (renderers.Length == 0)
                return named;

            Bounds fullBounds =
                renderers[0].bounds;

            for (int i = 1;
                 i < renderers.Length;
                 i++)
            {
                fullBounds.Encapsulate(
                    renderers[i].bounds);
            }

            Vector3 rootCenter =
                root.InverseTransformPoint(
                    fullBounds.center);

            Vector3 fullSize =
                fullBounds.size;

            float horizontalMax =
                Mathf.Max(
                    fullSize.x,
                    fullSize.z);

            var geometric =
                new List<Transform>();

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null)
                    continue;

                Transform item =
                    renderer.transform;

                Bounds bounds =
                    renderer.bounds;

                Vector3 local =
                    root.InverseTransformPoint(
                        bounds.center);

                Vector3 size =
                    bounds.size;

                bool low =
                    bounds.center.y <=
                    fullBounds.min.y +
                    fullBounds.size.y *
                    0.48f;

                bool small =
                    Mathf.Max(
                        size.x,
                        size.y,
                        size.z) <=
                    horizontalMax *
                    0.34f;

                bool awayFromCenter =
                    Mathf.Abs(
                        local.x -
                        rootCenter.x) >=
                        fullSize.x *
                        0.18f ||
                    Mathf.Abs(
                        local.z -
                        rootCenter.z) >=
                        fullSize.z *
                        0.18f;

                if (!low ||
                    !small ||
                    !awayFromCenter)
                    continue;

                if (!geometric.Contains(
                        item))
                {
                    geometric.Add(
                        item);
                }
            }

            return SelectFourCornerWheels(
                root,
                geometric);
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

        private static void NormalizeScaleOnly(
            Transform visual,
            float targetLength)
        {
            Bounds bounds =
                RendererBounds(
                    visual);

            float length =
                Mathf.Max(
                    bounds.size.x,
                    bounds.size.z);

            if (length < 0.01f)
                return;

            visual.localScale *=
                Mathf.Max(
                    1f,
                    targetLength) /
                length;
        }

        private static void NormalizeHorizontalScaleAndRotation(
            Transform visual,
            float targetLength)
        {
            Bounds bounds = RendererBounds(visual);

            if (bounds.size.x > bounds.size.z * 1.12f)
            {
                visual.localRotation = Quaternion.Euler(0f, 90f, 0f);
                bounds = RendererBounds(visual);
            }

            float length = Mathf.Max(bounds.size.x, bounds.size.z);
            if (length < 0.01f) return;

            visual.localScale *=
                Mathf.Max(
                    1f,
                    targetLength) /
                length;
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

            // Successful visual alignment is routine and intentionally silent.
            // Warnings/errors in the installer remain visible in Console.
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

        private static void StripImportedTrafficBehaviour(
            GameObject visual)
        {
            if (visual == null)
                return;

            foreach (MonoBehaviour behaviour in
                     visual.GetComponentsInChildren<MonoBehaviour>(
                         true))
            {
                if (behaviour == null)
                    continue;

                Type type =
                    behaviour.GetType();

                // FCG player visuals must not keep the TrafficCar AI running
                // inside the Motor City player vehicle. Match by type name so
                // this runtime code does not depend on the third-party class at
                // compile time.
                if (string.Equals(
                        type.Name,
                        "TrafficCar",
                        StringComparison.Ordinal))
                {
                    behaviour.enabled =
                        false;

                    UnityEngine.Object.Destroy(
                        behaviour);
                }
            }
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

                    if (RuntimeUrpMaterialCache.TryGetValue(
                            old,
                            out Material cached) &&
                        cached != null)
                    {
                        upgraded[i] = cached;
                        continue;
                    }

                    Texture baseTexture =
                        old.HasProperty("_MainTex")
                            ? old.GetTexture("_MainTex")
                            : null;

                    Texture emissionTexture =
                        old.HasProperty("_EmissionMap")
                            ? old.GetTexture("_EmissionMap")
                            : null;

                    Color oldColor =
                        old.HasProperty("_Color")
                            ? old.GetColor("_Color")
                            : Color.white;

                    Color emissionColor =
                        old.HasProperty("_EmissionColor")
                            ? old.GetColor("_EmissionColor")
                            : Color.black;

                    bool hadEmission =
                        old.IsKeywordEnabled("_EMISSION") ||
                        emissionTexture != null ||
                        emissionColor.maxColorComponent > 0.001f;

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
                        enableInstancing = true,
                        hideFlags = HideFlags.DontSave
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

                    // Preserve the imported model's real emissive lamp
                    // submeshes/materials when converting Standard -> URP.
                    // Previously this conversion discarded _EmissionMap and
                    // _EmissionColor, which is why the actual taillights went
                    // dark and a fake projected overlay had been added later.
                    if (hadEmission)
                    {
                        if (emissionTexture == null &&
                            baseTexture != null &&
                            (old.name.IndexOf(
                                 "emission",
                                 StringComparison.OrdinalIgnoreCase) >= 0 ||
                             old.name.IndexOf(
                                 "light",
                                 StringComparison.OrdinalIgnoreCase) >= 0 ||
                             old.name.IndexOf(
                                 "lamp",
                                 StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            emissionTexture =
                                baseTexture;
                        }

                        if (emissionTexture != null &&
                            material.HasProperty("_EmissionMap"))
                        {
                            material.SetTexture(
                                "_EmissionMap",
                                emissionTexture);
                        }

                        if (material.HasProperty("_EmissionColor"))
                        {
                            material.SetColor(
                                "_EmissionColor",
                                emissionColor.maxColorComponent > 0.001f
                                    ? emissionColor
                                    : Color.white);
                        }

                        material.EnableKeyword(
                            "_EMISSION");
                    }

                    RuntimeUrpMaterialCache[old] =
                        material;

                    upgraded[i] = material;
                }

                renderer.sharedMaterials = upgraded;
            }
        }

        private static void FixMirrorMaterialsForCurrentPipeline(
            GameObject root)
        {
            if (root == null ||
                GraphicsSettings.currentRenderPipeline == null)
            {
                return;
            }

            Shader urpLit =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            if (urpLit == null)
                return;

            foreach (Renderer renderer in
                     root.GetComponentsInChildren<Renderer>(
                         true))
            {
                if (renderer == null ||
                    !LooksLikeMirrorPart(
                        renderer.transform,
                        root.transform))
                {
                    continue;
                }

                Material[] source =
                    renderer.sharedMaterials;

                if (source == null ||
                    source.Length == 0)
                {
                    continue;
                }

                Material[] fixedMaterials =
                    new Material[source.Length];

                for (int materialIndex = 0;
                     materialIndex < source.Length;
                     materialIndex++)
                {
                    Material old =
                        source[materialIndex];

                    fixedMaterials[
                        materialIndex] =
                        GetOrCreateMirrorMaterial(
                            urpLit,
                            old);
                }

                renderer.sharedMaterials =
                    fixedMaterials;
            }
        }

        private static Material GetOrCreateMirrorMaterial(
            Shader urpLit,
            Material source)
        {
            if (source != null &&
                RuntimeMirrorMaterialCache.TryGetValue(
                    source,
                    out Material cached) &&
                cached != null)
            {
                return cached;
            }

            if (source == null &&
                runtimeNullMirrorMaterial != null)
            {
                return
                    runtimeNullMirrorMaterial;
            }

            Material material =
                new(urpLit)
                {
                    name =
                        (source != null
                            ? source.name
                            : "Mirror") +
                        "_MotorCityMirrorURP",
                    enableInstancing = true,
                    hideFlags = HideFlags.DontSave
                };

            // Do not reuse the imported emissive/magenta atlas on mirrors.
            // Some source packs share lamp/emission materials with mirror glass.
            if (material.HasProperty(
                    "_BaseMap"))
            {
                material.SetTexture(
                    "_BaseMap",
                    null);
            }

            if (material.HasProperty(
                    "_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(
                        0.075f,
                        0.095f,
                        0.12f,
                        1f));
            }

            if (material.HasProperty(
                    "_Metallic"))
            {
                material.SetFloat(
                    "_Metallic",
                    0.55f);
            }

            if (material.HasProperty(
                    "_Smoothness"))
            {
                material.SetFloat(
                    "_Smoothness",
                    0.82f);
            }

            if (material.HasProperty(
                    "_EmissionColor"))
            {
                material.SetColor(
                    "_EmissionColor",
                    Color.black);
            }

            material.DisableKeyword(
                "_EMISSION");

            if (source != null)
            {
                RuntimeMirrorMaterialCache[source] =
                    material;
            }
            else
            {
                runtimeNullMirrorMaterial =
                    material;
            }

            return
                material;
        }

        private static bool LooksLikeMirrorPart(
            Transform item,
            Transform stopAt)
        {
            Transform cursor =
                item;

            while (cursor != null &&
                   cursor != stopAt)
            {
                string name =
                    cursor.name
                        .ToLowerInvariant();

                if (name.Contains("mirror") ||
                    name.Contains("rearview") ||
                    name.Contains("rear_view") ||
                    name.Contains("sideview") ||
                    name.Contains("side_view"))
                {
                    return true;
                }

                cursor =
                    cursor.parent;
            }

            return false;
        }

        private static void HidePrimitiveFallback(Transform car)
        {
            foreach (Renderer renderer in
                     car.GetComponentsInChildren<Renderer>(true))
            {
                if (IsPrimitiveFallbackRenderer(
                        car,
                        renderer.transform))
                {
                    renderer.enabled =
                        false;
                }
            }
        }

        private static void HideFallbackBodyOnly(Transform car)
        {
            foreach (Renderer renderer in
                     car.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsPrimitiveFallbackRenderer(
                        car,
                        renderer.transform))
                {
                    continue;
                }

                Transform root =
                    RootChildUnder(
                        car,
                        renderer.transform);

                if (root != null &&
                    root.name.StartsWith(
                        "Wheel_",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                renderer.enabled =
                    false;
            }
        }

        private static bool IsPrimitiveFallbackRenderer(
            Transform car,
            Transform item)
        {
            Transform root =
                RootChildUnder(
                    car,
                    item);

            return
                root != null &&
                FallbackVisualNames.Contains(
                    root.name);
        }

        private static Transform RootChildUnder(
            Transform root,
            Transform item)
        {
            if (root == null ||
                item == null ||
                item == root)
            {
                return null;
            }

            Transform cursor =
                item;

            while (cursor.parent != null &&
                   cursor.parent != root)
            {
                cursor =
                    cursor.parent;
            }

            return cursor.parent == root
                ? cursor
                : null;
        }
    }
}
