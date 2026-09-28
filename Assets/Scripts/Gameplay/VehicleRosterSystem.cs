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
                : "beatall";

        public event Action VehicleChanged;

        private int masteryLevel = 1;
        private int masteryXp;
        private int masteryLevelStartXp;
        private int masteryNextXp = 100;

        public string SelectedName =>
            Valid(SelectedIndex)
                ? profiles[SelectedIndex].DisplayName
                : MotorCityLocalization.Text("vehicle.beatall.name");

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
            // Normal vehicles are reputation/progression based; supporter
            // exclusives use their permanent purchase entitlement.
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

                    new VehicleProfile(
                    "peugeot306",
                    MotorCityLocalization.Text("vehicle.peugeot306.name"),
                    "MotorCity/Vehicles/Player/Peugeot306",
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
                    MotorCityLocalization.Text("vehicle.peugeot306.desc")),

                    new VehicleProfile(
                    "toyotaae86",
                    MotorCityLocalization.Text("vehicle.toyotaae86.name"),
                    "MotorCity/Vehicles/Player/ToyotaAE86",
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
                    MotorCityLocalization.Text("vehicle.toyotaae86.desc")),

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

                    new VehicleProfile(
                    "porsche996",
                    MotorCityLocalization.Text("vehicle.porsche996.name"),
                    "MotorCity/Vehicles/Player/Porsche996",
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
                    MotorCityLocalization.Text("vehicle.porsche996.desc")),

                    new VehicleProfile(
                    "amggt",
                    MotorCityLocalization.Text("vehicle.amggt.name"),
                    "MotorCity/Vehicles/Player/AmgGT",
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
                    MotorCityLocalization.Text("vehicle.amggt.desc")),

                    new VehicleProfile(
                    "camaro",
                    MotorCityLocalization.Text("vehicle.camaro.name"),
                    "MotorCity/Vehicles/Player/Camaro",
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
                    MotorCityLocalization.Text("vehicle.camaro.desc")),

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
                    MotorCityLocalization.Text("vehicle.delorean.desc")),

                    new VehicleProfile(
                    "bus",
                    MotorCityLocalization.Text("vehicle.bus.name"),
                    "MotorCity/Vehicles/Player/Bus",
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
                    MotorCityLocalization.Text("vehicle.bus.desc"))
                };

            int stored =
                Mathf.Clamp(
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        SelectedKey,
                        0),
                    0,
                    profiles.Length - 1);

            const string rosterOrderVersionKey =
                "MotorCity.Vehicle.RosterOrderVersion";

            if (MotorCity.Persistence.MotorCitySaveService.GetInt(
                    rosterOrderVersionKey,
                    0) < 2)
            {
                string[] oldOrder =
                {
                    "street",
                    "hybrid",
                    "beatall",
                    "delorean",
                    "amggt",
                    "porsche996",
                    "peugeot306",
                    "toyotaae86",
                    "camaro",
                    "bus"
                };

                int oldIndex =
                    Mathf.Clamp(
                        MotorCity.Persistence.MotorCitySaveService.GetInt(
                            SelectedKey,
                            0),
                        0,
                        oldOrder.Length - 1);

                string oldId =
                    oldOrder[oldIndex];

                for (int i = 0;
                     i < profiles.Length;
                     i++)
                {
                    if (profiles[i].Id != oldId)
                        continue;

                    stored = i;
                    break;
                }

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    SelectedKey,
                    stored);

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    rosterOrderVersionKey,
                    2);

                MotorCity.Persistence.MotorCitySaveService.Save();
            }

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

            while (Valid(candidate) &&
                   profiles[candidate].Id == "delorean" &&
                   !IsUnlocked(candidate))
            {
                int skipped =
                    candidate + direction;

                if (!Valid(skipped))
                    break;

                candidate =
                    skipped;
            }

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
                    profile.Id == "delorean"
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
                    nextProfile.Id == "delorean"
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
                profile.Id == "delorean" ||
                profile.Id == "amggt" ||
                profile.Id == "porsche996" ||
                profile.Id == "peugeot306" ||
                profile.Id == "toyotaae86" ||
                profile.Id == "camaro" ||
                profile.Id == "bus";

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
            else if (profile.Id == "amggt")
            {
                // Low modern GT coupe: short travel, firm spring and controlled
                // damping so the body stays planted without looking lifted.
                car.ApplySuspensionPreset(
                    0.08f,
                    46000f,
                    7600f,
                    0.46f,
                    0.22f);
            }
            else if (profile.Id == "porsche996")
            {
                car.ApplySuspensionPreset(
                    0.09f,
                    44500f,
                    7300f,
                    0.46f,
                    0.24f);
            }
            else if (profile.Id == "peugeot306")
            {
                // Taller compact hatchback: a little more travel and softer
                // damping than the low coupes, while keeping the body planted.
                car.ApplySuspensionPreset(
                    0.12f,
                    40500f,
                    6800f,
                    0.48f,
                    0.28f);
            }
            else if (profile.Id == "toyotaae86")
            {
                // Lightweight classic coupe: compact travel with a slightly
                // freer rear-biased feel while staying stable in normal driving.
                car.ApplySuspensionPreset(
                    0.10f,
                    39500f,
                    6500f,
                    0.47f,
                    0.26f);
            }
            else if (profile.Id == "camaro")
            {
                // Wide modern muscle coupe: firm spring, short travel and
                // slightly heavier damping to keep the broad body controlled.
                car.ApplySuspensionPreset(
                    0.09f,
                    47500f,
                    7900f,
                    0.46f,
                    0.22f);
            }
            else if (profile.Id == "bus")
            {
                // Long city bus: more travel and damping for the tall body,
                // while keeping it stable enough for the shared player rig.
                car.ApplySuspensionPreset(
                    0.18f,
                    52000f,
                    9200f,
                    0.52f,
                    0.42f);
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
                        ? 4.485f
                        : profile.Id == "delorean"
                            ? 4.62f
                            : profile.Id == "amggt"
                                ? 4.30f
                                : profile.Id == "porsche996"
                                    ? 4.20f
                                    : profile.Id == "peugeot306"
                                        ? 4.21f
                                        : profile.Id == "toyotaae86"
                                            ? 4.31f
                                            : profile.Id == "camaro"
                                                ? 5.104f
                                                : profile.Id == "bus"
                                                    ? 7.9375f
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

            PlayerVehicleAudio vehicleAudio =
                car.GetComponent<PlayerVehicleAudio>();

            if (vehicleAudio != null)
                vehicleAudio.SetVehicleId(profile.Id);
        }

        private bool IsUnlocked(
            int index)
        {
            if (!Valid(index))
                return false;

            VehicleProfile profile =
                profiles[index];

            if (profile.Id == "delorean")
            {
                return
                    CosmeticStoreSystem.SupporterPackOwned;
            }

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
