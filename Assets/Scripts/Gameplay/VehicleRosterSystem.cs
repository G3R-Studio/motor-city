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
                : VehicleIds.Beatall;

        public event Action VehicleChanged;

        // Fired after the selected runtime visual has been installed/activated
        // and vehicle-dependent visual systems have been pointed at it.
        // Visual consumers (customization, presentation effects) should prefer
        // this over VehicleChanged, which also represents gameplay selection.
        public event Action VehicleVisualReady;

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

        public bool SelectedVehicleUnlocked =>
            Valid(
                SelectedIndex) &&
            HasVisual(
                SelectedIndex) &&
            IsUnlocked(
                SelectedIndex);

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
                    // Each car has a gameplay identity, not just a different mesh.
                    // The reputation ladder makes the garage itself part of progression.
                    new VehicleProfile(
                    VehicleIds.Beatall,
                    MotorCityLocalization.Text("vehicle.beatall.name"),
                    "MotorCity/Vehicles/Player/Beatall",
                    0,
                    135,
                    6,
                    0.98f,
                    -0.01f,
                    0.86f,
                    1.09f,
                    1.10f,
                    0.92f,
                    1.10f,
                    MotorCityLocalization.Text("vehicle.beatall.desc")),

                    new VehicleProfile(
                    VehicleIds.Street,
                    MotorCityLocalization.Text("vehicle.street.name"),
                    "MotorCity/PlayerCarVisual",
                    250,
                    155,
                    7,
                    1.00f,
                    0.00f,
                    1.00f,
                    1.00f,
                    1.00f,
                    1.00f,
                    1.00f,
                    MotorCityLocalization.Text("vehicle.street.desc")),

                    new VehicleProfile(
                    VehicleIds.Peugeot306,
                    MotorCityLocalization.Text("vehicle.peugeot306.name"),
                    "MotorCity/Vehicles/Player/Peugeot306",
                    700,
                    150,
                    7,
                    1.06f,
                    0.02f,
                    0.90f,
                    1.10f,
                    1.12f,
                    0.96f,
                    0.96f,
                    MotorCityLocalization.Text("vehicle.peugeot306.desc")),

                    new VehicleProfile(
                    VehicleIds.ToyotaAE86,
                    MotorCityLocalization.Text("vehicle.toyotaae86.name"),
                    "MotorCity/Vehicles/Player/ToyotaAE86",
                    1300,
                    155,
                    7,
                    0.95f,
                    -0.02f,
                    0.86f,
                    1.14f,
                    1.06f,
                    1.00f,
                    1.20f,
                    MotorCityLocalization.Text("vehicle.toyotaae86.desc")),

                    new VehicleProfile(
                    VehicleIds.Hybrid,
                    MotorCityLocalization.Text("vehicle.hybrid.name"),
                    "MotorCity/Vehicles/Player/Hybrid",
                    2100,
                    175,
                    9,
                    1.10f,
                    0.05f,
                    0.94f,
                    1.08f,
                    1.14f,
                    1.08f,
                    0.90f,
                    MotorCityLocalization.Text("vehicle.hybrid.desc")),

                    new VehicleProfile(
                    VehicleIds.Porsche996,
                    MotorCityLocalization.Text("vehicle.porsche996.name"),
                    "MotorCity/Vehicles/Player/Porsche996",
                    3100,
                    190,
                    9,
                    1.14f,
                    0.06f,
                    0.96f,
                    1.12f,
                    1.18f,
                    1.08f,
                    0.96f,
                    MotorCityLocalization.Text("vehicle.porsche996.desc")),

                    new VehicleProfile(
                    VehicleIds.AmgGT,
                    MotorCityLocalization.Text("vehicle.amggt.name"),
                    "MotorCity/Vehicles/Player/AmgGT",
                    4300,
                    195,
                    9,
                    1.10f,
                    0.09f,
                    1.08f,
                    1.02f,
                    1.20f,
                    1.12f,
                    0.92f,
                    MotorCityLocalization.Text("vehicle.amggt.desc")),

                    new VehicleProfile(
                    VehicleIds.Camaro,
                    MotorCityLocalization.Text("vehicle.camaro.name"),
                    "MotorCity/Vehicles/Player/Camaro",
                    5700,
                    180,
                    10,
                    1.01f,
                    0.07f,
                    1.15f,
                    0.92f,
                    1.10f,
                    1.18f,
                    1.12f,
                    MotorCityLocalization.Text("vehicle.camaro.desc")),

                    new VehicleProfile(
                    VehicleIds.Delorean,
                    MotorCityLocalization.Text("vehicle.delorean.name"),
                    "MotorCity/Vehicles/Player/Delorean",
                    0,
                    185,
                    9,
                    1.08f,
                    0.07f,
                    1.00f,
                    1.07f,
                    1.15f,
                    1.10f,
                    1.02f,
                    MotorCityLocalization.Text("vehicle.delorean.desc")),

                    new VehicleProfile(
                    VehicleIds.Bus,
                    MotorCityLocalization.Text("vehicle.bus.name"),
                    "MotorCity/Vehicles/Player/Bus",
                    7500,
                    105,
                    4,
                    1.04f,
                    0.12f,
                    1.26f,
                    0.82f,
                    1.30f,
                    1.10f,
                    0.72f,
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
                    VehicleIds.Street,
                    VehicleIds.Hybrid,
                    VehicleIds.Beatall,
                    VehicleIds.Delorean,
                    VehicleIds.AmgGT,
                    VehicleIds.Porsche996,
                    VehicleIds.Peugeot306,
                    VehicleIds.ToyotaAE86,
                    VehicleIds.Camaro,
                    VehicleIds.Bus
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
                   profiles[candidate].Id == VehicleIds.Delorean &&
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
                    string.Empty;

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
                if (profile.Id == VehicleIds.Delorean)
                {
                    status =
                        MotorCityLocalization.Format(
                            "vehicle.supporter_required",
                            profile.DisplayName);
                }
                else
                {
                    int currentRep =
                        reputation == null
                            ? 0
                            : reputation.Reputation;

                    int remaining =
                        Mathf.Max(
                            0,
                            profile.RequiredRep -
                            currentRep);

                    status =
                        MotorCityLocalization.Format(
                            "vehicle.rep_required_detailed",
                            profile.DisplayName,
                            currentRep,
                            profile.RequiredRep,
                            remaining);
                }

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
                string.Empty;

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
                string.Empty;

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

        public bool TryGetNextReputationVehicle(
            out string vehicleName,
            out int requiredRep,
            out int remainingRep)
        {
            vehicleName =
                string.Empty;

            requiredRep =
                0;

            remainingRep =
                0;

            if (profiles == null ||
                reputation == null)
            {
                return false;
            }

            int currentRep =
                reputation.Reputation;

            int bestIndex =
                -1;

            int bestRequirement =
                int.MaxValue;

            for (int i = 0;
                 i < profiles.Length;
                 i++)
            {
                VehicleProfile profile =
                    profiles[i];

                if (profile == null ||
                    profile.Id ==
                        VehicleIds.Delorean ||
                    !HasVisual(
                        i) ||
                    IsUnlocked(
                        i) ||
                    profile.RequiredRep <=
                        currentRep)
                {
                    continue;
                }

                if (profile.RequiredRep >=
                    bestRequirement)
                {
                    continue;
                }

                bestRequirement =
                    profile.RequiredRep;

                bestIndex =
                    i;
            }

            if (!Valid(
                    bestIndex))
            {
                return false;
            }

            VehicleProfile target =
                profiles[bestIndex];

            vehicleName =
                target.DisplayName;

            requiredRep =
                target.RequiredRep;

            remainingRep =
                Mathf.Max(
                    0,
                    requiredRep -
                    currentRep);

            return
                remainingRep > 0;
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
                if (nextProfile.Id == VehicleIds.Delorean)
                {
                    return
                        MotorCityLocalization.Format(
                            "vehicle.next_supporter",
                            nextProfile.DisplayName);
                }

                int currentRep =
                    reputation == null
                        ? 0
                        : reputation.Reputation;

                int remaining =
                    Mathf.Max(
                        0,
                        nextProfile.RequiredRep -
                        currentRep);

                return
                    MotorCityLocalization.Format(
                        "vehicle.next_rep_detailed",
                        nextProfile.DisplayName,
                        currentRep,
                        nextProfile.RequiredRep,
                        remaining);
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

        // Overall progress for the garage's level / 10 display. MasteryProgress
        // remains the XP progress within the current level for other consumers.
        public float MasteryCompletion => Mathf.Clamp01(masteryLevel / 10f);

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
                profile.TopSpeedKph +
                " " +
                MotorCityLocalization.Text(
                    "common.kmh").ToLowerInvariant();

            string accel =
                profile.AccelerationTune.ToString();

            int grip =
                Mathf.RoundToInt(
                    (profile.GripMultiplier - 1f) *
                    100f);

            int stability =
                Mathf.RoundToInt(
                    profile.StabilityBonus *
                    100f);

            int steering =
                Mathf.RoundToInt(
                    (profile.SteeringMultiplier - 1f) *
                    100f);

            int drift =
                Mathf.RoundToInt(
                    (profile.DriftMultiplier - 1f) *
                    100f);

            int mass =
                Mathf.RoundToInt(
                    (profile.MassMultiplier - 1f) *
                    100f);

            return
                MotorCityLocalization.Format(
                    "vehicle.stats",
                    speed,
                    accel,
                    Signed(grip),
                    Signed(stability),
                    Signed(steering),
                    Signed(drift),
                    Signed(mass));
        }

        public string GetCharacterLine()
        {
            if (!Valid(SelectedIndex))
                return string.Empty;

            return profiles[SelectedIndex].Character;
        }

        private void ApplySelectedVehicle()
        {
            if (car == null ||
                !Valid(SelectedIndex))
                return;

            VehicleProfile profile =
                profiles[SelectedIndex];

            bool preserveAuthoredTransform =
                profile.Id == VehicleIds.Hybrid ||
                profile.Id == VehicleIds.Beatall ||
                profile.Id == VehicleIds.Delorean ||
                profile.Id == VehicleIds.AmgGT ||
                profile.Id == VehicleIds.Porsche996 ||
                profile.Id == VehicleIds.Peugeot306 ||
                profile.Id == VehicleIds.ToyotaAE86 ||
                profile.Id == VehicleIds.Camaro ||
                profile.Id == VehicleIds.Bus;

            if (profile.Id == VehicleIds.Hybrid)
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
            else if (profile.Id == VehicleIds.Beatall)
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
            else if (profile.Id == VehicleIds.Delorean)
            {
                car.ApplySuspensionPreset(
                    0.08f,
                    43000f,
                    7000f,
                    0.46f,
                    0.24f);
            }
            else if (profile.Id == VehicleIds.AmgGT)
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
            else if (profile.Id == VehicleIds.Porsche996)
            {
                car.ApplySuspensionPreset(
                    0.09f,
                    44500f,
                    7300f,
                    0.46f,
                    0.24f);
            }
            else if (profile.Id == VehicleIds.Peugeot306)
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
            else if (profile.Id == VehicleIds.ToyotaAE86)
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
            else if (profile.Id == VehicleIds.Camaro)
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
            else if (profile.Id == VehicleIds.Bus)
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
                profile.Id == VehicleIds.Hybrid
                    ? 4.45f
                    : profile.Id == VehicleIds.Beatall
                        ? 4.485f
                        : profile.Id == VehicleIds.Delorean
                            ? 4.62f
                            : profile.Id == VehicleIds.AmgGT
                                ? 4.30f
                                : profile.Id == VehicleIds.Porsche996
                                    ? 4.20f
                                    : profile.Id == VehicleIds.Peugeot306
                                        ? 4.21f
                                        : profile.Id == VehicleIds.ToyotaAE86
                                            ? 4.31f
                                            : profile.Id == VehicleIds.Camaro
                                                ? 5.104f
                                                : profile.Id == VehicleIds.Bus
                                                    ? 7.9375f
                                                    : 4.35f;

            ArcadeRacingCarRuntimeInstaller.InstallVehicleVisual(
                car,
                profile.ResourcePath,
                false,
                targetLength,
                false,
                null,
                preserveAuthoredTransform,
                null,
                0f,
                true,
                profile.Id);

            car.ApplyVehicleProfile(
                profile.TopSpeedKph,
                profile.AccelerationTune,
                profile.GripMultiplier,
                profile.StabilityBonus,
                profile.MassMultiplier,
                profile.SteeringMultiplier,
                profile.BrakeMultiplier,
                profile.PowerMultiplier,
                profile.DriftMultiplier);

            // Drivetrain assignments are limited to the vehicles explicitly
            // identified in the physics audit. Other profiles retain the
            // existing AWD behavior until their drivetrain is authored.
            switch (profile.Id)
            {
                case VehicleIds.Peugeot306:
                    car.SetDriveTorqueDistribution(
                        2f,
                        0f);
                    break;

                case VehicleIds.ToyotaAE86:
                case VehicleIds.Camaro:
                case VehicleIds.Bus:
                    car.SetDriveTorqueDistribution(
                        0f,
                        2f);
                    break;

                default:
                    car.SetDriveTorqueDistribution(
                        1f,
                        1f);
                    break;
            }

            HybridCoordinateLights hybridLights =
                car.GetComponent<HybridCoordinateLights>();
            if (hybridLights == null)
                hybridLights = car.gameObject.AddComponent<HybridCoordinateLights>();
            hybridLights.SetVehicleId(profile.Id);

            DeloreanAuthoredLights deloreanLights =
                car.GetComponent<DeloreanAuthoredLights>();
            if (deloreanLights == null)
                deloreanLights = car.gameObject.AddComponent<DeloreanAuthoredLights>();
            deloreanLights.SetVehicleId(profile.Id);

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

            VehicleVisualReady?.Invoke();
        }

        private bool IsUnlocked(
            int index)
        {
            if (!Valid(index))
                return false;

            VehicleProfile profile =
                profiles[index];

            if (profile.Id == VehicleIds.Delorean)
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
            public readonly int TopSpeedKph;
            public readonly int AccelerationTune;
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
                int topSpeedKph,
                int accelerationTune,
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
                TopSpeedKph =
                    topSpeedKph;
                AccelerationTune =
                    accelerationTune;
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
