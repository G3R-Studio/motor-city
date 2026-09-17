using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class StreetSprintActivity : MonoBehaviour
    {
        private const string ActivityId = "sprint";

        [SerializeField] private int baseRewardCredits = 550;
        [SerializeField] private int maximumTimeBonusCredits = 450;
        [SerializeField] private float startRadius = 7f;
        [SerializeField] private float checkpointRadius = 6f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private Vector3[] route;
        private int checkpointIndex;
        private bool armed = true;

        public bool IsActive { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public int CheckpointIndex => checkpointIndex;
        public int CheckpointCount => route?.Length ?? 0;
        public Vector3 CurrentTarget =>
            route == null || route.Length == 0
                ? Vector3.zero
                : route[Mathf.Clamp(checkpointIndex, 0, route.Length - 1)];
        public string StatusText { get; private set; } = "Зелёный маркер: уличный спринт";

        public void Initialize(ArcadeCarController targetCar, PlayerWallet targetWallet, ActivityManager manager)
        {
            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            route = new[]
            {
                new Vector3(-84f, 0f, -84f),
                new Vector3(-84f, 0f, 42f),
                new Vector3(0f, 0f, 84f),
                new Vector3(84f, 0f, 42f),
                new Vector3(84f, 0f, -42f),
                new Vector3(0f, 0f, -84f)
            };
        }

        private void Update()
        {
            if (car == null || wallet == null || activityManager == null || route == null || route.Length < 2) return;

            float distance = Vector3.Distance(Flat(car.transform.position), Flat(CurrentTarget));

            if (!IsActive)
            {
                checkpointIndex = 0;

                if (!armed)
                {
                    if (distance > startRadius + 4f)
                    {
                        armed = true;
                        StatusText = "Зелёный маркер: уличный спринт";
                    }
                    return;
                }

                if (distance <= startRadius)
                {
                    if (activityManager.IsBusy && !activityManager.IsActive(ActivityId))
                    {
                        StatusText = $"Спринт недоступен: активно «{activityManager.ActiveName}»";
                        return;
                    }

                    BeginSprint();
                }
                else
                {
                    StatusText = "Зелёный маркер: уличный спринт";
                }

                return;
            }

            ElapsedSeconds += Time.deltaTime;
            if (distance > checkpointRadius) return;

            checkpointIndex++;
            if (checkpointIndex >= route.Length)
            {
                CompleteSprint();
                return;
            }

            StatusText = $"СПРИНТ  ТОЧКА {checkpointIndex + 1}/{route.Length}   {ElapsedSeconds:0.0}с";
        }

        private void BeginSprint()
        {
            if (!activityManager.TryBegin(ActivityId, "Уличный спринт")) return;

            IsActive = true;
            armed = false;
            ElapsedSeconds = 0f;
            checkpointIndex = 1;
            StatusText = $"СПРИНТ  ТОЧКА {checkpointIndex + 1}/{route.Length}   0.0с";
        }

        private void CompleteSprint()
        {
            int bonus = Mathf.RoundToInt(Mathf.Lerp(
                maximumTimeBonusCredits,
                0f,
                Mathf.InverseLerp(38f, 80f, ElapsedSeconds)));
            int reward = baseRewardCredits + bonus;
            wallet.AddCredits(reward);

            IsActive = false;
            activityManager.End(ActivityId);
            checkpointIndex = 0;
            StatusText = $"Спринт завершён за {ElapsedSeconds:0.0}с  +{reward} CR";
        }

        public void CancelActivity()
        {
            if (!IsActive) return;
            IsActive = false;
            armed = false;
            checkpointIndex = 0;
            ElapsedSeconds = 0f;
            activityManager?.End(ActivityId);
            StatusText = "Спринт отменён. Отъедь от старта, чтобы повторить.";
        }

        private static Vector3 Flat(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
