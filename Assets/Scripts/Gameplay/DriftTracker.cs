using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DriftTracker : MonoBehaviour
    {
        [SerializeField] private float minimumSpeedKph = 32f;
        [SerializeField] private float minimumSlipAngle = 9f;
        [SerializeField] private float maximumControlledSlipAngle = 58f;
        [SerializeField] private float minimumRearSidewaysSlip = 0.16f;
        [SerializeField] private float comboGraceSeconds = 0.85f;

        private ArcadeCarController car;
        private float graceTimer;
        private float currentScore;
        private float combo = 1f;

        public int CurrentScore => Mathf.RoundToInt(currentScore);
        public float Combo => combo;
        public bool IsDrifting { get; private set; }
        public int BestDrift { get; private set; }

        private void Awake()
        {
            car = GetComponent<ArcadeCarController>();
        }

        private void Update()
        {
            if (car == null) return;

            float speed = Mathf.Abs(car.ForwardSpeedKph);
            float angle = Mathf.Abs(car.SlipAngleDegrees);
            float rearSlip = car.RearSidewaysSlip;

            bool controlledAngle = angle >= minimumSlipAngle && angle <= maximumControlledSlipAngle;
            bool rearIsActuallySliding = rearSlip >= minimumRearSidewaysSlip;
            bool validDrift = speed >= minimumSpeedKph &&
                              car.GroundedWheels >= 3 &&
                              controlledAngle &&
                              rearIsActuallySliding;

            if (validDrift)
            {
                IsDrifting = true;
                graceTimer = comboGraceSeconds;

                float angleQuality = Mathf.InverseLerp(minimumSlipAngle, 38f, angle);
                float slipQuality = Mathf.InverseLerp(minimumRearSidewaysSlip, 0.65f, rearSlip);
                float speedQuality = Mathf.InverseLerp(minimumSpeedKph, 110f, speed);
                float quality = Mathf.Clamp01(angleQuality * 0.48f + slipQuality * 0.32f + speedQuality * 0.20f);

                combo = Mathf.Min(3.5f, combo + Time.deltaTime * Mathf.Lerp(0.12f, 0.34f, quality));
                currentScore += Time.deltaTime * speed * Mathf.Lerp(0.42f, 1.18f, quality) * combo;
                return;
            }

            IsDrifting = false;
            if (currentScore <= 0f) return;

            graceTimer -= Time.deltaTime;
            if (graceTimer > 0f) return;

            BestDrift = Mathf.Max(BestDrift, Mathf.RoundToInt(currentScore));
            currentScore = 0f;
            combo = 1f;
        }
    }
}
