using MotorCity.CameraSystem;
using MotorCity.UI;
using MotorCity.Vehicle;
using MotorCity.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.Bootstrap
{
    public static class MotorCityBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildPrototype()
        {
            if (Object.FindFirstObjectByType<ArcadeCarController>() != null) return;

            Time.fixedDeltaTime = 1f / 60f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.31f, 0.38f, 0.5f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.2f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.08f, 0.09f);

            CreateLighting();
            CreatePrototypeCity();
            ArcadeCarController car = CreateCar();
            CreateCamera(car.transform);
            CreateHud(car);
        }

        private static void CreateLighting()
        {
            GameObject sunObject = new("Sun");
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1f, 0.91f, 0.78f);
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private static void CreatePrototypeCity()
        {
            Material road = Material(new Color(0.055f, 0.06f, 0.07f), 0.18f, 0.25f);
            Material concrete = Material(new Color(0.28f, 0.3f, 0.32f), 0.02f, 0.1f);
            Material buildingA = Material(new Color(0.11f, 0.16f, 0.22f), 0.15f, 0.2f);
            Material buildingB = Material(new Color(0.21f, 0.13f, 0.12f), 0.08f, 0.15f);
            Material line = Material(new Color(0.92f, 0.77f, 0.18f), 0f, 0.1f);

            GameObject ground = Primitive("Ground", PrimitiveType.Cube, new Vector3(0f, -0.55f, 0f), new Vector3(240f, 1f, 240f), concrete);
            ground.isStatic = true;

            for (int i = -2; i <= 2; i++)
            {
                Primitive($"Road_NS_{i}", PrimitiveType.Cube, new Vector3(i * 42f, 0f, 0f), new Vector3(18f, 0.08f, 220f), road).isStatic = true;
                Primitive($"Road_EW_{i}", PrimitiveType.Cube, new Vector3(0f, 0.01f, i * 42f), new Vector3(220f, 0.08f, 18f), road).isStatic = true;
            }

            for (int z = -2; z <= 2; z++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    if ((x + z) % 2 == 0) continue;
                    Vector3 blockCenter = new(x * 42f + 21f, 0f, z * 42f + 21f);
                    for (int b = 0; b < 3; b++)
                    {
                        float height = 8f + ((x * 17 + z * 11 + b * 7 + 100) % 22);
                        Vector3 pos = blockCenter + new Vector3((b - 1) * 7f, height * 0.5f, ((b % 2) * 2 - 1) * 5f);
                        Primitive($"Building_{x}_{z}_{b}", PrimitiveType.Cube, pos, new Vector3(6f, height, 9f), b % 2 == 0 ? buildingA : buildingB).isStatic = true;
                    }
                }
            }

            for (int z = -100; z <= 100; z += 8)
                Primitive("CenterLine", PrimitiveType.Cube, new Vector3(0f, 0.08f, z), new Vector3(0.18f, 0.035f, 3.5f), line).isStatic = true;

            // Open drift pad beside the spawn area.
            Primitive("DriftPad", PrimitiveType.Cylinder, new Vector3(83f, 0.02f, -83f), new Vector3(34f, 0.04f, 34f), road).isStatic = true;
        }

        private static ArcadeCarController CreateCar()
        {
            Material bodyMaterial = Material(new Color(0.86f, 0.07f, 0.035f), 0.42f, 0.62f);
            Material darkMaterial = Material(new Color(0.018f, 0.02f, 0.025f), 0.05f, 0.2f);
            Material glassMaterial = Material(new Color(0.035f, 0.095f, 0.14f), 0.55f, 0.7f);

            GameObject car = new("PlayerCar");
            car.transform.position = new Vector3(0f, 1.2f, -68f);
            car.AddComponent<Rigidbody>();
            BoxCollider collider = car.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.9f, 0.75f, 4.35f);
            collider.center = new Vector3(0f, 0.42f, 0f);

            Primitive("Body", PrimitiveType.Cube, car.transform, new Vector3(1.85f, 0.58f, 4.15f), new Vector3(0f, 0.4f, 0f), bodyMaterial, false);
            Primitive("Cabin", PrimitiveType.Cube, car.transform, new Vector3(1.55f, 0.6f, 1.8f), new Vector3(0f, 0.93f, -0.15f), glassMaterial, false);
            Primitive("Hood", PrimitiveType.Cube, car.transform, new Vector3(1.75f, 0.12f, 1.3f), new Vector3(0f, 0.74f, 1.3f), bodyMaterial, false);

            CreateWheel(car.transform, new Vector3(-0.98f, 0.22f, 1.32f), darkMaterial);
            CreateWheel(car.transform, new Vector3(0.98f, 0.22f, 1.32f), darkMaterial);
            CreateWheel(car.transform, new Vector3(-0.98f, 0.22f, -1.35f), darkMaterial);
            CreateWheel(car.transform, new Vector3(0.98f, 0.22f, -1.35f), darkMaterial);

            ArcadeCarController controller = car.AddComponent<ArcadeCarController>();
            car.AddComponent<CarReset>();
            return controller;
        }

        private static void CreateWheel(Transform parent, Vector3 localPosition, Material material)
        {
            GameObject wheel = Primitive("Wheel", PrimitiveType.Cylinder, parent, new Vector3(0.7f, 0.28f, 0.7f), localPosition, material, false);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static void CreateCamera(Transform target)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 67f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 700f;
            cameraObject.transform.position = target.position + new Vector3(0f, 3.6f, -7.5f);
            ChaseCamera chase = cameraObject.AddComponent<ChaseCamera>();
            chase.SetTarget(target);
        }

        private static void CreateHud(ArcadeCarController car)
        {
            GameObject hud = new("Prototype HUD");
            PrototypeHud prototypeHud = hud.AddComponent<PrototypeHud>();
            prototypeHud.Bind(car);
        }

        private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 scale, Vector3 localPosition, Material material, bool keepCollider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                Collider primitiveCollider = go.GetComponent<Collider>();
                if (primitiveCollider != null) Object.Destroy(primitiveCollider);
            }
            return go;
        }

        private static Material Material(Color color, float metallic, float smoothness)
        {
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = Shader.Find(srp ? "Universal Render Pipeline/Lit" : "Standard");
            Material material = new(shader) { color = color };

            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            return material;
        }
    }
}
