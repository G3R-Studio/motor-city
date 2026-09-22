using System;
using MotorCity.Localization;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class DailyAdventureSystem :
        MonoBehaviour
    {
        private const string DayKey =
            "MotorCity.Daily.Day";
        private const string CompletedDaysKey =
            "MotorCity.Daily.CompletedDays";
        private const string DayCompletedKey =
            "MotorCity.Daily.DayCompleted";

        private const float MessageSeconds =
            4f;

        private ActivityManager activities;
        private PlayerWallet wallet;
        private TurboPetSystem turbo;

        private readonly DailyTask[] tasks =
            new DailyTask[3];

        private long currentDay;
        private int completedDays;
        private bool dayCompleted;
        private float messageTimer;
        private float dayCheckTimer;

        private const float DayCheckInterval =
            1f;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public int CompletedTasks
        {
            get
            {
                int count = 0;

                foreach (DailyTask task in
                         tasks)
                {
                    if (task != null &&
                        task.Completed)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int CompletedDays =>
            completedDays;

        public bool DayCompleted =>
            dayCompleted;

        public string ObjectiveLine
        {
            get
            {
                if (dayCompleted)
                {
                    return
                        MotorCityLocalization.Format(
                            "daily.all_done",
                            completedDays);
                }

                DailyTask task =
                    FirstIncomplete();

                if (task == null)
                {
                    return
                        MotorCityLocalization.Text(
                            "daily.all_tasks");
                }

                return
                    MotorCityLocalization.Format(
                        TaskTextKey(
                            task.Type),
                        Mathf.Min(
                            task.Progress,
                            task.Target),
                        task.Target);
            }
        }

        public void Initialize(
            ActivityManager activityManager,
            PlayerWallet targetWallet,
            TurboPetSystem turboSystem)
        {
            activities =
                activityManager;
            wallet =
                targetWallet;
            turbo =
                turboSystem;

            currentDay =
                Math.Max(
                    1L,
                    MotorCityPlatform.ServerUnixTime /
                    86400L);

            completedDays =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        CompletedDaysKey,
                        0));

            LoadOrCreateDay();

            if (activities != null)
            {
                activities.ActivityCompleted +=
                    OnActivityCompleted;
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
                        Time.unscaledDeltaTime);
            }

            dayCheckTimer -=
                Time.unscaledDeltaTime;

            if (dayCheckTimer > 0f)
                return;

            dayCheckTimer =
                DayCheckInterval;

            long serverDay =
                Math.Max(
                    1L,
                    MotorCityPlatform.ServerUnixTime /
                    86400L);

            if (serverDay ==
                currentDay)
            {
                return;
            }

            currentDay =
                serverDay;

            CreateDay();

            StatusText =
                MotorCityLocalization.Text(
                    "daily.new_day");

            messageTimer =
                MessageSeconds;
        }

        private void OnDestroy()
        {
            if (activities != null)
            {
                activities.ActivityCompleted -=
                    OnActivityCompleted;
            }
        }

        private void OnActivityCompleted(
            string activityId)
        {
            if (dayCompleted)
            {
                return;
            }

            bool changed =
                false;

            for (int i = 0;
                 i < tasks.Length;
                 i++)
            {
                DailyTask task =
                    tasks[i];

                if (task == null ||
                    task.Completed ||
                    !Matches(
                        task.Type,
                        activityId))
                {
                    continue;
                }

                task.Progress =
                    Mathf.Min(
                        task.Target,
                        task.Progress + 1);

                if (task.Progress >=
                    task.Target)
                {
                    task.Completed =
                        true;

                    int reward =
                        120 +
                        i * 40;

                    wallet?.AddCredits(
                        reward);

                    turbo?.AddXp(
                        12);

                    StatusText =
                        MotorCityLocalization.Format(
                            "daily.task_done",
                            i + 1,
                            reward);

                    messageTimer =
                        MessageSeconds;
                }

                changed =
                    true;
            }

            if (!changed)
                return;

            SaveDay();

            if (CompletedTasks >= 3)
            {
                CompleteDay();
            }
        }

        private void CompleteDay()
        {
            if (dayCompleted)
                return;

            dayCompleted =
                true;

            completedDays++;

            const int completionReward =
                350;

            wallet?.AddCredits(
                completionReward);

            turbo?.AddXp(
                30);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                CompletedDaysKey,
                completedDays);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DayCompletedKey,
                1);

            MotorCity.Persistence.MotorCitySaveService.Save();

            int milestoneReward =
                MilestoneReward(
                    completedDays);

            if (milestoneReward > 0)
            {
                wallet?.AddCredits(
                    milestoneReward);

                int skinIndex =
                    MilestoneSkinIndex(
                        completedDays);

                if (skinIndex > 0)
                {
                    turbo?.UnlockSkin(
                        skinIndex);
                }

                StatusText =
                    MotorCityLocalization.Format(
                        "daily.milestone",
                        completedDays,
                        completionReward +
                        milestoneReward);
            }
            else
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "daily.complete",
                        completionReward,
                        completedDays);
            }

            messageTimer =
                MessageSeconds + 1f;
        }

        private void LoadOrCreateDay()
        {
            long savedDay =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    DayKey,
                    0);

            if (savedDay !=
                currentDay)
            {
                CreateDay();
                return;
            }

            dayCompleted =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    DayCompletedKey,
                    0) != 0;

            for (int i = 0;
                 i < tasks.Length;
                 i++)
            {
                DailyTaskType type =
                    ResolveType(
                        currentDay,
                        i);

                int target =
                    ResolveTarget(
                        type,
                        i);

                int progress =
                    Mathf.Max(
                        0,
                        MotorCity.Persistence.MotorCitySaveService.GetInt(
                            ProgressKey(
                                i),
                            0));

                tasks[i] =
                    new DailyTask
                    {
                        Type = type,
                        Target = target,
                        Progress =
                            Mathf.Min(
                                target,
                                progress),
                        Completed =
                            MotorCity.Persistence.MotorCitySaveService.GetInt(
                                CompleteKey(
                                    i),
                                0) != 0
                    };
            }
        }

        private void CreateDay()
        {
            dayCompleted =
                false;

            for (int i = 0;
                 i < tasks.Length;
                 i++)
            {
                DailyTaskType type =
                    ResolveType(
                        currentDay,
                        i);

                tasks[i] =
                    new DailyTask
                    {
                        Type = type,
                        Target =
                            ResolveTarget(
                                type,
                                i),
                        Progress = 0,
                        Completed = false
                    };
            }

            SaveDay();
        }

        private void SaveDay()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DayKey,
                (int)Math.Min(
                    (long)int.MaxValue,
                    currentDay));

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DayCompletedKey,
                dayCompleted
                    ? 1
                    : 0);

            for (int i = 0;
                 i < tasks.Length;
                 i++)
            {
                DailyTask task =
                    tasks[i];

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    ProgressKey(
                        i),
                    task?.Progress ?? 0);

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    CompleteKey(
                        i),
                    task != null &&
                    task.Completed
                        ? 1
                        : 0);
            }

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private DailyTask FirstIncomplete()
        {
            foreach (DailyTask task in
                     tasks)
            {
                if (task != null &&
                    !task.Completed)
                {
                    return task;
                }
            }

            return null;
        }

        private static DailyTaskType ResolveType(
            long day,
            int slot)
        {
            int rotation =
                (int)((day + slot * 2L) %
                      4L);

            return
                rotation switch
                {
                    0 =>
                        DailyTaskType.AnyActivity,
                    1 =>
                        DailyTaskType.Race,
                    2 =>
                        DailyTaskType.Drift,
                    _ =>
                        DailyTaskType.Delivery
                };
        }

        private static int ResolveTarget(
            DailyTaskType type,
            int slot)
        {
            if (type ==
                DailyTaskType.AnyActivity)
            {
                return
                    slot == 2
                        ? 2
                        : 1;
            }

            return 1;
        }

        private static bool Matches(
            DailyTaskType type,
            string activityId)
        {
            return
                type switch
                {
                    DailyTaskType.AnyActivity =>
                        !string.IsNullOrWhiteSpace(
                            activityId),
                    DailyTaskType.Race =>
                        activityId == "sprint" ||
                        activityId == "circuit",
                    DailyTaskType.Drift =>
                        activityId == "drift",
                    DailyTaskType.Delivery =>
                        activityId == "delivery",
                    _ =>
                        false
                };
        }

        private static string TaskTextKey(
            DailyTaskType type)
        {
            return
                type switch
                {
                    DailyTaskType.Race =>
                        "daily.race",
                    DailyTaskType.Drift =>
                        "daily.drift",
                    DailyTaskType.Delivery =>
                        "daily.delivery",
                    _ =>
                        "daily.any"
                };
        }

        private static int MilestoneSkinIndex(
            int completedDayCount)
        {
            return
                completedDayCount switch
                {
                    3 => 1,
                    7 => 2,
                    14 => 3,
                    30 => 4,
                    _ => 0
                };
        }

        private static int MilestoneReward(
            int completedDayCount)
        {
            return
                completedDayCount switch
                {
                    3 => 600,
                    7 => 1200,
                    14 => 2400,
                    30 => 5000,
                    _ => 0
                };
        }

        private static string ProgressKey(
            int slot)
        {
            return
                "MotorCity.Daily.Progress." +
                slot;
        }

        private static string CompleteKey(
            int slot)
        {
            return
                "MotorCity.Daily.Complete." +
                slot;
        }

        private enum DailyTaskType
        {
            AnyActivity = 0,
            Race = 1,
            Drift = 2,
            Delivery = 3
        }

        private sealed class DailyTask
        {
            public DailyTaskType Type;
            public int Target;
            public int Progress;
            public bool Completed;
        }
    }
}
