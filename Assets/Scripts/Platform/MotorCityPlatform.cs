using System;
using UnityEngine;

namespace MotorCity.Platform
{
    public interface IMotorCityPlatformService
    {
        bool IsInitialized { get; }
        bool IsAuthenticated { get; }
        bool SupportsCloudSave { get; }
        bool SupportsLeaderboards { get; }
        bool SupportsAds { get; }
        bool SupportsPurchases { get; }
        string LanguageCode { get; }
        long ServerUnixTime { get; }
        long TrustedServerUnixTime { get; }

        void Initialize(
            Action<bool> completed);

        void GameReady();
        void GameplayStart();
        void GameplayStop();

        void LoadCloudSave(
            Action<bool, string> completed);

        void SaveCloudSave(
            string json,
            Action<bool> completed);

        void SubmitLeaderboard(
            string leaderboardId,
            long score,
            Action<bool> completed);

        void ShowRewarded(
            string placementId,
            Action<bool> completed);

        void ShowInterstitial(
            string placementId,
            Action completed);

        void Purchase(
            string productId,
            Action<bool, string> completed);

        void ConsumePurchase(
            string purchaseToken,
            Action<bool> completed);

        void LoadPendingPurchases(
            Action<bool, string> completed);

        void LoadRemoteConfig(
            Action<bool, string> completed);

        void IncrementStat(
            string key,
            long amount,
            Action<bool> completed);
    }

    public static class MotorCityPlatform
    {
        private static IMotorCityPlatformService service;

        public static IMotorCityPlatformService Service =>
            service ??=
                new LocalPlatformService();

        public static bool IsInitialized =>
            Service.IsInitialized;

        public static string LanguageCode =>
            Service.LanguageCode;

        public static long ServerUnixTime =>
            Service.ServerUnixTime;

        public static long TrustedServerUnixTime =>
            Service.TrustedServerUnixTime;

        public static bool IsAuthenticated =>
            Service.IsAuthenticated;

        public static bool SupportsCloudSave =>
            Service.SupportsCloudSave;

        public static bool SupportsLeaderboards =>
            Service.SupportsLeaderboards;

        public static bool SupportsAds =>
            Service.SupportsAds;

        public static bool SupportsPurchases =>
            Service.SupportsPurchases;

        public static void SetService(
            IMotorCityPlatformService platformService)
        {
            service =
                platformService ??
                new LocalPlatformService();
        }

        public static void Initialize(
            Action<bool> completed = null)
        {
            Service.Initialize(
                completed);
        }

        public static void GameReady()
        {
            Service.GameReady();
        }

        public static void GameplayStart()
        {
            Service.GameplayStart();
        }

        public static void GameplayStop()
        {
            Service.GameplayStop();
        }

        public static void LoadCloudSave(
            Action<bool, string> completed)
        {
            Service.LoadCloudSave(
                completed);
        }

        public static void SaveCloudSave(
            string json,
            Action<bool> completed = null)
        {
            Service.SaveCloudSave(
                json,
                completed);
        }

        public static void SubmitLeaderboard(
            string leaderboardId,
            long score,
            Action<bool> completed = null)
        {
            Service.SubmitLeaderboard(
                leaderboardId,
                score,
                completed);
        }

        public static void ShowRewarded(
            string placementId,
            Action<bool> completed)
        {
            Service.ShowRewarded(
                placementId,
                completed);
        }

        public static void ShowInterstitial(
            string placementId,
            Action completed = null)
        {
            Service.ShowInterstitial(
                placementId,
                completed);
        }

        public static void Purchase(
            string productId,
            Action<bool, string> completed)
        {
            Service.Purchase(
                productId,
                completed);
        }

        public static void ConsumePurchase(
            string purchaseToken,
            Action<bool> completed = null)
        {
            Service.ConsumePurchase(
                purchaseToken,
                completed);
        }

        public static void LoadPendingPurchases(
            Action<bool, string> completed)
        {
            Service.LoadPendingPurchases(
                completed);
        }

        public static void LoadRemoteConfig(
            Action<bool, string> completed)
        {
            Service.LoadRemoteConfig(
                completed);
        }

        public static void IncrementStat(
            string key,
            long amount,
            Action<bool> completed = null)
        {
            Service.IncrementStat(
                key,
                amount,
                completed);
        }
    }

    internal sealed class LocalPlatformService :
        IMotorCityPlatformService
    {
        public bool IsInitialized { get; private set; }

        public bool IsAuthenticated =>
            false;

        public bool SupportsCloudSave =>
            false;

        public bool SupportsLeaderboards =>
            false;

        public bool SupportsAds =>
            false;

        public bool SupportsPurchases =>
            false;

        public string LanguageCode
        {
            get
            {
                return
                    Application.systemLanguage ==
                    SystemLanguage.Russian
                        ? "ru"
                        : "en";
            }
        }

        public long ServerUnixTime =>
            DateTimeOffset.UtcNow
                .ToUnixTimeSeconds();

        public long TrustedServerUnixTime =>
            0L;

        public void Initialize(
            Action<bool> completed)
        {
            IsInitialized = true;
            completed?.Invoke(
                true);
        }

        public void GameReady()
        {
        }

        public void GameplayStart()
        {
        }

        public void GameplayStop()
        {
        }

        public void LoadCloudSave(
            Action<bool, string> completed)
        {
            completed?.Invoke(
                false,
                string.Empty);
        }

        public void SaveCloudSave(
            string json,
            Action<bool> completed)
        {
            completed?.Invoke(
                false);
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

        public void ConsumePurchase(
            string purchaseToken,
            Action<bool> completed)
        {
            completed?.Invoke(
                false);
        }

        public void LoadPendingPurchases(
            Action<bool, string> completed)
        {
            completed?.Invoke(
                false,
                string.Empty);
        }

        public void LoadRemoteConfig(
            Action<bool, string> completed)
        {
            completed?.Invoke(
                false,
                string.Empty);
        }

        public void IncrementStat(
            string key,
            long amount,
            Action<bool> completed)
        {
            completed?.Invoke(
                false);
        }
    }
}
