#if UNITY_EDITOR
using System.IO;
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

        static MotorCityProjectSetup()
        {
            EditorApplication.delayCall += ConfigureProject;
        }

        private static void ConfigureProject()
        {
            EnsureFolders();
            EnsureRenderPipeline();
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
                pipeline = UniversalRenderPipelineAsset.Create();
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                AssetDatabase.SaveAssets();
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        private static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Scene current = SceneManager.GetActiveScene();
            if (!current.isDirty && current.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }
    }
}
#endif
