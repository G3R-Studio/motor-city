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
                    new Vector2(600f, 316f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Color.clear);

            ApplyModalPanelTexture(
                panel);

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
                    new Vector2(520f, 26f),
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
                    new Vector2(0f, -80f),
                    new Vector2(520f, 48f),
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
                    new Vector2(0f, -142f),
                    new Vector2(520f, 48f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    SecondaryTextColor);

            resultRewardIcon =
                CreateHudIcon(
                    panel,
                    "Result Reward Icon",
                    MotorCityIconLibrary.Reward,
                    new Vector2(
                        -176f,
                        -198f),
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
                    28,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(20f, -198f),
                    new Vector2(380f, 40f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    TextColor);

            resultControlsText =
                CreateText(
                    panel,
                    "Result Controls",
                    15,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(0f, 30f),
                    new Vector2(540f, 28f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    SecondaryTextColor);

            resultControlsText.text =
                MotorCityLocalization.Text("hud.result_controls");
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
                    new Vector2(-95f, 4f),
                    new Vector2(170f, 48f));

            CreateLocalizedTouchPulseButton(
                root,
                "Result Continue",
                "touch.result.continue",
                MotorCityInputAction.Cancel,
                new Vector2(95f, 4f),
                new Vector2(170f, 48f));

            resultTouchControlsRoot.SetActive(
                false);
        }

        private void UpdateActivityResult()
        {
            if (activityManager == null ||
                !activityManager.HasResult)
                return;

            resultTitleText.text =
                activityManager.ResultTitle ?? string.Empty;

            resultHeadlineText.text =
                activityManager.ResultHeadline ?? string.Empty;

            resultDetailsText.text =
                activityManager.ResultDetails ?? string.Empty;

            bool hasCredits =
                activityManager.ResultRewardCredits > 0;

            bool hasReputation =
                activityManager.ResultReputationReward > 0;

            resultRewardText.text =
                hasCredits || hasReputation
                    ? MotorCityLocalization.Format(
                        "hud.result_reward",
                        activityManager.ResultRewardCredits,
                        activityManager.ResultReputationReward)
                    : MotorCityLocalization.Text(
                        "hud.no_rewards");

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

            if (MotorCityInput.CancelPressed)
            {
                activityManager.DismissResult();
                car?.SetDrivingEnabled(true);
                return;
            }

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
