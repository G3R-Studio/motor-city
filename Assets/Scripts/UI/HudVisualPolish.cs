using System.Collections.Generic;
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
        private readonly Dictionary<string, RectTransform> rectCache = new();
        private int lastScreenWidth;
        private int lastScreenHeight;
        private bool lastTouchLayout;
        private float bindRetryTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindAnyObjectByType<HudVisualPolish>() != null)
                return;

            GameObject host = new(
                "Motor City HUD Visual Polish",
                typeof(HudVisualPolish));

            Object.DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            HudVisualPolish[] instances =
                Object.FindObjectsByType<HudVisualPolish>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (HudVisualPolish instance in instances)
            {
                if (instance != this && instance.GetEntityId() < GetEntityId())
                {
                    Object.Destroy(gameObject);
                    return;
                }
            }
        }

        private void Update()
        {
            if (hudRoot == null)
            {
                bindRetryTimer -= Time.unscaledDeltaTime;
                if (bindRetryTimer <= 0f)
                {
                    bindRetryTimer = 0.25f;
                    TryBind();
                }
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

            rectCache.Clear();
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

            // NavigatorView owns the Minimap and Navigation Target Strip geometry.
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
                        lastTouchLayout ? 480f : 560f,
                        lastTouchLayout ? 38f : 42f);

                Text label =
                    statusText.GetComponent<Text>();

                if (label != null)
                {
                    label.resizeTextForBestFit =
                        false;
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

            // NavigatorView creates the target label at its final size and
            // position. Avoid rewriting its geometry in a second owner.
            // TouchControlsView owns utility rail and secondary action positions.
            // Their builder already applies the same coordinates; do not
            // overwrite these controls from the persistent visual polish.
            // Apply bounds after responsive composition has sized the cells.
            foreach (Text label in hudRoot.GetComponentsInChildren<Text>(true))
                MotorCityTextLayout.Configure(label);
        }

        private void ApplyDesktopOrTouchComposition(bool touchLayout)
        {
            RectTransform playerCard = FindRect("Player Card");
            RectTransform characterCard = FindRect("Character Card");

            if (touchLayout)
            {
                SetRect(playerCard, new Vector2(14f, -14f), new Vector2(340f, 116f), 1f);
                SetRect(characterCard, new Vector2(14f, -14f), new Vector2(430f, 154f), 1f);
            }
            else
            {
                SetRect(playerCard, new Vector2(22f, -22f), new Vector2(392f, 132f), 1f);
                SetRect(characterCard, new Vector2(22f, -22f), new Vector2(448f, 154f), 1f);
            }
        }

        private void ApplyModalComposition(
            bool touchLayout)
        {
            // ActivityResultView, NavigatorView and ClubView own the modal
            // panel geometry. Their builders use the previously polished
            // desktop sizes, so the pass only styles modal contents.

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
                        -206f);

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
            NormalizeTexturedButtonColors();

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

        private void NormalizeTexturedButtonColors()
        {
            Button[] buttons =
                GetComponentsInChildren<Button>(
                    true);

            foreach (Button button in
                     buttons)
            {
                if (button == null)
                    continue;

                Transform current =
                    button.transform;

                bool drivingControl =
                    false;

                while (current != null)
                {
                    if (current.name ==
                        "Touch Driving Controls")
                    {
                        drivingControl =
                            true;

                        break;
                    }

                    current =
                        current.parent;
                }

                if (drivingControl)
                    continue;

                Image image =
                    button.targetGraphic as Image;

                if (image == null ||
                    image.sprite == null)
                {
                    continue;
                }

                image.color =
                    Color.white;
            }
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

                RemoveOutline(
                    image.gameObject);
            }

            Text[] labels =
                root.GetComponentsInChildren<Text>(
                    true);

            foreach (Text label in labels)
            {
                if (label == null)
                    continue;

                Shadow shadow =
                    FindPlainShadow(
                        label.gameObject);

                if (shadow == null)
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

            Button button = target.GetComponent<Button>();
            if (button != null && button.targetGraphic is GarageReferenceGraphic)
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

            text.resizeTextForBestFit = false;
            text.resizeTextMinSize =
                text.fontSize <= 11
                    ? Mathf.Max(
                        9,
                        text.fontSize - 2)
                    : Mathf.Max(
                        11,
                        Mathf.RoundToInt(
                            text.fontSize * 0.78f));
            text.resizeTextMinSize = Mathf.Max(15, text.resizeTextMinSize);
            text.resizeTextMaxSize =
                text.fontSize;
            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            text.verticalOverflow =
                VerticalWrapMode.Overflow;
            text.alignByGeometry = false;
            text.lineSpacing =
                1f;

            rect.localScale =
                Vector3.one;

            Shadow shadow =
                FindPlainShadow(
                    rect.gameObject);

            if (shadow == null)
                shadow = rect.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0f, 0f, 0f, shadowAlpha);
            shadow.effectDistance = new Vector2(1f, -2f);
            shadow.useGraphicAlpha = true;
        }

        private static Shadow FindPlainShadow(
            GameObject target)
        {
            if (target == null)
                return null;

            Shadow[] shadows =
                target.GetComponents<Shadow>();

            for (int i = 0;
                 i < shadows.Length;
                 i++)
            {
                Shadow shadow =
                    shadows[i];

                if (shadow != null &&
                    shadow is not Outline)
                {
                    return shadow;
                }
            }

            return null;
        }

        private RectTransform FindRect(string objectName)
        {
            if (hudRoot == null)
                return null;

            // Layout builders may replace individual HUD children while the
            // persistent polish host remains alive. Never modify a detached
            // RectTransform left behind in the name cache.
            if (rectCache.TryGetValue(objectName, out RectTransform cached))
            {
                if (cached != null && cached.IsChildOf(hudRoot))
                    return cached;

                rectCache.Remove(objectName);
            }

            RectTransform rect = FindRecursive(hudRoot, objectName) as RectTransform;
            if (rect != null)
                rectCache[objectName] = rect;
            return rect;
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
