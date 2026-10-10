using MotorCity.Diagnostics;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MotorCity.World
{
    public static class CityAssetRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/CityVisual";

        private const string SceneCityName =
            "City-Maker";

        private const string RuntimeCityName =
            "MotorCity_FCGCity";

        private const float MarkerLift =
            0.05f;

        private static readonly Vector3 AuthoredPlayerSpawnPoint =
            new(
                -428.434631f,
                0.145054966f,
                315.49292f);

        private static readonly Quaternion AuthoredPlayerSpawnRotation =
            Quaternion.Euler(
                359.60907f,
                89.3797455f,
                3.25508745e-06f);

        private static readonly Vector3 AuthoredGaragePoint =
            new(
                -421.68866f,
                0.182654113f,
                330.916077f);

        private static readonly Quaternion AuthoredGarageSpawnRotation =
            Quaternion.Euler(
                7.40436444e-05f,
                89.9998322f,
                -4.8625111e-06f);

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

        private static Bounds cityBounds;
        private static bool hasCityBounds;

        private static readonly List<ReflectionProbe> cityReflectionProbes =
            new();

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            activeCity =
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
                AuthoredPlayerSpawnPoint;

            PlayerSpawnRotation =
                AuthoredPlayerSpawnRotation;

            GaragePoint =
                AuthoredGaragePoint;

            GarageSpawnRotation =
                AuthoredGarageSpawnRotation;

            DriftChallengePoint =
                new Vector3(
                    -450f,
                    0.2f,
                    150f);
        }

        public static Vector3 PlayerSpawnPoint { get; private set; } =
            AuthoredPlayerSpawnPoint;

        public static Quaternion PlayerSpawnRotation { get; private set; } =
            AuthoredPlayerSpawnRotation;

        // Dedicated player garage on the authored parking apron.
        public static Vector3 GaragePoint { get; private set; } =
            AuthoredGaragePoint;

        public static Quaternion GarageSpawnRotation { get; private set; } =
            AuthoredGarageSpawnRotation;

        public static Vector3 GarageVehiclePosition { get; } =
            AuthoredGaragePoint;

        public static Quaternion GarageVehicleRotation { get; } =
            Quaternion.Euler(
                0f,
                142.676514f,
                0f);

        public static Vector3 GarageExitPosition { get; } =
            new(
                -413.192017f,
                0.174802512f,
                331.059753f);

        public static Quaternion GarageExitRotation { get; } =
            Quaternion.Euler(
                359.607452f,
                181.803848f,
                0.0546497814f);

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

            // Preserve authored city materials. Only emission-capable windows
            // receive a faithful per-material copy for night-time glow.
            FcgRuntimeGlassMaterialFactory.BindAuthoredWindowEmission(activeCity);

#if UNITY_WEBGL && !UNITY_EDITOR
            ConvertUnsupportedCityMaterialsForWeb();
#if DEVELOPMENT_BUILD || MOTORCITY_CITY_MATERIAL_AUDIT
            MotorCityWebMaterialDiagnostics.Run(
                activeCity);
#endif
#endif

            // Runtime treats the authored city as read-only.
            // Colliders, props, parked vehicles, traffic signals and all
            // other map objects must come exactly from CityVisual.prefab.
            cityBounds =
                CalculateCityBounds(
                    activeCity);

            hasCityBounds =
                cityBounds.size.x > 10f &&
                cityBounds.size.z > 10f;

            Physics.SyncTransforms();

            ResolveGameplayLayout();

            // DayNightCycleController is initialized immediately after the city
            // by MotorCityBootstrap. Create the probes now, but let that
            // controller perform the first capture after sky/ambient/fog are
            // in their final runtime state.
            InstallCityReflectionProbes(
                false);

            return true;
        }

        private static void ConvertUnsupportedCityMaterialsForWeb()
        {
            if (activeCity == null)
                return;

            Shader urpLit =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            if (urpLit == null)
                return;

            var converted =
                new Dictionary<Material, Material>();

            Renderer[] renderers =
                activeCity.GetComponentsInChildren<Renderer>(
                    true);

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials =
                    renderer.sharedMaterials;

                if (materials == null ||
                    materials.Length == 0)
                {
                    continue;
                }

                bool changed =
                    false;

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    Material source =
                        materials[i];

                    if (source == null)
                        continue;

                    Shader sourceShader =
                        source.shader;

                    string rendererName =
                        renderer.transform.name;

                    bool forcePlantFallback =
                        rendererName.Equals(
                            "Plant-01",
                            StringComparison.OrdinalIgnoreCase) ||
                        rendererName.Equals(
                            "Plant-01 (1)",
                            StringComparison.OrdinalIgnoreCase) ||
                        renderer.transform.root.name.Equals(
                            "Plant-01",
                            StringComparison.OrdinalIgnoreCase) ||
                        renderer.transform.root.name.Equals(
                            "Plant-01 (1)",
                            StringComparison.OrdinalIgnoreCase);

                    if (!forcePlantFallback &&
                        sourceShader != null &&
                        sourceShader.isSupported)
                    {
                        // Keep working authored materials exactly as-is.
                        // This preserves foliage/background alpha cutouts and
                        // avoids adding Lit reflections to billboard textures.
                        continue;
                    }

                    if (converted.TryGetValue(
                            source,
                            out Material cached) &&
                        cached != null)
                    {
                        materials[i] =
                            cached;

                        changed =
                            true;

                        continue;
                    }

                    Material runtime =
                        new Material(
                            urpLit)
                        {
                            name =
                                source.name +
                                "_MotorCityWeb",
                            hideFlags =
                                HideFlags.DontSave
                        };

                    string[] textureProperties =
                        source.GetTexturePropertyNames();

                    string sourceTextureProperty =
                        null;

                    Texture baseTexture =
                        null;

                    string[] preferred =
                    {
                        "_BaseMap",
                        "_MainTex",
                        "_Albedo",
                        "_BaseColorMap"
                    };

                    foreach (string property in
                             preferred)
                    {
                        if (Array.IndexOf(
                                textureProperties,
                                property) < 0)
                        {
                            continue;
                        }

                        Texture candidate =
                            source.GetTexture(
                                property);

                        if (candidate == null)
                            continue;

                        sourceTextureProperty =
                            property;
                        baseTexture =
                            candidate;
                        break;
                    }

                    if (baseTexture != null)
                    {
                        runtime.SetTexture(
                            "_BaseMap",
                            baseTexture);

                        runtime.SetTextureScale(
                            "_BaseMap",
                            source.GetTextureScale(
                                sourceTextureProperty));

                        runtime.SetTextureOffset(
                            "_BaseMap",
                            source.GetTextureOffset(
                                sourceTextureProperty));
                    }

                    Color baseColor =
                        Color.white;

                    if (source.HasProperty(
                            "_BaseColor"))
                    {
                        baseColor =
                            source.GetColor(
                                "_BaseColor");
                    }
                    else if (source.HasProperty(
                                 "_Color"))
                    {
                        baseColor =
                            source.GetColor(
                                "_Color");
                    }

                    runtime.SetColor(
                        "_BaseColor",
                        baseColor);

                    if (runtime.HasProperty(
                            "_Metallic"))
                    {
                        runtime.SetFloat(
                            "_Metallic",
                            0f);
                    }

                    if (runtime.HasProperty(
                            "_Smoothness"))
                    {
                        runtime.SetFloat(
                            "_Smoothness",
                            0f);
                    }

                    if (runtime.HasProperty(
                            "_SpecularHighlights"))
                    {
                        runtime.SetFloat(
                            "_SpecularHighlights",
                            0f);
                    }

                    bool alphaCutout =
                        forcePlantFallback ||
                        source.HasProperty(
                            "_Cutoff") ||
                        source.IsKeywordEnabled(
                            "_ALPHATEST_ON");

                    if (alphaCutout)
                    {
                        float cutoff =
                            source.HasProperty(
                                "_Cutoff")
                                ? source.GetFloat(
                                    "_Cutoff")
                                : forcePlantFallback
                                    ? 0.45f
                                    : 0.35f;

                        if (runtime.HasProperty(
                                "_AlphaClip"))
                        {
                            runtime.SetFloat(
                                "_AlphaClip",
                                1f);
                        }

                        if (runtime.HasProperty(
                                "_Cutoff"))
                        {
                            runtime.SetFloat(
                                "_Cutoff",
                                Mathf.Clamp01(
                                    cutoff));
                        }

                        runtime.EnableKeyword(
                            "_ALPHATEST_ON");
                    }

                    if (source.HasProperty(
                            "_EmissionColor"))
                    {
                        Color emission =
                            source.GetColor(
                                "_EmissionColor");

                        if (emission.maxColorComponent >
                            0.001f)
                        {
                            runtime.SetColor(
                                "_EmissionColor",
                                emission);

                            runtime.EnableKeyword(
                                "_EMISSION");
                        }
                    }

                    converted[source] =
                        runtime;
                    materials[i] =
                        runtime;
                    changed =
                        true;
                }

                if (changed)
                {
                    renderer.sharedMaterials =
                        materials;
                }
            }
        }

        private static void InstallCityReflectionProbes(
            bool captureImmediately = true)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Realtime cubemap captures are disproportionately expensive in
            // browsers. Web uses ambient/sky lighting instead.
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
#else
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

            // At startup DayNightCycleController captures these after it has
            // applied the runtime sky/ambient/fog. Rebuilds caused by a quality
            // change still capture immediately.
            if (captureImmediately)
            {
                RefreshCityReflectionProbes();
            }
#endif
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
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#else
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
#endif
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
                AuthoredGaragePoint;

            GarageSpawnRotation =
                AuthoredGarageSpawnRotation;

            PlayerSpawnPoint =
                AuthoredPlayerSpawnPoint;

            PlayerSpawnRotation =
                AuthoredPlayerSpawnRotation;

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
