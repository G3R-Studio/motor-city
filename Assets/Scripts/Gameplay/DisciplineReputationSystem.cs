using UnityEngine;

namespace MotorCity.Gameplay
{
    public enum DisciplineType
    {
        Racing = 0,
        Drift = 1,
        Delivery = 2
    }

    public sealed class DisciplineReputationSystem : MonoBehaviour
    {
        private const string RacingKey =
            "MotorCity.Discipline.Racing.Reputation";

        private const string DriftKey =
            "MotorCity.Discipline.Drift.Reputation";

        private const string DeliveryKey =
            "MotorCity.Discipline.Delivery.Reputation";

        private const float MessageSeconds = 5f;
        private const int MaxLevel = 10;

        private static readonly int[] LevelThresholds =
        {
            0,
            120,
            280,
            500,
            780,
            1120,
            1520,
            1980,
            2500,
            3100
        };

        private ActivityManager activityManager;
        private float messageTimer;

        public int RacingReputation { get; private set; }
        public int DriftReputation { get; private set; }
        public int DeliveryReputation { get; private set; }

        public int RacingLevel =>
            ResolveLevel(
                RacingReputation);

        public int DriftLevel =>
            ResolveLevel(
                DriftReputation);

        public int DeliveryLevel =>
            ResolveLevel(
                DeliveryReputation);

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; } =
            string.Empty;

        public string HudLine =>
            $"RACING {RacingLevel}/10   •   " +
            $"DRIFT {DriftLevel}/10   •   " +
            $"DELIVERY {DeliveryLevel}/10";

        public void Initialize(
            ActivityManager manager)
        {
            activityManager =
                manager;

            RacingReputation =
                Load(
                    RacingKey);

            DriftReputation =
                Load(
                    DriftKey);

            DeliveryReputation =
                Load(
                    DeliveryKey);

            if (activityManager != null)
            {
                activityManager.ActivityResultShown +=
                    HandleActivityResult;
            }
        }

        private void Update()
        {
            if (messageTimer <= 0f)
                return;

            messageTimer =
                Mathf.Max(
                    0f,
                    messageTimer -
                    Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityResultShown -=
                    HandleActivityResult;
            }
        }

        public int GetReputation(
            DisciplineType type)
        {
            return type switch
            {
                DisciplineType.Racing =>
                    RacingReputation,
                DisciplineType.Drift =>
                    DriftReputation,
                _ =>
                    DeliveryReputation
            };
        }

        public int GetLevel(
            DisciplineType type)
        {
            return
                ResolveLevel(
                    GetReputation(
                        type));
        }

        public bool HasLevel(
            DisciplineType type,
            int requiredLevel)
        {
            return
                GetLevel(
                    type) >=
                Mathf.Clamp(
                    requiredLevel,
                    1,
                    MaxLevel);
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success)
                return;

            DisciplineType type;
            int reward;

            switch (activityId)
            {
                case "sprint":
                    type =
                        DisciplineType.Racing;
                    reward = 90;
                    break;

                case "circuit":
                    type =
                        DisciplineType.Racing;
                    reward = 110;
                    break;

                case "drift":
                    type =
                        DisciplineType.Drift;
                    reward = 85;
                    break;

                case "delivery":
                    type =
                        DisciplineType.Delivery;
                    reward = 70;
                    break;

                default:
                    return;
            }

            int previousLevel =
                GetLevel(
                    type);

            AddReputation(
                type,
                reward);

            int newLevel =
                GetLevel(
                    type);

            string name =
                DisciplineName(
                    type);

            StatusText =
                newLevel > previousLevel
                    ? $"{name}: УРОВЕНЬ {newLevel}/10   +{reward} REP"
                    : $"{name}: +{reward} REP";

            messageTimer =
                MessageSeconds;
        }

        private void AddReputation(
            DisciplineType type,
            int amount)
        {
            if (amount <= 0)
                return;

            switch (type)
            {
                case DisciplineType.Racing:
                    RacingReputation +=
                        amount;

                    Save(
                        RacingKey,
                        RacingReputation);
                    break;

                case DisciplineType.Drift:
                    DriftReputation +=
                        amount;

                    Save(
                        DriftKey,
                        DriftReputation);
                    break;

                case DisciplineType.Delivery:
                    DeliveryReputation +=
                        amount;

                    Save(
                        DeliveryKey,
                        DeliveryReputation);
                    break;
            }
        }

        private static int ResolveLevel(
            int reputation)
        {
            int level =
                1;

            for (int i = 1;
                 i < LevelThresholds.Length;
                 i++)
            {
                if (reputation <
                    LevelThresholds[i])
                {
                    break;
                }

                level =
                    i + 1;
            }

            return
                Mathf.Clamp(
                    level,
                    1,
                    MaxLevel);
        }

        private static int Load(
            string key)
        {
            return
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        key,
                        0));
        }

        private static void Save(
            string key,
            int value)
        {
            PlayerPrefs.SetInt(
                key,
                Mathf.Max(
                    0,
                    value));

            PlayerPrefs.Save();
        }

        private static string DisciplineName(
            DisciplineType type)
        {
            return type switch
            {
                DisciplineType.Racing =>
                    "RACING",
                DisciplineType.Drift =>
                    "DRIFT",
                _ =>
                    "DELIVERY"
            };
        }
    }
}
