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
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MotorCity.Bootstrap
{
    public static class MotorCityBootstrap
    {
        private static readonly Dictionary<RuntimeMaterialKey, Material> RuntimeMaterialCache =
            new();

        private static bool platformBootstrapReady;
        private static bool platformBootstrapPending;
        private static bool gameplayBuildRequested;
        private static bool gameplayBuildStarted;
        private static bool platformGameReadySent;
        private static MotorCityFrontEndFlow frontEnd;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            foreach (Material material in
                     RuntimeMaterialCache.Values)
            {
                if (material != null)
                {
                    Object.Destroy(
                        material);
                }
            }

            RuntimeMaterialCache.Clear();

            platformBootstrapReady =
                false;

            platformBootstrapPending =
                false;

            gameplayBuildRequested =
                false;

            gameplayBuildStarted =
                false;

            platformGameReadySent =
                false;

            frontEnd =
                null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeBootstrap()
        {
            SceneManager.sceneLoaded -=
                OnSceneLoaded;

            SceneManager.sceneLoaded +=
                OnSceneLoaded;

            TryBuildPrototype(
                SceneManager.GetActiveScene());
        }


        private static void EnsurePlatformBootstrap(
            Scene scene)
        {
            if (platformBootstrapPending)
                return;

            platformBootstrapPending = true;

            GameObject host =
                GameObject.Find(
                    "Motor City Platform Systems");

            if (host == null)
            {
                host =
                    new GameObject(
                        "Motor City Platform Systems");

                Object.DontDestroyOnLoad(
                    host);
            }

            MotorCityPlatformRuntime platformRuntime =
                host.GetComponent<MotorCityPlatformRuntime>();

            if (platformRuntime == null)
            {
                platformRuntime =
                    host.AddComponent<MotorCityPlatformRuntime>();
            }

            if (host.GetComponent<MotorCitySaveRuntime>() ==
                null)
            {
                host.AddComponent<MotorCitySaveRuntime>();
            }

            MotorCityCloudSaveRuntime cloudRuntime =
                host.GetComponent<MotorCityCloudSaveRuntime>();

            if (cloudRuntime == null)
            {
                cloudRuntime =
                    host.AddComponent<MotorCityCloudSaveRuntime>();
            }

            MotorCityPurchaseRuntime purchaseRuntime =
                host.GetComponent<MotorCityPurchaseRuntime>();

            if (purchaseRuntime == null)
            {
                purchaseRuntime =
                    host.AddComponent<MotorCityPurchaseRuntime>();
            }

            MotorCityRemoteConfigRuntime remoteConfigRuntime =
                host.GetComponent<MotorCityRemoteConfigRuntime>();

            if (remoteConfigRuntime == null)
            {
                remoteConfigRuntime =
                    host.AddComponent<MotorCityRemoteConfigRuntime>();
            }

            platformRuntime.InitializePlatform(
                _ =>
                {
                    remoteConfigRuntime.Load(
                        () =>
                        {
                            purchaseRuntime.RefreshPending(
                                () =>
                                {
                                    cloudRuntime.ResolveInitialCloud(
                                        () =>
                                        {
                                            platformBootstrapPending =
                                                false;

                                            platformBootstrapReady =
                                                true;

                                            TryBuildPrototype(
                                                scene);
                                        });
                                });
                        });
                });
        }


        private static void RequestGameplayBuild(
            Scene scene)
        {
            if (gameplayBuildRequested)
                return;

            gameplayBuildRequested = true;

            TryBuildPrototype(
                scene);
        }

        private static void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            TryBuildPrototype(
                scene);
        }

        private static void TryBuildPrototype(
            Scene scene)
        {
            if (!scene.IsValid() ||
                scene.name !=
                "Prototype")
            {
                return;
            }

            if (Object.FindAnyObjectByType<ArcadeCarController>() != null)
                return;

            if (!platformBootstrapReady)
            {
                EnsurePlatformBootstrap(
                    scene);
                return;
            }

            if (frontEnd == null)
            {
                GameObject frontEndHost =
                    GameObject.Find(
                        "Motor City Front End Systems");

                if (frontEndHost == null)
                {
                    frontEndHost =
                        new GameObject(
                            "Motor City Front End Systems");
                }

                frontEnd =
                    frontEndHost.GetComponent<
                        MotorCityFrontEndFlow>();

                if (frontEnd == null)
                {
                    frontEnd =
                        frontEndHost.AddComponent<
                            MotorCityFrontEndFlow>();
                }

                frontEnd.Initialize(
                    null,
                    null,
                    null,
                    null,
                    null,
                    () =>
                        RequestGameplayBuild(
                            scene));

                NotifyPlatformGameReady();
            }

            if (!gameplayBuildRequested ||
                gameplayBuildStarted)
            {
                return;
            }

            gameplayBuildStarted = true;

            MotorCityQualityRuntime.Initialize();
            MotorCityInput.RefreshTouchPromptPreference();

            Time.fixedDeltaTime = 0.02f;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
            Physics.defaultContactOffset = 0.01f;
            Physics.defaultSolverIterations = 10;
            Physics.defaultSolverVelocityIterations = 3;

            CreatePrototypeCity();
            CreateDayNightCycle();

            ArcadeCarController car = CreateCar();

            bool playOpeningPresentation =
                frontEnd == null ||
                frontEnd.OpeningPresentationRequested;

            if (playOpeningPresentation)
            {
                car.BeginOpeningPresentationLock(
                    5f);
            }

            ArcadeRacingCarRuntimeInstaller.TryInstallNow(car);
            car.gameObject.AddComponent<PlayerHeadlights>();
            car.gameObject.AddComponent<PlayerVehicleRearEmission>();

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

            ActivityStartFlow activityStartFlow =
                systems.AddComponent<ActivityStartFlow>();

            activityStartFlow.Initialize(
                activityManager);

            activityManager.SetStartFlow(
                activityStartFlow);

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
                reputation);

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
                activityManager,
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
                turbo,
                onboarding);

            SeasonSystem season =
                systems.AddComponent<SeasonSystem>();

            season.Initialize(
                activityManager,
                wallet,
                reputation,
                turbo,
                onboarding,
                story);

            ResultNextGoalResolver resultNextGoalResolver =
                systems.AddComponent<ResultNextGoalResolver>();

            resultNextGoalResolver.Initialize(
                onboarding,
                story,
                vehicleRoster,
                reputation,
                dailyAdventures,
                season,
                liveEvents,
                null);

            activityManager.SetResultNextGoalResolver(
                resultNextGoalResolver);

            PhotoHuntSystem photoHunt =
                systems.AddComponent<PhotoHuntSystem>();

            photoHunt.Initialize(
                car,
                wallet,
                reputation,
                activityManager,
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

            garage.ConfigureCancelableActivities(
                underground,
                professions,
                carWash,
                towTruck);

            ClubSystem club =
                systems.AddComponent<ClubSystem>();

            club.Initialize(
                activityManager,
                wallet,
                reputation);

            activityManager.SetResultProgressSystems(
                vehicleMastery,
                dailyAdventures,
                season,
                club);

            RewardedBonusSystem rewardedBonus =
                systems.AddComponent<RewardedBonusSystem>();

            rewardedBonus.Initialize(
                wallet,
                activityManager);

            MotorCityInterstitialRuntime interstitialRuntime =
                systems.AddComponent<MotorCityInterstitialRuntime>();

            interstitialRuntime.Initialize(
                activityManager);

            activityStartFlow.SetInterstitialRuntime(
                interstitialRuntime);

            CosmeticStoreSystem cosmeticStore =
                systems.AddComponent<CosmeticStoreSystem>();

            cosmeticStore.Initialize();

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

            resultNextGoalResolver.Initialize(
                onboarding,
                story,
                vehicleRoster,
                reputation,
                dailyAdventures,
                season,
                liveEvents,
                adventureDirector);

#if UNITY_EDITOR
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
                circuitRace,
                story,
                onboarding);
#endif

            CreateDeliveryMarker(delivery, activityManager);
            CreateDriftChallengeMarker(driftChallenge, activityManager);
            CreateStreetSprintMarker(streetSprint, activityManager);
            CreateCircuitRaceMarker(circuitRace, activityManager);
            CreateDiscoveryMarkers(discoveries);
            CreateGarageMarker(garage);
            CreateUndergroundMarker(underground);
            CreateUndergroundCheckpointMarker(underground);
            CreateProfessionMarkers(professions);
            CreateProfessionCheckpointMarker(professions);
            CreateCarWashMarker(carWash);
            CreateTowTruckMarker(towTruck);
            CreateTowCheckpointMarker(towTruck);

            CreateCamera(
                car.transform,
                playOpeningPresentation);

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
                customization,
                professions,
                carWash,
                towTruck,
                club,
                rewardedBonus,
                cosmeticStore,
                achievements,
                adventureDirector,
                playOpeningPresentation);

            PrototypeHud runtimeHud =
                Object.FindAnyObjectByType<PrototypeHud>();

            if (frontEnd == null)
            {
                frontEnd =
                    Object.FindAnyObjectByType<
                        MotorCityFrontEndFlow>();
            }

            frontEnd?.AttachGameplay(
                car,
                wallet,
                reputation,
                onboarding,
                runtimeHud);
        }

        private static void NotifyPlatformGameReady()
        {
            if (platformGameReadySent)
                return;

            platformGameReadySent =
                true;

            MotorCityPlatform.GameReady();
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

                TrafficQualityAdapter qualityAdapter =
                    behaviour.GetComponent<TrafficQualityAdapter>();

                if (qualityAdapter == null)
                {
                    qualityAdapter =
                        behaviour.gameObject.AddComponent<
                            TrafficQualityAdapter>();
                }

                qualityAdapter.Bind(
                    behaviour);

                return;
            }
        }

        private static void CreateDayNightCycle()
        {
            GameObject cycleObject =
                new("Day Night Cycle");

            DayNightCycleController cycle =
                cycleObject.AddComponent<DayNightCycleController>();

            cycle.Initialize(
                null);
        }

        private static void CreatePrototypeCity()
        {
            if (CityAssetRuntimeInstaller.TryInstall())
            {
                return;
            }

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

            Vector3 playerSpawn =
                CityAssetRuntimeInstaller.PlayerSpawnPoint;

            car.transform.position =
                playerSpawn;

            car.transform.rotation =
                CityAssetRuntimeInstaller.PlayerSpawnRotation;
            car.AddComponent<Rigidbody>();

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

            GameObject root =
                new(
                    "Profession Car Wash");

            root.transform.position =
                carWash.StartPoint;

            StaticActivityMarkerVisual visual =
                root.AddComponent<
                    StaticActivityMarkerVisual>();

            visual.Bind(
                "MotorCity/Markers/CarWashMarkerVfx",
                new Color(
                    0.10f,
                    0.82f,
                    1f),
                CheckpointBeaconStyle.Profession);
        }

        private static void CreateTowTruckMarker(
            TowTruckJobSystem towTruck)
        {
            if (towTruck == null)
                return;

            GameObject root =
                new(
                    "Profession Tow Service");

            root.transform.position =
                towTruck.StartPoint;

            StaticActivityMarkerVisual visual =
                root.AddComponent<
                    StaticActivityMarkerVisual>();

            visual.Bind(
                "MotorCity/Markers/TowMarkerVfx",
                new Color(
                    1f,
                    0.56f,
                    0.06f),
                CheckpointBeaconStyle.Tow);
        }

        private static void CreateProfessionMarkers(
            CityProfessionSystem professions)
        {
            if (professions == null)
                return;

            for (int i = 0;
                 i < professions.StartCount;
                 i++)
            {
                GameObject root =
                    new(
                        "Profession " +
                        professions.GetStartName(i));

                root.transform.position =
                    professions.GetStartPoint(i);

                StaticActivityMarkerVisual visual =
                    root.AddComponent<
                        StaticActivityMarkerVisual>();

                visual.Bind(
                    "MotorCity/Markers/ProfessionMarkerVfx",
                    new Color(
                        1f,
                        0.68f,
                        0.10f),
                    CheckpointBeaconStyle.Profession);
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

        private static void CreateDiscoveryMarkers(
            DiscoverySystem discoveries)
        {
            if (discoveries == null)
                return;

            for (int i = 0;
                 i < discoveries.DiscoveryCount;
                 i++)
            {
                GameObject root =
                    new($"Discovery {i + 1}");

                DiscoveryMarkerVisual visual =
                    root.AddComponent<DiscoveryMarkerVisual>();

                visual.Bind(
                    discoveries,
                    i);
            }
        }

        private static void CreateUndergroundMarker(
            UndergroundSceneSystem underground)
        {
            if (underground == null)
                return;

            GameObject root =
                new(
                    "Underground Marker");

            root.transform.position =
                CityAssetRuntimeInstaller.UndergroundMeetingPoint;

            UndergroundMarkerVisual visual =
                root.AddComponent<
                    UndergroundMarkerVisual>();

            visual.Bind(
                underground);
        }

        private static void CreateGarageMarker(
            GarageUpgradeSystem garage)
        {
            GameObject marker =
                new("Garage Marker");

            if (garage != null)
            {
                marker.transform.position =
                    garage.GarageCenter;
            }

            GarageMarkerVisual visual =
                marker.AddComponent<GarageMarkerVisual>();

            visual.Bind(
                garage);
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

        private static void CreateCamera(
            Transform target,
            bool playOpeningPresentation)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();

            UniversalAdditionalCameraData cameraData =
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing =
                true;

            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.12f;
            camera.farClipPlane = 2200f;
            cameraObject.transform.position = target.position + new Vector3(0f, 2.8f, -6.8f);
            ChaseCamera chase = cameraObject.AddComponent<ChaseCamera>();
            chase.SetTarget(target);

            OpeningCinematicCamera openingCamera =
                cameraObject.AddComponent<OpeningCinematicCamera>();

            openingCamera.Initialize(
                target);

            if (playOpeningPresentation)
            {
                openingCamera.Arm(
                    5f);
            }
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
            VehicleCustomizationSystem customization,
            CityProfessionSystem professions,
            CarWashJobSystem carWash,
            TowTruckJobSystem towTruck,
            ClubSystem club,
            RewardedBonusSystem rewardedBonus,
            CosmeticStoreSystem cosmeticStore,
            AchievementSystem achievements,
            AdventureDirector adventureDirector,
            bool playOpeningPresentation)
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
                customization,
                professions,
                carWash,
                towTruck,
                club,
                rewardedBonus,
                cosmeticStore,
                achievements,
                adventureDirector,
                playOpeningPresentation);
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
