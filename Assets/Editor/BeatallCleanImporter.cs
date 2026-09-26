#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    [InitializeOnLoad]
    public static class BeatallCleanImporter
    {
        private const string SourcePath =
            "Assets/Vehicles/Imported/Designersoup_CarPack1/Beatall/beatall.obj";

        private const string TexturePath =
            "Assets/Vehicles/Imported/Designersoup_CarPack1/Beatall/387359c5580f06c08c266126b3b46db47e48ba44.png";

        private const string PrototypeRoot =
            "Assets/MotorCity/VehiclePrototypes/Beatall";

        private const string MaterialRoot =
            PrototypeRoot + "/Materials";

        private const string PrototypePath =
            PrototypeRoot + "/BeatallVisual.prefab";

        private const string ResourceRoot =
            "Assets/Resources/MotorCity/Vehicles/Player";

        private const string ResourcePath =
            ResourceRoot + "/Beatall.prefab";

        static BeatallCleanImporter()
        {
            EditorApplication.delayCall += EnsureBuilt;
        }

        [MenuItem("Motor City/Vehicles/Rebuild Beatall Clean")]
        public static void Rebuild()
        {
            Build(true);
        }

        private static void EnsureBuilt()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrototypePath) != null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(ResourcePath) != null)
            {
                return;
            }

            Build(false);
        }

        private static void Build(bool log)
        {
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);

            if (source == null)
            {
                if (log)
                    Debug.LogWarning("[MotorCity][Beatall] Source OBJ not found.");

                return;
            }

            EnsureFolder(PrototypeRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(ResourceRoot);

            GameObject instance =
                Object.Instantiate(source);

            instance.name = "BeatallVisual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            try
            {
                ApplyUrpMaterials(instance);

                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    PrototypePath);

                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    ResourcePath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (log)
                {
                    Debug.Log(
                        "[MotorCity][Beatall] Clean Beatall visual rebuilt from OBJ.");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void ApplyUrpMaterials(GameObject root)
        {
            Shader lit =
                Shader.Find("Universal Render Pipeline/Lit");

            if (lit == null)
                return;

            Texture texture =
                AssetDatabase.LoadAssetAtPath<Texture>(TexturePath);

            foreach (Renderer renderer in
                     root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] sourceMaterials =
                    renderer.sharedMaterials;

                Material[] assigned =
                    new Material[sourceMaterials.Length];

                for (int i = 0; i < sourceMaterials.Length; i++)
                {
                    Material source =
                        sourceMaterials[i];

                    string lower =
                        source == null
                            ? string.Empty
                            : source.name.ToLowerInvariant();

                    if (lower.Contains("blackglass"))
                    {
                        assigned[i] =
                            GetOrCreateMaterial(
                                "Beatall_BlackGlass",
                                lit,
                                new Color(0.015f, 0.02f, 0.025f, 1f),
                                null,
                                0.05f,
                                0.82f);
                    }
                    else if (lower.Contains("gradientemmisive") ||
                             lower.Contains("gradientemissive"))
                    {
                        assigned[i] =
                            GetOrCreateMaterial(
                                "Beatall_EmissiveDetails",
                                lit,
                                Color.white,
                                texture,
                                0f,
                                0.42f);
                    }
                    else
                    {
                        assigned[i] =
                            GetOrCreateMaterial(
                                "Beatall_BaseGradient",
                                lit,
                                Color.white,
                                texture,
                                0f,
                                0.38f);
                    }
                }

                renderer.sharedMaterials =
                    assigned;
            }
        }

        private static Material GetOrCreateMaterial(
            string name,
            Shader shader,
            Color color,
            Texture texture,
            float metallic,
            float smoothness)
        {
            string path =
                MaterialRoot + "/" + name + ".mat";

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material =
                    new Material(shader)
                    {
                        name = name
                    };

                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.black);

            material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);

            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent =
                Path.GetDirectoryName(path)
                    ?.Replace('\\', '/');

            string name =
                Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
