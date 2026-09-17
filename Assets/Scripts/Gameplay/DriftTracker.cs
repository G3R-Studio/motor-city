using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DriftTracker : MonoBehaviour
    {
        [SerializeField] private float minimumSpeedKph = 28f;
        [SerializeField] private float minimumSlipAngle = 12f;
        [SerializeField] private float comboGraceSeconds = 1.15f;

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

            float speed = car.SpeedKph;
            float slip = Mathf.Abs(car.SlipAngleDegrees);
            bool validDrift = speed >= minimumSpeedKph && slip >= minimumSlipAngle;

            if (validDrift)
            {
                IsDrifting = true;
                graceTimer = comboGraceSeconds;

                float intensity = Mathf.InverseLerp(minimumSlipAngle, 48f, slip);
                combo = Mathf.Min(5f, combo + Time.deltaTime * (0.32f + intensity * 0.55f));
                currentScore += Time.deltaTime * speed * (0.65f + intensity) * combo;
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
