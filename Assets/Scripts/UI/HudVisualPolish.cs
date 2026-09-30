using System.Collections;
using MotorCity.Input;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    /// <summary>
    /// Non-invasive visual pass for the runtime-built PrototypeHud.
    /// Keeps gameplay/UI logic in PrototypeHud untouched and only refines
    /// composition, spacing, contrast and responsive sizing.
    /// </summary>
    public sealed class HudVisualPolish : MonoBehaviour
    {
        private const string HudRootName = "Motor City HUD";

        private Transform hudRoot;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private bool lastTouchLayout;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            GameObject host = new(
                "Motor City HUD Visual Polish",
                typeof(HudVisualPolish));

            Object.DontDestroyOnLoad(host);
        }

        private IEnumerator Start()
        {
            while (hudRoot == null)
            {
                TryBind();
                yield return new WaitForSecondsRealtime(0.25f);
            }

            ApplyVisualPass();
        }

        private void Update()
        {
            if (hudRoot == null)
            {
                TryBind();
                return;
            }

            bool touchLayout = UseLandscapeTouchLayout();

            if (lastScreenWidth != Screen.width ||
                lastScreenHeight != Screen.height ||
                lastTouchLayout != touchLayout)
            {
                ApplyVisualPass();
            }
        }

        private void TryBind()
        {
            GameObject root = GameObject.Find(HudRootName);

            if (root == null)
                return;

            hudRoot = root.transform;
            ApplyVisualPass();
        }

        private void ApplyVisualPass()
        {
            if (hudRoot == null)
                return;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastTouchLayout = UseLandscapeTouchLayout();

            ApplyDesktopOrTouchComposition(lastTouchLayout);
            ClearPanelBackdrop("Player Card");
            ClearPanelBackdrop("Character Card");
            ClearPanelBackdrop("Activity Status");
            ClearPanelBackdrop("Speedometer");
            ClearPanelBackdrop("Minimap");
            ClearPanelBackdrop("Drift HUD");
            ClearPanelBackdrop("Activity Result");
            ClearPanelBackdrop("Navigator Menu");
            ClearPanelBackdrop("Pause Panel");
            ClearPanelBackdrop("Club Panel");
            ClearPanelBackdrop("Garage Panel");

            PolishCoreText("Credits", 1.0f);
            PolishCoreText("Reputation", 0.65f);
            PolishCoreText("Active Objective", 0.75f);
            PolishCoreText("Character Source", 0.55f);
            PolishCoreText("Character Name", 0.75f);
            PolishCoreText("Character Mission Title", 0.60f);
            PolishCoreText("Character Line", 0.70f);

            RectTransform characterLine =
                FindRect("Character Line");

            if (characterLine != null)
            {
                Text characterBody =
                    characterLine.GetComponent<Text>();

                if (characterBody != null)
                {
                    characterBody.resizeTextMinSize =
                        12;
                }
            }
            PolishCoreText("Character Reward", 0.70f);
            PolishCoreText("Status Text", 0.88f);
            PolishCoreText("Speed", 0.90f);
            PolishCoreText("Minimap Target Label", 0.86f);
            PolishCoreText("Drift Score", 0.85f);
            PolishCoreText("Result Title", 0.55f);
            PolishCoreText("Result Headline", 0.90f);
            PolishCoreText("Result Details", 0.65f);
            PolishCoreText("Result Reward", 0.90f);
            PolishCoreText("Result Controls", 0.55f);
            PolishCoreText("Navigator Title", 0.80f);
            PolishCoreText("Navigator Index", 0.50f);
            PolishCoreText("Navigator Category", 0.50f);
            PolishCoreText("Navigator Selection", 0.80f);
            PolishCoreText("Navigator Description", 0.50f);
            PolishCoreText("Navigator Distance", 0.50f);
            PolishCoreText("Club Title", 0.80f);
            PolishCoreText("Club Name", 0.80f);
            PolishCoreText("Club Description", 0.55f);
            PolishCoreText("Club Weekly", 0.75f);
            PolishCoreText("Garage Title", 0.80f);
            PolishCoreText("Garage Credits", 0.80f);
            PolishCoreText("Garage Reputation", 0.75f);
            PolishCoreText("Garage Vehicle", 0.75f);
            PolishCoreText("Garage Next Vehicle", 0.70f);
            PolishCoreText("Garage Vehicle Stats", 0.60f);

            ApplyModalComposition(lastTouchLayout);
            PolishTouchButtons();
            RestoreDrivingControlAppearance();

            RectTransform objective = FindRect("Active Objective");
            if (objective != null && !lastTouchLayout)
            {
                objective.sizeDelta =
                    new Vector2(294f, objective.sizeDelta.y);
            }

            RectTransform statusText = FindRect("Status Text");
            if (statusText != null)
            {
                statusText.sizeDelta =
                    new Vector2(
                        lastTouchLayout ? 482f : 560f,
                        lastTouchLayout ? 38f : 42f);

                Text label =
                    statusText.GetComponent<Text>();

                if (label != null)
                {
                    label.resizeTextForBestFit =
                        true;
                    label.resizeTextMinSize =
                        lastTouchLayout
                            ? 12
                            : 13;
                    label.resizeTextMaxSize =
                        label.fontSize;
                    label.alignment =
                        TextAnchor.MiddleLeft;
                }
            }

            RectTransform minimapTarget = FindRect("Minimap Target Label");
            if (minimapTarget != null)
            {
                // The target strip itself is 148 px wide. Keep the label
                // safely inside it so long objectives/distances can use Best
                // Fit without painting underneath the navigator button.
                minimapTarget.sizeDelta =
                    new Vector2(
                        lastTouchLayout ? 124f : 132f,
                        lastTouchLayout ? 25f : 27f);

                minimapTarget.anchoredPosition =
                    Vector2.zero;
            }
        }

        private void ApplyDesktopOrTouchComposition(bool touchLayout)
        {
            RectTransform playerCard = FindRect("Player Card");
            RectTransform characterCard = FindRect("Character Card");
            RectTransform speedometer = FindRect("Speedometer");
            RectTransform status = FindRect("Activity Status");
            RectTransform minimap = FindRect("Minimap");

            if (touchLayout)
            {
                SetRect(playerCard, new Vector2(14f, -14f), new Vector2(340f, 116f), 1f);
                SetRect(characterCard, new Vector2(14f, -14f), new Vector2(430f, 154f), 1f);
                SetRect(speedometer, new Vector2(0f, 6f), new Vector2(226f, 166f), 0.90f);
                SetRect(status, new Vector2(110f, -174f), new Vector2(560f, 54f), 1f);
                SetRect(minimap, new Vector2(-14f, -14f), new Vector2(202f, 218f), 0.92f);
            }
            else
            {
                SetRect(playerCard, new Vector2(22f, -22f), new Vector2(392f, 132f), 1f);
                SetRect(characterCard, new Vector2(22f, -22f), new Vector2(448f, 154f), 1f);
                SetRect(speedometer, new Vector2(0f, 16f), new Vector2(258f, 190f), 1f);
                SetRect(status, new Vector2(150f, -196f), new Vector2(640f, 58f), 1f);
                SetRect(minimap, new Vector2(-22f, -22f), new Vector2(214f, 218f), 1f);
            }
        }

        private void ApplyModalComposition(
            bool touchLayout)
        {
            RectTransform result =
                FindRect("Activity Result");

            RectTransform navigator =
                FindRect("Navigator Menu");

            RectTransform club =
                FindRect("Club Panel");

            RectTransform garage =
                FindRect("Garage Panel");

            if (touchLayout)
            {
                SetRect(
                    result,
                    Vector2.zero,
                    new Vector2(560f, 302f),
                    0.94f);

                SetRect(
                    navigator,
                    Vector2.zero,
                    new Vector2(520f, 292f),
                    0.94f);

                SetRect(
                    club,
                    Vector2.zero,
                    new Vector2(520f, 334f),
                    0.94f);

                SetRect(
                    garage,
                    Vector2.zero,
                    new Vector2(720f, 500f),
                    0.92f);
            }
            else
            {
                SetRect(
                    result,
                    Vector2.zero,
                    new Vector2(650f, 350f),
                    1f);

                SetRect(
                    navigator,
                    Vector2.zero,
                    new Vector2(580f, 320f),
                    1f);

                SetRect(
                    club,
                    Vector2.zero,
                    new Vector2(580f, 380f),
                    1f);

                SetRect(
                    garage,
                    Vector2.zero,
                    new Vector2(780f, 520f),
                    1f);
            }

            RectTransform resultDetails =
                FindRect("Result Details");

            if (resultDetails != null)
            {
                resultDetails.sizeDelta =
                    new Vector2(
                        touchLayout ? 500f : 570f,
                        58f);
            }

            RectTransform resultReward =
                FindRect("Result Reward");

            if (resultReward != null)
            {
                resultReward.anchoredPosition =
                    new Vector2(
                        touchLayout ? 18f : 20f,
                        -198f);

                resultReward.sizeDelta =
                    new Vector2(
                        touchLayout ? 360f : 400f,
                        46f);

                Text rewardLabel =
                    resultReward.GetComponent<Text>();

                if (rewardLabel != null)
                {
                    rewardLabel.alignment =
                        TextAnchor.MiddleCenter;
                }
            }

            RectTransform resultRewardIcon =
                FindRect("Result Reward Icon");

            if (resultRewardIcon != null)
            {
                resultRewardIcon.anchoredPosition =
                    new Vector2(
                        touchLayout ? -166f : -184f,
                        -198f);
            }
        }

        private void PolishTouchButtons()
        {
            if (!MotorCityInput.PreferTouchPrompts)
                return;

            PolishTouchGroup(
                "Touch Utility Controls");

            PolishTouchGroup(
                "Result Touch Controls");

            PolishTouchGroup(
                "Navigator Touch Controls");

            PolishTouchGroup(
                "Store Touch Controls");

            PolishTouchGroup(
                "Club Touch Controls");
        }

        private void PolishTouchGroup(
            string rootName)
        {
            RectTransform root =
                FindRect(rootName);

            if (root == null)
                return;

            Image[] images =
                root.GetComponentsInChildren<Image>(
                    true);

            foreach (Image image in images)
            {
                if (image == null ||
                    !image.raycastTarget)
                {
                    continue;
                }

                image.color =
                    Color.white;

                Outline outline =
                    image.GetComponent<Outline>();

                if (outline == null)
                {
                    outline =
                        image.gameObject
                            .AddComponent<Outline>();
                }

                outline.effectColor =
                    new Color(
                        0.18f,
                        0.62f,
                        1f,
                        0.30f);

                outline.effectDistance =
                    new Vector2(1f, -1f);

                outline.useGraphicAlpha =
                    true;
            }

            Text[] labels =
                root.GetComponentsInChildren<Text>(
                    true);

            foreach (Text label in labels)
            {
                if (label == null)
                    continue;

                Shadow shadow =
                    label.GetComponent<Shadow>();

                if (shadow == null ||
                    shadow is Outline)
                {
                    shadow =
                        label.gameObject
                            .AddComponent<Shadow>();
                }

                shadow.effectColor =
                    new Color(
                        0f,
                        0f,
                        0f,
                        0.85f);

                shadow.effectDistance =
                    new Vector2(1f, -2f);
            }
        }

        private void RestoreDrivingControlAppearance()
        {
            RectTransform root =
                FindRect(
                    "Touch Driving Controls");

            if (root == null)
                return;

            SetDrivingControlImage(
                root,
                "Throttle",
                new Color32(255, 255, 255, 0x64));

            SetDrivingControlImage(
                root,
                "Reverse",
                new Color32(255, 255, 255, 0x64));

            SetDrivingControlImage(
                root,
                "Handbrake",
                new Color32(255, 255, 255, 165));

            SetDrivingControlImage(
                root,
                "Steering Wheel",
                new Color32(
                    255,
                    255,
                    255,
                    0x96));

            SetDrivingControlImage(
                root,
                "Steer Left",
                new Color32(
                    255,
                    255,
                    255,
                    0x00));

            SetDrivingControlImage(
                root,
                "Steer Right",
                new Color32(
                    255,
                    255,
                    255,
                    0x00));

            SetDrivingControlImage(
                root,
                "Interact",
                new Color32(
                    255,
                    255,
                    255,
                    0xFF));

            Transform arrowSteering =
                FindRecursive(
                    root,
                    "Arrow Steering");

            if (arrowSteering != null)
            {
                Image[] arrowImages =
                    arrowSteering.GetComponentsInChildren<Image>(
                        true);

                foreach (Image image in arrowImages)
                {
                    if (image == null ||
                        image.gameObject.name !=
                        "Arrow Stroke")
                    {
                        continue;
                    }

                    image.color =
                        new Color32(
                            255,
                            255,
                            255,
                            0xFF);

                    RemoveOutline(
                        image.gameObject);
                }
            }
        }

        private static void SetDrivingControlImage(
            Transform root,
            string objectName,
            Color32 color)
        {
            Transform target =
                FindRecursive(
                    root,
                    objectName);

            if (target == null)
                return;

            Image image =
                target.GetComponent<Image>();

            if (image != null)
            {
                image.color =
                    color;
            }

            RemoveOutline(
                target.gameObject);
        }

        private static void RemoveOutline(
            GameObject target)
        {
            if (target == null)
                return;

            Outline[] outlines =
                target.GetComponents<Outline>();

            foreach (Outline outline in outlines)
            {
                if (outline == null)
                    continue;

                outline.enabled =
                    false;

                Object.Destroy(
                    outline);
            }
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 position,
            Vector2 size,
            float scale)
        {
            if (rect == null)
                return;

            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            // Keep UI text on an unscaled transform. Fractional scaling of
            // parent RectTransforms makes legacy Unity Text rasterize softer
            // after resolution/aspect changes, especially on small labels.
            rect.localScale = Vector3.one;
        }

        private void ClearPanelBackdrop(
            string objectName)
        {
            RectTransform rect =
                FindRect(objectName);

            if (rect == null)
                return;

            Image image =
                rect.GetComponent<Image>();

            if (image != null)
            {
                image.color =
                    Color.clear;

                image.raycastTarget =
                    false;
            }

            Outline outline =
                rect.GetComponent<Outline>();

            if (outline != null)
            {
                outline.enabled =
                    false;
            }

            Transform highlight =
                rect.Find(
                    "Visual Polish Highlight");

            if (highlight != null)
            {
                highlight.gameObject.SetActive(
                    false);
            }
        }

        private void ApplyPanelTreatment(
            string objectName,
            Color color,
            bool addTopHighlight)
        {
            RectTransform rect = FindRect(objectName);

            if (rect == null)
                return;

            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
                image.raycastTarget = false;
            }

            Outline outline = rect.GetComponent<Outline>();
            if (outline == null)
                outline = rect.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0.18f, 0.50f, 0.82f, 0.18f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            if (addTopHighlight)
                EnsureTopHighlight(rect);
        }

        private static void EnsureTopHighlight(RectTransform parent)
        {
            const string highlightName = "Visual Polish Highlight";

            Transform existing = parent.Find(highlightName);
            if (existing != null)
                return;

            GameObject lineObject = new(
                highlightName,
                typeof(RectTransform),
                typeof(Image));

            lineObject.transform.SetParent(parent, false);

            RectTransform line = lineObject.GetComponent<RectTransform>();
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = new Vector2(1f, 1f);
            line.pivot = new Vector2(0.5f, 1f);
            line.anchoredPosition = new Vector2(0f, -1f);
            line.sizeDelta = new Vector2(-20f, 2f);

            Image image = lineObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.62f, 1f, 0.32f);
            image.raycastTarget = false;
        }

        private void PolishCoreText(
            string objectName,
            float shadowAlpha)
        {
            RectTransform rect = FindRect(objectName);
            if (rect == null)
                return;

            Text text = rect.GetComponent<Text>();
            if (text == null)
                return;

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize =
                text.fontSize <= 11
                    ? Mathf.Max(
                        9,
                        text.fontSize - 2)
                    : Mathf.Max(
                        11,
                        Mathf.RoundToInt(
                            text.fontSize * 0.78f));
            text.resizeTextMaxSize =
                text.fontSize;
            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            text.verticalOverflow =
                VerticalWrapMode.Truncate;
            text.alignByGeometry =
                true;
            text.lineSpacing =
                1f;

            rect.localScale =
                Vector3.one;

            Shadow shadow = rect.GetComponent<Shadow>();
            if (shadow == null || shadow is Outline)
                shadow = rect.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0f, 0f, shadowAlpha);
            shadow.effectDistance = new Vector2(1f, -2f);
            shadow.useGraphicAlpha = true;
        }

        private RectTransform FindRect(string objectName)
        {
            if (hudRoot == null)
                return null;

            Transform target = FindRecursive(hudRoot, objectName);
            return target == null
                ? null
                : target as RectTransform;
        }

        private static Transform FindRecursive(
            Transform parent,
            string objectName)
        {
            if (parent.name == objectName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform found = FindRecursive(child, objectName);

                if (found != null)
                    return found;
            }

            return null;
        }

        private static bool UseLandscapeTouchLayout()
        {
            // The chosen driving input must never rearrange the main HUD.
            // Wheel/arrows are an input overlay only; the underlying HUD keeps
            // the exact same composition as keyboard mode on the same screen.
            return false;
        }
    }
}
