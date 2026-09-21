using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CityLiveEventSystem : MonoBehaviour
    {
        private const string EventIndexKey =
            "MotorCity.LiveEvent.Index";

        private const string CompletedKey =
            "MotorCity.LiveEvent.Completed";

        private const float ActiveSeconds = 420f;
        private const float CooldownSeconds = 60f;
        private const float MessageSeconds = 6f;
        private const int EventCount = 5;

        private ActivityManager activityManager;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private DayNightCycleController dayNight;

        private int eventIndex;
        private int completedEvents;
        private int racingProgress;
        private int driftProgress;
        private int deliveryProgress;
        private int nightProgress;

        private float activeTimer;
        private float cooldownTimer;
        private float messageTimer;
        private bool coolingDown;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; } =
            string.Empty;

        public string HudLine
        {
            get
            {
                if (coolingDown)
                {
                    return
                        $"ГОРОДСКОЕ СОБЫТИЕ • СЛЕДУЮЩЕЕ ЧЕРЕЗ {FormatTime(cooldownTimer)}";
                }

                EventDefinition current =
                    CurrentDefinition();

                return
                    $"ГОРОДСКОЕ СОБЫТИЕ — {current.Name} • " +
                    $"{ProgressText(current)} • " +
                    $"{FormatTime(activeTimer)} • " +
                    $"+{current.Credits:N0} КР";
            }
        }

        public string AdminLine
        {
            get
            {
                if (coolingDown)
                {
                    return
                        $"ГОРОДСКОЕ СОБЫТИЕ • ПЕРЕРЫВ {FormatTime(cooldownTimer)} • " +
                        $"ВЫПОЛНЕНО {completedEvents}";
                }

                EventDefinition current =
                    CurrentDefinition();

                return
                    $"{current.Name} • {ProgressText(current)} • " +
                    $"{FormatTime(activeTimer)} • ВЫПОЛНЕНО {completedEvents}";
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

            eventIndex =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        EventIndexKey,
                        0),
                    0,
                    EventCount - 1);

            completedEvents =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        CompletedKey,
                        0));

            StartCurrentEvent(
                false);

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

            if (coolingDown)
            {
                cooldownTimer -=
                    Time.deltaTime;

                if (cooldownTimer <= 0f)
                {
                    AdvanceEvent();
                }

                return;
            }

            activeTimer -=
                Time.deltaTime;

            if (activeTimer > 0f)
                return;

            StatusText =
                $"ГОРОДСКОЕ СОБЫТИЕ ЗАВЕРШЕНО — {CurrentDefinition().Name}   " +
                "время вышло";

            messageTimer =
                MessageSeconds;

            BeginCooldown();
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityResultShown -=
                    HandleActivityResult;
            }
        }

        public void CompleteCurrentForTesting()
        {
            if (coolingDown)
                AdvanceEvent();

            EventDefinition current =
                CurrentDefinition();

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

        public void NextEventForTesting()
        {
            AdvanceEvent();
        }

        public void ResetForTesting()
        {
            eventIndex = 0;
            completedEvents = 0;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                EventIndexKey,
                eventIndex);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                CompletedKey,
                completedEvents);

            MotorCity.Persistence.MotorCitySaveService.Save();

            StartCurrentEvent(
                false);

            StatusText =
                "ГОРОДСКОЕ СОБЫТИЕS СБРОШЕНЫ";

            messageTimer =
                MessageSeconds;
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success ||
                coolingDown)
            {
                return;
            }

            switch (activityId)
            {
                case "sprint":
                case "circuit":
                    racingProgress++;
                    break;

                case "drift":
                    driftProgress++;
                    break;

                case "delivery":
                    deliveryProgress++;
                    break;

                default:
                    return;
            }

            if (IsNight())
                nightProgress++;

            EventDefinition current =
                CurrentDefinition();

            if (IsComplete(
                    current))
            {
                CompleteEvent(
                    current);
                return;
            }

            StatusText =
                $"ГОРОДСКОЕ СОБЫТИЕ — {current.Name}   " +
                ProgressText(
                    current);

            messageTimer =
                3f;
        }

        private void CompleteEvent(
            EventDefinition current)
        {
            wallet?.AddCredits(
                current.Credits);

            reputation?.AddReputation(
                current.Reputation);

            completedEvents++;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                CompletedKey,
                completedEvents);

            MotorCity.Persistence.MotorCitySaveService.Save();

            StatusText =
                $"ГОРОДСКОЕ СОБЫТИЕ ВЫПОЛНЕНО — {current.Name}   " +
                $"+{current.Credits:N0} КР   " +
                $"+{current.Reputation:N0} РЕП";

            messageTimer =
                MessageSeconds;

            BeginCooldown();
        }

        private void BeginCooldown()
        {
            coolingDown = true;
            cooldownTimer =
                CooldownSeconds;

            ClearProgress();
        }

        private void AdvanceEvent()
        {
            eventIndex =
                (eventIndex + 1) %
                EventCount;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                EventIndexKey,
                eventIndex);

            MotorCity.Persistence.MotorCitySaveService.Save();

            StartCurrentEvent(
                true);
        }

        private void StartCurrentEvent(
            bool announce)
        {
            coolingDown = false;
            cooldownTimer = 0f;
            activeTimer =
                ActiveSeconds;

            ClearProgress();

            if (!announce)
                return;

            StatusText =
                $"НОВОЕ ГОРОДСКОЕ СОБЫТИЕ — {CurrentDefinition().Name}   " +
                ProgressText(
                    CurrentDefinition());

            messageTimer =
                MessageSeconds;
        }

        private bool IsComplete(
            EventDefinition current)
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

        private EventDefinition CurrentDefinition()
        {
            return eventIndex switch
            {
                0 => new EventDefinition(
                    "УЛИЧНЫЙ ЖАР",
                    2,
                    0,
                    0,
                    0,
                    1500,
                    150),

                1 => new EventDefinition(
                    "ДРИФТ-СЕССИЯ",
                    0,
                    2,
                    0,
                    0,
                    1300,
                    140),

                2 => new EventDefinition(
                    "КУРЬЕРСКИЙ РЫВОК",
                    0,
                    0,
                    2,
                    0,
                    1200,
                    120),

                3 => new EventDefinition(
                    "ПОЛУНОЧНЫЙ ЗАЕЗД",
                    0,
                    0,
                    0,
                    2,
                    1800,
                    180),

                _ => new EventDefinition(
                    "ТРОЙНАЯ УГРОЗА",
                    1,
                    1,
                    1,
                    0,
                    2200,
                    220)
            };
        }

        private string ProgressText(
            EventDefinition current)
        {
            string text =
                string.Empty;

            AppendProgress(
                ref text,
                "R",
                racingProgress,
                current.RacingRequired);

            AppendProgress(
                ref text,
                "D",
                driftProgress,
                current.DriftRequired);

            AppendProgress(
                ref text,
                "ДОСТ",
                deliveryProgress,
                current.DeliveryRequired);

            AppendProgress(
                ref text,
                "НОЧЬ",
                nightProgress,
                current.NightRequired);

            return
                string.IsNullOrEmpty(
                    text)
                    ? "ГОТОВО"
                    : text;
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

        private void ClearProgress()
        {
            racingProgress = 0;
            driftProgress = 0;
            deliveryProgress = 0;
            nightProgress = 0;
        }

        private static string FormatTime(
            float seconds)
        {
            int safeSeconds =
                Mathf.Max(
                    0,
                    Mathf.CeilToInt(
                        seconds));

            int minutes =
                safeSeconds / 60;

            int remaining =
                safeSeconds % 60;

            return
                $"{minutes:00}:{remaining:00}";
        }

        private readonly struct EventDefinition
        {
            public readonly string Name;
            public readonly int RacingRequired;
            public readonly int DriftRequired;
            public readonly int DeliveryRequired;
            public readonly int NightRequired;
            public readonly int Credits;
            public readonly int Reputation;

            public EventDefinition(
                string name,
                int racingRequired,
                int driftRequired,
                int deliveryRequired,
                int nightRequired,
                int credits,
                int reputation)
            {
                Name = name;
                RacingRequired =
                    racingRequired;
                DriftRequired =
                    driftRequired;
                DeliveryRequired =
                    deliveryRequired;
                NightRequired =
                    nightRequired;
                Credits =
                    credits;
                Reputation =
                    reputation;
            }
        }
    }
}
