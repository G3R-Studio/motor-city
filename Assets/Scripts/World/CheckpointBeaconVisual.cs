using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    public sealed class CheckpointBeaconVisual : MonoBehaviour
    {
        private Transform visualRoot;
        private Transform directionRoot;
        private Material pillarMaterial;
        private Material baseMaterial;
        private Material arrowMaterial;
        private bool initialized;

        public void Initialize(
            Color color,
            bool createDirectionArrow = true)
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
                    3.2f,
                    4.5f,
                    3.2f),
                new Vector3(
                    0f,
                    4.5f,
                    0f),
                Quaternion.identity,
                pillarMaterial);

            CreatePrimitive(
                "Checkpoint Ground Ring",
                PrimitiveType.Cylinder,
                visualRoot,
                new Vector3(
                    4f,
                    0.035f,
                    4f),
                new Vector3(
                    0f,
                    0.07f,
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
                    6.3f,
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
