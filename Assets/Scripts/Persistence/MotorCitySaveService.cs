using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.Persistence
{
    public static class MotorCitySaveService
    {
        private const string StorageKey =
            "MotorCity.Save.Json.v1";

        private const string CorruptBackupKey =
            "MotorCity.Save.CorruptBackup.v1";

        private const int CurrentVersion = 2;

#if UNITY_EDITOR
        private static string isolatedStorageKey;
        private static string isolatedBackupKey;
#endif

        private static string ActiveStorageKey
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(isolatedStorageKey))
                    return isolatedStorageKey;
#endif
                return StorageKey;
            }
        }

        private static string ActiveBackupKey
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(isolatedBackupKey))
                    return isolatedBackupKey;
#endif
                return CorruptBackupKey;
            }
        }

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

#if UNITY_EDITOR || DEBUG
        public static void ResetProgressForTesting()
        {
            EnsureLoaded();

            SaveDocument previous =
                document;

            SaveDocument reset =
                new()
                {
                    Version =
                        CurrentVersion,
                    Revision =
                        Math.Max(
                            1L,
                            previous.Revision),
                    LastSyncedRevision =
                        Math.Min(
                            previous.LastSyncedRevision,
                            previous.Revision),
                    CloudRevision =
                        previous.CloudRevision,
                    ServerModifiedUnixTime =
                        previous.ServerModifiedUnixTime
                };

            CopyPreservedTestingEntries(
                previous,
                reset);

            DeleteLegacyProgressKeys(
                previous);

            // Keep one QA-only marker so the cloud resolver sees this reset as
            // a real local edit instead of treating an empty local document as
            // permission to restore the older remote progress.
            reset.Ints.Add(
                new IntEntry
                {
                    Key =
                        "MotorCity.QA.ProgressReset",
                    Value =
                        1
                });

            document =
                reset;

            initialized =
                true;

            dirty =
                false;

            needsFlush =
                false;

            Touch();

            StoreJson(
                true);
        }

#endif

        private static void CopyPreservedTestingEntries(
            SaveDocument source,
            SaveDocument target)
        {
            foreach (IntEntry entry in
                     source.Ints)
            {
                if (!IsPreservedTestingKey(
                        entry.Key))
                {
                    continue;
                }

                target.Ints.Add(
                    new IntEntry
                    {
                        Key =
                            entry.Key,
                        Value =
                            entry.Value
                    });
            }

            foreach (FloatEntry entry in
                     source.Floats)
            {
                if (!IsPreservedTestingKey(
                        entry.Key))
                {
                    continue;
                }

                target.Floats.Add(
                    new FloatEntry
                    {
                        Key =
                            entry.Key,
                        Value =
                            entry.Value
                    });
            }

            foreach (StringEntry entry in
                     source.Strings)
            {
                if (!IsPreservedTestingKey(
                        entry.Key))
                {
                    continue;
                }

                target.Strings.Add(
                    new StringEntry
                    {
                        Key =
                            entry.Key,
                        Value =
                            entry.Value
                    });
            }
        }

        private static void DeleteLegacyProgressKeys(
            SaveDocument source)
        {
            foreach (IntEntry entry in
                     source.Ints)
            {
                if (!IsPreservedTestingKey(
                        entry.Key))
                {
                    PlayerPrefs.DeleteKey(
                        entry.Key);
                }
            }

            foreach (FloatEntry entry in
                     source.Floats)
            {
                if (!IsPreservedTestingKey(
                        entry.Key))
                {
                    PlayerPrefs.DeleteKey(
                        entry.Key);
                }
            }

            foreach (StringEntry entry in
                     source.Strings)
            {
                if (!IsPreservedTestingKey(
                        entry.Key))
                {
                    PlayerPrefs.DeleteKey(
                        entry.Key);
                }
            }

            // Important legacy progress keys can exist outside the JSON if the
            // project was launched on an old save before migration-on-read.
            string[] knownLegacyProgressKeys =
            {
                "MotorCity.FrontEnd.IntroCompleted",
                "MotorCity.Onboarding.Step",
                "MotorCity.Onboarding.Complete",
                "MotorCity.Onboarding.FlowVersion",
                "MotorCity.PlayerCredits",
                "MotorCity.Player.Reputation",
                "MotorCity.Story.Mission",
                "MotorCity.Story.Progress",
                "MotorCity.Story.Complete",
                "MotorCity.Vehicle.Position.Has",
                "MotorCity.Vehicle.Position.X",
                "MotorCity.Vehicle.Position.Y",
                "MotorCity.Vehicle.Position.Z",
                "MotorCity.Vehicle.Position.Yaw"
            };

            foreach (string key in
                     knownLegacyProgressKeys)
            {
                PlayerPrefs.DeleteKey(
                    key);
            }

            PlayerPrefs.Save();
        }

        private static bool IsPreservedTestingKey(
            string key)
        {
            if (string.IsNullOrWhiteSpace(
                    key))
            {
                return false;
            }

            return
                key.StartsWith(
                    "MotorCity.Settings.",
                    StringComparison.Ordinal) ||
                key.StartsWith(
                    "MotorCity.Purchase.",
                    StringComparison.Ordinal);
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

                if (!IsValidParsedDocument(json, parsed))
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

            if (!IsValidParsedDocument(json, imported))
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
                    ActiveStorageKey,
                    string.Empty);

            if (!string.IsNullOrWhiteSpace(
                    json))
            {
                try
                {
                    document =
                        JsonUtility.FromJson<SaveDocument>(
                            json);

                    if (!IsValidParsedDocument(json, document))
                        throw new FormatException("Invalid save document envelope.");
                }
                catch (Exception exception)
                {
                    // Preserve the raw payload before clearing the active slot.
                    // This prevents a malformed save from being parsed on every
                    // launch while still leaving recovery data available.
                    PlayerPrefs.SetString(
                        ActiveBackupKey,
                        json);

                    PlayerPrefs.DeleteKey(
                        ActiveStorageKey);

                    PlayerPrefs.Save();

                    Debug.LogWarning(
                        "Motor City: локальное сохранение повреждено, " +
                        "создана резервная копия и используется совместимый режим: " +
                        exception.Message);
                }
            }

            document =
                Normalize(
                    document);
        }

        private static bool IsValidParsedDocument(
            string json,
            SaveDocument parsed)
        {
            // JsonUtility can produce a default-initialized object for '{}'.
            // Never treat an arbitrary JSON object as a valid save or replace
            // an existing local/cloud snapshot with it.
            return
                parsed != null &&
                parsed.Version > 0 &&
                json.IndexOf(
                    "\"Version\"",
                    StringComparison.Ordinal) >= 0;
        }

#if UNITY_EDITOR
        // Runs only from the isolated Editor CI gate. Production slot names
        // remain untouched; the temporary slots are removed in finally.
        public static void VerifyIsolatedPlayerPrefsRecovery()
        {
            SaveDocument previousDocument = document;
            bool previousInitialized = initialized;
            bool previousDirty = dirty;
            bool previousNeedsFlush = needsFlush;
            string previousStorageKey = isolatedStorageKey;
            string previousBackupKey = isolatedBackupKey;
            string token = Guid.NewGuid().ToString("N");
            string testStorage = "MotorCity.Phase8.Isolated." + token;
            string testBackup = testStorage + ".Backup";
            string legacyKey = testStorage + ".Legacy";
            try
            {
                isolatedStorageKey = testStorage;
                isolatedBackupKey = testBackup;
                initialized = false;
                document = null;
                dirty = false;
                needsFlush = false;

                PlayerPrefs.SetString(testStorage, "{ broken JSON");
                PlayerPrefs.Save();
                EnsureLoaded();
                if (PlayerPrefs.HasKey(testStorage) ||
                    PlayerPrefs.GetString(testBackup, "") != "{ broken JSON")
                    throw new InvalidOperationException("Corrupt local JSON backup failed.");

                PlayerPrefs.SetInt(legacyKey, 731);
                initialized = false;
                document = null;
                if (GetInt(legacyKey, -1) != 731)
                    throw new InvalidOperationException("Legacy PlayerPrefs migration failed.");
                Save();
                FlushNow();

                // Simulated restart: clear only in-memory service state.
                initialized = false;
                document = null;
                if (GetInt(legacyKey, -1) != 731)
                    throw new InvalidOperationException("Migrated value lost after reload.");
                if (!ExportJson().Contains(legacyKey))
                    throw new InvalidOperationException("Migrated key missing in JSON slot.");
                
                // A cloud upload must acknowledge only the snapshot revision:
                // edits made while the request is in flight stay unsynced.
                SetInt(testStorage + ".FirstEdit", 1);
                string upload = ExportCloudJson(7, 100, out long attempted);
                if (string.IsNullOrEmpty(upload))
                    throw new InvalidOperationException("Cannot export cloud snapshot.");
                SetInt(testStorage + ".SecondEdit", 2);
                MarkCloudUploadSucceeded(attempted, 7, 100);
                if (!GetCloudMetadata().HasUnsyncedChanges)
                    throw new InvalidOperationException("In-flight edits were incorrectly marked synced.");
                if (GetCloudMetadata().CloudRevision != 7)
                    throw new InvalidOperationException("Cloud revision acknowledgment was lost.");

                // Invalid imported cloud JSON must not replace the local data.
                if (ImportJson("{}", true) ||
                    ImportJson(@"{""Version"":0}", true) ||
                    GetInt(legacyKey, -1) != 731)
                    throw new InvalidOperationException("Invalid cloud JSON replaced local progress.");

            }
            finally
            {
                PlayerPrefs.DeleteKey(testStorage);
                PlayerPrefs.DeleteKey(testBackup);
                PlayerPrefs.DeleteKey(legacyKey);
                PlayerPrefs.Save();
                isolatedStorageKey = previousStorageKey;
                isolatedBackupKey = previousBackupKey;
                document = previousDocument;
                initialized = previousInitialized;
                dirty = previousDirty;
                needsFlush = previousNeedsFlush;
            }
        }
#endif

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

            SanitizeIntEntries(
                source.Ints);

            SanitizeFloatEntries(
                source.Floats);

            SanitizeStringEntries(
                source.Strings);

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

        private static void SanitizeIntEntries(
            List<IntEntry> entries)
        {
            HashSet<string> keys =
                new(
                    StringComparer.Ordinal);

            entries.RemoveAll(
                item =>
                    item == null ||
                    string.IsNullOrWhiteSpace(
                        item.Key) ||
                    !keys.Add(
                        item.Key));
        }

        private static void SanitizeFloatEntries(
            List<FloatEntry> entries)
        {
            HashSet<string> keys =
                new(
                    StringComparer.Ordinal);

            entries.RemoveAll(
                item =>
                    item == null ||
                    string.IsNullOrWhiteSpace(
                        item.Key) ||
                    float.IsNaN(
                        item.Value) ||
                    float.IsInfinity(
                        item.Value) ||
                    !keys.Add(
                        item.Key));
        }

        private static void SanitizeStringEntries(
            List<StringEntry> entries)
        {
            HashSet<string> keys =
                new(
                    StringComparer.Ordinal);

            entries.RemoveAll(
                item =>
                    item == null ||
                    string.IsNullOrWhiteSpace(
                        item.Key) ||
                    !keys.Add(
                        item.Key));

            for (int i = 0;
                 i < entries.Count;
                 i++)
            {
                entries[i].Value ??=
                    string.Empty;
            }
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
                ActiveStorageKey,
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
