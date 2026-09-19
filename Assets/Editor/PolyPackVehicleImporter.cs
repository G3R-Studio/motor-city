using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PolyPackVehicleImporter
{
    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Vehicles";

    private const int VehicleCount = 4;

    static PolyPackVehicleImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Rebuild Vehicles - PolyPack Garage Cars")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Build(false);
    }

    private static void Build(
        bool force)
    {
        List<Candidate> candidates =
            FindCandidates();

        if (candidates.Count == 0)
        {
            if (force)
            {
                Debug.LogWarning(
                    "Motor City: Vehicles - PolyPack prefabs were not found. " +
                    "Import the free Vehicles - PolyPack package first.");
            }

            return;
        }

        Directory.CreateDirectory(
            OutputDirectory);

        List<Candidate> selected =
            SelectDistinctVehicles(
                candidates,
                VehicleCount);

        int written = 0;

        for (int i = 0;
             i < selected.Count;
             i++)
        {
            string outputPath =
                $"{OutputDirectory}/Vehicle_{i + 1:00}.prefab";

            if (!force &&
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    outputPath) != null)
            {
                written++;
                continue;
            }

            EnsureReadable(
                selected[i].Path);

            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    selected[i].Path);

            if (source == null)
                continue;

            GameObject instance =
                PrefabUtility.InstantiatePrefab(
                    source) as GameObject;

            if (instance == null)
            {
                instance =
                    UnityEngine.Object.Instantiate(
                        source);
            }

            instance.name =
                $"MotorCityVehicle_{i + 1:00}";

            try
            {
                bool wheelReady =
                    CountSeparableWheelParts(
                        instance) >= 4;

                if (!wheelReady)
                {
                    wheelReady =
                        TryExtractEmbeddedWheels(
                            instance,
                            i + 1);
                }

                if (!wheelReady)
                {
                    Debug.LogWarning(
                        $"Motor City: could not extract four animated wheels from " +
                        $"'{selected[i].Path}'. Vehicle slot {i + 1} was not rebuilt.");

                    continue;
                }

                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    outputPath);

                written++;

                Debug.Log(
                    $"Motor City: prepared garage vehicle {i + 1} " +
                    $"from '{selected[i].Path}' with four animated wheel meshes.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    instance);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (force)
        {
            if (written < VehicleCount)
            {
                Debug.LogWarning(
                    $"Motor City: prepared only {written}/{VehicleCount} driveable " +
                    "PolyPack vehicles. Embedded wheel extraction failed for one or more source models.");
            }
            else
            {
                Debug.Log(
                    $"Motor City: prepared {written}/{VehicleCount} " +
                    "Vehicles - PolyPack garage vehicles.");
            }
        }
    }

    private static List<Candidate> FindCandidates()
    {
        List<Candidate> explicitVehicles =
            FindExplicitStarterVehicles();

        if (explicitVehicles.Count >=
            VehicleCount)
        {
            return explicitVehicles;
        }

        string[] paths =
            AssetDatabase.GetAllAssetPaths();

        var result =
            new List<Candidate>(
                explicitVehicles);

        var seen =
            new HashSet<string>(
                result.Select(
                    item => item.Path),
                StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(
                    path) ||
                !path.StartsWith(
                    "Assets/",
                    StringComparison.OrdinalIgnoreCase))
                continue;

            string extension =
                Path.GetExtension(
                        path)
                    .ToLowerInvariant();

            if (extension != ".prefab" &&
                extension != ".fbx" &&
                extension != ".obj")
                continue;

            string lower =
                path.ToLowerInvariant();

            if (lower.Contains(
                    "/resources/motorcity/") ||
                lower.Contains(
                    "fantastic city generator") ||
                lower.Contains(
                    "arcade - free racing car") ||
                lower.Contains(
                    "cityvisual"))
                continue;

            bool packageMatch =
                lower.Contains("polypack") ||
                lower.Contains("alstra") ||
                lower.Contains("musclecar") ||
                lower.Contains("pickup") ||
                lower.Contains("suvv") ||
                lower.Contains("swifto") ||
                lower.Contains("minivan");

            if (!packageMatch)
                continue;

            if (seen.Contains(path))
                continue;

            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path);

            if (asset == null)
                continue;

            Renderer[] renderers =
                asset.GetComponentsInChildren<Renderer>(
                    true);

            if (renderers.Length == 0)
                continue;

            int score =
                Score(
                    path,
                    asset);

            if (score < -20)
                continue;

            result.Add(
                new Candidate
                {
                    Path = path,
                    Score = score,
                    Family =
                        FamilyKey(
                            path)
                });

            seen.Add(path);
        }

        if (result.Count == 0)
        {
            Debug.LogWarning(
                "Motor City: Vehicles - PolyPack scan found no usable GameObject assets. " +
                "Expected model names include SwiftoV1, MuscleCarV1, PickupV1 and SuvV1.");
        }
        else
        {
            Debug.Log(
                "Motor City: Vehicles - PolyPack candidates: " +
                string.Join(
                    " | ",
                    result
                        .OrderByDescending(
                            item => item.Score)
                        .Take(12)
                        .Select(
                            item =>
                                $"{item.Path} (score {item.Score})")));
        }

        return result
            .OrderByDescending(
                item => item.Score)
            .ThenBy(
                item => item.Path,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<Candidate> FindExplicitStarterVehicles()
    {
        string[] preferredNames =
        {
            "SwiftoV1",
            "MuscleCarV1",
            "PickupV1",
            "SuvV1"
        };

        var result =
            new List<Candidate>();

        foreach (string preferredName in
                 preferredNames)
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    preferredName);

            string bestPath =
                guids
                    .Select(
                        AssetDatabase.GUIDToAssetPath)
                    .Where(
                        path =>
                        {
                            if (string.IsNullOrWhiteSpace(
                                    path))
                                return false;

                            string extension =
                                Path.GetExtension(
                                        path)
                                    .ToLowerInvariant();

                            if (extension != ".fbx" &&
                                extension != ".prefab" &&
                                extension != ".obj")
                                return false;

                            string fileName =
                                Path.GetFileNameWithoutExtension(
                                    path);

                            return string.Equals(
                                fileName,
                                preferredName,
                                StringComparison.OrdinalIgnoreCase);
                        })
                    .OrderBy(
                        path =>
                            Path.GetExtension(
                                    path)
                                .Equals(
                                    ".prefab",
                                    StringComparison.OrdinalIgnoreCase)
                                ? 0
                                : 1)
                    .ThenBy(
                        path => path,
                        StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(
                    bestPath))
                continue;

            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    bestPath);

            if (asset == null)
                continue;

            result.Add(
                new Candidate
                {
                    Path = bestPath,
                    Score =
                        1000 -
                        result.Count * 10,
                    Family =
                        FamilyKey(
                            bestPath)
                });
        }

        if (result.Count > 0)
        {
            Debug.Log(
                "Motor City: explicit Vehicles - PolyPack models found: " +
                string.Join(
                    " | ",
                    result.Select(
                        item => item.Path)));
        }

        return result;
    }

    private static void EnsureReadable(
        string assetPath)
    {
        ModelImporter importer =
            AssetImporter.GetAtPath(
                assetPath) as ModelImporter;

        if (importer == null ||
            importer.isReadable)
            return;

        importer.isReadable = true;
        importer.SaveAndReimport();
    }

    private static bool TryExtractEmbeddedWheels(
        GameObject instance,
        int vehicleIndex)
    {
        if (instance == null)
            return false;

        MeshFilter[] filters =
            instance.GetComponentsInChildren<MeshFilter>(
                true);

        MeshFilter sourceFilter =
            filters
                .Where(
                    item =>
                        item != null &&
                        item.sharedMesh != null)
                .OrderByDescending(
                    item =>
                        item.sharedMesh.vertexCount)
                .FirstOrDefault();

        if (sourceFilter == null ||
            sourceFilter.sharedMesh == null)
            return false;

        Mesh source =
            sourceFilter.sharedMesh;

        if (!source.isReadable)
        {
            Debug.LogWarning(
                $"Motor City: mesh '{source.name}' is not readable, so embedded wheels cannot be extracted.");

            return false;
        }

        List<MeshIsland> islands =
            BuildMeshIslands(
                source);

        if (islands.Count < 5)
        {
            Debug.LogWarning(
                $"Motor City: mesh '{source.name}' contains only {islands.Count} disconnected geometry islands.");

            return false;
        }

        Bounds meshBounds =
            source.bounds;

        List<List<int>> wheelGroups =
            SelectWheelIslandGroups(
                islands,
                meshBounds);

        if (wheelGroups == null ||
            wheelGroups.Count != 4)
        {
            Debug.LogWarning(
                $"Motor City: could not classify four embedded wheel groups in mesh '{source.name}'.");

            return false;
        }

        string meshDirectory =
            $"{OutputDirectory}/GeneratedMeshes/Vehicle_{vehicleIndex:00}";

        if (AssetDatabase.IsValidFolder(
                meshDirectory))
        {
            string[] existing =
                AssetDatabase.FindAssets(
                    string.Empty,
                    new[] { meshDirectory });

            foreach (string guid in existing)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guid);

                if (!AssetDatabase.IsValidFolder(path))
                    AssetDatabase.DeleteAsset(path);
            }
        }
        else
        {
            Directory.CreateDirectory(
                meshDirectory);

            AssetDatabase.Refresh();
        }

        var wheelIslandIds =
            new HashSet<int>(
                wheelGroups.SelectMany(
                    group => group));

        Mesh bodyMesh =
            BuildMeshFromIslands(
                source,
                islands,
                Enumerable.Range(
                        0,
                        islands.Count)
                    .Where(
                        id =>
                            !wheelIslandIds.Contains(
                                id)),
                Vector3.zero);

        if (bodyMesh == null)
            return false;

        bodyMesh.name =
            $"Vehicle_{vehicleIndex:00}_Body";

        string bodyPath =
            $"{meshDirectory}/Body.asset";

        AssetDatabase.CreateAsset(
            bodyMesh,
            bodyPath);

        sourceFilter.sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(
                bodyPath);

        MeshRenderer sourceRenderer =
            sourceFilter.GetComponent<MeshRenderer>();

        Material[] materials =
            sourceRenderer != null
                ? sourceRenderer.sharedMaterials
                : Array.Empty<Material>();

        string[] names =
        {
            "Wheel_FL",
            "Wheel_FR",
            "Wheel_RL",
            "Wheel_RR"
        };

        for (int wheelIndex = 0;
             wheelIndex < 4;
             wheelIndex++)
        {
            List<int> group =
                wheelGroups[wheelIndex];

            Bounds groupBounds =
                CombinedIslandBounds(
                    islands,
                    group);

            Vector3 pivot =
                groupBounds.center;

            Mesh wheelMesh =
                BuildMeshFromIslands(
                    source,
                    islands,
                    group,
                    pivot);

            if (wheelMesh == null)
                return false;

            wheelMesh.name =
                $"Vehicle_{vehicleIndex:00}_{names[wheelIndex]}";

            string wheelPath =
                $"{meshDirectory}/{names[wheelIndex]}.asset";

            AssetDatabase.CreateAsset(
                wheelMesh,
                wheelPath);

            GameObject wheelObject =
                new(
                    names[wheelIndex]);

            wheelObject.transform.SetParent(
                sourceFilter.transform,
                false);

            wheelObject.transform.localPosition =
                pivot;

            wheelObject.transform.localRotation =
                Quaternion.identity;

            wheelObject.transform.localScale =
                Vector3.one;

            MeshFilter wheelFilter =
                wheelObject.AddComponent<MeshFilter>();

            wheelFilter.sharedMesh =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    wheelPath);

            MeshRenderer wheelRenderer =
                wheelObject.AddComponent<MeshRenderer>();

            wheelRenderer.sharedMaterials =
                materials;
        }

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Motor City: extracted four embedded wheel meshes from '{source.name}' " +
            $"({islands.Count} disconnected geometry islands).");

        return true;
    }

    private static List<MeshIsland> BuildMeshIslands(
        Mesh mesh)
    {
        int vertexCount =
            mesh.vertexCount;

        int[] parent =
            new int[vertexCount];

        for (int i = 0;
             i < vertexCount;
             i++)
        {
            parent[i] = i;
        }

        Vector3[] vertices =
            mesh.vertices;

        var positionOwners =
            new Dictionary<string, int>();

        for (int i = 0;
             i < vertices.Length;
             i++)
        {
            Vector3 v =
                vertices[i];

            string key =
                $"{Mathf.RoundToInt(v.x * 10000f)}:" +
                $"{Mathf.RoundToInt(v.y * 10000f)}:" +
                $"{Mathf.RoundToInt(v.z * 10000f)}";

            if (positionOwners.TryGetValue(
                    key,
                    out int other))
            {
                Union(
                    parent,
                    i,
                    other);
            }
            else
            {
                positionOwners.Add(
                    key,
                    i);
            }
        }

        var triangleRecords =
            new List<TriangleRecord>();

        for (int subMesh = 0;
             subMesh < mesh.subMeshCount;
             subMesh++)
        {
            int[] triangles =
                mesh.GetTriangles(
                    subMesh);

            for (int i = 0;
                 i + 2 < triangles.Length;
                 i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int d = triangles[i + 2];

                Union(parent, a, b);
                Union(parent, b, d);
                Union(parent, d, a);

                triangleRecords.Add(
                    new TriangleRecord
                    {
                        A = a,
                        B = b,
                        C = d,
                        SubMesh = subMesh
                    });
            }
        }

        var byRoot =
            new Dictionary<int, MeshIsland>();

        foreach (TriangleRecord triangle in
                 triangleRecords)
        {
            int root =
                Find(
                    parent,
                    triangle.A);

            if (!byRoot.TryGetValue(
                    root,
                    out MeshIsland island))
            {
                island =
                    new MeshIsland();

                byRoot.Add(
                    root,
                    island);
            }

            island.Triangles.Add(
                triangle);

            island.Vertices.Add(
                triangle.A);

            island.Vertices.Add(
                triangle.B);

            island.Vertices.Add(
                triangle.C);
        }

        foreach (MeshIsland island in
                 byRoot.Values)
        {
            bool first = true;

            foreach (int vertexIndex in
                     island.Vertices)
            {
                Vector3 vertex =
                    vertices[vertexIndex];

                if (first)
                {
                    island.Bounds =
                        new Bounds(
                            vertex,
                            Vector3.zero);

                    first = false;
                }
                else
                {
                    island.Bounds.Encapsulate(
                        vertex);
                }
            }
        }

        return byRoot.Values
            .Where(
                island =>
                    island.Triangles.Count > 0)
            .OrderByDescending(
                island =>
                    island.Triangles.Count)
            .ToList();
    }

    private static List<List<int>> SelectWheelIslandGroups(
        List<MeshIsland> islands,
        Bounds fullBounds)
    {
        bool lengthAlongZ =
            fullBounds.size.z >=
            fullBounds.size.x;

        float halfWidth =
            (lengthAlongZ
                ? fullBounds.extents.x
                : fullBounds.extents.z);

        float halfLength =
            (lengthAlongZ
                ? fullBounds.extents.z
                : fullBounds.extents.x);

        if (halfWidth < 0.001f ||
            halfLength < 0.001f)
            return null;

        var groups =
            new List<List<int>>();

        for (int longitudinalSign = 1;
             longitudinalSign >= -1;
             longitudinalSign -= 2)
        {
            for (int lateralSign = -1;
                 lateralSign <= 1;
                 lateralSign += 2)
            {
                int best = -1;
                float bestScore =
                    float.NegativeInfinity;

                for (int i = 0;
                     i < islands.Count;
                     i++)
                {
                    MeshIsland island =
                        islands[i];

                    Bounds bounds =
                        island.Bounds;

                    Vector3 center =
                        bounds.center -
                        fullBounds.center;

                    float lateral =
                        lengthAlongZ
                            ? center.x
                            : center.z;

                    float longitudinal =
                        lengthAlongZ
                            ? center.z
                            : center.x;

                    if (Mathf.Sign(
                            lateral) !=
                        lateralSign ||
                        Mathf.Sign(
                            longitudinal) !=
                        longitudinalSign)
                        continue;

                    float low =
                        Mathf.InverseLerp(
                            fullBounds.max.y,
                            fullBounds.min.y,
                            bounds.center.y);

                    float outward =
                        Mathf.Abs(lateral) /
                        halfWidth;

                    float foreAft =
                        Mathf.Abs(longitudinal) /
                        halfLength;

                    float largest =
                        Mathf.Max(
                            bounds.size.x,
                            bounds.size.y,
                            bounds.size.z);

                    float normalizedSize =
                        largest /
                        Mathf.Max(
                            fullBounds.size.x,
                            fullBounds.size.z);

                    if (normalizedSize > 0.42f)
                        continue;

                    float wheelLike =
                        1f -
                        Mathf.Abs(
                            normalizedSize -
                            0.18f) /
                        0.18f;

                    float score =
                        outward * 3f +
                        foreAft * 2f +
                        low * 2.5f +
                        wheelLike;

                    if (score >
                        bestScore)
                    {
                        bestScore =
                            score;

                        best = i;
                    }
                }

                if (best < 0)
                    return null;

                MeshIsland primary =
                    islands[best];

                float mergeRadius =
                    Mathf.Max(
                        primary.Bounds.size.x,
                        primary.Bounds.size.y,
                        primary.Bounds.size.z) *
                    0.85f;

                var group =
                    new List<int>();

                for (int i = 0;
                     i < islands.Count;
                     i++)
                {
                    MeshIsland island =
                        islands[i];

                    float distance =
                        Vector3.Distance(
                            island.Bounds.center,
                            primary.Bounds.center);

                    if (distance <=
                        mergeRadius)
                    {
                        group.Add(
                            i);
                    }
                }

                groups.Add(
                    group);
            }
        }

        return groups;
    }

    private static Mesh BuildMeshFromIslands(
        Mesh source,
        List<MeshIsland> islands,
        IEnumerable<int> islandIds,
        Vector3 pivot)
    {
        var trianglesBySubMesh =
            new List<int>[source.subMeshCount];

        for (int i = 0;
             i < trianglesBySubMesh.Length;
             i++)
        {
            trianglesBySubMesh[i] =
                new List<int>();
        }

        var used =
            new HashSet<int>();

        foreach (int islandId in
                 islandIds)
        {
            if (islandId < 0 ||
                islandId >= islands.Count)
                continue;

            foreach (TriangleRecord triangle in
                     islands[islandId].Triangles)
            {
                trianglesBySubMesh[
                    triangle.SubMesh]
                    .Add(
                        triangle.A);

                trianglesBySubMesh[
                    triangle.SubMesh]
                    .Add(
                        triangle.B);

                trianglesBySubMesh[
                    triangle.SubMesh]
                    .Add(
                        triangle.C);

                used.Add(triangle.A);
                used.Add(triangle.B);
                used.Add(triangle.C);
            }
        }

        if (used.Count == 0)
            return null;

        int[] sourceIndices =
            used.OrderBy(
                    index => index)
                .ToArray();

        var remap =
            new Dictionary<int, int>();

        for (int i = 0;
             i < sourceIndices.Length;
             i++)
        {
            remap[sourceIndices[i]] =
                i;
        }

        Vector3[] sourceVertices =
            source.vertices;

        Vector3[] vertices =
            sourceIndices
                .Select(
                    index =>
                        sourceVertices[index] -
                        pivot)
                .ToArray();

        Mesh mesh =
            new();

        mesh.indexFormat =
            source.indexFormat;

        mesh.vertices =
            vertices;

        CopyOptionalVertexData(
            source,
            mesh,
            sourceIndices);

        mesh.subMeshCount =
            source.subMeshCount;

        for (int subMesh = 0;
             subMesh < source.subMeshCount;
             subMesh++)
        {
            int[] triangles =
                trianglesBySubMesh[
                        subMesh]
                    .Select(
                        oldIndex =>
                            remap[oldIndex])
                    .ToArray();

            mesh.SetTriangles(
                triangles,
                subMesh,
                false);
        }

        mesh.RecalculateBounds();

        if (mesh.normals == null ||
            mesh.normals.Length !=
            mesh.vertexCount)
        {
            mesh.RecalculateNormals();
        }

        return mesh;
    }

    private static void CopyOptionalVertexData(
        Mesh source,
        Mesh destination,
        int[] indices)
    {
        Vector3[] normals =
            source.normals;

        if (normals != null &&
            normals.Length ==
            source.vertexCount)
        {
            destination.normals =
                indices.Select(
                        index =>
                            normals[index])
                    .ToArray();
        }

        Vector4[] tangents =
            source.tangents;

        if (tangents != null &&
            tangents.Length ==
            source.vertexCount)
        {
            destination.tangents =
                indices.Select(
                        index =>
                            tangents[index])
                    .ToArray();
        }

        Vector2[] uv =
            source.uv;

        if (uv != null &&
            uv.Length ==
            source.vertexCount)
        {
            destination.uv =
                indices.Select(
                        index =>
                            uv[index])
                    .ToArray();
        }

        Vector2[] uv2 =
            source.uv2;

        if (uv2 != null &&
            uv2.Length ==
            source.vertexCount)
        {
            destination.uv2 =
                indices.Select(
                        index =>
                            uv2[index])
                    .ToArray();
        }

        Color[] colors =
            source.colors;

        if (colors != null &&
            colors.Length ==
            source.vertexCount)
        {
            destination.colors =
                indices.Select(
                        index =>
                            colors[index])
                    .ToArray();
        }
    }

    private static Bounds CombinedIslandBounds(
        List<MeshIsland> islands,
        IEnumerable<int> ids)
    {
        bool first = true;
        Bounds result =
            new(
                Vector3.zero,
                Vector3.zero);

        foreach (int id in ids)
        {
            if (id < 0 ||
                id >= islands.Count)
                continue;

            if (first)
            {
                result =
                    islands[id].Bounds;

                first = false;
            }
            else
            {
                result.Encapsulate(
                    islands[id].Bounds);
            }
        }

        return result;
    }

    private static int Find(
        int[] parent,
        int value)
    {
        while (parent[value] !=
               value)
        {
            parent[value] =
                parent[parent[value]];

            value =
                parent[value];
        }

        return value;
    }

    private static void Union(
        int[] parent,
        int a,
        int b)
    {
        int rootA =
            Find(
                parent,
                a);

        int rootB =
            Find(
                parent,
                b);

        if (rootA !=
            rootB)
        {
            parent[rootB] =
                rootA;
        }
    }

    private sealed class MeshIsland
    {
        public readonly List<TriangleRecord> Triangles =
            new();

        public readonly HashSet<int> Vertices =
            new();

        public Bounds Bounds;
    }

    private sealed class TriangleRecord
    {
        public int A;
        public int B;
        public int C;
        public int SubMesh;
    }

    private static int CountSeparableWheelParts(
        GameObject asset)
    {
        if (asset == null)
            return 0;

        Transform root =
            asset.transform;

        Transform[] all =
            asset.GetComponentsInChildren<Transform>(
                true);

        int named = 0;

        foreach (Transform item in all)
        {
            if (item == root)
                continue;

            string name =
                item.name.ToLowerInvariant();

            if (!(name.Contains("wheel") ||
                  name.Contains("tire") ||
                  name.Contains("tyre")))
                continue;

            if (item.GetComponentInChildren<Renderer>(
                    true) == null)
                continue;

            named++;
        }

        if (named >= 4)
            return named;

        Renderer[] renderers =
            asset.GetComponentsInChildren<Renderer>(
                true);

        if (renderers.Length < 5)
            return named;

        Bounds fullBounds =
            renderers[0].bounds;

        for (int i = 1;
             i < renderers.Length;
             i++)
        {
            fullBounds.Encapsulate(
                renderers[i].bounds);
        }

        Vector3 rootCenter =
            root.InverseTransformPoint(
                fullBounds.center);

        Vector3 fullSize =
            fullBounds.size;

        float horizontalMax =
            Mathf.Max(
                fullSize.x,
                fullSize.z);

        var candidates =
            new List<Vector3>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Bounds bounds =
                renderer.bounds;

            Vector3 local =
                root.InverseTransformPoint(
                    bounds.center);

            Vector3 size =
                bounds.size;

            bool low =
                bounds.center.y <=
                fullBounds.min.y +
                fullBounds.size.y *
                0.48f;

            bool small =
                Mathf.Max(
                    size.x,
                    size.y,
                    size.z) <=
                horizontalMax *
                0.34f;

            bool awayFromCenter =
                Mathf.Abs(
                    local.x -
                    rootCenter.x) >=
                    fullSize.x *
                    0.18f ||
                Mathf.Abs(
                    local.z -
                    rootCenter.z) >=
                    fullSize.z *
                    0.18f;

            if (!low ||
                !small ||
                !awayFromCenter)
                continue;

            candidates.Add(
                local);
        }

        if (candidates.Count < 4)
            return Mathf.Max(
                named,
                candidates.Count);

        bool lengthAlongZ =
            fullSize.z >=
            fullSize.x;

        bool[] quadrants =
            new bool[4];

        foreach (Vector3 local in
                 candidates)
        {
            float lateral =
                lengthAlongZ
                    ? local.x - rootCenter.x
                    : local.z - rootCenter.z;

            float longitudinal =
                lengthAlongZ
                    ? local.z - rootCenter.z
                    : local.x - rootCenter.x;

            int index =
                (longitudinal >= 0f
                    ? 0
                    : 2) +
                (lateral >= 0f
                    ? 1
                    : 0);

            quadrants[index] =
                true;
        }

        int covered = 0;

        foreach (bool quadrant in quadrants)
        {
            if (quadrant)
                covered++;
        }

        return Mathf.Max(
            named,
            covered);
    }

    private static int Score(
        string path,
        GameObject prefab)
    {
        string lower =
            path.ToLowerInvariant();

        int score = 0;

        if (lower.Contains("prefab"))
            score += 25;

        string[] strong =
        {
            "car",
            "sedan",
            "coupe",
            "sport",
            "race",
            "muscle",
            "super",
            "pickup",
            "suv",
            "hatch"
        };

        foreach (string token in strong)
        {
            if (lower.Contains(token))
                score += 18;
        }

        string[] reject =
        {
            "wheel",
            "tire",
            "tyre",
            "trailer",
            "traffic",
            "prop",
            "demo",
            "scene",
            "showcase"
        };

        foreach (string token in reject)
        {
            if (lower.Contains(token))
                score -= 55;
        }

        int wheelLike =
            prefab
                .GetComponentsInChildren<Transform>(
                    true)
                .Count(
                    item =>
                    {
                        string name =
                            item.name.ToLowerInvariant();

                        return
                            name.Contains("wheel") ||
                            name.Contains("tire") ||
                            name.Contains("tyre");
                    });

        if (wheelLike >= 4)
            score += 55;
        else if (wheelLike > 0)
            score += 10;

        return score;
    }

    private static List<Candidate> SelectDistinctVehicles(
        List<Candidate> candidates,
        int count)
    {
        var selected =
            new List<Candidate>();

        var families =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (Candidate candidate in candidates)
        {
            if (families.Contains(
                    candidate.Family))
                continue;

            selected.Add(
                candidate);

            families.Add(
                candidate.Family);

            if (selected.Count >= count)
                return selected;
        }

        foreach (Candidate candidate in candidates)
        {
            if (selected.Any(
                    item =>
                        item.Path ==
                        candidate.Path))
                continue;

            selected.Add(
                candidate);

            if (selected.Count >= count)
                break;
        }

        return selected;
    }

    private static string FamilyKey(
        string path)
    {
        string stem =
            Path.GetFileNameWithoutExtension(
                    path)
                .ToLowerInvariant();

        string[] removable =
        {
            "blue",
            "red",
            "green",
            "yellow",
            "black",
            "white",
            "orange",
            "purple",
            "grey",
            "gray",
            "variant",
            "prefab"
        };

        foreach (string token in removable)
        {
            stem =
                stem.Replace(
                    token,
                    string.Empty);
        }

        return stem
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);
    }

    private sealed class Candidate
    {
        public string Path;
        public int Score;
        public string Family;
    }
}
