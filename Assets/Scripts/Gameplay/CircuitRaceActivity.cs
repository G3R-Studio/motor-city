using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CircuitRaceActivity : MonoBehaviour
    {
        private const string ActivityId = "circuit";
        private const string BestTimeKey = "MotorCity.Circuit.BestTime";

        [SerializeField] private int lapCount = 2;
        [SerializeField] private int baseRewardCredits = 800;
        [SerializeField] private int maximumTimeBonusCredits = 700;
        [SerializeField] private float startRadius = 14f;
        [SerializeField] private float checkpointRadius = 14f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activityManager;
        private Vector3[] route;
        private int checkpointIndex;
        private int currentLap = 1;
        private bool armed = true;

        public bool IsActive { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public int CurrentLap => currentLap;
        public int LapCount => lapCount;
        public int CheckpointIndex => checkpointIndex;
        public int CheckpointCount => route?.Length ?? 0;
        public float BestTimeSeconds { get; private set; }

        public Vector3 CurrentTarget =>
            route == null || route.Length == 0
                ? Vector3.zero
                : route[Mathf.Clamp(
                    checkpointIndex,
                    0,
                    route.Length - 1)];

        public string StatusText { get; private set; } =
            "Бирюзовый флаг: кольцевая гонка";

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager manager)
        {
            car = targetCar;
            wallet = targetWallet;
            activityManager = manager;
            route = CityAssetRuntimeInstaller.CircuitRoute;

            BestTimeSeconds =
                Mathf.Max(
                    0f,
                    PlayerPrefs.GetFloat(
                        BestTimeKey,
                        0f));
        }

        private void Update()
        {
            if (car == null ||
                wallet == null ||
                activityManager == null ||
                route == null ||
                route.Length < 4)
                return;

            float distance =
                Vector3.Distance(
                    Flat(car.transform.position),
                    Flat(CurrentTarget));

            if (!IsActive)
            {
                checkpointIndex = 0;
                currentLap = 1;

                if (!armed)
                {
                    if (distance > startRadius + 4f)
                    {
                        armed = true;
                        StatusText =
                            "Бирюзовый флаг: кольцевая гонка";
                    }

                    return;
                }

                if (distance <= startRadius)
                {
                    if (activityManager.IsBusy &&
                        !activityManager.IsActive(ActivityId))
                    {
                        StatusText =
                            $"Кольцо недоступно: активно «{activityManager.ActiveName}»";
                        return;
                    }

                    BeginRace();
                }
                else
                {
                    StatusText =
                        "Бирюзовый флаг: кольцевая гонка";
                }

                return;
            }

            ElapsedSeconds +=
                Time.deltaTime;

            if (distance >
                checkpointRadius)
                return;

            if (checkpointIndex == 0)
            {
                if (currentLap >=
                    lapCount)
                {
                    CompleteRace();
                    return;
                }

                currentLap++;
                checkpointIndex = 1;
                UpdateStatus();
                return;
            }

            checkpointIndex++;

            if (checkpointIndex >=
                route.Length)
            {
                // Finish the current lap by crossing the original start line.
                checkpointIndex = 0;
            }

            UpdateStatus();
        }

        private void BeginRace()
        {
            if (!activityManager.TryBegin(
                    ActivityId,
                    "Кольцевая гонка"))
                return;

            IsActive = true;
            armed = false;
            ElapsedSeconds = 0f;
            currentLap = 1;
            checkpointIndex = 1;
            UpdateStatus();
        }

        private void CompleteRace()
        {
            int bonus =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        maximumTimeBonusCredits,
                        0f,
                        Mathf.InverseLerp(
                            92f,
                            170f,
                            ElapsedSeconds)));

            int reward =
                baseRewardCredits +
                bonus;

            bool newBest =
                BestTimeSeconds <= 0f ||
                ElapsedSeconds <
                BestTimeSeconds;

            if (newBest)
            {
                BestTimeSeconds =
                    ElapsedSeconds;

                PlayerPrefs.SetFloat(
                    BestTimeKey,
                    BestTimeSeconds);

                PlayerPrefs.Save();
            }

            wallet.AddCredits(
                reward);

            IsActive = false;
            activityManager.End(
                ActivityId);

            checkpointIndex = 0;
            currentLap = 1;

            string record =
                newBest
                    ? "   НОВЫЙ РЕКОРД!"
                    : BestTimeSeconds > 0f
                        ? $"   РЕКОРД {BestTimeSeconds:0.0}с"
                        : string.Empty;

            StatusText =
                $"Кольцо завершено за {ElapsedSeconds:0.0}с  +{reward} КР{record}";
        }

        public void CancelActivity()
        {
            if (!IsActive)
                return;

            IsActive = false;
            armed = false;
            checkpointIndex = 0;
            currentLap = 1;
            ElapsedSeconds = 0f;

            activityManager?.End(
                ActivityId);

            StatusText =
                "Кольцевая гонка отменена. Отъедь от старта, чтобы повторить.";
        }

        private void UpdateStatus()
        {
            int shownCheckpoint =
                checkpointIndex == 0
                    ? route.Length
                    : checkpointIndex + 1;

            int total =
                route.Length;

            string best =
                BestTimeSeconds > 0f
                    ? $"   РЕК {BestTimeSeconds:0.0}с"
                    : string.Empty;

            StatusText =
                $"КОЛЬЦО  КРУГ {currentLap}/{lapCount}   " +
                $"ТОЧКА {shownCheckpoint}/{total}   " +
                $"{ElapsedSeconds:0.0}с{best}";
        }

        private static Vector3 Flat(
            Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
