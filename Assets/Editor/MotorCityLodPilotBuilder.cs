using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    public static class MotorCityLodPilotBuilder
    {
        private const string Source = "Assets/Fantastic City Generator/Buildings/FBX/";
        private const string Folder = "Assets/Resources/MotorCityLodPilot";
        [MenuItem("Tools/Motor City/Audit/Generate Building LOD Pilot")]
        private static void Generate()
        {
            var specs = new (string name, int vertices, int submeshes)[]
            {
                ("BR-003", 13061, 3),
                ("EC-009", 12969, 3),
                ("BR-002", 11586, 3),
                ("QD-14", 11580, 3),
                ("BR-014", 11099, 3),
                ("BR-027", 11048, 3),
                ("BC-P07", 10765, 4),
                ("BR-025", 10037, 3),
                ("BC-053", 9810, 5),
                ("EC-006", 9974, 3)
            };
            var report = new StringBuilder("MOTOR CITY BUILDING LOD PILOT\n");
            int generated = 0;
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Resources", "MotorCityLodPilot");
            foreach (var spec in specs)
            {
                string path = Folder + "/" + spec.name + ".asset";
                try
                {
                    var meshes = AssetDatabase.LoadAllAssetsAtPath(Source + spec.name + ".fbx")
                        .OfType<Mesh>().Where(m => m.name == spec.name &&
                            m.vertexCount == spec.vertices && m.subMeshCount == spec.submeshes &&
                            m.isReadable).ToArray();
                    if (meshes.Length != 1) { report.AppendLine("SKIP " + spec.name + " source mismatch"); continue; }
                    if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    {
                        report.AppendLine("EXISTS " + path + " (not overwritten)");
                        continue;
                    }
                    var original = meshes[0];
                    var clone = UnityEngine.Object.Instantiate(original);
                    clone.name = original.name;
                    try
                    {
                        MeshLodUtility.GenerateMeshLods(clone, 2);
                        if (clone.GetLods(0).Length < 2 || clone.vertexCount != original.vertexCount ||
                            clone.subMeshCount != original.subMeshCount)
                        {
                            report.AppendLine("SKIP " + spec.name + " no usable LOD");
                            continue;
                        }
                        AssetDatabase.CreateAsset(clone, path);
                        generated++;
                        report.AppendLine("GENERATED " + spec.name + " levels=" + clone.GetLods(0).Length);
                    }
                    finally
                    {
                        if (!AssetDatabase.Contains(clone)) UnityEngine.Object.DestroyImmediate(clone);
                    }
                }
                catch (Exception ex) { report.AppendLine("ERROR " + spec.name + ": " + ex.Message); }
            }
            AssetDatabase.SaveAssets();
            report.AppendLine("Generated=" + generated);
            File.WriteAllText("Library/MotorCityLodPilot.txt", report.ToString());
            Debug.Log(report.ToString());
        }
    }
}
