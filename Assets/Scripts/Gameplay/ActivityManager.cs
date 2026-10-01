using System;
using MotorCity.Audio;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class ActivityManager : MonoBehaviour
    {
        public string ActiveId { get; private set; }
        public string ActiveName { get; private set; }
        public bool IsBusy => !string.IsNullOrEmpty(ActiveId);

        public bool HasResult { get; private set; }
        public int ResultSequence { get; private set; }
        public string ResultActivityId { get; private set; }
        public string ResultTitle { get; private set; }
        public string ResultHeadline { get; private set; }
        public string ResultDetails { get; private set; }
        public int ResultRewardCredits { get; private set; }
        public bool ResultSuccess { get; private set; }
        public int ResultReputationReward { get; private set; }
        public bool ResultIsRookieDelivery { get; private set; }
        public bool ResultIsNewRecord { get; private set; }
        public int ResultMasteryXp { get; private set; }
        public string ResultSecondaryProgress { get; private set; } =
            string.Empty;
        public string ResultNextGoal { get; private set; } =
            string.Empty;
        public int TotalReputation =>
            reputation == null
                ? 0
                : reputation.Reputation;
        public int ReputationLevel =>
            reputation == null
                ? 1
                : reputation.Level;

        public bool SecondaryProgressionAllowed =>
            onboarding == null ||
            onboarding.IsComplete;

        public bool IsRookieDeliveryStep =>
            onboarding != null &&
            !onboarding.IsComplete &&
            onboarding.CurrentStep == 4;

        public bool IsOnboardingActive =>
            onboarding != null &&
            !onboarding.IsComplete;

        private PlayerReputation reputation;
        private ActivityStartFlow startFlow;

        public ActivityStartFlow StartFlow =>
            startFlow;
        private DisciplineReputationSystem disciplineReputation;
        private FirstSessionOnboardingSystem onboarding;
        private StoryMissionSystem story;
        private VehicleMasterySystem resultMastery;
        private DailyAdventureSystem resultDaily;
        private SeasonSystem resultSeason;
        private ClubSystem resultClub;
        private ResultNextGoalResolver resultNextGoalResolver;

        public event Action<string, bool> ActivityResultShown;
        public event Action<string, bool> ActivityResultDismissed;
        public event Action<string> ActivityCompleted;

        public string DisciplineHudLine =>
            disciplineReputation == null
                ? string.Empty
                : disciplineReputation.HudLine;

        public bool DisciplineShowMessage =>
            disciplineReputation != null &&
            disciplineReputation.ShowMessage;

        public string DisciplineStatusText =>
            disciplineReputation == null
                ? string.Empty
                : disciplineReputation.StatusText;

        public void Initialize(
            PlayerReputation playerReputation)
        {
            reputation =
                playerReputation;
        }

        public void SetStartFlow(
            ActivityStartFlow flow)
        {
            startFlow =
                flow;
        }

        public bool RequestStart(
            string id,
            string displayName,
            Action beginPreparedActivity)
        {
            if (startFlow == null)
            {
                if (!TryBegin(
                        id,
                        displayName))
                {
                    return false;
                }

                beginPreparedActivity?.Invoke();
                return true;
            }

            return startFlow.RequestStart(
                id,
                displayName,
                beginPreparedActivity);
        }

        public bool RequestStart(
            string id,
            string displayName,
            float countdownSeconds,
            Action onCountdownStarted,
            Action<int> onCountdownTick,
            Action onGameplayStarted)
        {
            if (startFlow == null)
            {
                if (!TryBegin(
                        id,
                        displayName))
                {
                    return false;
                }

                onCountdownStarted?.Invoke();

                if (countdownSeconds > 0f)
                {
                    onCountdownTick?.Invoke(
                        Mathf.Max(
                            1,
                            Mathf.CeilToInt(
                                countdownSeconds)));
                }

                onGameplayStarted?.Invoke();
                return true;
            }

            return startFlow.RequestStart(
                id,
                displayName,
                countdownSeconds,
                onCountdownStarted,
                onCountdownTick,
                onGameplayStarted);
        }

        public bool IsStartPending(
            string id)
        {
            return
                startFlow != null &&
                startFlow.IsPending(
                    id);
        }

        public bool CancelPendingStart(
            string id)
        {
            return
                startFlow != null &&
                startFlow.CancelPending(
                    id);
        }

        public void SetResultNextGoalResolver(
            ResultNextGoalResolver resolver)
        {
            resultNextGoalResolver =
                resolver;
        }

        public void SetResultProgressSystems(
            VehicleMasterySystem mastery,
            DailyAdventureSystem daily,
            SeasonSystem season,
            ClubSystem club)
        {
            resultMastery =
                mastery;

            resultDaily =
                daily;

            resultSeason =
                season;

            resultClub =
                club;
        }

        public void SetDisciplineReputation(
            DisciplineReputationSystem system)
        {
            disciplineReputation =
                system;
        }

        public bool HasDisciplineLevel(
            DisciplineType type,
            int requiredLevel)
        {
            return
                disciplineReputation != null &&
                disciplineReputation.HasLevel(
                    type,
                    requiredLevel);
        }

        public bool TryBegin(string id, string displayName)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (HasResult) return false;
            if (IsBusy) return false;

            if (!CanBeginDuringOnboarding(
                    id))
            {
                onboarding?.ShowActivityBlockedPrompt();
                return false;
            }

            if (!CanBeginDuringStory(
                    id))
            {
                story?.ShowActivityBlockedPrompt();
                return false;
            }

            ActiveId = id;
            ActiveName = string.IsNullOrEmpty(displayName) ? id : displayName;
            return true;
        }

        public void SetOnboardingSystem(
            FirstSessionOnboardingSystem system)
        {
            onboarding =
                system;
        }

        private bool CanBeginDuringOnboarding(
            string id)
        {
            if (onboarding == null ||
                onboarding.IsComplete)
            {
                return true;
            }

            return onboarding.CurrentStep switch
            {
                4 => id == "delivery",
                5 or 6 => id == "garage",
                _ => false
            };
        }

        public void SetStorySystem(
            StoryMissionSystem system)
        {
            story =
                system;
        }

        private bool CanBeginDuringStory(
            string id)
        {
            if (story == null ||
                story.IsComplete ||
                onboarding != null &&
                !onboarding.IsComplete)
            {
                return true;
            }

            if (id == "garage")
                return true;

            string required =
                story.RequiredActivityId;

            if (string.IsNullOrWhiteSpace(
                    required))
            {
                return true;
            }

            return required == "*" ||
                   required == id;
        }

        public void End(string id)
        {
            if (ActiveId != id) return;
            ActiveId = null;
            ActiveName = null;
        }

        public void ShowResult(
            string activityId,
            string title,
            string headline,
            string details,
            int rewardCredits,
            bool success,
            bool newRecord = false)
        {
            int masteryBefore =
                resultMastery == null
                    ? 0
                    : resultMastery.CurrentXp;

            int dailyBefore =
                resultDaily == null
                    ? 0
                    : resultDaily.CompletedTasks;

            int seasonMissionBefore =
                resultSeason == null
                    ? 0
                    : resultSeason.CurrentMissionNumber;

            int seasonProgressBefore =
                resultSeason == null
                    ? 0
                    : resultSeason.CurrentMissionProgress;

            int clubBefore =
                resultClub == null
                    ? 0
                    : resultClub.WeeklyContribution;

            End(activityId);

            ResultSequence =
                ResultSequence == int.MaxValue
                    ? 1
                    : ResultSequence + 1;

            HasResult = true;
            ResultActivityId = activityId;
            ResultTitle = title;
            ResultHeadline = headline;
            ResultDetails = details;
            ResultRewardCredits =
                Mathf.Max(
                    0,
                    rewardCredits);

            ResultSuccess =
                success;

            ResultIsNewRecord =
                success &&
                newRecord;

            ResultIsRookieDelivery =
                success &&
                activityId == "delivery" &&
                onboarding != null &&
                !onboarding.IsComplete &&
                onboarding.CurrentStep == 4;

            MotorCitySfxRuntime.PlayActivityResult(
                success);

            ResultReputationReward =
                success
                    ? Mathf.Clamp(
                        Mathf.RoundToInt(
                            ResultRewardCredits *
                            0.10f),
                        25,
                        150)
                    : 0;

            reputation?.AddReputation(
                ResultReputationReward);

            ActivityResultShown?.Invoke(
                activityId,
                success);

            ResultMasteryXp =
                0;

            ResultSecondaryProgress =
                string.Empty;

            if (success)
            {
                ActivityCompleted?.Invoke(
                    activityId);

                ResultMasteryXp =
                    resultMastery == null
                        ? 0
                        : Mathf.Max(
                            0,
                            resultMastery.CurrentXp -
                            masteryBefore);

                BuildResultSecondaryProgress(
                    dailyBefore,
                    seasonMissionBefore,
                    seasonProgressBefore,
                    clubBefore);
            }

            ResultNextGoal =
                resultNextGoalResolver == null
                    ? string.Empty
                    : resultNextGoalResolver.Resolve();
        }

        private void BuildResultSecondaryProgress(
            int dailyBefore,
            int seasonMissionBefore,
            int seasonProgressBefore,
            int clubBefore)
        {
            System.Collections.Generic.List<string> lines =
                new();

            if (resultDaily != null &&
                resultDaily.CompletedTasks >
                    dailyBefore)
            {
                lines.Add(
                    MotorCity.Localization.MotorCityLocalization.Format(
                        "hud.result_daily_progress",
                        resultDaily.CompletedTasks,
                        3));
            }

            if (resultSeason != null)
            {
                bool seasonChanged =
                    resultSeason.CurrentMissionNumber !=
                        seasonMissionBefore ||
                    resultSeason.CurrentMissionProgress !=
                        seasonProgressBefore;

                if (seasonChanged)
                {
                    lines.Add(
                        MotorCity.Localization.MotorCityLocalization.Format(
                            "hud.result_season_progress",
                            resultSeason.CurrentMissionNumber,
                            resultSeason.MissionCount));
                }
            }

            if (resultClub != null &&
                resultClub.WeeklyContribution >
                    clubBefore)
            {
                lines.Add(
                    MotorCity.Localization.MotorCityLocalization.Format(
                        "hud.result_club_progress",
                        resultClub.WeeklyContribution -
                            clubBefore));
            }

            ResultSecondaryProgress =
                string.Join(
                    "\n",
                    lines);
        }

        public void ReportCompletion(
            string activityId)
        {
            if (string.IsNullOrWhiteSpace(
                    activityId))
            {
                return;
            }

            ActivityCompleted?.Invoke(
                activityId);
        }

        public void DismissResult(
            bool notifyDismissed = true)
        {
            string dismissedActivityId =
                ResultActivityId;

            bool dismissedSuccess =
                ResultSuccess;

            HasResult = false;
            ResultActivityId = null;
            ResultTitle = null;
            ResultHeadline = null;
            ResultDetails = null;
            ResultRewardCredits = 0;
            ResultReputationReward = 0;
            ResultSuccess = false;
            ResultIsRookieDelivery = false;
            ResultIsNewRecord = false;
            ResultMasteryXp = 0;
            ResultSecondaryProgress =
                string.Empty;
            ResultNextGoal =
                string.Empty;

            if (notifyDismissed)
            {
                ActivityResultDismissed?.Invoke(
                    dismissedActivityId,
                    dismissedSuccess);
            }
        }

        public bool IsActive(string id) => ActiveId == id;
    }
}
