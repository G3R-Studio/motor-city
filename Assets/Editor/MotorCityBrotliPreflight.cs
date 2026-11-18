using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace MotorCity.EditorTools
{
    public static class MotorCityBrotliPreflight
    {
        [MenuItem("Tools/Motor City/Build/Check and Set Brotli")]
        private static void Configure()
        {
            var profile = BuildProfile.GetActiveBuildProfile();
            Debug.Log("Motor City: profile=" + (profile == null ? "global" : AssetDatabase.GetAssetPath(profile)) +
                "; compression before=" + PlayerSettings.WebGL.compressionFormat +
                "; development=" + EditorUserBuildSettings.development);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
            AssetDatabase.SaveAssets();
            Debug.Log("Motor City: compression after=" + PlayerSettings.WebGL.compressionFormat +
                "; fallback=" + PlayerSettings.WebGL.decompressionFallback);
        }
    }
}
