#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Arms a one-shot native URP GPU Resident Drawer + GPU Occlusion test.
/// The settings are applied to MotorCityQualityRuntime's transient URP clone
/// before Unity activates that pipeline. No serialized asset is modified.
/// </summary>
public static class MotorCityOcclusionCullingExperiment
{
    private const string PilotKey =
        "MotorCity.NativeGpuOcclusionPilot.NextPlayMode";

    [MenuItem("Tools/Motor City/Occlusion Culling/Arm Native GPU Pilot (Next Play Mode)")]
    private static void ArmNativeGpuPilot()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "Stop Play Mode first. The GPU culling systems must be configured before URP initializes.",
                "OK");
            return;
        }

        if (!SystemInfo.supportsComputeShaders)
        {
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "The active Editor graphics API does not support compute shaders.",
                "OK");
            return;
        }

        UniversalRenderPipelineAsset pipeline =
            GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;

        if (pipeline == null)
        {
            pipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        }

        if (pipeline == null)
        {
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "Could not find the project's active URP Asset.",
                "OK");
            return;
        }

        if (!pipeline.useSRPBatcher)
        {
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "SRP Batcher is disabled on the project URP Asset.",
                "OK");
            return;
        }

        UniversalRendererData rendererData = GetDefaultRendererData(pipeline);
        if (rendererData == null ||
            (rendererData.renderingMode != RenderingMode.ForwardPlus &&
             rendererData.renderingMode != RenderingMode.DeferredPlus))
        {
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "The active Universal Renderer must use Forward+ or Deferred+.",
                "OK");
            return;
        }

        EditorPrefs.SetBool(PilotKey, true);
        Debug.Log(
            "[Motor City Native Occlusion] Armed for the NEXT Play Mode. " +
            "Stop/enter Play Mode so MotorCityQualityRuntime can configure the " +
            "transient URP clone before pipeline initialization. " +
            "Graphics API=" + SystemInfo.graphicsDeviceType +
            "; ComputeShaders=" + SystemInfo.supportsComputeShaders +
            "; Renderer=" + rendererData.renderingMode + ".");

        EditorUtility.DisplayDialog(
            "Native GPU Occlusion Pilot",
            "Armed for the next Play Mode. Enter Play Mode now and wait for the city to load. " +
            "The pilot will be consumed once when MotorCityQualityRuntime creates its temporary URP clone.",
            "OK");
    }

    [MenuItem("Tools/Motor City/Occlusion Culling/Arm Native GPU Pilot (Next Play Mode)", true)]
    private static bool ValidateArmNativeGpuPilot()
    {
        return !EditorApplication.isPlaying;
    }

    [MenuItem("Tools/Motor City/Occlusion Culling/Disarm Native GPU Pilot")]
    private static void DisarmNativeGpuPilot()
    {
        EditorPrefs.SetBool(PilotKey, false);
        Debug.Log("[Motor City Native Occlusion] Next Play Mode pilot disarmed.");
    }

    private static UniversalRendererData GetDefaultRendererData(
        UniversalRenderPipelineAsset pipeline)
    {
        var serialized = new SerializedObject(pipeline);
        SerializedProperty list = serialized.FindProperty("m_RendererDataList");
        SerializedProperty defaultIndex = serialized.FindProperty("m_DefaultRendererIndex");

        if (list == null || defaultIndex == null || list.arraySize == 0)
            return null;

        int index = Mathf.Clamp(defaultIndex.intValue, 0, list.arraySize - 1);
        return list.GetArrayElementAtIndex(index).objectReferenceValue
            as UniversalRendererData;
    }
}
#endif
