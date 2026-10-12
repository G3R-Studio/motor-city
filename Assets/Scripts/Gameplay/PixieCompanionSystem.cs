using System;
using System.Collections.Generic;
using MotorCity.Localization;
using MotorCity.Platform;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    // Pixie is the rooftop vehicle companion: XP, levels, visuals, daily
    // tasks and the temporary driving boost belong to Pixie, never to Turbo.
    // Turbo is the separate story narrator/navigation assistant.
    // Legacy MotorCity.Turbo.* save keys remain untouched for old saves.
    public sealed class PixieCompanionSystem :
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
        // Per player request (2026-10-11), both tested skin offsets
        // lowered by 5 cm. These are ADDITIONAL offsets on top of the
        // central body-collider contact height, not world-space heights.
        private const float StandardRoofHeightOffset =
            0.05f;
        private const float ExclusiveRoofHeightOffset =
            0.10f;
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
        // The requested supporter skin can fall back to HAON if its
        // prefab is unavailable. Use the actual rig for animation/height.
        private bool usingKisoraVisual;
        private bool useSupporterPixieSkin;
        private float ActiveSkinRoofHeightOffset =>
            usingKisoraVisual
                ? ExclusiveRoofHeightOffset
                : StandardRoofHeightOffset;
        private Vector3 visualAnchorLocal;
        private Transform observedVehicleVisual;
#if UNITY_EDITOR
        // Preview-only fine adjustment; never written to saves.
        // QA shows the *total* height so the value agrees with the
        // original +0.15m / +0.10m values the player calibrated.
        private float editorRoofHeightAdjustment;

        public float EditorRoofHeightOffset =>
            ActiveSkinRoofHeightOffset + editorRoofHeightAdjustment;

        public string EditorActiveVisual =>
            usingKisoraVisual
                ? "EX / SapphiArt"
                : externalVisual != null
                    ? "HAON / Standard"
                    : "Primitive fallback";

        public bool EditorExclusiveSkinFallback =>
            ExclusiveVisualFallback;

        // Actual Animator clip, useful if a third-party animation produces
        // an unexpected bind/T-pose during Editor QA.
        public string EditorCurrentAnimationClip
        {
            get
            {
                if (externalAnimator == null ||
                    !externalAnimator.isActiveAndEnabled ||
                    externalAnimator.runtimeAnimatorController == null)
                {
                    return "Animator отсутствует";
                }

                AnimatorClipInfo[] clips =
                    externalAnimator.GetCurrentAnimatorClipInfo(0);

                return clips.Length > 0 && clips[0].clip != null
                    ? clips[0].clip.name
                    : "нет активного клипа";
            }
        }

        public void SetEditorRoofHeightOffset(float totalOffsetMeters)
        {
            editorRoofHeightAdjustment =
                Mathf.Clamp(
                    totalOffsetMeters - ActiveSkinRoofHeightOffset,
                    -0.60f,
                    0.60f);

            if (car != null)
                visualAnchorLocal = ResolveVisualAnchor();
        }

        public void ResetEditorRoofHeightOffset()
        {
            editorRoofHeightAdjustment = 0f;

            if (car != null)
                visualAnchorLocal = ResolveVisualAnchor();
        }
#endif

        private float animationReactionTimer;
        private float ambientPoseTimer;
        private float nextAmbientPoseSeconds;
        private float ambientReactionTimer;
        private float nextAmbientReactionSeconds;
        private int ambientPoseIndex;
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

        // Read-only UX progress from the very same threshold used by AddXp.
        // No XP is consumed or awarded by opening the Player Experience page.
        public int XpForNextLevelTarget =>
            Level >= 10 ? 0 : XpForNextLevel(Level);
        public int XpRemainingToNextLevel =>
            Level >= 10 ? 0 : Mathf.Max(0, XpForNextLevel(Level) - Xp);
        public int CatalogDailyProgress => dailyProgress;
        public int CatalogDailyTarget => dailyTarget;
        public bool CatalogDailyClaimed => dailyClaimed;
        public int CatalogDailyCreditsReward => dailyClaimed ? 0 : 300 + Level * 35;
        public int CatalogDailyXpReward => dailyClaimed ? 0 : 45;

        public bool ExclusiveVisualActive => usingKisoraVisual;
        public bool ExclusiveVisualFallback =>
            useSupporterPixieSkin && !usingKisoraVisual;

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
                            "pixie.daily_done");
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
                        "pixie.mood_happy");
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
            if (Time.timeScale <= 0f)
                return;

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
            if (Time.timeScale <= 0f ||
                visualRoot == null ||
                car == null)
            {
                return;
            }

            Transform vehicleVisual =
                car.transform.Find(
                    "MotorCityVehicleVisual_Runtime");

            if (vehicleVisual != observedVehicleVisual)
            {
                observedVehicleVisual = vehicleVisual;
                visualAnchorLocal = ResolveVisualAnchor();
            }

            // Pixie now STANDS on the roof instead of hovering.
            // Follow the exact vehicle transform every frame: no bobbing,
            // sway or interpolated positional lag on acceleration/turns.
            visualRoot.transform.position =
                car.transform.TransformPoint(visualAnchorLocal);
            visualRoot.transform.rotation =
                car.transform.rotation;
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
            int amount,
            bool saveImmediately = true)
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

            SaveProgress(
                saveImmediately);

            if (!leveled)
                return;

            ApplyAbilities();
            RefreshVisualSkin();
            PlayPixieReaction(
                "Pixie Victory",
                2.2f);

            StatusText =
                MotorCityLocalization.Format(
                    "pixie.level_up",
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

            if (activityManager != null &&
                !activityManager.SecondaryProgressionAllowed)
            {
                return;
            }

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
                        "pixie.daily_progress",
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
                    "pixie.daily_reward",
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
                        "pixie.hello");

                messageTimer =
                    MessageSeconds + 1f;

                return;
            }

            if (missedDays >= 1L)
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "pixie.reunion");

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
                    "pixie.new_daily");

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
                    car?.ActivatePixieBoost(
                        1.30f,
                        3.2f);

                    PlayPixieReaction(
                        "Pixie Boost",
                        1.25f);

                    StatusText =
                        MotorCityLocalization.Text(
                            "pixie.boost");

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
                    "pixie.hint");

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

            // Determine which prefab actually loaded before computing the
            // roof anchor. EX missing -> HAON animation and HAON +0.05m
            // offset rather than EX-only states on a standard controller.
            GameObject authoredPrefab = useSupporterPixieSkin
                ? Resources.Load<GameObject>(
                    "MotorCity/Pixie/AmaneKisoraVisual")
                : null;
            usingKisoraVisual = authoredPrefab != null;
            if (authoredPrefab == null)
                authoredPrefab = Resources.Load<GameObject>(
                    "MotorCity/Byte/HaonByteVisual");

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

            if (authoredPrefab != null)
            {
                externalVisual =
                    Instantiate(
                        authoredPrefab,
                        visualRoot.transform,
                        false);

                externalVisual.name =
                    usingKisoraVisual
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
                    (usingKisoraVisual
                        ? 0.58f
                        : 0.66f);

#if UNITY_WEBGL && !UNITY_EDITOR
                ConvertPixieMaterialsForWeb(
                    externalVisual);
#endif

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

                AlignPixieFeetToRoof();
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

        private void AlignPixieFeetToRoof()
        {
            if (externalVisual == null ||
                externalAnimator == null ||
                externalAnimator.avatar == null ||
                !externalAnimator.isHuman)
            {
                return;
            }

            // A prefab root is not necessarily at the character's feet.
            // Use animated humanoid foot bones to place the shoes on the
            // vehicle roof rather than placing the prefab's pivot there.
            Transform left = externalAnimator.GetBoneTransform(
                HumanBodyBones.LeftFoot);
            Transform right = externalAnimator.GetBoneTransform(
                HumanBodyBones.RightFoot);

            if (left == null && right == null)
                return;

            float lowestAnkleY = float.PositiveInfinity;

            if (left != null)
                lowestAnkleY = Mathf.Min(
                    lowestAnkleY,
                    visualRoot.transform.InverseTransformPoint(
                        left.position).y);

            if (right != null)
                lowestAnkleY = Mathf.Min(
                    lowestAnkleY,
                    visualRoot.transform.InverseTransformPoint(
                        right.position).y);

            // Foot bones usually sit at the ankle, not the shoe sole.
            // The modest margin avoids planting the shoes inside the roof.
            const float SoleBelowAnkle = 0.055f;
            externalVisual.transform.localPosition -=
                Vector3.up * (lowestAnkleY - SoleBelowAnkle);
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
                return new Vector3(0f, 1.65f, 0f);

            Bounds bounds = ResolveCarLocalBounds();
            Vector3 roofCenter = new Vector3(
                bounds.center.x,
                bounds.max.y + 0.01f,
                bounds.center.z);

            // Rear towers/spoilers on cars such as the DeLorean are higher
            // than the cabin roof. Use the central body collision proxy
            // instead of the global maximum height of every renderer.
            // This works immediately after prefab creation, before physics
            // collider transforms have synchronized for raycasts.
            Transform vehicleVisual = car.transform.Find(
                "MotorCityVehicleVisual_Runtime");

            if (vehicleVisual != null)
            {
                float bestRoofY = float.NegativeInfinity;
                Vector3 sampleWorld =
                    car.transform.TransformPoint(roofCenter);

                foreach (BoxCollider collider in
                         vehicleVisual.GetComponentsInChildren<BoxCollider>(
                             true))
                {
                    if (collider == null ||
                        !collider.enabled ||
                        !collider.name.StartsWith(
                            "BodyCollider_",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Vector3 localSample =
                        collider.transform.InverseTransformPoint(sampleWorld);
                    Vector3 offset = localSample - collider.center;

                    if (Mathf.Abs(offset.x) >
                            collider.size.x * 0.5f + 0.015f ||
                        Mathf.Abs(offset.z) >
                            collider.size.z * 0.5f + 0.015f)
                    {
                        continue;
                    }

                    Vector3 contactLocal = new Vector3(
                        localSample.x,
                        collider.center.y + collider.size.y * 0.5f,
                        localSample.z);

                    float surfaceY = car.transform.InverseTransformPoint(
                        collider.transform.TransformPoint(contactLocal)).y;

                    if (surfaceY > bestRoofY)
                        bestRoofY = surfaceY;
                }

                if (bestRoofY > float.NegativeInfinity)
                {
                    // Proxy boxes sit inside the visible shell.
                    roofCenter.y = bestRoofY + 0.045f;
                }
            }

            // Applies in Editor and builds alike. Do not put these values
            // behind UNITY_EDITOR: otherwise released skins sit too low.
            roofCenter.y += ActiveSkinRoofHeightOffset;

#if UNITY_EDITOR
            roofCenter.y += editorRoofHeightAdjustment;
#endif
            return roofCenter;
        }

        private Bounds ResolveCarLocalBounds()
        {
            if (car == null)
            {
                return new Bounds(
                    Vector3.zero,
                    new Vector3(
                        1.8f,
                        1.4f,
                        4f));
            }

            // Pixie's anchor must be based on the actual vehicle body only.
            // Using every Renderer under the car also includes runtime neon,
            // effects, helper geometry and other presentation objects. A bad
            // helper bound can otherwise move Pixie hundreds of metres away.
            Transform vehicleVisual =
                car.transform.Find(
                    "MotorCityVehicleVisual_Runtime");

            Renderer[] renderers =
                vehicleVisual != null
                    ? vehicleVisual.GetComponentsInChildren<Renderer>(
                        true)
                    : Array.Empty<Renderer>();

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
                if (renderer == null ||
                    !renderer.enabled)
                {
                    continue;
                }

                Bounds world =
                    renderer.bounds;

                Vector3 center =
                    car.transform.InverseTransformPoint(
                        world.center);

                Vector3 extentsWorld =
                    world.extents;

                Vector3 size =
                    new(
                        Mathf.Abs(
                            car.transform.InverseTransformVector(
                                new Vector3(
                                    extentsWorld.x,
                                    0f,
                                    0f)).x) * 2f,
                        Mathf.Abs(
                            car.transform.InverseTransformVector(
                                new Vector3(
                                    0f,
                                    extentsWorld.y,
                                    0f)).y) * 2f,
                        Mathf.Abs(
                            car.transform.InverseTransformVector(
                                new Vector3(
                                    0f,
                                    0f,
                                    extentsWorld.z)).z) * 2f);

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

            if (!initialized)
            {
                BoxCollider chassis =
                    car.GetComponent<BoxCollider>();

                if (chassis != null)
                {
                    local =
                        new Bounds(
                            chassis.center,
                            chassis.size);
                }
            }

            // Final sanity bounds. Pixie is a close companion and should never
            // derive an anchor from an obviously invalid presentation bound.
            local.extents =
                new Vector3(
                    Mathf.Clamp(
                        local.extents.x,
                        0.45f,
                        2.2f),
                    Mathf.Clamp(
                        local.extents.y,
                        0.45f,
                        2.2f),
                    Mathf.Clamp(
                        local.extents.z,
                        1.0f,
                        4.5f));

            local.center =
                new Vector3(
                    Mathf.Clamp(
                        local.center.x,
                        -1.5f,
                        1.5f),
                    Mathf.Clamp(
                        local.center.y,
                        0f,
                        2.0f),
                    Mathf.Clamp(
                        local.center.z,
                        -1.5f,
                        1.5f));

            return local;
        }

        private void UpdatePixieAnimation()
        {
            if (!usingHaonVisual || externalAnimator == null)
                return;

            // The source SapphiArt controller supports walk, run and a
            // "default" pose that can show the bind/T-pose on this rig.
            // We only request known playable idle/winpose/jump clips.
            // Leave its third-party controller asset unchanged.
            if (usingKisoraVisual)
            {
                externalAnimator.SetBool(
                    "param_idletorunning",
                    false);
                externalAnimator.SetBool(
                    "param_idletowalk",
                    false);
                externalAnimator.SetBool(
                    "param_idletodefault",
                    false);
            }

            if (animationReactionTimer > 0f)
            {
                animationReactionTimer = Mathf.Max(
                    0f,
                    animationReactionTimer - Time.unscaledDeltaTime);

                if (animationReactionTimer > 0f)
                    return;
            }

            // Each companion chooses a new pose roughly every 6-12s,
            // rather than switching exactly in lockstep with the car.
            if (nextAmbientPoseSeconds <= 0f)
                nextAmbientPoseSeconds = UnityEngine.Random.Range(6f, 12f);

            if (nextAmbientReactionSeconds <= 0f)
                nextAmbientReactionSeconds = UnityEngine.Random.Range(15f, 25f);

            ambientPoseTimer += Time.unscaledDeltaTime;
            ambientReactionTimer += Time.unscaledDeltaTime;

            // Rare lively animations while standing; ordinary gameplay
            // reactions still interrupt these and take precedence.
            if (ambientReactionTimer >= nextAmbientReactionSeconds)
            {
                ambientReactionTimer = 0f;
                nextAmbientReactionSeconds =
                    UnityEngine.Random.Range(15f, 25f);

                if (usingKisoraVisual &&
                    UnityEngine.Random.value < 0.12f)
                {
                    // Very rare supporter-only playful jump animation.
                    // Does NOT invoke the driving boost ability.
                    PlayPixieReaction("Pixie Playful Jump", 1.25f);
                }
                else if (usingKisoraVisual ||
                         UnityEngine.Random.value < 0.75f)
                {
                    PlayPixieReaction("Pixie Clap", 1.8f);
                }
                else
                {
                    PlayPixieReaction("Pixie Victory", 2.0f);
                }

                return;
            }

            if (ambientPoseTimer >= nextAmbientPoseSeconds)
            {
                ambientPoseTimer = 0f;
                nextAmbientPoseSeconds = UnityEngine.Random.Range(6f, 12f);

                // HAON safely alternates StandA / StandB / StandC.
                // SapphiArt's "default" clip may show a T-pose, so its
                // passive state stays on the verified looping "idle".
                // Variety for that skin comes from occasional winpose/jump
                // reactions, NOT a bind-pose-like ambient state.
                ambientPoseIndex = usingKisoraVisual
                    ? 0
                    : (ambientPoseIndex + UnityEngine.Random.Range(1, 3)) % 3;
            }

            string state = ambientPoseIndex switch
            {
                1 => "Pixie Roof Pose",
                2 => "Pixie Idle Alt",
                _ => "Pixie Idle"
            };

            externalAnimator.speed = 1f;
            PlayAnimatorState(state, 0.22f);
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

            // Space out decorative reactions after any actual reward
            // or ability so they never fire immediately back-to-back.
            ambientReactionTimer = 0f;
            nextAmbientReactionSeconds =
                UnityEngine.Random.Range(15f, 25f);

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
                usingKisoraVisual
                    ? MapKisoraState(
                        state)
                    : state;

            string fullStateName =
                (usingKisoraVisual
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
                if (usingKisoraVisual)
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
                "Pixie Victory" or "Pixie Clap" => "winpose",
                "Pixie Boost" or "Pixie Playful Jump" => "jump",
                "Pixie Roof Pose" or "Pixie Idle Alt" or
                "Pixie Idle" => "idle",
                _ => "idle"
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

#if UNITY_EDITOR
            // A temporary fine-adjustment for one skin must not carry
            // over when QA switches to the other skin.
            editorRoofHeightAdjustment = 0f;
#endif

            if (car != null)
                RebuildVisual();
        }

        private void RebuildVisual()
        {
            if (visualRoot != null)
            {
                // Destroy() is deferred until the end of the frame.
                // Hide the previous skin immediately so QA/player never
                // sees two Pixie models on the roof during a skin change.
                visualRoot.SetActive(false);
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
            usingKisoraVisual =
                false;
            currentAnimatorStateHash =
                0;
            observedVehicleVisual =
                null;
            ambientPoseIndex = 0;
            ambientPoseTimer = 0f;
            nextAmbientPoseSeconds = 0f;
            ambientReactionTimer = 0f;
            nextAmbientReactionSeconds = 0f;
            animationReactionTimer = 0f;

            BuildVisual();
        }

        private static void ConvertPixieMaterialsForWeb(
            GameObject root)
        {
            if (root == null)
                return;

            Shader urpLit =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            if (urpLit == null)
            {
                Debug.LogWarning(
                    "Motor City: URP/Lit shader is unavailable for the Web Pixie fallback.");

                return;
            }

            var converted =
                new Dictionary<Material, Material>();

            foreach (Renderer renderer in
                     root.GetComponentsInChildren<Renderer>(
                         true))
            {
                if (renderer == null)
                    continue;

                Material[] source =
                    renderer.sharedMaterials;

                if (source == null ||
                    source.Length == 0)
                {
                    continue;
                }

                Material[] assigned =
                    new Material[source.Length];

                bool changed =
                    false;

                for (int i = 0;
                     i < source.Length;
                     i++)
                {
                    Material original =
                        source[i];

                    if (original == null)
                    {
                        assigned[i] =
                            null;

                        continue;
                    }

                    string shaderName =
                        original.shader != null
                            ? original.shader.name
                            : string.Empty;

                    if (shaderName.StartsWith(
                            "Universal Render Pipeline/",
                            StringComparison.Ordinal))
                    {
                        assigned[i] =
                            original;

                        continue;
                    }

                    if (converted.TryGetValue(
                            original,
                            out Material cached) &&
                        cached != null)
                    {
                        assigned[i] =
                            cached;

                        changed =
                            true;

                        continue;
                    }

                    string textureProperty =
                        null;

                    Texture mainTexture =
                        null;

                    string[] preferredTextureProperties =
                    {
                        "_BaseMap",
                        "_MainTex",
                        "_1st_ShadeMap",
                        "_BaseColorMap"
                    };

                    string[] availableTextureProperties =
                        original.GetTexturePropertyNames();

                    foreach (string preferred in
                             preferredTextureProperties)
                    {
                        if (Array.IndexOf(
                                availableTextureProperties,
                                preferred) < 0)
                        {
                            continue;
                        }

                        Texture candidate =
                            original.GetTexture(
                                preferred);

                        if (candidate == null)
                            continue;

                        textureProperty =
                            preferred;
                        mainTexture =
                            candidate;
                        break;
                    }

                    if (mainTexture == null)
                    {
                        foreach (string propertyName in
                                 availableTextureProperties)
                        {
                            Texture candidate =
                                original.GetTexture(
                                    propertyName);

                            if (candidate == null)
                                continue;

                            textureProperty =
                                propertyName;
                            mainTexture =
                                candidate;
                            break;
                        }
                    }

                    Color color =
                        Color.white;

                    if (original.HasProperty(
                            "_BaseColor"))
                    {
                        color =
                            original.GetColor(
                                "_BaseColor");
                    }
                    else if (original.HasProperty(
                                 "_Color"))
                    {
                        color =
                            original.GetColor(
                                "_Color");
                    }

                    Material runtime =
                        new Material(
                            urpLit)
                        {
                            name =
                                original.name +
                                "_MotorCityWeb",
                            hideFlags =
                                HideFlags.DontSave
                        };

                    if (mainTexture != null &&
                        runtime.HasProperty(
                            "_BaseMap"))
                    {
                        runtime.SetTexture(
                            "_BaseMap",
                            mainTexture);

                        if (!string.IsNullOrEmpty(
                                textureProperty))
                        {
                            runtime.SetTextureScale(
                                "_BaseMap",
                                original.GetTextureScale(
                                    textureProperty));

                            runtime.SetTextureOffset(
                                "_BaseMap",
                                original.GetTextureOffset(
                                    textureProperty));
                        }
                    }

                    if (runtime.HasProperty(
                            "_BaseColor"))
                    {
                        runtime.SetColor(
                            "_BaseColor",
                            color);
                    }

                    if (runtime.HasProperty(
                            "_Smoothness"))
                    {
                        runtime.SetFloat(
                            "_Smoothness",
                            0.35f);
                    }

                    converted[
                        original] =
                        runtime;

                    assigned[i] =
                        runtime;

                    changed =
                        true;
                }

                if (changed)
                {
                    renderer.sharedMaterials =
                        assigned;
                }
            }
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

        private void SaveProgress(
            bool flush = true)
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                LevelKey,
                Level);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                XpKey,
                Xp);

            if (flush)
            {
                MotorCity.Persistence.MotorCitySaveService.Save();
            }
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
                        "pixie.daily_race",
                    2 =>
                        "pixie.daily_drift",
                    _ =>
                        "pixie.daily_any"
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
