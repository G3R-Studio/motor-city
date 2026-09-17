using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DriftChallenge : MonoBehaviour
    {
        [SerializeField] private Vector3 zoneCenter = new(83f, 0f, -83f);
        [SerializeField] private float startRadius = 7f;
        [SerializeField] private float activityRadius = 19f;
        [SerializeField] private float durationSeconds = 42f;
        [SerializeField] private int targetScore = 1800;
        [SerializeField] private int rewardCredits = 650;

        private ArcadeCarController car;
        private DriftTracker drift;
        private PlayerWallet wallet;
        private int scoreAtStart;
        private bool armed = true;

        public bool IsActive { get; private set; }
        public float TimeRemaining { get; private set; }
        public int CurrentScore => drift == null ? 0 : Mathf.Max(0, drift.TotalScore - scoreAtStart);
        public int TargetScore => targetScore;
        public int RewardCredits => rewardCredits;
        public Vector3 ZoneCenter => zoneCenter;
        public string StatusText { get; private set; } = "Orange zone: drift challenge";

        public void Initialize(ArcadeCarController targetCar, DriftTracker driftTracker, PlayerWallet targetWallet)
        {
            car = targetCar;
            drift = driftTracker;
            wallet = targetWallet;
            TimeRemaining = durationSeconds;
        }

        private void Update()
        {
            if (car == null || drift == null || wallet == null) return;

            float distance = Vector3.Distance(Flat(car.transform.position), Flat(zoneCenter));

            if (!IsActive)
            {
                if (!armed)
                {
                    if (distance > startRadius + 4f)
                    {
                        armed = true;
                        StatusText = "Orange zone: drift challenge";
                    }
                    return;
                }

                if (distance <= startRadius)
                    BeginChallenge();

                return;
            }

            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);

            if (CurrentScore >= targetScore)
            {
                wallet.AddCredits(rewardCredits);
                EndChallenge($"Drift challenge complete +{rewardCredits} CR");
                return;
            }

            if (distance > activityRadius + 3f)
            {
                EndChallenge("Drift challenge failed: stay inside the parking lot");
                return;
            }

            if (TimeRemaining <= 0f)
            {
                EndChallenge($"Drift challenge failed: {CurrentScore:N0}/{targetScore:N0}");
                return;
            }

            StatusText = $"DRIFT CHALLENGE  {CurrentScore:N0}/{targetScore:N0}   {TimeRemaining:0.0}s";
        }

        private void BeginChallenge()
        {
            IsActive = true;
            armed = false;
            TimeRemaining = durationSeconds;
            scoreAtStart = drift.TotalScore;
            StatusText = $"DRIFT CHALLENGE  0/{targetScore:N0}   {TimeRemaining:0.0}s";
        }

        private void EndChallenge(string message)
        {
            IsActive = false;
            TimeRemaining = 0f;
            StatusText = message + ". Leave the zone to retry.";
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
