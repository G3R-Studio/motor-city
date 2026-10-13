using System;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.World
{
    /// <summary>
    /// Reversible renderer-only HLOD for pre-baked city cell proxies.
    /// CityVisual meshes, collisions, transforms and gameplay are untouched.
    /// </summary>
    public sealed class CityHlodRuntime : MonoBehaviour
    {
        private const string ResourcePath =
            "MotorCity/Environment/HLOD/CityHlodData";
        private const int CellsPerFrame = 12;

        private sealed class Instance
        {
            public CityHlodCell Data;
            public MeshRenderer Proxy;
            public bool UsingProxy;
        }

        private Instance[] cells = Array.Empty<Instance>();
        private MeshRenderer[] sources;
        private Camera observer;
        private GameObject proxyRoot;
        private int cursor;
        private bool refresh = true;
        private bool initialized;
        private Vector3 anchor;
        private MotorCityQualityPreset lastPreset;

        public static CityHlodData LoadData() =>
            Resources.Load<CityHlodData>(ResourcePath);

        public bool Initialized => initialized;
        public int CellCount => cells.Length;
        public int ActiveProxyCount { get; private set; }

        public bool Initialize(GameObject city, Camera camera, CityHlodData data)
        {
            if (initialized || city == null || camera == null || data == null ||
                data.Cells == null || data.Cells.Length == 0)
                return false;

            MeshRenderer[] renderers =
                city.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length != data.SourceRendererCount)
            {
                Debug.LogWarning("[MotorCity][HLOD] City renderer count has " +
                    "changed since baking. Rebake required; leaving original city.");
                return false;
            }

            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (CityHlodCell cell in data.Cells)
            {
                if (cell == null || cell.ProxyMesh == null ||
                    cell.ProxyMaterial == null ||
                    cell.SourceRendererIndices == null ||
                    cell.SourceRendererNames == null ||
                    cell.SourceRendererCenters == null ||
                    cell.SourceRendererIndices.Length !=
                    cell.SourceRendererNames.Length ||
                    cell.SourceRendererIndices.Length !=
                    cell.SourceRendererCenters.Length)
                    return false;

                for (int j = 0; j < cell.SourceRendererIndices.Length; j++)
                {
                    int index = cell.SourceRendererIndices[j];
                    if (index < 0 || index >= renderers.Length ||
                        !seen.Add(index) || renderers[index] == null ||
                        renderers[index].name != cell.SourceRendererNames[j] ||
                        (renderers[index].bounds.center -
                         cell.SourceRendererCenters[j]).sqrMagnitude > 1f)
                    {
                        Debug.LogWarning("[MotorCity][HLOD] Outdated or " +
                            "overlapping source mapping. Rebake required.");
                        return false;
                    }
                }
            }

            sources = renderers;
            observer = camera;
            // Combined vertices were baked in WORLD coordinates. Do NOT
            // parent proxies to the moving camera: keep world origin fixed.
            proxyRoot = new GameObject("MotorCity_HLOD_Proxies");
            GameObject root = proxyRoot;
            cells = new Instance[data.Cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                CityHlodCell cell = data.Cells[i];
                GameObject go = new GameObject("HLOD_Cell_" + i,
                    typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root.transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = cell.ProxyMesh;
                MeshRenderer proxy = go.GetComponent<MeshRenderer>();
                proxy.sharedMaterial = cell.ProxyMaterial;
                proxy.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                proxy.receiveShadows = false;
                proxy.enabled = false;
                cells[i] = new Instance { Data = cell, Proxy = proxy };
            }

            initialized = true;
            RequestRefresh();
            Debug.Log("[MotorCity][HLOD] Ready: " + cells.Length +
                " cells. Source triangles=" + data.SourceTriangles +
                "; proxy triangles=" + data.ProxyTriangles +
                ". These are asset totals, not frame statistics.");
            return true;
        }

        public void RequestRefresh()
        {
            cursor = 0;
            refresh = true;
        }

        private void OnEnable() => RequestRefresh();
        private void OnDisable() => RestoreAll();
        private void OnDestroy()
        {
            RestoreAll();
            if (proxyRoot != null)
                Destroy(proxyRoot);
        }

        public void RestoreAll()
        {
            if (sources != null)
                foreach (Instance instance in cells)
                {
                    if (instance == null)
                        continue;
                    if (instance.UsingProxy)
                        SetOriginals(instance.Data, false);
                    instance.UsingProxy = false;
                    if (instance.Proxy != null)
                        instance.Proxy.enabled = false;
                }

            ActiveProxyCount = 0;
            cursor = 0;
            refresh = true;
        }

        private void Update()
        {
            if (!initialized || observer == null || cells.Length == 0)
                return;

            Vector3 cameraPosition = observer.transform.position;
            MotorCityQualityPreset preset =
                MotorCityQualityRuntime.CurrentPreset;
            if (preset != lastPreset)
            {
                lastPreset = preset;
                RequestRefresh();
            }

            if (cursor == 0)
            {
                if (!refresh &&
                    (cameraPosition - anchor).sqrMagnitude < 64f)
                    return;
                anchor = cameraPosition;
                refresh = false;
            }

            float threshold = preset switch
            {
                MotorCityQualityPreset.Low => 360f,
                MotorCityQualityPreset.Medium => 450f,
                _ => 560f
            };

            int limit = Mathf.Min(cells.Length, cursor + CellsPerFrame);
            for (int i = cursor; i < limit; i++)
            {
                Instance instance = cells[i];
                float distance = Vector3.Distance(
                    cameraPosition, instance.Data.Center);
                bool proxyRequired = instance.UsingProxy
                    ? distance > threshold - 45f
                    : distance > threshold + 45f;

                if (instance.UsingProxy == proxyRequired)
                    continue;

                if (proxyRequired)
                {
                    instance.Proxy.enabled = true;
                    SetOriginals(instance.Data, true);
                    instance.UsingProxy = true;
                    ActiveProxyCount++;
                }
                else
                {
                    SetOriginals(instance.Data, false);
                    instance.Proxy.enabled = false;
                    instance.UsingProxy = false;
                    ActiveProxyCount--;
                }
            }
            cursor = limit == cells.Length ? 0 : limit;
        }

        private void SetOriginals(CityHlodCell cell, bool hide)
        {
            foreach (int index in cell.SourceRendererIndices)
                if (sources[index] != null)
                    sources[index].forceRenderingOff = hide;
        }
    }
}
