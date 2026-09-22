using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CarWashJobSystem : MonoBehaviour
    {
        private const string ActivityId =
            "profession_carwash";

        private const float StartRadius =
            13f;

        private const float MaxStartSpeedKph =
            5f;

        private const float PhaseSeconds =
            2.3f;

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private ActivityManager activities;
        private CityProfessionSystem professions;

        private int phase;
        private float phaseProgress;
        private int lastStatusPhase = -1;
        private int lastStatusPercent = -1;

        public bool IsActive { get; private set; }
        public bool IsNearStart { get; private set; }

        public Vector3 StartPoint { get; private set; }

        public Vector3 CurrentTarget =>
            StartPoint;

        public string StatusText { get; private set; }

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            ActivityManager activityManager,
            CityProfessionSystem professionSystem)
        {
            car =
                targetCar;

            wallet =
                targetWallet;

            activities =
                activityManager;

            professions =
                professionSystem;

            Vector3[] delivery =
                CityAssetRuntimeInstaller.DeliveryRoute;

            StartPoint =
                delivery != null &&
                delivery.Length > 11
                    ? delivery[11]
                    : CityAssetRuntimeInstaller.GaragePoint;
        }

        private void Update()
        {
            if (car == null ||
                wallet == null ||
                activities == null)
            {
                return;
            }

            Vector3 startDelta =
                Flat(
                    car.transform.position) -
                Flat(
                    StartPoint);

            IsNearStart =
                startDelta.sqrMagnitude <=
                StartRadius * StartRadius;

            if (IsActive)
            {
                UpdateWash();
                return;
            }

            if (!IsNearStart)
                return;

            if (activities.IsBusy)
            {
                StatusText =
                    MotorCityLocalization.Format(
                        "carwash.busy",
                        activities.ActiveName);
                return;
            }

            if (car.SpeedKph >
                MaxStartSpeedKph)
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "carwash.stop");
                return;
            }

            StatusText =
                MotorCityLocalization.Text(
                    "carwash.start");

            if (MotorCityInput.InteractPressed)
            {
                BeginWash();
            }
        }

        private void BeginWash()
        {
            if (!activities.TryBegin(
                    ActivityId,
                    MotorCityLocalization.Text(
                        "carwash.title")))
            {
                return;
            }

            IsActive =
                true;

            phase =
                0;

            phaseProgress =
                0f;
            lastStatusPhase =
                -1;
            lastStatusPercent =
                -1;

            car.SetDrivingEnabled(
                false);

            UpdateStatus();
        }

        private void UpdateWash()
        {
            if (MotorCityInput.CancelPressed)
            {
                CancelWash();
                return;
            }

            if (MotorCityInput.InteractHeld)
            {
                phaseProgress +=
                    Time.unscaledDeltaTime;
            }
            else
            {
                phaseProgress =
                    Mathf.Max(
                        0f,
                        phaseProgress -
                        Time.unscaledDeltaTime *
                        0.35f);
            }

            if (phaseProgress >=
                PhaseSeconds)
            {
                phase++;
                phaseProgress = 0f;

                if (phase >= 3)
                {
                    CompleteWash();
                    return;
                }
            }

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            float normalized =
                Mathf.Clamp01(
                    phaseProgress /
                    PhaseSeconds);

            int percent =
                Mathf.RoundToInt(
                    normalized *
                    100f);

            if (phase ==
                    lastStatusPhase &&
                percent ==
                    lastStatusPercent)
            {
                return;
            }

            lastStatusPhase =
                phase;
            lastStatusPercent =
                percent;

            string phaseName =
                phase switch
                {
                    0 =>
                        MotorCityLocalization.Text(
                            "carwash.phase.water"),
                    1 =>
                        MotorCityLocalization.Text(
                            "carwash.phase.foam"),
                    _ =>
                        MotorCityLocalization.Text(
                            "carwash.phase.polish")
                };

            StatusText =
                MotorCityLocalization.Format(
                    "carwash.progress",
                    phase + 1,
                    phaseName,
                    percent);
        }

        private void CompleteWash()
        {
            int level =
                professions != null
                    ? professions.RegisterExternalCompletion()
                    : 1;

            int reward =
                Mathf.RoundToInt(
                    300f *
                    (1f +
                     (level - 1) *
                     0.04f));

            wallet.AddCredits(
                reward);

            IsActive =
                false;

            car.SetDrivingEnabled(
                false);

            activities.ShowResult(
                ActivityId,
                MotorCityLocalization.Text(
                    "carwash.title"),
                MotorCityLocalization.Text(
                    "carwash.complete"),
                MotorCityLocalization.Format(
                    "carwash.result",
                    level),
                reward,
                true);

            phase =
                0;

            phaseProgress =
                0f;
        }

        public void CancelWash()
        {
            if (!IsActive)
                return;

            IsActive =
                false;

            phase =
                0;

            phaseProgress =
                0f;

            car?.SetDrivingEnabled(
                true);

            activities?.End(
                ActivityId);

            StatusText =
                MotorCityLocalization.Text(
                    "carwash.cancelled");
        }

        private void OnDisable()
        {
            if (IsActive)
                CancelWash();
        }

        private static Vector3 Flat(
            Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
