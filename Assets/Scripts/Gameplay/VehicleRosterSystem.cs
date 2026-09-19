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

        public string SelectedName =>
            Valid(SelectedIndex)
                ? profiles[SelectedIndex].DisplayName
                : "STREET";

        public int SelectedRequiredRep =>
            Valid(SelectedIndex)
                ? profiles[SelectedIndex].RequiredRep
                : 0;

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
                        "STREET",
                        "MotorCity/PlayerCarVisual",
                        0,
                        0,
                        0,
                        1f,
                        0f),

                    new VehicleProfile(
                        "club",
                        "CLUB",
                        "MotorCity/Vehicles/Vehicle_01",
                        500,
                        -6,
                        1,
                        1.04f,
                        0.02f),

                    new VehicleProfile(
                        "muscle",
                        "MUSCLE",
                        "MotorCity/Vehicles/Vehicle_02",
                        1200,
                        12,
                        2,
                        0.97f,
                        -0.01f),

                    new VehicleProfile(
                        "gt",
                        "GT",
                        "MotorCity/Vehicles/Vehicle_03",
                        2200,
                        24,
                        2,
                        1.03f,
                        0.035f),

                    new VehicleProfile(
                        "apex",
                        "APEX",
                        "MotorCity/Vehicles/Vehicle_04",
                        3500,
                        38,
                        3,
                        1.08f,
                        0.055f)
                };

            int stored =
                Mathf.Clamp(
                    PlayerPrefs.GetInt(
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
                    $"{profile.DisplayName}: модель ещё не подготовлена из Vehicles - PolyPack";

                return false;
            }

            if (!IsUnlocked(candidate))
            {
                status =
                    $"{profile.DisplayName}: нужно {profile.RequiredRep:N0} REP";

                return false;
            }

            SelectedIndex =
                candidate;

            PlayerPrefs.SetInt(
                SelectedKey,
                SelectedIndex);

            PlayerPrefs.Save();

            ApplySelectedVehicle();

            status =
                $"Выбрана машина {profile.DisplayName}";

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
                        $"   •   ДАЛЬШЕ: {nextProfile.DisplayName} — {nextProfile.RequiredRep:N0} REP";
                }
            }

            return
                $"МАШИНА {SelectedIndex + 1}/{profiles.Length}: " +
                current.DisplayName +
                nextText;
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
                $"СЦЕП {Signed(grip)}%   •   СТАБ {Signed(stability)}";
        }

        private void ApplySelectedVehicle()
        {
            if (car == null ||
                !Valid(SelectedIndex))
                return;

            VehicleProfile profile =
                profiles[SelectedIndex];

            car.SetDrivingEnabled(
                false);

            bool installed =
                ArcadeRacingCarRuntimeInstaller.InstallVehicleVisual(
                    car,
                    profile.ResourcePath);

            if (!installed &&
                SelectedIndex != 0)
            {
                SelectedIndex = 0;

                PlayerPrefs.SetInt(
                    SelectedKey,
                    0);

                PlayerPrefs.Save();

                profile =
                    profiles[0];

                ArcadeRacingCarRuntimeInstaller.InstallVehicleVisual(
                    car,
                    profile.ResourcePath);
            }

            car.ApplyVehicleProfile(
                profile.SpeedBonus,
                profile.AccelerationBonus,
                profile.GripMultiplier,
                profile.StabilityBonus);

            car.SetDrivingEnabled(
                true);
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

            public VehicleProfile(
                string id,
                string displayName,
                string resourcePath,
                int requiredRep,
                int speedBonus,
                int accelerationBonus,
                float gripMultiplier,
                float stabilityBonus)
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
            }
        }
    }
}
