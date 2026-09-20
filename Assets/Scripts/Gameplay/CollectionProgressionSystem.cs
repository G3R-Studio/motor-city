using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CollectionProgressionSystem : MonoBehaviour
    {
        private const string ClaimedTierKey =
            "MotorCity.Collection.ClaimedTier";

        private const float RefreshSeconds = 0.75f;
        private const float MessageSeconds = 6f;

        private static readonly int[] Thresholds =
        {
            250,
            550,
            900,
            1350,
            1900
        };

        private static readonly int[] CreditRewards =
        {
            1200,
            2500,
            4500,
            7000,
            11000
        };

        private static readonly int[] ReputationRewards =
        {
            80,
            140,
            220,
            320,
            500
        };

        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private VehicleRosterSystem roster;
        private VehicleMasterySystem mastery;
        private VehicleHistorySystem history;

        private float refreshTimer;
        private float messageTimer;
        private int claimedTier;

        public int Rating { get; private set; }

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; } =
            string.Empty;

        public string HudLine
        {
            get
            {
                if (claimedTier >=
                    Thresholds.Length)
                {
                    return
                        $"КОЛЛЕКЦИЯ {Rating:N0} • ЛЕГЕНДАРНЫЙ ГАРАЖ";
                }

                return
                    $"КОЛЛЕКЦИЯ {Rating:N0}/{Thresholds[claimedTier]:N0} • " +
                    $"НАГРАДА {CreditRewards[claimedTier]:N0} КР";
            }
        }

        public string GarageLine =>
            $"КОЛЛЕКЦИОННЫЙ РЕЙТИНГ: {Rating:N0}   •   " +
            $"МАШИН {UnlockedVehicles()}/{VehicleCount()}   •   " +
            $"ЭТАП {Mathf.Min(claimedTier + 1, Thresholds.Length)}/{Thresholds.Length}";

        public void Initialize(
            PlayerWallet playerWallet,
            PlayerReputation playerReputation,
            VehicleRosterSystem vehicleRoster,
            VehicleMasterySystem masterySystem,
            VehicleHistorySystem historySystem)
        {
            wallet =
                playerWallet;

            reputation =
                playerReputation;

            roster =
                vehicleRoster;

            mastery =
                masterySystem;

            history =
                historySystem;

            claimedTier =
                Mathf.Clamp(
                    PlayerPrefs.GetInt(
                        ClaimedTierKey,
                        0),
                    0,
                    Thresholds.Length);

            Recalculate(
                false);

            if (roster != null)
            {
                roster.VehicleChanged +=
                    HandleVehicleChanged;
            }
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.deltaTime);
            }

            refreshTimer -=
                Time.deltaTime;

            if (refreshTimer > 0f)
                return;

            refreshTimer =
                RefreshSeconds;

            Recalculate(
                true);
        }

        private void OnDestroy()
        {
            if (roster != null)
            {
                roster.VehicleChanged -=
                    HandleVehicleChanged;
            }
        }

        public void RecalculateForTesting()
        {
            Recalculate(
                true);
        }

        public void ResetMilestonesForTesting()
        {
            claimedTier = 0;

            PlayerPrefs.SetInt(
                ClaimedTierKey,
                claimedTier);

            PlayerPrefs.Save();

            Recalculate(
                false);

            StatusText =
                "КОЛЛЕКЦИОННЫЕ НАГРАДЫ СБРОШЕНЫ";

            messageTimer =
                MessageSeconds;
        }

        public void ClaimAllForTesting()
        {
            Recalculate(
                false);

            while (claimedTier <
                       Thresholds.Length)
            {
                GrantTier(
                    claimedTier);

                claimedTier++;
            }

            SaveClaimedTier();
        }

        private void HandleVehicleChanged()
        {
            Recalculate(
                true);
        }

        private void Recalculate(
            bool allowRewards)
        {
            int score = 0;

            if (roster != null)
            {
                score +=
                    roster.GetUnlockedVehicleCount() *
                    120;

                for (int i = 0;
                     i < roster.VehicleCount;
                     i++)
                {
                    string vehicleId =
                        roster.GetVehicleId(
                            i);

                    if (string.IsNullOrEmpty(
                            vehicleId))
                    {
                        continue;
                    }

                    int masteryLevel =
                        mastery == null
                            ? 1
                            : mastery.GetLevelForVehicle(
                                vehicleId);

                    score +=
                        Mathf.Max(
                            0,
                            masteryLevel - 1) *
                        18;

                    int legacyTier =
                        history == null
                            ? 0
                            : history.GetLegacyTierForVehicle(
                                vehicleId);

                    score +=
                        legacyTier *
                        55;
                }
            }

            Rating =
                Mathf.Max(
                    0,
                    score);

            if (!allowRewards)
                return;

            while (claimedTier <
                       Thresholds.Length &&
                   Rating >=
                       Thresholds[claimedTier])
            {
                int tier =
                    claimedTier;

                GrantTier(
                    tier);

                claimedTier++;

                SaveClaimedTier();

                StatusText =
                    $"КОЛЛЕКЦИЯ — ЭТАП {claimedTier}/{Thresholds.Length}   " +
                    $"+{CreditRewards[tier]:N0} КР   " +
                    $"+{ReputationRewards[tier]:N0} REP";

                messageTimer =
                    MessageSeconds;
            }
        }

        private void GrantTier(
            int tier)
        {
            if (tier < 0 ||
                tier >=
                    Thresholds.Length)
            {
                return;
            }

            wallet?.AddCredits(
                CreditRewards[tier]);

            reputation?.AddReputation(
                ReputationRewards[tier]);
        }

        private void SaveClaimedTier()
        {
            PlayerPrefs.SetInt(
                ClaimedTierKey,
                claimedTier);

            PlayerPrefs.Save();
        }

        private int UnlockedVehicles()
        {
            return
                roster == null
                    ? 0
                    : roster.GetUnlockedVehicleCount();
        }

        private int VehicleCount()
        {
            return
                roster == null
                    ? 0
                    : roster.VehicleCount;
        }
    }
}
