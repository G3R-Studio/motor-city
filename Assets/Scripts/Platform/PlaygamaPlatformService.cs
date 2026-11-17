using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace MotorCity.Platform
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // The official Bridge JS Core is initialized before the Unity loader.
    // This adapter keeps the existing game-facing platform contract intact.
    [Preserve]
    public sealed class PlaygamaPlatformService : MonoBehaviour, IMotorCityPlatformService
    {
        private Action<bool> initializeCallback;
        private Action<bool> rewardedCallback;
        private Action interstitialCallback;
        private string languageCode = "en";
        private bool supportsRewarded;
        private bool supportsInterstitial;
        private bool readySent;
        private bool started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("Motor City Playgama Bridge");
            DontDestroyOnLoad(host);
            var service = host.AddComponent<PlaygamaPlatformService>();
            MotorCityPlatform.SetService(service);
        }

        public bool IsInitialized { get; private set; }
        public bool IsAuthenticated => false;
        // Storage is already backed by Bridge, including cloud synchronization
        // when the host supports it. Do not run the legacy second cloud writer.
        public bool SupportsCloudSave => false;
        public bool SupportsLeaderboards => false;
        public bool SupportsAds => IsInitialized && supportsRewarded;
        public bool SupportsInterstitial => IsInitialized && supportsInterstitial;
        // Paid products require a verified Playgama product catalog and receipt flow.
        public bool SupportsPurchases => false;
        public string LanguageCode => languageCode;
        public long ServerUnixTime => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public long TrustedServerUnixTime => 0L;

        public void Initialize(Action<bool> completed)
        {
            if (IsInitialized) { completed?.Invoke(true); return; }
            bool requested = initializeCallback != null;
            initializeCallback += completed;
            if (requested) return;
            MotorCityWebConfigureBrowser();
            MotorCityBridgeInitialize(gameObject.name);
        }

        [Preserve]
        public void OnBridgeInitialized(string payload)
        {
            var parts = (payload ?? "").Split('|');
            languageCode = parts.Length > 0 && parts[0] == "ru" ? "ru" : "en";
            supportsRewarded = parts.Length > 1 && parts[1] == "1";
            supportsInterstitial = parts.Length > 2 && parts[2] == "1";
            IsInitialized = true;
            var callback = initializeCallback;
            initializeCallback = null;
            callback?.Invoke(true);
        }

        [Preserve]
        public void OnBridgeInitFailed(string reason)
        {
            Debug.LogError("Motor City: Playgama initialization failed: " + reason);
            var callback = initializeCallback;
            initializeCallback = null;
            callback?.Invoke(false);
        }

        [Preserve]
        public void OnBridgePause(string value) =>
            MotorCityPlatformRuntime.SetPlatformEventPaused(value == "1");

        [Preserve]
        public void OnBridgeAudio(string value) =>
            AudioListener.volume = value == "1" ? 1f : 0f;

        public void GameReady()
        {
            if (!IsInitialized || readySent) return;
            readySent = true;
            MotorCityBridgeMessage("game_ready");
        }
        public void GameplayStart()
        {
            if (!IsInitialized) return;
            MotorCityBridgeMessage(started ? "level_resumed" : "level_started");
            started = true;
        }
        public void GameplayStop()
        {
            if (IsInitialized && started) MotorCityBridgeMessage("level_paused");
        }

        public void ShowRewarded(string placementId, Action<bool> completed)
        {
            if (!SupportsAds || rewardedCallback != null || interstitialCallback != null)
            { completed?.Invoke(false); return; }
            rewardedCallback = completed ?? (_ => {});
            MotorCityPlatformRuntime.SetPlatformModalPaused(true);
            MotorCityBridgeAd(gameObject.name, placementId, 1);
        }
        public void ShowInterstitial(string placementId, Action completed)
        {
            if (!SupportsInterstitial || rewardedCallback != null || interstitialCallback != null)
            { completed?.Invoke(); return; }
            interstitialCallback = completed ?? (() => {});
            MotorCityPlatformRuntime.SetPlatformModalPaused(true);
            MotorCityBridgeAd(gameObject.name, placementId, 0);
        }
        [Preserve]
        public void OnBridgeRewarded(string value)
        {
            if (rewardedCallback == null) return;
            var callback = rewardedCallback;
            rewardedCallback = null;
            MotorCityPlatformRuntime.SetPlatformModalPaused(false);
            callback(value == "1");
        }
        [Preserve]
        public void OnBridgeInterstitial(string ignored)
        {
            if (interstitialCallback == null) return;
            var callback = interstitialCallback;
            interstitialCallback = null;
            MotorCityPlatformRuntime.SetPlatformModalPaused(false);
            callback();
        }

        // Optional legacy capabilities are deliberately unavailable for the first
        // Playgama release; no fake rewards, receipts or trusted timestamps.
        public void LoadCloudSave(Action<bool, string> completed) => completed?.Invoke(false, "");
        public void SaveCloudSave(string json, Action<bool> completed) => completed?.Invoke(false);
        public void SubmitLeaderboard(string leaderboardId, long score, Action<bool> completed) => completed?.Invoke(false);
        public void Purchase(string productId, Action<bool, string> completed) => completed?.Invoke(false, "");
        public void ConsumePurchase(string purchaseToken, Action<bool> completed) => completed?.Invoke(false);
        public void LoadPendingPurchases(Action<bool, string> completed) => completed?.Invoke(false, "");
        public void LoadProductInfo(string productId, Action<bool, string> completed) => completed?.Invoke(false, "");
        public void LoadRemoteConfig(Action<bool, string> completed) => completed?.Invoke(false, "");
        public void IncrementStat(string key, long amount, Action<bool> completed) => completed?.Invoke(false);

        [DllImport("__Internal")] private static extern void MotorCityWebConfigureBrowser();
        [DllImport("__Internal")] private static extern void MotorCityBridgeInitialize(string name);
        [DllImport("__Internal")] private static extern void MotorCityBridgeMessage(string message);
        [DllImport("__Internal")] private static extern void MotorCityBridgeAd(string name, string placement, int rewarded);
    }
#endif
}
