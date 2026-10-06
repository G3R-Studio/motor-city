using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MotorCity.EditorTools
{
    public static class MotorCityMaterialAudit
    {
        [MenuItem("Motor City/Diagnostics/Audit Materials In Open Scene")]
        public static void AuditOpenSceneMaterials()
        {
            var bad = new Dictionary<Material, List<string>>();
            var missing = new List<string>();

            Scene scene = SceneManager.GetActiveScene();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null)
                        continue;

                    Material[] materials = renderer.sharedMaterials;

                    if (materials == null || materials.Length == 0)
                        continue;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material material = materials[i];
                        string objectPath = HierarchyPath(renderer.transform);

                        if (material == null)
                        {
                            missing.Add(objectPath + "  [slot " + i + "]");
                            continue;
                        }

                        Shader shader = material.shader;

                        if (shader != null && shader.isSupported)
                            continue;

                        if (!bad.TryGetValue(material, out List<string> users))
                        {
                            users = new List<string>();
                            bad.Add(material, users);
                        }

                        users.Add(objectPath + "  [slot " + i + "]");
                    }
                }
            }

            Debug.Log(
                "Motor City Material Audit — scene: " + scene.name +
                "\nMissing material slots: " + missing.Count +
                "\nUnsupported/null-shader materials: " + bad.Count);

            foreach (string item in missing.OrderBy(
                         value => value,
                         StringComparer.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[Missing Material] " + item);
            }

            foreach (KeyValuePair<Material, List<string>> pair in
                     bad.OrderBy(
                         pair => pair.Key != null ? pair.Key.name : string.Empty,
                         StringComparer.OrdinalIgnoreCase))
            {
                Material material = pair.Key;
                string assetPath = AssetDatabase.GetAssetPath(material);
                string shaderName =
                    material != null && material.shader != null
                        ? material.shader.name
                        : "<null shader>";

                Debug.LogWarning(
                    "[Unsupported Material] " + material.name +
                    "\nShader: " + shaderName +
                    "\nAsset: " + assetPath +
                    "\nUsed by:\n- " +
                    string.Join("\n- ", pair.Value.Distinct()),
                    material);
            }

            Selection.objects =
                bad.Keys
                    .Where(material => material != null)
                    .Cast<UnityEngine.Object>()
                    .ToArray();
        }

        [MenuItem("Motor City/Diagnostics/Audit All Project Materials")]
        public static void AuditAllProjectMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");
            int unsupportedCount = 0;
            int missingShaderCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                Material material =
                    AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null)
                    continue;

                Shader shader = material.shader;

                if (shader == null)
                {
                    missingShaderCount++;
                    Debug.LogWarning("[Material Null Shader] " + path, material);
                    continue;
                }

                if (shader.isSupported)
                    continue;

                unsupportedCount++;

                Debug.LogWarning(
                    "[Material Unsupported Shader] " + path +
                    "\nShader: " + shader.name,
                    material);
            }

            Debug.Log(
                "Motor City Project Material Audit complete." +
                "\nMaterials scanned: " + guids.Length +
                "\nNull shaders: " + missingShaderCount +
                "\nUnsupported shaders: " + unsupportedCount);
        }

        private static string HierarchyPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            string path = transform.name;
            Transform current = transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
