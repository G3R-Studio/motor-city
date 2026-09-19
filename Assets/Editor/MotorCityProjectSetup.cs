#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MotorCity.Editor
{
    [InitializeOnLoad]
    public static class MotorCityProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        private const string PipelinePath = "Assets/Settings/MotorCityURP.asset";
        private const string RendererPath = "Assets/Settings/MotorCityRenderer.asset";

        static MotorCityProjectSetup()
        {
            EditorApplication.delayCall += ConfigureProject;
        }

        private static void ConfigureProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EnsureFolders();
            EnsureRenderPipeline();
            EnsureInputSystem();
            EnsureScene();

            PlayerSettings.companyName = "G3R Studio";
            PlayerSettings.productName = "Motor City";
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        private static void EnsureFolders()
        {
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
            if (!Directory.Exists("Assets/Settings")) Directory.CreateDirectory("Assets/Settings");
            AssetDatabase.Refresh();
        }

        private static void EnsureRenderPipeline()
        {
            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
                if (rendererData == null)
                {
                    rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(rendererData, RendererPath);
                }

                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                pipeline.name = "Motor City URP";
                pipeline.supportsHDR = false;
                pipeline.renderScale = 1f;
                pipeline.msaaSampleCount = 2;
                pipeline.shadowDistance = 80f;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                AssetDatabase.SaveAssets();
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        private static void EnsureInputSystem()
        {
            PlayerSettings settings = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            if (settings == null) return;

            SerializedObject serializedSettings = new(settings);
            SerializedProperty activeInputHandler = serializedSettings.FindProperty("activeInputHandler");
            if (activeInputHandler == null ||
                activeInputHandler.intValue == 2)
                return;

            // Prometeo Car Controller reads keyboard input through the legacy
            // UnityEngine.Input API, while the rest of Motor City uses the
            // newer Input System. "Both" keeps both systems available.
            activeInputHandler.intValue = 2;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log(
                "Motor City: Active Input Handling switched to Both for Prometeo + Input System compatibility.");
        }

        private static void EnsureScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!File.Exists(ScenePath))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Scene current = SceneManager.GetActiveScene();
            if (!current.isDirty && current.path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
#endif
