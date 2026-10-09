using System;
using UnityEngine;

namespace MotorCity.Vehicle
{
    // Explicit artist-authored role takes precedence over imported name heuristics.
    [Flags]
    public enum VehicleMaterialRole
    {
        None = 0,
        Body = 1,
        Glass = 2,
        Mirror = 4,
        FrontLamp = 8,
        RearLamp = 16,
        Rim = 32,
        Rubber = 64
    }

    [DisallowMultipleComponent]
    public sealed class VehicleVisualRoles : MonoBehaviour
    {
        [SerializeField] private VehicleMaterialRole roles;
        public VehicleMaterialRole Roles => roles;

        [SerializeField] private VehicleMaterialRole[] slotRoles;

        public VehicleMaterialRole RolesAt(int slot)
        {
            return slotRoles != null && slot >= 0 && slot < slotRoles.Length
                ? slotRoles[slot] : roles;
        }

        public bool Has(VehicleMaterialRole role)
        {
            return (roles & role) == role;
        }
    }
}
