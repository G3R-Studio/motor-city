using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Measures the real WebGL LZ4 city AssetBundle and per-source-file probe bundles.
/// Each dependency file is assigned to its own probe bundle so shared dependencies
/// remain separate bundles instead of being counted repeatedly.
/// Diagnostic only: source assets, import settings, scenes and prefabs are not modified.
/// Per-file probe sizes rank assets; they are not additive or exact byte attribution
/// inside the combined bundle because compression context and bundle metadata differ.
/// </summary>
public static class MotorCityAssetBundlePayloadAudit
{
    private const string CityPrefab = "Assets/Resources/MotorCity/Environment/CityVisual.prefab";
    private const string EnvironmentRoot = "Assets/Resources/MotorCity/Environment/";
    private static readonly string[] ExtraRoots =
        { "FCGMaterials", "FCGTrafficCars", "TrafficVehicleCatalog.prefab" };

    private const string CombinedBundleName = "motorcity-city-audit.bundle";
    private const BuildAssetBundleOptions BundleOptions =
        BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode;

    private sealed class AssetRow
    {
        public string Path;
        public string Type;
        public long SourceBytes;
        public string BundleName;
        public long BundleBytes;
        public int DirectDependencies;
        public string Status;
    }

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    [MenuItem("Tools/Motor City/Build/Audit Actual City Bundle Payload")]
    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
        {
            EditorUtility.DisplayDialog("City AssetBundle Payload Audit",
                "Exit Play Mode and wait for any current build to finish.", "OK");
            return;
        }

        string outputFolder = EditorUtility.SaveFolderPanel(
            "Save actual city AssetBundle audit",
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "MotorCity_AssetBundle_Payload_Audit");
        if (string.IsNullOrEmpty(outputFolder))
            return;

        string tempRoot = Path.GetFullPath(Path.Combine("Library",
            "MotorCityAssetBundlePayloadAudit_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", Invariant)));

        try
        {
            RunAudit(outputFolder, tempRoot);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("City AssetBundle Payload Audit Failed",
                "The audit failed. Check the Unity Console for details.\n\n" + ex.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            try
            {
                if (Directory.Exists(tempRoot))
                    Directory.Delete(tempRoot, true);
            }
            catch (Exception cleanupError)
            {
                Debug.LogWarning("[Motor City Payload Audit] Could not remove temporary build files: " +
                    tempRoot + "\n" + cleanupError.Message);
            }
        }
    }

    private static void RunAudit(string outputFolder, string tempRoot)
    {
        if (!File.Exists(CityPrefab))
            throw new FileNotFoundException("CityVisual prefab was not found.", CityPrefab);

        string[] bundleRoots = GetProductionCityRoots();
        if (bundleRoots.Length == 0)
            throw new InvalidOperationException("No city bundle roots were found.");

        string[] dependencies = AssetDatabase.GetDependencies(bundleRoots, true)
            .Where(IsBuildableProjectAsset)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        if (dependencies.Length == 0)
            throw new InvalidOperationException("No buildable Assets/ dependencies were found.");

        Directory.CreateDirectory(outputFolder);
        string combinedDirectory = Path.Combine(tempRoot, "Combined");
        string probeDirectory = Path.Combine(tempRoot, "PerAsset");
        Directory.CreateDirectory(combinedDirectory);
        Directory.CreateDirectory(probeDirectory);

        EditorUtility.DisplayProgressBar("Motor City Payload Audit",
            "Building combined city AssetBundle with WebGL LZ4 compression...", 0.05f);

        AssetBundleBuild combinedBuild = new AssetBundleBuild
        {
            assetBundleName = CombinedBundleName,
            assetNames = bundleRoots
        };

        AssetBundleManifest combinedManifest = BuildPipeline.BuildAssetBundles(
            combinedDirectory, new[] { combinedBuild }, BundleOptions, BuildTarget.WebGL);

        if (combinedManifest == null)
            throw new InvalidOperationException("Combined city AssetBundle build returned no manifest.");

        string combinedPath = Path.Combine(combinedDirectory, CombinedBundleName);
        if (!File.Exists(combinedPath))
            throw new FileNotFoundException("Combined city bundle output was not created.", combinedPath);

        long combinedBytes = new FileInfo(combinedPath).Length;
        string[] combinedAssetNames = ReadBundleAssetNames(combinedPath);

        EditorUtility.DisplayProgressBar("Motor City Payload Audit",
            "Preparing " + dependencies.Length + " individual asset probes...", 0.15f);

        var rows = new List<AssetRow>(dependencies.Length);
        var builds = new List<AssetBundleBuild>(dependencies.Length);
        var bundleToPath = new Dictionary<string, string>(StringComparer.Ordinal);

        for (int i = 0; i < dependencies.Length; i++)
        {
            string assetPath = dependencies[i];
            string bundleName = "mcaudit_" + i.ToString("D5", Invariant) + ".bundle";
            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            string type = mainAsset != null ? mainAsset.GetType().Name : "Unknown";
            string absolutePath = Path.GetFullPath(assetPath);
            long sourceBytes = File.Exists(absolutePath)
                ? new FileInfo(absolutePath).Length : 0;

            rows.Add(new AssetRow
            {
                Path = assetPath,
                Type = type,
                SourceBytes = sourceBytes,
                BundleName = bundleName,
                Status = "Pending"
            });

            builds.Add(new AssetBundleBuild
            {
                assetBundleName = bundleName,
                assetNames = new[] { assetPath }
            });
            bundleToPath.Add(bundleName, assetPath);

            if (i % 25 == 0)
                EditorUtility.DisplayProgressBar("Motor City Payload Audit",
                    "Preparing asset " + (i + 1) + " / " + dependencies.Length + ": " + assetPath,
                    0.15f + 0.15f * i / Math.Max(1, dependencies.Length));
        }

        EditorUtility.DisplayProgressBar("Motor City Payload Audit",
            "Building per-asset WebGL LZ4 probe bundles. This can take several minutes...", 0.32f);

        AssetBundleManifest probeManifest = BuildPipeline.BuildAssetBundles(
            probeDirectory, builds.ToArray(), BundleOptions, BuildTarget.WebGL);

        if (probeManifest == null)
            throw new InvalidOperationException("Per-asset probe build returned no manifest.");

        var dependenciesByBundle = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (AssetRow row in rows)
        {
            string bundlePath = Path.Combine(probeDirectory, row.BundleName);
            if (!File.Exists(bundlePath))
            {
                row.Status = "Bundle output missing";
                row.BundleBytes = 0;
                row.DirectDependencies = 0;
                dependenciesByBundle[row.BundleName] = new string[0];
                continue;
            }

            row.BundleBytes = new FileInfo(bundlePath).Length;
            row.DirectDependencies = probeManifest.GetDirectDependencies(row.BundleName).Length;
            row.Status = "OK";
            dependenciesByBundle[row.BundleName] =
                probeManifest.GetDirectDependencies(row.BundleName);
        }

        WriteReports(outputFolder, rows, bundleToPath, dependenciesByBundle,
            bundleRoots, combinedBytes, combinedAssetNames);

        long probeTotal = rows.Sum(r => r.BundleBytes);
        int missing = rows.Count(r => r.Status != "OK");

        Debug.Log("[Motor City Payload Audit] Complete. Combined city bundle=" +
            FormatBytes(combinedBytes) + "; source files=" + rows.Count +
            "; per-asset probe bundle sum=" + FormatBytes(probeTotal) +
            "; missing probes=" + missing + "; output=" + outputFolder);

        EditorUtility.DisplayDialog("City AssetBundle Payload Audit Complete",
            "Combined city LZ4 bundle: " + FormatBytes(combinedBytes) +
            "\nDependency files measured: " + rows.Count +
            "\nPer-asset probe bundle sum: " + FormatBytes(probeTotal) +
            "\nMissing probe bundles: " + missing +
            "\n\nReports saved to:\n" + outputFolder +
            "\n\nIMPORTANT: per-asset probe sizes are not additive. " +
            "They are ranking measurements; the combined bundle is the real total.",
            "OK");
    }

    // Mirrors MotorCityCityBundleExperiment.Measure: CityVisual plus the contents
    // of FCGMaterials and FCGTrafficCars, and TrafficVehicleCatalog.prefab.
    private static string[] GetProductionCityRoots()
    {
        var roots = new List<string> { CityPrefab };

        foreach (string extra in ExtraRoots)
        {
            string path = EnvironmentRoot + extra;
            if (AssetDatabase.IsValidFolder(path))
            {
                roots.AddRange(AssetDatabase.GetAllAssetPaths().Where(p =>
                    p.StartsWith(path + "/", StringComparison.Ordinal) &&
                    !AssetDatabase.IsValidFolder(p) &&
                    AssetDatabase.LoadMainAssetAtPath(p) != null));
            }
            else if (File.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                roots.Add(path);
            }
            else
            {
                throw new FileNotFoundException("Expected city bundle root was not found.", path);
            }
        }

        return roots.Distinct(StringComparer.Ordinal)
            .Where(p => AssetDatabase.LoadMainAssetAtPath(p) != null)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsBuildableProjectAsset(string path)
    {
        if (string.IsNullOrEmpty(path) ||
            !path.StartsWith("Assets/", StringComparison.Ordinal) ||
            AssetDatabase.IsValidFolder(path))
            return false;

        UnityEngine.Object main = AssetDatabase.LoadMainAssetAtPath(path);
        if (main == null || main is MonoScript)
            return false;

        string extension = Path.GetExtension(path);
        if (string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".asmdef", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".rsp", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static string[] ReadBundleAssetNames(string bundlePath)
    {
        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
            throw new InvalidOperationException("Could not reopen the combined city AssetBundle.");
        try
        {
            return bundle.GetAllAssetNames().OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }
        finally
        {
            bundle.Unload(false);
        }
    }

    private static void WriteReports(
        string outputFolder,
        List<AssetRow> rows,
        Dictionary<string, string> bundleToPath,
        Dictionary<string, string[]> dependenciesByBundle,
        string[] bundleRoots,
        long combinedBytes,
        string[] combinedAssetNames)
    {
        var csv = new StringBuilder();
        csv.AppendLine("AssetPath,MainAssetType,SourceFileBytes,StandaloneLz4BundleBytes,StandaloneLz4BundleMiB,DirectDependencyBundles,ProbeBundle,Status");
        foreach (AssetRow row in rows.OrderByDescending(r => r.BundleBytes)
                     .ThenBy(r => r.Path, StringComparer.Ordinal))
        {
            csv.Append(Csv(row.Path)).Append(',')
                .Append(Csv(row.Type)).Append(',')
                .Append(row.SourceBytes).Append(',')
                .Append(row.BundleBytes).Append(',')
                .Append((row.BundleBytes / 1048576.0).ToString("F4", Invariant)).Append(',')
                .Append(row.DirectDependencies).Append(',')
                .Append(Csv(row.BundleName)).Append(',')
                .Append(Csv(row.Status)).AppendLine();
        }
        WriteText(outputFolder, "01_Per_Asset_LZ4_Bundle_Sizes.csv", csv.ToString());

        var graph = new StringBuilder();
        graph.AppendLine("AssetBundle,SourceAssetPath,DirectDependencyBundle,DependencyAssetPath");
        foreach (AssetRow row in rows.OrderBy(r => r.Path, StringComparer.Ordinal))
        {
            string[] direct;
            if (!dependenciesByBundle.TryGetValue(row.BundleName, out direct))
                continue;
            foreach (string dependencyBundle in direct)
            {
                string dependencyPath;
                bundleToPath.TryGetValue(dependencyBundle, out dependencyPath);
                graph.Append(Csv(row.BundleName)).Append(',')
                    .Append(Csv(row.Path)).Append(',')
                    .Append(Csv(dependencyBundle)).Append(',')
                    .Append(Csv(dependencyPath ?? "(not in project probe map)")).AppendLine();
            }
        }
        WriteText(outputFolder, "02_AssetBundle_Direct_Dependencies.csv", graph.ToString());

        var typeSummary = new StringBuilder();
        typeSummary.AppendLine("MainAssetType,AssetFiles,SourceFileBytes,StandaloneProbeBundleBytes,AverageProbeBundleBytes,MaxProbeBundleBytes");
        foreach (var group in rows.GroupBy(r => r.Type).OrderByDescending(g => g.Sum(x => x.BundleBytes)))
        {
            long sourceBytes = group.Sum(x => x.SourceBytes);
            long probeBytes = group.Sum(x => x.BundleBytes);
            typeSummary.Append(Csv(group.Key)).Append(',')
                .Append(group.Count()).Append(',')
                .Append(sourceBytes).Append(',')
                .Append(probeBytes).Append(',')
                .Append((group.Count() == 0 ? 0 : (double)probeBytes / group.Count()).ToString("F1", Invariant)).Append(',')
                .Append(group.Max(x => x.BundleBytes)).AppendLine();
        }
        WriteText(outputFolder, "03_Asset_Type_Summary.csv", typeSummary.ToString());

        var combinedInventory = new StringBuilder();
        combinedInventory.AppendLine("CombinedBundleAssetName");
        foreach (string assetName in combinedAssetNames)
            combinedInventory.Append(Csv(assetName)).AppendLine();
        WriteText(outputFolder, "04_Combined_Bundle_Asset_Inventory.csv", combinedInventory.ToString());

        long probeSum = rows.Sum(r => r.BundleBytes);
        long sourceSum = rows.Sum(r => r.SourceBytes);
        var summary = new StringBuilder();
        summary.AppendLine("MOTOR CITY - ACTUAL CITY ASSETBUNDLE PAYLOAD AUDIT");
        summary.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Invariant));
        summary.AppendLine("Build target: WebGL");
        summary.AppendLine("Compression: ChunkBasedCompression (LZ4)");
        summary.AppendLine("Combined bundle roots: " + bundleRoots.Length);
        summary.AppendLine("Unique dependency source files probed: " + rows.Count);
        summary.AppendLine();
        summary.AppendLine("COMBINED CITY BUNDLE");
        summary.AppendLine("Bundle name: " + CombinedBundleName);
        summary.AppendLine("Compressed bundle bytes: " + combinedBytes);
        summary.AppendLine("Compressed bundle MiB: " + (combinedBytes / 1048576.0).ToString("F3", Invariant));
        summary.AppendLine("Assets reported by AssetBundle.GetAllAssetNames(): " + combinedAssetNames.Length);
        summary.AppendLine();
        summary.AppendLine("PER-ASSET PROBE BUNDLES");
        summary.AppendLine("Sum of separate probe bundle bytes: " + probeSum);
        summary.AppendLine("Sum of source file bytes: " + sourceSum);
        summary.AppendLine("Largest individual probe bundles:");
        foreach (AssetRow row in rows.OrderByDescending(r => r.BundleBytes).Take(20))
            summary.AppendLine(FormatBytes(row.BundleBytes) + " | " + row.Type + " | " + row.Path);
        summary.AppendLine();
        summary.AppendLine("IMPORTANT INTERPRETATION");
        summary.AppendLine("The combined bundle size is the real total for the selected city roots, built with WebGL LZ4.");
        summary.AppendLine("Each per-asset probe bundle contains one source asset file; dependencies assigned to their own probe bundles are external dependencies.");
        summary.AppendLine("Per-asset probe sizes are useful for ranking source files, but they are NOT additive and are not exact byte attribution inside the combined bundle.");
        summary.AppendLine("Compression context, bundle metadata, type trees and dependency layout differ between a single combined bundle and many small probe bundles.");
        summary.AppendLine("SourceFileBytes are raw project file sizes and are not transfer sizes.");
        summary.AppendLine("No source assets, import settings, scenes, prefabs or production bundle labels were modified.");
        WriteText(outputFolder, "00_Summary.txt", summary.ToString());
    }

    private static void WriteText(string folder, string name, string contents)
    {
        File.WriteAllText(Path.Combine(folder, name), contents, new UTF8Encoding(true));
    }

    private static string Csv(string value)
    {
        value = value ?? string.Empty;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static string FormatBytes(long bytes)
    {
        return (bytes / 1048576.0).ToString("F2", Invariant) + " MiB (" +
            bytes.ToString("N0", Invariant) + " bytes)";
    }
}
