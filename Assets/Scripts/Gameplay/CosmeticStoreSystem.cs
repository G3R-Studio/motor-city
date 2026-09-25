using System.Collections.Generic;
using MotorCity.Localization;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CosmeticStoreSystem : MonoBehaviour
    {
        public const string SupporterPackProductId =
            "motor_city_supporter_pack";

        public const string PixieKisoraProductId =
            "motor_city_pixie_kisora";

        private const string SupporterPackKey =
            "MotorCity.Purchase.SupporterPack";

        private const string PixieKisoraKey =
            "MotorCity.Purchase.PixieKisora";

        private TurboPetSystem turbo;
        private MotorCityPurchaseRuntime purchaseRuntime;

        private int selectedProduct;
        private float messageTimer;
        private bool purchaseRunning;

        public bool HasSupporterPack { get; private set; }
        public bool HasPixieKisora { get; private set; }

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public int SelectedProduct =>
            selectedProduct;

        public string SelectedName =>
            MotorCityLocalization.Text(
                selectedProduct == 0
                    ? "store.supporter.name"
                    : "store.pixie_kisora.name");

        public string SelectedDescription =>
            MotorCityLocalization.Text(
                selectedProduct == 0
                    ? "store.supporter.desc"
                    : "store.pixie_kisora.desc");

        public string SelectedOwnershipLine =>
            IsSelectedOwned()
                ? MotorCityLocalization.Text(
                    "store.owned")
                : MotorCityLocalization.Text(
                    "store.buy");

        public string ProductDetailsLine =>
            MotorCityLocalization.Text(
                selectedProduct == 0
                    ? "store.supporter.details"
                    : "store.pixie_kisora.details");

        public void Initialize(
            TurboPetSystem turboSystem)
        {
            turbo =
                turboSystem;

            purchaseRuntime =
                Object.FindAnyObjectByType<MotorCityPurchaseRuntime>();

            LoadEntitlements();
            ProcessPendingPurchases();
            ApplyEntitlements();
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

        public void CycleProduct(
            int direction)
        {
            selectedProduct =
                (selectedProduct +
                 (direction >= 0
                     ? 1
                     : -1) +
                 2) %
                2;
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

            if (!MotorCityPlatform.SupportsPurchases)
            {
                StatusText =
                    MotorCityLocalization.Text(
                        "store.yandex_only");

                messageTimer =
                    4f;

                return;
            }

            string productId =
                selectedProduct == 0
                    ? SupporterPackProductId
                    : PixieKisoraProductId;

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

        private void LoadEntitlements()
        {
            HasSupporterPack =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    SupporterPackKey,
                    0) != 0;

            HasPixieKisora =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    PixieKisoraKey,
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

            if (productId ==
                PixieKisoraProductId)
            {
                if (!HasPixieKisora)
                {
                    HasPixieKisora =
                        true;

                    MotorCity.Persistence.MotorCitySaveService.SetInt(
                        PixieKisoraKey,
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

            if (HasPixieKisora)
            {
                turbo?.UnlockSkin(
                    9);
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
                selectedProduct == 0
                    ? HasSupporterPack
                    : HasPixieKisora;
        }

        private static bool IsKnownProduct(
            string productId)
        {
            return
                productId ==
                    SupporterPackProductId ||
                productId ==
                    PixieKisoraProductId;
        }

        private static string ProductDisplayName(
            string productId)
        {
            return
                MotorCityLocalization.Text(
                    productId ==
                    SupporterPackProductId
                        ? "store.supporter.name"
                        : "store.pixie_kisora.name");
        }
    }
}
