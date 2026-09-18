using UnityEngine;

namespace MotorCity.World
{
    public static class CityAssetRuntimeInstaller
    {
        private const string ResourcePath =
            "MotorCity/Environment/CityVisual";

        public static bool TryInstall()
        {
            GameObject prefab =
                Resources.Load<GameObject>(ResourcePath);

            if (prefab == null)
                return false;

            GameObject city =
                Object.Instantiate(prefab);
            city.name = "Motor City — CC0 Asset City";

            AddBuildingColliders(city);
            CreateGroundCollider();

            return true;
        }

        private static void AddBuildingColliders(
            GameObject city)
        {
            Renderer[] renderers =
                city.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;

                string hierarchyName =
                    BuildHierarchyName(renderer.transform);

                if (!hierarchyName.Contains("building"))
                    continue;

                if (renderer.GetComponent<Collider>() != null)
                    continue;

                BoxCollider collider =
                    renderer.gameObject.AddComponent<BoxCollider>();

                collider.center = renderer.localBounds.center;
                collider.size = renderer.localBounds.size;
            }
        }

        private static string BuildHierarchyName(
            Transform transform)
        {
            string value = string.Empty;
            Transform current = transform;

            for (int i = 0;
                 current != null && i < 4;
                 i++)
            {
                value += " " +
                         current.name.ToLowerInvariant();
                current = current.parent;
            }

            return value;
        }

        private static void CreateGroundCollider()
        {
            GameObject ground =
                new("City Ground Physics");

            ground.transform.position =
                new Vector3(0f, -0.22f, 0f);

            BoxCollider collider =
                ground.AddComponent<BoxCollider>();

            collider.size =
                new Vector3(378f, 0.4f, 378f);
        }
    }
}
