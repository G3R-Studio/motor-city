using System;
using MotorCity.Localization;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class VehicleRosterSystem : MonoBehaviour
    {
        private const string SelectedKey =
            "MotorCity.Vehicle.Selected";
        private const string OwnershipMigrationKey =
            "MotorCity.Vehicle.OwnershipMigrationV2";

        private const string SupporterPackKey =
            "MotorCity.Purchase.SupporterPack";

        private ArcadeCarController car;
        private PlayerReputation reputation;
        private VehicleProfile[] profiles;

        public int SelectedIndex { get; private set; }
        public int VehicleCount => profiles?.Length ?? 0;

        public string SelectedId =>
            Valid(SelectedIndex)
                ? profiles[SelectedIndex].Id
                : "tois08";

        public event Action VehicleChanged;

        private int masteryLevel = 1;
        private int masteryXp;
        private int masteryLevelStartXp;
        private int masteryNextXp = 100;

        public string SelectedName =>
            Valid(SelectedIndex)
                ? profiles[SelectedIndex].DisplayName
                : MotorCityLocalization.Text("vehicle.tois08.name");

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
            // Vehicle ownership is reputation-based now:
            // once unlocked, the vehicle is immediately available.
            return
                IsUnlocked(
                    index);
        }

        public int GetPurchasePrice(
            int index)
        {
            // Kept for compatibility with older callers.
            return 0;
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

        public bool HasNextVehicle =>
            Valid(
                SelectedIndex + 1) &&
            HasVisual(
                SelectedIndex + 1);

        public bool NextVehicleUnlocked =>
            HasNextVehicle &&
            IsUnlocked(
                SelectedIndex + 1);

        public bool NextVehicleOwned =>
            NextVehicleUnlocked;

        public int NextVehiclePrice =>
            0;

        public int NextVehicleRequiredRep =>
            HasNextVehicle
                ? profiles[
                    SelectedIndex + 1]
                    .RequiredRep
                : 0;

        public bool CanAffordNextVehicle =>
            NextVehicleUnlocked;

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
            _ = playerWallet;

            profiles =
                new[]
                {
                    new VehicleProfile(
                        "tois08",
                        MotorCityLocalization.Text("vehicle.tois08.name"),
                        "MotorCity/Vehicles/Player/Tois08GT",
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
                        MotorCityLocalization.Text("vehicle.tois08.desc")),

                    // Keep the existing authored street car completely intact.
                    new VehicleProfile(
                        "street",
                        MotorCityLocalization.Text("vehicle.street.name"),
                        "MotorCity/PlayerCarVisual",
                        500,
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
                        "toro86",
                        MotorCityLocalization.Text("vehicle.toro86.name"),
                        "MotorCity/Vehicles/Player/Toro86",
                        1200,
                        0,
                        8,
                        1,
                        0.98f,
                        0.015f,
                        0.98f,
                        1.03f,
                        1.02f,
                        1.05f,
                        1.08f,
                        MotorCityLocalization.Text("vehicle.toro86.desc")),

                    new VehicleProfile(
                        "stuttgart996",
                        MotorCityLocalization.Text("vehicle.stuttgart996.name"),
                        "MotorCity/Vehicles/Player/Stuttgart996",
                        2200,
                        0,
                        16,
                        2,
                        1.02f,
                        0.025f,
                        1.00f,
                        1.04f,
                        1.06f,
                        1.08f,
                        1.03f,
                        MotorCityLocalization.Text("vehicle.stuttgart996.desc")),

                    new VehicleProfile(
                        "muscle10",
                        MotorCityLocalization.Text("vehicle.muscle10.name"),
                        "MotorCity/Vehicles/Player/MuscleCar10",
                        3500,
                        0,
                        22,
                        2,
                        0.96f,
                        -0.005f,
                        1.12f,
                        0.95f,
                        0.98f,
                        1.16f,
                        1.18f,
                        MotorCityLocalization.Text("vehicle.muscle10.desc")),

                    new VehicleProfile(
                        "hybrid",
                        MotorCityLocalization.Text("vehicle.hybrid.name"),
                        "MotorCity/Vehicles/Player/Hybrid",
                        5000,
                        0,
                        28,
                        3,
                        1.05f,
                        0.035f,
                        0.94f,
                        1.07f,
                        1.10f,
                        1.12f,
                        0.94f,
                        MotorCityLocalization.Text("vehicle.hybrid.desc")),

                    new VehicleProfile(
                        "tristar",
                        MotorCityLocalization.Text("vehicle.tristar.name"),
                        "MotorCity/Vehicles/Player/TristarRacer",
                        7000,
                        0,
                        34,
                        3,
                        1.07f,
                        0.045f,
                        0.91f,
                        1.09f,
                        1.14f,
                        1.15f,
                        0.88f,
                        MotorCityLocalization.Text("vehicle.tristar.desc")),

                    new VehicleProfile(
                        "van",
                        MotorCityLocalization.Text("vehicle.van.name"),
                        "MotorCity/Vehicles/Player/Van",
                        9000,
                        0,
                        -8,
                        0,
                        0.92f,
                        0.03f,
                        1.28f,
                        0.84f,
                        1.14f,
                        0.94f,
                        0.82f,
                        MotorCityLocalization.Text("vehicle.van.desc")),

                    new VehicleProfile(
                        "doclorean",
                        MotorCityLocalization.Text("vehicle.doclorean.name"),
                        "MotorCity/Vehicles/Player/DocLorean",
                        0,
                        0,
                        30,
                        3,
                        1.04f,
                        0.04f,
                        0.96f,
                        1.06f,
                        1.08f,
                        1.12f,
                        0.94f,
                        MotorCityLocalization.Text("vehicle.doclorean.desc"),
                        true)

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
                    profile.SupporterOnly
                        ? MotorCityLocalization.Format(
                            "vehicle.supporter_required",
                            profile.DisplayName)
                        : MotorCityLocalization.Format(
                            "vehicle.rep_required",
                            profile.DisplayName,
                            profile.RequiredRep);

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

            return
                MotorCityLocalization.Format(
                    "vehicle.garage_current",
                    SelectedIndex + 1,
                    profiles.Length,
                    current.DisplayName);
        }

        public string GetNextVehicleLine()
        {
            int next =
                SelectedIndex + 1;

            if (!Valid(next))
            {
                return
                    MotorCityLocalization.Text(
                        "vehicle.next_none");
            }

            VehicleProfile nextProfile =
                profiles[next];

            if (!HasVisual(next))
            {
                return
                    MotorCityLocalization.Format(
                        "vehicle.next_missing",
                        nextProfile.DisplayName);
            }

            if (!IsUnlocked(next))
            {
                return
                    nextProfile.SupporterOnly
                        ? MotorCityLocalization.Format(
                            "vehicle.next_supporter",
                            nextProfile.DisplayName)
                        : MotorCityLocalization.Format(
                            "vehicle.next_rep",
                            nextProfile.DisplayName,
                            nextProfile.RequiredRep);
            }

            return
                MotorCityLocalization.Format(
                    "vehicle.next_available",
                    nextProfile.DisplayName);
        }

        public bool TryPurchaseNextVehicle(
            out string status)
        {
            // Legacy compatibility: vehicles are no longer purchased.
            // Selecting the next vehicle is enough once reputation unlocks it.
            return
                TrySelectOffset(
                    1,
                    out status);
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

        public float MasteryProgress
        {
            get
            {
                if (masteryLevel >= 10)
                    return 1f;

                int span =
                    Mathf.Max(
                        1,
                        masteryNextXp -
                        masteryLevelStartXp);

                return
                    Mathf.Clamp01(
                        (masteryXp -
                         masteryLevelStartXp) /
                        (float)span);
            }
        }

        public void SetMasteryDisplay(
            int level,
            int xp,
            int levelStartXp,
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

            masteryLevelStartXp =
                Mathf.Clamp(
                    levelStartXp,
                    0,
                    masteryXp);

            masteryNextXp =
                Mathf.Max(
                    masteryLevelStartXp + 1,
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

            float targetLength =
                profile.Id == "van"
                    ? 5.15f
                    : 4.35f;

            bool installed =
                ArcadeRacingCarRuntimeInstaller.InstallVehicleVisual(
                    car,
                    profile.ResourcePath,
                    false,
                    targetLength,
                    false);

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

            PlayerVehicleRearEmission rearEmission =
                car.GetComponent<PlayerVehicleRearEmission>();

            if (rearEmission != null)
            {
                rearEmission.SetVehicleId(profile.Id);
            }

            PlayerHeadlights headlights =
                car.GetComponent<PlayerHeadlights>();

            if (headlights != null)
            {
                headlights.SetVehicleId(profile.Id);
            }

        }

        private bool IsUnlocked(
            int index)
        {
            if (!Valid(index))
                return false;

            VehicleProfile profile =
                profiles[index];

            if (profile.SupporterOnly)
            {
                return
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        SupporterPackKey,
                        0) != 0;
            }

            int rep =
                reputation == null
                    ? 0
                    : reputation.Reputation;

            return rep >=
                profile.RequiredRep;
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
            public readonly bool SupporterOnly;

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
                string character,
                bool supporterOnly = false)
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
                SupporterOnly =
                    supporterOnly;
            }
        }
    }
}
