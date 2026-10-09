namespace MotorCity.Vehicle
{
    /// <summary>
    /// Conservative manifest of unambiguous authored OBJ material roles.
    /// Unknown or multi-purpose materials intentionally remain unclassified.
    /// Never changes the materials themselves.
    /// </summary>
    public static class VehicleMaterialRoleCatalog
    {
        public static VehicleMaterialRole Resolve(string sourceName)
        {
            string name = VehicleVisualRoleUtility.NormalizeVisualName(sourceName);
            if (name.EndsWith("_urp"))
                name = name.Substring(0, name.Length - 4);
            switch (name)
            {
                case "carpaint":
                case "carpaint_002":
                    return VehicleMaterialRole.Body;
                case "blackglass":
                case "blackglass_001":
                case "blackglass_002":
                case "blackglass_003":
                case "blackglass_004":
                case "blackglass_008":
                    return VehicleMaterialRole.Glass;
                case "mc_headlight":
                case "mc_headlight_l":
                case "mc_headlight_r":
                case "headlights":
                case "headlights_001":
                    return VehicleMaterialRole.FrontLamp;
                case "mc_brake":
                case "mc_brake_l":
                case "mc_brake_r":
                case "rearlights":
                case "rearlights_001":
                case "rearlights_002":
                    return VehicleMaterialRole.RearLamp;
                default:
                    return VehicleMaterialRole.None;
            }
        }
    }
}
