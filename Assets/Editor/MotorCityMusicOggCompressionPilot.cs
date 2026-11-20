#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MotorCity.EditorTools
{
    /// <summary>
    /// Reversible FFmpeg pilot for the 16 OGG tracks in StreamingAssets.
    /// Preview writes only to Library. Apply backs up originals to Library
    /// before replacing any track. Unity's AudioImporter is intentionally
    /// bypassed because StreamingAssets are copied byte-for-byte.
    /// </summary>
    public static class MotorCityMusicOggCompressionPilot
    {
        private const string MusicRoot = "Assets/StreamingAssets/MotorCity/Music";
        private const string WorkRoot = "Library/MotorCityMusicOggPilot";
        private const string PreviewRoot = WorkRoot + "/Preview";
        private const string BackupRoot = WorkRoot + "/Backup";
        private const string PreviewManifestPath = WorkRoot + "/preview.json";
        private const string BackupManifestPath = WorkRoot + "/backup.json";
        private const int TargetBitrateKbps = 96;

        [Serializable]
        private sealed class Manifest
        {
            public List<Entry> entries = new List<Entry>();
        }

        [Serializable]
        private sealed class Entry
        {
            public string fileName;
            public long originalBytes;
            public long compressedBytes;
        }

        [MenuItem("Tools/Motor City/Audio Compression/1 - Preview Music OGG at 96 kbps")]
        private static void Preview()
        {
            string[] sources = Directory.Exists(MusicRoot)
                ? Directory.GetFiles(MusicRoot, "*.ogg", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : Array.Empty<string>();

            if (sources.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Music Compression Pilot",
                    "No OGG files were found in " + MusicRoot,
                    "OK");
                return;
            }

            if (Directory.Exists(PreviewRoot))
                Directory.Delete(PreviewRoot, true);
            Directory.CreateDirectory(PreviewRoot);

            Manifest manifest = new Manifest();
            long originalTotal = 0;
            long compressedTotal = 0;

            try
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    string source = sources[i];
                    string fileName = Path.GetFileName(source);
                    string output = Path.Combine(PreviewRoot, fileName);

                    EditorUtility.DisplayProgressBar(
                        "Previewing music compression",
                        fileName,
                        (float)i / Math.Max(1, sources.Length));

                    RunFfmpeg(source, output);

                    long originalBytes = new FileInfo(source).Length;
                    long compressedBytes = new FileInfo(output).Length;

                    manifest.entries.Add(new Entry
                    {
                        fileName = fileName,
                        originalBytes = originalBytes,
                        compressedBytes = compressedBytes
                    });

                    originalTotal += originalBytes;
                    compressedTotal += compressedBytes;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Music Compression Pilot",
                    "Preview failed. Original tracks were not changed.\n\n" +
                    exception.Message +
                    "\n\nInstall FFmpeg with the libvorbis encoder and make sure ffmpeg is available in PATH.",
                    "OK");
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Directory.CreateDirectory(WorkRoot);
            File.WriteAllText(
                PreviewManifestPath,
                JsonUtility.ToJson(manifest, true));

            long saved = originalTotal - compressedTotal;
            float percent = originalTotal > 0
                ? 100f * saved / originalTotal
                : 0f;

            string summary =
                "Tracks previewed: " + manifest.entries.Count +
                "\nOriginal total: " + MiB(originalTotal) + " MiB" +
                "\nPreview total: " + MiB(compressedTotal) + " MiB" +
                "\nPotential saving: " + MiB(saved) + " MiB (" +
                percent.ToString("F1") + "%)" +
                "\n\nOriginal OGG files were not modified." +
                "\nListen to the preview files in:\n" + PreviewRoot;

            Debug.Log("[Motor City Music Compression]\n" + summary);
            EditorUtility.DisplayDialog(
                "Music Compression Preview Complete",
                summary,
                "OK");
        }

        [MenuItem("Tools/Motor City/Audio Compression/2 - Apply Previewed Music OGGs")]
        private static void Apply()
        {
            if (File.Exists(BackupManifestPath))
            {
                EditorUtility.DisplayDialog(
                    "Music Compression Pilot",
                    "A backup already exists. Restore it before applying another compression pass.",
                    "OK");
                return;
            }

            if (!File.Exists(PreviewManifestPath))
            {
                EditorUtility.DisplayDialog(
                    "Music Compression Pilot",
                    "Run the Preview command first.",
                    "OK");
                return;
            }

            Manifest manifest =
                JsonUtility.FromJson<Manifest>(File.ReadAllText(PreviewManifestPath));

            if (manifest == null || manifest.entries == null || manifest.entries.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Music Compression Pilot",
                    "The preview manifest is empty or invalid.",
                    "OK");
                return;
            }

            foreach (Entry entry in manifest.entries)
            {
                string source = Path.Combine(MusicRoot, entry.fileName);
                string preview = Path.Combine(PreviewRoot, entry.fileName);
                if (!File.Exists(source) || !File.Exists(preview) ||
                    new FileInfo(source).Length != entry.originalBytes ||
                    new FileInfo(preview).Length != entry.compressedBytes)
                {
                    EditorUtility.DisplayDialog(
                        "Music Compression Pilot",
                        "A source or preview file changed after the preview. Run Preview again.\n" +
                        entry.fileName,
                        "OK");
                    return;
                }
            }

            long originalTotal = manifest.entries.Sum(e => e.originalBytes);
            long compressedTotal = manifest.entries.Sum(e => e.compressedBytes);
            if (compressedTotal >= originalTotal)
            {
                EditorUtility.DisplayDialog(
                    "Music Compression Pilot",
                    "The preview did not reduce the total size. Nothing was changed.",
                    "OK");
                return;
            }

            Directory.CreateDirectory(BackupRoot);
            try
            {
                // Back up every source before replacing any of them.
                foreach (Entry entry in manifest.entries)
                {
                    string source = Path.Combine(MusicRoot, entry.fileName);
                    string backup = Path.Combine(BackupRoot, entry.fileName);
                    File.Copy(source, backup, true);
                }

                File.WriteAllText(
                    BackupManifestPath,
                    JsonUtility.ToJson(manifest, true));

                foreach (Entry entry in manifest.entries)
                {
                    string source = Path.Combine(MusicRoot, entry.fileName);
                    string preview = Path.Combine(PreviewRoot, entry.fileName);
                    File.Copy(preview, source, true);
                }
            }
            catch
            {
                RestoreFilesFromBackup(manifest);
                throw;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            long saved = originalTotal - compressedTotal;
            string summary =
                "Applied to " + manifest.entries.Count + " music tracks." +
                "\nSaved: " + MiB(saved) + " MiB (" +
                (100f * saved / originalTotal).ToString("F1") + "%)." +
                "\nOriginals are backed up in " + BackupRoot +
                "\nUse Restore Original Music OGGs to undo.";

            Debug.Log("[Motor City Music Compression]\n" + summary);
            EditorUtility.DisplayDialog(
                "Music Compression Applied",
                summary,
                "OK");
        }

        [MenuItem("Tools/Motor City/Audio Compression/3 - Restore Original Music OGGs")]
        private static void Restore()
        {
            if (!File.Exists(BackupManifestPath))
            {
                EditorUtility.DisplayDialog(
                    "Music Compression Pilot",
                    "No original music backup was found.",
                    "OK");
                return;
            }

            Manifest manifest =
                JsonUtility.FromJson<Manifest>(File.ReadAllText(BackupManifestPath));

            if (manifest == null || manifest.entries == null)
            {
                EditorUtility.DisplayDialog(
                    "Music Compression Pilot",
                    "The backup manifest is invalid and was left untouched.",
                    "OK");
                return;
            }

            RestoreFilesFromBackup(manifest);
            File.Delete(BackupManifestPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            EditorUtility.DisplayDialog(
                "Music Compression Pilot",
                "Original music OGG files have been restored.",
                "OK");
        }

        [MenuItem("Tools/Motor City/Audio Compression/3 - Restore Original Music OGGs", true)]
        private static bool ValidateRestore()
        {
            return File.Exists(BackupManifestPath);
        }

        private static void RestoreFilesFromBackup(Manifest manifest)
        {
            foreach (Entry entry in manifest.entries)
            {
                string backup = Path.Combine(BackupRoot, entry.fileName);
                string source = Path.Combine(MusicRoot, entry.fileName);
                if (File.Exists(backup))
                    File.Copy(backup, source, true);
            }
        }

        private static void RunFfmpeg(string source, string output)
        {
            string arguments =
                "-hide_banner -loglevel error -y -i " + Quote(source) +
                " -map 0:a:0 -map_metadata 0 -vn -c:a libvorbis -b:a " +
                TargetBitrateKbps + "k " + Quote(output);

            var start = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(start))
            {
                if (process == null)
                    throw new InvalidOperationException("Could not start FFmpeg.");

                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 || !File.Exists(output))
                    throw new InvalidOperationException(
                        "FFmpeg failed for " + Path.GetFileName(source) + ": " + error);
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string MiB(long bytes)
        {
            return (bytes / 1048576.0).ToString("F2");
        }
    }
}
#endif
