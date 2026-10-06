using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Persistence;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class FirstSessionOnboardingSystem :
        MonoBehaviour
    {
        private const string StepKey =
            "MotorCity.Onboarding.Step";
        private const string CompleteKey =
            "MotorCity.Onboarding.Complete";
        private const string FlowVersionKey =
            "MotorCity.Onboarding.FlowVersion";

        private const int CurrentFlowVersion = 2;

        public const int ThrottleStep = 0;
        public const int BrakeStep = 1;
        public const int SteeringStep = 2;
        public const int GarageStep = 3;
        public const int CustomizationStep = 4;
        public const int RookieSprintStep = 5;
        public const int CompletionStep = 6;

        private const int TutorialTaskCount = 6;
        private const float MessageSeconds = 4f;
        private const float CompletionPresentationSeconds = 5f;
        private const float MinimumBrakeSpeedKph = 7f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private ActivityManager activities;
        private GarageUpgradeSystem garage;
        private TurboPetSystem turbo;
        private VehicleCustomizationSystem customization;

        private int step;
        private float messageTimer;
        private float messageDelayTimer;
        private float completionTimer;

        private int customizationColorIndexAtStepStart = -1;
        private int customizationWheelIndexAtStepStart = -1;
        private int customizationNeonIndexAtStepStart = -1;

        public bool IsComplete { get; private set; }

        public int CurrentStep =>
            step;

        public int CurrentStepNumber =>
            Mathf.Clamp(
                step + 1,
                1,
                TutorialTaskCount);

        public int StepCount =>
            TutorialTaskCount;

        public int CurrentRewardCredits =>
            0;

        public bool IsThrottleStep =>
            !IsComplete &&
            step == ThrottleStep;

        public bool IsBrakeStep =>
            !IsComplete &&
            step == BrakeStep;

        public bool IsSteeringStep =>
            !IsComplete &&
            step == SteeringStep;

        public bool IsGarageStep =>
            !IsComplete &&
            step == GarageStep;

        public bool IsCustomizationStep =>
            !IsComplete &&
            step == CustomizationStep;

        public bool IsRookieSprintStep =>
            !IsComplete &&
            step == RookieSprintStep;

        public bool IsCompletionPresentationStep =>
            !IsComplete &&
            step == CompletionStep;

        public bool ShowMessage =>
            messageDelayTimer <= 0f &&
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public string ObjectiveLine
        {
            get
            {
                if (IsComplete)
                    return string.Empty;

                return
                    step switch
                    {
                        ThrottleStep =>
                            MotorCityLocalization.Text(
                                ResolveThrottleObjectiveKey()),

                        BrakeStep =>
                            MotorCityLocalization.Text(
                                ResolveBrakeObjectiveKey()),

                        SteeringStep =>
                            MotorCityLocalization.Text(
                                ResolveSteeringObjectiveKey()),

                        GarageStep =>
                            MotorCityLocalization.Text(
                                "onboarding.garage"),

                        CustomizationStep =>
                            MotorCityLocalization.Text(
                                "onboarding.customize"),

                        RookieSprintStep =>
                            MotorCityLocalization.Text(
                                ResolveRookieSprintObjectiveKey()),

                        CompletionStep =>
                            MotorCityLocalization.Text(
                                "onboarding.complete_next"),

                        _ =>
                            turbo != null
                                ? turbo.DailyObjectiveLine
                                : MotorCityLocalization.Text(
                                    "onboarding.daily")
                    };
            }
        }

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            PlayerReputation targetReputation,
            ActivityManager activityManager,
            GarageUpgradeSystem garageSystem,
            TurboPetSystem turboSystem,
            VehicleCustomizationSystem customizationSystem)
        {
            car =
                targetCar;

            wallet =
                targetWallet;

            reputation =
                targetReputation;

            activities =
                activityManager;

            activities?.SetOnboardingSystem(
                this);

            garage =
                garageSystem;

            turbo =
                turboSystem;

            customization =
                customizationSystem;

            bool hasOnboardingSave =
                MotorCitySaveService.HasKey(
                    StepKey) ||
                MotorCitySaveService.HasKey(
                    CompleteKey);

            IsComplete =
                MotorCitySaveService.GetInt(
                    CompleteKey,
                    0) != 0;

            // Do not clamp before migration: flow v1 had step 7 while v2
            // ends at step 6, and clamping would lose the distinction.
            int savedStep =
                MotorCitySaveService.GetInt(
                    StepKey,
                    ThrottleStep);

            int savedFlowVersion =
                MotorCitySaveService.GetInt(
                    FlowVersionKey,
                    0);

            step =
                IsComplete
                    ? CompletionStep
                    : MigrateStep(
                        savedStep,
                        savedFlowVersion);

            if (!IsComplete &&
                !hasOnboardingSave &&
                IsLegacyPlayer())
            {
                CompleteSilently();
                return;
            }

            PersistFlowVersion();

            if (activities != null)
            {
                activities.ActivityCompleted +=
                    OnActivityCompleted;
            }

            if (customization != null)
            {
                customization.CustomizationChanged +=
                    OnCustomizationChanged;
            }

            if (IsCustomizationStep)
            {
                CaptureCustomizationBaseline();
            }

            if (!IsComplete)
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "onboarding.welcome");

                messageTimer =
                    MessageSeconds;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            if (messageDelayTimer > 0f)
            {
                messageDelayTimer =
                    Mathf.Max(
                        0f,
                        messageDelayTimer -
                        Time.unscaledDeltaTime);
            }
            else if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.unscaledDeltaTime);
            }

            if (IsComplete)
                return;

            switch (step)
            {
                case ThrottleStep:
                    // Require the actual throttle control. Spawn movement,
                    // slopes and reverse input must not skip the first lesson.
                    if (MotorCityInput.ThrottleHeld)
                    {
                        Advance(
                            "onboarding.good_throttle");
                    }
                    break;

                case BrakeStep:
                    // In the current Prometeo bridge the reverse control is
                    // also the service brake while the vehicle is moving
                    // forward. Require forward motion so reversing in place
                    // cannot satisfy the lesson.
                    if (MotorCityInput.ReverseHeld &&
                        car != null &&
                        car.ForwardSpeedKph >=
                            MinimumBrakeSpeedKph)
                    {
                        Advance(
                            "onboarding.good_brake");
                    }
                    break;

                case SteeringStep:
                    if ((MotorCityInput.SteerLeftHeld ||
                         MotorCityInput.SteerRightHeld) &&
                        car != null &&
                        car.SpeedKph > 4f)
                    {
                        Advance(
                            "onboarding.good_steer");
                    }
                    break;

                case GarageStep:
                    if (garage != null &&
                        garage.IsOpen)
                    {
                        Advance(
                            "onboarding.garage_done");
                    }
                    break;

                case CustomizationStep:
                    // Completion is driven by CustomizationChanged so a real
                    // visual change is required.
                    break;

                case RookieSprintStep:
                    // Completion is driven by ActivityCompleted("sprint").
                    break;

                case CompletionStep:
                    completionTimer +=
                        Time.unscaledDeltaTime;

                    if (completionTimer >=
                        CompletionPresentationSeconds)
                    {
                        Complete();
                    }
                    break;
            }

        }

        private void OnDestroy()
        {
            if (activities != null)
            {
                activities.ActivityCompleted -=
                    OnActivityCompleted;
            }

            if (customization != null)
            {
                customization.CustomizationChanged -=
                    OnCustomizationChanged;
            }
        }

        private static string ResolveThrottleObjectiveKey()
        {
            return
                MotorCityInput.CurrentControlScheme switch
                {
                    MotorCityControlScheme.Arrows =>
                        "onboarding.throttle.arrows",

                    MotorCityControlScheme.Wheel =>
                        "onboarding.throttle.wheel",

                    _ =>
                        "onboarding.throttle.keyboard"
                };
        }

        private static string ResolveBrakeObjectiveKey()
        {
            return
                MotorCityInput.CurrentControlScheme switch
                {
                    MotorCityControlScheme.Keyboard =>
                        "onboarding.brake.keyboard",

                    _ =>
                        "onboarding.brake.touch"
                };
        }

        private static string ResolveSteeringObjectiveKey()
        {
            return
                MotorCityInput.CurrentControlScheme switch
                {
                    MotorCityControlScheme.Arrows =>
                        "onboarding.steer.arrows",

                    MotorCityControlScheme.Wheel =>
                        "onboarding.steer.wheel",

                    _ =>
                        "onboarding.steer.keyboard"
                };
        }

        private static string ResolveRookieSprintObjectiveKey()
        {
            return
                MotorCityInput.CurrentControlScheme ==
                MotorCityControlScheme.Keyboard
                    ? "onboarding.race.keyboard"
                    : "onboarding.race.touch";
        }

        private void OnCustomizationChanged()
        {
            if (!IsCustomizationStep ||
                customization == null)
            {
                return;
            }

            if (customizationColorIndexAtStepStart < 0 ||
                customizationWheelIndexAtStepStart < 0 ||
                customizationNeonIndexAtStepStart < 0)
            {
                CaptureCustomizationBaseline();
                return;
            }

            bool changed =
                customization.SelectedColorIndex !=
                    customizationColorIndexAtStepStart ||
                customization.SelectedWheelStyleIndex !=
                    customizationWheelIndexAtStepStart ||
                customization.SelectedNeonIndex !=
                    customizationNeonIndexAtStepStart;

            if (!changed)
                return;

            garage?.CloseAfterRookieCustomization();

            Advance(
                "onboarding.customized");
        }

        private void CaptureCustomizationBaseline()
        {
            if (customization == null)
            {
                customizationColorIndexAtStepStart =
                    -1;

                customizationWheelIndexAtStepStart =
                    -1;

                customizationNeonIndexAtStepStart =
                    -1;

                return;
            }

            customizationColorIndexAtStepStart =
                customization.SelectedColorIndex;

            customizationWheelIndexAtStepStart =
                customization.SelectedWheelStyleIndex;

            customizationNeonIndexAtStepStart =
                customization.SelectedNeonIndex;
        }

        private void OnActivityCompleted(
            string activityId)
        {
            if (!IsRookieSprintStep ||
                activityId != "sprint")
            {
                return;
            }

            Advance(
                "onboarding.race_done");
        }

        private void Advance(
            string messageKey)
        {
            step =
                Mathf.Min(
                    CompletionStep,
                    step + 1);

            completionTimer =
                0f;

            if (step ==
                CustomizationStep)
            {
                CaptureCustomizationBaseline();
            }

            MotorCitySaveService.SetInt(
                StepKey,
                step);

            PersistFlowVersion();

            MotorCitySaveService.Save();

            StatusText =
                MotorCityLocalization.Text(
                    messageKey);

            messageTimer =
                MessageSeconds;
        }

        private void Complete()
        {
            IsComplete =
                true;

            MotorCitySaveService.SetInt(
                CompleteKey,
                1);

            MotorCitySaveService.SetInt(
                StepKey,
                CompletionStep);

            PersistFlowVersion();

            MotorCitySaveService.Save();

            messageTimer =
                0f;

            messageDelayTimer =
                0f;
        }

        public void ShowWelcomeAfterDelay(
            float delaySeconds)
        {
            if (IsComplete)
                return;

            StatusText =
                MotorCityLocalization.Text(
                    "onboarding.welcome");

            messageTimer =
                MessageSeconds;

            messageDelayTimer =
                Mathf.Max(
                    0f,
                    delaySeconds);
        }

        public void ShowPathPrompt()
        {
            if (IsComplete)
                return;

            StatusText =
                MotorCityLocalization.Text(
                    "onboarding.path_prompt");

            messageTimer =
                MessageSeconds + 2f;
        }

        public void ShowActivityBlockedPrompt()
        {
            if (IsComplete)
                return;

            StatusText =
                MotorCityLocalization.Text(
                    "onboarding.finish_current_step");

            messageTimer =
                MessageSeconds;
        }

        public void AdvanceStepForTesting()
        {
            if (IsComplete)
                return;

            step =
                Mathf.Min(
                    CompletionStep,
                    step + 1);

            completionTimer =
                0f;

            if (step ==
                CustomizationStep)
            {
                CaptureCustomizationBaseline();
            }

            MotorCitySaveService.DeleteKey(
                CompleteKey);

            MotorCitySaveService.SetInt(
                StepKey,
                step);

            PersistFlowVersion();

            MotorCitySaveService.Save();

            StatusText =
                ObjectiveLine;

            messageTimer =
                MessageSeconds;
        }

        public void CompleteForTesting()
        {
            step =
                CompletionStep;

            IsComplete =
                true;

            MotorCitySaveService.SetInt(
                StepKey,
                step);

            MotorCitySaveService.SetInt(
                CompleteKey,
                1);

            PersistFlowVersion();

            MotorCitySaveService.Save();

            StatusText =
                string.Empty;

            messageTimer =
                0f;

            messageDelayTimer =
                0f;
        }

        public void ResetForTesting()
        {
            IsComplete =
                false;

            step =
                ThrottleStep;

            completionTimer =
                0f;

            customizationColorIndexAtStepStart =
                -1;

            customizationWheelIndexAtStepStart =
                -1;

            customizationNeonIndexAtStepStart =
                -1;

            MotorCitySaveService.DeleteKey(
                StepKey);

            MotorCitySaveService.DeleteKey(
                CompleteKey);

            MotorCitySaveService.SetInt(
                FlowVersionKey,
                CurrentFlowVersion);

            MotorCitySaveService.Save();

            StatusText =
                MotorCityLocalization.Text(
                    "onboarding.welcome");

            messageTimer =
                MessageSeconds;

            messageDelayTimer =
                0f;
        }

        private void CompleteSilently()
        {
            IsComplete =
                true;

            step =
                CompletionStep;

            MotorCitySaveService.SetInt(
                CompleteKey,
                1);

            MotorCitySaveService.SetInt(
                StepKey,
                CompletionStep);

            PersistFlowVersion();

            MotorCitySaveService.Save();
        }

        private void PersistFlowVersion()
        {
            MotorCitySaveService.SetInt(
                FlowVersionKey,
                CurrentFlowVersion);
        }

        private static int MigrateStep(
            int savedStep,
            int savedFlowVersion)
        {
            if (savedFlowVersion >=
                CurrentFlowVersion)
            {
                return
                    Mathf.Clamp(
                        savedStep,
                        ThrottleStep,
                        CompletionStep);
            }

            // Flow v1:
            // 0 throttle, 1 steer, 2 drive, 3 reward handoff,
            // 4 delivery, 5 garage, 6 customize, 7 completion.
            // Keep as much progress as possible while inserting the new brake
            // lesson and replacing delivery with the final rookie sprint.
            return savedStep switch
            {
                0 =>
                    ThrottleStep,

                1 =>
                    BrakeStep,

                2 or 3 or 4 or 5 =>
                    GarageStep,

                6 =>
                    CustomizationStep,

                _ =>
                    RookieSprintStep
            };
        }

        private bool IsLegacyPlayer()
        {
            return
                (wallet != null &&
                 wallet.Credits > 0) ||
                (reputation != null &&
                 reputation.Reputation > 0);
        }
    }
}
