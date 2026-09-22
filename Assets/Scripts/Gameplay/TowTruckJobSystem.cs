using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class TowTruckJobSystem : MonoBehaviour
    {
        private const string ActivityId =
            "profession_tow";

        private const float StartRadius =
            13f;

        private const float PickupRadius =
            11f;

        private const float DeliveryRadius =
            14f;

        private const float MaxStartSpeedKph =
            7f;

        private const float HookSeconds =
            1.6f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activities;
        private CityProfessionSystem professions;

        private TowStage stage;
        private float hookProgress;
        private float statusTextTimer;
        private int lastHookPercent = -1;

        private const float StatusTextInterval =
            0.10f;
        private GameObject strandedCar;
        private GameObject towRig;
        private Material serviceMaterial;
        private Material darkMaterial;

        public bool IsActive =>
            stage !=
            TowStage.None;

        public bool IsNearStart { get; private set; }

        public Vector3 StartPoint { get; private set; }
        public Vector3 BreakdownPoint { get; private set; }
        public Vector3 ServicePoint { get; private set; }

        public Vector3 CurrentTarget
        {
            get
            {
                return
                    stage switch
                    {
                        TowStage.DriveToBreakdown =>
                            BreakdownPoint,
                        TowStage.Hooking =>
                            BreakdownPoint,
                        TowStage.Delivering =>
                            ServicePoint,
                        _ =>
                            StartPoint
                    };
            }
        }

        public bool TryGetNextTarget(
            out Vector3 target)
        {
            target =
                CurrentTarget;

            if (stage ==
                    TowStage.DriveToBreakdown ||
                stage ==
                    TowStage.Hooking)
            {
                target =
                    ServicePoint;

                return true;
            }

            return false;
        }

        public string StatusText { get; private set; }

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager activityManager,
            CityProfessionSystem professionSystem)
        {
            car =
                targetCar;

            wallet =
                targetWallet;

            activities =
                activityManager;

            professions =
                professionSystem;

            Vector3[] delivery =
                CityAssetRuntimeInstaller.DeliveryRoute;

            StartPoint =
                Point(
                    delivery,
                    5,
                    CityAssetRuntimeInstaller.GaragePoint);

            BreakdownPoint =
                Point(
                    delivery,
                    2,
                    CityAssetRuntimeInstaller.PlayerSpawnPoint);

            ServicePoint =
                Point(
                    delivery,
                    9,
                    CityAssetRuntimeInstaller.GaragePoint);

            BuildMaterials();
        }

        private void Update()
        {
            if (car == null ||
                wallet == null ||
                activities == null)
            {
                return;
            }

            if (IsActive)
            {
                UpdateActive();
                return;
            }

            float distance =
                FlatDistance(
                    car.transform.position,
                    StartPoint);

            IsNearStart =
                distance <=
                StartRadius;

            if (!IsNearStart)
                return;

            if (activities.IsBusy)
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "tow.busy",
                        activities.ActiveName);

                return;
            }

            if (car.SpeedKph >
                MaxStartSpeedKph)
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "tow.stop");

                return;
            }

            StatusText =
                MotorCityLocalization.Format(
                    "tow.start",
                    professions != null
                        ? professions.ProfessionLevel
                        : 1);

            if (MotorCityInput.InteractPressed)
            {
                BeginJob();
            }
        }

        private void BeginJob()
        {
            if (!activities.TryBegin(
                    ActivityId,
                    MotorCityLocalization.Text(
                        "tow.title")))
            {
                return;
            }

            stage =
                TowStage.DriveToBreakdown;

            IsNearStart =
                false;

            hookProgress =
                0f;
            lastHookPercent =
                -1;
            statusTextTimer =
                0f;

            SpawnStrandedCar();

            StatusText =
                MotorCityLocalization.Text(
                    "tow.go_pickup");
        }

        private void UpdateActive()
        {
            if (MotorCityInput.CancelPressed)
            {
                CancelJob();
                return;
            }

            switch (stage)
            {
                case TowStage.DriveToBreakdown:
                    UpdatePickupApproach();
                    break;

                case TowStage.Hooking:
                    UpdateHooking();
                    break;

                case TowStage.Delivering:
                    UpdateDelivery();
                    break;
            }
        }

        private void UpdatePickupApproach()
        {
            float distanceSquared =
                FlatDistanceSquared(
                    car.transform.position,
                    BreakdownPoint);

            if (distanceSquared >
                PickupRadius * PickupRadius)
            {
                statusTextTimer -=
                    Time.deltaTime;

                if (statusTextTimer <= 0f)
                {
                    statusTextTimer =
                        StatusTextInterval;

                    StatusText =
                        MotorCityLocalization.Format(
                            "tow.pickup_distance",
                            Mathf.RoundToInt(
                                Mathf.Sqrt(
                                    distanceSquared)));
                }

                return;
            }

            if (car.SpeedKph >
                MaxStartSpeedKph)
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "tow.pickup_stop");

                return;
            }

            StatusText =
                MotorCityLocalization.Text(
                    "tow.hook_prompt");

            if (MotorCityInput.InteractPressed)
            {
                stage =
                    TowStage.Hooking;

                hookProgress =
                    0f;
                lastHookPercent =
                    -1;
                statusTextTimer =
                    0f;

                car.SetDrivingEnabled(
                    false);
            }
        }

        private void UpdateHooking()
        {
            if (MotorCityInput.InteractHeld)
            {
                hookProgress +=
                    Time.unscaledDeltaTime;
            }
            else
            {
                hookProgress =
                    Mathf.Max(
                        0f,
                        hookProgress -
                        Time.unscaledDeltaTime *
                        0.35f);
            }

            int percent =
                Mathf.RoundToInt(
                    Mathf.Clamp01(
                        hookProgress /
                        HookSeconds) *
                    100f);

            if (percent !=
                lastHookPercent)
            {
                lastHookPercent =
                    percent;

                StatusText =
                    MotorCityLocalization.Format(
                        "tow.hooking",
                        percent);
            }

            if (hookProgress <
                HookSeconds)
            {
                return;
            }

            AttachTow();

            stage =
                TowStage.Delivering;

            car.SetDrivingEnabled(
                true);

            StatusText =
                MotorCityLocalization.Text(
                    "tow.deliver");
        }

        private void UpdateDelivery()
        {
            float distanceSquared =
                FlatDistanceSquared(
                    car.transform.position,
                    ServicePoint);

            statusTextTimer -=
                Time.deltaTime;

            if (statusTextTimer <= 0f)
            {
                statusTextTimer =
                    StatusTextInterval;

                StatusText =
                    MotorCityLocalization.Format(
                        "tow.delivery_distance",
                        Mathf.RoundToInt(
                            Mathf.Sqrt(
                                distanceSquared)));
            }

            if (distanceSquared >
                DeliveryRadius * DeliveryRadius)
            {
                return;
            }

            if (car.SpeedKph >
                MaxStartSpeedKph)
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "tow.service_stop");

                return;
            }

            CompleteJob();
        }

        private void CompleteJob()
        {
            int level =
                professions != null
                    ? professions.RegisterExternalCompletion()
                    : 1;

            int reward =
                Mathf.RoundToInt(
                    680f *
                    (1f +
                     (level - 1) *
                     0.04f));

            wallet.AddCredits(
                reward);

            CleanupVisuals();

            stage =
                TowStage.None;

            hookProgress =
                0f;

            activities.ShowResult(
                ActivityId,
                MotorCityLocalization.Text(
                    "tow.title"),
                MotorCityLocalization.Text(
                    "tow.complete"),
                MotorCityLocalization.Format(
                    "tow.result",
                    level),
                reward,
                true);
        }

        public void RestartFromResult()
        {
            if (activities == null ||
                !activities.HasResult ||
                activities.ResultActivityId !=
                    ActivityId ||
                car == null)
            {
                return;
            }

            activities.DismissResult();

            CleanupVisuals();

            stage =
                TowStage.None;

            car.SetDrivingEnabled(
                true);

            car.TeleportTo(
                StartPoint +
                Vector3.up * 1.1f,
                Quaternion.Euler(
                    0f,
                    car.transform.eulerAngles.y,
                    0f));

            BeginJob();
        }

        public void CancelJob()
        {
            if (!IsActive)
                return;

            CleanupVisuals();

            stage =
                TowStage.None;

            hookProgress =
                0f;

            car?.SetDrivingEnabled(
                true);

            activities?.End(
                ActivityId);

            StatusText =
                MotorCityLocalization.Text(
                    "tow.cancelled");
        }

        private void SpawnStrandedCar()
        {
            CleanupStrandedCar();

            strandedCar =
                new GameObject(
                    "Tow Job Stranded Car");

            strandedCar.transform.position =
                BreakdownPoint +
                Vector3.up *
                0.08f;

            strandedCar.transform.rotation =
                Quaternion.LookRotation(
                    Vector3.forward,
                    Vector3.up);

            if (TryCreateTrafficCarVisual())
            {
                return;
            }

            CreateFallbackStrandedCarVisual();
        }

        private bool TryCreateTrafficCarVisual()
        {
            string[] resourcePaths =
            {
                "MotorCity/Environment/FCGTrafficCars/Fiat 147_04",
                "MotorCity/Environment/FCGTrafficCars/Fiat 147-b_05"
            };

            int startIndex =
                Random.Range(
                    0,
                    resourcePaths.Length);

            GameObject prefab =
                null;

            for (int i = 0;
                 i < resourcePaths.Length;
                 i++)
            {
                string path =
                    resourcePaths[
                        (startIndex + i) %
                        resourcePaths.Length];

                prefab =
                    Resources.Load<GameObject>(
                        path);

                if (prefab != null)
                    break;
            }

            if (prefab == null)
            {
                return false;
            }

            GameObject visual =
                Instantiate(
                    prefab,
                    strandedCar.transform);

            visual.name =
                "Traffic Car Visual";

            visual.transform.localPosition =
                Vector3.zero;

            visual.transform.localRotation =
                Quaternion.identity;

            Rigidbody[] bodies =
                visual.GetComponentsInChildren<Rigidbody>(
                    true);

            foreach (Rigidbody body in bodies)
            {
                Destroy(
                    body);
            }

            Collider[] colliders =
                visual.GetComponentsInChildren<Collider>(
                    true);

            foreach (Collider collider in colliders)
            {
                Destroy(
                    collider);
            }

            MonoBehaviour[] behaviours =
                visual.GetComponentsInChildren<MonoBehaviour>(
                    true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                behaviour.enabled =
                    false;
            }

            FitTrafficCarVisual(
                visual);

            return true;
        }

        private static void FitTrafficCarVisual(
            GameObject visual)
        {
            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(
                    true);

            if (renderers.Length == 0)
                return;

            Bounds bounds =
                renderers[0].bounds;

            for (int i = 1;
                 i < renderers.Length;
                 i++)
            {
                bounds.Encapsulate(
                    renderers[i].bounds);
            }

            float horizontalLength =
                Mathf.Max(
                    bounds.size.x,
                    bounds.size.z);

            if (horizontalLength >
                0.01f)
            {
                float scale =
                    Mathf.Clamp(
                        3.8f /
                        horizontalLength,
                        0.65f,
                        1.35f);

                visual.transform.localScale *=
                    scale;
            }

            renderers =
                visual.GetComponentsInChildren<Renderer>(
                    true);

            bounds =
                renderers[0].bounds;

            for (int i = 1;
                 i < renderers.Length;
                 i++)
            {
                bounds.Encapsulate(
                    renderers[i].bounds);
            }

            Transform parent =
                visual.transform.parent;

            Vector3 localCenter =
                parent.InverseTransformPoint(
                    bounds.center);

            Vector3 localBottom =
                parent.InverseTransformPoint(
                    new Vector3(
                        bounds.center.x,
                        bounds.min.y,
                        bounds.center.z));

            visual.transform.localPosition -=
                new Vector3(
                    localCenter.x,
                    localBottom.y,
                    localCenter.z);
        }

        private void CreateFallbackStrandedCarVisual()
        {
            CreatePart(
                "Body",
                PrimitiveType.Cube,
                strandedCar.transform,
                new Vector3(
                    1.75f,
                    0.48f,
                    3.5f),
                new Vector3(
                    0f,
                    0.35f,
                    0f),
                serviceMaterial);

            CreatePart(
                "Cabin",
                PrimitiveType.Cube,
                strandedCar.transform,
                new Vector3(
                    1.45f,
                    0.52f,
                    1.55f),
                new Vector3(
                    0f,
                    0.82f,
                    -0.05f),
                darkMaterial);

            Vector3[] wheels =
            {
                new(-0.92f, 0f, 1.05f),
                new(0.92f, 0f, 1.05f),
                new(-0.92f, 0f, -1.05f),
                new(0.92f, 0f, -1.05f)
            };

            foreach (Vector3 wheel in
                     wheels)
            {
                GameObject part =
                    CreatePart(
                        "Wheel",
                        PrimitiveType.Cylinder,
                        strandedCar.transform,
                        new Vector3(
                            0.48f,
                            0.18f,
                            0.48f),
                        wheel,
                        darkMaterial);

                part.transform.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        90f);
            }
        }

        private void AttachTow()
        {
            if (strandedCar == null ||
                car == null)
            {
                return;
            }

            if (towRig != null)
            {
                Destroy(
                    towRig);
            }

            towRig =
                new GameObject(
                    "Motor City Tow Rig");

            towRig.transform.SetParent(
                car.transform,
                false);

            towRig.transform.localPosition =
                new Vector3(
                    0f,
                    0.65f,
                    -1.8f);

            GameObject boom =
                CreatePart(
                    "Tow Boom",
                    PrimitiveType.Cube,
                    towRig.transform,
                    new Vector3(
                        0.18f,
                        0.18f,
                        2.25f),
                    new Vector3(
                        0f,
                        0.15f,
                        -0.65f),
                    serviceMaterial);

            boom.transform.localRotation =
                Quaternion.Euler(
                    -12f,
                    0f,
                    0f);

            CreatePart(
                "Tow Crossbar",
                PrimitiveType.Cube,
                towRig.transform,
                new Vector3(
                    1.15f,
                    0.16f,
                    0.16f),
                new Vector3(
                    0f,
                    -0.08f,
                    -1.72f),
                serviceMaterial);

            CreatePart(
                "Tow Hook",
                PrimitiveType.Sphere,
                towRig.transform,
                new Vector3(
                    0.24f,
                    0.24f,
                    0.24f),
                new Vector3(
                    0f,
                    -0.12f,
                    -1.98f),
                darkMaterial);

            strandedCar.transform.SetParent(
                car.transform,
                false);

            strandedCar.transform.localPosition =
                new Vector3(
                    0f,
                    0.12f,
                    -4.6f);

            strandedCar.transform.localRotation =
                Quaternion.identity;
        }

        private GameObject CreatePart(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 scale,
            Vector3 localPosition,
            Material material)
        {
            GameObject part =
                GameObject.CreatePrimitive(
                    type);

            part.name =
                name;

            part.transform.SetParent(
                parent,
                false);

            part.transform.localScale =
                scale;

            part.transform.localPosition =
                localPosition;

            Renderer renderer =
                part.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    material;

                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;

                renderer.receiveShadows =
                    false;

                renderer.lightProbeUsage =
                    UnityEngine.Rendering.LightProbeUsage.Off;

                renderer.reflectionProbeUsage =
                    UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }

            Collider collider =
                part.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(
                    collider);
            }

            return part;
        }

        private void BuildMaterials()
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit") ??
                Shader.Find(
                    "Standard");

            serviceMaterial =
                new Material(
                    shader)
                {
                    name =
                        "MotorCity Tow Service",
                    color =
                        new Color(
                            1f,
                            0.66f,
                            0.08f,
                            1f)
                };

            darkMaterial =
                new Material(
                    shader)
                {
                    name =
                        "MotorCity Tow Dark",
                    color =
                        new Color(
                            0.06f,
                            0.075f,
                            0.095f,
                            1f)
                };
        }

        private void CleanupVisuals()
        {
            CleanupStrandedCar();

            if (towRig != null)
            {
                Destroy(
                    towRig);

                towRig =
                    null;
            }
        }

        private void CleanupStrandedCar()
        {
            if (strandedCar == null)
                return;

            Destroy(
                strandedCar);

            strandedCar =
                null;
        }

        private void OnDisable()
        {
            if (IsActive)
                CancelJob();
        }

        private void OnDestroy()
        {
            CleanupVisuals();

            if (serviceMaterial != null)
                Destroy(serviceMaterial);

            if (darkMaterial != null)
                Destroy(darkMaterial);
        }

        private static Vector3 Point(
            Vector3[] source,
            int index,
            Vector3 fallback)
        {
            if (source == null ||
                source.Length == 0)
            {
                return fallback;
            }

            return
                source[
                    Mathf.Clamp(
                        index,
                        0,
                        source.Length - 1)];
        }

        private static float FlatDistance(
            Vector3 a,
            Vector3 b)
        {
            return
                Mathf.Sqrt(
                    FlatDistanceSquared(
                        a,
                        b));
        }

        private static float FlatDistanceSquared(
            Vector3 a,
            Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;

            return
                (a - b).sqrMagnitude;
        }

        private enum TowStage
        {
            None = 0,
            DriveToBreakdown = 1,
            Hooking = 2,
            Delivering = 3
        }
    }
}
