#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    [InitializeOnLoad]
    public static class PlayerVehicleAssetInstaller
    {
        private const string OutputRoot =
            "Assets/Resources/MotorCity/Vehicles/Player";

        private static readonly VehicleAsset[] Assets =
        {
            new("Assets/Vehicles/Imported/Designersoup_CarPack2/Exports/Tois08_GT.fbx", "Tois08GT"),
            new("Assets/Vehicles/Imported/Designersoup_CarPack2/Exports/Toro86.fbx", "Toro86"),
            new("Assets/Vehicles/Imported/Designersoup_CarPack2/Exports/Stuttgart996.fbx", "Stuttgart996"),
            new("Assets/Vehicles/Imported/Fbx_MuscleCar/Fbx/N_Muscle Car_10.fbx", "MuscleCar10"),
            new("Assets/Gudamore/Free Sports Car/Prefabs/Mesh Only/Sports Car.prefab", "Hybrid"),
            new("Assets/Vehicles/Imported/Designersoup_CarPack1/Tristar Racer.fbx", "TristarRacer"),
            new("Assets/Vehicles/Imported/CityTransport/Van.fbx", "Van"),
            new("Assets/Vehicles/Imported/Designersoup_CarPack1/docLorean.fbx", "DocLorean")
        };

        static PlayerVehicleAssetInstaller()
        {
            EditorApplication.delayCall += EnsureInstalled;
        }

        [MenuItem("Motor City/Vehicles/Rebuild Imported Player Vehicles")]
        public static void RebuildAll()
        {
            EnsureFolder();

            foreach (VehicleAsset asset in Assets)
            {
                string outputPath = OutputPath(asset);

                if (AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
                    AssetDatabase.DeleteAsset(outputPath);

                Build(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MotorCity][Vehicles] Imported player vehicle prefabs rebuilt.");
        }

        private static void EnsureInstalled()
        {
            EnsureFolder();
            bool changed = false;

            foreach (VehicleAsset asset in Assets)
            {
                string outputPath = OutputPath(asset);

                if (AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
                    continue;

                changed |= Build(asset);
            }

            if (!changed)
                return;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MotorCity][Vehicles] Imported player vehicle prefabs generated under Resources.");
        }

        private static bool Build(VehicleAsset asset)
        {
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(asset.SourcePath);

            if (source == null)
            {
                Debug.LogWarning(
                    "[MotorCity][Vehicles] Source asset was not found: " +
                    asset.SourcePath);
                return false;
            }

            GameObject instance =
                Object.Instantiate(source);

            instance.name = asset.OutputName;

            try
            {
                StripEditorOnlyObjects(instance.transform);

                string outputPath = OutputPath(asset);
                GameObject result =
                    PrefabUtility.SaveAsPrefabAsset(instance, outputPath);

                if (result == null)
                {
                    Debug.LogError(
                        "[MotorCity][Vehicles] Failed to create prefab: " +
                        outputPath);
                    return false;
                }

                Debug.Log(
                    "[MotorCity][Vehicles] Prepared " +
                    asset.OutputName +
                    " from " +
                    asset.SourcePath);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void StripEditorOnlyObjects(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);

                if (child == null)
                    continue;

                if (child.CompareTag("EditorOnly"))
                {
                    Object.DestroyImmediate(child.gameObject);
                    continue;
                }

                StripEditorOnlyObjects(child);
            }
        }

        private static void EnsureFolder()
        {
            string[] parts = OutputRoot.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private static string OutputPath(VehicleAsset asset)
        {
            return OutputRoot + "/" + asset.OutputName + ".prefab";
        }

        private readonly struct VehicleAsset
        {
            public readonly string SourcePath;
            public readonly string OutputName;

            public VehicleAsset(string sourcePath, string outputName)
            {
                SourcePath = sourcePath;
                OutputName = outputName;
            }
        }
    }
}
#endif
