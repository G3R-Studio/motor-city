using System.Globalization;
using System.Runtime.InteropServices;

namespace MotorCity.Platform
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // Synchronous in-memory API for existing game systems. The cache is populated
    // through Bridge storage before Unity starts; Save queues one changed snapshot.
    internal static class MotorCityBridgePrefs
    {
        public static bool HasKey(string key) => MotorCityBridgePrefHas(key) != 0;
        public static string GetString(string key, string fallback = "") =>
            HasKey(key) ? MotorCityBridgePrefGet(key) : fallback;
        public static int GetInt(string key, int fallback = 0) =>
            int.TryParse(GetString(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value : fallback;
        public static float GetFloat(string key, float fallback = 0f) =>
            float.TryParse(GetString(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value : fallback;
        public static void SetString(string key, string value) => MotorCityBridgePrefSet(key, value ?? "");
        public static void SetInt(string key, int value) => SetString(key, value.ToString(CultureInfo.InvariantCulture));
        public static void SetFloat(string key, float value) => SetString(key, value.ToString("R", CultureInfo.InvariantCulture));
        public static void DeleteKey(string key) => MotorCityBridgePrefDelete(key);
        public static void Save() => MotorCityBridgePrefFlush();

        [DllImport("__Internal")] private static extern int MotorCityBridgePrefHas(string key);
        [DllImport("__Internal")] private static extern string MotorCityBridgePrefGet(string key);
        [DllImport("__Internal")] private static extern void MotorCityBridgePrefSet(string key, string value);
        [DllImport("__Internal")] private static extern void MotorCityBridgePrefDelete(string key);
        [DllImport("__Internal")] private static extern void MotorCityBridgePrefFlush();
    }
#endif
}
