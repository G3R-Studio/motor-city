using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotorCity.Editor
{
    public static class AmaneKisoraPixieInstaller
    {
        private const string OutputFolder =
            "Assets/Resources/MotorCity/Pixie";

        private const string OutputPrefab =
            OutputFolder +
            "/AmaneKisoraVisual.prefab";

        [MenuItem(
            "Motor City/Pixie/Install Amane Kisora Exclusive")]
        public static void Install()
        {
            string sourcePath =
                FindKisoraAsset();

            if (string.IsNullOrEmpty(
                    sourcePath))
            {
                EditorUtility.DisplayDialog(
                    "Motor City",
                    "Amane Kisora-chan не найдена. Сначала импортируй пакет из Unity Asset Store, затем запусти эту команду ещё раз.",
                    "OK");

                return;
            }

            ConfigureHumanoidIfPossible(
                sourcePath);

            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    sourcePath);

            if (source == null)
            {
                EditorUtility.DisplayDialog(
                    "Motor City",
                    "Найденный ассет Kisora не удалось открыть как GameObject: " +
                    sourcePath,
                    "OK");

                return;
            }

            Directory.CreateDirectory(
                OutputFolder);

            GameObject instance =
                PrefabUtility.InstantiatePrefab(
                    source) as GameObject;

            if (instance == null)
            {
                instance =
                    UnityEngine.Object.Instantiate(
                        source);
            }

            try
            {
                instance.name =
                    "AmaneKisoraVisual";

                Animator animator =
                    instance.GetComponentInChildren<Animator>(
                        true);

                if (animator == null)
                {
                    animator =
                        instance.AddComponent<Animator>();
                }

                GameObject defaultPixie =
                    Resources.Load<GameObject>(
                        "MotorCity/Byte/HaonByteVisual");

                Animator defaultAnimator =
                    defaultPixie == null
                        ? null
                        : defaultPixie.GetComponentInChildren<Animator>(
                            true);

                if (defaultAnimator != null &&
                    defaultAnimator.runtimeAnimatorController != null)
                {
                    animator.runtimeAnimatorController =
                        defaultAnimator.runtimeAnimatorController;
                }

                animator.applyRootMotion =
                    false;
                animator.cullingMode =
                    AnimatorCullingMode.AlwaysAnimate;

                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    OutputPrefab);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        OutputPrefab);

                EditorUtility.DisplayDialog(
                    "Motor City",
                    "Kisora установлена как эксклюзивный облик Пикси. Prefab: " +
                    OutputPrefab,
                    "Готово");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    instance);
            }
        }

        private static string FindKisoraAsset()
        {
            string[] queries =
            {
                "SapphiArtchan t:GameObject",
                "Kisora t:GameObject",
                "Amane t:GameObject"
            };

            foreach (string query in queries)
            {
                string[] guids =
                    AssetDatabase.FindAssets(
                        query);

                foreach (string guid in guids)
                {
                    string path =
                        AssetDatabase.GUIDToAssetPath(
                            guid);

                    if (string.IsNullOrEmpty(
                            path) ||
                        path.StartsWith(
                            OutputFolder,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string lower =
                        path.ToLowerInvariant();

                    if (lower.Contains(
                            "sapphi") ||
                        lower.Contains(
                            "kisora") ||
                        lower.Contains(
                            "amane"))
                    {
                        return path;
                    }
                }
            }

            return string.Empty;
        }

        private static void ConfigureHumanoidIfPossible(
            string assetPath)
        {
            ModelImporter importer =
                AssetImporter.GetAtPath(
                    assetPath) as ModelImporter;

            if (importer == null ||
                importer.animationType ==
                    ModelImporterAnimationType.Human)
            {
                return;
            }

            importer.animationType =
                ModelImporterAnimationType.Human;

            try
            {
                importer.SaveAndReimport();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[MotorCity][Pixie] Не удалось автоматически перевести Kisora в Humanoid: " +
                    exception.Message);
            }
        }
    }
}
