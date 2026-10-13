using System;
using System.Text;
using MotorCity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MotorCityIconAspectAudit
{
    private const string TablerFolder =
        "Assets/Resources/MotorCity/UI/Icons/Tabler/";

    [MenuItem("Motor City/UI/Audit Icon Aspect Ratios (Play Mode)")]
    private static void Audit()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[MotorCity][Icons] Enter Play Mode and open " +
                "the screens you want to test before running the icon audit.");
            return;
        }

        int count = 0;
        int warnings = 0;
        StringBuilder report = new StringBuilder();

        foreach (Image image in UnityEngine.Object.FindObjectsByType<Image>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image == null || !image.gameObject.scene.IsValid() ||
                image.sprite == null || image.type != Image.Type.Simple)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(image.sprite);
            bool fromTabler = assetPath.StartsWith(
                TablerFolder, StringComparison.OrdinalIgnoreCase);
            bool iconName = image.name.IndexOf(
                "Icon", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!fromTabler && !iconName)
                continue;

            count++;
            // Previously the test only counted custom graphics, and missed
            // icons silently falling back to hand-drawn procedural meshes.
            // Ensure each Garage upgrade has a genuine imported Tabler PNG.
            if (image.name == "Upgrade Icon")
            {
                if (!fromTabler)
                {
                    warnings++;
                    report.AppendLine("Garage upgrade is not a Tabler sprite: " +
                        ImagePath(image.transform) + " (" + assetPath + ")");
                }
                if (image.sprite.rect.width <= 0f ||
                    image.sprite.rect.height <= 0f)
                {
                    warnings++;
                    report.AppendLine("Invalid sprite rectangle: " +
                        ImagePath(image.transform));
                }
            }

            if (!image.preserveAspect)
            {
                warnings++;
                report.AppendLine("Sprite aspect disabled: " +
                    ImagePath(image.transform));
            }

            RectTransform rect = image.rectTransform;
            float x = rect.TransformVector(Vector3.right).magnitude;
            float y = rect.TransformVector(Vector3.up).magnitude;
            bool nonUniform = x > 0.0001f && y > 0.0001f &&
                Mathf.Abs(x / y - 1f) > 0.02f;
            if (nonUniform &&
                image.GetComponent<MotorCityIconAspectGuard>() == null)
            {
                warnings++;
                report.AppendLine("Unequal X/Y inherited scale: " +
                    ImagePath(image.transform));
            }
        }

        foreach (RawImage image in UnityEngine.Object.FindObjectsByType<RawImage>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image == null || !image.gameObject.scene.IsValid() ||
                image.texture == null)
                continue;

            bool iconName =
                image.name.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                image.name.IndexOf("Pointer", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!iconName)
                continue;

            count++;
            RectTransform rect = image.rectTransform;
            float x = rect.rect.width *
                rect.TransformVector(Vector3.right).magnitude;
            float y = rect.rect.height *
                rect.TransformVector(Vector3.up).magnitude;
            float source = image.texture.width /
                (float)Mathf.Max(1, image.texture.height);
            if (y > 0.0001f && source > 0f &&
                Mathf.Abs(x / y / source - 1f) > 0.03f)
            {
                warnings++;
                report.AppendLine("Raw icon is stretched: " +
                    ImagePath(image.transform));
            }
        }

        int vectorCount = 0;
        foreach (GarageReferenceGraphic graphic in
                 UnityEngine.Object.FindObjectsByType<GarageReferenceGraphic>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (graphic == null || !graphic.gameObject.scene.IsValid() ||
                graphic.symbol == GarageReferenceGraphic.Symbol.Surface ||
                graphic.symbol == GarageReferenceGraphic.Symbol.CitySurface ||
                graphic.symbol == GarageReferenceGraphic.Symbol.NavigationLeft ||
                graphic.symbol == GarageReferenceGraphic.Symbol.NavigationRight)
                continue;

            vectorCount++;
            string tablerName = graphic.TablerIconName;
            if (!string.IsNullOrEmpty(tablerName))
            {
                Sprite source = MotorCityIconLibrary.TryGetTabler(tablerName);
                if (source == null)
                {
                    warnings++;
                    report.AppendLine("Procedural icon fallback instead of " +
                        "Tabler '" + tablerName + "': " +
                        ImagePath(graphic.transform));
                }
            }
        }

        int worldCount = 0;
        foreach (SpriteRenderer renderer in
                 UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (renderer == null || !renderer.gameObject.scene.IsValid() ||
                renderer.sprite == null)
                continue;

            bool markerIcon =
                renderer.name.IndexOf("Icon Core",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                renderer.name.IndexOf("Icon Glow",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                renderer.name.IndexOf("Sprint Flag",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                renderer.name.IndexOf("Circuit Flag",
                    StringComparison.OrdinalIgnoreCase) >= 0;
            if (!markerIcon)
                continue;

            worldCount++;
            float scaleX =
                renderer.transform.TransformVector(Vector3.right).magnitude;
            float scaleY =
                renderer.transform.TransformVector(Vector3.up).magnitude;
            if (scaleY > 0.0001f &&
                Mathf.Abs(scaleX / scaleY - 1f) > 0.03f)
            {
                warnings++;
                report.AppendLine("World marker uses non-uniform icon scale: " +
                    ImagePath(renderer.transform));
            }
        }

        int upgradeImages = 0;
        foreach (Image image in UnityEngine.Object.FindObjectsByType<Image>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image != null && image.gameObject.scene.IsValid() &&
                image.name == "Upgrade Icon" && image.sprite != null)
                upgradeImages++;
        }

        string message = "[MotorCity][Icons] Checked " + count +
            " sprite/texture UI icons, " + vectorCount +
            " custom graphics, " + worldCount + " world sprites; " +
            "garage upgrade Tabler Image slots: " + upgradeImages +
            "/3; aspect/source warnings: " + warnings;
        if (upgradeImages != 3)
        {
            warnings++;
            report.AppendLine("Expected three directly rendered garage " +
                "upgrade Tabler Images; found " + upgradeImages +
                ". Open the Garage screen before auditing.");
            message += " (upgrade count mismatch)";
        }
        if (warnings > 0)
            Debug.LogWarning(message + "\n" + report);
        else
            Debug.Log(message + ". No detected aspect errors. " +
                "Review the visible art in Game view to confirm styling.");
    }

    private static string ImagePath(Transform node)
    {
        string result = node.name;
        while (node.parent != null && result.Length < 220)
        {
            node = node.parent;
            result = node.name + "/" + result;
        }
        return result;
    }
}
