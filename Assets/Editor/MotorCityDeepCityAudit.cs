using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Deep, read-only audit of a selected city hierarchy.
/// Exports a detailed hierarchy plus object, component, block, mesh, material,
/// texture, collider, light and script inventories as CSV/TXT files.
/// No scene, prefab, importer or asset settings are modified.
/// </summary>
public static class MotorCityDeepCityAudit
{
    private sealed class BlockStats
    {
        public string Name;
        public string Path;
        public int GameObjects;
        public int ActiveGameObjects;
        public int Components;
        public int MeshFilters;
        public int MeshRenderers;
        public int SkinnedRenderers;
        public int MaterialSlots;
        public int Colliders;
        public int Lights;
        public long VertexInstances;
        public long TriangleInstances;
        public HashSet<string> MeshAssets = new HashSet<string>();
        public HashSet<string> MaterialAssets = new HashSet<string>();
        public HashSet<string> TextureAssets = new HashSet<string>();
    }

    private sealed class MeshUse
    {
        public string Block;
        public string Hierarchy;
        public string GameObject;
        public string Component;
        public string MeshName;
        public string AssetPath;
        public int Vertices;
        public long Triangles;
        public int SubMeshes;
        public string Materials;
    }

    private sealed class AssetUse
    {
        public string Path;
        public string Name;
        public string Type;
        public long Bytes;
        public int Width;
        public int Height;
        public int Uses;
        public string Details;
    }

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    [MenuItem("Tools/Motor City/Deep City Audit/Run Full Audit")]
    private static void RunAudit()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Motor City Deep Audit",
                "Select the CityVisual root object in the Hierarchy, then run the audit.",
                "OK");
            return;
        }

        string outputFolder = EditorUtility.SaveFolderPanel(
            "Choose folder for Motor City audit reports",
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "MotorCity_Deep_Audit");

        if (string.IsNullOrEmpty(outputFolder))
            return;

        try
        {
            Analyze(root, outputFolder);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Motor City Deep Audit",
                "The audit stopped because of an error. Check the Console for details.\n\n" +
                exception.Message, "OK");
        }
    }

    private static void Analyze(GameObject root, string outputFolder)
    {
        Transform rootTransform = root.transform;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        var blocks = new Dictionary<string, BlockStats>(StringComparer.Ordinal);
        var meshUses = new List<MeshUse>();
        var objectRows = new StringBuilder();
        var componentRows = new StringBuilder();
        var colliderRows = new StringBuilder();
        var lightRows = new StringBuilder();
        var scriptRows = new StringBuilder();
        var materialUses = new Dictionary<string, AssetUse>(StringComparer.Ordinal);
        var textureUses = new Dictionary<string, AssetUse>(StringComparer.Ordinal);
        var meshAssets = new Dictionary<string, AssetUse>(StringComparer.Ordinal);

        var totals = new BlockStats { Name = "TOTAL", Path = GetPath(rootTransform, rootTransform) };
        var hierarchy = new StringBuilder();
        hierarchy.AppendLine("MOTOR CITY - FULL HIERARCHY AUDIT");
        hierarchy.AppendLine("Root: " + root.name);
        hierarchy.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Invariant));
        hierarchy.AppendLine("Objects include inactive descendants.");
        hierarchy.AppendLine();

        objectRows.AppendLine("HierarchyPath,Block,Name,Depth,ActiveSelf,ActiveInHierarchy,Layer,Tag,StaticFlags,LocalPosition,LocalRotation,LocalScale,WorldPosition,WorldScale,ComponentCount,ComponentTypes,PrefabSource");
        componentRows.AppendLine("HierarchyPath,Block,GameObject,ComponentIndex,ComponentType,Enabled,Details,AssetPath,SourceBytes");
        colliderRows.AppendLine("HierarchyPath,Block,GameObject,ColliderType,Enabled,IsTrigger,Material,CenterOrBounds,SizeOrRadius,Mesh,Convex");
        lightRows.AppendLine("HierarchyPath,Block,GameObject,LightType,Enabled,Color,Intensity,Range,Shadows,SpotAngle");
        scriptRows.AppendLine("HierarchyPath,Block,GameObject,ScriptType,Enabled,ScriptAssetPath,SourceBytes");

        int processed = 0;
        try
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t == null) continue;

                GameObject go = t.gameObject;
                string path = GetPath(t, rootTransform);
                int depth = GetDepth(t, rootTransform);
                BlockStats block = GetBlockStats(t, rootTransform, blocks);
                Component[] components = go.GetComponents<Component>();
                string componentTypes = string.Join(" | ", components
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name).ToArray());

                totals.GameObjects++;
                block.GameObjects++;
                if (go.activeInHierarchy) { totals.ActiveGameObjects++; block.ActiveGameObjects++; }
                totals.Components += components.Length;
                block.Components += components.Length;

                AppendHierarchyLine(hierarchy, t, rootTransform, depth, components);
                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(go);
                string prefabPath = prefabSource != null
                    ? AssetDatabase.GetAssetPath(prefabSource) : string.Empty;

                objectRows.Append(Csv(path)).Append(',')
                    .Append(Csv(block.Path)).Append(',')
                    .Append(Csv(go.name)).Append(',')
                    .Append(depth).Append(',')
                    .Append(go.activeSelf).Append(',')
                    .Append(go.activeInHierarchy).Append(',')
                    .Append(go.layer).Append(',')
                    .Append(Csv(go.tag)).Append(',')
                    .Append(Csv(GameObjectUtility.GetStaticEditorFlags(go).ToString())).Append(',')
                    .Append(Csv(V3(t.localPosition))).Append(',')
                    .Append(Csv(V3(t.localEulerAngles))).Append(',')
                    .Append(Csv(V3(t.localScale))).Append(',')
                    .Append(Csv(V3(t.position))).Append(',')
                    .Append(Csv(V3(t.lossyScale))).Append(',')
                    .Append(components.Length).Append(',')
                    .Append(Csv(componentTypes)).Append(',')
                    .Append(Csv(prefabPath)).AppendLine();

                for (int ci = 0; ci < components.Length; ci++)
                {
                    Component c = components[ci];
                    if (c == null)
                    {
                        componentRows.Append(Csv(path)).Append(',')
                            .Append(Csv(block.Path)).Append(',')
                            .Append(Csv(go.name)).Append(',')
                            .Append(ci).Append(",MISSING_SCRIPT,,,,").AppendLine();
                        continue;
                    }

                    string details = DescribeComponent(c);
                    string assetPath = GetComponentAssetPath(c);
                    long sourceBytes = GetFileBytes(assetPath);

                    componentRows.Append(Csv(path)).Append(',')
                        .Append(Csv(block.Path)).Append(',')
                        .Append(Csv(go.name)).Append(',')
                        .Append(ci).Append(',')
                        .Append(Csv(c.GetType().FullName)).Append(',')
                        .Append(Csv(GetEnabledState(c))).Append(',')
                        .Append(Csv(details)).Append(',')
                        .Append(Csv(assetPath)).Append(',')
                        .Append(sourceBytes).AppendLine();

                    MonoBehaviour behaviour = c as MonoBehaviour;
                    if (behaviour != null)
                    {
                        MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
                        string scriptPath = script != null ? AssetDatabase.GetAssetPath(script) : string.Empty;
                        scriptRows.Append(Csv(path)).Append(',')
                            .Append(Csv(block.Path)).Append(',')
                            .Append(Csv(go.name)).Append(',')
                            .Append(Csv(c.GetType().FullName)).Append(',')
                            .Append(behaviour.enabled).Append(',')
                            .Append(Csv(scriptPath)).Append(',')
                            .Append(GetFileBytes(scriptPath)).AppendLine();
                    }

                    Collider collider = c as Collider;
                    if (collider != null)
                    {
                        totals.Colliders++; block.Colliders++;
                        colliderRows.Append(Csv(path)).Append(',')
                            .Append(Csv(block.Path)).Append(',')
                            .Append(Csv(go.name)).Append(',')
                            .Append(Csv(collider.GetType().Name)).Append(',')
                            .Append(collider.enabled).Append(',')
                            .Append(collider.isTrigger).Append(',')
                            .Append(Csv(collider.sharedMaterial != null ? collider.sharedMaterial.name : "")).Append(',')
                            .Append(Csv(V3(collider.bounds.center))).Append(',')
                            .Append(Csv(V3(collider.bounds.size))).Append(',')
                            .Append(Csv(collider is MeshCollider ? AssetDatabase.GetAssetPath(((MeshCollider)collider).sharedMesh) : "")).Append(',')
                            .Append(collider is MeshCollider && ((MeshCollider)collider).convex).AppendLine();
                    }

                    Light light = c as Light;
                    if (light != null)
                    {
                        totals.Lights++; block.Lights++;
                        lightRows.Append(Csv(path)).Append(',')
                            .Append(Csv(block.Path)).Append(',')
                            .Append(Csv(go.name)).Append(',')
                            .Append(light.type).Append(',')
                            .Append(light.enabled).Append(',')
                            .Append(Csv(ColorText(light.color))).Append(',')
                            .Append(light.intensity.ToString("F3", Invariant)).Append(',')
                            .Append(light.range.ToString("F3", Invariant)).Append(',')
                            .Append(light.shadows).Append(',')
                            .Append(light.spotAngle.ToString("F2", Invariant)).AppendLine();
                    }
                }

                MeshFilter filter = go.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
                if (filter != null) { totals.MeshFilters++; block.MeshFilters++; }
                if (meshRenderer != null) { totals.MeshRenderers++; block.MeshRenderers++; }

                if (filter != null && filter.sharedMesh != null)
                    AddMeshUse(filter.sharedMesh, meshRenderer, "MeshFilter", t, rootTransform,
                        block, meshUses, meshAssets, materialUses, textureUses);

                SkinnedMeshRenderer[] skinned = go.GetComponents<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer skinnedRenderer in skinned)
                {
                    totals.SkinnedRenderers++; block.SkinnedRenderers++;
                    if (skinnedRenderer.sharedMesh != null)
                        AddMeshUse(skinnedRenderer.sharedMesh, skinnedRenderer, "SkinnedMeshRenderer",
                            t, rootTransform, block, meshUses, meshAssets, materialUses, textureUses);
                }

                if (meshRenderer != null)
                    AddMaterials(meshRenderer.sharedMaterials, block, materialUses, textureUses);
                foreach (SkinnedMeshRenderer sr in skinned)
                    AddMaterials(sr.sharedMaterials, block, materialUses, textureUses);

                LODGroup lod = go.GetComponent<LODGroup>();
                if (lod != null)
                {
                    LOD[] levels = lod.GetLODs();
                    hierarchy.Append(new string(' ', depth * 2 + 2))
                        .Append("  [LODGroup] levels=").Append(levels.Length)
                        .Append(" crossFade=").Append(lod.fadeMode)
                        .AppendLine();
                }

                processed++;
                if (processed % 100 == 0)
                    EditorUtility.DisplayProgressBar("Motor City Deep Audit",
                        "Analyzing " + path, (float)i / Math.Max(1, transforms.Length));
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        WriteFile(outputFolder, "01_Full_Hierarchy.txt", hierarchy.ToString());
        WriteFile(outputFolder, "02_All_GameObjects.csv", objectRows.ToString());
        WriteFile(outputFolder, "03_All_Components.csv", componentRows.ToString());
        WriteFile(outputFolder, "04_City_Blocks_Summary.csv", BuildBlockCsv(blocks, totals));
        WriteFile(outputFolder, "05_Mesh_Instances.csv", BuildMeshCsv(meshUses));
        WriteFile(outputFolder, "06_Unique_Mesh_Assets.csv", BuildAssetCsv(meshAssets.Values));
        WriteFile(outputFolder, "07_Materials.csv", BuildAssetCsv(materialUses.Values));
        WriteFile(outputFolder, "08_Textures.csv", BuildAssetCsv(textureUses.Values));
        WriteFile(outputFolder, "09_Colliders.csv", colliderRows.ToString());
        WriteFile(outputFolder, "10_Lights.csv", lightRows.ToString());
        WriteFile(outputFolder, "11_MonoBehaviours_Scripts.csv", scriptRows.ToString());
        WriteFile(outputFolder, "12_Summary.txt", BuildSummary(root, totals, blocks, meshUses, materialUses, textureUses, transforms.Length));

        AssetDatabase.Refresh();
        Debug.Log("[Motor City Deep Audit] Finished. Root=" + root.name +
            "; GameObjects=" + totals.GameObjects +
            "; Mesh instances=" + meshUses.Count +
            "; blocks=" + blocks.Count +
            "; output=" + outputFolder);

        EditorUtility.DisplayDialog("Motor City Deep Audit Complete",
            "Full audit finished.\n\nGameObjects: " + totals.GameObjects +
            "\nComponents: " + totals.Components +
            "\nMesh instances: " + meshUses.Count +
            "\nCity blocks: " + blocks.Count +
            "\nUnique mesh assets: " + meshAssets.Count +
            "\nMaterials: " + materialUses.Count +
            "\nTextures: " + textureUses.Count +
            "\n\n12 detailed reports saved to:\n" + outputFolder,
            "OK");
    }

    private static void AddMeshUse(
        Mesh mesh, Renderer renderer, string componentName, Transform t, Transform root,
        BlockStats block, List<MeshUse> meshUses, Dictionary<string, AssetUse> meshAssets,
        Dictionary<string, AssetUse> materialUses, Dictionary<string, AssetUse> textureUses)
    {
        long triangles = 0;
        for (int s = 0; s < mesh.subMeshCount; s++)
            if (mesh.GetTopology(s) == MeshTopology.Triangles)
                triangles += (long)mesh.GetIndexCount(s) / 3L;

        string assetPath = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
        string key = string.IsNullOrEmpty(assetPath) ? "SCENE:" + mesh.name : assetPath + "|" + mesh.name;
        string materials = renderer != null
            ? string.Join(" | ", renderer.sharedMaterials.Where(m => m != null).Select(m => m.name).ToArray())
            : string.Empty;

        meshUses.Add(new MeshUse
        {
            Block = block.Path,
            Hierarchy = GetPath(t, root),
            GameObject = t.name,
            Component = componentName,
            MeshName = mesh.name,
            AssetPath = assetPath,
            Vertices = mesh.vertexCount,
            Triangles = triangles,
            SubMeshes = mesh.subMeshCount,
            Materials = materials
        });

        block.VertexInstances += mesh.vertexCount;
        block.TriangleInstances += triangles;
        if (!string.IsNullOrEmpty(assetPath)) block.MeshAssets.Add(assetPath);

        AssetUse info;
        if (!meshAssets.TryGetValue(key, out info))
        {
            info = new AssetUse
            {
                Path = assetPath,
                Name = mesh.name,
                Type = "Mesh",
                Bytes = GetFileBytes(assetPath),
                Details = "Vertices=" + mesh.vertexCount + "; Triangles=" + triangles +
                    "; SubMeshes=" + mesh.subMeshCount
            };
            meshAssets.Add(key, info);
        }
        info.Uses++;

        if (renderer != null)
            AddMaterials(renderer.sharedMaterials, block, materialUses, textureUses);
    }

    private static void AddMaterials(Material[] materials, BlockStats block,
        Dictionary<string, AssetUse> materialUses, Dictionary<string, AssetUse> textureUses)
    {
        if (materials == null) return;
        block.MaterialSlots += materials.Length;
        foreach (Material material in materials)
        {
            if (material == null) continue;
            string path = AssetDatabase.GetAssetPath(material).Replace('\\', '/');
            string key = string.IsNullOrEmpty(path) ? "SCENE:" + material.name : path + "|" + material.name;
            block.MaterialAssets.Add(key);

            AssetUse info;
            if (!materialUses.TryGetValue(key, out info))
            {
                info = new AssetUse
                {
                    Path = path,
                    Name = material.name,
                    Type = "Material",
                    Bytes = GetFileBytes(path),
                    Details = "Shader=" + (material.shader != null ? material.shader.name : "Missing")
                };
                materialUses.Add(key, info);
            }
            info.Uses++;

            string[] properties = material.GetTexturePropertyNames();
            foreach (string property in properties)
            {
                Texture texture = material.GetTexture(property);
                if (texture == null) continue;
                string texturePath = AssetDatabase.GetAssetPath(texture).Replace('\\', '/');
                string textureKey = string.IsNullOrEmpty(texturePath)
                    ? "SCENE:" + texture.name : texturePath + "|" + texture.name;
                AssetUse textureInfo;
                if (!textureUses.TryGetValue(textureKey, out textureInfo))
                {
                    textureInfo = new AssetUse
                    {
                        Path = texturePath,
                        Name = texture.name,
                        Type = texture.GetType().Name,
                        Bytes = GetFileBytes(texturePath),
                        Width = texture.width,
                        Height = texture.height,
                        Details = "Material property=" + property
                    };
                    textureUses.Add(textureKey, textureInfo);
                }
                textureInfo.Uses++;
                block.TextureAssets.Add(textureKey);
            }
        }
    }

    private static BlockStats GetBlockStats(Transform t, Transform root,
        Dictionary<string, BlockStats> blocks)
    {
        Transform current = t;
        Transform blockTransform = null;
        while (current != null && current != root)
        {
            if (current.name.StartsWith("Double-Block", StringComparison.OrdinalIgnoreCase))
            {
                blockTransform = current;
                break;
            }
            current = current.parent;
        }

        string path = blockTransform != null
            ? GetPath(blockTransform, root)
            : "(outside Double-Block)";
        BlockStats stats;
        if (!blocks.TryGetValue(path, out stats))
        {
            stats = new BlockStats
            {
                Name = blockTransform != null ? blockTransform.name : "(outside Double-Block)",
                Path = path
            };
            blocks.Add(path, stats);
        }
        return stats;
    }

    private static string BuildBlockCsv(Dictionary<string, BlockStats> blocks, BlockStats total)
    {
        var csv = new StringBuilder();
        csv.AppendLine("BlockPath,BlockName,GameObjects,ActiveGameObjects,Components,MeshFilters,MeshRenderers,SkinnedRenderers,MaterialSlots,UniqueMeshFiles,UniqueMaterials,UniqueTextures,Colliders,Lights,VertexInstances,TriangleInstances");
        foreach (BlockStats b in blocks.Values.OrderByDescending(x => x.TriangleInstances)
                     .ThenByDescending(x => x.GameObjects))
            AppendBlock(csv, b);
        AppendBlock(csv, total);
        return csv.ToString();
    }

    private static void AppendBlock(StringBuilder csv, BlockStats b)
    {
        csv.Append(Csv(b.Path)).Append(',')
            .Append(Csv(b.Name)).Append(',')
            .Append(b.GameObjects).Append(',')
            .Append(b.ActiveGameObjects).Append(',')
            .Append(b.Components).Append(',')
            .Append(b.MeshFilters).Append(',')
            .Append(b.MeshRenderers).Append(',')
            .Append(b.SkinnedRenderers).Append(',')
            .Append(b.MaterialSlots).Append(',')
            .Append(b.MeshAssets.Count).Append(',')
            .Append(b.MaterialAssets.Count).Append(',')
            .Append(b.TextureAssets.Count).Append(',')
            .Append(b.Colliders).Append(',')
            .Append(b.Lights).Append(',')
            .Append(b.VertexInstances).Append(',')
            .Append(b.TriangleInstances).AppendLine();
    }

    private static string BuildMeshCsv(List<MeshUse> uses)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Block,HierarchyPath,GameObject,Component,Mesh,AssetPath,Vertices,Triangles,SubMeshes,Materials");
        foreach (MeshUse m in uses.OrderByDescending(x => x.Triangles))
            csv.Append(Csv(m.Block)).Append(',')
                .Append(Csv(m.Hierarchy)).Append(',')
                .Append(Csv(m.GameObject)).Append(',')
                .Append(Csv(m.Component)).Append(',')
                .Append(Csv(m.MeshName)).Append(',')
                .Append(Csv(m.AssetPath)).Append(',')
                .Append(m.Vertices).Append(',')
                .Append(m.Triangles).Append(',')
                .Append(m.SubMeshes).Append(',')
                .Append(Csv(m.Materials)).AppendLine();
        return csv.ToString();
    }

    private static string BuildAssetCsv(IEnumerable<AssetUse> assets)
    {
        var csv = new StringBuilder();
        csv.AppendLine("AssetPath,Name,Type,SourceBytes,SourceMiB,Uses,Width,Height,Details");
        foreach (AssetUse a in assets.OrderByDescending(x => x.Bytes).ThenByDescending(x => x.Uses))
            csv.Append(Csv(a.Path)).Append(',')
                .Append(Csv(a.Name)).Append(',')
                .Append(Csv(a.Type)).Append(',')
                .Append(a.Bytes).Append(',')
                .Append((a.Bytes / 1048576.0).ToString("F3", Invariant)).Append(',')
                .Append(a.Uses).Append(',')
                .Append(a.Width).Append(',')
                .Append(a.Height).Append(',')
                .Append(Csv(a.Details)).AppendLine();
        return csv.ToString();
    }

    private static string BuildSummary(GameObject root, BlockStats total,
        Dictionary<string, BlockStats> blocks, List<MeshUse> meshUses,
        Dictionary<string, AssetUse> materials, Dictionary<string, AssetUse> textures,
        int transformCount)
    {
        var s = new StringBuilder();
        s.AppendLine("MOTOR CITY - DEEP CITY AUDIT SUMMARY");
        s.AppendLine("Root: " + root.name);
        s.AppendLine("Hierarchy transforms scanned: " + transformCount);
        s.AppendLine("GameObjects: " + total.GameObjects);
        s.AppendLine("Active in hierarchy: " + total.ActiveGameObjects);
        s.AppendLine("Components: " + total.Components);
        s.AppendLine("MeshFilter: " + total.MeshFilters);
        s.AppendLine("MeshRenderer: " + total.MeshRenderers);
        s.AppendLine("SkinnedMeshRenderer: " + total.SkinnedRenderers);
        s.AppendLine("Mesh instances: " + meshUses.Count);
        s.AppendLine("Vertex instances (sum): " + meshUses.Sum(x => (long)x.Vertices));
        s.AppendLine("Triangle instances (sum): " + meshUses.Sum(x => x.Triangles));
        s.AppendLine("Colliders: " + total.Colliders);
        s.AppendLine("Lights: " + total.Lights);
        s.AppendLine("Named Double-Block groups: " + blocks.Count);
        s.AppendLine("Unique materials: " + materials.Count);
        s.AppendLine("Unique referenced textures: " + textures.Count);
        s.AppendLine();
        s.AppendLine("TOP 20 BLOCKS BY TRIANGLE INSTANCES");
        foreach (BlockStats b in blocks.Values.OrderByDescending(x => x.TriangleInstances).Take(20))
            s.AppendLine(b.Path + " | objects=" + b.GameObjects +
                " | renderers=" + b.MeshRenderers +
                " | triangles=" + b.TriangleInstances +
                " | colliders=" + b.Colliders);
        s.AppendLine();
        s.AppendLine("NOTE: Triangle/vertex instance sums count each scene renderer use.");
        s.AppendLine("SourceBytes are original asset-file sizes, not compressed AssetBundle contribution sizes.");
        s.AppendLine("This is an editor-time read-only hierarchy audit; it does not measure runtime frame time or streaming stalls.");
        return s.ToString();
    }

    private static void AppendHierarchyLine(StringBuilder sb, Transform t, Transform root,
        int depth, Component[] components)
    {
        GameObject go = t.gameObject;
        sb.Append(new string(' ', depth * 2))
            .Append(go.activeSelf ? "[+]" : "[-]")
            .Append(' ')
            .Append(go.name)
            .Append(" {")
            .Append(go.layer).Append(" / ").Append(go.tag)
            .Append("} [")
            .Append(string.Join(", ", components.Where(c => c != null)
                .Select(c => c.GetType().Name).ToArray()))
            .AppendLine("]");
    }

    private static string DescribeComponent(Component c)
    {
        MeshFilter mf = c as MeshFilter;
        if (mf != null)
            return mf.sharedMesh != null ? "Mesh=" + mf.sharedMesh.name +
                "; Vertices=" + mf.sharedMesh.vertexCount +
                "; Triangles=" + GetTriangles(mf.sharedMesh) : "Mesh=NULL";

        Renderer renderer = c as Renderer;
        if (renderer != null)
            return "Enabled=" + renderer.enabled +
                "; CastShadows=" + renderer.shadowCastingMode +
                "; ReceiveShadows=" + renderer.receiveShadows +
                "; Materials=" + string.Join(" | ", renderer.sharedMaterials
                    .Where(m => m != null).Select(m => m.name).ToArray()) +
                "; Bounds=" + V3(renderer.bounds.size);

        Collider collider = c as Collider;
        if (collider != null)
            return "Trigger=" + collider.isTrigger + "; Bounds=" + V3(collider.bounds.size);

        Light light = c as Light;
        if (light != null)
            return "Type=" + light.type + "; Intensity=" + light.intensity +
                "; Range=" + light.range + "; Shadows=" + light.shadows;

        Rigidbody rb = c as Rigidbody;
        if (rb != null)
            return "Mass=" + rb.mass + "; IsKinematic=" + rb.isKinematic +
                "; UseGravity=" + rb.useGravity;

        LODGroup lod = c as LODGroup;
        if (lod != null)
            return "LODLevels=" + lod.GetLODs().Length + "; FadeMode=" + lod.fadeMode;

        return c.GetType().Name;
    }

    private static string GetComponentAssetPath(Component c)
    {
        MeshFilter mf = c as MeshFilter;
        if (mf != null && mf.sharedMesh != null) return AssetDatabase.GetAssetPath(mf.sharedMesh);
        MeshCollider mc = c as MeshCollider;
        if (mc != null && mc.sharedMesh != null) return AssetDatabase.GetAssetPath(mc.sharedMesh);
        MonoBehaviour mb = c as MonoBehaviour;
        if (mb != null)
        {
            MonoScript script = MonoScript.FromMonoBehaviour(mb);
            return script != null ? AssetDatabase.GetAssetPath(script) : string.Empty;
        }
        return string.Empty;
    }

    private static string GetEnabledState(Component c)
    {
        Behaviour b = c as Behaviour;
        return b != null ? b.enabled.ToString() : "N/A";
    }

    private static long GetTriangles(Mesh mesh)
    {
        long result = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
            if (mesh.GetTopology(i) == MeshTopology.Triangles)
                result += (long)mesh.GetIndexCount(i) / 3L;
        return result;
    }

    private static string GetPath(Transform t, Transform root)
    {
        var parts = new List<string>();
        Transform current = t;
        while (current != null)
        {
            parts.Add(current.name);
            if (current == root) break;
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts.ToArray());
    }

    private static int GetDepth(Transform t, Transform root)
    {
        int depth = 0;
        Transform current = t;
        while (current != null && current != root)
        {
            depth++;
            current = current.parent;
        }
        return depth;
    }

    private static string V3(Vector3 v)
    {
        return v.x.ToString("F3", Invariant) + ";" +
               v.y.ToString("F3", Invariant) + ";" +
               v.z.ToString("F3", Invariant);
    }

    private static string ColorText(Color c)
    {
        return c.r.ToString("F3", Invariant) + ";" +
               c.g.ToString("F3", Invariant) + ";" +
               c.b.ToString("F3", Invariant) + ";" +
               c.a.ToString("F3", Invariant);
    }

    private static long GetFileBytes(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return 0;
        string fullPath = Path.GetFullPath(assetPath);
        return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0;
    }

    private static void WriteFile(string folder, string fileName, string contents)
    {
        File.WriteAllText(Path.Combine(folder, fileName), contents, new UTF8Encoding(true));
    }

    private static string Csv(string value)
    {
        value = value ?? string.Empty;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
