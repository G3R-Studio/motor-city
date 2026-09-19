using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    public sealed class BreakableStreetProp : MonoBehaviour
    {
        private enum PropKind
        {
            LightPole,
            TrafficLight,
            Sign,
            Hydrant,
            Bench,
            Pole
        }

        [SerializeField] private PropKind kind;
        [SerializeField] private float minimumBreakSpeedKph = 6f;
        [SerializeField] private float speedRetention = 0.97f;
        [SerializeField] private float fallSeconds = 0.55f;
        [SerializeField] private float fadeSeconds = 1.8f;

        private Collider breakCollider;
        private bool broken;

        public static int PrepareAll(
            GameObject cityRoot)
        {
            if (cityRoot == null)
                return 0;

            int prepared = 0;

            foreach (Transform item in
                     cityRoot.GetComponentsInChildren<Transform>(true))
            {
                if (item == null ||
                    item == cityRoot.transform)
                    continue;

                if (!TryClassify(
                        item,
                        out PropKind propKind))
                    continue;

                if (item.GetComponent<BreakableStreetProp>() != null)
                    continue;

                BreakableStreetProp prop =
                    item.gameObject.AddComponent<BreakableStreetProp>();

                prop.Configure(
                    propKind);

                if (!prop.PrepareTriggerCollider())
                {
                    DestroyComponent(
                        prop);
                    continue;
                }

                prepared++;
            }

            return prepared;
        }

        private void Configure(
            PropKind propKind)
        {
            kind =
                propKind;

            switch (kind)
            {
                case PropKind.Hydrant:
                    speedRetention = 0.988f;
                    fallSeconds = 0.38f;
                    fadeSeconds = 1.4f;
                    break;

                case PropKind.Bench:
                    speedRetention = 0.982f;
                    fallSeconds = 0.48f;
                    fadeSeconds = 1.6f;
                    break;

                case PropKind.TrafficLight:
                    speedRetention = 0.968f;
                    fallSeconds = 0.62f;
                    fadeSeconds = 2.0f;
                    break;

                case PropKind.LightPole:
                    speedRetention = 0.972f;
                    fallSeconds = 0.62f;
                    fadeSeconds = 2.0f;
                    break;

                case PropKind.Sign:
                case PropKind.Pole:
                    speedRetention = 0.982f;
                    fallSeconds = 0.5f;
                    fadeSeconds = 1.6f;
                    break;
            }
        }

        private bool PrepareTriggerCollider()
        {
            Collider[] existing =
                GetComponentsInChildren<Collider>(true);

            foreach (Collider collider in existing)
            {
                if (collider == null ||
                    collider.transform == transform)
                    continue;

                collider.enabled =
                    false;
            }

            breakCollider =
                GetComponent<Collider>();

            if (breakCollider == null)
            {
                if (!TryCalculateLocalRendererBounds(
                        out Bounds localBounds))
                    return false;

                BoxCollider box =
                    gameObject.AddComponent<BoxCollider>();

                box.center =
                    localBounds.center;

                Vector3 size =
                    localBounds.size;

                size.x =
                    Mathf.Max(
                        0.16f,
                        size.x);

                size.y =
                    Mathf.Max(
                        0.22f,
                        size.y);

                size.z =
                    Mathf.Max(
                        0.16f,
                        size.z);

                box.size =
                    size;

                breakCollider =
                    box;
            }

            breakCollider.enabled =
                true;

            // Street furniture is deliberately non-blocking before impact.
            // The slight loss of car speed is applied manually.
            breakCollider.isTrigger =
                true;

            return true;
        }

        private void OnTriggerEnter(
            Collider other)
        {
            if (broken ||
                other == null)
                return;

            MotorCity.Vehicle.ArcadeCarController car =
                other.GetComponentInParent<MotorCity.Vehicle.ArcadeCarController>();

            if (car == null)
                return;

            Rigidbody carBody =
                car.GetComponent<Rigidbody>();

            if (carBody == null)
                return;

            float speedKph =
                carBody.linearVelocity.magnitude *
                3.6f;

            if (speedKph <
                minimumBreakSpeedKph)
                return;

            Break(
                carBody);
        }

        private void Break(
            Rigidbody carBody)
        {
            if (broken)
                return;

            broken =
                true;

            foreach (Light light in
                     GetComponentsInChildren<Light>(true))
            {
                if (light != null)
                {
                    light.enabled =
                        false;
                }
            }

            Vector3 carVelocity =
                carBody.linearVelocity;

            Vector3 horizontal =
                new(
                    carVelocity.x,
                    0f,
                    carVelocity.z);

            carBody.linearVelocity =
                new Vector3(
                    carVelocity.x * speedRetention,
                    carVelocity.y,
                    carVelocity.z * speedRetention);

            transform.SetParent(
                null,
                true);

            foreach (Transform child in
                     GetComponentsInChildren<Transform>(true))
            {
                if (child != null)
                    child.gameObject.isStatic =
                        false;
            }

            if (breakCollider != null)
            {
                // No physical tumbling after the hit. This avoids poles
                // spinning forever and keeps them from disturbing drifting.
                breakCollider.enabled =
                    false;
            }

            Rigidbody existingBody =
                GetComponent<Rigidbody>();

            if (existingBody != null)
            {
                Destroy(
                    existingBody);
            }

            Vector3 fallDirection =
                horizontal.sqrMagnitude > 0.01f
                    ? horizontal.normalized
                    : carBody.transform.forward;

            StartCoroutine(
                FallAndFade(
                    fallDirection));
        }

        private IEnumerator FallAndFade(
            Vector3 fallDirection)
        {
            Quaternion startRotation =
                transform.rotation;

            Vector3 horizontal =
                new(
                    fallDirection.x,
                    0f,
                    fallDirection.z);

            if (horizontal.sqrMagnitude <
                0.001f)
            {
                horizontal =
                    transform.forward;
            }

            horizontal.Normalize();

            Vector3 fallAxis =
                Vector3.Cross(
                    Vector3.up,
                    horizontal);

            if (fallAxis.sqrMagnitude <
                0.001f)
            {
                fallAxis =
                    transform.right;
            }

            float fallAngle =
                kind == PropKind.Hydrant
                    ? 68f
                    : kind == PropKind.Bench
                        ? 74f
                        : 86f;

            Quaternion targetRotation =
                Quaternion.AngleAxis(
                    fallAngle,
                    fallAxis) *
                startRotation;

            Material[] fadeMaterials =
                CreateFadeMaterialInstances();

            float totalSeconds =
                Mathf.Max(
                    0.1f,
                    fallSeconds + fadeSeconds);

            float elapsed =
                0f;

            while (elapsed <
                   totalSeconds)
            {
                elapsed +=
                    Time.deltaTime;

                float fallT =
                    Mathf.Clamp01(
                        elapsed /
                        Mathf.Max(
                            0.05f,
                            fallSeconds));

                float easedFall =
                    1f -
                    Mathf.Pow(
                        1f - fallT,
                        3f);

                transform.rotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        easedFall);

                float fadeStart =
                    Mathf.Max(
                        0f,
                        fallSeconds * 0.35f);

                float fadeT =
                    Mathf.Clamp01(
                        (elapsed - fadeStart) /
                        Mathf.Max(
                            0.05f,
                            fadeSeconds));

                SetFadeAlpha(
                    fadeMaterials,
                    1f - fadeT);

                yield return null;
            }

            SetFadeAlpha(
                fadeMaterials,
                0f);

            Destroy(
                gameObject);
        }

        private Material[] CreateFadeMaterialInstances()
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(true);

            var materials =
                new System.Collections.Generic.List<Material>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material[] source =
                    renderer.materials;

                foreach (Material material in source)
                {
                    if (material == null)
                        continue;

                    ConfigureTransparentMaterial(
                        material);

                    materials.Add(
                        material);
                }
            }

            return materials.ToArray();
        }

        private static void ConfigureTransparentMaterial(
            Material material)
        {
            if (material == null)
                return;

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);

            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");

            material.SetOverrideTag(
                "RenderType",
                "Transparent");

            material.renderQueue =
                (int)RenderQueue.Transparent;
        }

        private static void SetFadeAlpha(
            Material[] materials,
            float alpha)
        {
            if (materials == null)
                return;

            alpha =
                Mathf.Clamp01(
                    alpha);

            foreach (Material material in materials)
            {
                if (material == null)
                    continue;

                if (material.HasProperty("_BaseColor"))
                {
                    Color color =
                        material.GetColor(
                            "_BaseColor");

                    color.a =
                        alpha;

                    material.SetColor(
                        "_BaseColor",
                        color);
                }

                if (material.HasProperty("_Color"))
                {
                    Color color =
                        material.GetColor(
                            "_Color");

                    color.a =
                        alpha;

                    material.SetColor(
                        "_Color",
                        color);
                }
            }
        }

        private bool TryCalculateLocalRendererBounds(
            out Bounds localBounds)
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                localBounds =
                    default;

                return false;
            }

            bool initialized =
                false;

            localBounds =
                default;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Bounds world =
                    renderer.bounds;

                Vector3 min =
                    world.min;

                Vector3 max =
                    world.max;

                for (int x = 0;
                     x <= 1;
                     x++)
                {
                    for (int y = 0;
                         y <= 1;
                         y++)
                    {
                        for (int z = 0;
                             z <= 1;
                             z++)
                        {
                            Vector3 corner =
                                new(
                                    x == 0 ? min.x : max.x,
                                    y == 0 ? min.y : max.y,
                                    z == 0 ? min.z : max.z);

                            Vector3 local =
                                transform.InverseTransformPoint(
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
                                localBounds.Encapsulate(
                                    local);
                            }
                        }
                    }
                }
            }

            return
                initialized;
        }

        private static bool TryClassify(
            Transform item,
            out PropKind propKind)
        {
            propKind =
                default;

            string name =
                item.name.ToLowerInvariant();

            if (name.StartsWith("streetlight") ||
                name.StartsWith("parklamp") ||
                name.Contains("street-light"))
            {
                propKind =
                    PropKind.LightPole;

                return true;
            }

            if (name.Contains("hydrant"))
            {
                propKind =
                    PropKind.Hydrant;

                return true;
            }

            if (name.Contains("parkbench") ||
                name.StartsWith("bench"))
            {
                propKind =
                    PropKind.Bench;

                return true;
            }

            if (IsTrafficLightRoot(
                    item,
                    name))
            {
                propKind =
                    PropKind.TrafficLight;

                return true;
            }

            if (name.Contains("traffic-sign") ||
                name.Contains("traffic_sign") ||
                name.Contains("roadsign") ||
                name.Contains("road-sign") ||
                name.Contains("road_sign") ||
                name.Contains("signpost") ||
                name.StartsWith("sign-") ||
                name.StartsWith("sign_"))
            {
                propKind =
                    PropKind.Sign;

                return true;
            }

            if (name.Contains("bollard") ||
                name.StartsWith("pole-") ||
                name.StartsWith("pole_") ||
                name.EndsWith("-pole") ||
                name.EndsWith("_pole"))
            {
                propKind =
                    PropKind.Pole;

                return true;
            }

            return false;
        }

        private static bool IsTrafficLightRoot(
            Transform item,
            string lowerName)
        {
            bool looksLikeAssembly =
                lowerName.StartsWith("traffic_light-") ||
                lowerName.StartsWith("traffic_light (") ||
                lowerName.StartsWith("traffic_lights-");

            if (!looksLikeAssembly)
                return false;

            Transform parent =
                item.parent;

            if (parent == null)
                return true;

            string parentName =
                parent.name.ToLowerInvariant();

            return
                !parentName.StartsWith("traffic_light-") &&
                !parentName.StartsWith("traffic_light (") &&
                !parentName.StartsWith("traffic_lights-");
        }

        private static void DestroyComponent(
            Component component)
        {
            if (component == null)
                return;

            if (Application.isPlaying)
            {
                Destroy(
                    component);
            }
            else
            {
                DestroyImmediate(
                    component);
            }
        }
    }
}
