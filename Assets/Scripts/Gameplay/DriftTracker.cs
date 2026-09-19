using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DriftTracker : MonoBehaviour
    {
        [Header("Drift detection")]
        [SerializeField] private float minimumSpeedKph = 26f;
        [SerializeField] private float minimumSlipAngle = 8f;
        [SerializeField] private float maximumControlledSlipAngle = 78f;
        [SerializeField] private float minimumRearSidewaysSlip = 0.12f;

        // A long drift regularly has short moments where wheel contact, slip
        // or angle telemetry dips while the car is still visibly drifting.
        // Do not destroy the combo because of those transient samples.
        [SerializeField] private float comboGraceSeconds = 2.0f;

        [Header("Drift continuation")]
        [SerializeField] private float continuationSpeedFactor = 0.72f;
        [SerializeField] private float continuationAngleFactor = 0.55f;
        [SerializeField] private float continuationSlipFactor = 0.55f;
        [SerializeField] private float maximumContinuationSlipAngle = 88f;
        [SerializeField] private int minimumContinuationGroundedWheels = 2;

        [Header("Free Drift Rewards")]
        [SerializeField] private int minimumBankScore = 150;
        [SerializeField] private float minimumBankTravelDistance = 18f;
        [SerializeField] private int fullRateScore = 2500;
        [SerializeField] private float scorePerCredit = 18f;
        [SerializeField] private float highScorePerCredit = 36f;
        [SerializeField] private int maximumCreditsPerDrift = 350;
        [SerializeField] private float rewardMessageSeconds = 2.5f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private float graceTimer;
        private float currentScore;
        private float totalScore;
        private float combo = 1f;
        private float rewardMessageTimer;
        private bool comboInProgress;
        private Vector3 comboStartPosition;

        public int CurrentScore => Mathf.RoundToInt(currentScore);
        public int TotalScore => Mathf.RoundToInt(totalScore);
        public float Combo => combo;
        public bool IsDrifting { get; private set; }
        public int BestDrift { get; private set; }
        public int LastBankedCredits { get; private set; }
        public bool ShowRewardMessage => rewardMessageTimer > 0f;

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
        }

        public void Initialize(
            PlayerWallet targetWallet,
            ActivityManager manager)
        {
            wallet = targetWallet;
            activityManager = manager;
        }

        private void Update()
        {
            if (rewardMessageTimer > 0f)
            {
                rewardMessageTimer =
                    Mathf.Max(
                        0f,
                        rewardMessageTimer - Time.deltaTime);
            }

            if (car == null)
                return;

            float speed =
                car.SpeedKph;

            float angle =
                Mathf.Abs(
                    car.SlipAngleDegrees);

            float rearSlip =
                car.RearSidewaysSlip;

            bool scoringDrift =
                IsScoringDrift(
                    speed,
                    angle,
                    rearSlip);

            if (scoringDrift)
            {
                if (!comboInProgress)
                {
                    comboStartPosition =
                        transform.position;
                }

                comboInProgress =
                    true;

                IsDrifting =
                    true;

                graceTimer =
                    comboGraceSeconds;

                AddDriftScore(
                    speed,
                    angle,
                    rearSlip);

                return;
            }

            // Once a combo has started, use hysteresis. A sustained drift can
            // briefly lose one wheel on a curb, exceed the scoring angle,
            // or dip below the strict slip threshold during a transition.
            // Preserve the combo in those cases, but do not award score until
            // the strict scoring conditions become valid again.
            if (comboInProgress &&
                IsContinuationDrift(
                    speed,
                    angle,
                    rearSlip))
            {
                IsDrifting =
                    true;

                graceTimer =
                    comboGraceSeconds;

                return;
            }

            IsDrifting =
                false;

            if (!comboInProgress ||
                currentScore <= 0f)
                return;

            graceTimer -=
                Time.deltaTime;

            if (graceTimer > 0f)
                return;

            BankCurrentDrift();
        }

        private bool IsScoringDrift(
            float speed,
            float angle,
            float rearSlip)
        {
            bool controlledAngle =
                angle >= minimumSlipAngle &&
                angle <= maximumControlledSlipAngle;

            bool rearIsActuallySliding =
                rearSlip >= minimumRearSidewaysSlip;

            return
                car.IsSliding &&
                speed >= minimumSpeedKph &&
                car.GroundedWheels >= 3 &&
                controlledAngle &&
                rearIsActuallySliding;
        }

        private bool IsContinuationDrift(
            float speed,
            float angle,
            float rearSlip)
        {
            float continuationSpeed =
                minimumSpeedKph *
                Mathf.Clamp(
                    continuationSpeedFactor,
                    0.4f,
                    1f);

            float continuationAngle =
                minimumSlipAngle *
                Mathf.Clamp(
                    continuationAngleFactor,
                    0.25f,
                    1f);

            float continuationSlip =
                minimumRearSidewaysSlip *
                Mathf.Clamp(
                    continuationSlipFactor,
                    0.25f,
                    1f);

            bool enoughGroundContact =
                car.GroundedWheels >=
                Mathf.Clamp(
                    minimumContinuationGroundedWheels,
                    1,
                    4);

            bool angleStillLooksLikeDrift =
                angle >= continuationAngle &&
                angle <=
                    Mathf.Max(
                        maximumControlledSlipAngle,
                        maximumContinuationSlipAngle);

            bool rearStillMovingSideways =
                rearSlip >= continuationSlip;

            // IsSliding itself uses Prometeo + WheelCollider telemetry.
            // The relaxed physical checks cover one-frame / curb transitions
            // where that flag can briefly fall false.
            bool stillSliding =
                car.IsSliding ||
                rearStillMovingSideways ||
                (car.IsHandbrake &&
                 angle >= continuationAngle);

            return
                speed >= continuationSpeed &&
                enoughGroundContact &&
                angleStillLooksLikeDrift &&
                stillSliding;
        }

        private void AddDriftScore(
            float speed,
            float angle,
            float rearSlip)
        {
            float angleQuality =
                Mathf.InverseLerp(
                    minimumSlipAngle,
                    38f,
                    angle);

            float slipQuality =
                Mathf.Max(
                    car.DriftIntensity,
                    Mathf.InverseLerp(
                        minimumRearSidewaysSlip,
                        0.58f,
                        rearSlip));

            float speedQuality =
                Mathf.InverseLerp(
                    minimumSpeedKph,
                    110f,
                    speed);

            float quality =
                Mathf.Clamp01(
                    angleQuality * 0.48f +
                    slipQuality * 0.32f +
                    speedQuality * 0.20f);

            combo =
                Mathf.Min(
                    3.5f,
                    combo +
                    Time.deltaTime *
                    Mathf.Lerp(
                        0.12f,
                        0.34f,
                        quality));

            float earned =
                Time.deltaTime *
                speed *
                Mathf.Lerp(
                    0.42f,
                    1.18f,
                    quality) *
                combo;

            currentScore +=
                earned;

            totalScore +=
                earned;
        }

        private void BankCurrentDrift()
        {
            int finishedScore =
                Mathf.RoundToInt(
                    currentScore);

            BestDrift =
                Mathf.Max(
                    BestDrift,
                    finishedScore);

            bool freeRoam =
                activityManager == null ||
                !activityManager.IsBusy;

            float travelDistance =
                Vector3.Distance(
                    Flat(comboStartPosition),
                    Flat(transform.position));

            if (freeRoam &&
                wallet != null &&
                finishedScore >= minimumBankScore &&
                travelDistance >=
                    minimumBankTravelDistance)
            {
                int regularScore =
                    Mathf.Min(
                        finishedScore,
                        fullRateScore);

                int overflowScore =
                    Mathf.Max(
                        0,
                        finishedScore -
                        fullRateScore);

                float calculatedCredits =
                    regularScore /
                    Mathf.Max(
                        1f,
                        scorePerCredit);

                calculatedCredits +=
                    overflowScore /
                    Mathf.Max(
                        scorePerCredit,
                        highScorePerCredit);

                int credits =
                    Mathf.Clamp(
                        Mathf.RoundToInt(
                            calculatedCredits),
                        1,
                        maximumCreditsPerDrift);

                wallet.AddCredits(
                    credits);

                LastBankedCredits =
                    credits;

                rewardMessageTimer =
                    rewardMessageSeconds;
            }
            else
            {
                LastBankedCredits = 0;
            }

            currentScore =
                0f;

            combo =
                1f;

            comboInProgress =
                false;

            graceTimer =
                0f;

            comboStartPosition =
                transform.position;
        }

        private static Vector3 Flat(
            Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
