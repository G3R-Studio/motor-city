using System;
using UnityEngine;

namespace MotorCity.World
{
    [Serializable]
    public sealed class CityHlodCell
    {
        public Vector3 Center;
        public Mesh ProxyMesh;
        public Material ProxyMaterial;
        public int[] SourceRendererIndices;
        public string[] SourceRendererNames;
        public int SourceTriangles;
        public int ProxyTriangles;
    }

    /// <summary>
    /// Editor-baked city-cell proxy geometry. Kept separate from the authored
    /// CityVisual.prefab. The array indices refer to MeshRenderer traversal
    /// order and are checked against exact renderer names at runtime.
    /// </summary>
    public sealed class CityHlodData : ScriptableObject
    {
        public int SourceRendererCount;
        public int SourceTriangles;
        public int ProxyTriangles;
        public CityHlodCell[] Cells;
    }
}
