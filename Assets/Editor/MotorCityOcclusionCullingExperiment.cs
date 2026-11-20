#if UNITY_EDITOR
using MotorCity.World;
using UnityEditor;
using UnityEngine;

public static class MotorCityOcclusionCullingExperiment
{
    [MenuItem("Tools/Motor City/Occlusion Culling/Start Play Mode Pilot")]
    private static void StartPilot()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Occlusion Culling Pilot",
                "Start Play Mode first, wait until the city and player camera appear, then run this command.",
                "OK");
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
            camera = Object.FindFirstObjectByType<Camera>();

        if (camera == null)
        {
            Debug.LogWarning("[Motor City Occlusion] No camera found. Pilot was not started.");
            return;
        }

        if (camera.GetComponent<CityOcclusionCullingPilot>() != null)
        {
            Debug.Log("[Motor City Occlusion] Pilot is already running.");
            return;
        }

        camera.gameObject.AddComponent<CityOcclusionCullingPilot>();
    }

    [MenuItem("Tools/Motor City/Occlusion Culling/Start Play Mode Pilot", true)]
    private static bool ValidateStart()
    {
        return EditorApplication.isPlaying;
    }

    [MenuItem("Tools/Motor City/Occlusion Culling/Stop and Restore")]
    private static void StopPilot()
    {
        CityOcclusionCullingPilot[] pilots =
            Object.FindObjectsByType<CityOcclusionCullingPilot>(
                FindObjectsSortMode.None);

        foreach (CityOcclusionCullingPilot pilot in pilots)
        {
            if (pilot != null)
                Object.Destroy(pilot);
        }

        Debug.Log(
            "[Motor City Occlusion] Pilot stopped. Renderer flags restored. " +
            "If the city still looks wrong, exit Play Mode to restore the original scene state.");
    }

    [MenuItem("Tools/Motor City/Occlusion Culling/Stop and Restore", true)]
    private static bool ValidateStop()
    {
        return EditorApplication.isPlaying &&
            Object.FindFirstObjectByType<CityOcclusionCullingPilot>() != null;
    }
}
#endif
