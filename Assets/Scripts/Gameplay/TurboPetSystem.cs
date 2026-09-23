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
        private const string SkinMaskKey =
            "MotorCity.Turbo.SkinMask";
        private const string SelectedSkinKey =
            "MotorCity.Turbo.SelectedSkin";

        private const float MessageSeconds =
            4f;
        private const float HintDelaySeconds =
            22f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private DiscoverySystem discoveries;

        private GameObject visualRoot;
        private Vector3 visualAnchorLocal;
        private float visualAnchorRefreshTimer;
        private float messageTimer;
        private float activeSeconds;
        private string observedActivityId;

        private int dailyType;
        private int dailyTarget;
        private int dailyProgress;
        private bool dailyClaimed;
        private long currentDay;
        private int unlockedSkinMask;
        private int selectedSkin;

        public int Level { get; private set; }
        public int Xp { get; private set; }

        public int SelectedSkin =>
            selectedSkin;

        public string SkinName =>
            MotorCityLocalization.Text(
                SkinNameKey(
                    selectedSkin));

        public string GarageLine =>
            MotorCityLocalization.Format(
                "turbo.garage_skin",
                SkinName);

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

            unlockedSkinMask =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    SkinMaskKey,
                    1) |
                1;

            selectedSkin =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        SelectedSkinKey,
                        0),
                    0,
                    9);

            if (!IsSkinUnlocked(
                    selectedSkin))
            {
                selectedSkin = 0;
            }

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
            RefreshDayIfNeeded();
        }

        private void LateUpdate()
        {
            if (visualRoot == null ||
                car == null)
            {
                return;
            }

            visualAnchorRefreshTimer -=
                Time.unscaledDeltaTime;

            if (visualAnchorRefreshTimer <= 0f)
            {
                visualAnchorRefreshTimer =
                    0.25f;

                visualAnchorLocal =
                    ResolveVisualAnchor();
            }

            // Byte is a companion attached to the player vehicle, not a free
            // world object. Keep it close to the car even after changing
            // vehicles or rebuilding the runtime visual/wheel rig.
            visualRoot.transform.localPosition =
                Vector3.Lerp(
                    visualRoot.transform.localPosition,
                    visualAnchorLocal,
                    1f -
                    Mathf.Exp(
                        -12f *
                        Time.unscaledDeltaTime));

            visualRoot.transform.localRotation =
                Quaternion.identity;
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityCompleted -=
                    OnActivityCompleted;
            }
        }

        public bool IsSkinUnlocked(
            int skinIndex)
        {
            if (skinIndex < 0 ||
                skinIndex > 9)
            {
                return false;
            }

            return
                (unlockedSkinMask &
                 (1 << skinIndex)) != 0;
        }

        public void UnlockSkin(
            int skinIndex)
        {
            if (skinIndex <= 0 ||
                skinIndex > 9 ||
                IsSkinUnlocked(
                    skinIndex))
            {
                return;
            }

            unlockedSkinMask |=
                1 << skinIndex;

            selectedSkin =
                skinIndex;

            SaveSkin();
            RefreshVisualSkin();

            StatusText =
                MotorCityLocalization.Format(
                    "turbo.skin_unlocked",
                    SkinName);

            messageTimer =
                MessageSeconds + 1f;
        }

        public void CycleSkin()
        {
            for (int offset = 1;
                 offset <= 10;
                 offset++)
            {
                int candidate =
                    (selectedSkin +
                     offset) %
                    10;

                if (!IsSkinUnlocked(
                        candidate))
                {
                    continue;
                }

                selectedSkin =
                    candidate;

                SaveSkin();
                RefreshVisualSkin();

                StatusText =
                    MotorCityLocalization.Format(
                        "turbo.skin_selected",
                        SkinName);

                messageTimer =
                    MessageSeconds;

                return;
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
                    "Byte Robo Cat");

            visualRoot.transform.SetParent(
                car.transform,
                false);

            visualAnchorLocal =
                ResolveVisualAnchor();

            visualRoot.transform.localPosition =
                visualAnchorLocal;

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
                    carWidth * 0.11f,
                    0.16f,
                    0.28f);

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

            BoxCollider chassis =
                car.GetComponent<BoxCollider>();

            if (chassis != null &&
                chassis.enabled)
            {
                Vector3 center =
                    chassis.center;

                Vector3 size =
                    chassis.size;

                // Hover just outside the passenger-side upper body. Using the
                // physical chassis instead of aggregate renderer bounds avoids
                // distant wheel/overlay/runtime renderers pushing Turbo far
                // away from the actual vehicle.
                return
                    new Vector3(
                        center.x +
                        size.x * 0.50f +
                        0.42f,
                        center.y +
                        size.y * 0.22f,
                        center.z -
                        size.z * 0.08f);
            }

            Bounds bounds =
                ResolveCarLocalBounds();

            return
                new Vector3(
                    bounds.max.x +
                    0.18f,
                    bounds.max.y -
                    bounds.size.y * 0.18f,
                    bounds.center.z +
                    bounds.size.z * 0.08f);
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

        private void RefreshVisualSkin()
        {
            if (visualRoot == null)
                return;

            Color bodyColor =
                selectedSkin switch
                {
                    1 =>
                        new Color(
                            0.12f,
                            0.70f,
                            1f,
                            1f),
                    2 =>
                        new Color(
                            1f,
                            0.52f,
                            0.12f,
                            1f),
                    3 =>
                        new Color(
                            0.72f,
                            0.30f,
                            1f,
                            1f),
                    4 =>
                        new Color(
                            1f,
                            0.78f,
                            0.16f,
                            1f),
                    5 =>
                        new Color(
                            0.12f,
                            1f,
                            0.82f,
                            1f),
                    6 =>
                        new Color(
                            1f,
                            0.24f,
                            0.56f,
                            1f),
                    7 =>
                        new Color(
                            0.22f,
                            0.95f,
                            0.62f,
                            1f),
                    8 =>
                        new Color(
                            0.36f,
                            0.22f,
                            1f,
                            1f),
                    9 =>
                        new Color(
                            1f,
                            0.32f,
                            0.08f,
                            1f),
                    _ =>
                        new Color(
                            0.92f,
                            0.95f,
                            1f,
                            1f)
                };

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

        private void SaveSkin()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                SkinMaskKey,
                unlockedSkinMask);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                SelectedSkinKey,
                selectedSkin);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private static string SkinNameKey(
            int skinIndex)
        {
            return
                skinIndex switch
                {
                    1 => "turbo.skin_blue",
                    2 => "turbo.skin_orange",
                    3 => "turbo.skin_purple",
                    4 => "turbo.skin_gold",
                    5 => "turbo.skin_season1",
                    6 => "turbo.skin_premium_1",
                    7 => "turbo.skin_premium_2",
                    8 => "turbo.skin_premium_3",
                    9 => "turbo.skin_cosmetic_pack",
                    _ => "turbo.skin_classic"
                };
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
