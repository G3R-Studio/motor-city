using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class StreetSprintActivity : MonoBehaviour
    {
        private const string ActivityId = "sprint";
        private const string BestTimeKey =
            "MotorCity.Sprint.BestTime";
        private const int EliteRequiredLevel = 3;
        private const int RookieRewardCredits = 250;
        private const float RookieGoldTimeSeconds = 75f;
        private const float RookieSilverTimeSeconds = 100f;
        private const float RookieBronzeTimeSeconds = 130f;

        [Header("Награда")]
        [SerializeField] private int baseRewardCredits = 550;
        [SerializeField] private int maximumTimeBonusCredits = 450;

        [Header("Пороги времени")]
        [SerializeField] private float goldTimeSeconds = 200f;
        [SerializeField] private float silverTimeSeconds = 245f;
        [SerializeField] private float bronzeTimeSeconds = 300f;

        [Header("Старт")]
        [SerializeField] private float startRadius = 14f;
        [SerializeField] private float checkpointRadius = 14f;
        [SerializeField] private float maxStartSpeedKph = 8f;
        [SerializeField] private float countdownSeconds = 3f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private Vector3[] route;
        private Vector3[] rookieRoute;
        private int checkpointIndex;
        private bool armed = true;
        private bool isCountingDown;
        private bool eliteMode;
        private bool rookieMode;

        public bool IsActive { get; private set; }
        public bool IsCountingDown => isCountingDown;
        public bool IsNearStart { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public float BestTimeSeconds { get; private set; }
        public int CheckpointIndex => checkpointIndex;
        public int CheckpointCount =>
            CurrentRoute?.Length ?? 0;

        private Vector3[] CurrentRoute
        {
            get
            {
                bool rookieContext =
                    rookieMode ||
                    (!IsActive &&
                     !isCountingDown &&
                     activityManager != null &&
                     activityManager.IsRookieSprintStep);

                if (rookieContext &&
                    rookieRoute != null &&
                    rookieRoute.Length >= 2)
                {
                    return rookieRoute;
                }

                return route;
            }
        }

        public Vector3 CurrentTarget
        {
            get
            {
                Vector3[] activeRoute =
                    CurrentRoute;

                return
                    activeRoute == null ||
                    activeRoute.Length == 0
                        ? Vector3.zero
                        : activeRoute[Mathf.Clamp(
                            checkpointIndex,
                            0,
                            activeRoute.Length - 1)];
            }
        }

        public bool TryGetNextTarget(
            out Vector3 target)
        {
            target =
                CurrentTarget;

            Vector3[] activeRoute =
                CurrentRoute;

            if (activeRoute == null ||
                activeRoute.Length < 2)
            {
                return false;
            }

            int nextIndex =
                checkpointIndex + 1;

            if (nextIndex < 0 ||
                nextIndex >= activeRoute.Length)
            {
                return false;
            }

            target =
                activeRoute[nextIndex];

            return true;
        }

        public string StatusText { get; private set; } =
            MotorCityLocalization.Text("activity.marker.sprint");

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager manager)
        {
            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            route = CityAssetRuntimeInstaller.SprintRoute;
            rookieRoute =
                BuildRookieRoute(
                    route);

            BestTimeSeconds =
                Mathf.Max(
                    0f,
                    MotorCity.Persistence.MotorCitySaveService.GetFloat(
                        BestTimeKey,
                        0f));
        }

        private void Update()
        {
            Vector3[] activeRoute =
                CurrentRoute;

            if (car == null ||
                wallet == null ||
                activityManager == null ||
                activeRoute == null ||
                activeRoute.Length < 2)
                return;

            if (isCountingDown)
            {
                IsNearStart = false;

                if (MotorCityInput.CancelPressed)
                {
                    CancelActivity();
                }

                return;
            }

            if (IsActive)
            {
                IsNearStart = false;

                if (MotorCityInput.CancelPressed)
                {
                    CancelActivity();
                    return;
                }

                UpdateActiveSprint();
                return;
            }

            checkpointIndex = 0;

            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(activeRoute[0]));

            IsNearStart =
                distance <= startRadius;

            if (!armed)
            {
                IsNearStart = false;

                if (distance >
                    startRadius + 4f)
                {
                    armed = true;
                    StatusText =
                        MotorCityLocalization.Text("activity.marker.sprint");
                }

                return;
            }

            if (!IsNearStart)
            {
                StatusText =
                    MotorCityLocalization.Text("activity.marker.sprint");
                return;
            }

            if (activityManager.IsBusy &&
                !activityManager.IsActive(ActivityId))
            {
                StatusText =
                    MotorCityLocalization.Format("activity.busy", MotorCityLocalization.Text("activity.sprint"), activityManager.ActiveName);
                return;
            }

            if (car.SpeedKph >
                maxStartSpeedKph)
            {
                StatusText =
                    MotorCityLocalization.Format("activity.stop", MotorCityLocalization.Text("hud.sprint"), maxStartSpeedKph);
                return;
            }

            string best =
                BestTimeSeconds > 0f
                    ? MotorCityLocalization.Format("activity.best_short", BestTimeSeconds)
                    : string.Empty;

            bool eliteUnlocked =
                activityManager.HasDisciplineLevel(
                    DisciplineType.Racing,
                    EliteRequiredLevel);

            string eliteHint =
                eliteUnlocked
                    ? MotorCityLocalization.Text("activity.elite_hint")
                    : MotorCityLocalization.Format("activity.elite_locked", MotorCityLocalization.Text("discipline.racing"), EliteRequiredLevel);

            StatusText =
                MotorCityLocalization.Format(
                    "activity.start_time",
                    MotorCityLocalization.Text("hud.sprint"),
                    goldTimeSeconds,
                    silverTimeSeconds,
                    bronzeTimeSeconds,
                    best,
                    eliteHint);

            bool elitePressed =
                MotorCityInput.EliteModifierPressed;

            if (elitePressed &&
                !eliteUnlocked)
            {
                return;
            }

            if (elitePressed &&
                car != null &&
                car.SpeedKph > 1f)
            {
                return;
            }

            if (MotorCityInput.InteractPressed ||
                elitePressed)
            {
                rookieMode =
                    activityManager.IsRookieSprintStep;

                eliteMode =
                    !rookieMode &&
                    elitePressed;

                BeginCountdown();
            }
        }

        private void BeginCountdown()
        {
            activityManager.RequestStart(
                ActivityId,
                MotorCityLocalization.Text("activity.sprint"),
                countdownSeconds,
                BeginPreparedCountdown,
                UpdateCountdownStatus,
                BeginGameplay);
        }

        private void BeginPreparedCountdown()
        {
            isCountingDown = true;
            armed = false;
            ElapsedSeconds = 0f;
            checkpointIndex = 0;

            car.SetDrivingBlocked("ActivityCountdown", true);
        }

        private void UpdateCountdownStatus(
            int shown)
        {
            string activityName =
                rookieMode
                    ? MotorCityLocalization.Text(
                        "onboarding.first_race_title")
                    : eliteMode
                        ? MotorCityLocalization.Text(
                            "activity.elite_sprint")
                        : MotorCityLocalization.Text(
                            "hud.sprint");

            StatusText =
                shown <= 0
                    ? MotorCityLocalization.Format(
                        "activity.go",
                        activityName)
                    : MotorCityLocalization.Format(
                        "activity.countdown",
                        activityName,
                        shown);
        }

        private void BeginGameplay()
        {
            isCountingDown = false;
            IsActive = true;
            ElapsedSeconds = 0f;
            checkpointIndex = 1;

            car.SetDrivingBlocked("ActivityCountdown", false);

            UpdateStatus();
        }

        private void UpdateActiveSprint()
        {
            ElapsedSeconds +=
                Time.deltaTime;

            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(CurrentTarget));

            if (distance >
                checkpointRadius)
            {
                UpdateStatus();
                return;
            }

            checkpointIndex++;

            Vector3[] activeRoute =
                CurrentRoute;

            if (activeRoute == null ||
                checkpointIndex >=
                activeRoute.Length)
            {
                CompleteSprint();
                return;
            }

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            StatusText =
                MotorCityLocalization.Format(
                    "activity.checkpoint",
                    rookieMode
                        ? MotorCityLocalization.Text(
                            "onboarding.first_race_title")
                        : eliteMode
                            ? MotorCityLocalization.Text(
                                "activity.elite_sprint")
                            : MotorCityLocalization.Text(
                                "hud.sprint"),
                    checkpointIndex + 1,
                    CurrentRoute == null
                        ? 0
                        : CurrentRoute.Length,
                    ElapsedSeconds,
                    CurrentTierHint());
        }

        private string CurrentTierHint()
        {
            float gold =
                rookieMode
                    ? RookieGoldTimeSeconds
                    : eliteMode
                        ? 180f
                        : goldTimeSeconds;

            float silver =
                rookieMode
                    ? RookieSilverTimeSeconds
                    : eliteMode
                        ? 225f
                        : silverTimeSeconds;

            float bronze =
                rookieMode
                    ? RookieBronzeTimeSeconds
                    : eliteMode
                        ? 275f
                        : bronzeTimeSeconds;

            if (ElapsedSeconds <= gold)
                return MotorCityLocalization.Format("activity.tier_time", MotorCityLocalization.Text("medal.gold"), gold);

            if (ElapsedSeconds <= silver)
                return MotorCityLocalization.Format("activity.tier_time", MotorCityLocalization.Text("medal.silver"), silver);

            if (ElapsedSeconds <= bronze)
                return MotorCityLocalization.Format("activity.tier_time", MotorCityLocalization.Text("medal.bronze"), bronze);

            return MotorCityLocalization.Text("activity.finish_now");
        }

        private void CompleteSprint()
        {
            bool completedRookieRace =
                rookieMode;

            float gold =
                completedRookieRace
                    ? RookieGoldTimeSeconds
                    : eliteMode
                        ? 180f
                        : goldTimeSeconds;

            float silver =
                completedRookieRace
                    ? RookieSilverTimeSeconds
                    : eliteMode
                        ? 225f
                        : silverTimeSeconds;

            float bronze =
                completedRookieRace
                    ? RookieBronzeTimeSeconds
                    : eliteMode
                        ? 275f
                        : bronzeTimeSeconds;

            string tier =
                ElapsedSeconds <= gold
                    ? MotorCityLocalization.Text("medal.gold")
                    : ElapsedSeconds <= silver
                        ? MotorCityLocalization.Text("medal.silver")
                        : ElapsedSeconds <= bronze
                            ? MotorCityLocalization.Text("medal.bronze")
                            : MotorCityLocalization.Text("common.finish");

            int bonus =
                completedRookieRace
                    ? 0
                    : Mathf.RoundToInt(
                        Mathf.Lerp(
                            maximumTimeBonusCredits,
                            0f,
                            Mathf.InverseLerp(
                                eliteMode ? 165f : 185f,
                                eliteMode
                                    ? 275f
                                    : bronzeTimeSeconds,
                                ElapsedSeconds)));

            int reward =
                completedRookieRace
                    ? RookieRewardCredits
                    : baseRewardCredits +
                      bonus;

            if (!completedRookieRace &&
                eliteMode)
            {
                reward =
                    Mathf.RoundToInt(
                        reward * 1.6f);
            }

            bool newBest =
                !completedRookieRace &&
                (BestTimeSeconds <= 0f ||
                 ElapsedSeconds <
                    BestTimeSeconds);

            if (newBest)
            {
                BestTimeSeconds =
                    ElapsedSeconds;

                MotorCity.Persistence.MotorCitySaveService.SetFloat(
                    BestTimeKey,
                    BestTimeSeconds);

                MotorCity.Persistence.MotorCitySaveService.Save();
            }

            wallet.AddCredits(
                reward);

            IsActive = false;
            checkpointIndex = 0;

            car.SetDrivingBlocked(
                "ActivityResult",
                true);

            string record =
                !completedRookieRace &&
                newBest
                    ? MotorCityLocalization.Text(
                        "activity.new_record_inline")
                    : !completedRookieRace &&
                      BestTimeSeconds > 0f
                        ? MotorCityLocalization.Format(
                            "activity.record_inline",
                            BestTimeSeconds)
                        : string.Empty;

            activityManager.ShowResult(
                ActivityId,
                completedRookieRace
                    ? MotorCityLocalization.Text(
                        "onboarding.first_race_title")
                    : eliteMode
                        ? MotorCityLocalization.Text(
                            "activity.elite_sprint")
                        : MotorCityLocalization.Text(
                            "activity.sprint"),
                MotorCityLocalization.Format(
                    "activity.result_primary_time",
                    ElapsedSeconds),
                completedRookieRace
                    ? MotorCityLocalization.Format(
                        "onboarding.first_race_result",
                        tier)
                    : MotorCityLocalization.Format(
                        "activity.result_tier_bonus_record",
                        tier,
                        bonus,
                        record),
                reward,
                true,
                newBest);

            StatusText =
                completedRookieRace
                    ? MotorCityLocalization.Text(
                        "onboarding.race_done")
                    : MotorCityLocalization.Format(
                        "activity.status_reward",
                        MotorCityLocalization.Text(
                            "hud.sprint"),
                        tier,
                        reward);

            rookieMode =
                false;
        }

        public void RestartFromResult()
        {
            if (activityManager == null ||
                !activityManager.HasResult ||
                activityManager.ResultActivityId !=
                    ActivityId ||
                activityManager.ResultIsRookieSprint ||
                route == null ||
                route.Length < 2 ||
                car == null)
                return;

            activityManager.DismissResult(false);

            car.SetDrivingBlocked(
                "ActivityResult",
                false);

            Vector3 direction =
                Flat(
                    route[1] -
                    route[0]);

            Quaternion rotation =
                direction.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up)
                    : Quaternion.Euler(
                        0f,
                        car.transform.eulerAngles.y,
                        0f);

            car.TeleportTo(
                route[0] +
                Vector3.up * 1.1f,
                rotation);

            armed = true;

            BeginCountdown();
        }

        public void CancelActivity()
        {
            if (!IsActive &&
                !isCountingDown)
                return;

            if (isCountingDown)
            {
                activityManager?.CancelPendingStart(
                    ActivityId);
            }

            IsActive = false;
            isCountingDown = false;
            armed = false;
            rookieMode = false;
            eliteMode = false;
            checkpointIndex = 0;
            ElapsedSeconds = 0f;

            car?.SetDrivingBlocked(
                "ActivityCountdown",
                false);
            car?.SetDrivingBlocked(
                "ActivityResult",
                false);
            activityManager?.End(ActivityId);

            StatusText =
                MotorCityLocalization.Text("activity.sprint_cancelled");
        }

        private void OnDisable()
        {
            if (IsActive ||
                isCountingDown)
            {
                CancelActivity();
                return;
            }

            car?.SetDrivingBlocked(
                "ActivityCountdown",
                false);
            car?.SetDrivingBlocked(
                "ActivityResult",
                false);
        }

        private static Vector3[] BuildRookieRoute(
            Vector3[] source)
        {
            if (source == null ||
                source.Length < 2)
            {
                return source;
            }

            // The regular sprint loops around most of the district. The
            // tutorial uses the already validated west/north section from
            // indices 5..10, which begins reasonably close to the garage and
            // remains long enough to teach checkpoint following.
            int start =
                source.Length >= 11
                    ? 5
                    : 0;

            int count =
                Mathf.Min(
                    6,
                    source.Length - start);

            if (count < 2)
            {
                return source;
            }

            Vector3[] result =
                new Vector3[count];

            for (int i = 0;
                 i < count;
                 i++)
            {
                result[i] =
                    source[start + i];
            }

            return result;
        }

        private static Vector3 Flat(
            Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
