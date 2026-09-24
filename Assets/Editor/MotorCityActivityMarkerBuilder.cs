using System.IO;
using MotorCity.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MotorCityActivityMarkerBuilder
{
    private const string SourceMagicCirclePath =
        "Assets/Eric VFX Studio/Game VFX - Magic Circle(Free)/Prefabs/FX_MagicCircle_Icearrow01.prefab";

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

        if (driftIcon == null)
        {
            Debug.LogError(
                "Motor City: drift icon could not be imported as a Sprite: " +
                DriftIconPath);

            return;
        }

        AssetDatabase.DeleteAsset(
            OutputMaterialPath);

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
                    driftIcon);

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
        Sprite sprite)
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
                2.85f,
                0f);

        iconObject.transform.localRotation =
            Quaternion.identity;

        iconObject.transform.localScale =
            Vector3.one;

        CreateIconPlane(
            iconObject.transform,
            "Drift Icon Glow",
            sprite,
            new Color(
                1f,
                0.24f,
                0.015f,
                0.22f),
            2.18f,
            44);

        CreateIconPlane(
            iconObject.transform,
            "Drift Icon Core",
            sprite,
            new Color(
                1f,
                0.36f,
                0.025f,
                1f),
            1.82f,
            45);

        return
            iconObject.transform;
    }

    private static void CreateIconPlane(
        Transform parent,
        string name,
        Sprite sprite,
        Color color,
        float scale,
        int sortingOrder)
    {
        GameObject plane =
            new(name);

        plane.transform.SetParent(
            parent,
            false);

        plane.transform.localPosition =
            Vector3.zero;

        plane.transform.localRotation =
            Quaternion.identity;

        plane.transform.localScale =
            Vector3.one *
            scale;

        SpriteRenderer renderer =
            plane.AddComponent<SpriteRenderer>();

        renderer.sprite =
            sprite;

        renderer.color =
            color;

        renderer.shadowCastingMode =
            ShadowCastingMode.Off;

        renderer.receiveShadows =
            false;

        renderer.sortingOrder =
            sortingOrder;
    }
}
