using System;
using System.Collections.Generic;
using System.IO;
using MotorCity.Vehicle;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>Read-only inventory of material roles; never rewrites materials or prefabs.</summary>
    public static class MotorCityVehicleMaterialRoleAudit
    {
        private static readonly string[] Vehicles =
        {
            "Beatall", "Peugeot306", "ToyotaAE86", "Hybrid",
            "Porsche996", "AmgGT", "Camaro", "Delorean", "Bus"
        };

        public static void Validate()
        {
            var lines = new List<string>
            {
                "Vehicle material role inventory. An unassigned role is not an error.",
                "Explicit VehicleVisualRoles takes precedence; unmatched imported assets remain unclassified."
            };

            foreach (string vehicle in Vehicles)
            {
                string path = "Assets/Resources/MotorCity/Vehicles/Player/" + vehicle + ".prefab";
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null)
                        throw new InvalidOperationException("Missing visual: " + path);
                    int renderers = 0;
                    int explicitRoles = 0;
                    int unassigned = 0;
                    foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer == null)
                            continue;
                        renderers++;
                        VehicleVisualRoles authored = renderer.GetComponent<VehicleVisualRoles>();
                        if (authored != null && authored.Roles != VehicleMaterialRole.None)
                            explicitRoles++;
                        else
                            unassigned++;
                    }
                    lines.Add(vehicle + ": renderers=" + renderers +
                        ", explicitly tagged=" + explicitRoles +
                        ", unassigned=" + unassigned);
                    if (renderers < 4)
                        throw new InvalidOperationException(vehicle + ": missing visual renderers");
                }
                finally
                {
                    if (root != null)
                        PrefabUtility.UnloadPrefabContents(root);
                }
            }

            string reportDir = Path.GetFullPath("Temp/MotorCityAudit/UnityPhase2");
            Directory.CreateDirectory(reportDir);
            File.WriteAllLines(Path.Combine(reportDir, "vehicle-material-roles.txt"), lines);
            Debug.Log("Motor City material-role audit passed: " + Vehicles.Length + " vehicle prefabs.");
        }
    }
}
