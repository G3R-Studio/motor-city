#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MotorCityPlayerGarageBuilder
{
    private const string SourceRoot =
        "Assets/Art/MotorCity/Garage";

    private const string ModelPath =
        SourceRoot + "/Garages_4.fbx";

    private const string BaseColorPath =
        SourceRoot + "/Garage_1_Base_color.jpg";

    private const string NormalPath =
        SourceRoot + "/Garage_1_Normal_DirectX.jpg";

    private const string MetallicPath =
        SourceRoot + "/Garage_1_Metallic.jpg";

    private const string RoughnessPath =
        SourceRoot + "/Garage_1_Roughness.jpg";

    private const string OcclusionPath =
        SourceRoot + "/Garage_1_Mixed_AO.jpg";

    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Environment/Garage";

    private const string PackedMetallicSmoothnessPath =
        RuntimeRoot + "/Garage_1_MetallicSmoothness.png";

    private const string MaterialPath =
        RuntimeRoot + "/Garage_1_URP.mat";

    private const string PrefabPath =
        RuntimeRoot + "/MotorCity_PlayerGarage.prefab";

    private static readonly Vector3 VisualRotation =
        new(270f, 179.999832f, 0f);

    private static readonly Vector3 VisualScale =
        new(308.016449f, 228.785355f, 242.584091f);

    [MenuItem("Motor City/Garage/1 - Build Player Garage Assets")]
    public static void Build()
    {
        GameObject model =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                ModelPath);

        Texture2D baseColor =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                BaseColorPath);

        Texture2D normal =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                NormalPath);

        Texture2D occlusion =
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                OcclusionPath);

        if (model == null ||
            baseColor == null ||
            normal == null ||
            occlusion == null)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Player Garage",
                "Не найдены исходные FBX/PBR ассеты гаража в Assets/Art/MotorCity/Garage.",
                "OK");

            return;
        }

        EnsureFolder(
            RuntimeRoot);

        try
        {
            BuildMetallicSmoothness();
            AssetDatabase.Refresh();

            Texture2D metallicSmoothness =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    PackedMetallicSmoothnessPath);

            if (metallicSmoothness == null)
            {
                throw new InvalidOperationException(
                    "Не удалось создать Garage_1_MetallicSmoothness.png.");
            }

            Material material =
                BuildMaterial(
                    baseColor,
                    normal,
                    metallicSmoothness,
                    occlusion);

            BuildPrefab(
                model,
                material);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Motor City — Player Garage",
                "Готово. Созданы URP-материал и runtime prefab гаража.\n\n" +
                PrefabPath,
                "OK");

            Debug.Log(
                "Motor City: player garage assets built. " +
                "Prefab=" + PrefabPath +
                ", Material=" + MaterialPath);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Motor City: failed to build player garage assets. " +
                exception);

            EditorUtility.DisplayDialog(
                "Motor City — Player Garage",
                "Не удалось собрать гараж. Посмотри Console.",
                "OK");
        }
    }

    private static void BuildMetallicSmoothness()
    {
        TextureImporter metallicImporter =
            AssetImporter.GetAtPath(
                MetallicPath) as TextureImporter;

        TextureImporter roughnessImporter =
            AssetImporter.GetAtPath(
                RoughnessPath) as TextureImporter;

        if (metallicImporter == null ||
            roughnessImporter == null)
        {
            throw new InvalidOperationException(
                "Metallic/Roughness importers are unavailable.");
        }

        bool metallicReadable =
            metallicImporter.isReadable;

        bool roughnessReadable =
            roughnessImporter.isReadable;

        try
        {
            if (!metallicReadable)
            {
                metallicImporter.isReadable = true;
                metallicImporter.SaveAndReimport();
            }

            if (!roughnessReadable)
            {
                roughnessImporter.isReadable = true;
                roughnessImporter.SaveAndReimport();
            }

            Texture2D metallic =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    MetallicPath);

            Texture2D roughness =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    RoughnessPath);

            if (metallic == null ||
                roughness == null)
            {
                throw new InvalidOperationException(
                    "Metallic/Roughness textures could not be loaded.");
            }

            if (metallic.width != roughness.width ||
                metallic.height != roughness.height)
            {
                throw new InvalidOperationException(
                    "Metallic and Roughness textures must have equal imported dimensions.");
            }

            Color32[] metallicPixels =
                metallic.GetPixels32();

            Color32[] roughnessPixels =
                roughness.GetPixels32();

            Color32[] packedPixels =
                new Color32[metallicPixels.Length];

            for (int i = 0;
                 i < packedPixels.Length;
                 i++)
            {
                byte metallicValue =
                    metallicPixels[i].r;

                byte smoothnessValue =
                    (byte)(255 -
                           roughnessPixels[i].r);

                packedPixels[i] =
                    new Color32(
                        metallicValue,
                        0,
                        0,
                        smoothnessValue);
            }

            Texture2D packed =
                new(
                    metallic.width,
                    metallic.height,
                    TextureFormat.RGBA32,
                    false,
                    true);

            packed.name =
                "Garage_1_MetallicSmoothness";

            packed.SetPixels32(
                packedPixels);

            packed.Apply(
                false,
                false);

            File.WriteAllBytes(
                PackedMetallicSmoothnessPath,
                packed.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(
                packed);

            AssetDatabase.ImportAsset(
                PackedMetallicSmoothnessPath,
                ImportAssetOptions.ForceUpdate);

            TextureImporter packedImporter =
                AssetImporter.GetAtPath(
                    PackedMetallicSmoothnessPath) as TextureImporter;

            if (packedImporter != null)
            {
                packedImporter.sRGBTexture =
                    false;

                packedImporter.maxTextureSize =
                    1024;

                packedImporter.textureCompression =
                    TextureImporterCompression.Compressed;

                packedImporter.isReadable =
                    false;

                packedImporter.SaveAndReimport();
            }
        }
        finally
        {
            metallicImporter =
                AssetImporter.GetAtPath(
                    MetallicPath) as TextureImporter;

            roughnessImporter =
                AssetImporter.GetAtPath(
                    RoughnessPath) as TextureImporter;

            if (metallicImporter != null &&
                metallicImporter.isReadable !=
                metallicReadable)
            {
                metallicImporter.isReadable =
                    metallicReadable;

                metallicImporter.SaveAndReimport();
            }

            if (roughnessImporter != null &&
                roughnessImporter.isReadable !=
                roughnessReadable)
            {
                roughnessImporter.isReadable =
                    roughnessReadable;

                roughnessImporter.SaveAndReimport();
            }
        }
    }

    private static Material BuildMaterial(
        Texture2D baseColor,
        Texture2D normal,
        Texture2D metallicSmoothness,
        Texture2D occlusion)
    {
        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (shader == null)
        {
            throw new InvalidOperationException(
                "Universal Render Pipeline/Lit shader was not found.");
        }

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MaterialPath);

        if (material == null)
        {
            material =
                new Material(
                    shader)
                {
                    name =
                        "Garage_1_URP"
                };

            AssetDatabase.CreateAsset(
                material,
                MaterialPath);
        }
        else
        {
            material.shader =
                shader;
        }

        material.SetColor(
            "_BaseColor",
            Color.white);

        material.SetTexture(
            "_BaseMap",
            baseColor);

        material.SetTexture(
            "_BumpMap",
            normal);

        material.SetFloat(
            "_BumpScale",
            1f);

        material.SetTexture(
            "_MetallicGlossMap",
            metallicSmoothness);

        material.SetFloat(
            "_Metallic",
            1f);

        material.SetFloat(
            "_Smoothness",
            1f);

        material.SetTexture(
            "_OcclusionMap",
            occlusion);

        material.SetFloat(
            "_OcclusionStrength",
            0.85f);

        material.EnableKeyword(
            "_NORMALMAP");

        material.EnableKeyword(
            "_METALLICSPECGLOSSMAP");

        material.EnableKeyword(
            "_OCCLUSIONMAP");

        EditorUtility.SetDirty(
            material);

        return
            material;
    }

    private static void BuildPrefab(
        GameObject model,
        Material material)
    {
        GameObject root =
            new(
                "MotorCity_PlayerGarage");

        try
        {
            GameObject visual =
                PrefabUtility.InstantiatePrefab(
                    model) as GameObject;

            if (visual == null)
            {
                visual =
                    UnityEngine.Object.Instantiate(
                        model);
            }

            visual.name =
                "Garage Visual";

            visual.transform.SetParent(
                root.transform,
                false);

            visual.transform.localPosition =
                Vector3.zero;

            visual.transform.localRotation =
                Quaternion.Euler(
                    VisualRotation);

            visual.transform.localScale =
                VisualScale;

            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(
                    true);

            foreach (Renderer renderer in
                     renderers)
            {
                if (renderer == null)
                    continue;

                Material[] slots =
                    renderer.sharedMaterials;

                if (slots == null ||
                    slots.Length == 0)
                {
                    renderer.sharedMaterial =
                        material;

                    continue;
                }

                for (int i = 0;
                     i < slots.Length;
                     i++)
                {
                    slots[i] =
                        material;
                }

                renderer.sharedMaterials =
                    slots;
            }

            MeshFilter[] filters =
                visual.GetComponentsInChildren<MeshFilter>(
                    true);

            foreach (MeshFilter filter in
                     filters)
            {
                if (filter == null ||
                    filter.sharedMesh == null)
                {
                    continue;
                }

                MeshCollider collider =
                    filter.GetComponent<MeshCollider>();

                if (collider == null)
                {
                    collider =
                        filter.gameObject.AddComponent<MeshCollider>();
                }

                collider.sharedMesh =
                    filter.sharedMesh;

                collider.convex =
                    false;
            }

            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                root);
        }
    }

    private static void EnsureFolder(
        string assetPath)
    {
        string normalized =
            assetPath.Replace(
                '\\',
                '/');

        if (AssetDatabase.IsValidFolder(
                normalized))
        {
            return;
        }

        string parent =
            Path.GetDirectoryName(
                    normalized)
                ?.Replace(
                    '\\',
                    '/');

        string name =
            Path.GetFileName(
                normalized);

        if (string.IsNullOrWhiteSpace(
                parent) ||
            string.IsNullOrWhiteSpace(
                name))
        {
            throw new InvalidOperationException(
                "Invalid asset folder path: " +
                assetPath);
        }

        if (parent != "Assets")
        {
            EnsureFolder(
                parent);
        }

        if (!AssetDatabase.IsValidFolder(
                normalized))
        {
            AssetDatabase.CreateFolder(
                parent,
                name);
        }
    }
}
#endif
