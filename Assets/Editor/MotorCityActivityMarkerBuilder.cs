using System.IO;
using MotorCity.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MotorCityActivityMarkerBuilder
{
    private const string SourceMagicCirclePath =
        "Assets/Eric VFX Studio/Game VFX - Magic Circle(Free)/Prefabs/FX_MagicCircle_Icearrow01.prefab";

    private const string SourceHologramMaterialPath =
        "Assets/VOiD1 Gaming - 2D Hologram Shader Unity URP/Material/Shader Graphs_2D Dissolve Shader.mat";

    private const string DriftIconPath =
        "Assets/Art/MotorCity/Markers/KenneyGameIcons/2x/return.png";

    private const string OutputRoot =
        "Assets/Resources/MotorCity/Markers";

    private const string OutputPrefabPath =
        OutputRoot +
        "/DriftMarkerVfx.prefab";

    private const string OutputMaterialPath =
        OutputRoot +
        "/DriftMarker_Hologram.mat";

    private static readonly Color DriftOrange =
        new(
            1f,
            0.30f,
            0.035f,
            1f);

    [MenuItem(
        "Motor City/Markers/1 - Build Drift Marker VFX")]
    private static void BuildDriftMarker()
    {
        Directory.CreateDirectory(
            OutputRoot);

        PrepareDriftIcon();

        GameObject sourceVfx =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                SourceMagicCirclePath);

        Material sourceHologram =
            AssetDatabase.LoadAssetAtPath<Material>(
                SourceHologramMaterialPath);

        Sprite driftIcon =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                DriftIconPath);

        if (sourceVfx == null)
        {
            Debug.LogError(
                "Motor City: Magic Circle source prefab was not found: " +
                SourceMagicCirclePath);

            return;
        }

        if (sourceHologram == null)
        {
            Debug.LogError(
                "Motor City: hologram source material was not found: " +
                SourceHologramMaterialPath);

            return;
        }

        if (driftIcon == null)
        {
            Debug.LogError(
                "Motor City: drift icon could not be imported as a Sprite: " +
                DriftIconPath);

            return;
        }

        Material hologramMaterial =
            BuildHologramMaterial(
                sourceHologram);

        if (hologramMaterial == null)
            return;

        GameObject root =
            new(
                "MotorCity_DriftMarkerVfx");

        try
        {
            GameObject groundVfx =
                PrefabUtility.InstantiatePrefab(
                    sourceVfx,
                    root.transform) as
                    GameObject;

            if (groundVfx == null)
            {
                groundVfx =
                    Object.Instantiate(
                        sourceVfx,
                        root.transform);
            }

            if (groundVfx == null)
            {
                Debug.LogError(
                    "Motor City: failed to instantiate Magic Circle source.");

                return;
            }

            groundVfx.name =
                "Ground VFX";

            groundVfx.transform.localPosition =
                new Vector3(
                    0f,
                    0.035f,
                    0f);

            groundVfx.transform.localRotation =
                Quaternion.identity;

            groundVfx.transform.localScale =
                Vector3.one *
                0.62f;

            ConfigureGroundVfx(
                groundVfx);

            Transform iconRoot =
                CreateMissionIcon(
                    root.transform,
                    driftIcon,
                    hologramMaterial);

            ActivityMarkerVfxAnimator animator =
                root.AddComponent<
                    ActivityMarkerVfxAnimator>();

            animator.Configure(
                iconRoot);

            GameObject saved =
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    OutputPrefabPath);

            if (saved == null)
            {
                Debug.LogError(
                    "Motor City: failed to save drift marker VFX prefab.");

                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Motor City: built drift marker VFX at '" +
                OutputPrefabPath +
                "'.");
        }
        finally
        {
            Object.DestroyImmediate(
                root);
        }
    }

    private static void PrepareDriftIcon()
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(
                DriftIconPath) as
                TextureImporter;

        if (importer == null)
        {
            Debug.LogError(
                "Motor City: drift icon TextureImporter is unavailable: " +
                DriftIconPath);

            return;
        }

        bool changed =
            importer.textureType !=
                TextureImporterType.Sprite ||
            importer.spriteImportMode !=
                SpriteImportMode.Single ||
            importer.mipmapEnabled ||
            !importer.alphaIsTransparency ||
            importer.maxTextureSize !=
                256;

        if (!changed)
            return;

        importer.textureType =
            TextureImporterType.Sprite;

        importer.spriteImportMode =
            SpriteImportMode.Single;

        importer.mipmapEnabled =
            false;

        importer.alphaIsTransparency =
            true;

        importer.wrapMode =
            TextureWrapMode.Clamp;

        importer.filterMode =
            FilterMode.Bilinear;

        importer.maxTextureSize =
            256;

        importer.SaveAndReimport();
    }

    private static Material BuildHologramMaterial(
        Material source)
    {
        AssetDatabase.DeleteAsset(
            OutputMaterialPath);

        Material material =
            new(source)
            {
                name =
                    "MotorCity_DriftMarker_Hologram"
            };

        if (material.HasProperty(
                "Color_7C878D04"))
        {
            material.SetColor(
                "Color_7C878D04",
                new Color(
                    1.45f,
                    0.34f,
                    0.035f,
                    0.92f));
        }

        if (material.HasProperty(
                "Vector1_990D825D"))
        {
            material.SetFloat(
                "Vector1_990D825D",
                0.12f);
        }

        if (material.HasProperty(
                "Vector2_C409DFC2"))
        {
            material.SetVector(
                "Vector2_C409DFC2",
                new Vector4(
                    3f,
                    5f,
                    0f,
                    0f));
        }

        AssetDatabase.CreateAsset(
            material,
            OutputMaterialPath);

        return
            material;
    }

    private static void ConfigureGroundVfx(
        GameObject groundVfx)
    {
        foreach (Collider collider in
                 groundVfx.GetComponentsInChildren<Collider>(
                     true))
        {
            if (collider != null)
            {
                Object.DestroyImmediate(
                    collider);
            }
        }

        foreach (ParticleSystem particles in
                 groundVfx.GetComponentsInChildren<ParticleSystem>(
                     true))
        {
            if (particles == null)
                continue;

            ParticleSystem.MainModule main =
                particles.main;

            Color particleColor =
                new(
                    DriftOrange.r,
                    DriftOrange.g,
                    DriftOrange.b,
                    0.86f);

            main.startColor =
                particleColor;
        }

        foreach (Renderer renderer in
                 groundVfx.GetComponentsInChildren<Renderer>(
                     true))
        {
            if (renderer == null)
                continue;

            renderer.shadowCastingMode =
                ShadowCastingMode.Off;

            renderer.receiveShadows =
                false;
        }
    }

    private static Transform CreateMissionIcon(
        Transform parent,
        Sprite sprite,
        Material material)
    {
        GameObject iconObject =
            new(
                "Mission Icon");

        iconObject.transform.SetParent(
            parent,
            false);

        iconObject.transform.localPosition =
            new Vector3(
                0f,
                2.75f,
                0f);

        iconObject.transform.localRotation =
            Quaternion.identity;

        iconObject.transform.localScale =
            Vector3.one;

        CreateIconPlane(
            iconObject.transform,
            "Drift Hologram",
            sprite,
            material,
            Quaternion.identity);

        return
            iconObject.transform;
    }

    private static void CreateIconPlane(
        Transform parent,
        string name,
        Sprite sprite,
        Material material,
        Quaternion localRotation)
    {
        GameObject plane =
            new(name);

        plane.transform.SetParent(
            parent,
            false);

        plane.transform.localPosition =
            Vector3.zero;

        plane.transform.localRotation =
            localRotation;

        plane.transform.localScale =
            Vector3.one *
            1.62f;

        SpriteRenderer renderer =
            plane.AddComponent<SpriteRenderer>();

        renderer.sprite =
            sprite;

        renderer.sharedMaterial =
            material;

        renderer.color =
            Color.white;

        renderer.shadowCastingMode =
            ShadowCastingMode.Off;

        renderer.receiveShadows =
            false;

        renderer.sortingOrder =
            45;
    }
}
