using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DriftTracker : MonoBehaviour
    {
        [SerializeField] private float minimumSpeedKph = 32f;
        [SerializeField] private float minimumSlipAngle = 12f;
        [SerializeField] private float maximumControlledSlipAngle = 65f;
        [SerializeField] private float minimumRearSidewaysSlip = 0.18f;
        [SerializeField] private float comboGraceSeconds = 1.0f;

        [Header("Free Drift Rewards")]
        [SerializeField] private int minimumBankScore = 80;
        [SerializeField] private float scorePerCredit = 8f;
        [SerializeField] private float rewardMessageSeconds = 2.5f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private float graceTimer;
        private float currentScore;
        private float totalScore;
        private float combo = 1f;
        private float rewardMessageTimer;

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

        public void Initialize(PlayerWallet targetWallet, ActivityManager manager)
        {
            wallet = targetWallet;
            activityManager = manager;
        }

        private void Update()
        {
            if (rewardMessageTimer > 0f)
                rewardMessageTimer = Mathf.Max(0f, rewardMessageTimer - Time.deltaTime);

            if (car == null) return;

            float speed = car.SpeedKph;
            float angle = Mathf.Abs(car.SlipAngleDegrees);
            float rearSlip = car.RearSidewaysSlip;

            bool controlledAngle =
                angle >= minimumSlipAngle &&
                angle <= maximumControlledSlipAngle;
            bool rearIsActuallySliding =
                rearSlip >= minimumRearSidewaysSlip;

            bool rearIsBrokenLoose =
                car.IsHandbrake ||
                car.RearForwardSlip >= 0.12f ||
                angle >= 18f;

            bool validDrift =
                speed >= minimumSpeedKph &&
                car.GroundedWheels >= 3 &&
                controlledAngle &&
                rearIsActuallySliding &&
                rearIsBrokenLoose;

            if (validDrift)
            {
                IsDrifting = true;
                graceTimer = comboGraceSeconds;

                float angleQuality =
                    Mathf.InverseLerp(minimumSlipAngle, 38f, angle);
                float slipQuality =
                    Mathf.InverseLerp(minimumRearSidewaysSlip, 0.65f, rearSlip);
                float speedQuality =
                    Mathf.InverseLerp(minimumSpeedKph, 110f, speed);

                float quality = Mathf.Clamp01(
                    angleQuality * 0.48f +
                    slipQuality * 0.32f +
                    speedQuality * 0.20f);

                combo = Mathf.Min(
                    3.5f,
                    combo +
                    Time.deltaTime *
                    Mathf.Lerp(0.12f, 0.34f, quality));

                float earned =
                    Time.deltaTime *
                    speed *
                    Mathf.Lerp(0.42f, 1.18f, quality) *
                    combo;

                currentScore += earned;
                totalScore += earned;
                return;
            }

            IsDrifting = false;
            if (currentScore <= 0f) return;

            graceTimer -= Time.deltaTime;
            if (graceTimer > 0f) return;

            BankCurrentDrift();
        }

        private void BankCurrentDrift()
        {
            int finishedScore = Mathf.RoundToInt(currentScore);
            BestDrift = Mathf.Max(BestDrift, finishedScore);

            bool freeRoam =
                activityManager == null ||
                !activityManager.IsBusy;

            if (freeRoam &&
                wallet != null &&
                finishedScore >= minimumBankScore)
            {
                int credits = Mathf.Max(
                    1,
                    Mathf.RoundToInt(finishedScore / scorePerCredit));

                wallet.AddCredits(credits);
                LastBankedCredits = credits;
                rewardMessageTimer = rewardMessageSeconds;
            }

            currentScore = 0f;
            combo = 1f;
        }
    }
}
