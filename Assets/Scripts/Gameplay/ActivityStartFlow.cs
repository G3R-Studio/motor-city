using System;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class ActivityStartFlow : MonoBehaviour
    {
        private ActivityManager activityManager;
        private bool requestInProgress;

        public bool RequestInProgress =>
            requestInProgress;

        public string PendingActivityId { get; private set; }

        public void Initialize(
            ActivityManager manager)
        {
            activityManager =
                manager;
        }

        public bool RequestStart(
            string activityId,
            string displayName,
            Action beginPreparedActivity)
        {
            if (requestInProgress ||
                activityManager == null ||
                string.IsNullOrWhiteSpace(
                    activityId) ||
                beginPreparedActivity == null)
            {
                return false;
            }

            requestInProgress =
                true;

            PendingActivityId =
                activityId;

            if (!activityManager.TryBegin(
                    activityId,
                    displayName))
            {
                ClearRequest();
                return false;
            }

            // P0.3 centralizes interaction -> eligibility -> preparation.
            // P0.4 can insert its ad decision here without rewriting each
            // activity start path again.
            beginPreparedActivity();

            ClearRequest();
            return true;
        }

        private void ClearRequest()
        {
            requestInProgress =
                false;

            PendingActivityId =
                null;
        }
    }
}
