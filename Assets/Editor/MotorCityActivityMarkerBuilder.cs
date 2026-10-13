using System.IO;
using MotorCity.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MotorCityActivityMarkerBuilder
{
    private const string SourceMagicCirclePath =
        "Assets/Eric VFX Studio/Game VFX - Magic Circle(Free)/Prefabs/FX_MagicCircle_Icearrow01.prefab";

    private const string IconRoot =
        "Assets/Resources/MotorCity/UI/Icons/Tabler/";

    private const string OutputRoot =
        "Assets/Resources/MotorCity/Markers";

    private const string LegacyDriftMaterialPath =
        OutputRoot +
        "/DriftMarker_Hologram.mat";

    [MenuItem(
        "Motor City/Markers/1 - Build Drift Marker VFX")]
    private static void BuildDriftMarker()
    {
        AssetDatabase.DeleteAsset(
            LegacyDriftMaterialPath);

        BuildMarker(
            "Drift",
            "car-turbine.png",
            new Color(1f, 0.30f, 0.035f, 1f),
            0.62f,
            2.85f,
            1.82f);
    }

    [MenuItem(
        "Motor City/Markers/2 - Build Delivery Marker VFX")]
    private static void BuildDeliveryMarker()
    {
        BuildMarker(
            "Delivery",
            "truck-delivery.png",
            new Color(0.06f, 0.48f, 1f, 1f),
            0.58f,
            3.05f,
            1.68f);
    }

    [MenuItem(
        "Motor City/Markers/3 - Build Sprint Marker VFX")]
    private static void BuildSprintMarker()
    {
        BuildMarker(
            "Sprint",
            "flag-3.png",
            new Color(0.08f, 1f, 0.28f, 1f),
            0.60f,
            2.95f,
            1.74f);
    }

    [MenuItem(
        "Motor City/Markers/4 - Build Circuit Marker VFX")]
    private static void BuildCircuitMarker()
    {
        BuildMarker(
            "Circuit",
            "trophy.png",
            new Color(0.04f, 0.86f, 1f, 1f),
            0.60f,
            3.00f,
            1.72f);
    }

    [MenuItem(
        "Motor City/Markers/5 - Build Discovery Marker VFX")]
    private static void BuildDiscoveryMarker()
    {
        BuildMarker(
            "Discovery",
            "map-pin.png",
            new Color(0.72f, 0.28f, 1f, 1f),
            0.52f,
            2.65f,
            1.45f);
    }

    [MenuItem(
        "Motor City/Markers/6 - Build Underground Marker VFX")]
    private static void BuildUndergroundMarker()
    {
        BuildMarker(
            "Underground",
            "moon-stars.png",
            new Color(0.78f, 0.18f, 1f, 1f),
            0.64f,
            2.95f,
            1.72f);
    }

    [MenuItem(
        "Motor City/Markers/7 - Build Profession Marker VFX")]
    private static void BuildProfessionMarker()
    {
        BuildMarker(
            "Profession",
            "briefcase.png",
            new Color(1f, 0.68f, 0.10f, 1f),
            0.56f,
            2.80f,
            1.58f);
    }

    [MenuItem(
        "Motor City/Markers/8 - Build Car Wash Marker VFX")]
    private static void BuildCarWashMarker()
    {
        BuildMarker(
            "CarWash",
            "spray.png",
            new Color(0.10f, 0.82f, 1f, 1f),
            0.58f,
            2.85f,
            1.56f);
    }

    [MenuItem(
        "Motor City/Markers/9 - Build Tow Marker VFX")]
    private static void BuildTowMarker()
    {
        BuildMarker(
            "Tow",
            "car-crane.png",
            new Color(1f, 0.56f, 0.06f, 1f),
            0.58f,
            2.85f,
            1.58f);
    }

    [MenuItem(
        "Motor City/Markers/Build All Marker VFX")]
    private static void BuildAllMarkers()
    {
        BuildDriftMarker();
        BuildDeliveryMarker();
        BuildSprintMarker();
        BuildCircuitMarker();
        BuildDiscoveryMarker();
        BuildUndergroundMarker();
        BuildProfessionMarker();
        BuildCarWashMarker();
        BuildTowMarker();
    }

    private static void BuildMarker(
        string markerName,
        string iconFileName,
        Color color,
        float groundScale,
        float iconHeight,
        float iconScale)
    {
        Directory.CreateDirectory(
            OutputRoot);

        string iconPath =
            IconRoot +
            iconFileName;

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

        if (sourceVfx == null ||
            icon == null)
        {
            Debug.LogError(
                "Motor City: marker VFX source is missing for " +
                markerName +
                ".");

            return;
        }

        string outputPrefabPath =
            OutputRoot +
            "/" +
            markerName +
            "MarkerVfx.prefab";

        GameObject root =
            new(
                "MotorCity_" +
                markerName +
                "MarkerVfx");

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
                return;

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
                    markerName,
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
                    "Motor City: failed to save marker VFX: " +
                    outputPrefabPath);

                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Motor City: built marker VFX at '" +
                outputPrefabPath +
                "'.");
        }
        finally
        {
            Object.DestroyImmediate(
                root);
        }
    }

    private static bool PrepareIcon(string iconPath)
    {
        // The shared MotorCityIconPostprocessor already owns Sprite import
        // settings. Overriding them here would re-import the HD HUD icons
        // with lower resolution and incompatible mip settings.
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (sprite != null)
            return true;

        Debug.LogError("[MotorCity][Markers] Missing Tabler Sprite: " +
            iconPath + ". Check the SVG export workflow.");
        return false;
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
            color,
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

        // Explicit world footprint independent of PNG pixel resolution.
        // Tabler HUD icons may be exported at 192 or 384px.
        float spriteUnits = sprite == null ? 1f : Mathf.Max(
            sprite.bounds.size.x, sprite.bounds.size.y);
        plane.transform.localScale =
            Vector3.one * (scale / Mathf.Max(0.001f, spriteUnits));

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
