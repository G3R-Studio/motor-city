using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace MotorCity.Platform
{
    public sealed class YandexPlatformService :
        IMotorCityPlatformService
    {
        private const int MaxCloudSaveBytes =
            190 * 1024;

        private readonly YandexPlatformBridge bridge;

        private string languageCode = "ru";
        private bool isAuthenticated;
        private long initializedServerUnixTime;
        private float initializedRealtime;

        public bool IsInitialized { get; private set; }

        public bool IsAuthenticated =>
            isAuthenticated;

        public bool SupportsCloudSave =>
            IsInitialized &&
            bridge != null &&
            bridge.PlayerAvailable;

        public bool SupportsLeaderboards =>
            false;

        public bool SupportsAds =>
            false;

        public bool SupportsPurchases =>
            false;

        public string LanguageCode =>
            languageCode;

        public long ServerUnixTime
        {
            get
            {
                if (!IsInitialized)
                {
                    return
                        DateTimeOffset.UtcNow
                            .ToUnixTimeSeconds();
                }

#if UNITY_WEBGL && !UNITY_EDITOR
                double milliseconds =
                    MotorCityYandexServerTime();

                if (milliseconds > 0d)
                {
                    return
                        (long)(milliseconds / 1000d);
                }
#endif

                long elapsed =
                    Mathf.Max(
                        0,
                        Mathf.FloorToInt(
                            Time.realtimeSinceStartup -
                            initializedRealtime));

                return
                    initializedServerUnixTime +
                    elapsed;
            }
        }

        public YandexPlatformService(
            YandexPlatformBridge targetBridge)
        {
            bridge =
                targetBridge;
        }

        public void Initialize(
            Action<bool> completed)
        {
            if (IsInitialized)
            {
                completed?.Invoke(
                    true);
                return;
            }

            if (bridge == null)
            {
                completed?.Invoke(
                    false);
                return;
            }

            bridge.InitializeYandex(
                result =>
                {
                    if (!result.Success)
                    {
                        completed?.Invoke(
                            false);
                        return;
                    }

                    IsInitialized = true;
                    isAuthenticated =
                        result.IsAuthenticated;
                    languageCode =
                        string.IsNullOrWhiteSpace(
                            result.LanguageCode)
                            ? "ru"
                            : result.LanguageCode;

                    initializedServerUnixTime =
                        result.ServerTimeMilliseconds > 0L
                            ? result.ServerTimeMilliseconds / 1000L
                            : DateTimeOffset.UtcNow
                                .ToUnixTimeSeconds();

                    initializedRealtime =
                        Time.realtimeSinceStartup;

                    completed?.Invoke(
                        true);
                });
        }

        public void GameReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsInitialized)
            {
                MotorCityYandexGameReady();
            }
#endif
        }

        public void GameplayStart()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsInitialized)
            {
                MotorCityYandexGameplayStart();
            }
#endif
        }

        public void GameplayStop()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (IsInitialized)
            {
                MotorCityYandexGameplayStop();
            }
#endif
        }

        public void LoadCloudSave(
            Action<bool, string> completed)
        {
            if (!SupportsCloudSave)
            {
                completed?.Invoke(
                    false,
                    string.Empty);
                return;
            }

            bridge.LoadCloudSave(
                completed);
        }

        public void SaveCloudSave(
            string json,
            Action<bool> completed)
        {
            if (!SupportsCloudSave ||
                string.IsNullOrWhiteSpace(
                    json))
            {
                completed?.Invoke(
                    false);
                return;
            }

            int byteCount =
                Encoding.UTF8.GetByteCount(
                    json);

            if (byteCount >
                MaxCloudSaveBytes)
            {
                Debug.LogWarning(
                    "Motor City: cloud save is too large for Yandex Player Data: " +
                    byteCount +
                    " bytes.");

                completed?.Invoke(
                    false);
                return;
            }

            bridge.SaveCloudSave(
                json,
                completed);
        }

        public void SubmitLeaderboard(
            string leaderboardId,
            long score,
            Action<bool> completed)
        {
            completed?.Invoke(
                false);
        }

        public void ShowRewarded(
            string placementId,
            Action<bool> completed)
        {
            completed?.Invoke(
                false);
        }

        public void ShowInterstitial(
            string placementId,
            Action completed)
        {
            completed?.Invoke();
        }

        public void Purchase(
            string productId,
            Action<bool, string> completed)
        {
            completed?.Invoke(
                false,
                string.Empty);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void MotorCityYandexGameReady();

        [DllImport("__Internal")]
        private static extern void MotorCityYandexGameplayStart();

        [DllImport("__Internal")]
        private static extern void MotorCityYandexGameplayStop();

        [DllImport("__Internal")]
        private static extern double MotorCityYandexServerTime();
#endif
    }

    public sealed class YandexPlatformBridge :
        MonoBehaviour
    {
        private Action<YandexInitResult> initializeCallback;
        private Action<bool, string> loadCallback;
        private Action<bool> saveCallback;

        public bool PlayerAvailable { get; private set; }

        public void InitializeYandex(
            Action<YandexInitResult> completed)
        {
            initializeCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexInitialize(
                gameObject.name);
#else
            completed?.Invoke(
                new YandexInitResult
                {
                    Success = false
                });
#endif
        }

        public void LoadCloudSave(
            Action<bool, string> completed)
        {
            loadCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexLoadCloudSave(
                gameObject.name);
#else
            completed?.Invoke(
                false,
                string.Empty);
#endif
        }

        public void SaveCloudSave(
            string json,
            Action<bool> completed)
        {
            saveCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexSaveCloudSave(
                gameObject.name,
                json);
#else
            completed?.Invoke(
                false);
#endif
        }

        public void OnYandexInitialized(
            string payload)
        {
            string[] parts =
                (payload ?? string.Empty)
                    .Split('|');

            bool authenticated =
                parts.Length > 0 &&
                parts[0] == "1";

            string language =
                parts.Length > 1
                    ? parts[1]
                    : "ru";

            long serverTime = 0L;

            if (parts.Length > 2)
            {
                long.TryParse(
                    parts[2],
                    out serverTime);
            }

            bool playerAvailable =
                parts.Length > 3 &&
                parts[3] == "1";

            PlayerAvailable =
                playerAvailable;

            Action<YandexInitResult> callback =
                initializeCallback;

            initializeCallback = null;

            callback?.Invoke(
                new YandexInitResult
                {
                    Success = true,
                    IsAuthenticated = authenticated,
                    LanguageCode = language,
                    ServerTimeMilliseconds = serverTime
                });
        }

        public void OnYandexInitFailed(
            string message)
        {
            PlayerAvailable = false;

            Debug.LogWarning(
                "Motor City: Yandex SDK initialization failed: " +
                message);

            Action<YandexInitResult> callback =
                initializeCallback;

            initializeCallback = null;

            callback?.Invoke(
                new YandexInitResult
                {
                    Success = false
                });
        }

        public void OnYandexCloudLoaded(
            string json)
        {
            Action<bool, string> callback =
                loadCallback;

            loadCallback = null;

            callback?.Invoke(
                true,
                json ?? string.Empty);
        }

        public void OnYandexCloudLoadFailed(
            string message)
        {
            Debug.LogWarning(
                "Motor City: Yandex cloud load failed: " +
                message);

            Action<bool, string> callback =
                loadCallback;

            loadCallback = null;

            callback?.Invoke(
                false,
                string.Empty);
        }

        public void OnYandexCloudSaved(
            string ignored)
        {
            Action<bool> callback =
                saveCallback;

            saveCallback = null;

            callback?.Invoke(
                true);
        }

        public void OnYandexCloudSaveFailed(
            string message)
        {
            Debug.LogWarning(
                "Motor City: Yandex cloud save failed: " +
                message);

            Action<bool> callback =
                saveCallback;

            saveCallback = null;

            callback?.Invoke(
                false);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void MotorCityYandexInitialize(
            string gameObjectName);

        [DllImport("__Internal")]
        private static extern void MotorCityYandexLoadCloudSave(
            string gameObjectName);

        [DllImport("__Internal")]
        private static extern void MotorCityYandexSaveCloudSave(
            string gameObjectName,
            string json);
#endif
    }

    public struct YandexInitResult
    {
        public bool Success;
        public bool IsAuthenticated;
        public string LanguageCode;
        public long ServerTimeMilliseconds;
    }
}
