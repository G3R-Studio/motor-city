using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MotorCity.Diagnostics
{
    public static class MotorCityWebMaterialDiagnostics
    {
        private sealed class MaterialUse
        {
            public Material Material;
            public string ShaderName;
            public bool ShaderSupported;
            public int RendererCount;
            public readonly List<string> Samples = new();
        }

        private static bool hasRun;

        public static void Run(GameObject root)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (hasRun || root == null)
                return;

            hasRun = true;

            Renderer[] renderers =
                root.GetComponentsInChildren<Renderer>(true);

            var uses =
                new Dictionary<Material, MaterialUse>();

            var missingSlots =
                new List<string>();

            int rendererCount = 0;
            int materialSlotCount = 0;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                rendererCount++;

                Material[] materials =
                    renderer.sharedMaterials;

                if (materials == null)
                    continue;

                for (int i = 0; i < materials.Length; i++)
                {
                    materialSlotCount++;

                    Material material = materials[i];
                    string hierarchyPath =
                        HierarchyPath(renderer.transform);

                    if (material == null)
                    {
                        if (missingSlots.Count < 50)
                        {
                            missingSlots.Add(
                                hierarchyPath +
                                " [slot " +
                                i +
                                "]");
                        }

                        continue;
                    }

                    if (!uses.TryGetValue(
                            material,
                            out MaterialUse use))
                    {
                        Shader shader =
                            material.shader;

                        use =
                            new MaterialUse
                            {
                                Material = material,
                                ShaderName =
                                    shader != null
                                        ? shader.name
                                        : "<null>",
                                ShaderSupported =
                                    shader != null &&
                                    shader.isSupported
                            };

                        uses.Add(material, use);
                    }

                    use.RendererCount++;

                    if (use.Samples.Count < 5)
                    {
                        use.Samples.Add(
                            hierarchyPath +
                            " [slot " +
                            i +
                            "]");
                    }
                }
            }

            Debug.Log(
                "[MotorCity Web Material Audit] Renderers=" +
                rendererCount +
                ", materialSlots=" +
                materialSlotCount +
                ", uniqueMaterials=" +
                uses.Count +
                ", missingSlots=" +
                missingSlots.Count);

            foreach (string missing in missingSlots)
            {
                Debug.LogWarning(
                    "[MotorCity Web Missing Material] " +
                    missing);
            }

            var suspicious =
                uses.Values
                    .Where(
                        use =>
                            !use.ShaderSupported ||
                            string.IsNullOrEmpty(use.ShaderName) ||
                            use.ShaderName.Contains(
                                "InternalError",
                                StringComparison.OrdinalIgnoreCase) ||
                            !use.ShaderName.StartsWith(
                                "Universal Render Pipeline/",
                                StringComparison.Ordinal))
                    .OrderBy(
                        use => use.ShaderName,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        use => use.Material != null
                            ? use.Material.name
                            : string.Empty,
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            Debug.Log(
                "[MotorCity Web Material Audit] Suspicious/non-URP materials=" +
                suspicious.Length);

            foreach (MaterialUse use in suspicious)
            {
                string materialName =
                    use.Material != null
                        ? use.Material.name
                        : "<null>";

                Debug.LogWarning(
                    "[MotorCity Web Material] Material='" +
                    materialName +
                    "', Shader='" +
                    use.ShaderName +
                    "', Supported=" +
                    use.ShaderSupported +
                    ", Renderers=" +
                    use.RendererCount +
                    "\nUsed by:\n- " +
                    string.Join(
                        "\n- ",
                        use.Samples));
            }

            var shaderGroups =
                uses.Values
                    .GroupBy(use => use.ShaderName)
                    .OrderBy(
                        group => group.Key,
                        StringComparer.OrdinalIgnoreCase);

            foreach (var group in shaderGroups)
            {
                Debug.Log(
                    "[MotorCity Web Shader Summary] Shader='" +
                    group.Key +
                    "', Supported=" +
                    group.All(use => use.ShaderSupported) +
                    ", Materials=" +
                    group.Count() +
                    ", RendererUses=" +
                    group.Sum(use => use.RendererCount));
            }
#endif
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
