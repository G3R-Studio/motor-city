using UnityEditor;
using UnityEngine;

public static class PolyPackVehicleImporter
{
    [MenuItem("Motor City/Vehicles/Validate Curated Garage Cars")]
    private static void ValidateCuratedGarageCars()
    {
        string[] paths =
        {
            "Assets/Resources/MotorCity/Vehicles/Vehicle_01.prefab",
            "Assets/Resources/MotorCity/Vehicles/Vehicle_02.prefab",
            "Assets/Resources/MotorCity/Vehicles/Vehicle_03.prefab",
            "Assets/Resources/MotorCity/Vehicles/Vehicle_04.prefab"
        };

        int valid = 0;

        foreach (string path in paths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                valid++;
            }
            else
            {
                Debug.LogWarning(
                    "Motor City: missing curated garage vehicle: " +
                    path);
            }
        }

        Debug.Log(
            $"Motor City: curated garage vehicles available: {valid}/{paths.Length}. " +
            "The garage lineup is authored explicitly and is not auto-rebuilt.");
    }
}
