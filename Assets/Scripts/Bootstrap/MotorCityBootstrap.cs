using MotorCity.CameraSystem;
using MotorCity.Gameplay;
using MotorCity.UI;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.Bootstrap
{
    public static class MotorCityBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildPrototype()
        {
            if (Object.FindAnyObjectByType<ArcadeCarController>() != null) return;

            Time.fixedDeltaTime = 0.02f;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            Physics.defaultContactOffset = 0.01f;
            Physics.defaultSolverIterations = 10;
            Physics.defaultSolverVelocityIterations = 3;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.40f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.19f, 0.20f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.075f, 0.072f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.61f, 0.67f);
            RenderSettings.fogStartDistance = 260f;
            RenderSettings.fogEndDistance = 980f;

            CreateLighting();
            CreatePrototypeCity();

            ArcadeCarController car = CreateCar();
            ArcadeRacingCarRuntimeInstaller.TryInstallNow(car);
            DriftTracker drift = car.gameObject.AddComponent<DriftTracker>();

            GameObject systems = new("Gameplay Systems");
            ActivityManager activityManager = systems.AddComponent<ActivityManager>();
            PlayerWallet wallet = systems.AddComponent<PlayerWallet>();
            drift.Initialize(wallet, activityManager);

            DeliveryActivity delivery = systems.AddComponent<DeliveryActivity>();
            delivery.Initialize(car, wallet, activityManager);
            delivery.SnapRouteToRoad();

            DriftChallenge driftChallenge = systems.AddComponent<DriftChallenge>();
            driftChallenge.Initialize(car, drift, wallet, activityManager);
            driftChallenge.SnapZoneToRoad();

            StreetSprintActivity streetSprint = systems.AddComponent<StreetSprintActivity>();
            streetSprint.Initialize(car, wallet, activityManager);
            streetSprint.SnapRouteToRoad();

            GarageUpgradeSystem garage = systems.AddComponent<GarageUpgradeSystem>();
            garage.Initialize(
                car,
                wallet,
                activityManager,
                delivery,
                driftChallenge,
                streetSprint);
            garage.SnapGarageToRoad();

            CreateDeliveryMarker(delivery, activityManager);
            CreateDriftChallengeMarker(driftChallenge, activityManager);
            CreateStreetSprintMarker(streetSprint, activityManager);
            CreateGarageMarker(garage);

            CreateCamera(car.transform);
            CreateHud(car, wallet, drift, delivery, driftChallenge, streetSprint, activityManager, garage);
        }

        private static void CreateLighting()
        {
            GameObject sunObject = new("Sun");
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.05f;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
        }

        private static void CreatePrototypeCity()
        {
            // Preferred world: the downloaded CC0 Community Core / Kenney city.
            // The old procedural primitives remain only as a fallback if the
            // editor asset installer could not prepare the external assets.
            if (CityAssetRuntimeInstaller.TryInstall())
                return;

            Material asphalt = Material(new Color(0.045f, 0.048f, 0.055f), 0.08f, 0.23f);
            Material groundMaterial = Material(new Color(0.115f, 0.125f, 0.13f), 0f, 0.08f);
            Material sidewalk = Material(new Color(0.31f, 0.32f, 0.33f), 0f, 0.18f);
            Material buildingA = Material(new Color(0.08f, 0.14f, 0.2f), 0.12f, 0.28f);
            Material buildingB = Material(new Color(0.23f, 0.105f, 0.075f), 0.06f, 0.22f);
            Material buildingC = Material(new Color(0.16f, 0.17f, 0.19f), 0.1f, 0.32f);
            Material laneWhite = Material(new Color(0.88f, 0.9f, 0.86f), 0f, 0.22f);
            Material laneYellow = Material(new Color(0.95f, 0.7f, 0.12f), 0f, 0.2f);
            Material glass = Material(new Color(0.05f, 0.16f, 0.23f), 0.45f, 0.72f);
            Material lampDark = Material(new Color(0.035f, 0.04f, 0.045f), 0.55f, 0.35f);
            Material lampGlow = Material(new Color(1f, 0.65f, 0.24f), 0f, 0.65f);

            GameObject ground = Primitive("Ground", PrimitiveType.Cube, new Vector3(0f, -0.52f, 0f), new Vector3(252f, 1f, 252f), groundMaterial);
            ground.isStatic = true;

            for (int i = -2; i <= 2; i++)
            {
                float axis = i * 42f;
                CreateVisualSurface($"Road_NS_{i}", PrimitiveType.Cube, new Vector3(axis, 0.012f, 0f), new Vector3(18f, 0.02f, 230f), asphalt);
                CreateVisualSurface($"Road_EW_{i}", PrimitiveType.Cube, new Vector3(0f, 0.014f, axis), new Vector3(230f, 0.02f, 18f), asphalt);

                for (int p = -108; p <= 108; p += 9)
                {
                    CreateVisualSurface("Lane_NS", PrimitiveType.Cube, new Vector3(axis, 0.03f, p), new Vector3(0.14f, 0.008f, 3.7f), i == 0 ? laneYellow : laneWhite);
                    CreateVisualSurface("Lane_EW", PrimitiveType.Cube, new Vector3(p, 0.032f, axis), new Vector3(3.7f, 0.008f, 0.14f), i == 0 ? laneYellow : laneWhite);
                }
            }

            for (int z = -2; z <= 1; z++)
            {
                for (int x = -2; x <= 1; x++)
                {
                    Vector3 blockCenter = new(x * 42f + 21f, 0f, z * 42f + 21f);
                    CreateCityBlock(x, z, blockCenter, sidewalk, buildingA, buildingB, buildingC, glass);
                }
            }

            CreateParkingLot(new Vector3(83f, 0f, -83f), asphalt, laneWhite);
            CreateStreetLights(lampDark, lampGlow);
        }

        private static void CreateCityBlock(int gridX, int gridZ, Vector3 center, Material sidewalk, Material buildingA, Material buildingB, Material buildingC, Material glass)
        {
            CreateVisualSurface("Sidewalk", PrimitiveType.Cube, center + new Vector3(0f, 0.035f, 0f), new Vector3(22f, 0.06f, 22f), sidewalk);

            int seed = Mathf.Abs(gridX * 37 + gridZ * 61 + 97);
            int buildingCount = 2 + seed % 2;

            for (int i = 0; i < buildingCount; i++)
            {
                float height = 10f + (seed * (i + 3) % 22);
                float side = i == 0 ? -1f : 1f;
                Vector3 pos = center + new Vector3(side * 5.5f, height * 0.5f + 0.08f, (i % 2 == 0 ? -1f : 1f) * 3.6f);
                Vector3 scale = new(8.5f, height, 11.5f);
                Material facade = (seed + i) % 3 == 0 ? buildingA : ((seed + i) % 3 == 1 ? buildingB : buildingC);
                GameObject building = Primitive($"Building_{gridX}_{gridZ}_{i}", PrimitiveType.Cube, pos, scale, facade);
                building.isStatic = true;

                for (float y = 3f; y < height - 1f; y += 3.2f)
                {
                    Vector3 windowPos = new(pos.x, y, pos.z - scale.z * 0.5f - 0.01f);
                    CreateVisualSurface("Windows", PrimitiveType.Cube, windowPos, new Vector3(scale.x * 0.66f, 1.25f, 0.035f), glass);
                }
            }

            if ((gridX + gridZ) % 2 == 0)
            {
                Material shop = Material(new Color(0.12f, 0.28f, 0.34f), 0.08f, 0.55f);
                Vector3 storefront = center + new Vector3(0f, 1.3f, -8.2f);
                CreateVisualSurface("Storefront", PrimitiveType.Cube, storefront, new Vector3(8f, 2.5f, 0.18f), shop);
            }
        }

        private static void CreateParkingLot(Vector3 center, Material asphalt, Material line)
        {
            CreateVisualSurface("DriftAndParkingLot", PrimitiveType.Cube, center + new Vector3(0f, 0.018f, 0f), new Vector3(34f, 0.025f, 34f), asphalt);

            for (int i = -3; i <= 3; i++)
            {
                CreateVisualSurface("ParkingLine", PrimitiveType.Cube, center + new Vector3(i * 4.2f, 0.04f, 8f), new Vector3(0.09f, 0.008f, 6.5f), line);
                CreateVisualSurface("ParkingLine", PrimitiveType.Cube, center + new Vector3(i * 4.2f, 0.04f, -8f), new Vector3(0.09f, 0.008f, 6.5f), line);
            }
        }

        private static void CreateStreetLights(Material dark, Material glow)
        {
            for (int z = -84; z <= 84; z += 42)
            {
                for (int x = -84; x <= 84; x += 42)
                {
                    CreateLamp(new Vector3(x + 10.5f, 0f, z + 10.5f), dark, glow);
                    CreateLamp(new Vector3(x - 10.5f, 0f, z - 10.5f), dark, glow);
                }
            }
        }

        private static void CreateLamp(Vector3 position, Material dark, Material glow)
        {
            GameObject pole = CreateVisualSurface("StreetLamp", PrimitiveType.Cylinder, position + new Vector3(0f, 2.6f, 0f), new Vector3(0.12f, 2.6f, 0.12f), dark);
            pole.transform.localScale = new Vector3(0.12f, 2.6f, 0.12f);
            CreateVisualSurface("LampHead", PrimitiveType.Cube, position + new Vector3(0f, 5.2f, 0f), new Vector3(0.65f, 0.14f, 0.32f), glow);
        }

        private static ArcadeCarController CreateCar()
        {
            Material bodyMaterial = Material(new Color(0.72f, 0.025f, 0.018f), 0.55f, 0.72f);
            Material darkMaterial = Material(new Color(0.012f, 0.014f, 0.018f), 0.35f, 0.3f);
            Material glassMaterial = Material(new Color(0.025f, 0.095f, 0.145f), 0.62f, 0.82f);
            Material chrome = Material(new Color(0.42f, 0.44f, 0.46f), 0.92f, 0.68f);
            Material headlight = Material(new Color(0.92f, 0.95f, 1f), 0.05f, 0.88f);
            Material tailLight = Material(new Color(0.9f, 0.015f, 0.008f), 0.05f, 0.78f);

            GameObject car = new("PlayerCar");
            Vector3 roadSpawn =
                CityAssetRuntimeInstaller.SnapToNearestRoad(
                    new Vector3(0f, 0f, -325.3f));
            car.transform.position =
                roadSpawn + Vector3.up * 1.2f;
            car.AddComponent<Rigidbody>();

            BoxCollider chassis = car.AddComponent<BoxCollider>();
            chassis.size = new Vector3(1.9f, 0.7f, 4.2f);
            chassis.center = new Vector3(0f, 0.58f, 0f);

            Primitive("LowerBody", PrimitiveType.Cube, car.transform, new Vector3(1.86f, 0.48f, 4.18f), new Vector3(0f, 0.46f, 0f), bodyMaterial, false);
            Primitive("UpperBody", PrimitiveType.Cube, car.transform, new Vector3(1.72f, 0.25f, 3.5f), new Vector3(0f, 0.73f, -0.05f), bodyMaterial, false);
            Primitive("Cabin", PrimitiveType.Cube, car.transform, new Vector3(1.48f, 0.55f, 1.72f), new Vector3(0f, 1.02f, -0.25f), glassMaterial, false);
            Primitive("Hood", PrimitiveType.Cube, car.transform, new Vector3(1.66f, 0.12f, 1.18f), new Vector3(0f, 0.84f, 1.32f), bodyMaterial, false);
            Primitive("FrontBumper", PrimitiveType.Cube, car.transform, new Vector3(1.78f, 0.18f, 0.18f), new Vector3(0f, 0.38f, 2.12f), darkMaterial, false);
            Primitive("RearBumper", PrimitiveType.Cube, car.transform, new Vector3(1.78f, 0.18f, 0.18f), new Vector3(0f, 0.38f, -2.12f), darkMaterial, false);
            Primitive("Spoiler", PrimitiveType.Cube, car.transform, new Vector3(1.45f, 0.08f, 0.32f), new Vector3(0f, 0.96f, -1.9f), darkMaterial, false);

            Primitive("HeadlightL", PrimitiveType.Cube, car.transform, new Vector3(0.48f, 0.18f, 0.05f), new Vector3(-0.55f, 0.62f, 2.105f), headlight, false);
            Primitive("HeadlightR", PrimitiveType.Cube, car.transform, new Vector3(0.48f, 0.18f, 0.05f), new Vector3(0.55f, 0.62f, 2.105f), headlight, false);
            Primitive("TailLightL", PrimitiveType.Cube, car.transform, new Vector3(0.5f, 0.17f, 0.05f), new Vector3(-0.55f, 0.59f, -2.105f), tailLight, false);
            Primitive("TailLightR", PrimitiveType.Cube, car.transform, new Vector3(0.5f, 0.17f, 0.05f), new Vector3(0.55f, 0.59f, -2.105f), tailLight, false);

            CreateWheel(car.transform, "Wheel_FL", new Vector3(-0.94f, 0.18f, 1.32f), darkMaterial, chrome);
            CreateWheel(car.transform, "Wheel_FR", new Vector3(0.94f, 0.18f, 1.32f), darkMaterial, chrome);
            CreateWheel(car.transform, "Wheel_RL", new Vector3(-0.94f, 0.18f, -1.34f), darkMaterial, chrome);
            CreateWheel(car.transform, "Wheel_RR", new Vector3(0.94f, 0.18f, -1.34f), darkMaterial, chrome);

            ArcadeCarController controller = car.AddComponent<ArcadeCarController>();
            car.AddComponent<CarReset>();
            return controller;
        }

        private static void CreateWheel(Transform parent, string name, Vector3 localPosition, Material tire, Material rim)
        {
            GameObject wheel = Primitive(name, PrimitiveType.Cylinder, parent, new Vector3(0.68f, 0.3f, 0.68f), localPosition, tire, false);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject wheelRim = Primitive(name + "_Rim", PrimitiveType.Cylinder, wheel.transform, new Vector3(0.42f, 0.32f, 0.42f), Vector3.zero, rim, false);
            wheelRim.transform.localRotation = Quaternion.identity;
        }

        private static void CreateDeliveryMarker(
            DeliveryActivity delivery,
            ActivityManager activityManager)
        {
            GameObject marker = CreateAssetMarker(
                "Delivery Marker",
                "MotorCity/Environment/DeliveryCrate",
                delivery.CurrentTarget,
                2.4f,
                new Color(0.08f, 0.5f, 1f));

            RouteMarkerVisual visual =
                marker.AddComponent<RouteMarkerVisual>();
            visual.Bind(delivery, activityManager);
        }

        private static void CreateDriftChallengeMarker(
            DriftChallenge challenge,
            ActivityManager activityManager)
        {
            GameObject marker =
                new("Drift Challenge Marker");
            marker.transform.position =
                challenge.ZoneCenter;

            float coneOffset = 6f;
            Vector3[] offsets =
            {
                new(-coneOffset, 0f, -coneOffset),
                new(coneOffset, 0f, -coneOffset),
                new(-coneOffset, 0f, coneOffset),
                new(coneOffset, 0f, coneOffset)
            };

            foreach (Vector3 offset in offsets)
            {
                GameObject cone =
                    CreateAssetMarker(
                        "Drift Cone",
                        "MotorCity/Environment/DriftCone",
                        challenge.ZoneCenter + offset,
                        2.2f,
                        new Color(1f, 0.48f, 0.06f));

                cone.transform.SetParent(
                    marker.transform,
                    true);
            }

            DriftChallengeMarkerVisual visual =
                marker.AddComponent<DriftChallengeMarkerVisual>();
            visual.Bind(challenge, activityManager);
        }

        private static void CreateStreetSprintMarker(
            StreetSprintActivity sprint,
            ActivityManager activityManager)
        {
            GameObject marker = CreateSprintFlagMarker(
                sprint.CurrentTarget);

            StreetSprintMarkerVisual visual =
                marker.AddComponent<StreetSprintMarkerVisual>();
            visual.Bind(sprint, activityManager);
        }

        private static GameObject CreateSprintFlagMarker(
            Vector3 position)
        {
            GameObject root = new("Street Sprint Marker");
            root.transform.position = position;

            Material poleMaterial =
                Material(new Color(0.12f, 0.13f, 0.15f), 0.35f, 0.45f);

            Primitive(
                "Sprint Flag Pole",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(0.08f, 1.35f, 0.08f),
                new Vector3(0f, 1.35f, 0f),
                poleMaterial,
                false);

            Sprite flagSprite =
                Resources.Load<Sprite>("MotorCity/Markers/flag");

            if (flagSprite != null)
            {
                GameObject flag = new("Sprint Flag");
                flag.transform.SetParent(root.transform, false);
                flag.transform.localPosition = new Vector3(0.68f, 2.25f, 0f);
                flag.transform.localScale = Vector3.one * 1.35f;

                SpriteRenderer renderer =
                    flag.AddComponent<SpriteRenderer>();
                renderer.sprite = flagSprite;
                renderer.color = new Color(0.18f, 1f, 0.34f);
            }
            else
            {
                Material flagMaterial =
                    Material(new Color(0.18f, 1f, 0.34f), 0.02f, 0.55f);

                Primitive(
                    "Sprint Flag Fallback",
                    PrimitiveType.Cube,
                    root.transform,
                    new Vector3(1.35f, 0.75f, 0.08f),
                    new Vector3(0.68f, 2.25f, 0f),
                    flagMaterial,
                    false);
            }

            return root;
        }

        private static void CreateGarageMarker(
            GarageUpgradeSystem garage)
        {
            GameObject marker =
                new("Garage Marker");

            GarageMarkerVisual visual =
                marker.AddComponent<GarageMarkerVisual>();
            visual.Bind(garage);
        }

        private static GameObject CreateAssetMarker(
            string name,
            string resourcePath,
            Vector3 position,
            float targetSize,
            Color fallbackColor)
        {
            GameObject root = new(name);
            root.transform.position = position;

            GameObject prefab =
                Resources.Load<GameObject>(resourcePath);

            if (prefab != null)
            {
                GameObject visual =
                    Object.Instantiate(prefab, root.transform);

                visual.name = "Asset Visual";
                NormalizeAssetVisual(
                    visual.transform,
                    root.transform.position,
                    targetSize);

                foreach (Collider collider in
                         visual.GetComponentsInChildren<Collider>(true))
                    Object.Destroy(collider);

                return root;
            }

            // Emergency fallback only if the editor could not prepare CC0 assets.
            Material material =
                Material(fallbackColor, 0.02f, 0.7f);

            GameObject fallback =
                Primitive(
                    "Fallback Marker",
                    PrimitiveType.Cylinder,
                    root.transform,
                    new Vector3(targetSize, 0.08f, targetSize),
                    Vector3.zero,
                    material,
                    false);

            fallback.isStatic = false;
            return root;
        }

        private static void NormalizeAssetVisual(
            Transform visual,
            Vector3 anchor,
            float targetSize)
        {
            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float maxSize =
                Mathf.Max(
                    bounds.size.x,
                    bounds.size.y,
                    bounds.size.z);

            if (maxSize > 0.001f)
                visual.localScale *= targetSize / maxSize;

            renderers =
                visual.GetComponentsInChildren<Renderer>(true);

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            visual.position += new Vector3(
                anchor.x - bounds.center.x,
                anchor.y - bounds.min.y,
                anchor.z - bounds.center.z);
        }

        private static void CreateCamera(Transform target)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.12f;
            camera.farClipPlane = 3162.5f;
            cameraObject.transform.position = target.position + new Vector3(0f, 2.8f, -6.8f);
            ChaseCamera chase = cameraObject.AddComponent<ChaseCamera>();
            chase.SetTarget(target);
        }

        private static void CreateHud(
            ArcadeCarController car,
            PlayerWallet wallet,
            DriftTracker drift,
            DeliveryActivity delivery,
            DriftChallenge driftChallenge,
            StreetSprintActivity streetSprint,
            ActivityManager activityManager,
            GarageUpgradeSystem garage)
        {
            GameObject hud = new("Prototype HUD");
            PrototypeHud prototypeHud = hud.AddComponent<PrototypeHud>();
            prototypeHud.Bind(
                car,
                wallet,
                drift,
                delivery,
                driftChallenge,
                streetSprint,
                activityManager,
                garage);
        }

        private static GameObject CreateVisualSurface(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = Primitive(name, type, position, scale, material);
            Collider surfaceCollider = go.GetComponent<Collider>();
            if (surfaceCollider != null) Object.Destroy(surfaceCollider);
            go.isStatic = true;
            return go;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 scale, Vector3 localPosition, Material material, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Collider primitiveCollider = go.GetComponent<Collider>();
                if (primitiveCollider != null) Object.Destroy(primitiveCollider);
            }

            return go;
        }

        private static Material Material(Color color, float metallic, float smoothness)
        {
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = Shader.Find(srp ? "Universal Render Pipeline/Lit" : "Standard");
            Material material = new(shader) { color = color };

            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            return material;
        }
    }
}
