using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class UndergroundSceneSystem : MonoBehaviour
    {
        private const string CredKey =
            "MotorCity.Underground.Cred";

        private const string EventIndexKey =
            "MotorCity.Underground.EventIndex";

        private const string RacingKey =
            "MotorCity.Underground.Racing";

        private const string DriftKey =
            "MotorCity.Underground.Drift";

        private const string DeliveryKey =
            "MotorCity.Underground.Delivery";

        private const string NightKey =
            "MotorCity.Underground.Night";

        private const int EventCount = 4;
        private const float MessageSeconds = 6f;

        private ActivityManager activityManager;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private DayNightCycleController dayNight;

        private int streetCred;
        private int eventIndex;
        private int racingProgress;
        private int driftProgress;
        private int deliveryProgress;
        private int nightProgress;
        private float messageTimer;
        private bool invitationAnnounced;

        public int StreetCred =>
            streetCred;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; } =
            string.Empty;

        public bool HasActiveInvitation
        {
            get
            {
                if (eventIndex >= EventCount)
                    return false;

                UndergroundEvent current =
                    CurrentEvent();

                return
                    streetCred >=
                    current.RequiredCred;
            }
        }

        public string HudLine
        {
            get
            {
                if (eventIndex >= EventCount)
                {
                    return
                        "UNDERGROUND • INNER CIRCLE";
                }

                UndergroundEvent current =
                    CurrentEvent();

                if (streetCred <
                    current.RequiredCred)
                {
                    return
                        $"UNDERGROUND • ДОВЕРИЕ {streetCred}/{current.RequiredCred}";
                }

                return
                    $"UNDERGROUND — {current.Name} • " +
                    ProgressText(
                        current);
            }
        }

        public string AdminLine
        {
            get
            {
                string rank =
                    RankName();

                if (eventIndex >= EventCount)
                {
                    return
                        $"STREET CRED {streetCred} • {rank} • СЕРИЯ ЗАВЕРШЕНА";
                }

                UndergroundEvent current =
                    CurrentEvent();

                return
                    $"STREET CRED {streetCred} • {rank} • " +
                    $"{current.Name} • " +
                    (streetCred >= current.RequiredCred
                        ? ProgressText(current)
                        : $"НУЖНО {current.RequiredCred}");
            }
        }

        public void Initialize(
            ActivityManager manager,
            PlayerWallet playerWallet,
            PlayerReputation playerReputation)
        {
            activityManager =
                manager;

            wallet =
                playerWallet;

            reputation =
                playerReputation;

            Load();

            if (activityManager != null)
            {
                activityManager.ActivityResultShown +=
                    HandleActivityResult;
            }
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.deltaTime);
            }

            if (eventIndex >= EventCount ||
                invitationAnnounced)
            {
                return;
            }

            UndergroundEvent current =
                CurrentEvent();

            if (streetCred <
                current.RequiredCred)
            {
                return;
            }

            invitationAnnounced = true;

            StatusText =
                current.Invitation;

            messageTimer =
                MessageSeconds;
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityResultShown -=
                    HandleActivityResult;
            }

            Save();
        }

        public void AddCredForTesting(
            int amount)
        {
            streetCred =
                Mathf.Clamp(
                    streetCred + amount,
                    0,
                    9999);

            invitationAnnounced = false;
            Save();
        }

        public void UnlockCurrentForTesting()
        {
            if (eventIndex >= EventCount)
                return;

            streetCred =
                Mathf.Max(
                    streetCred,
                    CurrentEvent().RequiredCred);

            invitationAnnounced = false;
            Save();
        }

        public void CompleteCurrentForTesting()
        {
            if (eventIndex >= EventCount)
                return;

            UndergroundEvent current =
                CurrentEvent();

            streetCred =
                Mathf.Max(
                    streetCred,
                    current.RequiredCred);

            racingProgress =
                current.RacingRequired;

            driftProgress =
                current.DriftRequired;

            deliveryProgress =
                current.DeliveryRequired;

            nightProgress =
                current.NightRequired;

            CompleteEvent(
                current);
        }

        public void ResetForTesting()
        {
            streetCred = 0;
            eventIndex = 0;
            invitationAnnounced = false;
            ClearProgress();
            Save();

            StatusText =
                "UNDERGROUND ПРОГРЕСС СБРОШЕН";

            messageTimer =
                MessageSeconds;
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success)
                return;

            bool night =
                IsNight();

            if (night)
            {
                int credGain =
                    activityId switch
                    {
                        "drift" => 12,
                        "sprint" => 10,
                        "circuit" => 10,
                        "delivery" => 4,
                        _ => 0
                    };

                if (credGain > 0)
                {
                    int oldCred =
                        streetCred;

                    streetCred =
                        Mathf.Clamp(
                            streetCred + credGain,
                            0,
                            9999);

                    if (RankForCred(oldCred) !=
                        RankForCred(streetCred))
                    {
                        StatusText =
                            $"UNDERGROUND — {RankName()}   STREET CRED {streetCred}";

                        messageTimer =
                            MessageSeconds;

                        invitationAnnounced = false;
                    }
                }
            }

            if (eventIndex >= EventCount)
            {
                Save();
                return;
            }

            UndergroundEvent current =
                CurrentEvent();

            if (streetCred <
                    current.RequiredCred ||
                !night)
            {
                Save();
                return;
            }

            bool counted = false;

            switch (activityId)
            {
                case "sprint":
                case "circuit":
                    if (current.RacingRequired > 0)
                    {
                        racingProgress++;
                        counted = true;
                    }
                    break;

                case "drift":
                    if (current.DriftRequired > 0)
                    {
                        driftProgress++;
                        counted = true;
                    }
                    break;

                case "delivery":
                    if (current.DeliveryRequired > 0)
                    {
                        deliveryProgress++;
                        counted = true;
                    }
                    break;
            }

            if (counted &&
                current.NightRequired > 0)
            {
                nightProgress++;
            }

            if (IsComplete(
                    current))
            {
                CompleteEvent(
                    current);
                return;
            }

            Save();

            if (counted)
            {
                StatusText =
                    $"UNDERGROUND — {current.Name}   " +
                    ProgressText(
                        current);

                messageTimer =
                    3.5f;
            }
        }

        private void CompleteEvent(
            UndergroundEvent current)
        {
            wallet?.AddCredits(
                current.Credits);

            reputation?.AddReputation(
                current.Reputation);

            streetCred =
                Mathf.Clamp(
                    streetCred +
                    current.CredReward,
                    0,
                    9999);

            string completed =
                current.Name;

            eventIndex =
                Mathf.Min(
                    EventCount,
                    eventIndex + 1);

            invitationAnnounced = false;
            ClearProgress();
            Save();

            StatusText =
                eventIndex >= EventCount
                    ? $"UNDERGROUND — INNER CIRCLE   {completed} ЗАВЕРШЁН   " +
                      $"+{current.Credits:N0} КР   +{current.CredReward} STREET CRED"
                    : $"{completed} ЗАВЕРШЁН   " +
                      $"+{current.Credits:N0} КР   +{current.CredReward} STREET CRED   " +
                      $"СЛЕДУЮЩЕЕ ДОВЕРИЕ: {CurrentEvent().RequiredCred}";

            messageTimer =
                MessageSeconds;
        }

        private bool IsComplete(
            UndergroundEvent current)
        {
            return
                racingProgress >=
                    current.RacingRequired &&
                driftProgress >=
                    current.DriftRequired &&
                deliveryProgress >=
                    current.DeliveryRequired &&
                nightProgress >=
                    current.NightRequired;
        }

        private bool IsNight()
        {
            if (dayNight == null)
            {
                dayNight =
                    Object.FindAnyObjectByType<DayNightCycleController>();
            }

            return
                dayNight != null &&
                dayNight.IsNight;
        }

        private UndergroundEvent CurrentEvent()
        {
            return eventIndex switch
            {
                0 => new UndergroundEvent(
                    "01:20 — СТАРЫЙ ПОРТ",
                    "01:20. Старый порт. Приезжай один.",
                    40,
                    2,
                    1,
                    0,
                    3,
                    2800,
                    180,
                    45),

                1 => new UndergroundEvent(
                    "NO NAME RUN",
                    "Имя здесь ничего не значит. Докажи темп ночью.",
                    110,
                    2,
                    2,
                    0,
                    4,
                    4500,
                    260,
                    60),

                2 => new UndergroundEvent(
                    "BLACKLIST TEST",
                    "Blacklist наблюдает. Ошибок не прощают.",
                    200,
                    3,
                    3,
                    1,
                    7,
                    7200,
                    420,
                    80),

                _ => new UndergroundEvent(
                    "INNER CIRCLE",
                    "Последнее приглашение. Только для своих.",
                    320,
                    4,
                    4,
                    2,
                    10,
                    12000,
                    700,
                    120)
            };
        }

        private string ProgressText(
            UndergroundEvent current)
        {
            string result =
                string.Empty;

            AppendProgress(
                ref result,
                "R",
                racingProgress,
                current.RacingRequired);

            AppendProgress(
                ref result,
                "D",
                driftProgress,
                current.DriftRequired);

            AppendProgress(
                ref result,
                "DEL",
                deliveryProgress,
                current.DeliveryRequired);

            AppendProgress(
                ref result,
                "NIGHT",
                nightProgress,
                current.NightRequired);

            return
                string.IsNullOrEmpty(
                    result)
                    ? "ГОТОВО"
                    : result;
        }

        private static void AppendProgress(
            ref string text,
            string label,
            int progress,
            int required)
        {
            if (required <= 0)
                return;

            if (!string.IsNullOrEmpty(
                    text))
            {
                text += "  ";
            }

            text +=
                $"{label} {Mathf.Min(progress, required)}/{required}";
        }

        private string RankName()
        {
            return RankForCred(
                streetCred) switch
                {
                    4 => "INNER CIRCLE",
                    3 => "BLACKLIST",
                    2 => "ДОВЕРЕННЫЙ",
                    1 => "ЗАМЕЧЕН",
                    _ => "НЕИЗВЕСТЕН"
                };
        }

        private static int RankForCred(
            int cred)
        {
            if (cred >= 320)
                return 4;

            if (cred >= 200)
                return 3;

            if (cred >= 110)
                return 2;

            if (cred >= 40)
                return 1;

            return 0;
        }

        private void ClearProgress()
        {
            racingProgress = 0;
            driftProgress = 0;
            deliveryProgress = 0;
            nightProgress = 0;
        }

        private void Load()
        {
            streetCred =
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        CredKey,
                        0));

            eventIndex =
                Mathf.Clamp(
                    PlayerPrefs.GetInt(
                        EventIndexKey,
                        0),
                    0,
                    EventCount);

            racingProgress =
                LoadValue(
                    RacingKey);

            driftProgress =
                LoadValue(
                    DriftKey);

            deliveryProgress =
                LoadValue(
                    DeliveryKey);

            nightProgress =
                LoadValue(
                    NightKey);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(
                CredKey,
                streetCred);

            PlayerPrefs.SetInt(
                EventIndexKey,
                eventIndex);

            PlayerPrefs.SetInt(
                RacingKey,
                racingProgress);

            PlayerPrefs.SetInt(
                DriftKey,
                driftProgress);

            PlayerPrefs.SetInt(
                DeliveryKey,
                deliveryProgress);

            PlayerPrefs.SetInt(
                NightKey,
                nightProgress);

            PlayerPrefs.Save();
        }

        private static int LoadValue(
            string key)
        {
            return
                Mathf.Max(
                    0,
                    PlayerPrefs.GetInt(
                        key,
                        0));
        }

        private readonly struct UndergroundEvent
        {
            public readonly string Name;
            public readonly string Invitation;
            public readonly int RequiredCred;
            public readonly int RacingRequired;
            public readonly int DriftRequired;
            public readonly int DeliveryRequired;
            public readonly int NightRequired;
            public readonly int Credits;
            public readonly int Reputation;
            public readonly int CredReward;

            public UndergroundEvent(
                string name,
                string invitation,
                int requiredCred,
                int racingRequired,
                int driftRequired,
                int deliveryRequired,
                int nightRequired,
                int credits,
                int reputation,
                int credReward)
            {
                Name = name;
                Invitation = invitation;
                RequiredCred = requiredCred;
                RacingRequired = racingRequired;
                DriftRequired = driftRequired;
                DeliveryRequired = deliveryRequired;
                NightRequired = nightRequired;
                Credits = credits;
                Reputation = reputation;
                CredReward = credReward;
            }
        }
    }
}
