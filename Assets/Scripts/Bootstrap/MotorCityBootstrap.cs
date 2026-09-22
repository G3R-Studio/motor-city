using System.Collections.Generic;
using MotorCity.CameraSystem;
using MotorCity.Gameplay;
using MotorCity.Input;
using MotorCity.Persistence;
using MotorCity.Platform;
using MotorCity.UI;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MotorCity.Bootstrap
{
    public static class MotorCityBootstrap
    {
        private static readonly Dictionary<RuntimeMaterialKey, Material> RuntimeMaterialCache =
            new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildPrototype()
        {
            if (SceneManager.GetActiveScene().name !=
                "Prototype")
            {
                return;
            }

            if (Object.FindAnyObjectByType<ArcadeCarController>() != null)
                return;

            MotorCityQualityRuntime.Initialize();
            MotorCityInput.RefreshTouchPromptPreference();

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

            Light sun =
                CreateLighting();

            CreatePrototypeCity();
            CreateDayNightCycle(
                sun);

            ArcadeCarController car = CreateCar();
            ArcadeRacingCarRuntimeInstaller.TryInstallNow(car);
            car.gameObject.AddComponent<PlayerHeadlights>();

            BindFcgTrafficPlayer(
                car.transform);

            VehiclePositionPersistence positionPersistence =
                car.gameObject.AddComponent<VehiclePositionPersistence>();

            DriftTracker drift = car.gameObject.AddComponent<DriftTracker>();

            GameObject systems = new("Gameplay Systems");

            MotorCityPlatformRuntime platformRuntime =
                Object.FindAnyObjectByType<MotorCityPlatformRuntime>();

            if (platformRuntime == null)
            {
                platformRuntime =
                    systems.AddComponent<MotorCityPlatformRuntime>();

                platformRuntime.InitializePlatform();
            }

            if (Object.FindAnyObjectByType<MotorCitySaveRuntime>() ==
                null)
            {
                systems.AddComponent<MotorCitySaveRuntime>();
            }

            PlayerReputation reputation =
                systems.AddComponent<PlayerReputation>();
            ActivityManager activityManager =
                systems.AddComponent<ActivityManager>();
            activityManager.Initialize(
                reputation);

            PlayerWallet wallet =
                systems.AddComponent<PlayerWallet>();

            MotorCityAnalyticsRuntime analytics =
                systems.AddComponent<MotorCityAnalyticsRuntime>();

            analytics.Initialize(
                activityManager,
                wallet);

            DisciplineReputationSystem disciplineReputation =
                systems.AddComponent<DisciplineReputationSystem>();

            disciplineReputation.Initialize(
                activityManager);

            activityManager.SetDisciplineReputation(
                disciplineReputation);

            CareerProgressionSystem career =
                systems.AddComponent<CareerProgressionSystem>();

            career.Initialize(
                activityManager,
                wallet,
                reputation);

            CityContractSystem contracts =
                systems.AddComponent<CityContractSystem>();

            contracts.Initialize(
                activityManager,
                wallet,
                reputation);

            CityLiveEventSystem liveEvents =
                systems.AddComponent<CityLiveEventSystem>();

            liveEvents.Initialize(
                activityManager,
                wallet,
                reputation);

            UndergroundSceneSystem underground =
                systems.AddComponent<UndergroundSceneSystem>();

            underground.Initialize(
                activityManager,
                wallet,
                reputation,
                car);

            drift.Initialize(wallet, activityManager);

            VehicleRosterSystem vehicleRoster =
                systems.AddComponent<VehicleRosterSystem>();
            vehicleRoster.Initialize(
                car,
                reputation,
                wallet);

            VehicleCustomizationSystem customization =
                systems.AddComponent<VehicleCustomizationSystem>();

            customization.Initialize(
                car,
                vehicleRoster);

            VehicleMasterySystem vehicleMastery =
                systems.AddComponent<VehicleMasterySystem>();

            vehicleMastery.Initialize(
                activityManager,
                vehicleRoster,
                car);

            VehicleSpecializationSystem vehicleSpecialization =
                systems.AddComponent<VehicleSpecializationSystem>();

            vehicleSpecialization.Initialize(
                activityManager,
                wallet,
                vehicleRoster,
                vehicleMastery);

            VehicleHistorySystem vehicleHistory =
                systems.AddComponent<VehicleHistorySystem>();

            vehicleHistory.Initialize(
                car,
                vehicleRoster,
                activityManager);

            CollectionProgressionSystem collection =
                systems.AddComponent<CollectionProgressionSystem>();

            collection.Initialize(
                wallet,
                reputation,
                vehicleRoster,
                vehicleMastery,
                vehicleHistory);

            CityLegendSystem legends =
                systems.AddComponent<CityLegendSystem>();

            legends.Initialize(
                activityManager,
                wallet,
                reputation,
                disciplineReputation,
                vehicleMastery);

            positionPersistence.RestoreSavedPosition();

            DeliveryActivity delivery = systems.AddComponent<DeliveryActivity>();
            delivery.Initialize(car, wallet, activityManager);

            DriftChallenge driftChallenge = systems.AddComponent<DriftChallenge>();
            driftChallenge.Initialize(car, drift, wallet, activityManager);

            StreetSprintActivity streetSprint = systems.AddComponent<StreetSprintActivity>();
            streetSprint.Initialize(car, wallet, activityManager);

            CircuitRaceActivity circuitRace = systems.AddComponent<CircuitRaceActivity>();
            circuitRace.Initialize(car, wallet, activityManager);

            SpeedTrapSystem speedTraps =
                systems.AddComponent<SpeedTrapSystem>();
            speedTraps.Initialize(
                car,
                wallet,
                reputation,
                activityManager);

            DriftSpotSystem driftSpots =
                systems.AddComponent<DriftSpotSystem>();
            driftSpots.Initialize(
                car,
                drift,
                wallet,
                reputation,
                activityManager);

            DiscoverySystem discoveries =
                systems.AddComponent<DiscoverySystem>();
            discoveries.Initialize(
                car,
                wallet,
                reputation,
                activityManager);

            TurboPetSystem turbo =
                systems.AddComponent<TurboPetSystem>();

            turbo.Initialize(
                car,
                wallet,
                activityManager,
                discoveries);

            DailyAdventureSystem dailyAdventures =
                systems.AddComponent<DailyAdventureSystem>();

            dailyAdventures.Initialize(
                activityManager,
                wallet,
                turbo);

            StuntJumpSystem stuntJumps =
                systems.AddComponent<StuntJumpSystem>();
            stuntJumps.Initialize(
                car,
                wallet,
                reputation,
                activityManager);

            GarageUpgradeSystem garage = systems.AddComponent<GarageUpgradeSystem>();
            garage.Initialize(
                car,
                wallet,
                activityManager,
                delivery,
                driftChallenge,
                streetSprint,
                circuitRace,
                vehicleRoster,
                vehicleMastery,
                turbo,
                customization);

            CityRiskSystem cityRisk =
                systems.AddComponent<CityRiskSystem>();

            cityRisk.Initialize(
                car,
                activityManager,
                garage,
                vehicleRoster,
                underground);

            FirstSessionOnboardingSystem onboarding =
                systems.AddComponent<FirstSessionOnboardingSystem>();

            onboarding.Initialize(
                car,
                wallet,
                reputation,
                activityManager,
                garage,
                turbo,
                customization);

            StoryMissionSystem story =
                systems.AddComponent<StoryMissionSystem>();

            story.Initialize(
                activityManager,
                wallet,
                reputation,
                turbo);

            SeasonSystem season =
                systems.AddComponent<SeasonSystem>();

            season.Initialize(
                activityManager,
                wallet,
                reputation,
                turbo);

            PhotoHuntSystem photoHunt =
                systems.AddComponent<PhotoHuntSystem>();

            photoHunt.Initialize(
                car,
                wallet,
                reputation,
                discoveries,
                customization);

            CityProfessionSystem professions =
                systems.AddComponent<CityProfessionSystem>();

            professions.Initialize(
                car,
                wallet,
                activityManager,
                turbo);

            CarWashJobSystem carWash =
                systems.AddComponent<CarWashJobSystem>();

            carWash.Initialize(
                car,
                wallet,
                activityManager,
                professions);

            TowTruckJobSystem towTruck =
                systems.AddComponent<TowTruckJobSystem>();

            towTruck.Initialize(
                car,
                wallet,
                activityManager,
                professions);

            ClubSystem club =
                systems.AddComponent<ClubSystem>();

            club.Initialize(
                activityManager,
                wallet,
                reputation);

            WeekendEventSystem weekendEvents =
                systems.AddComponent<WeekendEventSystem>();

            weekendEvents.Initialize(
                activityManager,
                wallet);

            RewardedBonusSystem rewardedBonus =
                systems.AddComponent<RewardedBonusSystem>();

            rewardedBonus.Initialize(
                wallet,
                activityManager);

            CosmeticStoreSystem cosmeticStore =
                systems.AddComponent<CosmeticStoreSystem>();

            cosmeticStore.Initialize(
                turbo,
                season);

            LeaderboardSyncSystem leaderboardSync =
                systems.AddComponent<LeaderboardSyncSystem>();

            leaderboardSync.Initialize(
                reputation,
                collection,
                activityManager);

            AchievementSystem achievements =
                systems.AddComponent<AchievementSystem>();

            achievements.Initialize(
                wallet,
                reputation,
                activityManager,
                discoveries,
                photoHunt,
                vehicleRoster,
                story,
                season,
                professions);

            AdventureDirector adventureDirector =
                systems.AddComponent<AdventureDirector>();

            adventureDirector.Initialize(
                activityManager,
                career,
                contracts,
                liveEvents,
                legends,
                underground,
                cityRisk,
                turbo,
                onboarding,
                dailyAdventures,
                story,
                season);

            AdminDebugPanel adminPanel =
                systems.AddComponent<AdminDebugPanel>();

            adminPanel.Initialize(
                wallet,
                reputation,
                disciplineReputation,
                vehicleMastery,
                vehicleRoster,
                garage,
                career,
                vehicleHistory,
                vehicleSpecialization,
                collection,
                legends,
                contracts,
                liveEvents,
                underground,
                cityRisk,
                activityManager,
                car,
                delivery,
                driftChallenge,
                streetSprint,
                circuitRace);

            CreateDeliveryMarker(delivery, activityManager);
            CreateDriftChallengeMarker(driftChallenge, activityManager);
            CreateStreetSprintMarker(streetSprint, activityManager);
            CreateCircuitRaceMarker(circuitRace, activityManager);
            // Speed traps and drift spots are passive drive-through systems.
            // Their old primitive frames/rings added permanent visual clutter and
            // overlapped the primary activity markers, so keep the gameplay but
            // do not spawn separate world geometry for them.
            // CreateSpeedTrapMarkers(speedTraps);
            // CreateDriftSpotMarkers(driftSpots);
            CreateDiscoveryMarkers(discoveries);
            CreateStuntJumpRamps(stuntJumps);
            CreateGarageMarker(garage);
            CreateUndergroundMarker(underground);
            CreateUndergroundCheckpointMarker(underground);
            CreateProfessionMarkers(professions);
            CreateProfessionCheckpointMarker(professions);
            CreateCarWashMarker(carWash);
            CreateTowTruckMarker(towTruck);
            CreateTowCheckpointMarker(towTruck);

            CreateCamera(car.transform);
            CreateHud(
                car,
                wallet,
                drift,
                delivery,
                driftChallenge,
                streetSprint,
                circuitRace,
                speedTraps,
                driftSpots,
                discoveries,
                stuntJumps,
                activityManager,
                garage,
                career,
                vehicleHistory,
                vehicleSpecialization,
                collection,
                legends,
                contracts,
                liveEvents,
                underground,
                cityRisk,
                turbo,
                onboarding,
                dailyAdventures,
                story,
                season,
                photoHunt,
                professions,
                carWash,
                towTruck,
                club,
                weekendEvents,
                rewardedBonus,
                cosmeticStore,
                achievements,
                adventureDirector);

            MotorCityBootController bootController =
                Object.FindAnyObjectByType<MotorCityBootController>();

            bootController?.NotifyGameplayBuilt();

            MotorCityPlatform.GameReady();
            platformRuntime.MarkGameplayRunning();
            MotorCityPlatform.GameplayStart();
        }

        private static void BindFcgTrafficPlayer(
            Transform player)
        {
            if (player == null)
                return;

            GameObject cityRoot =
                GameObject.Find(
                    "MotorCity_FCGCity") ??
                GameObject.Find(
                    "City-Maker");

            if (cityRoot == null)
                return;

            foreach (MonoBehaviour behaviour in
                     cityRoot.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                    continue;

                System.Type type =
                    behaviour.GetType();

                if (!string.Equals(
                        type.FullName,
                        "FCG.TrafficSystem",
                        System.StringComparison.Ordinal))
                {
                    continue;
                }

                System.Reflection.FieldInfo playerField =
                    type.GetField(
                        "player");

                if (playerField == null ||
                    !typeof(Transform).IsAssignableFrom(
                        playerField.FieldType))
                {
                    Debug.LogWarning(
                        "Motor City: FCG TrafficSystem was found, but its public player field is unavailable.");

                    return;
                }

                playerField.SetValue(
                    behaviour,
                    player);

                return;
            }
        }

        private static Light CreateLighting()
        {
            GameObject sunObject = new("Sun");
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.05f;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

            return sun;
        }

        private static void CreateDayNightCycle(
            Light sun)
        {
            GameObject cycleObject =
                new("Day Night Cycle");

            DayNightCycleController cycle =
                cycleObject.AddComponent<DayNightCycleController>();

            cycle.Initialize(
                sun);
        }

        private static void CreatePrototypeCity()
        {
            if (CityAssetRuntimeInstaller.TryInstall())
                return;

            Debug.LogWarning(
                "Motor City: Fantastic City Generator runtime city is missing. " +
                "Generate City-Maker and bake it through the Motor City FCG tool.");

            Material asphalt =
                Material(
                    new Color(0.045f, 0.048f, 0.055f),
                    0.08f,
                    0.23f);

            GameObject ground =
                Primitive(
                    "Temporary City Test Surface",
                    PrimitiveType.Cube,
                    new Vector3(0f, -0.52f, 0f),
                    new Vector3(500f, 1f, 500f),
                    asphalt);

            ground.isStatic = true;
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
            car.transform.position =
                CityAssetRuntimeInstaller.PlayerSpawnPoint +
                Vector3.up * 1.2f;
            car.transform.rotation =
                CityAssetRuntimeInstaller.PlayerSpawnRotation;
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

            ArcadeCarController controller =
                car.AddComponent<ArcadeCarController>();
            car.AddComponent<HandbrakePhysicsAssist>();
            car.AddComponent<DriftEffects>();
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

        private static void CreateCircuitRaceMarker(
            CircuitRaceActivity race,
            ActivityManager activityManager)
        {
            GameObject marker =
                CreateCircuitFlagMarker(
                    race.CurrentTarget);

            CircuitRaceMarkerVisual visual =
                marker.AddComponent<CircuitRaceMarkerVisual>();

            visual.Bind(
                race,
                activityManager);
        }

        private static GameObject CreateCircuitFlagMarker(
            Vector3 position)
        {
            GameObject root =
                new("Circuit Race Marker");

            root.transform.position =
                position;

            Material poleMaterial =
                Material(
                    new Color(0.10f, 0.13f, 0.16f),
                    0.35f,
                    0.45f);

            Primitive(
                "Circuit Flag Pole",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(0.08f, 1.35f, 0.08f),
                new Vector3(0f, 1.35f, 0f),
                poleMaterial,
                false);

            Sprite flagSprite =
                Resources.Load<Sprite>(
                    "MotorCity/Markers/flag");

            Color circuitColor =
                new(0.08f, 0.9f, 1f);

            if (flagSprite != null)
            {
                GameObject flag =
                    new("Circuit Flag");

                flag.transform.SetParent(
                    root.transform,
                    false);

                flag.transform.localPosition =
                    new Vector3(
                        0.68f,
                        2.25f,
                        0f);

                flag.transform.localScale =
                    Vector3.one *
                    1.35f;

                SpriteRenderer renderer =
                    flag.AddComponent<SpriteRenderer>();

                renderer.sprite =
                    flagSprite;

                renderer.color =
                    circuitColor;
            }
            else
            {
                Material flagMaterial =
                    Material(
                        circuitColor,
                        0.02f,
                        0.55f);

                Primitive(
                    "Circuit Flag Fallback",
                    PrimitiveType.Cube,
                    root.transform,
                    new Vector3(1.35f, 0.75f, 0.08f),
                    new Vector3(0.68f, 2.25f, 0f),
                    flagMaterial,
                    false);
            }

            return root;
        }

        private static void CreateCarWashMarker(
            CarWashJobSystem carWash)
        {
            if (carWash == null)
                return;

            Material material =
                Material(
                    new Color(
                        0.16f,
                        0.82f,
                        1f),
                    0.02f,
                    0.55f);

            GameObject root =
                new(
                    "Profession Car Wash");

            root.transform.position =
                carWash.StartPoint;

            Primitive(
                "Car Wash Base",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(
                    2.5f,
                    0.06f,
                    2.5f),
                new Vector3(
                    0f,
                    0.08f,
                    0f),
                material,
                false);

            Primitive(
                "Car Wash Beacon",
                PrimitiveType.Cube,
                root.transform,
                new Vector3(
                    0.2f,
                    2.4f,
                    0.2f),
                new Vector3(
                    0f,
                    2.4f,
                    0f),
                material,
                false);
        }

        private static void CreateTowTruckMarker(
            TowTruckJobSystem towTruck)
        {
            if (towTruck == null)
                return;

            Material material =
                Material(
                    new Color(
                        1f,
                        0.62f,
                        0.08f),
                    0.04f,
                    0.62f);

            GameObject root =
                new(
                    "Profession Tow Service");

            root.transform.position =
                towTruck.StartPoint;

            Primitive(
                "Tow Service Base",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(
                    2.4f,
                    0.06f,
                    2.4f),
                new Vector3(
                    0f,
                    0.08f,
                    0f),
                material,
                false);

            Primitive(
                "Tow Service Beacon",
                PrimitiveType.Cube,
                root.transform,
                new Vector3(
                    0.22f,
                    2.2f,
                    0.22f),
                new Vector3(
                    0f,
                    2.2f,
                    0f),
                material,
                false);
        }

        private static void CreateProfessionMarkers(
            CityProfessionSystem professions)
        {
            if (professions == null)
                return;

            Color[] colors =
            {
                new(1f, 0.42f, 0.12f),
                new(0.18f, 0.72f, 1f),
                new(0.28f, 0.92f, 0.42f),
                new(1f, 0.78f, 0.18f)
            };

            for (int i = 0;
                 i < professions.StartCount;
                 i++)
            {
                Material material =
                    Material(
                        colors[
                            i %
                            colors.Length],
                        0.02f,
                        0.55f);

                GameObject root =
                    new(
                        "Profession " +
                        professions.GetStartName(i));

                root.transform.position =
                    professions.GetStartPoint(i);

                Primitive(
                    "Profession Base",
                    PrimitiveType.Cylinder,
                    root.transform,
                    new Vector3(
                        2.2f,
                        0.06f,
                        2.2f),
                    new Vector3(
                        0f,
                        0.08f,
                        0f),
                    material,
                    false);

                Primitive(
                    "Profession Beacon",
                    PrimitiveType.Cylinder,
                    root.transform,
                    new Vector3(
                        0.12f,
                        1.8f,
                        0.12f),
                    new Vector3(
                        0f,
                        1.8f,
                        0f),
                    material,
                    false);
            }
        }

        private static void CreateUndergroundCheckpointMarker(
            UndergroundSceneSystem underground)
        {
            if (underground == null)
                return;

            GameObject root =
                new(
                    "Underground Active Checkpoint");

            DynamicActivityCheckpointVisual visual =
                root.AddComponent<DynamicActivityCheckpointVisual>();

            visual.BindUnderground(
                underground);
        }

        private static void CreateProfessionCheckpointMarker(
            CityProfessionSystem professions)
        {
            if (professions == null)
                return;

            GameObject root =
                new(
                    "Profession Active Checkpoint");

            DynamicActivityCheckpointVisual visual =
                root.AddComponent<DynamicActivityCheckpointVisual>();

            visual.BindProfession(
                professions);
        }

        private static void CreateTowCheckpointMarker(
            TowTruckJobSystem towTruck)
        {
            if (towTruck == null)
                return;

            GameObject root =
                new(
                    "Tow Active Checkpoint");

            DynamicActivityCheckpointVisual visual =
                root.AddComponent<DynamicActivityCheckpointVisual>();

            visual.BindTowTruck(
                towTruck);
        }

        private static void CreateSpeedTrapMarkers(
            SpeedTrapSystem speedTraps)
        {
            if (speedTraps == null)
                return;

            Material frameMaterial =
                Material(
                    new Color(
                        0.08f,
                        0.78f,
                        1f),
                    0.08f,
                    0.72f);

            Material cameraMaterial =
                Material(
                    new Color(
                        0.04f,
                        0.05f,
                        0.07f),
                    0.45f,
                    0.42f);

            for (int i = 0;
                 i < speedTraps.TrapCount;
                 i++)
            {
                GameObject root =
                    new(
                        $"Speed Trap {i + 1}");

                root.transform.position =
                    speedTraps.GetTrapPosition(i);

                root.transform.rotation =
                    speedTraps.GetTrapRotation(i);

                Primitive(
                    "Left Post",
                    PrimitiveType.Cylinder,
                    root.transform,
                    new Vector3(
                        0.18f,
                        2.6f,
                        0.18f),
                    new Vector3(
                        -5.2f,
                        2.6f,
                        0f),
                    frameMaterial,
                    false);

                Primitive(
                    "Right Post",
                    PrimitiveType.Cylinder,
                    root.transform,
                    new Vector3(
                        0.18f,
                        2.6f,
                        0.18f),
                    new Vector3(
                        5.2f,
                        2.6f,
                        0f),
                    frameMaterial,
                    false);

                Primitive(
                    "Top Beam",
                    PrimitiveType.Cube,
                    root.transform,
                    new Vector3(
                        10.6f,
                        0.18f,
                        0.18f),
                    new Vector3(
                        0f,
                        5.15f,
                        0f),
                    frameMaterial,
                    false);

                Primitive(
                    "Radar Camera",
                    PrimitiveType.Cube,
                    root.transform,
                    new Vector3(
                        0.7f,
                        0.5f,
                        0.9f),
                    new Vector3(
                        0f,
                        4.65f,
                        0.35f),
                    cameraMaterial,
                    false);
            }
        }

        private static void CreateDriftSpotMarkers(
            DriftSpotSystem driftSpots)
        {
            if (driftSpots == null)
                return;

            Material ringMaterial =
                Material(
                    new Color(
                        1f,
                        0.36f,
                        0.08f),
                    0.02f,
                    0.72f);

            for (int i = 0;
                 i < driftSpots.SpotCount;
                 i++)
            {
                GameObject root =
                    new($"Drift Spot {i + 1}");

                root.transform.position =
                    driftSpots.GetSpotPosition(i) +
                    Vector3.up * 0.08f;

                GameObject ring =
                    Primitive(
                        "Drift Ring",
                        PrimitiveType.Cylinder,
                        root.transform,
                        new Vector3(
                            8f,
                            0.025f,
                            8f),
                        Vector3.zero,
                        ringMaterial,
                        false);

                ring.transform.localScale =
                    new Vector3(
                        8f,
                        0.025f,
                        8f);
            }
        }

        private static void CreateDiscoveryMarkers(
            DiscoverySystem discoveries)
        {
            if (discoveries == null)
                return;

            Material beaconMaterial =
                Material(
                    new Color(
                        0.72f,
                        0.28f,
                        1f),
                    0.02f,
                    0.72f);

            for (int i = 0;
                 i < discoveries.DiscoveryCount;
                 i++)
            {
                if (discoveries.IsFound(i))
                    continue;

                GameObject root =
                    new($"Discovery {i + 1}");

                root.transform.position =
                    discoveries.GetDiscoveryPosition(i);

                Primitive(
                    "Discovery Base",
                    PrimitiveType.Cylinder,
                    root.transform,
                    new Vector3(
                        1.3f,
                        0.08f,
                        1.3f),
                    new Vector3(
                        0f,
                        0.08f,
                        0f),
                    beaconMaterial,
                    false);

                Primitive(
                    "Discovery Beacon",
                    PrimitiveType.Cylinder,
                    root.transform,
                    new Vector3(
                        0.13f,
                        2.4f,
                        0.13f),
                    new Vector3(
                        0f,
                        2.4f,
                        0f),
                    beaconMaterial,
                    false);
            }
        }

        private static void CreateStuntJumpRamps(
            StuntJumpSystem stuntJumps)
        {
            if (stuntJumps == null)
                return;

            Material rampMaterial =
                Material(
                    new Color(
                        0.92f,
                        0.72f,
                        0.08f),
                    0.10f,
                    0.54f);

            Material stripeMaterial =
                Material(
                    new Color(
                        0.08f,
                        0.09f,
                        0.11f),
                    0.04f,
                    0.42f);

            for (int i = 0;
                 i < stuntJumps.JumpCount;
                 i++)
            {
                GameObject root =
                    new(
                        $"Stunt Jump {i + 1}");

                root.transform.position =
                    stuntJumps.GetJumpPosition(i);

                root.transform.rotation =
                    stuntJumps.GetJumpRotation(i);

                GameObject ramp =
                    Primitive(
                        "Ramp",
                        PrimitiveType.Cube,
                        root.transform,
                        new Vector3(
                            4.4f,
                            0.45f,
                            8.2f),
                        new Vector3(
                            0f,
                            0.72f,
                            0f),
                        rampMaterial,
                        true);

                ramp.transform.localRotation =
                    Quaternion.Euler(
                        -11.5f,
                        0f,
                        0f);

                for (int stripe = -1;
                     stripe <= 1;
                     stripe++)
                {
                    GameObject marker =
                        Primitive(
                            "Ramp Stripe",
                            PrimitiveType.Cube,
                            root.transform,
                            new Vector3(
                                0.34f,
                                0.05f,
                                7.5f),
                            new Vector3(
                                stripe * 1.35f,
                                1.13f,
                                0.05f),
                            stripeMaterial,
                            false);

                    marker.transform.localRotation =
                        Quaternion.Euler(
                            -11.5f,
                            0f,
                            0f);
                }
            }
        }

        private static void CreateUndergroundMarker(
            UndergroundSceneSystem underground)
        {
            if (underground == null)
                return;

            GameObject root =
                new("Underground Marker");

            root.transform.position =
                CityAssetRuntimeInstaller.UndergroundMeetingPoint;

            Material baseMaterial =
                Material(
                    new Color(
                        0.26f,
                        0.03f,
                        0.34f),
                    0.08f,
                    0.66f);

            Material glowMaterial =
                Material(
                    new Color(
                        0.74f,
                        0.12f,
                        1f),
                    0.02f,
                    0.78f);

            Primitive(
                "Underground Ring",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(
                    5.6f,
                    0.035f,
                    5.6f),
                new Vector3(
                    0f,
                    0.07f,
                    0f),
                baseMaterial,
                false);

            Primitive(
                "Underground Beacon",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(
                    0.12f,
                    2.8f,
                    0.12f),
                new Vector3(
                    0f,
                    2.8f,
                    0f),
                glowMaterial,
                false);

            UndergroundMarkerVisual visual =
                root.AddComponent<UndergroundMarkerVisual>();

            visual.Bind(
                underground);
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
            camera.farClipPlane = 2200f;
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
            CircuitRaceActivity circuitRace,
            SpeedTrapSystem speedTraps,
            DriftSpotSystem driftSpots,
            DiscoverySystem discoveries,
            StuntJumpSystem stuntJumps,
            ActivityManager activityManager,
            GarageUpgradeSystem garage,
            CareerProgressionSystem career,
            VehicleHistorySystem vehicleHistory,
            VehicleSpecializationSystem vehicleSpecialization,
            CollectionProgressionSystem collection,
            CityLegendSystem legends,
            CityContractSystem contracts,
            CityLiveEventSystem liveEvents,
            UndergroundSceneSystem underground,
            CityRiskSystem cityRisk,
            TurboPetSystem turbo,
            FirstSessionOnboardingSystem onboarding,
            DailyAdventureSystem dailyAdventures,
            StoryMissionSystem story,
            SeasonSystem season,
            PhotoHuntSystem photoHunt,
            CityProfessionSystem professions,
            CarWashJobSystem carWash,
            TowTruckJobSystem towTruck,
            ClubSystem club,
            WeekendEventSystem weekendEvents,
            RewardedBonusSystem rewardedBonus,
            CosmeticStoreSystem cosmeticStore,
            AchievementSystem achievements,
            AdventureDirector adventureDirector)
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
                circuitRace,
                speedTraps,
                driftSpots,
                discoveries,
                stuntJumps,
                activityManager,
                garage,
                career,
                vehicleHistory,
                vehicleSpecialization,
                collection,
                legends,
                contracts,
                liveEvents,
                underground,
                cityRisk,
                turbo,
                onboarding,
                dailyAdventures,
                story,
                season,
                photoHunt,
                professions,
                carWash,
                towTruck,
                club,
                weekendEvents,
                rewardedBonus,
                cosmeticStore,
                achievements,
                adventureDirector);
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

        private static Material Material(
            Color color,
            float metallic,
            float smoothness)
        {
            RuntimeMaterialKey key =
                new(
                    color,
                    metallic,
                    smoothness);

            if (RuntimeMaterialCache.TryGetValue(
                    key,
                    out Material cached) &&
                cached != null)
            {
                return cached;
            }

            bool srp =
                GraphicsSettings.currentRenderPipeline != null;

            Shader shader =
                Shader.Find(
                    srp
                        ? "Universal Render Pipeline/Lit"
                        : "Standard");

            Material material =
                new(shader)
                {
                    color = color
                };

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);

            RuntimeMaterialCache[key] =
                material;

            return material;
        }

        private readonly struct RuntimeMaterialKey :
            System.IEquatable<RuntimeMaterialKey>
        {
            private readonly Color32 color;
            private readonly byte metallic;
            private readonly byte smoothness;

            public RuntimeMaterialKey(
                Color sourceColor,
                float sourceMetallic,
                float sourceSmoothness)
            {
                color =
                    sourceColor;

                metallic =
                    (byte)Mathf.RoundToInt(
                        Mathf.Clamp01(
                            sourceMetallic) *
                        255f);

                smoothness =
                    (byte)Mathf.RoundToInt(
                        Mathf.Clamp01(
                            sourceSmoothness) *
                        255f);
            }

            public bool Equals(
                RuntimeMaterialKey other)
            {
                return
                    color.Equals(
                        other.color) &&
                    metallic ==
                        other.metallic &&
                    smoothness ==
                        other.smoothness;
            }

            public override bool Equals(
                object obj)
            {
                return
                    obj is RuntimeMaterialKey other &&
                    Equals(
                        other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash =
                        color.GetHashCode();

                    hash =
                        hash * 397 ^
                        metallic;

                    hash =
                        hash * 397 ^
                        smoothness;

                    return hash;
                }
            }
        }
    }
}
