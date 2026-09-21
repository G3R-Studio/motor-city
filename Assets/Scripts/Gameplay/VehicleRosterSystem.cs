using System;
using MotorCity.Vehicle;
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
        private int masteryNextXp = 100;

        public string SelectedName =>
            Valid(SelectedIndex)
                ? profiles[SelectedIndex].DisplayName
                : "УЛИЧНАЯ";

        public int SelectedRequiredRep =>
            Valid(SelectedIndex)
                ? profiles[SelectedIndex].RequiredRep
                : 0;

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
            PlayerReputation playerReputation)
        {
            car = targetCar;
            reputation = playerReputation;

            profiles =
                new[]
                {
                    new VehicleProfile(
                        "street",
                        "УЛИЧНАЯ",
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
                        "СБАЛАНСИРОВАННАЯ — универсальная городская машина"),

                    new VehicleProfile(
                        "club",
                        "КЛУБНАЯ",
                        "MotorCity/Vehicles/Vehicle_01",
                        500,
                        -6,
                        1,
                        1.04f,
                        0.02f,
                        0.92f,
                        1.10f,
                        1.05f,
                        0.98f,
                        0.92f,
                        "ЛЁГКАЯ — резкий руль и удобство в городе"),

                    new VehicleProfile(
                        "muscle",
                        "МАСЛКАР",
                        "MotorCity/Vehicles/Vehicle_02",
                        1200,
                        12,
                        2,
                        0.97f,
                        -0.01f,
                        1.10f,
                        0.94f,
                        0.96f,
                        1.14f,
                        1.18f,
                        "СИЛОВАЯ — мощный разгон и естественный дрифт"),

                    new VehicleProfile(
                        "gt",
                        "ГРАН-ТУРИЗМО",
                        "MotorCity/Vehicles/Vehicle_03",
                        2200,
                        24,
                        2,
                        1.03f,
                        0.035f,
                        0.98f,
                        1.03f,
                        1.12f,
                        1.08f,
                        0.90f,
                        "ТРЕКОВАЯ — тормоза, скорость и устойчивость"),

                    new VehicleProfile(
                        "apex",
                        "АПЕКС",
                        "MotorCity/Vehicles/Vehicle_04",
                        3500,
                        38,
                        3,
                        1.08f,
                        0.055f,
                        0.90f,
                        1.08f,
                        1.18f,
                        1.16f,
                        0.82f,
                        "ЭЛИТНАЯ — максимум темпа и точности")
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
                        ? "Это первая машина в гараже"
                        : "Это последняя машина в гараже";

                return false;
            }

            VehicleProfile profile =
                profiles[candidate];

            if (!HasVisual(candidate))
            {
                status =
                    $"{profile.DisplayName}: модель ещё не подготовлена из пакета машин";

                return false;
            }

            if (!IsUnlocked(candidate))
            {
                status =
                    $"{profile.DisplayName}: нужно {profile.RequiredRep:N0} РЕП";

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
                $"Выбрана машина {profile.DisplayName}";

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
                    "Некорректный индекс машины";
                return false;
            }

            if (!HasVisual(index))
            {
                status =
                    $"{profiles[index].DisplayName}: модель не подготовлена";
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
                $"АДМИН: выбрана {profiles[SelectedIndex].DisplayName}";

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
                        $"   •   ДАЛЬШЕ: {nextProfile.DisplayName} — модель не подготовлена";
                }
                else if (IsUnlocked(next))
                {
                    nextText =
                        $"   •   ДАЛЬШЕ: {nextProfile.DisplayName} — ДОСТУПНА";
                }
                else
                {
                    nextText =
                        $"   •   ДАЛЬШЕ: {nextProfile.DisplayName} — {nextProfile.RequiredRep:N0} РЕП";
                }
            }

            return
                $"МАШИНА {SelectedIndex + 1}/{profiles.Length}: " +
                current.DisplayName +
                $"   •   МАСТЕРСТВО {masteryLevel}/10" +
                nextText;
        }

        public string GetMasteryLine()
        {
            if (masteryLevel >= 10)
            {
                return
                    $"МАСТЕРСТВО: УР. 10/10   •   {masteryXp:N0} ОПЫТ   •   МАКСИМУМ";
            }

            return
                $"МАСТЕРСТВО: УР. {masteryLevel}/10   •   " +
                $"{masteryXp:N0}/{masteryNextXp:N0} ОПЫТ";
        }

        public string GetMasteryShort()
        {
            return
                $"МАСТ {masteryLevel}/10";
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
                " км/ч";

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
                $"БАЗА: СКОРОСТЬ {speed}   •   РАЗГОН {accel}   •   " +
                $"СЦЕП {Signed(grip)}%   •   СТАБ {Signed(stability)}   •   " +
                profile.Character;
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
