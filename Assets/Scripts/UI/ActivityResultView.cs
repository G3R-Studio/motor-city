using MotorCity.Audio;
using MotorCity.Input;
using MotorCity.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private void BuildActivityResult(Transform canvas)
        {
            activityResultOverlay =
                new GameObject(
                    "Activity Result Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            activityResultOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                activityResultOverlay.GetComponent<RectTransform>();

            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            Image backdrop =
                activityResultOverlay.GetComponent<Image>();

            backdrop.color =
                new Color(
                    0.012f,
                    0.010f,
                    0.022f,
                    0.78f);

            backdrop.raycastTarget = false;

            RectTransform panel =
                CreatePanel(
                    activityResultOverlay.transform,
                    "Activity Result",
                    Vector2.zero,
                    new Vector2(680f, 500f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Color.clear);

            ApplyModalPanelTexture(
                panel);

            GameObject glowObject =
                new GameObject(
                    "New Record Glow",
                    typeof(RectTransform),
                    typeof(Image));

            glowObject.transform.SetParent(
                panel,
                false);

            RectTransform glowRect =
                glowObject.GetComponent<RectTransform>();

            glowRect.anchorMin =
                Vector2.zero;

            glowRect.anchorMax =
                Vector2.one;

            glowRect.offsetMin =
                new Vector2(
                    8f,
                    8f);

            glowRect.offsetMax =
                new Vector2(
                    -8f,
                    -8f);

            resultRecordGlow =
                glowObject.GetComponent<Image>();

            resultRecordGlow.color =
                new Color(
                    0.20f,
                    0.58f,
                    1f,
                    0f);

            resultRecordGlow.raycastTarget =
                false;

            glowObject.transform.SetAsFirstSibling();

            resultActivityIcon =
                CreateHudIcon(
                    panel,
                    "Result Activity Icon",
                    MotorCityIconLibrary.Achievement,
                    new Vector2(
                        -246f,
                        -32f),
                    new Vector2(
                        30f,
                        30f),
                    new Vector2(
                        0.5f,
                        1f),
                    SecondaryTextColor);

            resultTitleText =
                CreateText(
                    panel,
                    "Result Title",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -32f),
                    new Vector2(560f, 30f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            resultHeadlineText =
                CreateText(
                    panel,
                    "Result Headline",
                    32,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -86f),
                    new Vector2(560f, 52f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    TextColor);

            resultDetailsText =
                CreateText(
                    panel,
                    "Result Details",
                    18,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -150f),
                    new Vector2(560f, 56f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            resultRewardIcon =
                CreateHudIcon(
                    panel,
                    "Result Reward Icon",
                    MotorCityIconLibrary.Reward,
                    new Vector2(
                        -214f,
                        -218f),
                    new Vector2(
                        30f,
                        30f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Color(
                        1f,
                        0.78f,
                        0.20f,
                        1f));

            resultRewardText =
                CreateText(
                    panel,
                    "Result Reward",
                    25,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(12f, -218f),
                    new Vector2(520f, 44f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    TextColor);

            resultMasteryText =
                CreateText(
                    panel,
                    "Result Mastery",
                    17,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -270f),
                    new Vector2(540f, 32f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    BlueAccent);

            resultSecondaryProgressText =
                CreateText(
                    panel,
                    "Result Secondary Progress",
                    15,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -314f),
                    new Vector2(570f, 74f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            resultNextGoalText =
                CreateText(
                    panel,
                    "Result Next Goal",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, -390f),
                    new Vector2(560f, 44f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    BlueAccent);

            resultNextGoalText.text =
                string.Empty;

            resultControlsText =
                CreateText(
                    panel,
                    "Result Controls",
                    13,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 82f),
                    new Vector2(560f, 24f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    SecondaryTextColor);

            resultControlsText.text =
                string.Empty;

            resultRetryButton =
                CreateResultActionButton(
                    activityResultOverlay.transform,
                    "Result Retry Button",
                    "touch.result.retry",
                    MotorCityInputAction.Retry,
                    new Vector2(-110f, -270f),
                    new Vector2(190f, 50f));

            resultContinueButton =
                CreateResultActionButton(
                    activityResultOverlay.transform,
                    "Result Continue Button",
                    "touch.result.continue",
                    MotorCityInputAction.Cancel,
                    new Vector2(120f, -270f),
                    new Vector2(230f, 50f));
        }

        private GameObject CreateResultActionButton(
            Transform parent,
            string name,
            string localizationKey,
            MotorCityInputAction action,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject buttonObject =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            buttonObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0.5f, 0.5f);
            rect.anchorMax =
                new Vector2(0.5f, 0.5f);
            rect.pivot =
                new Vector2(0.5f, 0.5f);
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                buttonObject.GetComponent<Image>();

            Sprite buttonSprite =
                GetModalButtonSprite(
                    uiThemeAssets == null
                        ? null
                        : uiThemeAssets.modalButton);

            if (buttonSprite != null)
            {
                image.sprite =
                    buttonSprite;
                image.type =
                    Image.Type.Simple;
                image.color =
                    Color.white;
            }
            else
            {
                image.color =
                    new Color(
                        0.075f,
                        0.07f,
                        0.12f,
                        0.96f);
            }

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            button.onClick.AddListener(
                () =>
                    MotorCityInput.PulseVirtual(
                        action));

            RectTransform iconRect =
                GarageObject(
                    rect,
                    "Reference Action Icon");

            iconRect.anchorMin =
                iconRect.anchorMax =
                iconRect.pivot =
                    new Vector2(
                        0f,
                        0.5f);

            iconRect.anchoredPosition =
                new Vector2(
                    16f,
                    0f);

            iconRect.sizeDelta =
                new Vector2(
                    24f,
                    24f);

            GarageReferenceGraphic icon =
                iconRect.gameObject.AddComponent<
                    GarageReferenceGraphic>();

            icon.symbol =
                action == MotorCityInputAction.Retry
                    ? GarageReferenceGraphic.Symbol.NavigationRight
                    : GarageReferenceGraphic.Symbol.Check;

            icon.color =
                TextColor;

            icon.raycastTarget =
                false;

            Text label =
                CreateText(
                    rect,
                    "Label",
                    15,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        12f,
                        0f),
                    size -
                    new Vector2(12f, 8f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            label.text =
                MotorCityLocalization.Text(
                    localizationKey);

            MakeButtonTextCrisp(
                label);

            return buttonObject;
        }

        private void BuildResultTouchControls(
            Transform canvas)
        {
            resultTouchControlsRoot =
                new GameObject(
                    "Result Touch Controls",
                    typeof(RectTransform));

            resultTouchControlsRoot.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                resultTouchControlsRoot.GetComponent<RectTransform>();

            root.anchorMin =
                new Vector2(0.5f, 0f);
            root.anchorMax =
                new Vector2(0.5f, 0f);
            root.pivot =
                new Vector2(0.5f, 0f);
            root.anchoredPosition =
                new Vector2(0f, 42f);
            root.sizeDelta =
                new Vector2(380f, 56f);

            resultRetryTouchButton =
                CreateLocalizedTouchPulseButton(
                    root,
                    "Result Retry",
                    "touch.result.retry",
                    MotorCityInputAction.Retry,
                    new Vector2(-112f, 4f),
                    new Vector2(140f, 44f));

            resultContinueTouchButton =
                CreateLocalizedTouchPulseButton(
                    root,
                    "Result Continue",
                    "touch.result.continue",
                    MotorCityInputAction.Cancel,
                    new Vector2(78f, 4f),
                    new Vector2(220f, 52f));

            resultTouchControlsRoot.SetActive(
                false);
        }

        private void UpdateActivityResult()
        {
            if (activityManager == null ||
                !activityManager.HasResult)
            {
                resultRecordAnimationTimer =
                    0f;

                return;
            }

            resultTitleText.text =
                activityManager.ResultTitle ?? string.Empty;

            resultHeadlineText.text =
                activityManager.ResultHeadline ?? string.Empty;

            resultDetailsText.text =
                activityManager.ResultDetails ?? string.Empty;

            AlignResultIconToText(
                resultActivityIcon,
                resultTitleText,
                18f);

            bool hasCredits =
                activityManager.ResultRewardCredits > 0;

            bool hasReputation =
                activityManager.ResultReputationReward > 0;

            resultRewardText.text =
                hasCredits || hasReputation
                    ? MotorCityLocalization.Format(
                        "hud.result_reward_split",
                        activityManager.ResultRewardCredits,
                        activityManager.ResultReputationReward)
                    : MotorCityLocalization.Text(
                        "hud.no_rewards");

            AlignResultIconToText(
                resultRewardIcon,
                resultRewardText,
                18f);

            if (resultMasteryText != null)
            {
                bool hasMastery =
                    activityManager.ResultMasteryXp > 0;

                resultMasteryText.text =
                    hasMastery
                        ? MotorCityLocalization.Format(
                            "hud.result_mastery",
                            activityManager.ResultMasteryXp)
                        : string.Empty;

                resultMasteryText.gameObject.SetActive(
                    hasMastery);
            }

            if (resultSecondaryProgressText != null)
            {
                string secondary =
                    activityManager.ResultSecondaryProgress ??
                    string.Empty;

                resultSecondaryProgressText.text =
                    secondary;

                resultSecondaryProgressText.gameObject.SetActive(
                    !string.IsNullOrWhiteSpace(
                        secondary));
            }

            Color accent =
                activityManager.ResultSuccess
                    ? new Color(0.20f, 1f, 0.58f, 1f)
                    : new Color(1f, 0.38f, 0.22f, 1f);

            if (resultActivityIcon != null)
            {
                resultActivityIcon.sprite =
                    MotorCityIconLibrary.ForActivity(
                        activityManager.ResultActivityId,
                        activityManager.ResultSuccess);

                resultActivityIcon.enabled =
                    resultActivityIcon.sprite != null;

                resultActivityIcon.color =
                    accent;
            }

            resultHeadlineText.color = accent;

            bool rookieDeliveryResult =
                activityManager.ResultIsRookieDelivery;

            if (resultNextGoalText != null)
            {
                string nextGoal =
                    activityManager.ResultNextGoal ??
                    string.Empty;

                if (string.IsNullOrWhiteSpace(
                        nextGoal) &&
                    rookieDeliveryResult)
                {
                    nextGoal =
                        MotorCityLocalization.Text(
                            "onboarding.result_next_garage");
                }

                resultNextGoalText.text =
                    nextGoal;

                resultNextGoalText.gameObject.SetActive(
                    !string.IsNullOrWhiteSpace(
                        nextGoal));
            }

            if (resultControlsText != null)
            {
                resultControlsText.gameObject.SetActive(
                    false);

                resultControlsText.text =
                    string.Empty;
            }

            bool replayable =
                !rookieDeliveryResult &&
                IsReplayableResult(
                    activityManager.ResultActivityId);

            if (resultRetryButton != null)
            {
                resultRetryButton.SetActive(
                    replayable);
            }

            if (resultContinueButton != null)
            {
                RectTransform continueRect =
                    resultContinueButton.GetComponent<
                        RectTransform>();

                if (continueRect != null)
                {
                    continueRect.anchoredPosition =
                        replayable
                            ? new Vector2(
                                120f,
                                -270f)
                            : new Vector2(
                                0f,
                                -270f);

                    continueRect.sizeDelta =
                        replayable
                            ? new Vector2(
                                230f,
                                50f)
                            : new Vector2(
                                310f,
                                50f);
                }
            }

            if (resultRetryTouchButton != null)
            {
                resultRetryTouchButton.SetActive(
                    replayable);
            }

            if (resultContinueTouchButton != null)
            {
                RectTransform continueRect =
                    resultContinueTouchButton.GetComponent<RectTransform>();

                if (continueRect != null)
                {
                    continueRect.anchoredPosition =
                        replayable
                            ? new Vector2(
                                78f,
                                4f)
                            : new Vector2(
                                0f,
                                4f);

                    continueRect.sizeDelta =
                        replayable
                            ? new Vector2(
                                220f,
                                52f)
                            : new Vector2(
                                300f,
                                52f);
                }
            }

            bool hasReward =
                activityManager.ResultRewardCredits > 0 ||
                activityManager.ResultReputationReward > 0;

            resultRewardText.color =
                hasReward
                    ? new Color(
                        1f,
                        0.78f,
                        0.20f,
                        1f)
                    : SecondaryTextColor;

            if (resultRewardIcon != null)
            {
                resultRewardIcon.enabled =
                    hasReward &&
                    resultRewardIcon.sprite != null;

                resultRewardIcon.color =
                    resultRewardText.color;
            }

            UpdateNewRecordPresentation();
        }

        private static void AlignResultIconToText(
            Image icon,
            Text text,
            float gap)
        {
            if (icon == null ||
                text == null)
            {
                return;
            }

            RectTransform iconRect =
                icon.rectTransform;

            RectTransform textRect =
                text.rectTransform;

            float textWidth =
                Mathf.Min(
                    textRect.rect.width,
                    Mathf.Max(
                        0f,
                        text.preferredWidth));

            float iconHalfWidth =
                iconRect.rect.width *
                0.5f;

            iconRect.anchoredPosition =
                new Vector2(
                    textRect.anchoredPosition.x -
                    textWidth * 0.5f -
                    gap -
                    iconHalfWidth,
                    textRect.anchoredPosition.y);
        }

        private void UpdateNewRecordPresentation()
        {
            bool newRecord =
                activityManager != null &&
                activityManager.ResultIsNewRecord;

            if (!newRecord)
            {
                if (resultRecordGlow != null)
                {
                    Color glow =
                        resultRecordGlow.color;

                    glow.a =
                        0f;

                    resultRecordGlow.color =
                        glow;
                }

                if (resultHeadlineText != null)
                {
                    resultHeadlineText.rectTransform.localScale =
                        Vector3.one;
                }

                resultRecordAnimationTimer =
                    0f;

                return;
            }

            if (resultRecordAnimatedSequence !=
                activityManager.ResultSequence)
            {
                resultRecordAnimatedSequence =
                    activityManager.ResultSequence;

                resultRecordAnimationTimer =
                    0f;

                MotorCitySfxRuntime.PlayNewRecord();
            }
            else
            {
                resultRecordAnimationTimer +=
                    Time.unscaledDeltaTime;
            }

            float t =
                resultRecordAnimationTimer;

            float pulse =
                1f +
                Mathf.Sin(
                    Mathf.Min(
                        1f,
                        t / 0.65f) *
                    Mathf.PI) *
                0.08f;

            if (resultHeadlineText != null)
            {
                resultHeadlineText.rectTransform.localScale =
                    Vector3.one *
                    pulse;

                resultHeadlineText.color =
                    new Color(
                        0.42f,
                        0.72f,
                        1f,
                        1f);
            }

            if (resultRecordGlow != null)
            {
                Color glow =
                    resultRecordGlow.color;

                glow.a =
                    Mathf.Clamp01(
                        0.28f *
                        (1f -
                         Mathf.Max(
                             0f,
                             t - 0.55f) /
                         0.65f));

                resultRecordGlow.color =
                    glow;
            }
        }

        private static bool IsReplayableResult(
            string activityId)
        {
            return
                activityId == "delivery" ||
                activityId == "drift" ||
                activityId == "sprint" ||
                activityId == "circuit" ||
                activityId == "profession_carwash" ||
                activityId == "profession_tow" ||
                activityId == "profession_pizza" ||
                activityId == "profession_taxi" ||
                activityId == "profession_mail" ||
                activityId == "profession_icecream";
        }

        private void HandleActivityResultInput()
        {
            if (activityManager == null ||
                !activityManager.HasResult)
                return;

            if (MotorCityInput.ResultContinuePressed)
            {
                activityManager.DismissResult();

                car?.SetDrivingBlocked(
                    "ActivityResult",
                    false);

                RefreshDrivingEnabledForUi();
                return;
            }

            if (activityManager.ResultIsRookieDelivery)
                return;

            bool restart =
                MotorCityInput.RetryPressed;

            if (!restart)
                return;

            switch (activityManager.ResultActivityId)
            {
                case "delivery":
                    delivery?.RestartFromResult();
                    break;

                case "drift":
                    driftChallenge?.RestartFromResult();
                    break;

                case "sprint":
                    streetSprint?.RestartFromResult();
                    break;

                case "circuit":
                    circuitRace?.RestartFromResult();
                    break;

                case "profession_carwash":
                    carWash?.RestartFromResult();
                    break;

                case "profession_tow":
                    towTruck?.RestartFromResult();
                    break;

                case "profession_pizza":
                case "profession_taxi":
                case "profession_mail":
                case "profession_icecream":
                    professions?.RestartFromResult();
                    break;
            }
        }

    }
}
