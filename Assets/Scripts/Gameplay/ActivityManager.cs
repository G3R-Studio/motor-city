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
        public string ResultActivityId { get; private set; }
        public string ResultTitle { get; private set; }
        public string ResultHeadline { get; private set; }
        public string ResultDetails { get; private set; }
        public int ResultRewardCredits { get; private set; }
        public bool ResultSuccess { get; private set; }
        public int ResultReputationReward { get; private set; }
        public bool ResultIsRookieDelivery { get; private set; }
        public int TotalReputation =>
            reputation == null
                ? 0
                : reputation.Reputation;
        public int ReputationLevel =>
            reputation == null
                ? 1
                : reputation.Level;

        public bool SecondaryProgressionAllowed =>
            (onboarding == null ||
             onboarding.IsComplete) &&
            (story == null ||
             story.IsComplete);

        public bool IsRookieDeliveryStep =>
            onboarding != null &&
            !onboarding.IsComplete &&
            onboarding.CurrentStep == 4;

        private PlayerReputation reputation;
        private DisciplineReputationSystem disciplineReputation;
        private FirstSessionOnboardingSystem onboarding;
        private StoryMissionSystem story;

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
            if (IsBusy && ActiveId != id) return false;

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
            bool success)
        {
            End(activityId);

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

            if (success)
            {
                ActivityCompleted?.Invoke(
                    activityId);
            }
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
