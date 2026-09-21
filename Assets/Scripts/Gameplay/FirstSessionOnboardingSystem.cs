using MotorCity.Input;
using MotorCity.Localization;
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
        private const float MessageSeconds =
            4f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private ActivityManager activities;
        private GarageUpgradeSystem garage;
        private TurboPetSystem turbo;

        private int step;
        private float drivenDistance;
        private Vector3 lastPosition;
        private bool activitySucceeded;
        private float messageTimer;
        private float introTimer;

        public bool IsComplete { get; private set; }

        public bool ShowMessage =>
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
                        0 =>
                            MotorCityLocalization.Text(
                                "onboarding.throttle"),
                        1 =>
                            MotorCityLocalization.Text(
                                "onboarding.steer"),
                        2 =>
                            MotorCityLocalization.Format(
                                "onboarding.drive",
                                Mathf.Min(
                                    80,
                                    Mathf.RoundToInt(
                                        drivenDistance))),
                        3 =>
                            MotorCityLocalization.Text(
                                "onboarding.meet_turbo"),
                        4 =>
                            MotorCityLocalization.Text(
                                "onboarding.activity"),
                        5 =>
                            MotorCityLocalization.Text(
                                "onboarding.garage"),
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
            TurboPetSystem turboSystem)
        {
            car =
                targetCar;
            wallet =
                targetWallet;
            reputation =
                targetReputation;
            activities =
                activityManager;
            garage =
                garageSystem;
            turbo =
                turboSystem;

            IsComplete =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    CompleteKey,
                    0) != 0;

            step =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        StepKey,
                        0),
                    0,
                    6);

            if (!IsComplete &&
                IsLegacyPlayer())
            {
                CompleteSilently();
                return;
            }

            if (car != null)
            {
                lastPosition =
                    car.transform.position;
            }

            if (activities != null)
            {
                activities.ActivityResultShown +=
                    OnActivityResult;
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
            if (IsComplete)
                return;

            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.unscaledDeltaTime);
            }

            TrackDistance();

            switch (step)
            {
                case 0:
                    if (MotorCityInput.ThrottleHeld ||
                        MotorCityInput.ReverseHeld ||
                        (car != null &&
                         car.SpeedKph > 3f))
                    {
                        Advance(
                            "onboarding.good_throttle");
                    }
                    break;

                case 1:
                    if ((MotorCityInput.SteerLeftHeld ||
                         MotorCityInput.SteerRightHeld) &&
                        car != null &&
                        car.SpeedKph > 4f)
                    {
                        drivenDistance = 0f;
                        lastPosition =
                            car.transform.position;

                        Advance(
                            "onboarding.good_steer");
                    }
                    break;

                case 2:
                    if (drivenDistance >= 80f)
                    {
                        wallet?.AddCredits(
                            250);

                        Advance(
                            "onboarding.first_reward");
                    }
                    break;

                case 3:
                    introTimer +=
                        Time.unscaledDeltaTime;

                    if (introTimer >= 4f)
                    {
                        Advance(
                            "onboarding.turbo_ready");
                    }
                    break;

                case 4:
                    if (activitySucceeded)
                    {
                        Advance(
                            "onboarding.activity_done");
                    }
                    break;

                case 5:
                    if (garage != null &&
                        garage.IsOpen)
                    {
                        Advance(
                            "onboarding.garage_done");
                    }
                    break;

                default:
                    Complete();
                    break;
            }
        }

        private void OnDestroy()
        {
            if (activities != null)
            {
                activities.ActivityResultShown -=
                    OnActivityResult;
            }
        }

        private void OnActivityResult(
            string activityId,
            bool success)
        {
            if (!IsComplete &&
                success)
            {
                activitySucceeded =
                    true;
            }
        }

        private void TrackDistance()
        {
            if (car == null)
                return;

            Vector3 current =
                car.transform.position;

            Vector3 delta =
                current -
                lastPosition;

            delta.y = 0f;

            float distance =
                delta.magnitude;

            if (distance <= 30f)
            {
                drivenDistance +=
                    distance;
            }

            lastPosition =
                current;
        }

        private void Advance(
            string messageKey)
        {
            step =
                Mathf.Min(
                    6,
                    step + 1);

            introTimer = 0f;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                StepKey,
                step);

            MotorCity.Persistence.MotorCitySaveService.Save();

            StatusText =
                MotorCityLocalization.Text(
                    messageKey);

            messageTimer =
                MessageSeconds;
        }

        private void Complete()
        {
            IsComplete = true;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                CompleteKey,
                1);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                StepKey,
                6);

            MotorCity.Persistence.MotorCitySaveService.Save();

            StatusText =
                MotorCityLocalization.Text(
                    "onboarding.complete");

            messageTimer =
                MessageSeconds + 1f;
        }

        private void CompleteSilently()
        {
            IsComplete = true;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                CompleteKey,
                1);

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                StepKey,
                6);

            MotorCity.Persistence.MotorCitySaveService.Save();
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
