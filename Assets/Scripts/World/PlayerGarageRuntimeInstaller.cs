using UnityEngine;

namespace MotorCity.World
{
    public static class PlayerGarageRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/Garage/MotorCity_PlayerGarage";

        private const string RuntimeName =
            "MotorCity_PlayerGarage_Runtime";

        private static readonly Vector3 WorldPosition =
            new(
                -585.822021f,
                3.49000001f,
                505.109009f);

        public static bool TryInstall()
        {
            GameObject existing =
                GameObject.Find(
                    RuntimeName);

            if (existing != null)
                return true;

            GameObject prefab =
                Resources.Load<GameObject>(
                    ResourcePath);

            if (prefab == null)
            {
                Debug.LogWarning(
                    "Motor City: player garage prefab is missing. " +
                    "Run Motor City/Garage/1 - Build Player Garage Assets in the Editor.");

                return false;
            }

            GameObject instance =
                Object.Instantiate(
                    prefab,
                    WorldPosition,
                    Quaternion.identity);

            instance.name =
                RuntimeName;

            return true;
        }
    }
}
