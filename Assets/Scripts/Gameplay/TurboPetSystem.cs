using System;
using MotorCity.Localization;
using MotorCity.Platform;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class TurboPetSystem :
        MonoBehaviour
    {
        private const string LevelKey =
            "MotorCity.Turbo.Level";
        private const string XpKey =
            "MotorCity.Turbo.Xp";
        private const string LastDayKey =
            "MotorCity.Turbo.LastDay";
        private const string DailyDayKey =
            "MotorCity.Turbo.Daily.Day";
        private const string DailyProgressKey =
            "MotorCity.Turbo.Daily.Progress";
        private const string DailyClaimedKey =
            "MotorCity.Turbo.Daily.Claimed";
        private const float MessageSeconds =
            4f;
        private const float HintDelaySeconds =
            22f;

        private ArcadeCarController car;
        private Rigidbody carBody;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private DiscoverySystem discoveries;

        private GameObject visualRoot;
        private GameObject externalVisual;
        private Animator externalAnimator;
        private bool usingHaonVisual;
        private bool useSupporterPixieSkin;
        private Vector3 visualAnchorLocal;
        private Transform observedVehicleVisual;
        private Vector3 previousCarPosition;
        private Quaternion previousCarRotation;
        private bool carPoseInitialized;
        private float hoverPhase;
        private float animationReactionTimer;
        private float idleVariantTimer;
        private bool alternateIdle;
        private int currentAnimatorStateHash;
        private float messageTimer;
        private float activeSeconds;
        private string observedActivityId;

        private int dailyType;
        private int dailyTarget;
        private int dailyProgress;
        private bool dailyClaimed;
        private long currentDay;
        public int Level { get; private set; }
        public int Xp { get; private set; }

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public string DailyObjectiveLine
        {
            get
            {
                if (dailyClaimed)
                {
                    return
                        MotorCityLocalization.Text(
                            "turbo.daily_done");
                }

                return
                    MotorCityLocalization.Format(
                        DailyTextKey(),
                        Mathf.Min(
                            dailyProgress,
                            dailyTarget),
                        dailyTarget);
            }
        }

        public string MoodName
        {
            get
            {
                return
                    MotorCityLocalization.Text(
                        "turbo.mood_happy");
            }
        }

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager manager,
            DiscoverySystem discoverySystem)
        {
            car =
                targetCar;

            carBody =
                car != null
                    ? car.GetComponent<Rigidbody>()
                    : null;

            wallet =
                targetWallet;
            activityManager =
                manager;
            discoveries =
                discoverySystem;

            Level =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        LevelKey,
                        1),
                    1,
                    10);

            Xp =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        XpKey,
                        0));

            useSupporterPixieSkin =
                CosmeticStoreSystem.SupporterPackOwned;

            currentDay =
                Math.Max(
                    1L,
                    MotorCityPlatform.ServerUnixTime /
                    86400L);

            ResolveReturnMood();
            ResolveDailyTask();
            ApplyAbilities();
            BuildVisual();

            if (activityManager != null)
            {
                activityManager.ActivityCompleted +=
                    OnActivityCompleted;
            }
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.unscaledDeltaTime);
            }

            UpdateActivityAssist();
            UpdatePixieAnimation();
            RefreshDayIfNeeded();
        }

        private void LateUpdate()
        {
            if (visualRoot == null ||
                car == null)
            {
                return;
            }

            Transform vehicleVisual =
                car.transform.Find(
                    "MotorCityVehicleVisual_Runtime");

            if (vehicleVisual !=
                observedVehicleVisual)
            {
                observedVehicleVisual =
                    vehicleVisual;

                visualAnchorLocal =
                    ResolveVisualAnchor();
            }

            Vector3 carPosition =
                car.transform.position;

            Quaternion carRotation =
                car.transform.rotation;

            if (!carPoseInitialized)
            {
                previousCarPosition =
                    carPosition;

                previousCarRotation =
                    carRotation;

                carPoseInitialized =
                    true;
            }
            else
            {
                Vector3 carDelta =
                    carPosition -
                    previousCarPosition;

                float carDeltaDistance =
                    carDelta.magnitude;

                if (carDeltaDistance < 25f)
                {
                    // Move Pixie by the car's exact frame-to-frame transform
                    // first. Smoothing only the remaining relative offset means
                    // there is no built-in delay behind a fast moving car.
                    Quaternion rotationDelta =
                        carRotation *
                        Quaternion.Inverse(
                            previousCarRotation);

                    Vector3 relative =
                        visualRoot.transform.position -
                        previousCarPosition;

                    visualRoot.transform.position =
                        carPosition +
                        rotationDelta *
                        relative;
                }

                previousCarPosition =
                    carPosition;

                previousCarRotation =
                    carRotation;
            }

            hoverPhase +=
                Time.unscaledDeltaTime *
                2.25f;

            Vector3 targetWorld =
                car.transform.TransformPoint(
                    visualAnchorLocal);

            targetWorld +=
                Vector3.up *
                Mathf.Sin(
                    hoverPhase) *
                0.16f;

            float distance =
                Vector3.Distance(
                    visualRoot.transform.position,
                    targetWorld);

            if (distance > 25f)
            {
                // This path is now reserved for a real player teleport/reset.
                visualRoot.transform.position =
                    targetWorld;
            }
            else
            {
                // Exponential correction is frame-rate independent and does
                // not accumulate the velocity lag that SmoothDamp introduced.
                float followBlend =
                    1f -
                    Mathf.Exp(
                        -18f *
                        Time.unscaledDeltaTime);

                visualRoot.transform.position =
                    Vector3.Lerp(
                        visualRoot.transform.position,
                        targetWorld,
                        followBlend);
            }

            float rotationBlend =
                1f -
                Mathf.Exp(
                    -9f *
                    Time.unscaledDeltaTime);

            visualRoot.transform.rotation =
                Quaternion.Slerp(
                    visualRoot.transform.rotation,
                    carRotation,
                    rotationBlend);
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityCompleted -=
                    OnActivityCompleted;
            }
        }

        public void AddXp(
            int amount)
        {
            if (amount <= 0)
                return;

            Xp +=
                amount;

            bool leveled =
                false;

            while (Level < 10 &&
                   Xp >= XpForNextLevel(
                       Level))
            {
                Xp -=
                    XpForNextLevel(
                        Level);

                Level++;
                leveled =
                    true;
            }

            SaveProgress();

            if (!leveled)
                return;

            ApplyAbilities();
            RefreshVisualSkin();
            PlayPixieReaction(
                "Pixie Victory",
                2.2f);

            StatusText =
                MotorCityLocalization.Format(
                    "turbo.level_up",
                    Level);

            messageTimer =
                MessageSeconds;
        }

        private void OnActivityCompleted(
            string activityId)
        {
            PlayPixieReaction(
                "Pixie Clap",
                1.8f);

            RegisterSuccessfulActivity(
                activityId);
        }

        private void RegisterSuccessfulActivity(
            string activityId)
        {
            AddXp(
                18);

            if (dailyClaimed)
                return;

            bool counts =
                dailyType switch
                {
                    0 =>
                        !string.IsNullOrWhiteSpace(
                            activityId),
                    1 =>
                        activityId == "sprint" ||
                        activityId == "circuit",
                    2 =>
                        activityId == "drift",
                    _ =>
                        false
                };

            if (!counts)
                return;

            dailyProgress =
                Mathf.Min(
                    dailyTarget,
                    dailyProgress + 1);

            SaveDaily();

            if (dailyProgress <
                dailyTarget)
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "turbo.daily_progress",
                        dailyProgress,
                        dailyTarget);

                messageTimer =
                    MessageSeconds;

                return;
            }

            CompleteDaily();
        }

        private void CompleteDaily()
        {
            dailyClaimed =
                true;

            int credits =
                300 +
                Level * 35;

            wallet?.AddCredits(
                credits);

            AddXp(
                45);

            SaveDaily();

            StatusText =
                MotorCityLocalization.Format(
                    "turbo.daily_reward",
                    credits);

            messageTimer =
                MessageSeconds + 1f;
        }

        private void ResolveReturnMood()
        {
            long previousDay =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    LastDayKey,
                    0);

            long missedDays =
                previousDay <= 0L
                    ? 0L
                    : Mathf.Max(
                        0,
                        (int)(currentDay -
                              previousDay -
                              1L));

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                LastDayKey,
                (int)Math.Min(
                    (long)int.MaxValue,
                    currentDay));

            MotorCity.Persistence.MotorCitySaveService.Save();

            if (previousDay <= 0L)
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "turbo.hello");

                messageTimer =
                    MessageSeconds + 1f;

                return;
            }

            if (missedDays >= 1L)
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "turbo.reunion");

                messageTimer =
                    MessageSeconds + 1f;
            }
        }

        private void ResolveDailyTask()
        {
            long savedDay =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    DailyDayKey,
                    0);

            dailyType =
                (int)(currentDay % 3L);

            dailyTarget =
                dailyType == 0
                    ? 2
                    : 1;

            if (savedDay ==
                currentDay)
            {
                dailyProgress =
                    Mathf.Max(
                        0,
                        MotorCity.Persistence.MotorCitySaveService.GetInt(
                            DailyProgressKey,
                            0));

                dailyClaimed =
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        DailyClaimedKey,
                        0) != 0;

                return;
            }

            dailyProgress = 0;
            dailyClaimed = false;
            SaveDaily();
        }

        private void RefreshDayIfNeeded()
        {
            long serverDay =
                Math.Max(
                    1L,
                    MotorCityPlatform.ServerUnixTime /
                    86400L);

            if (serverDay ==
                currentDay)
            {
                return;
            }

            currentDay =
                serverDay;

            ResolveDailyTask();

            StatusText =
                MotorCityLocalization.Text(
                    "turbo.new_daily");

            messageTimer =
                MessageSeconds;
        }

        private void UpdateActivityAssist()
        {
            string activeId =
                activityManager != null
                    ? activityManager.ActiveId
                    : null;

            if (activeId !=
                observedActivityId)
            {
                observedActivityId =
                    activeId;

                activeSeconds = 0f;

                if (Level >= 4 &&
                    IsRaceActivity(
                        activeId))
                {
                    car?.ActivateTurboAssist(
                        1.30f,
                        3.2f);

                    PlayPixieReaction(
                        "Pixie Boost",
                        1.25f);

                    StatusText =
                        MotorCityLocalization.Text(
                            "turbo.boost");

                    messageTimer =
                        2.8f;
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    activeId) ||
                Level < 2)
            {
                activeSeconds = 0f;
                return;
            }

            activeSeconds +=
                Time.unscaledDeltaTime;

            if (activeSeconds <
                HintDelaySeconds)
            {
                return;
            }

            activeSeconds =
                -999f;

            StatusText =
                MotorCityLocalization.Text(
                    "turbo.hint");

            messageTimer =
                MessageSeconds;
        }

        private void ApplyAbilities()
        {
            if (discoveries != null)
            {
                discoveries.BonusDiscoveryRadius =
                    Level >= 3
                        ? 9f
                        : 0f;
            }
        }

        private void BuildVisual()
        {
            if (car == null)
                return;

            visualRoot =
                new GameObject(
                    "Pixie Companion");

            visualRoot.transform.SetParent(
                null,
                false);

            visualAnchorLocal =
                ResolveVisualAnchor();

            visualRoot.transform.position =
                car.transform.TransformPoint(
                    visualAnchorLocal);

            visualRoot.transform.rotation =
                car.transform.rotation;

            observedVehicleVisual =
                car.transform.Find(
                    "MotorCityVehicleVisual_Runtime");

            previousCarPosition =
                car.transform.position;

            previousCarRotation =
                car.transform.rotation;

            carPoseInitialized =
                true;

            GameObject authoredPrefab =
                useSupporterPixieSkin
                    ? Resources.Load<GameObject>(
                        "MotorCity/Pixie/AmaneKisoraVisual")
                    : Resources.Load<GameObject>(
                        "MotorCity/Byte/HaonByteVisual");

            if (authoredPrefab == null &&
                useSupporterPixieSkin)
            {
                authoredPrefab =
                    Resources.Load<GameObject>(
                        "MotorCity/Byte/HaonByteVisual");
            }

            if (authoredPrefab != null)
            {
                externalVisual =
                    Instantiate(
                        authoredPrefab,
                        visualRoot.transform,
                        false);

                externalVisual.name =
                    useSupporterPixieSkin
                        ? "Pixie EX Amane Kisora Visual"
                        : "Pixie Haon SD Visual";

                externalVisual.transform.localPosition =
                    Vector3.zero;

                externalVisual.transform.localRotation =
                    Quaternion.Euler(
                        0f,
                        180f,
                        0f);

                externalVisual.transform.localScale =
                    Vector3.one *
                    (useSupporterPixieSkin
                        ? 0.58f
                        : 0.66f);

                externalAnimator =
                    externalVisual.GetComponentInChildren<Animator>(
                        true);

                // Both authored Pixie prefabs are animated characters.
                // Keep the shared animation path enabled for the supporter skin too.
                usingHaonVisual =
                    true;

                if (externalAnimator != null)
                {
                    externalAnimator.applyRootMotion =
                        false;
                    externalAnimator.cullingMode =
                        AnimatorCullingMode.AlwaysAnimate;
                    externalAnimator.updateMode =
                        AnimatorUpdateMode.Normal;

                    // HAON 2026.8 ships the CharacterSet with an authored
                    // humanoid Avatar. After swapping its controller in the
                    // generated Pixie prefab Unity can keep the instantiated
                    // Animator in bind pose until it is explicitly rebound.
                    // Rebind here so the first Pixie state drives the humanoid
                    // immediately instead of leaving her in a T-pose.
                    externalAnimator.Rebind();
                    externalAnimator.Update(
                        0f);

                    PlayAnimatorState(
                        "Pixie Idle",
                        0f);

                    externalAnimator.Update(
                        0f);
                }

                RefreshVisualSkin();
                return;
            }

            BoxCollider chassis =
                car.GetComponent<BoxCollider>();

            float carWidth =
                chassis != null
                    ? Mathf.Max(
                        1.4f,
                        chassis.size.x)
                    : 1.8f;

            float scale =
                Mathf.Clamp(
                    carWidth * 0.15f,
                    0.24f,
                    0.40f);

            visualRoot.transform.localScale =
                Vector3.one *
                scale;

            CreatePart(
                "Body",
                PrimitiveType.Cube,
                new Vector3(
                    0f,
                    0f,
                    0f),
                new Vector3(
                    1.05f,
                    0.72f,
                    0.72f));

            CreatePart(
                "Head",
                PrimitiveType.Cube,
                new Vector3(
                    0f,
                    0.73f,
                    0.08f),
                new Vector3(
                    0.82f,
                    0.68f,
                    0.72f));

            CreatePart(
                "Ear L",
                PrimitiveType.Cube,
                new Vector3(
                    -0.30f,
                    1.16f,
                    0.08f),
                new Vector3(
                    0.24f,
                    0.44f,
                    0.24f),
                new Vector3(
                    0f,
                    0f,
                    18f));

            CreatePart(
                "Ear R",
                PrimitiveType.Cube,
                new Vector3(
                    0.30f,
                    1.16f,
                    0.08f),
                new Vector3(
                    0.24f,
                    0.44f,
                    0.24f),
                new Vector3(
                    0f,
                    0f,
                    -18f));

            CreateEye(
                -0.22f);

            CreateEye(
                0.22f);

            RefreshVisualSkin();
        }

        private void CreateEye(
            float x)
        {
            GameObject eye =
                CreatePart(
                    "Eye",
                    PrimitiveType.Sphere,
                    new Vector3(
                        x,
                        0.80f,
                        0.39f),
                    new Vector3(
                        0.18f,
                        0.16f,
                        0.10f));

            Renderer renderer =
                eye.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.material.color =
                    new Color(
                        0.12f,
                        0.92f,
                        1f,
                        1f);
            }
        }

        private GameObject CreatePart(
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEuler = default)
        {
            GameObject part =
                GameObject.CreatePrimitive(
                    type);

            part.name =
                name;

            part.transform.SetParent(
                visualRoot.transform,
                false);

            part.transform.localPosition =
                localPosition;

            part.transform.localRotation =
                Quaternion.Euler(
                    localEuler);

            part.transform.localScale =
                localScale;

            Collider collider =
                part.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(
                    collider);
            }

            return part;
        }

        private Vector3 ResolveVisualAnchor()
        {
            if (car == null)
            {
                return
                    new Vector3(
                        1.1f,
                        1.35f,
                        0.15f);
            }

            Bounds bounds =
                ResolveCarLocalBounds();

            float halfWidth =
                Mathf.Max(
                    0.45f,
                    bounds.extents.x);

            float width =
                halfWidth * 2f;

            // Anchor Pixie from the vehicle's upper centre instead of using
            // fixed offsets. She sits just outside the passenger/right side,
            // with the gap scaling gently with vehicle width, and slightly
            // below the highest point of the body.
            float sideGap =
                Mathf.Clamp(
                    width * 0.10f,
                    0.16f,
                    0.34f);

            float verticalDrop =
                Mathf.Clamp(
                    bounds.size.y * 0.16f,
                    0.14f,
                    0.32f);

            return
                new Vector3(
                    bounds.center.x +
                    halfWidth +
                    sideGap,
                    bounds.max.y -
                    verticalDrop,
                    bounds.center.z);
        }

        private Bounds ResolveCarLocalBounds()
        {
            Renderer[] renderers =
                car.GetComponentsInChildren<Renderer>(
                    true);

            bool initialized =
                false;

            Bounds local =
                new(
                    Vector3.zero,
                    new Vector3(
                        1.8f,
                        1.4f,
                        4f));

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null)
                    continue;

                Bounds world =
                    renderer.bounds;

                Vector3 center =
                    car.transform.InverseTransformPoint(
                        world.center);

                Vector3 size =
                    car.transform.InverseTransformVector(
                        world.size);

                size =
                    new Vector3(
                        Mathf.Abs(
                            size.x),
                        Mathf.Abs(
                            size.y),
                        Mathf.Abs(
                            size.z));

                Bounds item =
                    new(
                        center,
                        size);

                if (!initialized)
                {
                    local =
                        item;

                    initialized =
                        true;
                }
                else
                {
                    local.Encapsulate(
                        item);
                }
            }

            return local;
        }

        private void UpdatePixieAnimation()
        {
            if (!usingHaonVisual ||
                externalAnimator == null)
            {
                return;
            }

            if (animationReactionTimer > 0f)
            {
                animationReactionTimer =
                    Mathf.Max(
                        0f,
                        animationReactionTimer -
                        Time.unscaledDeltaTime);

                if (animationReactionTimer > 0f)
                    return;
            }

            float speed =
                car == null
                    ? 0f
                    : car.SpeedKph;

            if (useSupporterPixieSkin &&
                externalAnimator != null)
            {
                externalAnimator.SetBool(
                    "param_idletorunning",
                    speed > 7f);
            }

            string state;

            if (speed > 7f)
            {
                idleVariantTimer = 0f;
                alternateIdle = false;
                state = "Pixie Follow";
            }
            else
            {
                idleVariantTimer +=
                    Time.unscaledDeltaTime;

                if (idleVariantTimer >= 5.5f)
                {
                    idleVariantTimer = 0f;
                    alternateIdle =
                        !alternateIdle;
                }

                state =
                    alternateIdle
                        ? "Pixie Idle Alt"
                        : "Pixie Idle";
            }

            externalAnimator.speed =
                speed > 7f
                    ? Mathf.Lerp(
                        0.95f,
                        1.18f,
                        Mathf.InverseLerp(
                            7f,
                            150f,
                            speed))
                    : 1f;

            PlayAnimatorState(
                state,
                0.18f);
        }

        private void PlayPixieReaction(
            string state,
            float seconds)
        {
            if (!usingHaonVisual ||
                externalAnimator == null)
            {
                return;
            }

            externalAnimator.speed = 1f;

            animationReactionTimer =
                Mathf.Max(
                    animationReactionTimer,
                    seconds);

            PlayAnimatorState(
                state,
                0.08f);
        }

        private void PlayAnimatorState(
            string state,
            float transitionSeconds)
        {
            if (externalAnimator == null ||
                externalAnimator.runtimeAnimatorController == null ||
                string.IsNullOrWhiteSpace(
                    state))
            {
                return;
            }

            string mappedState =
                useSupporterPixieSkin
                    ? MapKisoraState(
                        state)
                    : state;

            string fullStateName =
                (useSupporterPixieSkin
                    ? "Body Animation Layer."
                    : "Base Layer.") +
                mappedState;

            int hash =
                Animator.StringToHash(
                    fullStateName);

            if (!externalAnimator.HasState(
                    0,
                    hash))
            {
                return;
            }

            if (hash ==
                currentAnimatorStateHash)
            {
                if (useSupporterPixieSkin)
                {
                    AnimatorStateInfo currentState =
                        externalAnimator.GetCurrentAnimatorStateInfo(
                            0);

                    int shortHash =
                        Animator.StringToHash(
                            mappedState);

                    // Kisora's original controller can transition back to idle
                    // when its own parameters disagree with Motor City's state.
                    // Keep the requested locomotion state authoritative.
                    if (currentState.shortNameHash !=
                        shortHash)
                    {
                        externalAnimator.Play(
                            hash,
                            0,
                            0f);

                        return;
                    }

                    // Extra guard for imported copies where running is still
                    // treated as a one-shot instead of a looping clip.
                    if (mappedState == "running" &&
                        currentState.normalizedTime >= 0.98f)
                    {
                        externalAnimator.Play(
                            hash,
                            0,
                            0f);
                    }
                }

                return;
            }

            currentAnimatorStateHash =
                hash;

            if (transitionSeconds <= 0f)
            {
                externalAnimator.Play(
                    hash,
                    0,
                    0f);
            }
            else
            {
                externalAnimator.CrossFadeInFixedTime(
                    hash,
                    transitionSeconds,
                    0);
            }
        }

        private static string MapKisoraState(
            string pixieState)
        {
            return pixieState switch
            {
                "Pixie Follow" =>
                    "running",

                "Pixie Victory" or
                "Pixie Clap" =>
                    "winpose",

                "Pixie Boost" =>
                    "jump",

                "Pixie Idle Alt" or
                "Pixie Idle" =>
                    "idle",

                _ =>
                    "idle"
            };
        }

        public void SetSupporterPackSkin(
            bool enabled)
        {
            if (useSupporterPixieSkin ==
                enabled)
            {
                return;
            }

            useSupporterPixieSkin =
                enabled;

            if (car != null)
                RebuildVisual();
        }

        private void RebuildVisual()
        {
            if (visualRoot != null)
            {
                Destroy(
                    visualRoot);
            }

            visualRoot =
                null;
            externalVisual =
                null;
            externalAnimator =
                null;
            usingHaonVisual =
                false;
            currentAnimatorStateHash =
                0;
            observedVehicleVisual =
                null;
            carPoseInitialized =
                false;

            BuildVisual();
        }

        private void RefreshVisualSkin()
        {
            if (visualRoot == null)
                return;

            if (usingHaonVisual ||
                useSupporterPixieSkin)
            {
                // Both authored Pixie prefabs are complete characters.
                // Never apply the primitive fallback tint to their materials.
                return;
            }

            Color bodyColor =
                new Color(
                    0.92f,
                    0.95f,
                    1f,
                    1f);

            Renderer[] renderers =
                visualRoot.GetComponentsInChildren<Renderer>(
                    true);

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null ||
                    renderer.gameObject.name ==
                    "Eye")
                {
                    continue;
                }

                renderer.material.color =
                    bodyColor;
            }
        }

        private void SaveProgress()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                LevelKey,
                Level);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                XpKey,
                Xp);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private void SaveDaily()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DailyDayKey,
                (int)Math.Min(
                    (long)int.MaxValue,
                    currentDay));

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DailyProgressKey,
                dailyProgress);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DailyClaimedKey,
                dailyClaimed
                    ? 1
                    : 0);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private string DailyTextKey()
        {
            return
                dailyType switch
                {
                    1 =>
                        "turbo.daily_race",
                    2 =>
                        "turbo.daily_drift",
                    _ =>
                        "turbo.daily_any"
                };
        }

        private static int XpForNextLevel(
            int level)
        {
            return
                70 +
                level * 35;
        }

        private static bool IsRaceActivity(
            string id)
        {
            return
                id == "sprint" ||
                id == "circuit" ||
                id == "underground";
        }
    }
}
