using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Read-only city prefab audit. Reports authored material holes before
    /// considering removal of the production runtime fallback.
    /// </summary>
    public static class MotorCityPhase9CityMaterialAudit
    {
        public static void Validate(string folder)
        {
            const string path =
                "Assets/Resources/MotorCity/Environment/CityVisual.prefab";
            GameObject city = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (city == null)
                throw new InvalidOperationException("Missing city prefab: " + path);

            int renderers = 0;
            int missingSlots = 0;
            int plantRenderers = 0;
            int missingPlantSlots = 0;
            foreach (Renderer renderer in city.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                renderers++;
                string name = renderer.gameObject.name;
                bool plant = string.Equals(name, "Plant-01", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "Plant-01 (1)", StringComparison.OrdinalIgnoreCase);
                if (plant)
                    plantRenderers++;

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    missingSlots++;
                    if (plant) missingPlantSlots++;
                    continue;
                }

                foreach (Material material in materials)
                {
                    if (material != null) continue;
                    missingSlots++;
                    if (plant) missingPlantSlots++;
                }
            }

            int unexpectedMissingSlots = missingSlots - missingPlantSlots;
            if (unexpectedMissingSlots < 0)
                throw new InvalidOperationException("Invalid city material audit counters.");

            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "unity-phase9-city-materials.txt"),
                "Read-only CityVisual.prefab material audit\n" +
                "Renderer count: " + renderers + "\n" +
                "Missing material slots: " + missingSlots + "\n" +
                "Plant-01 renderers: " + plantRenderers + "\n" +
                "Plant-01 missing material slots: " + missingPlantSlots + "\n" +
                "Unexpected non-plant missing material slots: " + unexpectedMissingSlots + "\n" +
                "Runtime fallback remains until authored dependencies are verified.\n");
            Debug.Log("Motor City Phase 9 city prefab audit: renderers=" + renderers +
                ", missingSlots=" + missingSlots +
                ", plantRenderers=" + plantRenderers +
                ", plantMissingSlots=" + missingPlantSlots +
                ", unexpectedMissingSlots=" + unexpectedMissingSlots);
            if (unexpectedMissingSlots != 0)
                throw new InvalidOperationException(
                    "CityVisual.prefab has " + unexpectedMissingSlots +
                    " missing material slots outside the known Plant-01 fallback.");
        }
    }
}
