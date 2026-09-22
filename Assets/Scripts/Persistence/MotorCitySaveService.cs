using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.Persistence
{
    public static class MotorCitySaveService
    {
        private const string StorageKey =
            "MotorCity.Save.Json.v1";

        private const int CurrentVersion = 1;

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
            if (string.IsNullOrWhiteSpace(
                    json))
            {
                return 0L;
            }

            try
            {
                SaveDocument parsed =
                    JsonUtility.FromJson<SaveDocument>(
                        json);

                return
                    parsed == null
                        ? 0L
                        : parsed.ModifiedUtcTicks;
            }
            catch
            {
                return 0L;
            }
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

            return source;
        }

        private static void SetIntInternal(
            string key,
            int value)
        {
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
            }
            else
            {
                entry.Value = value;
            }

            Touch();
        }

        private static void SetFloatInternal(
            string key,
            float value)
        {
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
            }
            else
            {
                entry.Value = value;
            }

            Touch();
        }

        private static void SetStringInternal(
            string key,
            string value)
        {
            RemoveOtherTypes(
                key,
                SaveValueType.String);

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
                        Value = value ?? string.Empty
                    });
            }
            else
            {
                entry.Value =
                    value ?? string.Empty;
            }

            Touch();
        }

        private static void RemoveOtherTypes(
            string key,
            SaveValueType keep)
        {
            if (keep != SaveValueType.Int)
            {
                document.Ints.RemoveAll(
                    item =>
                        item.Key == key);
            }

            if (keep != SaveValueType.Float)
            {
                document.Floats.RemoveAll(
                    item =>
                        item.Key == key);
            }

            if (keep != SaveValueType.String)
            {
                document.Strings.RemoveAll(
                    item =>
                        item.Key == key);
            }
        }

        private static void Touch()
        {
            dirty = true;

            document.ModifiedUtcTicks =
                DateTime.UtcNow.Ticks;
        }

        private static void StoreJson(
            bool flushToDisk)
        {
            document.ModifiedUtcTicks =
                DateTime.UtcNow.Ticks;

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
