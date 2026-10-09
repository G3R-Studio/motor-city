using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>Read-only Phase 3 validation of committed player visual prefabs.</summary>
    public static class MotorCityVehicleContractGate
    {
        private static readonly string[] VehicleNames =
        {
            "Beatall", "Peugeot306", "ToyotaAE86", "Hybrid",
            "Porsche996", "AmgGT", "Camaro", "Delorean", "Bus"
        };

        private static readonly string[] WheelNames =
        {
            "front_left", "front_right", "rear_left", "rear_right"
        };

        public static void Validate()
        {
            string directory = "Assets/Resources/MotorCity/Vehicles/Player/";
            var lines = new List<string>();
            foreach (string vehicle in VehicleNames)
            {
                string path = directory + vehicle + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    throw new InvalidOperationException("Missing vehicle prefab: " + path);

                GameObject instance = null;
                try
                {
                    instance = PrefabUtility.LoadPrefabContents(path);
                    Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
                    foreach (string wheelName in WheelNames)
                    {
                        Transform found = null;
                        int count = 0;
                        foreach (Transform transform in transforms)
                        {
                            if (transform.name != wheelName)
                                continue;
                            found = transform;
                            count++;
                        }

                        if (count != 1)
                            throw new InvalidOperationException(
                                vehicle + ": expected one " + wheelName + ", found " + count);
                        Vector3 position = found.localPosition;
                        Quaternion rotation = found.localRotation;
                        if (!Finite(position.x) || !Finite(position.y) || !Finite(position.z) ||
                            !Finite(rotation.x) || !Finite(rotation.y) ||
                            !Finite(rotation.z) || !Finite(rotation.w))
                            throw new InvalidOperationException(vehicle + ": invalid wheel pose " + wheelName);
                        if (found.lossyScale.sqrMagnitude < 0.000001f)
                            throw new InvalidOperationException(vehicle + ": zero wheel scale " + wheelName);
                        if (found.childCount == 0)
                            throw new InvalidOperationException(vehicle + ": wheel has no visual child " + wheelName);
                        if (found.GetComponentsInChildren<Renderer>(true).Length == 0)
                            throw new InvalidOperationException(vehicle + ": wheel has no renderer " + wheelName);
                    }
                    if (instance.GetComponentsInChildren<Renderer>(true).Length < 4)
                        throw new InvalidOperationException(vehicle + ": visual prefab has fewer than four renderers");
                    lines.Add(vehicle + ": wheel names, finite poses, visual children and renderers validated");
                }
                finally
                {
                    if (instance != null)
                        PrefabUtility.UnloadPrefabContents(instance);
                }
            }

            string folder = Path.GetFullPath("Temp/MotorCityAudit/UnityPhase2");
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "vehicle-contract.txt"), lines);
            Debug.Log("Motor City Phase 3 vehicle contract gate passed: " + lines.Count + " prefabs.");
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
