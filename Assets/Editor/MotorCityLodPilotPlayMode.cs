using MotorCity.Platform;
using UnityEditor;

namespace MotorCity.EditorTools
{
    public static class MotorCityLodPilotPlayMode
    {
        [MenuItem("Tools/Motor City/Audit/Building LOD Pilot ON")]
        private static void Enable() => MotorCityBuildingLodPilot.SetEnabled(true);

        [MenuItem("Tools/Motor City/Audit/Building LOD Pilot OFF")]
        private static void Disable() => MotorCityBuildingLodPilot.SetEnabled(false);

        [MenuItem("Tools/Motor City/Audit/Building LOD Pilot ON", true)]
        [MenuItem("Tools/Motor City/Audit/Building LOD Pilot OFF", true)]
        private static bool Validate() => EditorApplication.isPlaying;
    }
}
