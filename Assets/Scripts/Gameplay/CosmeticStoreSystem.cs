using System.Collections.Generic;
using MotorCity.Localization;
using MotorCity.Platform;
using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class CosmeticStoreSystem : MonoBehaviour
    {
        public const string SeasonPremiumProductId =
            "motor_city_season1_premium";

        public const string TurboPackProductId =
            "motor_city_turbo_cosmetic_pack";

        private const string SeasonPremiumKey =
            "MotorCity.Purchase.Season1Premium";

        private const string TurboPackKey =
            "MotorCity.Purchase.TurboPack";

        private TurboPetSystem turbo;
        private SeasonSystem season;
        private MotorCityPurchaseRuntime purchaseRuntime;

        private int selectedProduct;
        private int lastSeasonProgress = -1;
        private float messageTimer;
        private bool purchaseRunning;

        public bool HasSeasonPremium { get; private set; }
        public bool HasTurboPack { get; private set; }

        public bool ShowMessage =>
            messageTimer > 0f;

        public string StatusText { get; private set; }

        public int SelectedProduct =>
            selectedProduct;

        public string SelectedName =>
            MotorCityLocalization.Text(
                selectedProduct == 0
                    ? "store.season.name"
                    : "store.turbo.name");

        public string SelectedDescription =>
            MotorCityLocalization.Text(
                selectedProduct == 0
                    ? "store.season.desc"
                    : "store.turbo.desc");

        public string SelectedOwnershipLine =>
            IsSelectedOwned()
                ? MotorCityLocalization.Text(
                    "store.owned")
                : MotorCityPlatform.SupportsPurchases
                    ? MotorCityLocalization.Text(
                        "store.buy")
                    : MotorCityLocalization.Text(
                        "store.yandex_only");

        public string SeasonPathLine
        {
            get
            {
                int completed =
                    season != null
                        ? season.CompletedMissionCount
                        : 0;

                string premium =
                    HasSeasonPremium
                        ? MotorCityLocalization.Text(
                            "store.premium_active")
                        : MotorCityLocalization.Text(
                            "store.premium_locked");

                return
                    MotorCityLocalization.Format(
                        "store.season_path",
                        completed,
                        season != null
                            ? season.MissionCount
                            : 10,
                        premium);
            }
        }

        public void Initialize(
            TurboPetSystem turboSystem,
            SeasonSystem seasonSystem)
        {
            turbo =
                turboSystem;

            season =
                seasonSystem;

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

            int progress =
                season != null
                    ? season.CompletedMissionCount
                    : 0;

            if (progress !=
                lastSeasonProgress)
            {
                lastSeasonProgress =
                    progress;

                ApplyPremiumSeasonRewards();
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
                    ? SeasonPremiumProductId
                    : TurboPackProductId;

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
            HasSeasonPremium =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    SeasonPremiumKey,
                    0) != 0;

            HasTurboPack =
                MotorCity.Persistence.MotorCitySaveService.GetInt(
                    TurboPackKey,
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
                SeasonPremiumProductId)
            {
                HasSeasonPremium =
                    true;

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    SeasonPremiumKey,
                    1);

                MotorCity.Persistence.MotorCitySaveService.Save();

                return true;
            }

            if (productId ==
                TurboPackProductId)
            {
                HasTurboPack =
                    true;

                MotorCity.Persistence.MotorCitySaveService.SetInt(
                    TurboPackKey,
                    1);

                MotorCity.Persistence.MotorCitySaveService.Save();

                return true;
            }

            return false;
        }

        private void ApplyEntitlements()
        {
            if (HasTurboPack)
            {
                turbo?.UnlockSkin(
                    9);
            }

            ApplyPremiumSeasonRewards();
        }

        private void ApplyPremiumSeasonRewards()
        {
            if (!HasSeasonPremium ||
                turbo == null ||
                season == null)
            {
                return;
            }

            int completed =
                season.CompletedMissionCount;

            if (completed >= 3)
            {
                turbo.UnlockSkin(
                    6);
            }

            if (completed >= 6)
            {
                turbo.UnlockSkin(
                    7);
            }

            if (completed >= 10)
            {
                turbo.UnlockSkin(
                    8);
            }
        }

        private bool IsSelectedOwned()
        {
            return
                selectedProduct == 0
                    ? HasSeasonPremium
                    : HasTurboPack;
        }

        private static bool IsKnownProduct(
            string productId)
        {
            return
                productId ==
                    SeasonPremiumProductId ||
                productId ==
                    TurboPackProductId;
        }

        private static string ProductDisplayName(
            string productId)
        {
            return
                MotorCityLocalization.Text(
                    productId ==
                    SeasonPremiumProductId
                        ? "store.season.name"
                        : "store.turbo.name");
        }
    }
}
