using System;
using System.Collections.Generic;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.Rendering;

namespace MotorCity.World
{
    public sealed class CityDistanceOptimizer : MonoBehaviour
    {
        private const float UpdateInterval = 0.45f;

        private const float SmallCullDistance = 175f;
        private const float MediumCullDistance = 340f;
        private const float LargeCullDistance = 560f;

        private const float SmallSize = 9f;
        private const float MediumSize = 28f;
        private const float LargeSize = 65f;

        private readonly List<RendererEntry> managedRenderers =
            new();

        private Transform observer;
        private float timer;

        private struct RendererEntry
        {
            public Renderer Renderer;
            public Vector3 Center;
            public float CullDistanceSquared;
            public bool OriginalEnabled;
        }

        public int ManagedRendererCount =>
            managedRenderers.Count;

        public int TunedLodGroupCount { get; private set; }

        public void Initialize()
        {
            GameObject cityRoot =
                GameObject.Find(
                    "MotorCity_FCGCity") ??
                GameObject.Find(
                    "City-Maker");

            if (cityRoot == null)
                return;

            TuneLodGroups(
                cityRoot);

            BuildRendererCache(
                cityRoot);

            ResolveObserver();
            ApplyDistanceCulling();

        }

        private void Update()
        {
            timer -=
                Time.deltaTime;

            if (timer > 0f)
                return;

            timer =
                UpdateInterval;

            ResolveObserver();
            ApplyDistanceCulling();
        }

        private void OnDisable()
        {
            RestoreOriginalRendererState();
        }

        private void ResolveObserver()
        {
            if (observer != null)
                return;

            ArcadeCarController car =
                UnityEngine.Object.FindAnyObjectByType<ArcadeCarController>();

            if (car != null)
            {
                observer =
                    car.transform;

                return;
            }

            Camera mainCamera =
                Camera.main;

            if (mainCamera != null)
            {
                observer =
                    mainCamera.transform;
            }
        }

        private void TuneLodGroups(
            GameObject cityRoot)
        {
            LODGroup[] groups =
                cityRoot.GetComponentsInChildren<LODGroup>(
                    true);

            int tuned =
                0;

            foreach (LODGroup group in groups)
            {
                if (group == null ||
                    ShouldIgnoreHierarchy(
                        group.transform))
                {
                    continue;
                }

                LOD[] lods =
                    group.GetLODs();

                if (lods == null ||
                    lods.Length < 2)
                {
                    continue;
                }

                bool changed =
                    false;

                for (int i = 0;
                     i < lods.Length - 1;
                     i++)
                {
                    float current =
                        lods[i].screenRelativeTransitionHeight;

                    float adjusted =
                        Mathf.Clamp01(
                            current * 1.22f);

                    if (adjusted <=
                        current + 0.0001f)
                    {
                        continue;
                    }

                    lods[i].screenRelativeTransitionHeight =
                        adjusted;

                    changed =
                        true;
                }

                if (!changed)
                    continue;

                group.SetLODs(
                    lods);

                group.RecalculateBounds();

                tuned++;
            }

            TunedLodGroupCount =
                tuned;
        }

        private void BuildRendererCache(
            GameObject cityRoot)
        {
            managedRenderers.Clear();

            Renderer[] renderers =
                cityRoot.GetComponentsInChildren<Renderer>(
                    true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    !renderer.enabled ||
                    ShouldIgnoreHierarchy(
                        renderer.transform) ||
                    renderer.GetComponentInParent<LODGroup>() != null)
                {
                    continue;
                }

                Bounds bounds =
                    renderer.bounds;

                float maximumSize =
                    Mathf.Max(
                        bounds.size.x,
                        Mathf.Max(
                            bounds.size.y,
                            bounds.size.z));

                float cullDistance;

                if (maximumSize <=
                    SmallSize)
                {
                    cullDistance =
                        SmallCullDistance;

                    renderer.shadowCastingMode =
                        ShadowCastingMode.Off;

                    renderer.receiveShadows =
                        false;

                    renderer.lightProbeUsage =
                        LightProbeUsage.Off;

                    renderer.reflectionProbeUsage =
                        ReflectionProbeUsage.Off;
                }
                else if (maximumSize <=
                         MediumSize)
                {
                    cullDistance =
                        MediumCullDistance;

                    renderer.reflectionProbeUsage =
                        ReflectionProbeUsage.Off;
                }
                else if (maximumSize <=
                         LargeSize)
                {
                    cullDistance =
                        LargeCullDistance;
                }
                else
                {
                    // Large buildings, combined city blocks and skyline
                    // meshes stay visible and are left entirely to frustum
                    // culling / existing FCG LOD.
                    continue;
                }

                managedRenderers.Add(
                    new RendererEntry
                    {
                        Renderer =
                            renderer,
                        Center =
                            bounds.center,
                        CullDistanceSquared =
                            cullDistance *
                            cullDistance,
                        OriginalEnabled =
                            true
                    });
            }
        }

        private void ApplyDistanceCulling()
        {
            if (observer == null)
                return;

            Vector3 observerPosition =
                observer.position;

            for (int i = 0;
                 i < managedRenderers.Count;
                 i++)
            {
                RendererEntry entry =
                    managedRenderers[i];

                Renderer renderer =
                    entry.Renderer;

                if (renderer == null)
                    continue;

                bool visible =
                    (entry.Center -
                     observerPosition).sqrMagnitude <=
                    entry.CullDistanceSquared;

                if (renderer.enabled !=
                    visible)
                {
                    renderer.enabled =
                        visible;
                }
            }
        }

        private void RestoreOriginalRendererState()
        {
            foreach (RendererEntry entry in
                     managedRenderers)
            {
                if (entry.Renderer != null)
                {
                    entry.Renderer.enabled =
                        entry.OriginalEnabled;
                }
            }
        }

        private static bool ShouldIgnoreHierarchy(
            Transform item)
        {
            Transform current =
                item;

            while (current != null)
            {
                string normalized =
                    NormalizeName(
                        current.name);

                if (normalized.Contains(
                        "motorcitybackground") ||
                    normalized.Contains(
                        "trafficsystem") ||
                    normalized.Contains(
                        "carcontainer") ||
                    normalized.StartsWith(
                        "trafficlight") ||
                    normalized.StartsWith(
                        "streetlight") ||
                    normalized.StartsWith(
                        "parklamp"))
                {
                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

        private static string NormalizeName(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return string.Empty;
            }

            var result =
                new System.Text.StringBuilder(
                    value.Length);

            foreach (char character in
                     value.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(
                        character))
                {
                    result.Append(
                        character);
                }
            }

            return result.ToString();
        }
    }
}
