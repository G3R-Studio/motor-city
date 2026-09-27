#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    [InitializeOnLoad]
    public static class BeatallCleanImporter
    {
        private const string BodySourcePath =
            "Assets/Vehicles/Imported/Beatall/beatall.obj";

        private const string WheelSourcePath =
            "Assets/Vehicles/Imported/Beatall/wheels.obj";

        private const string TexturePath =
            "Assets/Vehicles/Imported/Beatall/387359c5580f06c08c266126b3b46db47e48ba44.png";

        private const string PrototypeRoot =
            "Assets/MotorCity/VehiclePrototypes/Beatall";

        private const string MaterialRoot =
            PrototypeRoot + "/Materials";

        private const string PrototypePath =
            PrototypeRoot + "/BeatallVisual.prefab";

        private const string ResourceRoot =
            "Assets/Resources/MotorCity/Vehicles/Player";

        private const string ResourcePath =
            ResourceRoot + "/Beatall.prefab";

        static BeatallCleanImporter()
        {
            EditorApplication.delayCall += EnsureBuilt;
        }

        [MenuItem("Motor City/Vehicles/Rebuild Beatall Clean")]
        public static void Rebuild()
        {
            Build(true);
        }

        private static void EnsureBuilt()
        {
            GameObject prototype =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PrototypePath);

            GameObject resource =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ResourcePath);

            bool complete =
                prototype != null &&
                resource != null &&
                prototype.transform.Find(
                    "RunningGear") != null;

            if (!complete)
                Build(false);
        }

        private static void Build(bool log)
        {
            GameObject bodySource =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    BodySourcePath);

            GameObject wheelSource =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    WheelSourcePath);

            if (bodySource == null ||
                wheelSource == null)
            {
                if (log)
                {
                    Debug.LogWarning(
                        "[MotorCity][Beatall] Body or wheel OBJ is missing.");
                }

                return;
            }

            EnsureFolder(PrototypeRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(ResourceRoot);

            GameObject instance =
                Object.Instantiate(bodySource);

            instance.name = "BeatallVisual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            try
            {
                ApplyUrpMaterials(instance);
                AddAuthoredWheels(
                    instance,
                    wheelSource);

                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    PrototypePath);

                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    ResourcePath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (log)
                {
                    Debug.Log(
                        "[MotorCity][Beatall] Rebuilt visual with authored transforms only.");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AddAuthoredWheels(
            GameObject car,
            GameObject wheelSource)
        {
            Transform old =
                car.transform.Find("RunningGear");

            if (old != null)
                Object.DestroyImmediate(
                    old.gameObject);

            GameObject runningGear =
                new("RunningGear");

            runningGear.transform.SetParent(
                car.transform,
                false);

            runningGear.transform.localPosition =
                Vector3.zero;

            runningGear.transform.localRotation =
                Quaternion.identity;

            runningGear.transform.localScale =
                Vector3.one;

            // The imported wheel asset is authored for the RIGHT side.
            // Duplicate that exact mesh four times. Right-side wheels keep
            // their authored rotation; left-side wheels are flipped 180° on Y.
            // Wheel centres are solved directly from the circular arch-edge
            // vertices in beatall.obj. The fitted arch radii (~0.294 rear and
            // ~0.300 front) match the authored wheel radius (~0.292).
            // The body itself is never moved, scaled or rotated.
            CreateWheel(
                wheelSource,
                runningGear.transform,
                "Front Left Wheel",
                new Vector3(
                    -0.605f,
                    0.25447f,
                    1.06565f),
                true);

            CreateWheel(
                wheelSource,
                runningGear.transform,
                "Front Right Wheel",
                new Vector3(
                    0.605f,
                    0.25447f,
                    1.06565f),
                false);

            CreateWheel(
                wheelSource,
                runningGear.transform,
                "Rear Left Wheel",
                new Vector3(
                    -0.605f,
                    0.26095f,
                    -0.95573f),
                true);

            CreateWheel(
                wheelSource,
                runningGear.transform,
                "Rear Right Wheel",
                new Vector3(
                    0.605f,
                    0.26095f,
                    -0.95573f),
                false);
        }

        private static void CreateWheel(
            GameObject source,
            Transform parent,
            string name,
            Vector3 localPosition,
            bool leftSide)
        {
            GameObject wheel =
                Object.Instantiate(source);

            wheel.name =
                name;

            wheel.transform.SetParent(
                parent,
                false);

            wheel.transform.localPosition =
                localPosition;

            wheel.transform.localRotation =
                leftSide
                    ? Quaternion.Euler(
                        0f,
                        180f,
                        0f)
                    : Quaternion.identity;

            wheel.transform.localScale =
                Vector3.one;

            ApplyUrpMaterials(
                wheel);
        }

        private static void ApplyUrpMaterials(
            GameObject root)
        {
            Shader lit =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            if (lit == null)
                return;

            Texture texture =
                AssetDatabase.LoadAssetAtPath<Texture>(
                    TexturePath);

            foreach (Renderer renderer in
                     root.GetComponentsInChildren<Renderer>(
                         true))
            {
                Material[] sourceMaterials =
                    renderer.sharedMaterials;

                Material[] assigned =
                    new Material[
                        sourceMaterials.Length];

                for (int i = 0;
                     i < sourceMaterials.Length;
                     i++)
                {
                    string lower =
                        sourceMaterials[i] == null
                            ? string.Empty
                            : sourceMaterials[i]
                                .name
                                .ToLowerInvariant();

                    if (lower.Contains(
                            "blackglass"))
                    {
                        assigned[i] =
                            GetOrCreateMaterial(
                                "Beatall_BlackGlass",
                                lit,
                                new Color(
                                    0.015f,
                                    0.02f,
                                    0.025f,
                                    1f),
                                null,
                                0.05f,
                                0.82f);
                    }
                    else if (
                        lower.Contains(
                            "gradientemmisive") ||
                        lower.Contains(
                            "gradientemissive"))
                    {
                        assigned[i] =
                            GetOrCreateMaterial(
                                "Beatall_EmissiveDetails",
                                lit,
                                Color.white,
                                texture,
                                0f,
                                0.42f);
                    }
                    else
                    {
                        assigned[i] =
                            GetOrCreateMaterial(
                                "Beatall_BaseGradient",
                                lit,
                                Color.white,
                                texture,
                                0f,
                                0.38f);
                    }
                }

                renderer.sharedMaterials =
                    assigned;
            }
        }

        private static Material GetOrCreateMaterial(
            string name,
            Shader shader,
            Color color,
            Texture texture,
            float metallic,
            float smoothness)
        {
            string path =
                MaterialRoot + "/" +
                name +
                ".mat";

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            if (material == null)
            {
                material =
                    new Material(shader)
                    {
                        name = name
                    };

                AssetDatabase.CreateAsset(
                    material,
                    path);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty(
                    "_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    color);
            }

            if (material.HasProperty(
                    "_BaseMap"))
            {
                material.SetTexture(
                    "_BaseMap",
                    texture);
            }

            if (material.HasProperty(
                    "_Metallic"))
            {
                material.SetFloat(
                    "_Metallic",
                    metallic);
            }

            if (material.HasProperty(
                    "_Smoothness"))
            {
                material.SetFloat(
                    "_Smoothness",
                    smoothness);
            }

            if (material.HasProperty(
                    "_EmissionColor"))
            {
                material.SetColor(
                    "_EmissionColor",
                    Color.black);
            }

            material.DisableKeyword(
                "_EMISSION");

            EditorUtility.SetDirty(
                material);

            return material;
        }

        private static Bounds CalculateRendererBounds(
            GameObject root)
        {
            Renderer[] renderers =
                root.GetComponentsInChildren<Renderer>(
                    true);

            if (renderers.Length == 0)
            {
                return new Bounds(
                    Vector3.zero,
                    Vector3.one);
            }

            Transform rootTransform =
                root.transform;

            bool initialized =
                false;

            Bounds localBounds =
                new(
                    Vector3.zero,
                    Vector3.zero);

            foreach (Renderer renderer in
                     renderers)
            {
                Bounds world =
                    renderer.bounds;

                Vector3 min =
                    world.min;

                Vector3 max =
                    world.max;

                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0;
                         y < 2;
                         y++)
                    {
                        for (int z = 0;
                             z < 2;
                             z++)
                        {
                            Vector3 corner =
                                new(
                                    x == 0
                                        ? min.x
                                        : max.x,
                                    y == 0
                                        ? min.y
                                        : max.y,
                                    z == 0
                                        ? min.z
                                        : max.z);

                            Vector3 local =
                                rootTransform
                                    .InverseTransformPoint(
                                        corner);

                            if (!initialized)
                            {
                                localBounds =
                                    new Bounds(
                                        local,
                                        Vector3.zero);

                                initialized =
                                    true;
                            }
                            else
                            {
                                localBounds
                                    .Encapsulate(
                                        local);
                            }
                        }
                    }
                }
            }

            return localBounds;
        }

        private static void EnsureFolder(
            string path)
        {
            if (AssetDatabase.IsValidFolder(
                    path))
            {
                return;
            }

            string parent =
                Path.GetDirectoryName(path)
                    ?.Replace(
                        '\\',
                        '/');

            string name =
                Path.GetFileName(path);

            if (!string.IsNullOrEmpty(
                    parent))
            {
                EnsureFolder(
                    parent);
            }

            AssetDatabase.CreateFolder(
                parent,
                name);
        }
    }
}
#endif
