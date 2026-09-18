using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DriftChallenge : MonoBehaviour
    {
        private const string ActivityId = "drift";

        [SerializeField] private Vector3 zoneCenter = new(361f, 0f, -361f);
        [SerializeField] private float startRadius = 30.5f;
        [SerializeField] private float activityHalfExtent = 87.1f;
        [SerializeField] private float outsideGraceSeconds = 3.5f;
        [SerializeField] private float durationSeconds = 42f;
        [SerializeField] private int targetScore = 1800;
        [SerializeField] private int rewardCredits = 650;

        private ArcadeCarController car;
        private DriftTracker drift;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private int scoreAtStart;
        private bool armed = true;
        private float outsideTimer;

        public bool IsActive { get; private set; }
        public float TimeRemaining { get; private set; }
        public int CurrentScore => drift == null ? 0 : Mathf.Max(0, drift.TotalScore - scoreAtStart);
        public int TargetScore => targetScore;
        public int RewardCredits => rewardCredits;
        public Vector3 ZoneCenter => zoneCenter;
        public string StatusText { get; private set; } = "Оранжевая зона: дрифт-заезд";

        public void Initialize(ArcadeCarController targetCar, DriftTracker driftTracker, PlayerWallet targetWallet, ActivityManager manager)
        {
            car = targetCar;
            drift = driftTracker;
            wallet = targetWallet;
            activityManager = manager;
            TimeRemaining = durationSeconds;
        }

        private void Update()
        {
            if (car == null || drift == null || wallet == null || activityManager == null) return;

            Vector3 flatPosition = Flat(car.transform.position);
            float distance = Vector3.Distance(flatPosition, Flat(zoneCenter));

            if (!IsActive)
            {
                if (!armed)
                {
                    if (distance > startRadius + 4f)
                    {
                        armed = true;
                        StatusText = "Оранжевая зона: дрифт-заезд";
                    }
                    return;
                }

                if (distance <= startRadius)
                {
                    if (activityManager.IsBusy && !activityManager.IsActive(ActivityId))
                    {
                        StatusText = $"Дрифт-заезд недоступен: активно «{activityManager.ActiveName}»";
                        return;
                    }

                    BeginChallenge();
                }
                else
                {
                    StatusText = "Оранжевая зона: дрифт-заезд";
                }

                return;
            }

            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);

            if (CurrentScore >= targetScore)
            {
                wallet.AddCredits(rewardCredits);
                EndChallenge($"Дрифт-заезд завершён  +{rewardCredits} КР");
                return;
            }

            Vector3 local = flatPosition - Flat(zoneCenter);
            bool insideChallengeArea =
                Mathf.Abs(local.x) <= activityHalfExtent &&
                Mathf.Abs(local.z) <= activityHalfExtent;

            if (!insideChallengeArea)
            {
                outsideTimer += Time.deltaTime;
                float remainingGrace = Mathf.Max(0f, outsideGraceSeconds - outsideTimer);

                if (outsideTimer >= outsideGraceSeconds)
                {
                    EndChallenge("Провал: слишком далеко от площадки");
                    return;
                }

                StatusText =
                    $"ДРИФТ  {CurrentScore:N0}/{targetScore:N0}   {TimeRemaining:0.0}с   ВЕРНИСЬ {remainingGrace:0.0}с";
                return;
            }

            outsideTimer = 0f;

            if (TimeRemaining <= 0f)
            {
                EndChallenge($"Провал: {CurrentScore:N0}/{targetScore:N0}");
                return;
            }

            StatusText = $"ДРИФТ  {CurrentScore:N0}/{targetScore:N0}   {TimeRemaining:0.0}с";
        }

        private void BeginChallenge()
        {
            if (!activityManager.TryBegin(ActivityId, "Дрифт-заезд")) return;

            IsActive = true;
            armed = false;
            TimeRemaining = durationSeconds;
            scoreAtStart = drift.TotalScore;
            outsideTimer = 0f;
            StatusText = $"ДРИФТ  0/{targetScore:N0}   {TimeRemaining:0.0}с";
        }

        private void EndChallenge(string message)
        {
            IsActive = false;
            activityManager.End(ActivityId);
            TimeRemaining = 0f;
            outsideTimer = 0f;
            StatusText = message + ". Покинь зону, чтобы повторить.";
        }

        public void CancelActivity()
        {
            if (!IsActive) return;
            IsActive = false;
            armed = false;
            TimeRemaining = 0f;
            outsideTimer = 0f;
            activityManager?.End(ActivityId);
            StatusText = "Дрифт-заезд отменён. Покинь зону, чтобы повторить.";
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
