using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        public const string SmallCreditsId = "motor_city_credits_25000";
        public const string MediumCreditsId = "motor_city_credits_100000";
        public const string LargeCreditsId = "motor_city_credits_500000";
        public const string NoAdsId = "motor_city_no_ads";
        public const string DoubleIncomeId = "motor_city_double_income";
        private const string NoAdsKey = "MotorCity.Purchase.NoAds";
        private const string DoubleIncomeKey = "MotorCity.Purchase.DoubleIncome";
        private const string ReceiptPrefix = "MotorCity.Purchase.Receipt.";
        private static readonly string[] Products = {
            SupporterPackProductId, SmallCreditsId, MediumCreditsId,
            LargeCreditsId, NoAdsId, DoubleIncomeId
        };
        private int selectedProductIndex;
        public static bool NoAdsOwned =>
            MotorCity.Persistence.MotorCitySaveService.GetInt(NoAdsKey, 0) != 0;
        public static bool DoubleIncomeOwned =>
            MotorCity.Persistence.MotorCitySaveService.GetInt(DoubleIncomeKey, 0) != 0;
        public string SelectedProductId => Products[selectedProductIndex];

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void MotorCityYandexSetStickyEnabled(int enabled);
#endif

        public void NextProduct()
        {
            if (purchaseRunning || !productCatalogResolved)
                return;
            selectedProductIndex = (selectedProductIndex + 1) % Products.Length;
            LoadSupporterProductInfo();
        }


        private MotorCityPurchaseRuntime purchaseRuntime;

        private float messageTimer;
        private bool purchaseRunning;
        private string supporterPrice =
            string.Empty;
        private Texture2D currencyIconTexture;
        private bool productCatalogResolved;
        private bool supporterProductAvailable;

        public bool HasSupporterPack { get; private set; }

        public bool ProductCatalogResolved =>
            productCatalogResolved;

        public bool SelectedProductAvailable =>
            supporterProductAvailable;
        public bool SelectedOwned => IsSelectedOwned();

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
            MotorCityLocalization.Text(ProductLocalizationKey(SelectedProductId, "name"));

        public string SelectedDescription =>
            MotorCityLocalization.Text(ProductLocalizationKey(SelectedProductId, "desc"));

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

                if (!productCatalogResolved)
                {
                    return
                        MotorCityLocalization.Text(
                            "store.loading_product");
                }

                if (!supporterProductAvailable)
                {
                    return
                        MotorCityLocalization.Text(
                            "store.unavailable");
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
            MotorCityLocalization.Text(ProductLocalizationKey(SelectedProductId, "details"));

        private static string ProductLocalizationKey(string productId, string part)
        {
            string prefix = productId switch
            {
                SmallCreditsId => "store.credits.small.",
                MediumCreditsId => "store.credits.medium.",
                LargeCreditsId => "store.credits.large.",
                NoAdsId => "store.noads.",
                DoubleIncomeId => "store.doubleincome.",
                _ => "store.supporter."
            };
            return prefix + part;
        }

        public void Initialize()
        {
            purchaseRuntime =
                UnityEngine.Object.FindAnyObjectByType<MotorCityPurchaseRuntime>();

            LoadEntitlements();
            ProcessPendingPurchases();
            ApplyEntitlements();

#if UNITY_EDITOR
            if (!MotorCityPlatform.SupportsPurchases)
            {
                productCatalogResolved =
                    true;

                supporterProductAvailable =
                    true;
            }
#endif

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
                SelectedProductId;

#if !UNITY_EDITOR
            if (!productCatalogResolved ||
                !supporterProductAvailable)
            {
                StatusText =
                    !productCatalogResolved
                        ? MotorCityLocalization.Text(
                            "store.loading_product")
                        : MotorCityLocalization.Text(
                            "store.unavailable");

                messageTimer =
                    3.5f;

                return;
            }
#endif

#if UNITY_EDITOR
            // Editor-only test path. Production builds must never grant a
            // paid entitlement just because the platform purchase bridge is
            // unavailable or failed to initialize.
            if (!MotorCityPlatform.SupportsPurchases)
            {
                GrantAndConsume(
                    productId,
                    "editor-" + Guid.NewGuid().ToString("N"));

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
            string requestedId = SelectedProductId;
            productCatalogResolved = false;
            supporterProductAvailable = false;
            supporterPrice = string.Empty;
            if (currencyIconTexture != null)
            {
                UnityEngine.Object.Destroy(currencyIconTexture);
                currencyIconTexture = null;
            }
            if (!MotorCityPlatform.SupportsPurchases)
            {
                productCatalogResolved = true;
#if UNITY_EDITOR
                supporterProductAvailable = true;
#else
                supporterProductAvailable = false;
#endif
                return;
            }

            MotorCityPlatform.LoadProductInfo(
                requestedId,
                (success, payload) =>
                {
                    if (requestedId != SelectedProductId)
                        return;
                    productCatalogResolved =
                        true;

                    supporterProductAvailable =
                        false;

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

                    supporterProductAvailable =
                        !string.IsNullOrWhiteSpace(
                            supporterPrice);

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
                UnityWebRequest.Get(
                    url);

            yield return
                request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success ||
                request.downloadHandler == null ||
                request.downloadHandler.data == null ||
                request.downloadHandler.data.Length == 0)
            {
                yield break;
            }

            Texture2D texture =
                new(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false);

            if (!texture.LoadImage(
                    request.downloadHandler.data,
                    false))
            {
                UnityEngine.Object.Destroy(
                    texture);

                yield break;
            }

            currencyIconTexture =
                texture;
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
            bool known;
            int credits = productId switch
            {
                SmallCreditsId => 25000,
                MediumCreditsId => 100000,
                LargeCreditsId => 500000,
                _ => 0
            };
            if (credits > 0)
            {
                // Wallet and receipt are written in one save document before
                // consuming the token. Replayed pending purchases cannot
                // grant the same credit pack twice.
                if (string.IsNullOrWhiteSpace(token))
                    return;
                string receiptKey = ReceiptPrefix + Uri.EscapeDataString(token);
                if (MotorCity.Persistence.MotorCitySaveService.GetInt(receiptKey, 0) == 0)
                {
                    PlayerWallet wallet =
                        UnityEngine.Object.FindAnyObjectByType<PlayerWallet>();
                    if (wallet == null)
                        return;
                    wallet.AddCredits(credits, false, false);
                    MotorCity.Persistence.MotorCitySaveService.SetInt(receiptKey, 1);
                    MotorCity.Persistence.MotorCitySaveService.Save();
                }
                known = true;
            }
            else
            {
                known = GrantEntitlement(productId);
                if (known)
                    ApplyEntitlements();
            }
            if (!known)
                return;

            // Permanent entitlements remain visible in getPurchases().
            // Consumable tokens are acknowledged only after the granted
            // wallet/receipt revision has reached the cloud (if available).
            if (!IsPermanentProduct(productId))
                ConsumeCreditsAfterSync(token);

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

        private void ConsumeCreditsAfterSync(string token)
        {
            var cloud = UnityEngine.Object.FindAnyObjectByType<
                MotorCity.Persistence.MotorCityCloudSaveRuntime>();

            if (cloud == null || !MotorCityPlatform.SupportsCloudSave ||
                !MotorCity.Persistence.MotorCitySaveService
                    .GetCloudMetadata().HasUnsyncedChanges)
            {
                ConsumeToken(token);
                return;
            }

            Action<bool> uploaded = null;
            uploaded = successful =>
            {
                if (!successful)
                {
                    cloud.UploadCompleted -= uploaded;
                    return; // keep the token pending for the next launch
                }

                if (MotorCity.Persistence.MotorCitySaveService
                    .GetCloudMetadata().HasUnsyncedChanges)
                {
                    cloud.ForceUpload();
                    return;
                }

                cloud.UploadCompleted -= uploaded;
                ConsumeToken(token);
            };
            cloud.UploadCompleted += uploaded;
            cloud.ForceUpload();
        }

        private void ConsumeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return;
            if (purchaseRuntime != null)
                purchaseRuntime.ConsumeAfterGrant(token);
            else
                MotorCityPlatform.ConsumePurchase(token);
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
                        2);

                    MotorCity.Persistence.MotorCitySaveService.Save();
                }

                return true;
            }

            if (productId == NoAdsId)
            {
                MotorCity.Persistence.MotorCitySaveService.SetInt(NoAdsKey, 1);
                MotorCity.Persistence.MotorCitySaveService.Save();
                return true;
            }
            if (productId == DoubleIncomeId)
            {
                MotorCity.Persistence.MotorCitySaveService.SetInt(DoubleIncomeKey, 1);
                MotorCity.Persistence.MotorCitySaveService.Save();
                return true;
            }
            return false;
        }

        private void ApplyEntitlements()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (MotorCityPlatform.SupportsPurchases)
                MotorCityYandexSetStickyEnabled(NoAdsOwned ? 0 : 1);
#endif
            if (HasSupporterPack)
            {
                PixieCompanionSystem pixie =
                    UnityEngine.Object.FindAnyObjectByType<PixieCompanionSystem>();

                pixie?.SetSupporterPackSkin(
                    true);

                // Permanent one-time supporter reward.
                int claimed =
                    MotorCity.Persistence.MotorCitySaveService.GetInt(
                        "MotorCity.Purchase.SupporterPack.RewardClaimed",
                        0);

                if (claimed < 2)
                {
                    if (!walletReward(claimed == 1 ? 95000 : 100000))
                        return;

                    MotorCity.Persistence.MotorCitySaveService.SetInt(
                        "MotorCity.Purchase.SupporterPack.RewardClaimed",
                        2);

                    MotorCity.Persistence.MotorCitySaveService.Save();
                }
            }

        }

        private bool walletReward(int credits)
        {
            PlayerWallet wallet =
                UnityEngine.Object.FindAnyObjectByType<PlayerWallet>();

            if (wallet == null)
                return false;
            wallet.AddCredits(credits, true, false);
            return true;
        }

        private bool IsSelectedOwned()
        {
            return SelectedProductId switch
            {
                SupporterPackProductId => HasSupporterPack,
                NoAdsId => NoAdsOwned,
                DoubleIncomeId => DoubleIncomeOwned,
                _ => false
            };
        }

        private static bool IsKnownProduct(
            string productId)
        {
            return Array.IndexOf(Products, productId) >= 0;
        }

        private static bool IsPermanentProduct(
            string productId)
        {
            return productId == SupporterPackProductId ||
                productId == NoAdsId || productId == DoubleIncomeId;
        }

        private static string ProductDisplayName(
            string productId)
        {
            return MotorCityLocalization.Text(
                ProductLocalizationKey(productId, "name"));
        }
    }
}
