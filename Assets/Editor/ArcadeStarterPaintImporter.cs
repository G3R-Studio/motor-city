#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ArcadeStarterPaintImporter
{
    private const string OutputDirectory =
        "Assets/Resources/MotorCity/VehiclePaints";

    private const string SessionKey =
        "MotorCity.StarterPaintsBuilt.V1";

    private static readonly string[] SourceMaterials =
    {
        "Assets/ARCADE - FREE Racing Car/Materials/Color Variations/AFRC_Mat_Col1.mat",
        "Assets/ARCADE - FREE Racing Car/Materials/Color Variations/AFRC_Mat_Col2.mat",
        "Assets/ARCADE - FREE Racing Car/Materials/Color Variations/AFRC_Mat_Col3.mat",
        "Assets/ARCADE - FREE Racing Car/Materials/Color Variations/AFRC_Mat_Col4.mat",
        "Assets/ARCADE - FREE Racing Car/Materials/Color Variations/AFRC_Mat_Col5.mat"
    };

    static ArcadeStarterPaintImporter()
    {
        EditorApplication.delayCall += TryBuild;
    }

    [MenuItem("Motor City/Vehicles/Rebuild Starter Paints")]
    private static void Rebuild()
    {
        Build();
    }

    private static void TryBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        Build();
    }

    private static void Build()
    {
        Directory.CreateDirectory(OutputDirectory);

        Shader urp =
            Shader.Find("Universal Render Pipeline/Lit");

        if (urp == null)
            return;

        for (int i = 0; i < SourceMaterials.Length; i++)
        {
            Material source =
                AssetDatabase.LoadAssetAtPath<Material>(
                    SourceMaterials[i]);

            if (source == null)
                continue;

            string outputPath =
                $"{OutputDirectory}/StarterPaint_{i}.mat";

            Material target =
                AssetDatabase.LoadAssetAtPath<Material>(
                    outputPath);

            if (target == null)
            {
                target =
                    new Material(urp);

                AssetDatabase.CreateAsset(
                    target,
                    outputPath);
            }
            else
            {
                target.shader = urp;
            }

            Texture texture =
                source.HasProperty("_MainTex")
                    ? source.GetTexture("_MainTex")
                    : null;

            if (target.HasProperty("_BaseMap"))
                target.SetTexture("_BaseMap", texture);

            if (target.HasProperty("_BaseColor"))
                target.SetColor("_BaseColor", Color.white);

            if (target.HasProperty("_Metallic"))
                target.SetFloat(
                    "_Metallic",
                    source.HasProperty("_Metallic")
                        ? source.GetFloat("_Metallic")
                        : 0.075f);

            if (target.HasProperty("_Smoothness"))
                target.SetFloat(
                    "_Smoothness",
                    source.HasProperty("_Glossiness")
                        ? source.GetFloat("_Glossiness")
                        : 0.3f);

            target.name =
                $"MotorCity_StarterPaint_{i + 1}";

            EditorUtility.SetDirty(target);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
