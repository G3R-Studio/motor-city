using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.Persistence
{
    public static class MotorCitySaveService
    {
        private const string StorageKey =
            "MotorCity.Save.Json.v1";

        private const int CurrentVersion = 2;

        private static SaveDocument document;
        private static bool initialized;
        private static bool dirty;
        private static bool needsFlush;

        public static bool IsDirty
        {
            get
            {
                EnsureLoaded();
                return
                    dirty ||
                    needsFlush;
            }
        }

        public static long ModifiedUtcTicks
        {
            get
            {
                EnsureLoaded();
                return document.ModifiedUtcTicks;
            }
        }

        public static CloudSaveMetadata GetCloudMetadata()
        {
            EnsureLoaded();

            return
                CreateCloudMetadata(
                    document);
        }

        public static bool HasData
        {
            get
            {
                EnsureLoaded();

                return
                    document.Ints.Count > 0 ||
                    document.Floats.Count > 0 ||
                    document.Strings.Count > 0;
            }
        }

        public static int GetInt(
            string key,
            int defaultValue = 0)
        {
            EnsureLoaded();

            IntEntry entry =
                document.Ints.Find(
                    item =>
                        item.Key == key);

            if (entry != null)
                return entry.Value;

            if (PlayerPrefs.HasKey(key))
            {
                int legacy =
                    PlayerPrefs.GetInt(
                        key,
                        defaultValue);

                SetIntInternal(
                    key,
                    legacy);

                return legacy;
            }

            return defaultValue;
        }

        public static float GetFloat(
            string key,
            float defaultValue = 0f)
        {
            EnsureLoaded();

            FloatEntry entry =
                document.Floats.Find(
                    item =>
                        item.Key == key);

            if (entry != null)
                return entry.Value;

            if (PlayerPrefs.HasKey(key))
            {
                float legacy =
                    PlayerPrefs.GetFloat(
                        key,
                        defaultValue);

                SetFloatInternal(
                    key,
                    legacy);

                return legacy;
            }

            return defaultValue;
        }

        public static string GetString(
            string key,
            string defaultValue = "")
        {
            EnsureLoaded();

            StringEntry entry =
                document.Strings.Find(
                    item =>
                        item.Key == key);

            if (entry != null)
                return entry.Value ?? string.Empty;

            if (PlayerPrefs.HasKey(key))
            {
                string legacy =
                    PlayerPrefs.GetString(
                        key,
                        defaultValue);

                SetStringInternal(
                    key,
                    legacy);

                return legacy;
            }

            return defaultValue;
        }

        public static void SetInt(
            string key,
            int value)
        {
            EnsureLoaded();
            SetIntInternal(
                key,
                value);
        }

        public static void SetFloat(
            string key,
            float value)
        {
            EnsureLoaded();
            SetFloatInternal(
                key,
                value);
        }

        public static void SetString(
            string key,
            string value)
        {
            EnsureLoaded();
            SetStringInternal(
                key,
                value);
        }

        public static bool HasKey(
            string key)
        {
            EnsureLoaded();

            return
                document.Ints.Exists(
                    item =>
                        item.Key == key) ||
                document.Floats.Exists(
                    item =>
                        item.Key == key) ||
                document.Strings.Exists(
                    item =>
                        item.Key == key) ||
                PlayerPrefs.HasKey(key);
        }

        public static void DeleteKey(
            string key)
        {
            EnsureLoaded();

            bool changed =
                document.Ints.RemoveAll(
                    item =>
                        item.Key == key) > 0;

            changed |=
                document.Floats.RemoveAll(
                    item =>
                        item.Key == key) > 0;

            changed |=
                document.Strings.RemoveAll(
                    item =>
                        item.Key == key) > 0;

            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                changed = true;
            }

            if (changed)
            {
                Touch();
            }
        }

        public static void Save()
        {
            EnsureLoaded();

            if (!dirty)
                return;

            StoreJson(
                false);
        }

        public static void FlushNow()
        {
            EnsureLoaded();

            if (dirty)
            {
                StoreJson(
                    true);
                return;
            }

            if (!needsFlush)
                return;

            PlayerPrefs.Save();
            needsFlush = false;
        }

        public static string ExportJson()
        {
            EnsureLoaded();

            return
                JsonUtility.ToJson(
                    document);
        }

        public static long ReadModifiedUtcTicks(
            string json)
        {
            return
                TryReadCloudMetadata(
                    json,
                    out CloudSaveMetadata metadata)
                    ? metadata.LegacyModifiedUtcTicks
                    : 0L;
        }

        public static bool TryReadCloudMetadata(
            string json,
            out CloudSaveMetadata metadata)
        {
            metadata =
                default;

            if (string.IsNullOrWhiteSpace(
                    json))
            {
                return false;
            }

            try
            {
                SaveDocument parsed =
                    JsonUtility.FromJson<SaveDocument>(
                        json);

                if (parsed == null)
                    return false;

                metadata =
                    CreateCloudMetadata(
                        parsed);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ExportCloudJson(
            long cloudRevision,
            long serverModifiedUnixTime,
            out long revision)
        {
            EnsureLoaded();

            SaveDocument snapshot =
                JsonUtility.FromJson<SaveDocument>(
                    JsonUtility.ToJson(
                        document));

            snapshot =
                Normalize(
                    snapshot);

            revision =
                snapshot.Revision;

            snapshot.Version =
                CurrentVersion;

            snapshot.CloudRevision =
                Math.Max(
                    0L,
                    cloudRevision);

            snapshot.ServerModifiedUnixTime =
                Math.Max(
                    0L,
                    serverModifiedUnixTime);

            snapshot.LastSyncedRevision =
                snapshot.Revision;

            return
                JsonUtility.ToJson(
                    snapshot);
        }

        public static void MarkCloudUploadSucceeded(
            long uploadedRevision,
            long cloudRevision,
            long serverModifiedUnixTime)
        {
            EnsureLoaded();

            document.CloudRevision =
                Math.Max(
                    document.CloudRevision,
                    cloudRevision);

            document.ServerModifiedUnixTime =
                Math.Max(
                    document.ServerModifiedUnixTime,
                    serverModifiedUnixTime);

            document.LastSyncedRevision =
                Math.Max(
                    document.LastSyncedRevision,
                    Math.Min(
                        uploadedRevision,
                        document.Revision));

            StoreJson(
                true);
        }

        public static void MarkImportedCloudSnapshot(
            long cloudRevision,
            long serverModifiedUnixTime)
        {
            EnsureLoaded();

            document.CloudRevision =
                Math.Max(
                    0L,
                    cloudRevision);

            document.ServerModifiedUnixTime =
                Math.Max(
                    0L,
                    serverModifiedUnixTime);

            document.LastSyncedRevision =
                document.Revision;

            StoreJson(
                true);
        }

        public static bool ImportJson(
            string json,
            bool flushImmediately = true)
        {
            if (string.IsNullOrWhiteSpace(
                    json))
            {
                return false;
            }

            SaveDocument imported;

            try
            {
                imported =
                    JsonUtility.FromJson<SaveDocument>(
                        json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Motor City: не удалось прочитать сохранение: " +
                    exception.Message);

                return false;
            }

            if (imported == null)
                return false;

            document =
                Normalize(
                    imported);

            initialized = true;
            dirty = true;

            StoreJson(
                flushImmediately);

            return true;
        }

        private static void EnsureLoaded()
        {
            if (initialized)
                return;

            initialized = true;

            string json =
                PlayerPrefs.GetString(
                    StorageKey,
                    string.Empty);

            if (!string.IsNullOrWhiteSpace(
                    json))
            {
                try
                {
                    document =
                        JsonUtility.FromJson<SaveDocument>(
                            json);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "Motor City: локальное сохранение повреждено, " +
                        "используется совместимый режим: " +
                        exception.Message);
                }
            }

            document =
                Normalize(
                    document);
        }

        private static SaveDocument Normalize(
            SaveDocument source)
        {
            source ??=
                new SaveDocument();

            source.Version =
                Mathf.Max(
                    CurrentVersion,
                    source.Version);

            source.Ints ??=
                new List<IntEntry>();

            source.Floats ??=
                new List<FloatEntry>();

            source.Strings ??=
                new List<StringEntry>();

            bool hasData =
                source.Ints.Count > 0 ||
                source.Floats.Count > 0 ||
                source.Strings.Count > 0;

            if (source.Revision <= 0L &&
                hasData)
            {
                source.Revision = 1L;
            }

            source.LastSyncedRevision =
                Math.Max(
                    0L,
                    Math.Min(
                        source.LastSyncedRevision,
                        source.Revision));

            source.CloudRevision =
                Math.Max(
                    0L,
                    source.CloudRevision);

            source.ServerModifiedUnixTime =
                Math.Max(
                    0L,
                    source.ServerModifiedUnixTime);

            return source;
        }

        private static void SetIntInternal(
            string key,
            int value)
        {
            bool changed =
                RemoveOtherTypes(
                    key,
                    SaveValueType.Int);

            IntEntry entry =
                document.Ints.Find(
                    item =>
                        item.Key == key);

            if (entry == null)
            {
                document.Ints.Add(
                    new IntEntry
                    {
                        Key = key,
                        Value = value
                    });

                changed = true;
            }
            else if (entry.Value != value)
            {
                entry.Value = value;
                changed = true;
            }

            if (changed)
                Touch();
        }

        private static void SetFloatInternal(
            string key,
            float value)
        {
            bool changed =
                RemoveOtherTypes(
                    key,
                    SaveValueType.Float);

            FloatEntry entry =
                document.Floats.Find(
                    item =>
                        item.Key == key);

            if (entry == null)
            {
                document.Floats.Add(
                    new FloatEntry
                    {
                        Key = key,
                        Value = value
                    });

                changed = true;
            }
            else if (entry.Value != value)
            {
                entry.Value = value;
                changed = true;
            }

            if (changed)
                Touch();
        }

        private static void SetStringInternal(
            string key,
            string value)
        {
            bool changed =
                RemoveOtherTypes(
                    key,
                    SaveValueType.String);

            string normalizedValue =
                value ?? string.Empty;

            StringEntry entry =
                document.Strings.Find(
                    item =>
                        item.Key == key);

            if (entry == null)
            {
                document.Strings.Add(
                    new StringEntry
                    {
                        Key = key,
                        Value = normalizedValue
                    });

                changed = true;
            }
            else if (!string.Equals(
                         entry.Value ?? string.Empty,
                         normalizedValue,
                         StringComparison.Ordinal))
            {
                entry.Value =
                    normalizedValue;

                changed = true;
            }

            if (changed)
                Touch();
        }

        private static bool RemoveOtherTypes(
            string key,
            SaveValueType keep)
        {
            bool changed =
                false;

            if (keep != SaveValueType.Int)
            {
                changed |=
                    document.Ints.RemoveAll(
                        item =>
                            item.Key == key) > 0;
            }

            if (keep != SaveValueType.Float)
            {
                changed |=
                    document.Floats.RemoveAll(
                        item =>
                            item.Key == key) > 0;
            }

            if (keep != SaveValueType.String)
            {
                changed |=
                    document.Strings.RemoveAll(
                        item =>
                            item.Key == key) > 0;
            }

            return
                changed;
        }

        private static void Touch()
        {
            dirty = true;

            if (document.Revision <
                long.MaxValue)
            {
                document.Revision++;
            }

            document.ModifiedUtcTicks =
                DateTime.UtcNow.Ticks;
        }

        private static void StoreJson(
            bool flushToDisk)
        {
            document.Version =
                CurrentVersion;

            string json =
                JsonUtility.ToJson(
                    document);

            PlayerPrefs.SetString(
                StorageKey,
                json);

            dirty = false;
            needsFlush = true;

            if (flushToDisk)
            {
                PlayerPrefs.Save();
                needsFlush = false;
            }
        }

        public readonly struct CloudSaveMetadata
        {
            public readonly int Version;
            public readonly bool HasData;
            public readonly long Revision;
            public readonly long LastSyncedRevision;
            public readonly long CloudRevision;
            public readonly long ServerModifiedUnixTime;
            public readonly long LegacyModifiedUtcTicks;

            public bool HasUnsyncedChanges =>
                Revision >
                LastSyncedRevision;

            public bool HasTrustedCloudMetadata =>
                CloudRevision > 0L;

            public CloudSaveMetadata(
                int version,
                bool hasData,
                long revision,
                long lastSyncedRevision,
                long cloudRevision,
                long serverModifiedUnixTime,
                long legacyModifiedUtcTicks)
            {
                Version = version;
                HasData = hasData;
                Revision = revision;
                LastSyncedRevision =
                    lastSyncedRevision;
                CloudRevision =
                    cloudRevision;
                ServerModifiedUnixTime =
                    serverModifiedUnixTime;
                LegacyModifiedUtcTicks =
                    legacyModifiedUtcTicks;
            }
        }

        private static CloudSaveMetadata CreateCloudMetadata(
            SaveDocument source)
        {
            if (source == null)
                return default;

            bool hasData =
                (source.Ints != null &&
                 source.Ints.Count > 0) ||
                (source.Floats != null &&
                 source.Floats.Count > 0) ||
                (source.Strings != null &&
                 source.Strings.Count > 0);

            long revision =
                source.Revision;

            if (revision <= 0L &&
                hasData)
            {
                revision = 1L;
            }

            return
                new CloudSaveMetadata(
                    source.Version,
                    hasData,
                    Math.Max(
                        0L,
                        revision),
                    Math.Max(
                        0L,
                        Math.Min(
                            source.LastSyncedRevision,
                            revision)),
                    Math.Max(
                        0L,
                        source.CloudRevision),
                    Math.Max(
                        0L,
                        source.ServerModifiedUnixTime),
                    Math.Max(
                        0L,
                        source.ModifiedUtcTicks));
        }

        private enum SaveValueType
        {
            Int,
            Float,
            String
        }

        [Serializable]
        private sealed class SaveDocument
        {
            public int Version =
                CurrentVersion;

            public long ModifiedUtcTicks;

            public long Revision;
            public long LastSyncedRevision;
            public long CloudRevision;
            public long ServerModifiedUnixTime;

            public List<IntEntry> Ints =
                new();

            public List<FloatEntry> Floats =
                new();

            public List<StringEntry> Strings =
                new();
        }

        [Serializable]
        private sealed class IntEntry
        {
            public string Key;
            public int Value;
        }

        [Serializable]
        private sealed class FloatEntry
        {
            public string Key;
            public float Value;
        }

        [Serializable]
        private sealed class StringEntry
        {
            public string Key;
            public string Value;
        }
    }
}
