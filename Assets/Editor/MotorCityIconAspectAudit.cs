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

        // Tabler pictograms in garages and vector buttons are drawn by
        // GarageReferenceGraphic, which compensates X/Y scaling itself.
        // World markers use SpriteRenderer and uniform authored plane scale.
        string message = "[MotorCity][Icons] Checked " + count +
            " sprite/texture UI icons; aspect warnings: " + warnings;
        if (warnings > 0)
            Debug.LogWarning(message + "\n" + report);
        else
            Debug.Log(message + ". Proportions preserved.");
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
