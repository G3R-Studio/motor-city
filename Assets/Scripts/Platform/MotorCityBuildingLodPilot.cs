using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.Platform
{
    public static class MotorCityBuildingLodPilot
    {
        private static readonly Dictionary<MeshFilter, Mesh> lodOriginals = new();
        private static readonly Dictionary<MeshRenderer, short> lodOverrides = new();
        private static bool lodPilotEnabled;
        public static bool Enabled => lodPilotEnabled;
        public static int RendererCount => lodOriginals.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { lodOriginals.Clear(); lodOverrides.Clear(); lodPilotEnabled = false; }

        public static void SetEnabled(bool enabled)
        {
            if (!enabled)
            {
                foreach (var pair in lodOriginals)
                    if (pair.Key != null) pair.Key.sharedMesh = pair.Value;
                foreach (var pair in lodOverrides)
                    if (pair.Key != null) pair.Key.forceMeshLod = pair.Value;
                lodOriginals.Clear();
                lodOverrides.Clear();
                lodPilotEnabled = false;
                Debug.Log("Building LOD pilot OFF; original meshes restored.");
                return;
            }
            if (lodPilotEnabled) return;
            var city = GameObject.Find("MotorCity_FCGCity");
            if (city == null) { Debug.LogWarning("Enter city first."); return; }
            var names = new HashSet<string> { "BR-003", "EC-009", "BR-002", "QD-14", "BR-014", "BR-027", "BC-P07", "BR-025", "BC-053", "EC-006" };
            var copies = Resources.LoadAll<Mesh>("MotorCityLodPilot");
            var expanded = Resources.LoadAll<Mesh>("MotorCityLodExpanded");
            var byName = new Dictionary<string, Mesh>();
            foreach (var copy in copies)
                if (copy != null && names.Contains(copy.name) && !byName.ContainsKey(copy.name))
                    byName.Add(copy.name, copy);
            foreach (var copy in expanded)
                if (copy != null) byName[copy.name] = copy;
            var filters = city.GetComponentsInChildren<MeshFilter>(true);
            var uniqueNames = new Dictionary<string, HashSet<Mesh>>();
            foreach (var f in filters)
            {
                var m = f.sharedMesh;
                if (m == null) continue;
                if (!uniqueNames.TryGetValue(m.name, out var set)) uniqueNames[m.name] = set = new HashSet<Mesh>();
                set.Add(m);
            }
            foreach (var filter in filters)
            {
                var original = filter.sharedMesh;
                if (original == null || uniqueNames[original.name].Count != 1 || !byName.TryGetValue(original.name, out var copy) ||
                    original.vertexCount != copy.vertexCount || original.subMeshCount != copy.subMeshCount ||
                    original.bounds != copy.bounds ||
                    filter.GetComponentInParent<Rigidbody>(true) != null ||
                    filter.GetComponentInParent<Animator>(true) != null ||
                    filter.GetComponentInParent<LODGroup>(true) != null) continue;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || renderer.isPartOfStaticBatch) continue;
                lodOriginals.Add(filter, original);
                lodOverrides.Add(renderer, renderer.forceMeshLod);
                filter.sharedMesh = copy;
                renderer.forceMeshLod = -1;
            }
            lodPilotEnabled = lodOriginals.Count > 0;
            Debug.Log($"Building LOD pilot: enabled={lodPilotEnabled} renderers={lodOriginals.Count} generatedMeshes={byName.Count}");
        }


    }
}
