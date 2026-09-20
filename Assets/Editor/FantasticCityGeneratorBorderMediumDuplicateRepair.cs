#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FantasticCityGeneratorBorderMediumDuplicateRepair
{
    private const string MaterialRoot =
        "Assets/Resources/MotorCity/Environment/FCGMaterials";

    private sealed class Rule
    {
        public string Path;
        public string Mesh;
        public float PositionX;
        public string[] Materials;
    }

    private static readonly Rule[] Rules =
    {
        new Rule
        {
            Path = "City-Maker/Border-Medium 2(Clone)/BD-BL",
            Mesh = "BD-BL",
            PositionX = -300f,
            Materials = new[]
            {
                "FCG_Roads",
                "FCG_Atlas-1",
                "FCG_Grass-01",
                "FCG_HighWay"
            }
        },
        new Rule
        {
            Path = "City-Maker/Border-Medium 2(Clone)/BD-BL",
            Mesh = "BD-BL",
            PositionX = 300f,
            Materials = new[]
            {
                "FCG_Roads",
                "FCG_Atlas-1",
                "FCG_Grass-01"
            }
        },
        new Rule
        {
            Path = "City-Maker/Border-Medium 2(Clone)/BD-BL/BD-BL-Sidewalk",
            Mesh = "BD-BL-Sidewalk",
            PositionX = -300f,
            Materials = new[]
            {
                "FCG_Roads",
                "FCG_Grass-01",
                "FCG_Atlas-1"
            }
        },
        new Rule
        {
            Path = "City-Maker/Border-Medium 2(Clone)/BD-BL/BD-BL-Sidewalk",
            Mesh = "BD-BL-Sidewalk",
            PositionX = 300f,
            Materials = new[]
            {
                "FCG_Roads"
            }
        }
    };

    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Border-Medium Repair",
                "Останови Play Mode перед восстановлением.",
                "OK");
            return;
        }

        if (!FantasticCityGeneratorSceneSource.TryOpenSourceScene(
                out Scene scene,
                out bool openedTemporarily,
                out Scene previousActiveScene))
        {
            EditorUtility.DisplayDialog(
                "Motor City — Border-Medium Repair",
                "Не найдена сохранённая FCG-сцена.",
                "OK");
            return;
        }

        try
        {
            Dictionary<string, Material> materials =
                LoadMaterials();

            Renderer[] renderers =
                scene
                    .GetRootGameObjects()
                    .SelectMany(
                        root =>
                            root.GetComponentsInChildren<Renderer>(true))
                    .ToArray();

            int repaired =
                0;

            var details =
                new List<string>();

            foreach (Rule rule in Rules)
            {
                Renderer target =
                    renderers.FirstOrDefault(
                        renderer =>
                            renderer != null &&
                            string.Equals(
                                GetHierarchyPath(renderer.transform),
                                rule.Path,
                                StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(
                                GetMeshName(renderer),
                                rule.Mesh,
                                StringComparison.OrdinalIgnoreCase) &&
                            Mathf.Abs(
                                renderer.transform.position.x -
                                rule.PositionX) < 0.5f);

                if (target == null)
                {
                    details.Add(
                        $"NOT FOUND: {rule.Path} mesh={rule.Mesh} x={rule.PositionX}");

                    continue;
                }

                Material[] exact =
                    new Material[rule.Materials.Length];

                bool complete =
                    true;

                for (int i = 0;
                     i < rule.Materials.Length;
                     i++)
                {
                    if (!materials.TryGetValue(
                            rule.Materials[i],
                            out Material material) ||
                        material == null)
                    {
                        complete =
                            false;

                        details.Add(
                            $"MISSING MATERIAL: {rule.Materials[i]}");

                        break;
                    }

                    exact[i] =
                        material;
                }

                if (!complete)
                    continue;

                Undo.RecordObject(
                    target,
                    "Repair remaining Border-Medium duplicate slots");

                target.sharedMaterials =
                    exact;

                EditorUtility.SetDirty(
                    target);

                repaired++;

                details.Add(
                    $"RESTORED: {rule.Path} mesh={rule.Mesh} x={rule.PositionX} -> " +
                    string.Join(", ", rule.Materials));
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                scene);

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Motor City: repaired duplicated Border-Medium renderer slots.\n" +
                string.Join("\n", details));

            EditorUtility.DisplayDialog(
                "Motor City — Border-Medium Repair",
                "Готово.\n\n" +
                $"Восстановлено renderer'ов: {repaired}/{Rules.Length}\n\n" +
                "Исправление различает дубли с одинаковым hierarchy path по позиции X, " +
                "чего не делал прошлый fixer.\n\n" +
                "Теперь запусти Build Runtime City from Saved FCG City.",
                "OK");
        }
        finally
        {
            FantasticCityGeneratorSceneSource.FinishSourceScene(
                scene,
                openedTemporarily,
                previousActiveScene,
                true);
        }
    }

    private static Dictionary<string, Material> LoadMaterials()
    {
        var result =
            new Dictionary<string, Material>(
                StringComparer.OrdinalIgnoreCase);

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Material",
                new[]
                {
                    MaterialRoot
                });

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (material == null ||
                string.IsNullOrWhiteSpace(material.name))
                continue;

            if (!result.ContainsKey(material.name))
                result.Add(material.name, material);
        }

        return result;
    }

    private static string GetMeshName(
        Renderer renderer)
    {
        MeshFilter filter =
            renderer.GetComponent<MeshFilter>();

        if (filter != null &&
            filter.sharedMesh != null)
            return filter.sharedMesh.name;

        SkinnedMeshRenderer skinned =
            renderer as SkinnedMeshRenderer;

        return skinned != null &&
               skinned.sharedMesh != null
            ? skinned.sharedMesh.name
            : string.Empty;
    }

    private static string GetHierarchyPath(
        Transform item)
    {
        string path =
            item.name;

        Transform current =
            item.parent;

        while (current != null)
        {
            path =
                current.name +
                "/" +
                path;

            current =
                current.parent;
        }

        return path;
    }
}
#endif
