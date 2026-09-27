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
        private ArcadeCarController car;
        private PlayerReputation reputation;
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
        private int masteryLevelStartXp;
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
            // Vehicle ownership is reputation-based now:
            // once unlocked, the vehicle is immediately available.
            return
                IsUnlocked(
                    index);
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
            PlayerReputation playerReputation)
        {
            car = targetCar;
            reputation = playerReputation;

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
                        1f,
                        0f,
                        1f,
                        1f,
                        1f,
                        1f,
                        1f,
                        MotorCityLocalization.Text("vehicle.street.desc")),

                    // HYBRID keeps the authored Gudamore visual hierarchy
                    // while sharing Motor City's player-car physics rig.
                    new VehicleProfile(
                        "hybrid",
                        MotorCityLocalization.Text("vehicle.hybrid.name"),
                        "MotorCity/Vehicles/Player/Hybrid",
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
                        MotorCityLocalization.Text("vehicle.hybrid.desc")),

                    // BEATALL is imported from the standalone OBJ source and
                    // rebuilt into a clean Resources prefab by BeatallVehicleImporter.
                    new VehicleProfile(
                        "beatall",
                        MotorCityLocalization.Text("vehicle.beatall.name"),
                        "MotorCity/Vehicles/Player/Beatall",
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
                        MotorCityLocalization.Text("vehicle.beatall.desc")),

                    new VehicleProfile(
                        "delorean",
                        MotorCityLocalization.Text("vehicle.delorean.name"),
                        "MotorCity/Vehicles/Player/Delorean",
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
                        MotorCityLocalization.Text("vehicle.delorean.desc"))
                };

            int stored =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        SelectedKey,
                        0),
                    0,
                    profiles.Length - 1);

            if (!IsUnlocked(stored) ||
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
                    MotorCityLocalization.Format(
                        "vehicle.next_rep",
                        nextProfile.DisplayName,
                        nextProfile.RequiredRep);
            }

            return
                MotorCityLocalization.Format(
                    "vehicle.next_available",
                    nextProfile.DisplayName);
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

            bool preserveAuthoredTransform =
                profile.Id == "hybrid" ||
                profile.Id == "beatall" ||
                profile.Id == "delorean";

            if (profile.Id == "hybrid")
            {
                // Hybrid is a compact Asset Store model, so scale it to a
                // normal city-car footprint and use a short, well-damped
                // suspension instead of the bouncy generic setup.
                car.ApplySuspensionPreset(
                    0.15f,
                    42000f,
                    7600f,
                    0.44f,
                    0.42f);
            }
            else if (profile.Id == "beatall")
            {
                // Beatall is a short classic hatchback. Keep the body planted
                // without giving it the taller generic STREET suspension.
                car.ApplySuspensionPreset(
                    0.06f,
                    42000f,
                    7200f,
                    0.46f,
                    0.18f);
            }
            else if (profile.Id == "delorean")
            {
                car.ApplySuspensionPreset(
                    0.08f,
                    43000f,
                    7000f,
                    0.46f,
                    0.24f);
            }
            else
            {
                car.ApplySuspensionPreset(
                    0.24f,
                    36000f,
                    4400f,
                    0.50f,
                    0.30f);
            }

            float targetLength =
                profile.Id == "hybrid"
                    ? 4.45f
                    : profile.Id == "beatall"
                        ? 3.45f
                        : profile.Id == "delorean"
                            ? 4.62f
                            : 4.35f;

            ArcadeRacingCarRuntimeInstaller.InstallVehicleVisual(
                car,
                profile.ResourcePath,
                false,
                targetLength,
                false,
                null,
                preserveAuthoredTransform);

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
                rearEmission.SetVehicleId(profile.Id);

            PlayerHeadlights headlights =
                car.GetComponent<PlayerHeadlights>();

            if (headlights != null)
                headlights.SetVehicleId(profile.Id);
        }

        private bool IsUnlocked(
            int index)
        {
            if (!Valid(index))
                return false;

            VehicleProfile profile =
                profiles[index];

            int rep =
                reputation == null
                    ? 0
                    : reputation.Reputation;

            return rep >=
                profile.RequiredRep;
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
