using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DriftChallenge : MonoBehaviour
    {
        private const string ActivityId = "drift";
        private const int EliteRequiredLevel = 3;

        private Vector3 zoneCenter;
        [SerializeField] private float startRadius = 15f;
        [SerializeField] private float activityHalfExtent = 52f;
        [SerializeField] private float outsideGraceSeconds = 3.5f;
        [SerializeField] private float durationSeconds = 42f;

        [Header("Пороги очков")]
        [SerializeField] private int bronzeScore = 1800;
        [SerializeField] private int silverScore = 3000;
        [SerializeField] private int goldScore = 4500;
        [SerializeField] private int legendaryScore = 6000;

        [Header("Награды по уровням")]
        [SerializeField] private int bronzeRewardCredits = 650;
        [SerializeField] private int silverRewardCredits = 800;
        [SerializeField] private int goldRewardCredits = 1050;
        [SerializeField] private int legendaryRewardCredits = 1350;

        [SerializeField] private float maxStartSpeedKph = 8f;
        [SerializeField] private float countdownSeconds = 3f;

        private ArcadeCarController car;
        private DriftTracker drift;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private int scoreAtStart;
        private bool armed = true;
        private bool isCountingDown;
        private float countdownRemaining;
        private float outsideTimer;
        private bool eliteMode;

        public bool IsActive { get; private set; }
        public bool IsCountingDown => isCountingDown;
        public bool IsNearStart { get; private set; }
        public float TimeRemaining { get; private set; }

        public int CurrentScore =>
            drift == null
                ? 0
                : Mathf.Max(
                    0,
                    drift.TotalScore - scoreAtStart);

        public int TargetScore => bronzeScore;
        public int RewardCredits => bronzeRewardCredits;
        public Vector3 ZoneCenter => zoneCenter;

        public string StatusText { get; private set; } =
            MotorCityLocalization.Text("activity.marker.drift");

        public void Initialize(
            ArcadeCarController targetCar,
            DriftTracker driftTracker,
            PlayerWallet targetWallet,
            ActivityManager manager)
        {
            car = targetCar;
            drift = driftTracker;
            wallet = targetWallet;
            activityManager = manager;

            zoneCenter =
                MotorCity.World.CityAssetRuntimeInstaller.DriftChallengePoint;

            TimeRemaining = durationSeconds;
        }

        private void Update()
        {
            if (car == null ||
                drift == null ||
                wallet == null ||
                activityManager == null)
                return;

            if (isCountingDown)
            {
                IsNearStart = false;

                if (MotorCityInput.CancelPressed)
                {
                    CancelActivity();
                    return;
                }

                UpdateCountdown();
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

                UpdateActiveChallenge();
                return;
            }

            Vector3 flatPosition =
                Flat(car.transform.position);

            float distance =
                Vector3.Distance(
                    flatPosition,
                    Flat(zoneCenter));

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
                        MotorCityLocalization.Text("activity.marker.drift");
                }

                return;
            }

            if (!IsNearStart)
            {
                StatusText =
                    MotorCityLocalization.Text("activity.marker.drift");
                return;
            }

            if (activityManager.IsBusy &&
                !activityManager.IsActive(ActivityId))
            {
                StatusText =
                    MotorCityLocalization.Format("activity.busy", MotorCityLocalization.Text("activity.drift_challenge"), activityManager.ActiveName);
                return;
            }

            if (car.SpeedKph >
                maxStartSpeedKph)
            {
                StatusText =
                    MotorCityLocalization.Format("activity.stop", MotorCityLocalization.Text("activity.drift"), maxStartSpeedKph);
                return;
            }

            bool eliteUnlocked =
                activityManager.HasDisciplineLevel(
                    DisciplineType.Drift,
                    EliteRequiredLevel);

            string eliteHint =
                eliteUnlocked
                    ? MotorCityLocalization.Text("activity.elite_hint")
                    : MotorCityLocalization.Format("activity.elite_locked", MotorCityLocalization.Text("discipline.drift"), EliteRequiredLevel);

            StatusText =
                MotorCityLocalization.Format(
                    "activity.drift_start",
                    bronzeScore,
                    legendaryScore,
                    eliteHint);

            if (MotorCityInput.InteractPressed)
            {
                bool wantsElite =
                    MotorCityInput.EliteModifierHeld;

                eliteMode =
                    wantsElite &&
                    eliteUnlocked;

                BeginCountdown();
            }
        }

        private void BeginCountdown()
        {
            if (!activityManager.TryBegin(
                    ActivityId,
                    MotorCityLocalization.Text("activity.drift_challenge")))
                return;

            isCountingDown = true;
            armed = false;

            countdownRemaining =
                Mathf.Max(
                    0.1f,
                    countdownSeconds);

            TimeRemaining =
                eliteMode
                    ? 48f
                    : durationSeconds;

            outsideTimer = 0f;

            car.SetDrivingEnabled(false);

            UpdateCountdownStatus();
        }

        private void UpdateCountdown()
        {
            countdownRemaining =
                Mathf.Max(
                    0f,
                    countdownRemaining -
                    Time.deltaTime);

            if (countdownRemaining > 0f)
            {
                UpdateCountdownStatus();
                return;
            }

            isCountingDown = false;
            IsActive = true;
            TimeRemaining =
                eliteMode
                    ? 48f
                    : durationSeconds;

            scoreAtStart = drift.TotalScore;
            outsideTimer = 0f;

            car.SetDrivingEnabled(true);

            UpdateActiveStatus();
        }

        private void UpdateCountdownStatus()
        {
            int shown =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        countdownRemaining));

            StatusText =
                MotorCityLocalization.Format(
                    "activity.countdown",
                    eliteMode
                        ? MotorCityLocalization.Text("activity.elite_drift_short")
                        : MotorCityLocalization.Text("activity.drift"),
                    shown);
        }

        private void UpdateActiveChallenge()
        {
            Vector3 flatPosition =
                Flat(car.transform.position);

            TimeRemaining =
                Mathf.Max(
                    0f,
                    TimeRemaining -
                    Time.deltaTime);

            Vector3 local =
                flatPosition -
                Flat(zoneCenter);

            bool insideChallengeArea =
                Mathf.Abs(local.x) <=
                    activityHalfExtent &&
                Mathf.Abs(local.z) <=
                    activityHalfExtent;

            if (!insideChallengeArea)
            {
                outsideTimer +=
                    Time.deltaTime;

                float remainingGrace =
                    Mathf.Max(
                        0f,
                        outsideGraceSeconds -
                        outsideTimer);

                if (outsideTimer >=
                    outsideGraceSeconds)
                {
                    FailChallenge(
                        MotorCityLocalization.Text("activity.drift_too_far"));
                    return;
                }

                StatusText =
                    MotorCityLocalization.Format(
                        "activity.drift_return",
                        CurrentScore,
                        TimeRemaining,
                        remainingGrace);
                return;
            }

            outsideTimer = 0f;

            if (TimeRemaining <= 0f)
            {
                FinishByScore();
                return;
            }

            UpdateActiveStatus();
        }

        private void UpdateActiveStatus()
        {
            StatusText =
                MotorCityLocalization.Format(
                    "activity.drift_status",
                    CurrentScore,
                    CurrentTierProgress(),
                    TimeRemaining);
        }

        private string CurrentTierProgress()
        {
            int score =
                CurrentScore;

            int bronze =
                eliteMode ? 3000 : bronzeScore;
            int silver =
                eliteMode ? 4800 : silverScore;
            int gold =
                eliteMode ? 6800 : goldScore;
            int legendary =
                eliteMode ? 9000 : legendaryScore;

            if (score < bronze)
                return MotorCityLocalization.Format("activity.score_target", MotorCityLocalization.Text("medal.bronze"), bronze);

            if (score < silver)
                return MotorCityLocalization.Format("activity.score_target", MotorCityLocalization.Text("medal.silver"), silver);

            if (score < gold)
                return MotorCityLocalization.Format("activity.score_target", MotorCityLocalization.Text("medal.gold"), gold);

            if (score < legendary)
                return MotorCityLocalization.Format("activity.score_target", MotorCityLocalization.Text("activity.legend"), legendary);

            return MotorCityLocalization.Text("activity.legend_reached");
        }

        private void FinishByScore()
        {
            int finalScore =
                CurrentScore;

            int bronze =
                eliteMode ? 3000 : bronzeScore;
            int silver =
                eliteMode ? 4800 : silverScore;
            int gold =
                eliteMode ? 6800 : goldScore;
            int legendary =
                eliteMode ? 9000 : legendaryScore;

            if (finalScore <
                bronze)
            {
                FailChallenge(
                    MotorCityLocalization.Format("activity.not_enough_score", finalScore, bronze));
                return;
            }

            string tier;
            int reward;

            if (finalScore >=
                legendary)
            {
                tier = MotorCityLocalization.Text("activity.legend");
                reward =
                    legendaryRewardCredits;
            }
            else if (finalScore >=
                     gold)
            {
                tier = MotorCityLocalization.Text("medal.gold");
                reward =
                    goldRewardCredits;
            }
            else if (finalScore >=
                     silver)
            {
                tier = MotorCityLocalization.Text("medal.silver");
                reward =
                    silverRewardCredits;
            }
            else
            {
                tier = MotorCityLocalization.Text("medal.bronze");
                reward =
                    bronzeRewardCredits;
            }

            if (eliteMode)
            {
                reward =
                    Mathf.RoundToInt(
                        reward * 1.6f);
            }

            wallet.AddCredits(
                reward);

            IsActive = false;
            isCountingDown = false;
            TimeRemaining = 0f;
            outsideTimer = 0f;

            car.SetDrivingEnabled(false);

            activityManager.ShowResult(
                ActivityId,
                eliteMode
                    ? MotorCityLocalization.Text("activity.elite_drift_short")
                    : MotorCityLocalization.Text("activity.drift_challenge"),
                tier,
                MotorCityLocalization.Format("activity.drift_result", finalScore, eliteMode ? 48f : durationSeconds),
                reward,
                true);

            StatusText =
                MotorCityLocalization.Format("activity.status_reward", MotorCityLocalization.Text("activity.drift_challenge"), tier, reward);
        }

        private void FailChallenge(
            string reason)
        {
            int finalScore =
                CurrentScore;

            IsActive = false;
            isCountingDown = false;
            TimeRemaining = 0f;
            outsideTimer = 0f;

            car.SetDrivingEnabled(false);

            activityManager.ShowResult(
                ActivityId,
                MotorCityLocalization.Text("activity.drift_challenge"),
                MotorCityLocalization.Text("activity.failed"),
                MotorCityLocalization.Format("activity.drift_fail_details", reason, finalScore),
                0,
                false);

            StatusText =
                MotorCityLocalization.Text("activity.drift_failed");
        }

        public void RestartFromResult()
        {
            if (activityManager == null ||
                !activityManager.HasResult ||
                activityManager.ResultActivityId !=
                    ActivityId ||
                car == null)
                return;

            activityManager.DismissResult();

            Quaternion rotation =
                Quaternion.Euler(
                    0f,
                    car.transform.eulerAngles.y,
                    0f);

            car.TeleportTo(
                zoneCenter +
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

            IsActive = false;
            isCountingDown = false;
            armed = false;
            countdownRemaining = 0f;
            TimeRemaining = 0f;
            outsideTimer = 0f;

            car?.SetDrivingEnabled(true);
            activityManager?.End(ActivityId);

            StatusText =
                MotorCityLocalization.Text("activity.drift_cancelled");
        }

        private void OnDisable()
        {
            car?.SetDrivingEnabled(true);
        }

        private static Vector3 Flat(
            Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
