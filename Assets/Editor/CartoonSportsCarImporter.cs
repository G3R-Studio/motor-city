using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    [InitializeOnLoad]
    public static class CartoonSportsCarImporter
    {
        private const string OutputFolder = "Assets/Resources/MotorCity";
        private const string OutputPath = OutputFolder + "/PlayerCarVisual.prefab";

        static CartoonSportsCarImporter()
        {
            EditorApplication.delayCall += TryBuildRuntimePrefab;
        }

        [MenuItem("Motor City/Rebuild Cartoon Sports Car Visual")]
        public static void TryBuildRuntimePrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string sourcePath = FindBestSource();
            if (string.IsNullOrEmpty(sourcePath)) return;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) return;

            EnsureFolder(OutputFolder);

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null) instance = UnityEngine.Object.Instantiate(source);

            try
            {
                instance.name = "CartoonSportsCarVisual";
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                RemovePhysics(instance);
                PrefabUtility.SaveAsPrefabAsset(instance, OutputPath);
                Debug.Log($"Motor City: prepared Cartoon Sports Car runtime visual from '{sourcePath}'.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static string FindBestSource()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameObject");
            string bestPath = null;
            int bestScore = int.MinValue;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) continue;
                if (path.Equals(OutputPath, StringComparison.OrdinalIgnoreCase)) continue;

                string lower = path.ToLowerInvariant();
                int score = 0;

                if (lower.Contains("cartoon")) score += 8;
                if (lower.Contains("sport")) score += 8;
                if (lower.Contains("car")) score += 5;
                if (lower.Contains("rcc")) score += 6;
                if (lower.Contains("low")) score += 3;
                if (lower.Contains("prefab")) score += 2;
                if (lower.Contains("demo") || lower.Contains("scene")) score -= 6;

                if (score < 12) continue;

                GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (candidate == null) continue;

                int wheelLike = CountWheelLikeTransforms(candidate.transform);
                score += Mathf.Min(wheelLike, 4) * 5;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPath = path;
                }
            }

            return bestPath;
        }

        private static int CountWheelLikeTransforms(Transform root)
        {
            int count = 0;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform item in all)
            {
                string n = item.name.ToLowerInvariant();
                if (n.Contains("wheel") || n.Contains("tyre") || n.Contains("tire")) count++;
            }
            return count;
        }

        private static void RemovePhysics(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);

            foreach (Rigidbody rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.DestroyImmediate(rigidbody);

            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                UnityEngine.Object.DestroyImmediate(behaviour);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
