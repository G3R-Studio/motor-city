using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    public enum CheckpointBeaconStyle
    {
        Generic = 0,
        Delivery = 1,
        Sprint = 2,
        Circuit = 3,
        Drift = 4,
        Profession = 5,
        Tow = 6,
        Underground = 7
    }

    public sealed class CheckpointBeaconVisual : MonoBehaviour
    {
        private Transform visualRoot;
        private Transform directionRoot;
        private Transform emblemRoot;
        private float emblemBaseHeight;
        private Material pillarMaterial;
        private Material baseMaterial;
        private Material arrowMaterial;
        private bool initialized;

        public void Initialize(
            Color color,
            bool createDirectionArrow = true,
            CheckpointBeaconStyle style =
                CheckpointBeaconStyle.Generic)
        {
            if (initialized)
                return;

            initialized =
                true;

            GameObject visualRootObject =
                new(
                    "Checkpoint Beacon Visuals");

            visualRootObject.transform.SetParent(
                transform,
                false);

            visualRoot =
                visualRootObject.transform;

            pillarMaterial =
                CreateTransparentMaterial(
                    color,
                    0.18f);

            baseMaterial =
                CreateTransparentMaterial(
                    color,
                    0.42f);

            arrowMaterial =
                CreateTransparentMaterial(
                    color,
                    0.82f);

            CreatePrimitive(
                "Checkpoint Pillar",
                PrimitiveType.Cylinder,
                visualRoot,
                new Vector3(
                    2.55f,
                    4.1f,
                    2.55f),
                new Vector3(
                    0f,
                    4.1f,
                    0f),
                Quaternion.identity,
                pillarMaterial);

            CreatePrimitive(
                "Checkpoint Ground Ring Outer",
                PrimitiveType.Cylinder,
                visualRoot,
                new Vector3(
                    4.15f,
                    0.030f,
                    4.15f),
                new Vector3(
                    0f,
                    0.055f,
                    0f),
                Quaternion.identity,
                baseMaterial);

            CreatePrimitive(
                "Checkpoint Ground Ring Inner",
                PrimitiveType.Cylinder,
                visualRoot,
                new Vector3(
                    2.75f,
                    0.045f,
                    2.75f),
                new Vector3(
                    0f,
                    0.09f,
                    0f),
                Quaternion.identity,
                arrowMaterial);

            CreateEmblem(
                style);

            CreatePrimitive(
                "Emblem Core",
                PrimitiveType.Sphere,
                emblemRoot,
                new Vector3(
                    0.42f,
                    0.18f,
                    0.42f),
                new Vector3(
                    0f,
                    -0.18f,
                    0f),
                Quaternion.identity,
                baseMaterial);

            if (!createDirectionArrow)
                return;

            GameObject directionObject =
                new(
                    "Checkpoint Direction");

            directionObject.transform.SetParent(
                visualRoot,
                false);

            directionRoot =
                directionObject.transform;

            directionRoot.localPosition =
                new Vector3(
                    0f,
                    6.0f,
                    0f);

            CreatePrimitive(
                "Direction Left",
                PrimitiveType.Cube,
                directionRoot,
                new Vector3(
                    0.28f,
                    0.16f,
                    1.55f),
                new Vector3(
                    -0.52f,
                    0f,
                    0.42f),
                Quaternion.Euler(
                    0f,
                    43f,
                    0f),
                arrowMaterial);

            CreatePrimitive(
                "Direction Right",
                PrimitiveType.Cube,
                directionRoot,
                new Vector3(
                    0.28f,
                    0.16f,
                    1.55f),
                new Vector3(
                    0.52f,
                    0f,
                    0.42f),
                Quaternion.Euler(
                    0f,
                    -43f,
                    0f),
                arrowMaterial);

            directionRoot.gameObject.SetActive(
                false);
        }

        private void Update()
        {
            if (emblemRoot == null ||
                !emblemRoot.gameObject.activeInHierarchy)
            {
                return;
            }

            float bob =
                Mathf.Sin(
                    Time.unscaledTime *
                    2.0f) *
                0.12f;

            Vector3 localPosition =
                emblemRoot.localPosition;

            localPosition.y =
                emblemBaseHeight +
                bob;

            emblemRoot.localPosition =
                localPosition;

            emblemRoot.Rotate(
                0f,
                14f *
                Time.unscaledDeltaTime,
                0f,
                Space.Self);
        }

        private void CreateEmblem(
            CheckpointBeaconStyle style)
        {
            GameObject emblemObject =
                new(
                    "Checkpoint Emblem");

            emblemObject.transform.SetParent(
                visualRoot,
                false);

            emblemRoot =
                emblemObject.transform;

            emblemBaseHeight =
                6.95f;

            emblemRoot.localPosition =
                new Vector3(
                    0f,
                    emblemBaseHeight,
                    0f);

            switch (style)
            {
                case CheckpointBeaconStyle.Delivery:
                    CreatePrimitive(
                        "Package",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            1.45f,
                            0.95f,
                            1.15f),
                        Vector3.zero,
                        Quaternion.identity,
                        arrowMaterial);

                    CreatePrimitive(
                        "Package Band",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            0.22f,
                            1.08f,
                            1.24f),
                        Vector3.zero,
                        Quaternion.identity,
                        baseMaterial);
                    break;

                case CheckpointBeaconStyle.Sprint:
                    CreateChevronEmblem(
                        emblemRoot,
                        0f);

                    CreateChevronEmblem(
                        emblemRoot,
                        -0.9f);
                    break;

                case CheckpointBeaconStyle.Circuit:
                    CreatePrimitive(
                        "Circuit Top",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            1.55f,
                            0.20f,
                            0.24f),
                        new Vector3(
                            0f,
                            0f,
                            0.72f),
                        Quaternion.identity,
                        arrowMaterial);

                    CreatePrimitive(
                        "Circuit Bottom",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            1.55f,
                            0.20f,
                            0.24f),
                        new Vector3(
                            0f,
                            0f,
                            -0.72f),
                        Quaternion.identity,
                        arrowMaterial);

                    CreatePrimitive(
                        "Circuit Left",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            0.24f,
                            0.20f,
                            1.20f),
                        new Vector3(
                            -0.78f,
                            0f,
                            0f),
                        Quaternion.identity,
                        arrowMaterial);

                    CreatePrimitive(
                        "Circuit Right",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            0.24f,
                            0.20f,
                            1.20f),
                        new Vector3(
                            0.78f,
                            0f,
                            0f),
                        Quaternion.identity,
                        arrowMaterial);
                    break;

                case CheckpointBeaconStyle.Drift:
                    CreatePrimitive(
                        "Drift Slash Left",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            0.28f,
                            0.22f,
                            1.65f),
                        new Vector3(
                            -0.42f,
                            0f,
                            0f),
                        Quaternion.Euler(
                            0f,
                            28f,
                            0f),
                        arrowMaterial);

                    CreatePrimitive(
                        "Drift Slash Right",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            0.28f,
                            0.22f,
                            1.65f),
                        new Vector3(
                            0.42f,
                            0f,
                            0f),
                        Quaternion.Euler(
                            0f,
                            -28f,
                            0f),
                        arrowMaterial);
                    break;

                case CheckpointBeaconStyle.Profession:
                    CreatePrimitive(
                        "Profession Horizontal",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            1.65f,
                            0.24f,
                            0.30f),
                        Vector3.zero,
                        Quaternion.Euler(
                            0f,
                            32f,
                            0f),
                        arrowMaterial);

                    CreatePrimitive(
                        "Profession Vertical",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            0.30f,
                            0.24f,
                            1.65f),
                        Vector3.zero,
                        Quaternion.Euler(
                            0f,
                            32f,
                            0f),
                        arrowMaterial);
                    break;

                case CheckpointBeaconStyle.Tow:
                    CreatePrimitive(
                        "Tow Stem",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            0.28f,
                            0.28f,
                            1.55f),
                        new Vector3(
                            0f,
                            0f,
                            0.18f),
                        Quaternion.identity,
                        arrowMaterial);

                    CreatePrimitive(
                        "Tow Hook",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            0.85f,
                            0.28f,
                            0.28f),
                        new Vector3(
                            0.30f,
                            0f,
                            -0.55f),
                        Quaternion.Euler(
                            0f,
                            -22f,
                            0f),
                        arrowMaterial);
                    break;

                case CheckpointBeaconStyle.Underground:
                    CreatePrimitive(
                        "Underground Slash A",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            0.28f,
                            0.24f,
                            1.75f),
                        Vector3.zero,
                        Quaternion.Euler(
                            0f,
                            45f,
                            0f),
                        arrowMaterial);

                    CreatePrimitive(
                        "Underground Slash B",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            0.28f,
                            0.24f,
                            1.75f),
                        Vector3.zero,
                        Quaternion.Euler(
                            0f,
                            -45f,
                            0f),
                        arrowMaterial);
                    break;

                default:
                    CreatePrimitive(
                        "Generic Diamond",
                        PrimitiveType.Cube,
                        emblemRoot,
                        new Vector3(
                            1.1f,
                            0.24f,
                            1.1f),
                        Vector3.zero,
                        Quaternion.Euler(
                            0f,
                            45f,
                            0f),
                        arrowMaterial);
                    break;
            }
        }

        private void CreateChevronEmblem(
            Transform parent,
            float zOffset)
        {
            CreatePrimitive(
                "Sprint Chevron Left",
                PrimitiveType.Cube,
                parent,
                new Vector3(
                    0.26f,
                    0.20f,
                    1.15f),
                new Vector3(
                    -0.34f,
                    0f,
                    zOffset),
                Quaternion.Euler(
                    0f,
                    42f,
                    0f),
                arrowMaterial);

            CreatePrimitive(
                "Sprint Chevron Right",
                PrimitiveType.Cube,
                parent,
                new Vector3(
                    0.26f,
                    0.20f,
                    1.15f),
                new Vector3(
                    0.34f,
                    0f,
                    zOffset),
                Quaternion.Euler(
                    0f,
                    -42f,
                    0f),
                arrowMaterial);
        }

        public void SetVisible(
            bool visible)
        {
            if (visualRoot != null &&
                visualRoot.gameObject.activeSelf !=
                    visible)
            {
                visualRoot.gameObject.SetActive(
                    visible);
            }
        }

        public void SetDirection(
            Vector3 currentTarget,
            Vector3 nextTarget,
            bool visible)
        {
            if (directionRoot == null)
                return;

            Vector3 direction =
                nextTarget -
                currentTarget;

            direction.y =
                0f;

            bool show =
                visible &&
                direction.sqrMagnitude >
                    4f;

            if (directionRoot.gameObject.activeSelf !=
                show)
            {
                directionRoot.gameObject.SetActive(
                    show);
            }

            if (!show)
                return;

            directionRoot.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);
        }

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 scale,
            Vector3 localPosition,
            Quaternion localRotation,
            Material material)
        {
            GameObject go =
                GameObject.CreatePrimitive(
                    type);

            go.name =
                name;

            go.transform.SetParent(
                parent,
                false);

            go.transform.localPosition =
                localPosition;

            go.transform.localRotation =
                localRotation;

            go.transform.localScale =
                scale;

            Collider collider =
                go.GetComponent<Collider>();

            if (collider != null)
            {
                Object.Destroy(
                    collider);
            }

            Renderer renderer =
                go.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    material;

                renderer.shadowCastingMode =
                    ShadowCastingMode.Off;

                renderer.receiveShadows =
                    false;
            }

            return
                go;
        }

        private void OnDestroy()
        {
            if (pillarMaterial != null)
            {
                Destroy(
                    pillarMaterial);
            }

            if (baseMaterial != null)
            {
                Destroy(
                    baseMaterial);
            }

            if (arrowMaterial != null)
            {
                Destroy(
                    arrowMaterial);
            }
        }

        private static Material CreateTransparentMaterial(
            Color color,
            float alpha)
        {
            bool srp =
                GraphicsSettings.currentRenderPipeline != null;

            Shader shader =
                Shader.Find(
                    srp
                        ? "Universal Render Pipeline/Lit"
                        : "Standard") ??
                Shader.Find(
                    "Sprites/Default");

            Material material =
                new(
                    shader)
                {
                    name =
                        "MotorCity Checkpoint Beacon"
                };

            Color tinted =
                new(
                    color.r,
                    color.g,
                    color.b,
                    Mathf.Clamp01(
                        alpha));

            if (material.HasProperty(
                    "_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    tinted);
            }

            if (material.HasProperty(
                    "_Color"))
            {
                material.SetColor(
                    "_Color",
                    tinted);
            }

            material.SetOverrideTag(
                "RenderType",
                "Transparent");

            if (material.HasProperty(
                    "_Surface"))
            {
                material.SetFloat(
                    "_Surface",
                    1f);
            }

            if (material.HasProperty(
                    "_Mode"))
            {
                material.SetFloat(
                    "_Mode",
                    3f);
            }

            if (material.HasProperty(
                    "_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty(
                    "_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty(
                    "_ZWrite"))
            {
                material.SetFloat(
                    "_ZWrite",
                    0f);
            }

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");

            material.EnableKeyword(
                "_ALPHABLEND_ON");

            material.renderQueue =
                (int)RenderQueue.Transparent;

            return
                material;
        }
    }
}
