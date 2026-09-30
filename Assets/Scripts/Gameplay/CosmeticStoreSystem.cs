using System;
using System.Collections;
using System.Collections.Generic;
using MotorCity.Localization;
using MotorCity.Platform;
using UnityEngine;
using UnityEngine.Networking;

namespace MotorCity.Gameplay
{
    public sealed class CosmeticStoreSystem : MonoBehaviour
    {
        public const string SupporterPackProductId =
            "motor_city_supporter_pack";

        private const string SupporterPackKey =
            "MotorCity.Purchase.SupporterPack";

        private MotorCityPurchaseRuntime purchaseRuntime;

        private float messageTimer;
        private bool purchaseRunning;
        private string supporterPrice =
            string.Empty;
        private Texture2D currencyIconTexture;

        public bool HasSupporterPack { get; private set; }

        public Texture2D CurrencyIconTexture =>
            currencyIconTexture;

        public static bool SupporterPackOwned =>
            MotorCity.Persistence.MotorCitySaveService.GetInt(
                SupporterPackKey,
                0) != 0;

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public string SelectedName =>
            MotorCityLocalization.Text(
                "store.supporter.name");

        public string SelectedDescription =>
            MotorCityLocalization.Text(
                "store.supporter.desc");

        public string SelectedOwnershipLine
        {
            get
            {
                if (IsSelectedOwned())
                {
                    return
                        MotorCityLocalization.Text(
                            "store.owned");
                }

                return
                    string.IsNullOrWhiteSpace(
                        supporterPrice)
                        ? MotorCityLocalization.Text(
                            "store.buy")
                        : MotorCityLocalization.Format(
                            "store.buy_price",
                            supporterPrice);
            }
        }

        public string ProductDetailsLine =>
            MotorCityLocalization.Text(
                "store.supporter.details");

        public void Initialize()
        {
            purchaseRuntime =
                Object.FindAnyObjectByType<MotorCityPurchaseRuntime>();

            LoadEntitlements();
            ProcessPendingPurchases();
            ApplyEntitlements();
            LoadSupporterProductInfo();
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer =
                    Mathf.Max(
                        0f,
                        messageTimer -
                        Time.unscaledDeltaTime);
            }
        }

        public void PurchaseSelected()
        {
            if (purchaseRunning)
                return;

            if (IsSelectedOwned())
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "store.already_owned");

                messageTimer =
                    3.5f;

                return;
            }

            string productId =
                SupporterPackProductId;

#if UNITY_EDITOR
            // Editor-only test path. Production builds must never grant a
            // paid entitlement just because the platform purchase bridge is
            // unavailable or failed to initialize.
            if (!MotorCityPlatform.SupportsPurchases)
            {
                GrantAndConsume(
                    productId,
                    string.Empty);

                return;
            }
#else
            if (!MotorCityPlatform.SupportsPurchases)
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "store.unavailable");

                messageTimer =
                    3.5f;

                return;
            }
#endif

            purchaseRunning =
                true;

            StatusText =
                MotorCityLocalization.Text(
                    "store.opening");

            messageTimer =
                3f;

            MotorCityPlatform.Purchase(
                productId,
                (success, token) =>
                {
                    purchaseRunning =
                        false;

                    if (!success)
                    {
                        StatusText =
                            MotorCityLocalization.Text(
                                "store.cancelled");

                        messageTimer =
                            3.5f;

                        return;
                    }

                    GrantAndConsume(
                        productId,
                        token);
                });
        }

        private void LoadSupporterProductInfo()
        {
            if (!MotorCityPlatform.SupportsPurchases)
                return;

            MotorCityPlatform.LoadProductInfo(
                SupporterPackProductId,
                (success, payload) =>
                {
                    if (!success ||
                        string.IsNullOrWhiteSpace(
                            payload))
                    {
                        return;
                    }

                    int split =
                        payload.IndexOf('|');

                    string encodedPrice =
                        split >= 0
                            ? payload.Substring(
                                0,
                                split)
                            : payload;

                    string encodedIcon =
                        split >= 0 &&
                        split + 1 < payload.Length
                            ? payload.Substring(
                                split + 1)
                            : string.Empty;

                    supporterPrice =
                        DecodeCatalogValue(
                            encodedPrice);

                    string iconUrl =
                        DecodeCatalogValue(
                            encodedIcon);

                    if (!string.IsNullOrWhiteSpace(
                            iconUrl))
                    {
                        StartCoroutine(
                            LoadCurrencyIcon(
                                iconUrl));
                    }
                });
        }

        private IEnumerator LoadCurrencyIcon(
            string url)
        {
            if (string.IsNullOrWhiteSpace(
                    url))
            {
                yield break;
            }

            if (url.StartsWith(
                    "//",
                    StringComparison.Ordinal))
            {
                url =
                    "https:" +
                    url;
            }

            using UnityWebRequest request =
                UnityWebRequestTexture.GetTexture(
                    url);

            yield return
                request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                yield break;
            }

            currencyIconTexture =
                DownloadHandlerTexture.GetContent(
                    request);
        }

        private static string DecodeCatalogValue(
            string value)
        {
            if (string.IsNullOrEmpty(
                    value))
            {
                return string.Empty;
            }

            try
            {
                return
                    Uri.UnescapeDataString(
                        value);
            }
            catch
            {
                return value;
            }
        }

        private void LoadEntitlements()
        {
            HasSupporterPack =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    SupporterPackKey,
                    0) != 0;

        }

        private void ProcessPendingPurchases()
        {
            if (purchaseRuntime == null ||
                purchaseRuntime.Pending == null ||
                purchaseRuntime.Pending.Count == 0)
            {
                return;
            }

            List<PendingPurchase> copy =
                new(
                    purchaseRuntime.Pending);

            foreach (PendingPurchase purchase in
                     copy)
            {
                if (!IsKnownProduct(
                        purchase.ProductId))
                {
                    continue;
                }

                GrantAndConsume(
                    purchase.ProductId,
                    purchase.Token,
                    false);
            }
        }

        private void GrantAndConsume(
            string productId,
            string token,
            bool announce = true)
        {
            bool known =
                GrantEntitlement(
                    productId);

            if (!known)
                return;

            // Entitlement is persisted before consuming the purchase token.
            // If the game closes here, the pending transaction is safe to
            // process again because GrantEntitlement is idempotent.
            ApplyEntitlements();

            // Supporter Pack is a permanent, non-consumable purchase.
            // Keep it in Yandex getPurchases() so ownership can be restored
            // independently of local/cloud save state. Consumable products
            // should be routed through ConsumeAfterGrant when/if we add them.
            if (!IsPermanentProduct(
                    productId))
            {
                if (purchaseRuntime != null)
                {
                    purchaseRuntime.ConsumeAfterGrant(
                        token);
                }
                else if (!string.IsNullOrWhiteSpace(
                             token))
                {
                    MotorCityPlatform.ConsumePurchase(
                        token);
                }
            }

            if (!announce)
                return;

            StatusText =
                MotorCityLocalization.Format(
                    "store.granted",
                    ProductDisplayName(
                        productId));

            messageTimer =
                5f;
        }

        private bool GrantEntitlement(
            string productId)
        {
            if (productId ==
                SupporterPackProductId)
            {
                if (!HasSupporterPack)
                {
                    HasSupporterPack =
                        true;

                    MotorCity.Persistence.MotorCitySaveService.SetInt(
                        SupporterPackKey,
                        1);

                    MotorCity.Persistence.MotorCitySaveService.Save();
                }

                return true;
            }

            return false;
        }

        private void ApplyEntitlements()
        {
            if (HasSupporterPack)
            {
                TurboPetSystem pixie =
                    Object.FindAnyObjectByType<TurboPetSystem>();

                pixie?.SetSupporterPackSkin(
                    true);

                // Permanent one-time supporter reward.
                int claimed =
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        "MotorCity.Purchase.SupporterPack.RewardClaimed",
                        0);

                if (claimed == 0)
                {
                    walletReward();

                    MotorCity.Persistence.MotorCitySaveService.SetInt(
                        "MotorCity.Purchase.SupporterPack.RewardClaimed",
                        1);

                    MotorCity.Persistence.MotorCitySaveService.Save();
                }
            }

        }

        private void walletReward()
        {
            PlayerWallet wallet =
                Object.FindAnyObjectByType<PlayerWallet>();

            wallet?.AddCredits(
                5000);
        }

        private bool IsSelectedOwned()
        {
            return
                HasSupporterPack;
        }

        private static bool IsKnownProduct(
            string productId)
        {
            return
                productId ==
                    SupporterPackProductId;
        }

        private static bool IsPermanentProduct(
            string productId)
        {
            return
                productId ==
                    SupporterPackProductId;
        }

        private static string ProductDisplayName(
            string productId)
        {
            return
                MotorCityLocalization.Text(
                    "store.supporter.name");
        }
    }
}
