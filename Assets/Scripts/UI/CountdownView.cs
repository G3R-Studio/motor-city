using MotorCity.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private GameObject countdownOverlay;
        private CanvasGroup countdownOverlayGroup;
        private RectTransform countdownTextRect;
        private Text countdownText;
        private ActivityStartFlow countdownFlow;
        private int lastCountdownVisualValue = -2;
        private float countdownVisualTimer;

        private void BuildCountdownOverlay(
            Transform canvas)
        {
            countdownFlow =
                activityManager == null
                    ? null
                    : activityManager.StartFlow;

            countdownOverlay =
                new GameObject(
                    "Activity Countdown Overlay",
                    typeof(RectTransform),
                    typeof(CanvasGroup));

            countdownOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                countdownOverlay.GetComponent<RectTransform>();

            root.anchorMin =
                Vector2.zero;

            root.anchorMax =
                Vector2.one;

            root.offsetMin =
                Vector2.zero;

            root.offsetMax =
                Vector2.zero;

            countdownOverlayGroup =
                countdownOverlay.GetComponent<CanvasGroup>();

            countdownOverlayGroup.alpha =
                0f;

            countdownOverlayGroup.interactable =
                false;

            countdownOverlayGroup.blocksRaycasts =
                false;

            countdownText =
                CreateText(
                    root,
                    "Countdown Value",
                    96,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    new Vector2(
                        360f,
                        180f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    TextColor);

            countdownTextRect =
                countdownText.rectTransform;

            Outline outline =
                countdownText.gameObject.AddComponent<Outline>();

            outline.effectColor =
                new Color(
                    0.10f,
                    0.35f,
                    1f,
                    0.72f);

            outline.effectDistance =
                new Vector2(
                    3f,
                    -3f);

            countdownOverlay.SetActive(
                false);
        }

        private void UpdateCountdownOverlay()
        {
            if (countdownOverlay == null)
                return;

            if (countdownFlow == null)
            {
                countdownFlow =
                    activityManager == null
                        ? null
                        : activityManager.StartFlow;
            }

            bool visible =
                countdownFlow != null &&
                countdownFlow.IsCountdownPresentationActive;

            if (!visible)
            {
                if (countdownOverlay.activeSelf)
                {
                    countdownOverlay.SetActive(
                        false);
                }

                lastCountdownVisualValue =
                    -2;

                countdownVisualTimer =
                    0f;

                return;
            }

            if (!countdownOverlay.activeSelf)
            {
                countdownOverlay.SetActive(
                    true);
            }

            int value =
                countdownFlow.CountdownDisplayValue;

            if (value !=
                lastCountdownVisualValue)
            {
                lastCountdownVisualValue =
                    value;

                countdownVisualTimer =
                    0f;

                countdownText.text =
                    value <= 0
                        ? "GO"
                        : value.ToString();
            }
            else
            {
                countdownVisualTimer +=
                    Time.unscaledDeltaTime;
            }

            float t =
                countdownVisualTimer;

            float scale;

            if (t < 0.12f)
            {
                scale =
                    Mathf.Lerp(
                        0.80f,
                        1.10f,
                        t / 0.12f);
            }
            else if (t < 0.28f)
            {
                scale =
                    Mathf.Lerp(
                        1.10f,
                        1.00f,
                        (t - 0.12f) /
                        0.16f);
            }
            else
            {
                scale =
                    1f;
            }

            countdownTextRect.localScale =
                Vector3.one *
                scale;

            float fade =
                value <= 0
                    ? 1f
                    : Mathf.Clamp01(
                        1f -
                        Mathf.Max(
                            0f,
                            t - 0.56f) /
                        0.36f);

            countdownOverlayGroup.alpha =
                fade;

            countdownText.color =
                value <= 0
                    ? BlueAccent
                    : TextColor;
        }
    }
}
