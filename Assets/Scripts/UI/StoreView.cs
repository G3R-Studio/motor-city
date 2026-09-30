using MotorCity.Input;
using MotorCity.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private void HandleStoreInput()
        {
            if (cosmeticStore == null)
                return;

            if (activityManager == null ||
                !activityManager.SecondaryProgressionAllowed)
            {
                if (storeOpen)
                {
                    storeOpen =
                        false;

                    SetActiveIfChanged(
                        storeOverlay,
                        false);

                    RefreshDrivingEnabledForUi();
                }

                return;
            }

            if (MotorCityInput.ToggleStorePressed)
            {
                bool opening =
                    !storeOpen;

                if (opening &&
                    ((activityManager != null &&
                      (activityManager.IsBusy ||
                       activityManager.HasResult)) ||
                     (garage != null &&
                      garage.IsOpen)))
                {
                    return;
                }

                storeOpen =
                    opening;

                SetActiveIfChanged(
                    storeOverlay,
                    storeOpen);

                if (storeOpen)
                {
                    CloseNavigatorMenuVisualOnly();

                    if (clubOverlay != null)
                    {
                        clubOverlay.SetActive(
                            false);
                    }

                    UpdateStoreOverlay();
                }

                RefreshDrivingEnabledForUi();
            }

            if (!storeOpen)
                return;

            if (MotorCityInput.CancelPressed)
            {
                storeOpen =
                    false;

                SetActiveIfChanged(
                    storeOverlay,
                    false);

                RefreshDrivingEnabledForUi();
                return;
            }

            if (MotorCityInput.InteractPressed)
            {
                cosmeticStore.PurchaseSelected();
            }
        }

        private void BuildStoreOverlay(
            Transform canvas)
        {
            storeOverlay =
                new GameObject(
                    "Store Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            storeOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                storeOverlay.GetComponent<RectTransform>();

            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            Image backdrop =
                storeOverlay.GetComponent<Image>();

            backdrop.color =
                new Color(
                    0.005f,
                    0.008f,
                    0.014f,
                    0.84f);

            backdrop.raycastTarget =
                true;

            RectTransform panel =
                CreatePanel(
                    storeOverlay.transform,
                    "Store Panel",
                    new Vector2(0f, 18f),
                    new Vector2(760f, 470f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Color.clear);

            ApplyModalPanelTexture(
                panel);

            // Header: icon/title on the left, account summary on the right.
            CreateHudIcon(
                panel,
                "Store Header Icon",
                MotorCityIconLibrary.Store,
                new Vector2(-314f, -29f),
                new Vector2(30f, 30f),
                new Vector2(0.5f, 1f),
                TextColor);

            Text title =
                CreateText(
                    panel,
                    "Store Title",
                    26,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(-102f, -27f),
                    new Vector2(360f, 40f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    TextColor);

            title.text =
                MotorCityLocalization.Text(
                    "store.title");

            storeWalletText =
                CreateText(
                    panel,
                    "Store Wallet",
                    13,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    new Vector2(170f, -35f),
                    new Vector2(320f, 34f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            RectTransform divider =
                CreatePanel(
                    panel,
                    "Store Header Divider",
                    new Vector2(0f, 168f),
                    new Vector2(668f, 2f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        0.28f,
                        0.55f,
                        1f,
                        0.20f));

            Image dividerImage =
                divider.GetComponent<Image>();
            if (dividerImage != null)
            {
                dividerImage.raycastTarget =
                    false;
            }

            // Left hero tile gives the product a visual anchor instead of
            // presenting the whole store as one large block of text.
            RectTransform heroCard =
                CreatePanel(
                    panel,
                    "Store Hero Card",
                    new Vector2(-220f, -15f),
                    new Vector2(230f, 290f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        0.065f,
                        0.075f,
                        0.14f,
                        0.92f));

            storeProductIcon =
                CreateHudIcon(
                    heroCard,
                    "Store Product Icon",
                    MotorCityIconLibrary.Reward,
                    new Vector2(0f, 38f),
                    new Vector2(88f, 88f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        0.50f,
                        0.72f,
                        1f,
                        1f));

            Text selectionHint =
                CreateText(
                    heroCard,
                    "Store Selection Hint",
                    12,
                    FontStyle.Bold,
                    TextAnchor.LowerCenter,
                    new Vector2(0f, 22f),
                    new Vector2(190f, 28f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    SecondaryTextColor);

            selectionHint.text =
                MotorCityLocalization.Text(
                    "store.selection_hint");
            selectionHint.resizeTextForBestFit =
                true;
            selectionHint.resizeTextMinSize =
                9;
            selectionHint.resizeTextMaxSize =
                12;

            // Right side is a clean information stack.
            RectTransform detailCard =
                CreatePanel(
                    panel,
                    "Store Detail Card",
                    new Vector2(135f, -15f),
                    new Vector2(438f, 290f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        PanelSoftColor.r,
                        PanelSoftColor.g,
                        PanelSoftColor.b,
                        0.82f));

            storeNameText =
                CreateText(
                    detailCard,
                    "Store Product Name",
                    23,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(24f, -24f),
                    new Vector2(382f, 42f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    TextColor);

            storeDescriptionText =
                CreateText(
                    detailCard,
                    "Store Product Description",
                    14,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    new Vector2(24f, -78f),
                    new Vector2(382f, 76f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    SecondaryTextColor);

            storePathText =
                CreateText(
                    detailCard,
                    "Store Season Path",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(24f, -4f),
                    new Vector2(382f, 40f),
                    new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Color(0.24f, 0.88f, 1f, 1f));

            storeOwnershipText =
                CreateText(
                    detailCard,
                    "Store Ownership",
                    16,
                    FontStyle.Bold,
                    TextAnchor.LowerLeft,
                    new Vector2(24f, 26f),
                    new Vector2(350f, 38f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    DriftAccent);

            GameObject currencyIconObject =
                new(
                    "Store Currency Icon",
                    typeof(RectTransform),
                    typeof(RawImage));

            currencyIconObject.transform.SetParent(
                detailCard,
                false);

            RectTransform currencyIconRect =
                currencyIconObject.GetComponent<RectTransform>();

            currencyIconRect.anchorMin =
                new Vector2(1f, 0f);
            currencyIconRect.anchorMax =
                new Vector2(1f, 0f);
            currencyIconRect.pivot =
                new Vector2(1f, 0f);
            currencyIconRect.anchoredPosition =
                new Vector2(-20f, 31f);
            currencyIconRect.sizeDelta =
                new Vector2(22f, 22f);

            storeCurrencyIcon =
                currencyIconObject.GetComponent<RawImage>();

            storeCurrencyIcon.raycastTarget =
                false;
            storeCurrencyIcon.color =
                Color.white;
            storeCurrencyIcon.enabled =
                false;

            foreach (Text localizedText in
                     new[]
                     {
                         storeNameText,
                         storeDescriptionText,
                         storePathText,
                         storeOwnershipText,
                         storeWalletText
                     })
            {
                if (localizedText == null)
                    continue;

                localizedText.resizeTextForBestFit =
                    true;
                localizedText.resizeTextMinSize =
                    localizedText == storeDescriptionText
                        ? 10
                        : 11;
                localizedText.resizeTextMaxSize =
                    localizedText.fontSize;
                localizedText.horizontalOverflow =
                    HorizontalWrapMode.Wrap;
                localizedText.verticalOverflow =
                    VerticalWrapMode.Truncate;
            }

            // Store-specific footer lives inside the modal, so controls no
            // longer float detached below the window.
            CreateLocalizedTouchPulseButton(
                panel,
                "Store Buy",
                "touch.store.buy",
                MotorCityInputAction.Interact,
                new Vector2(-92f, 34f),
                new Vector2(170f, 44f));

            CreateLocalizedTouchPulseButton(
                panel,
                "Store Close",
                "touch.modal.close",
                MotorCityInputAction.ToggleStore,
                new Vector2(102f, 34f),
                new Vector2(170f, 44f));
        }

        private void UpdateStoreOverlay()
        {
            if (cosmeticStore == null ||
                storeOverlay == null ||
                !storeOpen)
            {
                return;
            }

            storeNameText.text =
                cosmeticStore.SelectedName;

            storeDescriptionText.text =
                cosmeticStore.SelectedDescription;

            storePathText.text =
                cosmeticStore.ProductDetailsLine;

            storeOwnershipText.text =
                cosmeticStore.SelectedOwnershipLine;

            storeWalletText.text =
                MotorCityLocalization.Format(
                    "store.wallet",
                    wallet != null
                        ? wallet.Credits
                        : 0,
                    activityManager != null
                        ? activityManager.TotalReputation
                        : 0,
                    activityManager != null
                        ? activityManager.ReputationLevel
                        : 1);

            if (storeProductIcon != null)
            {
                storeProductIcon.sprite =
                    MotorCityIconLibrary.Reward;

                storeProductIcon.color =
                    new Color(
                        0.50f,
                        0.72f,
                        1f,
                        1f);
            }

            if (storeCurrencyIcon != null)
            {
                Texture2D currencyTexture =
                    cosmeticStore.CurrencyIconTexture;

                bool showCurrencyIcon =
                    !cosmeticStore.HasSupporterPack &&
                    currencyTexture != null;

                storeCurrencyIcon.texture =
                    currencyTexture;
                storeCurrencyIcon.color =
                    Color.white;
                storeCurrencyIcon.enabled =
                    showCurrencyIcon;
            }
        }

    }
}
