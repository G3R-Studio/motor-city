using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class MotorCityRemoteConfigRuntime :
        MonoBehaviour
    {
        private static readonly Dictionary<string, string> Values =
            new()
            {
                { "rewarded_credits", "250" },
                { "interstitial_min_seconds", "240" },
                { "daily_tasks_enabled", "1" }
            };

        public bool IsLoaded { get; private set; }

        public void Load(
            Action completed = null)
        {
            if (!MotorCityPlatform.IsInitialized)
            {
                IsLoaded = true;
                completed?.Invoke();
                return;
            }

            MotorCityPlatform.LoadRemoteConfig(
                (success, payload) =>
                {
                    if (success)
                    {
                        ApplyCompactPayload(
                            payload);
                    }

                    IsLoaded = true;
                    completed?.Invoke();
                });
        }

        public static string GetString(
            string key,
            string fallback = "")
        {
            return
                !string.IsNullOrWhiteSpace(
                    key) &&
                Values.TryGetValue(
                    key,
                    out string value)
                    ? value
                    : fallback;
        }

        public static int GetInt(
            string key,
            int fallback)
        {
            string value =
                GetString(
                    key,
                    string.Empty);

            return
                int.TryParse(
                    value,
                    out int parsed)
                    ? parsed
                    : fallback;
        }

        public static float GetFloat(
            string key,
            float fallback)
        {
            string value =
                GetString(
                    key,
                    string.Empty);

            return
                float.TryParse(
                    value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float parsed)
                    ? parsed
                    : fallback;
        }

        public static bool GetBool(
            string key,
            bool fallback)
        {
            string value =
                GetString(
                    key,
                    string.Empty);

            if (value == "1")
                return true;

            if (value == "0")
                return false;

            return
                bool.TryParse(
                    value,
                    out bool parsed)
                    ? parsed
                    : fallback;
        }

        private static void ApplyCompactPayload(
            string payload)
        {
            if (string.IsNullOrWhiteSpace(
                    payload))
            {
                return;
            }

            string[] pairs =
                payload.Split('&');

            foreach (string pair in
                     pairs)
            {
                int split =
                    pair.IndexOf('=');

                if (split <= 0)
                    continue;

                string key =
                    Decode(
                        pair.Substring(
                            0,
                            split));

                string value =
                    Decode(
                        pair.Substring(
                            split + 1));

                if (!string.IsNullOrWhiteSpace(
                        key))
                {
                    Values[key] =
                        value;
                }
            }
        }

        private static string Decode(
            string value)
        {
            if (string.IsNullOrEmpty(
                    value))
            {
                return string.Empty;
            }

            return
                Uri.UnescapeDataString(
                    value.Replace(
                        "+",
                        " "));
        }
    }
}
