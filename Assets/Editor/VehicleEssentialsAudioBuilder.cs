#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MotorCity.Audio;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class VehicleEssentialsAudioBuilder
{
    private const string RuntimeRoot =
        "Assets/Resources/MotorCity/Audio";

    private const string LibraryPath =
        RuntimeRoot + "/VehicleAudioLibrary.asset";

    private const string LocalAudioRoot =
        "Assets/LocalAudio";

    private const string LocalTireSquealPath =
        LocalAudioRoot + "/tire-whistling-sound.wav";

    private static bool buildScheduled;

    static VehicleEssentialsAudioBuilder()
    {
        EditorApplication.delayCall +=
            TryBuildMissingLibrary;
    }

    [MenuItem("Motor City/Audio/Rebuild Vehicle Essentials Audio Library")]
    public static void RebuildMenu()
    {
        BuildLibrary(
            true);
    }

    [MenuItem("Motor City/Audio/Import Tire Squeal WAV")]
    public static void ImportTireSqueal()
    {
        string path =
            EditorUtility.OpenFilePanel(
                "Choose tire squeal WAV",
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                "wav");

        if (string.IsNullOrWhiteSpace(
                path))
            return;

        EnsureFolder(
            LocalAudioRoot);

        string destination =
            Path.GetFullPath(
                LocalTireSquealPath);

        string source =
            Path.GetFullPath(
                path);

        if (!string.Equals(
                source,
                destination,
                StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(
                source,
                destination,
                true);
        }

        AssetDatabase.ImportAsset(
            LocalTireSquealPath,
            ImportAssetOptions.ForceUpdate);

        BuildLibrary(
            true);
    }

    public static void ScheduleAutomaticBuild()
    {
        if (buildScheduled)
            return;

        buildScheduled =
            true;

        EditorApplication.delayCall +=
            () =>
            {
                buildScheduled =
                    false;

                BuildLibrary(
                    false);
            };
    }

    private static void TryBuildMissingLibrary()
    {
        if (AssetDatabase.LoadAssetAtPath<VehicleAudioLibrary>(
                LibraryPath) != null)
            return;

        BuildLibrary(
            false);
    }

    private static void BuildLibrary(
        bool showDialogs)
    {
        List<AudioEntry> clips =
            FindVehicleEssentialsClips();

        AudioClip tireSqueal =
            FindTireSquealClip();

        if (clips.Count == 0 &&
            tireSqueal == null)
        {
            if (showDialogs)
            {
                EditorUtility.DisplayDialog(
                    "Motor City — Vehicle Audio",
                    "Не найдено ни Vehicle - Essentials, ни локального tire squeal.\n\n" +
                    "Импортируй пакет Nox_Sound или используй " +
                    "Motor City > Audio > Import Tire Squeal WAV.",
                    "OK");
            }

            return;
        }

        EnsureFolder(
            RuntimeRoot);

        AudioClip engineIdle =
            FindBest(
                clips,
                AudioRole.EngineIdle,
                null);

        AudioClip engineDrive =
            FindBest(
                clips,
                AudioRole.EngineDrive,
                engineIdle);

        if (engineDrive == null)
        {
            engineDrive =
                FindBest(
                    clips,
                    AudioRole.EngineDrive,
                    null);
        }

        AudioClip driving =
            FindBest(
                clips,
                AudioRole.DrivingLoop,
                null);

        AudioClip handbrake =
            FindBest(
                clips,
                AudioRole.Handbrake,
                null);

        AudioClip horn =
            FindBest(
                clips,
                AudioRole.Horn,
                null);

        if (engineIdle == null &&
            engineDrive == null &&
            driving == null &&
            handbrake == null &&
            horn == null &&
            tireSqueal == null)
        {
            if (showDialogs)
            {
                EditorUtility.DisplayDialog(
                    "Motor City — Vehicle Audio",
                    "Аудиофайлы Vehicle - Essentials найдены, но их имена не удалось " +
                    "распознать как Engine/Driving/Handbrake/Horn.\n\n" +
                    "Открой Console и пришли лог Motor City: Vehicle Essentials scan.",
                    "OK");
            }

            LogScan(
                clips);

            return;
        }

        VehicleAudioLibrary library =
            AssetDatabase.LoadAssetAtPath<VehicleAudioLibrary>(
                LibraryPath);

        if (library == null)
        {
            library =
                ScriptableObject.CreateInstance<VehicleAudioLibrary>();

            AssetDatabase.CreateAsset(
                library,
                LibraryPath);
        }

        SerializedObject serialized =
            new(library);

        serialized.FindProperty(
                "engineIdleLoop")
            .objectReferenceValue =
                engineIdle;

        serialized.FindProperty(
                "engineDriveLoop")
            .objectReferenceValue =
                engineDrive;

        serialized.FindProperty(
                "drivingLoop")
            .objectReferenceValue =
                driving;

        serialized.FindProperty(
                "handbrakeClip")
            .objectReferenceValue =
                handbrake;

        serialized.FindProperty(
                "hornClip")
            .objectReferenceValue =
                horn;

        serialized.FindProperty(
                "tireSquealLoop")
            .objectReferenceValue =
                tireSqueal;

        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(
            library);

        AssetDatabase.SaveAssets();

        string summary =
            "Motor City: Vehicle Essentials audio library built.\n" +
            $"Engine idle: {ClipInfo(engineIdle)}\n" +
            $"Engine drive: {ClipInfo(engineDrive)}\n" +
            $"Driving loop: {ClipInfo(driving)}\n" +
            $"Handbrake: {ClipInfo(handbrake)}\n" +
            $"Horn: {ClipInfo(horn)}\n" +
            $"Tire squeal: {ClipInfo(tireSqueal)}";

        Debug.Log(
            summary);

        if (showDialogs)
        {
            EditorUtility.DisplayDialog(
                "Motor City — Vehicle Audio",
                "Готово.\n\n" +
                $"Engine idle: {ClipName(engineIdle)}\n" +
                $"Engine drive: {ClipName(engineDrive)}\n" +
                $"Driving loop: {ClipName(driving)}\n" +
                $"Handbrake: {ClipName(handbrake)}\n" +
                $"Horn: {ClipName(horn)}\n" +
                $"Tire squeal: {ClipName(tireSqueal)}\n\n" +
                "Нажми Play. Визг шин включается автоматически при скольжении, гудок — H.",
                "OK");
        }
    }

    private static AudioClip FindTireSquealClip()
    {
        AudioClip exact =
            AssetDatabase.LoadAssetAtPath<AudioClip>(
                LocalTireSquealPath);

        if (exact != null)
            return exact;

        AudioClip best =
            null;

        int bestScore =
            int.MinValue;

        foreach (string guid in
                 AssetDatabase.FindAssets(
                     "t:AudioClip",
                     new[]
                     {
                         "Assets"
                     }))
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (string.IsNullOrWhiteSpace(
                    path))
                continue;

            string compact =
                Compact(
                    path);

            int score =
                0;

            if (compact.Contains(
                    "tirewhistlingsound"))
                score += 100;

            if (compact.Contains(
                    "tiresqueal"))
                score += 80;

            if (compact.Contains(
                    "tirescreech"))
                score += 72;

            if (compact.Contains(
                    "skid"))
                score += 55;

            if (compact.Contains(
                    "tire"))
                score += 20;

            if (score <= bestScore)
                continue;

            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    path);

            if (clip == null)
                continue;

            bestScore =
                score;

            best =
                clip;
        }

        return
            bestScore >= 55
                ? best
                : null;
    }

    private static List<AudioEntry> FindVehicleEssentialsClips()
    {
        var result =
            new List<AudioEntry>();

        foreach (string guid in
                 AssetDatabase.FindAssets(
                     "t:AudioClip",
                     new[]
                     {
                         "Assets"
                     }))
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (!IsVehicleEssentialsPath(
                    path))
                continue;

            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    path);

            if (clip == null)
                continue;

            result.Add(
                new AudioEntry(
                    clip,
                    path));
        }

        return result;
    }

    private static AudioClip FindBest(
        List<AudioEntry> entries,
        AudioRole role,
        AudioClip excluded)
    {
        AudioEntry best =
            default;

        int bestScore =
            int.MinValue;

        foreach (AudioEntry entry in entries)
        {
            if (entry.Clip == null ||
                entry.Clip == excluded)
                continue;

            int score =
                Score(
                    entry,
                    role);

            if (score <= bestScore)
                continue;

            bestScore =
                score;

            best =
                entry;
        }

        return
            bestScore >= 60
                ? best.Clip
                : null;
    }

    private static int Score(
        AudioEntry entry,
        AudioRole role)
    {
        string value =
            Compact(
                entry.Path + " " + entry.Clip.name);

        int score =
            100;

        if (value.Contains("nox"))
            score += 8;

        if (value.Contains("vehicleessential"))
            score += 12;

        switch (role)
        {
            case AudioRole.EngineIdle:
                if (!value.Contains("engine"))
                    return int.MinValue;

                score += 30;

                if (value.Contains("loop"))
                    score += 28;

                if (value.Contains("idle") ||
                    value.Contains("low"))
                    score += 24;

                if (value.Contains("drive") ||
                    value.Contains("driving") ||
                    value.Contains("high") ||
                    value.Contains("fast"))
                    score -= 12;

                break;

            case AudioRole.EngineDrive:
                if (!value.Contains("engine"))
                    return int.MinValue;

                score += 30;

                if (value.Contains("loop"))
                    score += 28;

                if (value.Contains("drive") ||
                    value.Contains("driving") ||
                    value.Contains("rev") ||
                    value.Contains("accel") ||
                    value.Contains("high") ||
                    value.Contains("fast"))
                    score += 22;

                if (value.Contains("idle"))
                    score -= 15;

                break;

            case AudioRole.DrivingLoop:
                if (!(value.Contains("driving") ||
                      value.Contains("drive")))
                    return int.MinValue;

                score += 32;

                if (value.Contains("loop"))
                    score += 30;

                if (value.Contains("engine"))
                    score -= 24;

                break;

            case AudioRole.Handbrake:
                if (!value.Contains("handbrake"))
                    return int.MinValue;

                score += 55;

                if (value.Contains("up") ||
                    value.Contains("pull") ||
                    value.Contains("on"))
                    score += 8;

                break;

            case AudioRole.Horn:
                if (!value.Contains("horn"))
                    return int.MinValue;

                score += 55;
                break;
        }

        if (value.Contains("truck"))
            score -= 10;

        if (value.Contains("van"))
            score -= 6;

        if (value.Contains("tractor"))
            score -= 18;

        return score;
    }

    private static bool IsVehicleEssentialsPath(
        string path)
    {
        if (string.IsNullOrWhiteSpace(
                path))
            return false;

        string lower =
            Compact(
                path);

        if (lower.Contains(
                "resourcesmotorcityaudio"))
            return false;

        if (lower.Contains(
                "vehicleessential"))
            return true;

        return
            lower.Contains("nox") &&
            (lower.Contains("engine") ||
             lower.Contains("driving") ||
             lower.Contains("handbrake") ||
             lower.Contains("horn"));
    }

    private static void LogScan(
        List<AudioEntry> clips)
    {
        string lines =
            string.Join(
                "\n",
                clips
                    .Take(80)
                    .Select(
                        entry =>
                            entry.Path));

        Debug.Log(
            "Motor City: Vehicle Essentials scan. " +
            $"Audio clips found={clips.Count}\n{lines}");
    }

    private static string ClipInfo(
        AudioClip clip)
    {
        if (clip == null)
            return "<not found>";

        return
            clip.name +
            " | " +
            AssetDatabase.GetAssetPath(
                clip);
    }

    private static string ClipName(
        AudioClip clip)
    {
        return
            clip != null
                ? clip.name
                : "не найден";
    }

    private static string Compact(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
            return string.Empty;

        return new string(
            value
                .ToLowerInvariant()
                .Where(
                    char.IsLetterOrDigit)
                .ToArray());
    }

    private static void EnsureFolder(
        string assetPath)
    {
        string normalized =
            assetPath.Replace(
                '\\',
                '/');

        if (AssetDatabase.IsValidFolder(
                normalized))
            return;

        string parent =
            Path.GetDirectoryName(
                    normalized)
                ?.Replace(
                    '\\',
                    '/');

        string name =
            Path.GetFileName(
                normalized);

        if (string.IsNullOrWhiteSpace(
                parent) ||
            string.IsNullOrWhiteSpace(
                name))
            return;

        EnsureFolder(
            parent);

        AssetDatabase.CreateFolder(
            parent,
            name);
    }

    private readonly struct AudioEntry
    {
        public AudioClip Clip { get; }
        public string Path { get; }

        public AudioEntry(
            AudioClip clip,
            string path)
        {
            Clip = clip;
            Path = path;
        }
    }

    private enum AudioRole
    {
        EngineIdle,
        EngineDrive,
        DrivingLoop,
        Handbrake,
        Horn
    }
}

public sealed class VehicleEssentialsAudioAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool relevant =
            importedAssets
                .Concat(
                    movedAssets)
                .Any(
                    path =>
                        IsAudioFile(path) &&
                        LooksRelevant(path));

        if (!relevant)
            return;

        VehicleEssentialsAudioBuilder.ScheduleAutomaticBuild();
    }

    private static bool IsAudioFile(
        string path)
    {
        string extension =
            Path.GetExtension(
                    path)
                .ToLowerInvariant();

        return
            extension == ".wav" ||
            extension == ".mp3" ||
            extension == ".ogg" ||
            extension == ".aif" ||
            extension == ".aiff";
    }

    private static bool LooksRelevant(
        string path)
    {
        if (string.IsNullOrWhiteSpace(
                path))
            return false;

        string lower =
            path.ToLowerInvariant();

        return
            lower.Contains("vehicle") ||
            lower.Contains("nox_sound") ||
            lower.Contains("nox sound") ||
            lower.Contains("tire-whistling-sound") ||
            lower.Contains("tire_squeal") ||
            lower.Contains("tire squeal") ||
            lower.Contains("skid");
    }
}
#endif
