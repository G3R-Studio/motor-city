using System;
using MotorCity.Localization;
using MotorCity.Vehicle;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class VehicleRosterSystem : MonoBehaviour
    {
        private const string SelectedKey =
            "MotorCity.Vehicle.Selected";
        private const string OwnershipMigrationKey =
            "MotorCity.Vehicle.OwnershipMigrationV1";

        private ArcadeCarController car;
        private PlayerReputation reputation;
        private PlayerWallet wallet;
        private VehicleProfile[] profiles;

        public int SelectedIndex { get; private set; }
        public int VehicleCount => profiles?.Length ?? 0;

        public string SelectedId =>
            Valid(SelectedIndex)
                ? profiles[SelectedIndex].Id
                : "street";

        public event Action VehicleChanged;

        private int masteryLevel = 1;
        private int masteryXp;
        private int masteryNextXp = 100;

        public string SelectedName =>
            Valid(SelectedIndex)
                ? profiles[SelectedIndex].DisplayName
                : MotorCityLocalization.Text("vehicle.street.name");

        public int SelectedRequiredRep =>
            Valid(SelectedIndex)
                ? profiles[SelectedIndex].RequiredRep
                : 0;

        public int GetOwnedVehicleCount()
        {
            if (profiles == null)
                return 0;

            int count = 0;

            for (int i = 0;
                 i < profiles.Length;
                 i++)
            {
                if (IsOwned(i) &&
                    HasVisual(i))
                {
                    count++;
                }
            }

            return count;
        }

        public bool IsOwned(
            int index)
        {
            if (!Valid(index))
                return false;

            if (index == 0)
                return true;

            return
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    OwnershipKey(
                        profiles[index].Id),
                    0) != 0;
        }

        public int GetPurchasePrice(
            int index)
        {
            return
                Valid(index)
                    ? profiles[index].PurchasePrice
                    : 0;
        }

        public int GetUnlockedVehicleCount()
        {
            if (profiles == null)
                return 0;

            int count = 0;

            for (int i = 0;
                 i < profiles.Length;
                 i++)
            {
                if (IsUnlocked(i) &&
                    HasVisual(i))
                {
                    count++;
                }
            }

            return count;
        }

        public string GetVehicleId(
            int index)
        {
            return
                Valid(index)
                    ? profiles[index].Id
                    : string.Empty;
        }

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerReputation playerReputation,
            PlayerWallet playerWallet)
        {
            car = targetCar;
            reputation = playerReputation;
            wallet = playerWallet;

            profiles =
                new[]
                {
                    new VehicleProfile(
                        "street",
                        MotorCityLocalization.Text("vehicle.street.name"),
                        "MotorCity/PlayerCarVisual",
                        0,
                        0,
                        0,
                        0,
                        1f,
                        0f,
                        1f,
                        1f,
                        1f,
                        1f,
                        1f,
                        MotorCityLocalization.Text("vehicle.street.desc")),

                    new VehicleProfile(
                        "club",
                        MotorCityLocalization.Text("vehicle.club.name"),
                        "MotorCity/Vehicles/Vehicle_01",
                        500,
                        2800,
                        -6,
                        1,
                        1.04f,
                        0.02f,
                        0.92f,
                        1.10f,
                        1.05f,
                        0.98f,
                        0.92f,
                        MotorCityLocalization.Text("vehicle.club.desc")),

                    new VehicleProfile(
                        "muscle",
                        MotorCityLocalization.Text("vehicle.muscle.name"),
                        "MotorCity/Vehicles/Vehicle_02",
                        1200,
                        6500,
                        12,
                        2,
                        0.97f,
                        -0.01f,
                        1.10f,
                        0.94f,
                        0.96f,
                        1.14f,
                        1.18f,
                        MotorCityLocalization.Text("vehicle.muscle.desc")),

                    new VehicleProfile(
                        "gt",
                        MotorCityLocalization.Text("vehicle.gt.name"),
                        "MotorCity/Vehicles/Vehicle_03",
                        2200,
                        11500,
                        24,
                        2,
                        1.03f,
                        0.035f,
                        0.98f,
                        1.03f,
                        1.12f,
                        1.08f,
                        0.90f,
                        MotorCityLocalization.Text("vehicle.gt.desc")),

                    new VehicleProfile(
                        "apex",
                        MotorCityLocalization.Text("vehicle.apex.name"),
                        "MotorCity/Vehicles/Vehicle_04",
                        3500,
                        18000,
                        38,
                        3,
                        1.08f,
                        0.055f,
                        0.90f,
                        1.08f,
                        1.18f,
                        1.16f,
                        0.82f,
                        MotorCityLocalization.Text("vehicle.apex.desc"))
                };

            MigrateLegacyOwnership();

            int stored =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        SelectedKey,
                        0),
                    0,
                    profiles.Length - 1);

            if (!IsUnlocked(stored) ||
                !IsOwned(stored) ||
                !HasVisual(stored))
            {
                stored = 0;
            }

            SelectedIndex =
                stored;

            ApplySelectedVehicle();
        }

        public bool TrySelectOffset(
            int offset,
            out string status)
        {
            status = string.Empty;

            if (profiles == null ||
                profiles.Length == 0)
                return false;

            int direction =
                offset < 0
                    ? -1
                    : 1;

            int candidate =
                Mathf.Clamp(
                    SelectedIndex + direction,
                    0,
                    profiles.Length - 1);

            if (candidate == SelectedIndex)
            {
                status =
                    direction < 0
                        ? MotorCityLocalization.Text("vehicle.first")
                        : MotorCityLocalization.Text("vehicle.last");

                return false;
            }

            VehicleProfile profile =
                profiles[candidate];

            if (!HasVisual(candidate))
            {
                status =
                    MotorCityLocalization.Format(
                        "vehicle.visual_missing",
                        profile.DisplayName);

                return false;
            }

            if (!IsUnlocked(candidate))
            {
                status =
                    MotorCityLocalization.Format(
                        "vehicle.rep_required",
                        profile.DisplayName,
                        profile.RequiredRep);

                return false;
            }

            if (!IsOwned(candidate))
            {
                status =
                    MotorCityLocalization.Format(
                        "vehicle.not_owned",
                        profile.DisplayName,
                        profile.PurchasePrice);

                return false;
            }

            SelectedIndex =
                candidate;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                SelectedKey,
                SelectedIndex);

            MotorCity.Persistence.MotorCitySaveService.Save();

            ApplySelectedVehicle();

            VehicleChanged?.Invoke();

            status =
                MotorCityLocalization.Format(
                    "vehicle.selected",
                    profile.DisplayName);

            return true;
        }

        public bool SelectVehicleForTesting(
            int index,
            out string status)
        {
            status =
                string.Empty;

            if (!Valid(index))
            {
                status =
                    MotorCityLocalization.Text(
                        "vehicle.invalid");
                return false;
            }

            if (!HasVisual(index))
            {
                status =
                    MotorCityLocalization.Format(
                        "vehicle.visual_missing",
                        profiles[index].DisplayName);
                return false;
            }

            SelectedIndex =
                index;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                SelectedKey,
                SelectedIndex);

            MotorCity.Persistence.MotorCitySaveService.Save();

            ApplySelectedVehicle();
            VehicleChanged?.Invoke();

            status =
                MotorCityLocalization.Format(
                    "vehicle.selected",
                    profiles[SelectedIndex].DisplayName);

            return true;
        }

        public string GetGarageLine()
        {
            if (!Valid(SelectedIndex))
                return string.Empty;

            VehicleProfile current =
                profiles[SelectedIndex];

            string nextText =
                string.Empty;

            int next =
                SelectedIndex + 1;

            if (Valid(next))
            {
                VehicleProfile nextProfile =
                    profiles[next];

                if (!HasVisual(next))
                {
                    nextText =
                        MotorCityLocalization.Format(
                            "vehicle.next_missing",
                            nextProfile.DisplayName);
                }
                else if (!IsUnlocked(next))
                {
                    nextText =
                        MotorCityLocalization.Format(
                            "vehicle.next_rep",
                            nextProfile.DisplayName,
                            nextProfile.RequiredRep);
                }
                else if (!IsOwned(next))
                {
                    nextText =
                        MotorCityLocalization.Format(
                            "vehicle.next_buy",
                            nextProfile.DisplayName,
                            nextProfile.PurchasePrice);
                }
                else
                {
                    nextText =
                        MotorCityLocalization.Format(
                            "vehicle.next_available",
                            nextProfile.DisplayName);
                }
            }

            return
                MotorCityLocalization.Format(
                    "vehicle.garage_line",
                    SelectedIndex + 1,
                    profiles.Length,
                    current.DisplayName,
                    masteryLevel,
                    nextText);
        }

        public bool TryPurchaseNextVehicle(
            out string status)
        {
            status =
                string.Empty;

            int candidate =
                SelectedIndex + 1;

            if (!Valid(candidate) ||
                !HasVisual(candidate))
            {
                status =
                    MotorCityLocalization.Text(
                        "vehicle.buy_none");
                return false;
            }

            VehicleProfile profile =
                profiles[candidate];

            if (IsOwned(candidate))
            {
                status =
                    MotorCityLocalization.Format(
                        "vehicle.next_available",
                        profile.DisplayName);
                return false;
            }

            if (!IsUnlocked(candidate))
            {
                status =
                    MotorCityLocalization.Format(
                        "vehicle.buy_rep",
                        profile.DisplayName,
                        profile.RequiredRep);
                return false;
            }

            if (wallet == null ||
                !wallet.TrySpendCredits(
                    profile.PurchasePrice))
            {
                status =
                    MotorCityLocalization.Format(
                        "vehicle.buy_credits",
                        profile.DisplayName,
                        profile.PurchasePrice);
                return false;
            }

            SetOwned(
                candidate,
                true);

            SelectedIndex =
                candidate;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                SelectedKey,
                SelectedIndex);

            MotorCity.Persistence.MotorCitySaveService.Save();

            ApplySelectedVehicle();
            VehicleChanged?.Invoke();

            status =
                MotorCityLocalization.Format(
                    "vehicle.purchased",
                    profile.DisplayName,
                    profile.PurchasePrice);

            return true;
        }

        public void SetAllOwnedForTesting(
            bool owned)
        {
            if (profiles == null)
                return;

            for (int i = 1;
                 i < profiles.Length;
                 i++)
            {
                SetOwned(
                    i,
                    owned);
            }

            MotorCity.Persistence.MotorCitySaveService.Save();

            if (!IsOwned(SelectedIndex))
            {
                SelectedIndex = 0;
                ApplySelectedVehicle();
                VehicleChanged?.Invoke();
            }
        }

        public string GetMasteryLine()
        {
            if (masteryLevel >= 10)
            {
                return
                    MotorCityLocalization.Format(
                        "vehicle.mastery_max",
                        masteryXp);
            }

            return
                MotorCityLocalization.Format(
                    "vehicle.mastery",
                    masteryLevel,
                    masteryXp,
                    masteryNextXp);
        }

        public string GetMasteryShort()
        {
            return
                MotorCityLocalization.Format(
                    "vehicle.mastery_short",
                    masteryLevel);
        }

        public void SetMasteryDisplay(
            int level,
            int xp,
            int nextXp)
        {
            masteryLevel =
                Mathf.Clamp(
                    level,
                    1,
                    10);

            masteryXp =
                Mathf.Max(
                    0,
                    xp);

            masteryNextXp =
                Mathf.Max(
                    masteryXp,
                    nextXp);
        }

        public string GetStatsLine()
        {
            if (!Valid(SelectedIndex))
                return string.Empty;

            VehicleProfile profile =
                profiles[SelectedIndex];

            string speed =
                Signed(
                    profile.SpeedBonus) +
                " " +
                MotorCityLocalization.Text(
                    "common.kmh").ToLowerInvariant();

            string accel =
                Signed(
                    profile.AccelerationBonus);

            int grip =
                Mathf.RoundToInt(
                    (profile.GripMultiplier - 1f) *
                    100f);

            int stability =
                Mathf.RoundToInt(
                    profile.StabilityBonus *
                    100f);

            return
                MotorCityLocalization.Format(
                    "vehicle.stats",
                    speed,
                    accel,
                    Signed(grip),
                    Signed(stability),
                    profile.Character);
        }

        private void ApplySelectedVehicle()
        {
            if (car == null ||
                !Valid(SelectedIndex))
                return;

            VehicleProfile profile =
                profiles[SelectedIndex];

            bool installed =
                ArcadeRacingCarRuntimeInstaller.InstallVehicleVisual(
                    car,
                    profile.ResourcePath,
                    SelectedIndex != 0);

            if (!installed &&
                SelectedIndex != 0)
            {
                SelectedIndex = 0;

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    SelectedKey,
                    0);

                MotorCity.Persistence.MotorCitySaveService.Save();

                profile =
                    profiles[0];

                ArcadeRacingCarRuntimeInstaller.InstallVehicleVisual(
                    car,
                    profile.ResourcePath,
                    false);
            }

            car.ApplyVehicleProfile(
                profile.SpeedBonus,
                profile.AccelerationBonus,
                profile.GripMultiplier,
                profile.StabilityBonus,
                profile.MassMultiplier,
                profile.SteeringMultiplier,
                profile.BrakeMultiplier,
                profile.PowerMultiplier,
                profile.DriftMultiplier);

        }

        private bool IsUnlocked(
            int index)
        {
            if (!Valid(index))
                return false;

            int rep =
                reputation == null
                    ? 0
                    : reputation.Reputation;

            return rep >=
                profiles[index].RequiredRep;
        }

        private void MigrateLegacyOwnership()
        {
            int migrated =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    OwnershipMigrationKey,
                    0);

            if (migrated != 0)
                return;

            for (int i = 0;
                 i < profiles.Length;
                 i++)
            {
                if (IsUnlocked(i) &&
                    HasVisual(i))
                {
                    SetOwned(
                        i,
                        true);
                }
            }

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                OwnershipMigrationKey,
                1);

            MotorCity.Persistence.MotorCitySaveService.Save();
        }

        private void SetOwned(
            int index,
            bool owned)
        {
            if (!Valid(index) ||
                index == 0)
                return;

            MotorCity.Persistence.MotorCitySaveService.SetInt(
                OwnershipKey(
                    profiles[index].Id),
                owned ? 1 : 0);
        }

        private static string OwnershipKey(
            string vehicleId)
        {
            return
                "MotorCity.Vehicle.Owned." +
                vehicleId;
        }

        private bool HasVisual(
            int index)
        {
            if (!Valid(index))
                return false;

            return Resources.Load<GameObject>(
                       profiles[index].ResourcePath) !=
                   null;
        }

        private bool Valid(
            int index)
        {
            return profiles != null &&
                index >= 0 &&
                index < profiles.Length;
        }

        private static string Signed(
            int value)
        {
            return value > 0
                ? $"+{value}"
                : value.ToString();
        }

        private sealed class VehicleProfile
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string ResourcePath;
            public readonly int RequiredRep;
            public readonly int PurchasePrice;
            public readonly int SpeedBonus;
            public readonly int AccelerationBonus;
            public readonly float GripMultiplier;
            public readonly float StabilityBonus;
            public readonly float MassMultiplier;
            public readonly float SteeringMultiplier;
            public readonly float BrakeMultiplier;
            public readonly float PowerMultiplier;
            public readonly float DriftMultiplier;
            public readonly string Character;

            public VehicleProfile(
                string id,
                string displayName,
                string resourcePath,
                int requiredRep,
                int purchasePrice,
                int speedBonus,
                int accelerationBonus,
                float gripMultiplier,
                float stabilityBonus,
                float massMultiplier,
                float steeringMultiplier,
                float brakeMultiplier,
                float powerMultiplier,
                float driftMultiplier,
                string character)
            {
                Id = id;
                DisplayName = displayName;
                ResourcePath = resourcePath;
                RequiredRep = requiredRep;
                PurchasePrice =
                    Mathf.Max(
                        0,
                        purchasePrice);
                SpeedBonus = speedBonus;
                AccelerationBonus =
                    accelerationBonus;
                GripMultiplier =
                    gripMultiplier;
                StabilityBonus =
                    stabilityBonus;
                MassMultiplier =
                    massMultiplier;
                SteeringMultiplier =
                    steeringMultiplier;
                BrakeMultiplier =
                    brakeMultiplier;
                PowerMultiplier =
                    powerMultiplier;
                DriftMultiplier =
                    driftMultiplier;
                Character =
                    character;
            }
        }
    }
}
