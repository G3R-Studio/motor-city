using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Run only in a disposable CI checkout. Back up authored prefabs before invoking
    /// the real importer Build(false) entry points, and restore them even on failure.
    /// </summary>
    public static class MotorCityImporterRebuildGate
    {
        private static readonly string[] Names =
        {
            "Beatall", "Peugeot306", "ToyotaAE86", "Hybrid",
            "Porsche996", "AmgGT", "Camaro", "Delorean", "Bus"
        };

        public static void Validate()
        {
            string root = "Assets/Resources/MotorCity/Vehicles/Player/";
            var backups = new Dictionary<string, byte[]>();
            var results = new List<string>();
            try
            {
                foreach (string name in Names)
                {
                    string path = root + name + ".prefab";
                    if (!File.Exists(path))
                        throw new InvalidOperationException("Missing prefab: " + path);
                    backups[path] = File.ReadAllBytes(path);
                }

                foreach (string name in Names)
                {
                    Type importer = typeof(HybridVehicleImporter).Assembly.GetType(name + "VehicleImporter");
                    MethodInfo build = importer == null ? null : importer.GetMethod(
                        "Build", BindingFlags.Static | BindingFlags.NonPublic,
                        null, new[] { typeof(bool) }, null);
                    if (build == null)
                        throw new InvalidOperationException(name + ": no importer Build(bool) method");

                    object result;
                    try
                    {
                        result = build.Invoke(null, new object[] { false });
                    }
                    catch (TargetInvocationException exception)
                    {
                        throw new InvalidOperationException(
                            name + ": importer rebuild threw an exception", exception.InnerException);
                    }

                    if (!(result is bool) || !(bool)result)
                        throw new InvalidOperationException(name + ": importer rebuild returned failure");

                    string path = root + name + ".prefab";
                    GameObject generated = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (generated == null)
                        throw new InvalidOperationException(name + ": rebuilt prefab was not loadable");

                    results.Add(name + ": importer Build(false) succeeded and prefab loaded");
                }
            }
            finally
            {
                foreach (var pair in backups)
                    File.WriteAllBytes(pair.Key, pair.Value);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            string folder = Path.GetFullPath("Temp/MotorCityAudit/UnityPhase2");
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "vehicle-importer-rebuild.txt"), results);
            Debug.Log("Motor City importer rebuild CI gate passed: " + results.Count);
        }
    }
}
