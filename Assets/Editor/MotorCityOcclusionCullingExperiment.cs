#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Editor-only, reversible pilot for Unity's native GPU Resident Drawer
/// occlusion culling. It modifies only MotorCityQualityRuntime's transient
/// Play Mode URP clone; serialized project assets are never changed.
/// Requires a compute-capable graphics API and a Forward+/Deferred+ renderer.
/// </summary>
[InitializeOnLoad]
public static class MotorCityOcclusionCullingExperiment
{
    private static UniversalRenderPipelineAsset activePipeline;
    private static GPUResidentDrawerMode originalDrawerMode;
    private static bool originalGpuOcclusion;

    static MotorCityOcclusionCullingExperiment()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= Restore;
        AssemblyReloadEvents.beforeAssemblyReload += Restore;
        EditorApplication.quitting -= Restore;
        EditorApplication.quitting += Restore;
    }

    [MenuItem("Tools/Motor City/Occlusion Culling/Start Native GPU Pilot")]
    private static void StartPilot()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "Start Play Mode first and wait until the player camera and city have loaded.",
                "OK");
            return;
        }

        if (activePipeline != null)
        {
            Debug.Log("[Motor City Native Occlusion] Pilot is already active.");
            return;
        }

        if (!SystemInfo.supportsComputeShaders)
        {
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "This graphics API does not support compute shaders. " +
                "Unity GPU Occlusion cannot run here. WebGL 2 does not support compute shaders; " +
                "test on a compute-capable desktop API such as Direct3D 11/12, Vulkan or Metal.",
                "OK");
            return;
        }

        // MotorCityQualityRuntime creates this hidden transient copy in Editor
        // Play Mode. Refuse to touch the serialized project URP asset.
        UniversalRenderPipelineAsset pipeline =
            QualitySettings.renderPipeline as UniversalRenderPipelineAsset;

        if (pipeline == null ||
            !pipeline.name.EndsWith(" (Play Mode)", System.StringComparison.Ordinal) ||
            (pipeline.hideFlags & HideFlags.HideAndDontSave) == 0)
        {
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "The temporary Motor City Play Mode URP asset was not found. " +
                "No project asset was changed. Wait for the quality runtime to initialize, then try again.",
                "OK");
            return;
        }

        UniversalRendererData rendererData = GetDefaultRendererData(pipeline);
        if (rendererData == null)
        {
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "Could not resolve the active Universal Renderer. No settings were changed.",
                "OK");
            return;
        }

        if (rendererData.renderingMode != RenderingMode.ForwardPlus &&
            rendererData.renderingMode != RenderingMode.DeferredPlus)
        {
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "GPU Resident Drawer requires Forward+ or Deferred+. " +
                "Current renderer mode: " + rendererData.renderingMode +
                ". No settings were changed.",
                "OK");
            return;
        }

        originalDrawerMode = pipeline.gpuResidentDrawerMode;
        originalGpuOcclusion = pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras;
        activePipeline = pipeline;

        try
        {
            pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
            pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras = true;

            Debug.Log(
                "[Motor City Native Occlusion] Unity GPU Resident Drawer pilot ENABLED. " +
                "GPU Occlusion=true; Renderer=" + rendererData.renderingMode +
                "; API=" + SystemInfo.graphicsDeviceType +
                "; ComputeShaders=" + SystemInfo.supportsComputeShaders +
                "; SRPBatcher=" + pipeline.useSRPBatcher +
                ". This changes only the transient Play Mode URP clone. " +
                "Use Stop and Restore or exit Play Mode to revert.");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Restore();
            EditorUtility.DisplayDialog(
                "Native GPU Occlusion Pilot",
                "Unity could not enable GPU Resident Drawer. Original temporary settings were restored.\n\n" +
                exception.Message,
                "OK");
        }
    }

    [MenuItem("Tools/Motor City/Occlusion Culling/Stop and Restore")]
    private static void StopPilot()
    {
        Restore();
    }

    [MenuItem("Tools/Motor City/Occlusion Culling/Stop and Restore", true)]
    private static bool ValidateStop()
    {
        return EditorApplication.isPlaying && activePipeline != null;
    }

    private static UniversalRendererData GetDefaultRendererData(
        UniversalRenderPipelineAsset pipeline)
    {
        var serialized = new SerializedObject(pipeline);
        SerializedProperty list = serialized.FindProperty("m_RendererDataList");
        SerializedProperty defaultIndex = serialized.FindProperty("m_DefaultRendererIndex");

        if (list == null || defaultIndex == null ||
            list.arraySize == 0)
            return null;

        int index = Mathf.Clamp(defaultIndex.intValue, 0, list.arraySize - 1);
        return list.GetArrayElementAtIndex(index).objectReferenceValue
            as UniversalRendererData;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode ||
            state == PlayModeStateChange.EnteredEditMode)
            Restore();
    }

    private static void Restore()
    {
        if (activePipeline == null)
            return;

        // This is a transient Play Mode copy, never the serialized project asset.
        if (activePipeline != null)
        {
            activePipeline.gpuResidentDrawerMode = originalDrawerMode;
            activePipeline.gpuResidentDrawerEnableOcclusionCullingInCameras =
                originalGpuOcclusion;
        }

        activePipeline = null;
        Debug.Log(
            "[Motor City Native Occlusion] Native GPU pilot stopped. " +
            "Temporary GPU Resident Drawer and GPU Occlusion settings restored.");
    }
}
#endif
