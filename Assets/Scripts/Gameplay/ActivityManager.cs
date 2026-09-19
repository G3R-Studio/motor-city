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
        public int TotalReputation =>
            reputation == null
                ? 0
                : reputation.Reputation;
        public int ReputationLevel =>
            reputation == null
                ? 1
                : reputation.Level;

        private PlayerReputation reputation;

        public void Initialize(
            PlayerReputation playerReputation)
        {
            reputation =
                playerReputation;
        }

        public bool TryBegin(string id, string displayName)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (HasResult) return false;
            if (IsBusy && ActiveId != id) return false;

            ActiveId = id;
            ActiveName = string.IsNullOrEmpty(displayName) ? id : displayName;
            return true;
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
        }

        public void DismissResult()
        {
            HasResult = false;
            ResultActivityId = null;
            ResultTitle = null;
            ResultHeadline = null;
            ResultDetails = null;
            ResultRewardCredits = 0;
            ResultReputationReward = 0;
            ResultSuccess = false;
        }

        public bool IsActive(string id) => ActiveId == id;
    }
}
