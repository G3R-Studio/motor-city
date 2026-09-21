using System;
using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CareerProgressionSystem : MonoBehaviour
    {
        private const string StageKey =
            "MotorCity.Career.Stage";

        private const string DeliveryKey =
            "MotorCity.Career.DeliveryWins";

        private const string DriftKey =
            "MotorCity.Career.DriftWins";

        private const string SprintKey =
            "MotorCity.Career.SprintWins";

        private const string CircuitKey =
            "MotorCity.Career.CircuitWins";

        private const float RewardMessageSeconds =
            7f;

        private ActivityManager activityManager;
        private PlayerWallet wallet;
        private PlayerReputation reputation;

        private int deliveryWins;
        private int driftWins;
        private int sprintWins;
        private int circuitWins;
        private float messageTimer;

        public int Stage { get; private set; }

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; } =
            string.Empty;

        public string HudLine =>
            BuildHudLine();

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

            Stage =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        StageKey,
                        0),
                    0,
                    StageCount);

            deliveryWins =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        DeliveryKey,
                        0));

            driftWins =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        DriftKey,
                        0));

            sprintWins =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        SprintKey,
                        0));

            circuitWins =
                Mathf.Max(
                    0,
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        CircuitKey,
                        0));

            if (activityManager != null)
            {
                activityManager.ActivityResultShown +=
                    HandleActivityResult;
            }

            // Handles saves created before career stages existed.
            EvaluateStages(
                false);
        }

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityResultShown -=
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

        public void SetStageForTesting(
            int stage)
        {
            Stage =
                Mathf.Clamp(
                    stage,
                    0,
                    StageCount);

            int completedWins =
                Stage switch
                {
                    0 => 0,
                    1 => 1,
                    2 => 3,
                    _ => 5
                };

            deliveryWins = completedWins;
            driftWins = completedWins;
            sprintWins = completedWins;
            circuitWins = completedWins;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                StageKey,
                Stage);

            SaveCounters();
            MotorCity.Persistence.MotorCitySaveService.Save();

            messageTimer = 0f;
            StatusText = string.Empty;
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success)
                return;

            switch (activityId)
            {
                case "delivery":
                    deliveryWins++;
                    break;

                case "drift":
                    driftWins++;
                    break;

                case "sprint":
                    sprintWins++;
                    break;

                case "circuit":
                    circuitWins++;
                    break;

                default:
                    return;
            }

            SaveCounters();
            EvaluateStages(
                true);
        }

        private void EvaluateStages(
            bool showReward)
        {
            while (Stage < StageCount &&
                   MeetsStageRequirements(
                       Stage))
            {
                int completedStage =
                    Stage;

                Stage++;

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    StageKey,
                    Stage);

                MotorCity.Persistence.MotorCitySaveService.Save();

                GrantStageReward(
                    completedStage,
                    showReward);
            }
        }

        private bool MeetsStageRequirements(
            int stage)
        {
            int required =
                RequiredWins(
                    stage);

            return
                deliveryWins >= required &&
                driftWins >= required &&
                sprintWins >= required &&
                circuitWins >= required;
        }

        private void GrantStageReward(
            int completedStage,
            bool showReward)
        {
            int credits =
                completedStage switch
                {
                    0 => 1200,
                    1 => 2500,
                    2 => 5000,
                    _ => 0
                };

            int rep =
                completedStage switch
                {
                    0 => 200,
                    1 => 400,
                    2 => 750,
                    _ => 0
                };

            wallet?.AddCredits(
                credits);

            reputation?.AddReputation(
                rep);

            if (!showReward)
                return;

            StatusText =
                MotorCityLocalization.Format(
                    "career.reward",
                    completedStage + 1,
                    credits,
                    rep);

            messageTimer =
                RewardMessageSeconds;
        }

        private string BuildHudLine()
        {
            if (Stage >= StageCount)
            {
                return
                    MotorCityLocalization.Text("career.legend");
            }

            int required =
                RequiredWins(
                    Stage);

            string stageName =
                Stage switch
                {
                    0 => MotorCityLocalization.Text("career.rookie"),
                    1 => MotorCityLocalization.Text("career.street_pro"),
                    2 => MotorCityLocalization.Text("career.elite"),
                    _ => "MOTOR CITY"
                };

            return
                MotorCityLocalization.Format(
                    "career.hud",
                    Stage + 1,
                    StageCount,
                    stageName,
                    Mathf.Min(deliveryWins, required),
                    required,
                    Mathf.Min(driftWins, required),
                    Mathf.Min(sprintWins, required),
                    Mathf.Min(circuitWins, required));
        }

        private static int RequiredWins(
            int stage)
        {
            return
                stage switch
                {
                    0 => 1,
                    1 => 3,
                    2 => 5,
                    _ => int.MaxValue
                };
        }

        private void SaveCounters()
        {
            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DeliveryKey,
                deliveryWins);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                DriftKey,
                driftWins);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                SprintKey,
                sprintWins);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                CircuitKey,
                circuitWins);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private const int StageCount = 3;
    }
}
