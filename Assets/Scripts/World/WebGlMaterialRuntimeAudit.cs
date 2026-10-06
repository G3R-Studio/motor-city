using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    public sealed class WebGlMaterialRuntimeAudit : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private const float InitialDelaySeconds = 2.0f;
        private const int MaximumDetailedEntries = 200;

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(
                InitialDelaySeconds);

            RunAudit();

            Destroy(
                this);
        }

        private void RunAudit()
        {
            Renderer[] renderers =
                FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int rendererCount =
                0;

            int materialSlotCount =
                0;

            int nullMaterialSlots =
                0;

            int nullShaderCount =
                0;

            int unsupportedShaderCount =
                0;

            int errorShaderCount =
                0;

            var shaderUsage =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            var suspiciousEntries =
                new List<string>();

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null)
                    continue;

                rendererCount++;

                Material[] materials =
                    renderer.sharedMaterials;

                if (materials == null ||
                    materials.Length == 0)
                {
                    continue;
                }

                string objectPath =
                    HierarchyPath(
                        renderer.transform);

                for (int i = 0;
                     i < materials.Length;
                     i++)
                {
                    materialSlotCount++;

                    Material material =
                        materials[i];

                    if (material == null)
                    {
                        nullMaterialSlots++;

                        AddSuspicious(
                            suspiciousEntries,
                            "[NULL MATERIAL] " +
                            objectPath +
                            " [slot " +
                            i +
                            "]");

                        continue;
                    }

                    Shader shader =
                        material.shader;

                    if (shader == null)
                    {
                        nullShaderCount++;

                        AddSuspicious(
                            suspiciousEntries,
                            "[NULL SHADER] " +
                            objectPath +
                            " [slot " +
                            i +
                            "] material=" +
                            material.name);

                        continue;
                    }

                    string shaderName =
                        shader.name;

                    if (!shaderUsage.TryGetValue(
                            shaderName,
                            out int shaderCount))
                    {
                        shaderCount =
                            0;
                    }

                    shaderUsage[
                        shaderName] =
                        shaderCount +
                        1;

                    bool supported =
                        shader.isSupported;

                    if (!supported)
                    {
                        unsupportedShaderCount++;

                        AddSuspicious(
                            suspiciousEntries,
                            "[UNSUPPORTED SHADER] " +
                            objectPath +
                            " [slot " +
                            i +
                            "] material=" +
                            material.name +
                            " shader=" +
                            shaderName);
                    }

                    bool errorShader =
                        shaderName.Equals(
                            "Hidden/InternalErrorShader",
                            StringComparison.OrdinalIgnoreCase) ||
                        shaderName.IndexOf(
                            "error",
                            StringComparison.OrdinalIgnoreCase) >=
                        0;

                    if (errorShader)
                    {
                        errorShaderCount++;

                        AddSuspicious(
                            suspiciousEntries,
                            "[ERROR SHADER] " +
                            objectPath +
                            " [slot " +
                            i +
                            "] material=" +
                            material.name +
                            " shader=" +
                            shaderName);
                    }

                    if (material.passCount <= 0)
                    {
                        AddSuspicious(
                            suspiciousEntries,
                            "[NO PASSES] " +
                            objectPath +
                            " [slot " +
                            i +
                            "] material=" +
                            material.name +
                            " shader=" +
                            shaderName);
                    }

                    if (renderer is MeshRenderer)
                    {
                        MeshFilter filter =
                            renderer.GetComponent<MeshFilter>();

                        if (filter == null ||
                            filter.sharedMesh == null)
                        {
                            AddSuspicious(
                                suspiciousEntries,
                                "[MISSING MESH] " +
                                objectPath +
                                " material=" +
                                material.name);
                        }
                    }
                    else if (renderer is SkinnedMeshRenderer skinned &&
                             skinned.sharedMesh == null)
                    {
                        AddSuspicious(
                            suspiciousEntries,
                            "[MISSING SKINNED MESH] " +
                            objectPath +
                            " material=" +
                            material.name);
                    }
                }
            }

            Debug.Log(
                "Motor City WebGL Material Runtime Audit" +
                "\nRenderers: " +
                rendererCount +
                "\nMaterial slots: " +
                materialSlotCount +
                "\nNull material slots: " +
                nullMaterialSlots +
                "\nNull shaders: " +
                nullShaderCount +
                "\nUnsupported shaders: " +
                unsupportedShaderCount +
                "\nError shaders: " +
                errorShaderCount +
                "\nSuspicious entries: " +
                suspiciousEntries.Count);

            foreach (string entry in
                     suspiciousEntries)
            {
                Debug.LogWarning(
                    entry);
            }

            string shaderSummary =
                string.Join(
                    "\n",
                    shaderUsage
                        .OrderByDescending(
                            pair => pair.Value)
                        .ThenBy(
                            pair => pair.Key,
                            StringComparer.OrdinalIgnoreCase)
                        .Take(80)
                        .Select(
                            pair =>
                                pair.Key +
                                " => " +
                                pair.Value));

            Debug.Log(
                "Motor City WebGL shader usage (top):\n" +
                shaderSummary);
        }

        private static void AddSuspicious(
            List<string> entries,
            string value)
        {
            if (entries.Count >=
                MaximumDetailedEntries)
            {
                return;
            }

            entries.Add(
                value);
        }

        private static string HierarchyPath(
            Transform transform)
        {
            if (transform == null)
                return "<null>";

            string path =
                transform.name;

            Transform current =
                transform.parent;

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
#else
        private void Awake()
        {
            enabled =
                false;
        }
#endif
    }
}
