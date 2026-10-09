using UnityEngine;

namespace MotorCity.Vehicle
{
    /// <summary>
    /// Conservative manifest of unambiguous authored OBJ material roles.
    /// Unknown or multi-purpose materials intentionally remain unclassified.
    /// Never changes the materials themselves.
    /// </summary>
    public static class VehicleMaterialRoleCatalog
    {
        /// <summary>
        /// Annotate only unambiguous authored material slots on a runtime clone.
        /// This neither edits shared materials nor writes prefab assets.
        /// Existing artist-assigned roles always win.
        /// </summary>
        public static int AnnotateRuntimeVisual(GameObject root)
        {
            if (root == null)
                return 0;

            int assignedSlots = 0;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                VehicleVisualRoles authored = renderer.GetComponent<VehicleVisualRoles>();
                if (authored != null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                var roles = new VehicleMaterialRole[materials.Length];
                bool any = false;
                for (int i = 0; i < materials.Length; ++i)
                {
                    roles[i] = materials[i] == null
                        ? VehicleMaterialRole.None
                        : Resolve(materials[i].name);
                    if (roles[i] == VehicleMaterialRole.None)
                        continue;

                    any = true;
                    assignedSlots++;
                }
                if (!any)
                    continue;

                VehicleVisualRoles component = renderer.gameObject.AddComponent<VehicleVisualRoles>();
                component.AssignRuntimeSlots(roles);
            }
            return assignedSlots;
        }

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
