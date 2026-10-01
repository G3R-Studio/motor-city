using MotorCity.Localization;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class VehicleSpecializationSystem : MonoBehaviour
    {
        private const float MessageSeconds = 4.5f;

        private ActivityManager activityManager;
        private PlayerWallet wallet;
        private VehicleRosterSystem roster;
        private VehicleMasterySystem mastery;

        private float messageTimer;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; } =
            string.Empty;

        public string GarageLine =>
            roster == null
                ? string.Empty
                : MotorCityLocalization.Format(
                    "specialization.garage_line",
                    CurrentRoleName(),
                    CurrentRoleDescription());

        public string GarageCompactLine =>
            roster == null
                ? string.Empty
                : MotorCityLocalization.Format(
                    "specialization.garage_compact",
                    CurrentRoleName(),
                    CurrentRoleDescription());

        public string HudShort =>
            roster == null
                ? string.Empty
                : CurrentRoleName();

        public void Initialize(
            ActivityManager manager,
            PlayerWallet playerWallet,
            VehicleRosterSystem vehicleRoster,
            VehicleMasterySystem masterySystem)
        {
            activityManager =
                manager;

            wallet =
                playerWallet;

            roster =
                vehicleRoster;

            mastery =
                masterySystem;

            if (activityManager != null)
            {
                activityManager.ActivityCompleted +=
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

        private void OnDestroy()
        {
            if (activityManager != null)
            {
                activityManager.ActivityCompleted -=
                    HandleActivityResult;
            }
        }

        public int GetBonusPercent(
            string activityId)
        {
            if (roster == null)
                return 0;

            return roster.SelectedId switch
            {
                "beatall" =>
                    activityId == "drift"
                        ? 25
                        : 0,

                "street" =>
                    IsCoreActivity(
                        activityId)
                        ? 10
                        : 0,

                "peugeot306" =>
                    activityId == "delivery"
                        ? 35
                        : 0,

                "toyotaae86" =>
                    activityId == "drift"
                        ? 40
                        : 0,

                "hybrid" =>
                    activityId == "sprint"
                        ? 25
                        : activityId == "circuit"
                            ? 20
                            : 0,

                "porsche996" =>
                    activityId == "circuit"
                        ? 35
                        : activityId == "sprint"
                            ? 15
                            : 0,

                "amggt" =>
                    activityId == "sprint"
                        ? 25
                        : activityId == "circuit"
                            ? 20
                            : 0,

                "camaro" =>
                    activityId == "sprint" ||
                    activityId == "drift"
                        ? 25
                        : 0,

                "delorean" =>
                    IsCoreActivity(
                        activityId)
                        ? 15
                        : 0,

                "bus" =>
                    activityId == "delivery"
                        ? 40
                        : 0,

                _ => 0
            };
        }

        private void HandleActivityResult(
            string activityId)
        {
            if (activityManager == null ||
                !activityManager.SecondaryProgressionAllowed ||
                roster == null)
            {
                return;
            }

            int percent =
                GetBonusPercent(
                    activityId);

            if (percent <= 0)
                return;

            int baseCredits =
                Mathf.Max(
                    0,
                    activityManager.ResultRewardCredits);

            int bonusCredits =
                Mathf.RoundToInt(
                    baseCredits *
                    percent /
                    100f);

            int bonusMasteryXp =
                ResolveBonusMasteryXp(
                    activityId,
                    percent);

            wallet?.AddCredits(
                bonusCredits);

            mastery?.AddBonusXp(
                bonusMasteryXp);

            StatusText =
                MotorCityLocalization.Format(
                    "specialization.reward",
                    roster.SelectedName,
                    CurrentRoleName(),
                    bonusCredits,
                    bonusMasteryXp);

            messageTimer =
                MessageSeconds;
        }

        private string CurrentRoleName()
        {
            if (roster == null)
                return string.Empty;

            return roster.SelectedId switch
            {
                "beatall" =>
                    MotorCityLocalization.Text("specialization.light_drift"),

                "street" =>
                    MotorCityLocalization.Text("specialization.default"),

                "peugeot306" =>
                    MotorCityLocalization.Text("specialization.courier"),

                "toyotaae86" =>
                    MotorCityLocalization.Text("specialization.drift"),

                "hybrid" =>
                    MotorCityLocalization.Text("specialization.tech_sport"),

                "porsche996" =>
                    MotorCityLocalization.Text("specialization.circuit"),

                "amggt" =>
                    MotorCityLocalization.Text("specialization.grand_tourer"),

                "camaro" =>
                    MotorCityLocalization.Text("specialization.muscle"),

                "delorean" =>
                    MotorCityLocalization.Text("specialization.allrounder"),

                "bus" =>
                    MotorCityLocalization.Text("specialization.heavy_courier"),

                _ =>
                    MotorCityLocalization.Text("specialization.default")
            };
        }

        private string CurrentRoleDescription()
        {
            if (roster == null)
                return string.Empty;

            return roster.SelectedId switch
            {
                "beatall" =>
                    MotorCityLocalization.Text("specialization.light_drift_desc"),

                "street" =>
                    MotorCityLocalization.Text("specialization.default_desc"),

                "peugeot306" =>
                    MotorCityLocalization.Text("specialization.courier_desc"),

                "toyotaae86" =>
                    MotorCityLocalization.Text("specialization.drift_desc"),

                "hybrid" =>
                    MotorCityLocalization.Text("specialization.tech_sport_desc"),

                "porsche996" =>
                    MotorCityLocalization.Text("specialization.circuit_desc"),

                "amggt" =>
                    MotorCityLocalization.Text("specialization.grand_tourer_desc"),

                "camaro" =>
                    MotorCityLocalization.Text("specialization.muscle_desc"),

                "delorean" =>
                    MotorCityLocalization.Text("specialization.allrounder_desc"),

                "bus" =>
                    MotorCityLocalization.Text("specialization.heavy_courier_desc"),

                _ =>
                    MotorCityLocalization.Text("specialization.none_desc")
            };
        }

        private static int ResolveBonusMasteryXp(
            string activityId,
            int percent)
        {
            int baseXp =
                activityId switch
                {
                    "delivery" => 65,
                    "drift" => 80,
                    "sprint" => 90,
                    "circuit" => 110,
                    _ => 0
                };

            return
                Mathf.Max(
                    0,
                    Mathf.RoundToInt(
                        baseXp *
                        percent /
                        200f));
        }

        private static bool IsCoreActivity(
            string activityId)
        {
            return
                activityId == "delivery" ||
                activityId == "drift" ||
                activityId == "sprint" ||
                activityId == "circuit";
        }
    }
}
