using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MotorCity.World
{
    public static class CityAssetRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/CityVisual";

        private const string GarageResourcePath =
            "MotorCity/Garage/SimpleGarage";

        private const string RuntimeGarageName =
            "MotorCity_SimpleGarage";

        private const string SceneCityName =
            "City-Maker";

        private const string RuntimeCityName =
            "MotorCity_FCGCity";

        private const float MarkerLift =
            0.05f;

        // These targets follow the actual generated FCG grid from the
        // 2026-09-18 city report. Every road target is validated against
        // the exact MeshCollider triangle/submesh using the FCG_Roads material.
        private static readonly Vector3[] DeliveryPreferred =
        {
            // Large-district delivery loop from the 2026-09-18 17:09 FCG
            // workbench generation. Targets intentionally follow the visible
            // city grid from the report/screenshot; each is then snapped to
            // the exact FCG_Roads triangle.
            new(-450f, 0f, -100f),
            new(-150f, 0f, -100f),
            new(150f, 0f, -100f),
            new(450f, 0f, -100f),
            new(450f, 0f, 150f),
            new(450f, 0f, 360f),
            new(150f, 0f, 450f),
            new(-150f, 0f, 450f),
            new(-450f, 0f, 360f),
            new(-450f, 0f, 150f),
            new(-150f, 0f, 150f),
            new(150f, 0f, 150f)
        };

        private static readonly Vector3[] SprintPreferred =
        {
            // Street sprint stays entirely inside the verified main district.
            // The previous route continued onto an old remote highway section
            // that is outside the current playable city.
            new(450f, 0f, 150f),
            new(450f, 0f, -100f),
            new(150f, 0f, -100f),
            new(-150f, 0f, -100f),
            new(-450f, 0f, -100f),
            new(-450f, 0f, 150f),
            new(-450f, 0f, 360f),
            new(-150f, 0f, 450f),
            new(150f, 0f, 450f),
            new(450f, 0f, 430f),
            new(450f, 0f, 150f),
            new(150f, 0f, 150f),
            new(-150f, 0f, 150f),
            new(-450f, 0f, 150f),
            new(-150f, 0f, -100f)
        };

        private static readonly Vector3[] CircuitPreferred =
        {
            // Two-lap loop around the large district. As with the other
            // activities, every target is snapped to an exact FCG road triangle.
            new(150f, 0f, -100f),
            new(450f, 0f, -100f),
            new(450f, 0f, 150f),
            new(450f, 0f, 430f),
            new(150f, 0f, 450f),
            new(-150f, 0f, 450f),
            new(-450f, 0f, 360f),
            new(-450f, 0f, 150f),
            new(-450f, 0f, -100f),
            new(-150f, 0f, -100f)
        };

        private static Vector3[] deliveryRoute =
            (Vector3[])DeliveryPreferred.Clone();

        private static Vector3[] sprintRoute =
            (Vector3[])SprintPreferred.Clone();

        private static Vector3[] circuitRoute =
            (Vector3[])CircuitPreferred.Clone();

        private static readonly Vector3[] UndergroundPreferred =
        {
            // Underground must stay inside the verified large-district road
            // network. These points are shared with the working delivery /
            // circuit area and are therefore guaranteed to be on-map for the
            // current authored city.
            new(-450f, 0f, 360f),
            new(-450f, 0f, 150f),
            new(-450f, 0f, -100f),
            new(-150f, 0f, -100f),
            new(150f, 0f, -100f),
            new(450f, 0f, -100f),
            new(450f, 0f, 150f),
            new(450f, 0f, 360f),
            new(150f, 0f, 450f),
            new(-150f, 0f, 450f)
        };

        private static Vector3[] undergroundRoute =
            (Vector3[])UndergroundPreferred.Clone();

        private static GameObject activeCity;
        private static GameObject activeGarageInterior;
        private static GameObject garagePresentationLighting;
        private static GameObject garagePresentationPostFx;
        private static VolumeProfile garagePresentationVolumeProfile;

        private static readonly int GarageBaseColorId =
            Shader.PropertyToID("_BaseColor");

        private static readonly int GarageColorId =
            Shader.PropertyToID("_Color");

        private static readonly int GarageEmissionColorId =
            Shader.PropertyToID("_EmissionColor");

        private static Bounds cityBounds;
        private static bool hasCityBounds;

        private static readonly List<ReflectionProbe> cityReflectionProbes =
            new();

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            if (garagePresentationVolumeProfile != null)
            {
                UnityEngine.Object.Destroy(
                    garagePresentationVolumeProfile);
            }

            activeCity =
                null;

            activeGarageInterior =
                null;

            garagePresentationLighting =
                null;

            garagePresentationPostFx =
                null;

            garagePresentationVolumeProfile =
                null;

            cityBounds =
                default;

            hasCityBounds =
                false;

            cityReflectionProbes.Clear();

            deliveryRoute =
                (Vector3[])DeliveryPreferred.Clone();

            sprintRoute =
                (Vector3[])SprintPreferred.Clone();

            circuitRoute =
                (Vector3[])CircuitPreferred.Clone();

            undergroundRoute =
                (Vector3[])UndergroundPreferred.Clone();

            PlayerSpawnPoint =
                new Vector3(
                    -570f,
                    0.25f,
                    505.109f);

            PlayerSpawnRotation =
                Quaternion.Euler(
                    0f,
                    90f,
                    0f);

            GaragePoint =
                new Vector3(
                    -421.68866f,
                    0.182654113f,
                    330.916077f);

            GarageSpawnRotation =
                Quaternion.Euler(
                    7.40436444e-05f,
                    89.9998322f,
                    -4.8625111e-06f);

            DriftChallengePoint =
                new Vector3(
                    -450f,
                    0.2f,
                    150f);
        }

        public static Vector3 PlayerSpawnPoint { get; private set; } =
            new(-570f, 0.25f, 505.109f);

        public static Quaternion PlayerSpawnRotation { get; private set; } =
            Quaternion.Euler(
                0f,
                90f,
                0f);

        // Dedicated player garage on the authored parking apron.
        public static Vector3 GaragePoint { get; private set; } =
            new(
                -421.68866f,
                0.182654113f,
                330.916077f);

        public static Quaternion GarageSpawnRotation { get; private set; } =
            Quaternion.Euler(
                7.40436444e-05f,
                89.9998322f,
                -4.8625111e-06f);

        public static Vector3 GarageInteriorPosition { get; } =
            new(
                -552.546936f,
                1.375f,
                -798.200012f);

        public static Quaternion GarageInteriorRotation { get; } =
            Quaternion.Euler(
                0f,
                0f,
                0f);

        public static Vector3 GarageInteriorScale { get; } =
            new(
                2f,
                2f,
                2f);

        public static Vector3 GarageVehiclePosition { get; } =
            new(
                -421.68866f,
                0.182654113f,
                330.916077f);

        public static Quaternion GarageVehicleRotation { get; } =
            Quaternion.Euler(
                0f,
                142.676514f,
                0f);

        public static Vector3 GarageCameraPosition { get; } =
            new(
                -549.252f,
                3.25f,
                -792.25f);

        public static Quaternion GarageCameraRotation { get; } =
            Quaternion.Euler(
                11.5f,
                224.75f,
                0f);

        // Western broad junction in the large district, kept separate from
        // the street sprint start on the eastern side.
        public static Vector3 DriftChallengePoint { get; private set; } =
            new(-450f, 0.2f, 150f);

        public static Vector3[] DeliveryRoute =>
            (Vector3[])deliveryRoute.Clone();

        public static Vector3[] SprintRoute =>
            (Vector3[])sprintRoute.Clone();

        public static Vector3[] CircuitRoute =>
            (Vector3[])circuitRoute.Clone();

        public static Vector3 UndergroundMeetingPoint =>
            undergroundRoute != null &&
            undergroundRoute.Length > 0
                ? undergroundRoute[0]
                : PlayerSpawnPoint;

        public static Vector3[] UndergroundRoute =>
            (Vector3[])undergroundRoute.Clone();

        public static bool TryInstall()
        {
            activeCity =
                FindExistingCity();

            if (activeCity == null)
            {
                GameObject prefab =
                    Resources.Load<GameObject>(
                        ResourcePath);

                if (prefab == null)
                    return false;

                activeCity =
                    UnityEngine.Object.Instantiate(
                        prefab);

                activeCity.name =
                    RuntimeCityName;
            }

            RebindRuntimeCityMaterials();

            // Runtime treats the authored city as read-only.
            // Colliders, props, parked vehicles, traffic signals and all
            // other map objects must come exactly from CityVisual.prefab.
            cityBounds =
                CalculateCityBounds(
                    activeCity);

            hasCityBounds =
                cityBounds.size.x > 10f &&
                cityBounds.size.z > 10f;

            InstallGarageInterior();

            Physics.SyncTransforms();

            ResolveGameplayLayout();
            InstallCityReflectionProbes();

            return true;
        }

        private static void RebindRuntimeCityMaterials()
        {
            if (activeCity == null)
                return;

            Material[] generated =
                Resources.LoadAll<Material>(
                    "MotorCity/Environment/FCGMaterials");

            if (generated == null ||
                generated.Length == 0)
            {
                Debug.LogWarning(
                    "Motor City: no generated FCG materials were found in Resources.");
                return;
            }

            var materialMap =
                new Dictionary<string, Material>(
                    StringComparer.OrdinalIgnoreCase);

            for (int i = 0;
                 i < generated.Length;
                 i++)
            {
                Material material =
                    generated[i];

                if (material == null)
                    continue;

                string key =
                    RuntimeMaterialKey(
                        material.name);

                if (string.IsNullOrWhiteSpace(
                        key))
                {
                    continue;
                }

                if (!materialMap.TryGetValue(
                        key,
                        out Material existing) ||
                    RuntimeMaterialPriority(
                        material.name) >
                    RuntimeMaterialPriority(
                        existing != null
                            ? existing.name
                            : string.Empty))
                {
                    materialMap[key] =
                        material;
                }
            }

            int replaced =
                0;

            int dynamicallyConvertedGlass =
                0;

            var dynamicGlassMap =
                new Dictionary<Material, Material>();

            var unresolvedGlass =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            Renderer[] renderers =
                activeCity.GetComponentsInChildren<Renderer>(
                    true);

            for (int r = 0;
                 r < renderers.Length;
                 r++)
            {
                Renderer renderer =
                    renderers[r];

                if (renderer == null)
                    continue;

                Material[] materials =
                    renderer.sharedMaterials;

                bool changed =
                    false;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material current =
                        materials[i];

                    if (current == null)
                        continue;

                    string key =
                        RuntimeMaterialKey(
                            current.name);

                    if (string.IsNullOrWhiteSpace(
                            key))
                    {
                        continue;
                    }

                    bool architecturalGlass =
                        FcgRuntimeGlassMaterialFactory
                            .IsArchitecturalGlassKey(
                                key);

                    if (!materialMap.TryGetValue(
                            key,
                            out Material replacement) ||
                        replacement == null)
                    {
                        if (architecturalGlass)
                        {
                            if (!dynamicGlassMap.TryGetValue(
                                    current,
                                    out replacement) ||
                                replacement == null)
                            {
                                replacement =
                                    FcgRuntimeGlassMaterialFactory
                                        .Create(
                                            current,
                                            key);

                                if (replacement != null)
                                {
                                    dynamicGlassMap[current] =
                                        replacement;

                                    dynamicallyConvertedGlass++;
                                }
                            }

                            if (replacement == null)
                            {
                                unresolvedGlass.Add(
                                    current.name);
                            }
                        }

                        if (replacement == null)
                            continue;
                    }

                    if (architecturalGlass)
                        FcgRuntimeGlassMaterialFactory.ConfigureReflections(replacement);

                    if (replacement == current)
                        continue;

                    materials[i] =
                        replacement;

                    replaced++;
                    changed =
                        true;
                }

                if (changed)
                {
                    renderer.sharedMaterials =
                        materials;
                }

                // The generated city should participate in the reflection
                // probes unless an individual renderer explicitly opts out
                // later for a special-purpose effect.
                renderer.reflectionProbeUsage =
                    ReflectionProbeUsage.BlendProbes;
            }

            Debug.Log(
                "Motor City: rebound " +
                replaced +
                " runtime FCG material slots; dynamically converted " +
                dynamicallyConvertedGlass +
                " unresolved architectural glass materials.");

            if (unresolvedGlass.Count > 0)
            {
                Debug.LogWarning(
                    "Motor City: unresolved runtime glass materials: " +
                    string.Join(
                        ", ",
                        unresolvedGlass));
            }
        }

        private static int RuntimeMaterialPriority(
            string materialName)
        {
            if (string.IsNullOrWhiteSpace(
                    materialName))
            {
                return 0;
            }

            string value =
                materialName.ToLowerInvariant();

            // The runtime NightEmissive shader already performs the day/night
            // transition. Legacy FCG night/DN materials must never win a
            // duplicate canonical key such as Wins vs Wins-Night.
            if (value.Contains(
                    "night",
                    StringComparison.OrdinalIgnoreCase) ||
                value.Contains(
                    "-dn",
                    StringComparison.OrdinalIgnoreCase) ||
                value.Contains(
                    "_dn",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 10;
            }

            return 100;
        }

        private static string RuntimeMaterialKey(
            string materialName)
        {
            if (string.IsNullOrWhiteSpace(
                    materialName))
            {
                return string.Empty;
            }

            string value =
                materialName
                    .Replace(
                        "(Instance)",
                        string.Empty,
                        StringComparison.OrdinalIgnoreCase)
                    .Trim();

            if (value.StartsWith(
                    "FCG_",
                    StringComparison.OrdinalIgnoreCase))
            {
                value =
                    value.Substring(
                        4);
            }

            int separator =
                value.LastIndexOf(
                    '_');

            if (separator >= 0 &&
                separator <
                value.Length - 1)
            {
                string suffix =
                    value.Substring(
                        separator + 1);

                if (suffix.Length == 8)
                {
                    bool hexadecimal =
                        true;

                    for (int i = 0;
                         i < suffix.Length;
                         i++)
                    {
                        char character =
                            suffix[i];

                        if (!Uri.IsHexDigit(
                                character))
                        {
                            hexadecimal =
                                false;
                            break;
                        }
                    }

                    if (hexadecimal)
                    {
                        value =
                            value.Substring(
                                0,
                                separator);
                    }
                }
            }

            var buffer =
                new System.Text.StringBuilder(
                    value.Length);

            for (int i = 0;
                 i < value.Length;
                 i++)
            {
                char character =
                    char.ToLowerInvariant(
                        value[i]);

                if (char.IsLetterOrDigit(
                        character))
                {
                    buffer.Append(
                        character);
                }
            }

            string key =
                buffer.ToString();

            return
                key switch
                {
                    "winsnight" =>
                        "wins",
                    "wins02night" =>
                        "wins02",
                    "winglass01night" =>
                        "winglass01",
                    "winglass01dn" =>
                        "winglass01d",
                    "winglass03night" =>
                        "winglass03",
                    "winglass04night" =>
                        "winglass04",
                    _ =>
                        key
                };
        }

        private static void InstallCityReflectionProbes()
        {
            if (activeCity == null ||
                !hasCityBounds)
            {
                return;
            }

            for (int i = 0;
                 i < cityReflectionProbes.Count;
                 i++)
            {
                ReflectionProbe oldProbe =
                    cityReflectionProbes[i];

                if (oldProbe != null)
                {
                    UnityEngine.Object.Destroy(
                        oldProbe.gameObject);
                }
            }

            cityReflectionProbes.Clear();

            int gridSize =
                CurrentCityReflectionProbeGridSize();

            float cellSizeX =
                cityBounds.size.x /
                gridSize;

            float cellSizeZ =
                cityBounds.size.z /
                gridSize;

            Vector3 probeSize =
                new(
                    Mathf.Max(
                        100f,
                        cellSizeX * 1.42f),
                    Mathf.Max(
                        120f,
                        cityBounds.size.y * 1.12f),
                    Mathf.Max(
                        100f,
                        cellSizeZ * 1.42f));

            int probeIndex =
                0;

            for (int z = 0;
                 z < gridSize;
                 z++)
            {
                for (int x = 0;
                     x < gridSize;
                     x++)
                {
                    float normalizedX =
                        (x + 0.5f) /
                        gridSize;

                    float normalizedZ =
                        (z + 0.5f) /
                        gridSize;

                    Vector3 position =
                        new(
                            Mathf.Lerp(
                                cityBounds.min.x,
                                cityBounds.max.x,
                                normalizedX),
                            cityBounds.center.y,
                            Mathf.Lerp(
                                cityBounds.min.z,
                                cityBounds.max.z,
                                normalizedZ));

                    GameObject probeObject =
                        new(
                            "Motor City Reflection Probe " +
                            (++probeIndex));

                    probeObject.transform.SetParent(
                        activeCity.transform,
                        true);

                    probeObject.transform.position =
                        position;

                    ReflectionProbe probe =
                        probeObject.AddComponent<ReflectionProbe>();

                    probe.mode =
                        ReflectionProbeMode.Realtime;

                    probe.refreshMode =
                        ReflectionProbeRefreshMode.ViaScripting;

                    probe.timeSlicingMode =
                        ReflectionProbeTimeSlicingMode.IndividualFaces;

                    probe.resolution =
                        CurrentCityReflectionProbeResolution();

                    probe.size =
                        probeSize;

                    probe.center =
                        Vector3.zero;

                    probe.nearClipPlane =
                        0.5f;

                    probe.farClipPlane =
                        Mathf.Max(
                            probeSize.x,
                            Mathf.Max(
                                probeSize.y,
                                probeSize.z)) *
                        0.78f;

                    probe.intensity =
                        1.0f;

                    probe.blendDistance =
                        Mathf.Min(
                            probeSize.x,
                            probeSize.z) *
                        0.22f;

                    probe.importance =
                        2;

                    probe.hdr =
                        true;

                    probe.boxProjection =
                        true;

                    probe.cullingMask =
                        ~0;

                    probe.clearFlags =
                        ReflectionProbeClearFlags.Skybox;

                    cityReflectionProbes.Add(
                        probe);
                }
            }

            // Local probes contain much more useful architectural detail than
            // four huge city-wide cubemaps. Capture them once after creation;
            // subsequent refreshes happen only for sky/quality changes.
            RefreshCityReflectionProbes();
        }

        private static int CurrentCityReflectionProbeGridSize()
        {
            return
                MotorCity.Platform.MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCity.Platform.MotorCityQualityPreset.High =>
                        4,
                    MotorCity.Platform.MotorCityQualityPreset.Medium =>
                        3,
                    _ =>
                        2
                };
        }

        private static int CurrentCityReflectionProbeResolution()
        {
            return
                MotorCity.Platform.MotorCityQualityRuntime.CurrentPreset switch
                {
                    MotorCity.Platform.MotorCityQualityPreset.High =>
                        256,
                    MotorCity.Platform.MotorCityQualityPreset.Medium =>
                        128,
                    _ =>
                        64
                };
        }

        public static void RefreshCityReflectionProbes()
        {
            int expectedProbeCount =
                CurrentCityReflectionProbeGridSize();

            expectedProbeCount *=
                expectedProbeCount;

            if (activeCity != null &&
                hasCityBounds &&
                cityReflectionProbes.Count !=
                    expectedProbeCount)
            {
                InstallCityReflectionProbes();
                return;
            }

            for (int i = 0;
                 i < cityReflectionProbes.Count;
                 i++)
            {
                ReflectionProbe probe =
                    cityReflectionProbes[i];

                if (probe == null ||
                    !probe.isActiveAndEnabled)
                {
                    continue;
                }

                int targetResolution =
                    CurrentCityReflectionProbeResolution();

                if (probe.resolution !=
                    targetResolution)
                {
                    probe.resolution =
                        targetResolution;
                }
            }

            if (activeCity == null)
                return;

            CityReflectionProbeCaptureRunner runner =
                activeCity.GetComponent<CityReflectionProbeCaptureRunner>();

            if (runner == null)
            {
                runner =
                    activeCity.AddComponent<CityReflectionProbeCaptureRunner>();
            }

            runner.Capture(
                cityReflectionProbes);
        }

        private static void InstallGarageInterior()
        {
            if (activeGarageInterior == null)
            {
                activeGarageInterior =
                    GameObject.Find(
                        RuntimeGarageName);
            }

            if (activeGarageInterior == null)
            {
                GameObject prefab =
                    Resources.Load<GameObject>(
                        GarageResourcePath);

                if (prefab == null)
                {
                    Debug.LogWarning(
                        "Motor City: Simple Garage runtime prefab was not found.");
                    return;
                }

                activeGarageInterior =
                    UnityEngine.Object.Instantiate(
                        prefab);

                activeGarageInterior.name =
                    RuntimeGarageName;
            }

            Transform garageTransform =
                activeGarageInterior.transform;

            garageTransform.position =
                GarageInteriorPosition;

            garageTransform.rotation =
                GarageInteriorRotation;

            garageTransform.localScale =
                GarageInteriorScale;

            Transform floor =
                FindChildByName(
                    garageTransform,
                    "Floor");

            if (floor == null)
            {
                Debug.LogWarning(
                    "Motor City: Simple Garage Floor was not found.");
                return;
            }

            BoxCollider floorCollider =
                floor.GetComponent<BoxCollider>();

            if (floorCollider == null)
            {
                floorCollider =
                    floor.gameObject.AddComponent<BoxCollider>();
            }

            floorCollider.center =
                new Vector3(
                    0f,
                    0f,
                    -0.313701093f);

            floorCollider.size =
                new Vector3(
                    2f,
                    0f,
                    2.11779308f);

            floorCollider.isTrigger =
                false;

            Transform walls =
                FindChildByName(
                    garageTransform,
                    "Walls");

            if (walls != null)
            {
                BoxCollider[] wallColliders =
                    walls.GetComponents<BoxCollider>();

                while (wallColliders.Length < 3)
                {
                    walls.gameObject.AddComponent<BoxCollider>();

                    wallColliders =
                        walls.GetComponents<BoxCollider>();
                }

                wallColliders[0].center =
                    new Vector3(
                        1.71424532f,
                        -0.065721035f,
                        2.14011145f);

                wallColliders[0].size =
                    new Vector3(
                        0f,
                        2.42896795f,
                        6.04566765f);

                wallColliders[1].center =
                    new Vector3(
                        -1.06211495f,
                        -0.065721035f,
                        -0.882722378f);

                wallColliders[1].size =
                    new Vector3(
                        5.55272055f,
                        2.42896795f,
                        0f);

                wallColliders[2].center =
                    new Vector3(
                        -3.83847523f,
                        -0.065721035f,
                        2.14011145f);

                wallColliders[2].size =
                    new Vector3(
                        0f,
                        2.42896795f,
                        6.04566765f);

                for (int i = 0;
                     i < wallColliders.Length;
                     i++)
                {
                    wallColliders[i].isTrigger =
                        false;

                    wallColliders[i].enabled =
                        i < 3;
                }
            }

            Transform ceiling =
                FindChildByName(
                    garageTransform,
                    "Ceiling");

            if (ceiling != null)
            {
                BoxCollider ceilingCollider =
                    ceiling.GetComponent<BoxCollider>();

                if (ceilingCollider == null)
                {
                    ceilingCollider =
                        ceiling.gameObject.AddComponent<BoxCollider>();
                }

                ceilingCollider.center =
                    new Vector3(
                        7.62939453e-06f,
                        -1.33226752e-15f,
                        3.78653235e-29f);

                ceilingCollider.size =
                    new Vector3(
                        1.93929696f,
                        2.72384391e-08f,
                        2.08359361f);

                ceilingCollider.isTrigger =
                    false;
            }

            Transform garageDoor =
                FindChildByName(
                    garageTransform,
                    "Garage door");

            if (garageDoor != null)
            {
                BoxCollider doorCollider =
                    garageDoor.GetComponent<BoxCollider>();

                if (doorCollider == null)
                {
                    doorCollider =
                        garageDoor.gameObject.AddComponent<BoxCollider>();
                }

                doorCollider.center =
                    new Vector3(
                        -0.00499999989f,
                        5.96046448e-08f,
                        9.53674316e-07f);

                doorCollider.size =
                    new Vector3(
                        0.0100001041f,
                        2.42729425f,
                        5.55081701f);

                doorCollider.isTrigger =
                    false;
            }

            InstallGaragePresentationLighting(
                garageTransform);

            InstallGarageReflectionProbe(
                garageTransform);

            InstallGaragePresentationPostFx(
                garageTransform);
        }

        public static void SetGaragePresentationLighting(
            bool active)
        {
            if (garagePresentationLighting != null)
            {
                garagePresentationLighting.SetActive(true);
            }

            ApplyGarageInteriorMood(
                active);

            if (active &&
                activeGarageInterior != null)
            {
                Transform probeTransform =
                    activeGarageInterior.transform.Find(
                        "Garage Reflection Probe");

                ReflectionProbe probe =
                    probeTransform != null
                        ? probeTransform.GetComponent<ReflectionProbe>()
                        : null;

                probe?.RenderProbe();
            }

            if (garagePresentationPostFx != null)
            {
                garagePresentationPostFx.SetActive(
                    active);
            }

            DayNightCycleController dayNight =
                UnityEngine.Object.FindAnyObjectByType<DayNightCycleController>();

            dayNight?.SetGaragePresentationEnvironment(
                active);

            dayNight?.SetCityPostProcessingEnabled(
                !active);
        }

        private static void ApplyGarageInteriorMood(
            bool active)
        {
            if (activeGarageInterior == null)
                return;

            float garageEmissionMultiplier =
                1f;

            if (active)
            {
                DayNightCycleController dayNight =
                    UnityEngine.Object.FindAnyObjectByType<DayNightCycleController>();

                float nightAmount =
                    dayNight != null
                        ? dayNight.NightAmount
                        : 0f;

                // Day: keep the authored fluorescent panels readable.
                // Twilight: soften them so they do not dominate the car.
                // Night: strongly reduce the self-emissive white rectangle
                // while actual garage lights provide the illumination.
                garageEmissionMultiplier =
                    Mathf.Lerp(
                        0.74f,
                        0.20f,
                        Mathf.SmoothStep(
                            0.15f,
                            0.85f,
                            nightAmount));
            }

            Renderer[] renderers =
                activeGarageInterior.GetComponentsInChildren<Renderer>(
                    true);

            for (int r = 0;
                 r < renderers.Length;
                 r++)
            {
                Renderer renderer =
                    renderers[r];

                if (renderer == null ||
                    renderer is ParticleSystemRenderer ||
                    renderer is TrailRenderer ||
                    renderer is LineRenderer ||
                    renderer is SpriteRenderer)
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                for (int m = 0;
                     m < materials.Length;
                     m++)
                {
                    Material material =
                        materials[m];

                    if (material == null)
                        continue;

                    MaterialPropertyBlock block =
                        new();

                    renderer.GetPropertyBlock(
                        block,
                        m);

                    if (material.HasProperty(
                            GarageBaseColorId))
                    {
                        Color source =
                            material.GetColor(
                                GarageBaseColorId);

                        block.SetColor(
                            GarageBaseColorId,
                            active
                                ? new Color(
                                    source.r * 0.82f,
                                    source.g * 0.84f,
                                    source.b * 0.88f,
                                    source.a)
                                : source);
                    }
                    else if (material.HasProperty(
                                 GarageColorId))
                    {
                        Color source =
                            material.GetColor(
                                GarageColorId);

                        block.SetColor(
                            GarageColorId,
                            active
                                ? new Color(
                                    source.r * 0.82f,
                                    source.g * 0.84f,
                                    source.b * 0.88f,
                                    source.a)
                                : source);
                    }

                    if (material.HasProperty(
                            GarageEmissionColorId))
                    {
                        Color sourceEmission =
                            material.GetColor(
                                GarageEmissionColorId);

                        block.SetColor(
                            GarageEmissionColorId,
                            active
                                ? sourceEmission *
                                  garageEmissionMultiplier
                                : sourceEmission);
                    }

                    renderer.SetPropertyBlock(
                        block,
                        m);
                }
            }
        }

        private static void InstallGaragePresentationPostFx(
            Transform garageTransform)
        {
            if (garageTransform == null)
                return;

            const string volumeName =
                "Garage Presentation Post FX";

            Transform existing =
                garageTransform.Find(
                    volumeName);

            if (existing != null)
            {
                garagePresentationPostFx =
                    existing.gameObject;

                garagePresentationPostFx.SetActive(
                    false);

                return;
            }

            garagePresentationPostFx =
                new GameObject(
                    volumeName);

            garagePresentationPostFx.transform.SetParent(
                garageTransform,
                false);

            Volume volume =
                garagePresentationPostFx.AddComponent<Volume>();

            volume.isGlobal =
                true;
            volume.priority =
                120f;
            volume.weight =
                1f;

            garagePresentationVolumeProfile =
                ScriptableObject.CreateInstance<VolumeProfile>();

            garagePresentationVolumeProfile.name =
                "Garage Presentation Runtime Profile";

            ColorAdjustments colorAdjustments =
                garagePresentationVolumeProfile.Add<ColorAdjustments>(
                    true);

            colorAdjustments.postExposure.Override(
                0.08f);
            colorAdjustments.contrast.Override(
                16f);
            colorAdjustments.saturation.Override(
                4f);
            colorAdjustments.colorFilter.Override(
                new Color(
                    0.95f,
                    0.98f,
                    1.00f,
                    1f));

            Bloom bloom =
                garagePresentationVolumeProfile.Add<Bloom>(
                    true);

            bloom.threshold.Override(
                0.92f);
            bloom.intensity.Override(
                0.42f);
            bloom.scatter.Override(
                0.60f);
            bloom.clamp.Override(
                8f);

            Vignette vignette =
                garagePresentationVolumeProfile.Add<Vignette>(
                    true);

            vignette.color.Override(
                new Color(
                    0.01f,
                    0.015f,
                    0.035f,
                    1f));
            vignette.intensity.Override(
                0.085f);
            vignette.smoothness.Override(
                0.42f);
            vignette.rounded.Override(
                false);

            Tonemapping tonemapping =
                garagePresentationVolumeProfile.Add<Tonemapping>(
                    true);

            tonemapping.mode.Override(
                TonemappingMode.ACES);

            WhiteBalance whiteBalance =
                garagePresentationVolumeProfile.Add<WhiteBalance>(
                    true);

            whiteBalance.temperature.Override(
                -6f);
            whiteBalance.tint.Override(
                1f);

            SplitToning splitToning =
                garagePresentationVolumeProfile.Add<SplitToning>(
                    true);

            splitToning.shadows.Override(
                new Color(
                    0.40f,
                    0.47f,
                    0.58f,
                    1f));

            splitToning.highlights.Override(
                new Color(
                    0.62f,
                    0.54f,
                    0.43f,
                    1f));

            splitToning.balance.Override(
                -4f);

            DepthOfField depthOfField =
                garagePresentationVolumeProfile.Add<DepthOfField>(
                    true);

            depthOfField.mode.Override(
                DepthOfFieldMode.Bokeh);

            depthOfField.focusDistance.Override(
                5.6f);

            depthOfField.aperture.Override(
                7.0f);

            depthOfField.focalLength.Override(
                46f);

            volume.sharedProfile =
                garagePresentationVolumeProfile;

            garagePresentationPostFx.SetActive(
                false);
        }

        private static void InstallGaragePresentationLighting(
            Transform garageTransform)
        {
            if (garageTransform == null)
                return;

            const string rigName = "Garage Presentation Lighting";
            Transform existing = garageTransform.Find(rigName);
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(existing.gameObject);
            }

            // Remove authored lights too: the interior has exactly one source.
            foreach (Light oldLight in garageTransform.GetComponentsInChildren<Light>(true))
            {
                oldLight.enabled = false;
                UnityEngine.Object.Destroy(oldLight);
            }

            garagePresentationLighting = new GameObject(rigName);
            garagePresentationLighting.transform.SetParent(garageTransform, false);

            Transform floor = FindChildByName(garageTransform, "Floor");
            Transform ceiling = FindChildByName(garageTransform, "Ceiling");
            Renderer floorRenderer = floor != null ? floor.GetComponent<Renderer>() : null;
            Renderer ceilingRenderer = ceiling != null ? ceiling.GetComponent<Renderer>() : null;
            Bounds roomBounds = floorRenderer != null
                ? floorRenderer.bounds
                : new Bounds(garageTransform.position, Vector3.one * 10f);
            if (ceilingRenderer != null)
                roomBounds.Encapsulate(ceilingRenderer.bounds);

            // World-space dimensions account for the scaled garage prefab.
            Vector3 lightPosition = roomBounds.center;
            lightPosition.y = ceilingRenderer != null
                ? ceilingRenderer.bounds.min.y - 0.35f
                : roomBounds.max.y - 0.35f;
            garagePresentationLighting.transform.position = lightPosition;

            Light light = garagePresentationLighting.AddComponent<Light>();
            light.type = LightType.Point;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.color = new Color(1f, 0.94f, 0.85f);
            light.range = Mathf.Max(12f, (roomBounds.extents + Vector3.up * roomBounds.extents.y).magnitude * 1.6f);
            light.intensity = 4f;
            light.cullingMask = ~0;
            light.renderMode = LightRenderMode.ForcePixel;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.65f;
            light.shadowBias = 0.03f;
            light.shadowNormalBias = 0.2f;
            light.enabled = true;
        }

        private static void InstallGarageReflectionProbe(
            Transform garageTransform)
        {
            if (garageTransform == null)
                return;

            const string probeName =
                "Garage Reflection Probe";

            Transform existing =
                garageTransform.Find(
                    probeName);

            GameObject probeObject;

            if (existing != null)
            {
                probeObject =
                    existing.gameObject;
            }
            else
            {
                probeObject =
                    new GameObject(
                        probeName);

                probeObject.transform.SetParent(
                    garageTransform,
                    false);
            }

            // Garage Scene.unity uses local Y 1.041. The garage itself is
            // scaled x2 in the city, so the probe follows the same transform.
            probeObject.transform.localPosition =
                new Vector3(
                    0f,
                    1.041f,
                    0f);

            probeObject.transform.localRotation =
                Quaternion.identity;

            probeObject.transform.localScale =
                Vector3.one;

            ReflectionProbe probe =
                probeObject.GetComponent<ReflectionProbe>();

            if (probe == null)
            {
                probe =
                    probeObject.AddComponent<ReflectionProbe>();
            }

            probe.mode =
                ReflectionProbeMode.Realtime;

            probe.refreshMode =
                ReflectionProbeRefreshMode.ViaScripting;

            probe.timeSlicingMode =
                ReflectionProbeTimeSlicingMode.NoTimeSlicing;

            probe.resolution =
                256;

            probe.size =
                new Vector3(
                    20f,
                    20f,
                    20f);

            probe.center =
                Vector3.zero;

            probe.nearClipPlane =
                0.3f;

            probe.farClipPlane =
                1000f;

            probe.intensity =
                0.35f;

            probe.blendDistance =
                2f;

            probe.hdr =
                true;

            probe.boxProjection =
                true;

            probe.cullingMask =
                ~0;

            probe.clearFlags =
                ReflectionProbeClearFlags.Skybox;

            probe.RenderProbe();
        }

        private static Transform FindChildByName(
            Transform root,
            string childName)
        {
            if (root == null)
                return null;

            if (root.name ==
                childName)
            {
                return root;
            }

            for (int i = 0;
                 i < root.childCount;
                 i++)
            {
                Transform found =
                    FindChildByName(
                        root.GetChild(i),
                        childName);

                if (found != null)
                    return found;
            }

            return null;
        }

        public static Vector3 SnapToNearestRoad(
            Vector3 approximate)
        {
            if (activeCity == null)
                return approximate;

            return FindRoadPointNear(
                approximate,
                42f,
                false);
        }

        public static void ResolveNearestRoadResetPose(
            Vector3 approximate,
            Vector3 preferredForward,
            out Vector3 position,
            out Quaternion rotation)
        {
            Vector3 roadPoint =
                PlayerSpawnPoint;

            bool foundRoad =
                activeCity != null &&
                TryFindNearestRoadForReset(
                    approximate,
                    out roadPoint);

            if (!foundRoad)
            {
                roadPoint =
                    PlayerSpawnPoint;

                if (activeCity != null &&
                    TryGetRoadHit(
                        PlayerSpawnPoint.x,
                        PlayerSpawnPoint.z,
                        out Vector3 spawnRoad))
                {
                    roadPoint =
                        spawnRoad;
                }
            }

            // Use the actual road height only. Never derive rescue height
            // from the current vehicle Y; otherwise repeated resets can climb.
            position =
                new Vector3(
                    roadPoint.x,
                    roadPoint.y + 1.15f,
                    roadPoint.z);

            Vector3 roadDirection =
                activeCity != null
                    ? EstimateRoadDirection(
                        roadPoint)
                    : preferredForward;

            roadDirection.y = 0f;

            if (roadDirection.sqrMagnitude <
                0.001f)
            {
                roadDirection =
                    Vector3.forward;
            }

            roadDirection.Normalize();

            preferredForward.y = 0f;

            if (preferredForward.sqrMagnitude >
                    0.001f &&
                Vector3.Dot(
                    roadDirection,
                    preferredForward.normalized) <
                0f)
            {
                roadDirection =
                    -roadDirection;
            }

            rotation =
                Quaternion.LookRotation(
                    roadDirection,
                    Vector3.up);
        }

        private static bool TryFindNearestRoadForReset(
            Vector3 approximate,
            out Vector3 roadPoint)
        {
            roadPoint =
                default;
            if (TryGetRoadHit(
                    approximate.x,
                    approximate.z,
                    out roadPoint))
            {
                return true;
            }

            // City roads are laid out on broad FCG blocks. Searching by
            // expanding square rings gives us the nearest valid road in X/Z
            // without ever using the car's current height as a fallback.
            const float step =
                4f;

            const float maximumRadius =
                220f;

            float bestDistanceSquared =
                float.PositiveInfinity;

            bool found =
                false;

            int rings =
                Mathf.CeilToInt(
                    maximumRadius /
                    step);

            for (int ring = 1;
                 ring <= rings;
                 ring++)
            {
                float radius =
                    ring *
                    step;

                for (float offset = -radius;
                     offset <= radius;
                     offset += step)
                {
                    TestResetRoadCandidate(
                        approximate,
                        approximate.x + offset,
                        approximate.z - radius,
                        ref found,
                        ref roadPoint,
                        ref bestDistanceSquared);

                    TestResetRoadCandidate(
                        approximate,
                        approximate.x + offset,
                        approximate.z + radius,
                        ref found,
                        ref roadPoint,
                        ref bestDistanceSquared);

                    TestResetRoadCandidate(
                        approximate,
                        approximate.x - radius,
                        approximate.z + offset,
                        ref found,
                        ref roadPoint,
                        ref bestDistanceSquared);

                    TestResetRoadCandidate(
                        approximate,
                        approximate.x + radius,
                        approximate.z + offset,
                        ref found,
                        ref roadPoint,
                        ref bestDistanceSquared);
                }

                if (found &&
                    bestDistanceSquared <=
                    radius * radius)
                {
                    break;
                }
            }

            return found;
        }

        private static void TestResetRoadCandidate(
            Vector3 approximate,
            float x,
            float z,
            ref bool found,
            ref Vector3 best,
            ref float bestDistanceSquared)
        {
            if (hasCityBounds &&
                (x < cityBounds.min.x ||
                 x > cityBounds.max.x ||
                 z < cityBounds.min.z ||
                 z > cityBounds.max.z))
            {
                return;
            }

            if (!TryGetRoadHit(
                    x,
                    z,
                    out Vector3 hit))
            {
                return;
            }

            float dx =
                hit.x -
                approximate.x;

            float dz =
                hit.z -
                approximate.z;

            float distanceSquared =
                dx * dx +
                dz * dz;

            if (found &&
                distanceSquared >=
                bestDistanceSquared)
            {
                return;
            }

            found =
                true;

            best =
                hit;

            bestDistanceSquared =
                distanceSquared;
        }

        private static void ResolveGameplayLayout()
        {
            GaragePoint =
                new Vector3(
                    -585.822f,
                    0.2f,
                    505.109f);

            GarageSpawnRotation =
                Quaternion.Euler(
                    7.40436444e-05f,
                    89.9998322f,
                    -4.8625111e-06f);

            PlayerSpawnPoint =
                new Vector3(
                    -570f,
                    0.25f,
                    505.109f);

            PlayerSpawnRotation =
                Quaternion.Euler(
                    0f,
                    90f,
                    0f);

            DriftChallengePoint =
                FindRoadPointNear(
                    new Vector3(
                        -450f,
                        0f,
                        150f),
                    75f,
                    true);

            deliveryRoute =
                ResolveRoadRoute(
                    DeliveryPreferred);

            sprintRoute =
                ResolveRoadRoute(
                    SprintPreferred);

            circuitRoute =
                ResolveRoadRoute(
                    CircuitPreferred);

            undergroundRoute =
                ResolveRoadRoute(
                    UndergroundPreferred);
        }

        private static Vector3[] ResolveRoadRoute(
            Vector3[] preferred)
        {
            Vector3[] result =
                new Vector3[preferred.Length];

            for (int i = 0;
                 i < preferred.Length;
                 i++)
            {
                result[i] =
                    FindRoadPointNear(
                        preferred[i],
                        80f,
                        false);
            }

            return result;
        }

        private static Vector3 FindRoadPointNear(
            Vector3 preferred,
            float searchRadius,
            bool preferWideRoad)
        {
            if (TryGetRoadHit(
                    preferred.x,
                    preferred.z,
                    out Vector3 exact))
            {
                if (!preferWideRoad ||
                    RoadOpenness(
                        exact,
                        13f) >= 4)
                {
                    exact.y +=
                        MarkerLift;

                    return exact;
                }
            }

            const float step =
                5f;

            Vector3 best =
                preferred;

            float bestScore =
                float.PositiveInfinity;

            bool found =
                false;

            int rings =
                Mathf.CeilToInt(
                    searchRadius /
                    step);

            for (int ring = 1;
                 ring <= rings;
                 ring++)
            {
                float radius =
                    ring *
                    step;

                for (float offset = -radius;
                     offset <= radius;
                     offset += step)
                {
                    TestRoadCandidate(
                        preferred,
                        preferred.x + offset,
                        preferred.z - radius,
                        preferWideRoad,
                        ref found,
                        ref best,
                        ref bestScore);

                    TestRoadCandidate(
                        preferred,
                        preferred.x + offset,
                        preferred.z + radius,
                        preferWideRoad,
                        ref found,
                        ref best,
                        ref bestScore);

                    TestRoadCandidate(
                        preferred,
                        preferred.x - radius,
                        preferred.z + offset,
                        preferWideRoad,
                        ref found,
                        ref best,
                        ref bestScore);

                    TestRoadCandidate(
                        preferred,
                        preferred.x + radius,
                        preferred.z + offset,
                        preferWideRoad,
                        ref found,
                        ref best,
                        ref bestScore);
                }

                if (found &&
                    bestScore <=
                    radius * radius)
                    break;
            }

            if (!found)
            {
                Vector3 safeFallback =
                    PlayerSpawnPoint;

                if (TryGetRoadHit(
                        PlayerSpawnPoint.x,
                        PlayerSpawnPoint.z,
                        out Vector3 spawnRoad))
                {
                    safeFallback =
                        spawnRoad;

                    safeFallback.y +=
                        MarkerLift;
                }

                return safeFallback;
            }

            best.y +=
                MarkerLift;

            return best;
        }

        private static void TestRoadCandidate(
            Vector3 preferred,
            float x,
            float z,
            bool preferWideRoad,
            ref bool found,
            ref Vector3 best,
            ref float bestScore)
        {
            if (hasCityBounds &&
                (x < cityBounds.min.x ||
                 x > cityBounds.max.x ||
                 z < cityBounds.min.z ||
                 z > cityBounds.max.z))
                return;

            if (!TryGetRoadHit(
                    x,
                    z,
                    out Vector3 hit))
                return;

            float dx =
                hit.x -
                preferred.x;

            float dz =
                hit.z -
                preferred.z;

            float score =
                dx * dx +
                dz * dz;

            if (preferWideRoad)
            {
                int openness =
                    RoadOpenness(
                        hit,
                        13f);

                score -=
                    openness *
                    140f;
            }

            if (found &&
                score >= bestScore)
                return;

            found =
                true;

            best =
                hit;

            bestScore =
                score;
        }

        private static int RoadOpenness(
            Vector3 point,
            float radius)
        {
            int count =
                0;

            Vector2[] offsets =
            {
                new(radius, 0f),
                new(-radius, 0f),
                new(0f, radius),
                new(0f, -radius),
                new(radius * 0.7f, radius * 0.7f),
                new(-radius * 0.7f, radius * 0.7f),
                new(radius * 0.7f, -radius * 0.7f),
                new(-radius * 0.7f, -radius * 0.7f)
            };

            foreach (Vector2 offset in offsets)
            {
                if (TryGetRoadHit(
                        point.x + offset.x,
                        point.z + offset.y,
                        out _))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryGetRoadHit(
            float x,
            float z,
            out Vector3 point)
        {
            point =
                default;

            RaycastHit[] hits =
                Physics.RaycastAll(
                    new Vector3(
                        x,
                        RayStartY(),
                        z),
                    Vector3.down,
                    RayDistance(),
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

            if (hits == null ||
                hits.Length == 0)
                return false;

            Array.Sort(
                hits,
                (a, b) =>
                    a.distance.CompareTo(
                        b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (!IsRoadSurface(
                        hit))
                    continue;

                point =
                    hit.point;

                return true;
            }

            return false;
        }

        private static bool IsRoadSurface(
            RaycastHit hit)
        {
            if (hit.collider == null)
                return false;

            string path =
                GetHierarchyPath(
                        hit.collider.transform)
                    .ToLowerInvariant();

            if (path.Contains("sidewalk") ||
                path.Contains("/buildings/") ||
                path.Contains("/park-") ||
                path.Contains("/garden") ||
                path.Contains("/objects/") ||
                path.Contains("guardrail") ||
                path.Contains("guard-rail") ||
                path.Contains("grass"))
            {
                return false;
            }

            bool pathLooksLikeRoad =
                path.Contains("road") ||
                path.Contains("street") ||
                path.Contains("highway") ||
                path.Contains("asphalt") ||
                path.Contains("intersection") ||
                path.Contains("crossroad");

            if (IsExactRoadTriangle(
                    hit))
            {
                return true;
            }

            Renderer renderer =
                hit.collider.GetComponent<Renderer>();

            if (renderer == null)
            {
                renderer =
                    hit.collider.GetComponentInParent<Renderer>();
            }

            if (renderer == null)
            {
                renderer =
                    hit.collider.GetComponentInChildren<Renderer>();
            }

            if (renderer != null)
            {
                Material[] materials =
                    renderer.sharedMaterials;

                if (materials != null)
                {
                    foreach (Material material in
                             materials)
                    {
                        if (material == null)
                            continue;

                        string materialName =
                            material.name
                                .ToLowerInvariant();

                        bool materialLooksLikeRoad =
                            (materialName.Contains("road") ||
                             materialName.Contains("highway") ||
                             materialName.Contains("asphalt") ||
                             materialName.Contains("street")) &&
                            !materialName.Contains("grass");

                        if (materialLooksLikeRoad)
                            return true;
                    }
                }
            }

            return pathLooksLikeRoad;
        }

        private static bool IsExactRoadTriangle(
            RaycastHit hit)
        {
            MeshCollider meshCollider =
                hit.collider as MeshCollider;

            if (meshCollider == null ||
                meshCollider.sharedMesh == null)
                return false;

            string path =
                GetHierarchyPath(
                        hit.collider.transform)
                    .ToLowerInvariant();

            if (path.Contains("sidewalk") ||
                path.Contains("/buildings/") ||
                path.Contains("/park-") ||
                path.Contains("/garden") ||
                path.Contains("/objects/") ||
                path.Contains("guardrail") ||
                path.Contains("guard-rail"))
                return false;

            Renderer renderer =
                hit.collider.GetComponent<Renderer>();

            if (renderer == null)
                return false;

            Material material =
                ResolveTriangleMaterial(
                    hit,
                    renderer,
                    meshCollider);

            if (material == null)
                return false;

            string materialName =
                material.name.ToLowerInvariant();

            bool asphalt =
                materialName.Contains("road") &&
                !materialName.Contains("grass");

            bool highway =
                materialName.Contains("highway");

            return
                asphalt ||
                highway;
        }

        private static Material ResolveTriangleMaterial(
            RaycastHit hit,
            Renderer renderer,
            MeshCollider meshCollider)
        {
            if (renderer == null ||
                meshCollider == null ||
                meshCollider.sharedMesh == null)
                return null;

            Material[] materials =
                renderer.sharedMaterials;

            if (materials == null ||
                materials.Length == 0)
                return null;

            int triangleIndex =
                hit.triangleIndex;

            if (triangleIndex < 0)
                return materials[0];

            Mesh mesh =
                meshCollider.sharedMesh;

            int triangleCursor =
                0;

            int subMeshCount =
                Mathf.Min(
                    mesh.subMeshCount,
                    materials.Length);

            for (int subMesh = 0;
                 subMesh < subMeshCount;
                 subMesh++)
            {
                if (mesh.GetTopology(subMesh) !=
                    MeshTopology.Triangles)
                    continue;

                int triangleCount =
                    (int)mesh.GetIndexCount(
                        subMesh) /
                    3;

                if (triangleIndex >= triangleCursor &&
                    triangleIndex <
                    triangleCursor + triangleCount)
                {
                    return materials[subMesh];
                }

                triangleCursor +=
                    triangleCount;
            }

            return materials[0];
        }

        private static Vector3 EstimateRoadDirection(
            Vector3 point)
        {
            int xScore =
                AxisRoadScore(
                    point,
                    Vector3.right);

            int zScore =
                AxisRoadScore(
                    point,
                    Vector3.forward);

            Vector3 direction =
                zScore >= xScore
                    ? Vector3.forward
                    : Vector3.right;

            Vector3 towardCenter =
                cityBounds.center -
                point;

            towardCenter.y =
                0f;

            if (towardCenter.sqrMagnitude > 1f &&
                Vector3.Dot(
                    direction,
                    towardCenter) < 0f)
            {
                direction =
                    -direction;
            }

            return direction;
        }

        private static int AxisRoadScore(
            Vector3 point,
            Vector3 axis)
        {
            int score =
                0;

            float[] distances =
            {
                6f,
                12f,
                18f
            };

            foreach (float distance in distances)
            {
                Vector3 forward =
                    point +
                    axis *
                    distance;

                Vector3 backward =
                    point -
                    axis *
                    distance;

                if (TryGetRoadHit(
                        forward.x,
                        forward.z,
                        out _))
                    score++;

                if (TryGetRoadHit(
                        backward.x,
                        backward.z,
                        out _))
                    score++;
            }

            return score;
        }

        private static bool IsFcgCityRenderer(
            Renderer renderer)
        {
            if (renderer == null)
                return false;

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                string name =
                    material.name;

                if (name.StartsWith(
                        "FCG_",
                        StringComparison.OrdinalIgnoreCase) ||
                    name.IndexOf(
                        "FCG",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject FindExistingCity()
        {
            Scene scene =
                SceneManager.GetActiveScene();

            if (!scene.IsValid() ||
                !scene.isLoaded)
                return null;

            foreach (GameObject root in
                     scene.GetRootGameObjects())
            {
                if (string.Equals(
                        root.name,
                        SceneCityName,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        root.name,
                        RuntimeCityName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return root;
                }
            }

            return null;
        }

        private static Bounds CalculateCityBounds(
            GameObject city)
        {
            Renderer[] allRenderers =
                city.GetComponentsInChildren<Renderer>(
                    true);

            bool hasFcgRenderer =
                false;

            bool boundsInitialized =
                false;

            Bounds bounds =
                default;

            foreach (Renderer renderer in
                     allRenderers)
            {
                if (!IsFcgCityRenderer(
                        renderer))
                {
                    continue;
                }

                if (!boundsInitialized)
                {
                    bounds =
                        renderer.bounds;

                    boundsInitialized =
                        true;
                }
                else
                {
                    bounds.Encapsulate(
                        renderer.bounds);
                }

                hasFcgRenderer =
                    true;
            }

            if (!hasFcgRenderer)
            {
                foreach (Renderer renderer in
                         allRenderers)
                {
                    if (renderer == null)
                        continue;

                    if (!boundsInitialized)
                    {
                        bounds =
                            renderer.bounds;

                        boundsInitialized =
                            true;
                    }
                    else
                    {
                        bounds.Encapsulate(
                            renderer.bounds);
                    }
                }
            }

            if (!boundsInitialized)
            {
                // Safe fallback for the current authored main district.
                // Do not retain the obsolete remote/highway bounds from older
                // city generations, otherwise invalid legacy coordinates can
                // be treated as playable.
                return new Bounds(
                    new Vector3(
                        0f,
                        80f,
                        175f),
                    new Vector3(
                        1200f,
                        170f,
                        900f));
            }

            return
                bounds;
        }

        private static float RayStartY()
        {
            return
                hasCityBounds
                    ? cityBounds.max.y + 40f
                    : 220f;
        }

        private static float RayDistance()
        {
            return
                hasCityBounds
                    ? cityBounds.size.y + 100f
                    : 320f;
        }

        private static string GetHierarchyPath(
            Transform transform)
        {
            string path =
                transform.name;

            Transform current =
                transform.parent;

            while (current != null)
            {
                path =
                    current.name +
                    "/" +
                    path;

                current =
                    current.parent;
            }

            return path;
        }
    }
}
