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
                activityManager.ActivityResultShown +=
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
                activityManager.ActivityResultShown -=
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
                "street" =>
                    activityId == "delivery"
                        ? 35
                        : 0,

                "club" =>
                    activityId == "sprint"
                        ? 35
                        : activityId == "delivery"
                            ? 10
                            : 0,

                "muscle" =>
                    activityId == "drift"
                        ? 40
                        : 0,

                "gt" =>
                    activityId == "circuit"
                        ? 35
                        : activityId == "sprint"
                            ? 15
                            : 0,

                "apex" =>
                    IsCoreActivity(
                        activityId)
                        ? 15
                        : 0,

                _ => 0
            };
        }

        private void HandleActivityResult(
            string activityId,
            bool success)
        {
            if (!success ||
                activityManager == null ||
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
                "street" =>
                    MotorCityLocalization.Text("specialization.courier"),

                "club" =>
                    MotorCityLocalization.Text("specialization.sprint"),

                "muscle" =>
                    MotorCityLocalization.Text("specialization.drift"),

                "gt" =>
                    MotorCityLocalization.Text("specialization.circuit"),

                "apex" =>
                    MotorCityLocalization.Text("specialization.allrounder"),

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
                "street" =>
                    MotorCityLocalization.Text("specialization.courier_desc"),

                "club" =>
                    MotorCityLocalization.Text("specialization.sprint_desc"),

                "muscle" =>
                    MotorCityLocalization.Text("specialization.drift_desc"),

                "gt" =>
                    MotorCityLocalization.Text("specialization.circuit_desc"),

                "apex" =>
                    MotorCityLocalization.Text("specialization.allrounder_desc"),

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
