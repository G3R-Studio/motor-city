using System;
using UnityEngine;

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
        [SerializeField] private float brokenMass = 18f;
        [SerializeField] private float pushImpulseMultiplier = 0.55f;
        [SerializeField] private float despawnSeconds = 18f;

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
                    speedRetention = 0.985f;
                    brokenMass = 10f;
                    pushImpulseMultiplier = 0.7f;
                    despawnSeconds = 14f;
                    break;

                case PropKind.Bench:
                    speedRetention = 0.975f;
                    brokenMass = 16f;
                    pushImpulseMultiplier = 0.62f;
                    despawnSeconds = 18f;
                    break;

                case PropKind.TrafficLight:
                    speedRetention = 0.96f;
                    brokenMass = 28f;
                    pushImpulseMultiplier = 0.46f;
                    despawnSeconds = 22f;
                    break;

                case PropKind.LightPole:
                    speedRetention = 0.965f;
                    brokenMass = 24f;
                    pushImpulseMultiplier = 0.48f;
                    despawnSeconds = 22f;
                    break;

                case PropKind.Sign:
                case PropKind.Pole:
                    speedRetention = 0.98f;
                    brokenMass = 12f;
                    pushImpulseMultiplier = 0.72f;
                    despawnSeconds = 16f;
                    break;
            }
        }

        private bool PrepareTriggerCollider()
        {
            Collider[] existing =
                GetComponentsInChildren<Collider>(true);

            // A single collider on the breakable root makes the whole prop
            // behave as one lightweight obstacle. Child colliders are disabled
            // so a traffic light or lamp cannot act as an invisible wall.
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

            // Before impact the prop is non-blocking. The car loses a tiny
            // amount of speed manually, so street furniture never kills a
            // drift or abruptly stops free-roam driving.
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

            Vector3 carVelocity =
                carBody.linearVelocity;

            Vector3 horizontal =
                new(
                    carVelocity.x,
                    0f,
                    carVelocity.z);

            // Deliberately tiny speed loss: enough to make the impact readable
            // but not enough to ruin a drift line.
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
                breakCollider.isTrigger =
                    false;
            }

            Rigidbody body =
                GetComponent<Rigidbody>();

            if (body == null)
            {
                body =
                    gameObject.AddComponent<Rigidbody>();
            }

            body.mass =
                brokenMass;

            body.useGravity =
                true;

            body.linearDamping =
                0.22f;

            body.angularDamping =
                0.32f;

            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousSpeculative;

            body.interpolation =
                RigidbodyInterpolation.Interpolate;

            Vector3 direction =
                horizontal.sqrMagnitude > 0.01f
                    ? horizontal.normalized
                    : carBody.transform.forward;

            float impactSpeed =
                Mathf.Clamp(
                    horizontal.magnitude,
                    2f,
                    35f);

            float impulse =
                Mathf.Clamp(
                    brokenMass *
                    impactSpeed *
                    pushImpulseMultiplier *
                    0.12f,
                    4f,
                    48f);

            Vector3 impactPoint =
                transform.position +
                Vector3.up *
                Mathf.Max(
                    0.35f,
                    CombinedRendererHeight() *
                    0.28f);

            body.AddForceAtPosition(
                direction *
                impulse +
                Vector3.up *
                Mathf.Min(
                    2.5f,
                    impulse * 0.08f),
                impactPoint,
                ForceMode.Impulse);

            Vector3 torqueAxis =
                Vector3.Cross(
                    Vector3.up,
                    direction);

            body.AddTorque(
                torqueAxis *
                Mathf.Clamp(
                    impulse * 0.65f,
                    2f,
                    22f),
                ForceMode.Impulse);

            IgnoreCarCollisions(
                carBody);

            Destroy(
                gameObject,
                despawnSeconds);
        }

        private void IgnoreCarCollisions(
            Rigidbody carBody)
        {
            if (breakCollider == null ||
                carBody == null)
                return;

            foreach (Collider carCollider in
                     carBody.GetComponentsInChildren<Collider>(true))
            {
                if (carCollider == null ||
                    carCollider == breakCollider)
                    continue;

                Physics.IgnoreCollision(
                    breakCollider,
                    carCollider,
                    true);
            }
        }

        private float CombinedRendererHeight()
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
                return 1f;

            Bounds bounds =
                renderers[0].bounds;

            for (int i = 1;
                 i < renderers.Length;
                 i++)
            {
                bounds.Encapsulate(
                    renderers[i].bounds);
            }

            return
                bounds.size.y;
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
