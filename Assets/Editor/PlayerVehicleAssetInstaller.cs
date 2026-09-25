#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    [InitializeOnLoad]
    public static class PlayerVehicleAssetInstaller
    {
        private const string OutputRoot =
            "Assets/Resources/MotorCity/Vehicles/Player";

        private const string GeneratedMaterialRoot =
            OutputRoot + "/GeneratedMaterials";

        private const string MuscleColorTexturePath =
            "Assets/Vehicles/Imported/Fbx_MuscleCar/Fbx/Texture/Color.png";

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
            EnsureFolder(OutputRoot);
            EnsureFolder(GeneratedMaterialRoot);

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
            EnsureFolder(OutputRoot);
            EnsureFolder(GeneratedMaterialRoot);
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
                PrepareImportedMaterials(instance, asset.OutputName);

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

        private static void PrepareImportedMaterials(
            GameObject instance,
            string outputName)
        {
            if (outputName == "Hybrid")
            {
                ConvertHybridMaterials(instance);
                return;
            }

            if (outputName == "MuscleCar10")
                ConvertMuscleCarMaterials(instance);
        }

        private static void ConvertHybridMaterials(GameObject instance)
        {
            Shader lit = UrpLitShader();

            if (lit == null)
            {
                Debug.LogError(
                    "[MotorCity][Vehicles] URP/Lit shader was not found while preparing Hybrid.");
                return;
            }

            var converted =
                new Dictionary<Material, Material>();

            foreach (Renderer renderer in
                     instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];

                    if (source == null)
                        continue;

                    if (!converted.TryGetValue(source, out Material target))
                    {
                        string materialName =
                            "Hybrid_" + SafeName(source.name);

                        target =
                            GetOrCreateGeneratedMaterial(
                                materialName,
                                lit);

                        CopyToUrpMaterial(
                            source,
                            target,
                            materialName);

                        converted.Add(source, target);
                    }

                    materials[i] = target;
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }
        }

        private static void ConvertMuscleCarMaterials(GameObject instance)
        {
            Shader lit = UrpLitShader();

            if (lit == null)
            {
                Debug.LogError(
                    "[MotorCity][Vehicles] URP/Lit shader was not found while preparing MuscleCar10.");
                return;
            }

            Renderer bodyRenderer =
                FindLargestNonWheelRenderer(instance);

            Material bodyPaint =
                GetOrCreateGeneratedMaterial(
                    "MuscleCar10_BodyPaint",
                    lit);

            // Color.png is a palette/atlas used by the original FBX. It must not
            // be used as the body albedo map: most of the car UVs land on its
            // white area while the red area is used by the lamps.
            ResetUrpMaterial(
                bodyPaint,
                new Color(0.68f, 0.055f, 0.04f, 1f),
                null,
                0.10f,
                0.58f);

            var detailMaterials =
                new Dictionary<Material, Material>();

            foreach (Renderer renderer in
                     instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;

                if (materials == null ||
                    materials.Length == 0)
                {
                    continue;
                }

                bool wheel =
                    IsWheelLike(renderer.transform.name);

                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];

                    if (source == null)
                        continue;

                    // The main visible shell of this particular FBX is the first
                    // material slot on the largest non-wheel renderer. The same
                    // source material is also reused by separate tire renderers,
                    // so we only replace it on the body renderer itself.
                    if (!wheel &&
                        renderer == bodyRenderer &&
                        i == 0)
                    {
                        materials[i] = bodyPaint;
                        continue;
                    }

                    if (!detailMaterials.TryGetValue(
                            source,
                            out Material detail))
                    {
                        string detailName =
                            wheel
                                ? "MuscleCar10_Wheel_" + SafeName(source.name)
                                : "MuscleCar10_Detail_" + SafeName(source.name);

                        detail =
                            GetOrCreateGeneratedMaterial(
                                detailName,
                                lit);

                        CopyToUrpMaterial(
                            source,
                            detail,
                            detailName);

                        detailMaterials.Add(
                            source,
                            detail);
                    }

                    materials[i] = detail;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static Renderer FindLargestNonWheelRenderer(
            GameObject instance)
        {
            Renderer best = null;
            float bestVolume = -1f;

            foreach (Renderer renderer in
                     instance.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null ||
                    IsWheelLike(renderer.transform.name))
                {
                    continue;
                }

                Vector3 size = renderer.bounds.size;
                float volume =
                    Mathf.Abs(size.x * size.y * size.z);

                if (volume <= bestVolume)
                    continue;

                best = renderer;
                bestVolume = volume;
            }

            return best;
        }

        private static Material GetOrCreateGeneratedMaterial(
            string materialName,
            Shader shader)
        {
            string path =
                GeneratedMaterialRoot +
                "/" +
                SafeName(materialName) +
                ".mat";

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material =
                    new Material(shader)
                    {
                        name = materialName
                    };

                AssetDatabase.CreateAsset(
                    material,
                    path);
            }
            else
            {
                material.shader = shader;
                material.name = materialName;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CopyToUrpMaterial(
            Material source,
            Material target,
            string targetName)
        {
            Color color = Color.white;
            Texture texture = null;

            if (source != null)
            {
                if (source.HasProperty("_BaseColor"))
                    color = source.GetColor("_BaseColor");
                else if (source.HasProperty("_Color"))
                    color = source.GetColor("_Color");

                if (source.HasProperty("_BaseMap"))
                    texture = source.GetTexture("_BaseMap");
                else if (source.HasProperty("_MainTex"))
                    texture = source.GetTexture("_MainTex");
            }

            float metallic =
                source != null &&
                source.HasProperty("_Metallic")
                    ? source.GetFloat("_Metallic")
                    : 0f;

            float smoothness = 0.35f;

            if (source != null &&
                source.HasProperty("_Smoothness"))
            {
                smoothness = source.GetFloat("_Smoothness");
            }
            else if (source != null &&
                     source.HasProperty("_Glossiness"))
            {
                smoothness = source.GetFloat("_Glossiness");
            }

            ResetUrpMaterial(
                target,
                color,
                texture,
                metallic,
                smoothness);

            string lower =
                (targetName ?? string.Empty)
                    .ToLowerInvariant();

            if (lower.Contains("head light") ||
                lower.Contains("head_light") ||
                lower.Contains("tail light") ||
                lower.Contains("tail_light"))
            {
                Color emission =
                    new Color(
                        Mathf.Max(color.r, 0.15f),
                        Mathf.Max(color.g, 0.15f),
                        Mathf.Max(color.b, 0.15f),
                        1f);

                if (target.HasProperty("_EmissionColor"))
                {
                    target.SetColor(
                        "_EmissionColor",
                        emission * 1.35f);

                    target.EnableKeyword("_EMISSION");
                }
            }

            EditorUtility.SetDirty(target);
        }

        private static void ResetUrpMaterial(
            Material material,
            Color color,
            Texture texture,
            float metallic,
            float smoothness)
        {
            if (material == null)
                return;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);

            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            material.DisableKeyword("_EMISSION");

            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.black);

            EditorUtility.SetDirty(material);
        }

        private static Shader UrpLitShader()
        {
            return
                Shader.Find("Universal Render Pipeline/Lit") ??
                Shader.Find("Standard");
        }

        private static bool IsWheelLike(string name)
        {
            string lower =
                (name ?? string.Empty)
                    .ToLowerInvariant();

            return
                lower.Contains("wheel") ||
                lower.Contains("tire") ||
                lower.Contains("tyre") ||
                lower.Contains("rim");
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Material";

            var builder = new StringBuilder(value.Length);

            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c) ||
                    c == '_' ||
                    c == '-')
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append('_');
                }
            }

            return builder.ToString();
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

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
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
