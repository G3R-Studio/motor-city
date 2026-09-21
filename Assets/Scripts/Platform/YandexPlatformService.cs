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
            IsInitialized &&
            isAuthenticated;

        public bool SupportsAds =>
            IsInitialized;

        public bool SupportsPurchases =>
            IsInitialized;

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
            if (!SupportsLeaderboards ||
                string.IsNullOrWhiteSpace(
                    leaderboardId))
            {
                completed?.Invoke(
                    false);
                return;
            }

            bridge.SubmitLeaderboard(
                leaderboardId,
                (double)Math.Max(
                    0L,
                    score),
                completed);
        }

        public void ShowRewarded(
            string placementId,
            Action<bool> completed)
        {
            if (!SupportsAds)
            {
                completed?.Invoke(
                    false);
                return;
            }

            GameplayStop();

            bridge.ShowRewarded(
                placementId,
                rewarded =>
                {
                    GameplayStart();
                    completed?.Invoke(
                        rewarded);
                });
        }

        public void ShowInterstitial(
            string placementId,
            Action completed)
        {
            if (!SupportsAds)
            {
                completed?.Invoke();
                return;
            }

            GameplayStop();

            bridge.ShowInterstitial(
                placementId,
                () =>
                {
                    GameplayStart();
                    completed?.Invoke();
                });
        }

        public void Purchase(
            string productId,
            Action<bool, string> completed)
        {
            if (!SupportsPurchases ||
                string.IsNullOrWhiteSpace(
                    productId))
            {
                completed?.Invoke(
                    false,
                    string.Empty);
                return;
            }

            GameplayStop();

            bridge.Purchase(
                productId,
                (success, token) =>
                {
                    GameplayStart();
                    completed?.Invoke(
                        success,
                        token);
                });
        }

        public void ConsumePurchase(
            string purchaseToken,
            Action<bool> completed)
        {
            if (!SupportsPurchases ||
                string.IsNullOrWhiteSpace(
                    purchaseToken))
            {
                completed?.Invoke(
                    false);
                return;
            }

            bridge.ConsumePurchase(
                purchaseToken,
                completed);
        }

        public void LoadPendingPurchases(
            Action<bool, string> completed)
        {
            if (!SupportsPurchases)
            {
                completed?.Invoke(
                    false,
                    string.Empty);
                return;
            }

            bridge.LoadPendingPurchases(
                completed);
        }

        public void LoadRemoteConfig(
            Action<bool, string> completed)
        {
            if (!IsInitialized)
            {
                completed?.Invoke(
                    false,
                    string.Empty);
                return;
            }

            bridge.LoadRemoteConfig(
                completed);
        }

        public void IncrementStat(
            string key,
            long amount,
            Action<bool> completed)
        {
            if (!SupportsCloudSave ||
                string.IsNullOrWhiteSpace(
                    key) ||
                amount == 0L)
            {
                completed?.Invoke(
                    false);
                return;
            }

            bridge.IncrementStat(
                key,
                (double)amount,
                completed);
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
        private Action<bool> leaderboardCallback;
        private Action<bool> rewardedCallback;
        private Action interstitialCallback;
        private Action<bool, string> purchaseCallback;
        private Action<bool> consumeCallback;
        private Action<bool, string> pendingPurchasesCallback;
        private Action<bool, string> remoteConfigCallback;
        private Action<bool> statCallback;

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

        public void SubmitLeaderboard(
            string leaderboardId,
            double score,
            Action<bool> completed)
        {
            leaderboardCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexSubmitLeaderboard(
                gameObject.name,
                leaderboardId,
                score);
#else
            completed?.Invoke(
                false);
#endif
        }

        public void ShowRewarded(
            string placementId,
            Action<bool> completed)
        {
            rewardedCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexShowRewarded(
                gameObject.name,
                placementId ?? string.Empty);
#else
            completed?.Invoke(
                false);
#endif
        }

        public void ShowInterstitial(
            string placementId,
            Action completed)
        {
            interstitialCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexShowInterstitial(
                gameObject.name,
                placementId ?? string.Empty);
#else
            completed?.Invoke();
#endif
        }

        public void Purchase(
            string productId,
            Action<bool, string> completed)
        {
            purchaseCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexPurchase(
                gameObject.name,
                productId);
#else
            completed?.Invoke(
                false,
                string.Empty);
#endif
        }

        public void ConsumePurchase(
            string token,
            Action<bool> completed)
        {
            consumeCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexConsumePurchase(
                gameObject.name,
                token);
#else
            completed?.Invoke(
                false);
#endif
        }

        public void LoadPendingPurchases(
            Action<bool, string> completed)
        {
            pendingPurchasesCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexLoadPendingPurchases(
                gameObject.name);
#else
            completed?.Invoke(
                false,
                string.Empty);
#endif
        }

        public void LoadRemoteConfig(
            Action<bool, string> completed)
        {
            remoteConfigCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexLoadRemoteConfig(
                gameObject.name);
#else
            completed?.Invoke(
                false,
                string.Empty);
#endif
        }

        public void IncrementStat(
            string key,
            double amount,
            Action<bool> completed)
        {
            statCallback =
                completed;

#if UNITY_WEBGL && !UNITY_EDITOR
            MotorCityYandexIncrementStat(
                gameObject.name,
                key,
                amount);
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

        public void OnYandexLeaderboardResult(
            string value)
        {
            Action<bool> callback =
                leaderboardCallback;

            leaderboardCallback = null;

            callback?.Invoke(
                value == "1");
        }

        public void OnYandexRewardedResult(
            string value)
        {
            Action<bool> callback =
                rewardedCallback;

            rewardedCallback = null;

            callback?.Invoke(
                value == "1");
        }

        public void OnYandexInterstitialClosed(
            string ignored)
        {
            Action callback =
                interstitialCallback;

            interstitialCallback = null;

            callback?.Invoke();
        }

        public void OnYandexPurchaseResult(
            string payload)
        {
            int separator =
                string.IsNullOrEmpty(
                    payload)
                    ? -1
                    : payload.IndexOf('|');

            bool success =
                separator >= 0 &&
                payload.Substring(
                    0,
                    separator) == "1";

            string token =
                separator >= 0 &&
                separator + 1 <
                payload.Length
                    ? payload.Substring(
                        separator + 1)
                    : string.Empty;

            Action<bool, string> callback =
                purchaseCallback;

            purchaseCallback = null;

            callback?.Invoke(
                success,
                token);
        }

        public void OnYandexConsumeResult(
            string value)
        {
            Action<bool> callback =
                consumeCallback;

            consumeCallback = null;

            callback?.Invoke(
                value == "1");
        }

        public void OnYandexPendingPurchases(
            string payload)
        {
            Action<bool, string> callback =
                pendingPurchasesCallback;

            pendingPurchasesCallback = null;

            callback?.Invoke(
                true,
                payload ?? string.Empty);
        }

        public void OnYandexPendingPurchasesFailed(
            string message)
        {
            Debug.LogWarning(
                "Motor City: pending purchases failed: " +
                message);

            Action<bool, string> callback =
                pendingPurchasesCallback;

            pendingPurchasesCallback = null;

            callback?.Invoke(
                false,
                string.Empty);
        }

        public void OnYandexRemoteConfig(
            string payload)
        {
            Action<bool, string> callback =
                remoteConfigCallback;

            remoteConfigCallback = null;

            callback?.Invoke(
                true,
                payload ?? string.Empty);
        }

        public void OnYandexRemoteConfigFailed(
            string message)
        {
            Debug.LogWarning(
                "Motor City: remote config failed: " +
                message);

            Action<bool, string> callback =
                remoteConfigCallback;

            remoteConfigCallback = null;

            callback?.Invoke(
                false,
                string.Empty);
        }

        public void OnYandexStatResult(
            string value)
        {
            Action<bool> callback =
                statCallback;

            statCallback = null;

            callback?.Invoke(
                value == "1");
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

        [DllImport("__Internal")]
        private static extern void MotorCityYandexSubmitLeaderboard(
            string gameObjectName,
            string leaderboardId,
            double score);

        [DllImport("__Internal")]
        private static extern void MotorCityYandexShowRewarded(
            string gameObjectName,
            string placementId);

        [DllImport("__Internal")]
        private static extern void MotorCityYandexShowInterstitial(
            string gameObjectName,
            string placementId);

        [DllImport("__Internal")]
        private static extern void MotorCityYandexPurchase(
            string gameObjectName,
            string productId);

        [DllImport("__Internal")]
        private static extern void MotorCityYandexConsumePurchase(
            string gameObjectName,
            string purchaseToken);

        [DllImport("__Internal")]
        private static extern void MotorCityYandexLoadPendingPurchases(
            string gameObjectName);

        [DllImport("__Internal")]
        private static extern void MotorCityYandexLoadRemoteConfig(
            string gameObjectName);

        [DllImport("__Internal")]
        private static extern void MotorCityYandexIncrementStat(
            string gameObjectName,
            string key,
            double amount);
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
