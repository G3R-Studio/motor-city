using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MotorCity.Vehicle
{
    public sealed class CartoonSportsCarRuntimeInstaller : MonoBehaviour
    {
        private const float TargetLength = 4.2f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ScheduleInstall()
        {
            GameObject runner = new("Cartoon Sports Car Installer");
            DontDestroyOnLoad(runner);
            runner.AddComponent<CartoonSportsCarRuntimeInstaller>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;

            ArcadeCarController car = Object.FindAnyObjectByType<ArcadeCarController>();
            if (car == null)
            {
                Destroy(gameObject);
                yield break;
            }

            GameObject prefab = Resources.Load<GameObject>("MotorCity/PlayerCarVisual");
            if (prefab == null)
            {
                Destroy(gameObject);
                yield break;
            }

            Install(car, prefab);
            Destroy(gameObject);
        }

        private static void Install(ArcadeCarController car, GameObject prefab)
        {
            Transform carTransform = car.transform;

            GameObject visual = Instantiate(prefab, carTransform);
            visual.name = "CartoonSportsCarVisual_Runtime";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            DisablePhysics(visual);
            NormalizeVisualScale(visual.transform);

            List<Transform> wheelMeshes = FindWheelMeshes(visual.transform);
            if (wheelMeshes.Count < 4)
            {
                Debug.LogWarning("Motor City: Cartoon Sports Car imported, but four separate wheel transforms were not found. Keeping fallback suspension visuals.");
                visual.SetActive(false);
                return;
            }

            Transform[] ordered = OrderWheels(carTransform, wheelMeshes);
            Transform[] pivots = new Transform[4];
            Vector3[] suspensionPoints = new Vector3[4];
            float measuredRadius = 0.33f;

            for (int i = 0; i < 4; i++)
            {
                Transform wheel = ordered[i];
                Bounds bounds = RendererBounds(wheel);
                measuredRadius = Mathf.Clamp(Mathf.Max(bounds.extents.y, bounds.extents.z), 0.27f, 0.42f);

                GameObject pivotObject = new($"AssetWheelPivot_{i}");
                Transform pivot = pivotObject.transform;
                pivot.SetParent(carTransform, true);
                pivot.position = bounds.center;
                pivot.rotation = carTransform.rotation;

                wheel.SetParent(pivot, true);
                pivots[i] = pivot;

                Vector3 local = carTransform.InverseTransformPoint(bounds.center);
                suspensionPoints[i] = new Vector3(local.x, -0.10f, local.z);
            }

            HideFallbackVisuals(carTransform, visual.transform);
            car.ConfigureExternalWheelRig(pivots, suspensionPoints, measuredRadius);
            Debug.Log("Motor City: Cartoon Sports Car visual installed and suspension rebuilt from asset wheel positions.");
        }

        private static void NormalizeVisualScale(Transform visual)
        {
            Bounds bounds = RendererBounds(visual);

            // Most vehicle assets use Z as forward. If this one arrives with its long
            // axis on X, rotate the whole visual once before measuring/scaling it.
            if (bounds.size.x > bounds.size.z * 1.15f)
            {
                visual.localRotation = Quaternion.Euler(0f, 90f, 0f);
                bounds = RendererBounds(visual);
            }

            float length = Mathf.Max(bounds.size.x, bounds.size.z);
            if (length < 0.01f) return;

            float scale = TargetLength / length;
            visual.localScale = visual.localScale * scale;

            bounds = RendererBounds(visual);
            Transform parent = visual.parent;
            Vector3 centerLocal = parent.InverseTransformPoint(bounds.center);
            Vector3 bottomWorld = new(bounds.center.x, bounds.min.y, bounds.center.z);
            float bottomLocalY = parent.InverseTransformPoint(bottomWorld).y;

            // Center the car over the physics root and keep the lowest rendered point
            // just above local ground level. The actual wheel centres are repositioned
            // by the suspension immediately afterwards.
            visual.localPosition -= new Vector3(centerLocal.x, bottomLocalY - 0.08f, centerLocal.z);
        }

        private static List<Transform> FindWheelMeshes(Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            var candidates = new List<Transform>();

            foreach (Transform item in all)
            {
                if (item == root) continue;
                string n = item.name.ToLowerInvariant();
                if (!(n.Contains("wheel") || n.Contains("tyre") || n.Contains("tire"))) continue;
                if (item.GetComponentInChildren<Renderer>(true) == null) continue;

                bool childOfExisting = candidates.Any(c => item.IsChildOf(c));
                if (!childOfExisting) candidates.Add(item);
            }

            if (candidates.Count <= 4) return candidates;

            return candidates
                .OrderByDescending(t => RendererBounds(t).size.sqrMagnitude)
                .Take(4)
                .ToList();
        }

        private static Transform[] OrderWheels(Transform car, List<Transform> wheels)
        {
            return wheels
                .OrderByDescending(t => car.InverseTransformPoint(RendererBounds(t).center).z)
                .ThenBy(t => car.InverseTransformPoint(RendererBounds(t).center).x)
                .Take(4)
                .ToArray();
        }

        private static Bounds RendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.one * 0.1f);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void DisablePhysics(GameObject visual)
        {
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Rigidbody rigidbody in visual.GetComponentsInChildren<Rigidbody>(true)) rigidbody.isKinematic = true;
        }

        private static void HideFallbackVisuals(Transform car, Transform importedVisual)
        {
            foreach (Renderer renderer in car.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.transform.IsChildOf(importedVisual)) continue;
                if (renderer.transform.name.StartsWith("AssetWheelPivot_")) continue;
                renderer.enabled = false;
            }
        }
    }
}
