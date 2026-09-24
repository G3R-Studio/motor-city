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

    private const string DeliveryIconPath =
        "Assets/Art/MotorCity/Markers/KenneyGameIcons/2x/export.png";

    private const string SprintIconPath =
        "Assets/Art/MotorCity/Markers/KenneyGameIcons/2x/fastForward.png";

    private const string CircuitIconPath =
        "Assets/Art/MotorCity/Markers/KenneyGameIcons/2x/trophy.png";

    private const string OutputRoot =
        "Assets/Resources/MotorCity/Markers";

    private const string DriftPrefabPath =
        OutputRoot +
        "/DriftMarkerVfx.prefab";

    private const string DeliveryPrefabPath =
        OutputRoot +
        "/DeliveryMarkerVfx.prefab";

    private const string SprintPrefabPath =
        OutputRoot +
        "/SprintMarkerVfx.prefab";

    private const string CircuitPrefabPath =
        OutputRoot +
        "/CircuitMarkerVfx.prefab";

    private const string LegacyDriftMaterialPath =
        OutputRoot +
        "/DriftMarker_Hologram.mat";

    private static readonly Color DriftOrange =
        new(
            1f,
            0.30f,
            0.035f,
            1f);

    private static readonly Color DeliveryBlue =
        new(
            0.06f,
            0.48f,
            1f,
            1f);

    private static readonly Color SprintGreen =
        new(
            0.08f,
            1f,
            0.28f,
            1f);

    private static readonly Color CircuitCyan =
        new(
            0.04f,
            0.86f,
            1f,
            1f);

    [MenuItem(
        "Motor City/Markers/1 - Build Drift Marker VFX")]
    private static void BuildDriftMarker()
    {
        AssetDatabase.DeleteAsset(
            LegacyDriftMaterialPath);

        BuildMarker(
            "MotorCity_DriftMarkerVfx",
            DriftPrefabPath,
            DriftIconPath,
            "Drift",
            DriftOrange,
            0.62f,
            2.85f,
            1.82f);
    }

    [MenuItem(
        "Motor City/Markers/2 - Build Delivery Marker VFX")]
    private static void BuildDeliveryMarker()
    {
        BuildMarker(
            "MotorCity_DeliveryMarkerVfx",
            DeliveryPrefabPath,
            DeliveryIconPath,
            "Delivery",
            DeliveryBlue,
            0.58f,
            3.05f,
            1.68f);
    }

    [MenuItem(
        "Motor City/Markers/3 - Build Sprint Marker VFX")]
    private static void BuildSprintMarker()
    {
        BuildMarker(
            "MotorCity_SprintMarkerVfx",
            SprintPrefabPath,
            SprintIconPath,
            "Sprint",
            SprintGreen,
            0.60f,
            2.95f,
            1.74f);
    }

    [MenuItem(
        "Motor City/Markers/4 - Build Circuit Marker VFX")]
    private static void BuildCircuitMarker()
    {
        BuildMarker(
            "MotorCity_CircuitMarkerVfx",
            CircuitPrefabPath,
            CircuitIconPath,
            "Circuit",
            CircuitCyan,
            0.60f,
            3.00f,
            1.72f);
    }

    [MenuItem(
        "Motor City/Markers/Build All Marker VFX")]
    private static void BuildAllMarkers()
    {
        BuildDriftMarker();
        BuildDeliveryMarker();
        BuildSprintMarker();
        BuildCircuitMarker();
    }

    private static void BuildMarker(
        string rootName,
        string outputPrefabPath,
        string iconPath,
        string iconLabel,
        Color color,
        float groundScale,
        float iconHeight,
        float iconScale)
    {
        Directory.CreateDirectory(
            OutputRoot);

        if (!PrepareIcon(
                iconPath))
        {
            return;
        }

        GameObject sourceVfx =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                SourceMagicCirclePath);

        Sprite icon =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                iconPath);

        if (sourceVfx == null)
        {
            Debug.LogError(
                "Motor City: Magic Circle source prefab was not found: " +
                SourceMagicCirclePath);

            return;
        }

        if (icon == null)
        {
            Debug.LogError(
                "Motor City: marker icon could not be imported as a Sprite: " +
                iconPath);

            return;
        }

        GameObject root =
            new(
                rootName);

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
                groundScale;

            ConfigureGroundVfx(
                groundVfx,
                color);

            Transform iconRoot =
                CreateMissionIcon(
                    root.transform,
                    icon,
                    iconLabel,
                    color,
                    iconHeight,
                    iconScale);

            ActivityMarkerVfxAnimator animator =
                root.AddComponent<
                    ActivityMarkerVfxAnimator>();

            animator.Configure(
                iconRoot);

            GameObject saved =
                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    outputPrefabPath);

            if (saved == null)
            {
                Debug.LogError(
                    "Motor City: failed to save activity marker VFX prefab: " +
                    outputPrefabPath);

                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Motor City: built activity marker VFX at '" +
                outputPrefabPath +
                "'.");
        }
        finally
        {
            Object.DestroyImmediate(
                root);
        }
    }

    private static bool PrepareIcon(
        string iconPath)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(
                iconPath) as
                TextureImporter;

        if (importer == null)
        {
            Debug.LogError(
                "Motor City: marker icon TextureImporter is unavailable: " +
                iconPath);

            return
                false;
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

        if (changed)
        {
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

        return
            true;
    }

    private static void ConfigureGroundVfx(
        GameObject groundVfx,
        Color color)
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

            main.startColor =
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    0.86f);
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
        string label,
        Color color,
        float height,
        float scale)
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
                height,
                0f);

        iconObject.transform.localRotation =
            Quaternion.identity;

        iconObject.transform.localScale =
            Vector3.one;

        CreateIconPlane(
            iconObject.transform,
            label +
            " Icon Glow",
            sprite,
            new Color(
                color.r,
                color.g,
                color.b,
                0.22f),
            scale *
            1.20f,
            44);

        CreateIconPlane(
            iconObject.transform,
            label +
            " Icon Core",
            sprite,
            new Color(
                color.r,
                color.g,
                color.b,
                1f),
            scale,
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
