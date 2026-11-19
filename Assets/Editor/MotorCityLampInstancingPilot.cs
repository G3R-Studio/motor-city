using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotorCity.EditorTools
{
    [InitializeOnLoad]
    public static class MotorCityLampInstancingPilot
    {
        private static readonly Dictionary<MeshRenderer, Material[]> originals = new();
        private static readonly Dictionary<Material, Material> copies = new();
        public static int Count => originals.Count;
        static MotorCityLampInstancingPilot()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Restore();
            };
            AssemblyReloadEvents.beforeAssemblyReload += Restore;
        }

        [MenuItem("Tools/Motor City/Audit/Lamp Instancing Pilot ON")]
        private static void Enable()
        {
            if (!EditorApplication.isPlaying || Count > 0) return;
            var city = GameObject.Find("MotorCity_FCGCity");
            if (city == null || !SystemInfo.supportsInstancing) { Debug.LogWarning("Lamp pilot: city or instancing unavailable."); return; }
            var block = new MaterialPropertyBlock();
            // Limit to the exact authored lamp body mesh, never _LightV.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fantastic City Generator/Objects/StreetLight/Prefab/StreetLight-01.prefab");
            if (prefab == null || !prefab.TryGetComponent<MeshFilter>(out var source)) return;
            var mesh = source.sharedMesh;
            if (mesh == null) return;
            foreach (var renderer in city.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    renderer.isPartOfStaticBatch || renderer.forceRenderingOff || renderer.HasPropertyBlock() ||
                    !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh != mesh) continue;
                var mats = renderer.sharedMaterials;
                if (mats.Length != mesh.subMeshCount) continue;
                bool valid = true;
                foreach (var mat in mats)
                    if (mat == null || mat.renderQueue >= 2450) valid = false;
                if (!valid) continue;
                var replacements = new Material[mats.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    if (!copies.TryGetValue(mats[i], out var clone))
                    {
                        clone = new Material(mats[i]) { enableInstancing = true, hideFlags = HideFlags.HideAndDontSave };
                        copies.Add(mats[i], clone);
                    }
                    replacements[i] = clone;
                }
                originals.Add(renderer, mats);
                renderer.sharedMaterials = replacements;
                // A nonempty block excludes this renderer from SRP Batcher.
                // Unused marker has no intentional shader visual effect.
                block.Clear();
                block.SetFloat("_MotorCityInstancingPilotMarker", 1f);
                renderer.SetPropertyBlock(block);
            }
            Debug.Log($"Lamp instancing pilot ON: {Count} renderers, {copies.Count} shared material copies. Verify instanced events in Frame Debugger.");
        }

        [MenuItem("Tools/Motor City/Audit/Lamp Instancing Pilot OFF")]
        private static void Restore()
        {
            foreach (var pair in originals)
            {
                if (pair.Key == null) continue;
                pair.Key.SetPropertyBlock(null);
                pair.Key.sharedMaterials = pair.Value;
            }
            originals.Clear();
            foreach (var clone in copies.Values)
                if (clone != null) Object.DestroyImmediate(clone);
            copies.Clear();
        }

        [MenuItem("Tools/Motor City/Audit/Lamp Instancing Pilot ON", true)]
        [MenuItem("Tools/Motor City/Audit/Lamp Instancing Pilot OFF", true)]
        private static bool Validate() => EditorApplication.isPlaying;
    }
}
