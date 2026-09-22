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
            ApplyPanelTreatment("Player Card", new Color(0.016f, 0.026f, 0.043f, 0.96f), true);
            ApplyPanelTreatment("Character Card", new Color(0.020f, 0.034f, 0.052f, 0.94f), true);
            ApplyPanelTreatment("Activity Status", new Color(0.020f, 0.030f, 0.046f, 0.92f), false);
            ClearPanelBackdrop("Speedometer");
            ClearPanelBackdrop("Minimap");
            ApplyPanelTreatment("Drift HUD", new Color(0.080f, 0.038f, 0.018f, 0.92f), true);
            ApplyPanelTreatment("Activity Result", new Color(0.014f, 0.023f, 0.038f, 0.985f), true);
            ApplyPanelTreatment("Navigator Menu", new Color(0.014f, 0.023f, 0.038f, 0.985f), true);
            ApplyPanelTreatment("Club Panel", new Color(0.014f, 0.023f, 0.038f, 0.985f), true);
            ApplyPanelTreatment("Garage Panel", new Color(0.014f, 0.022f, 0.036f, 0.985f), true);

            PolishCoreText("Credits", 1.0f);
            PolishCoreText("Reputation", 0.65f);
            PolishCoreText("Active Objective", 0.75f);
            PolishCoreText("Character Name", 0.75f);
            PolishCoreText("Character Line", 0.70f);
            PolishCoreText("Status Text", 0.70f);
            PolishCoreText("Speed", 0.90f);
            PolishCoreText("Minimap Target Label", 0.70f);
            PolishCoreText("Drift Score", 0.85f);
            PolishCoreText("Result Title", 0.55f);
            PolishCoreText("Result Headline", 0.90f);
            PolishCoreText("Result Details", 0.65f);
            PolishCoreText("Result Reward", 0.90f);
            PolishCoreText("Result Controls", 0.55f);
            PolishCoreText("Navigator Title", 0.80f);
            PolishCoreText("Navigator Selection", 0.80f);
            PolishCoreText("Navigator Controls", 0.55f);
            PolishCoreText("Club Title", 0.80f);
            PolishCoreText("Club Name", 0.80f);
            PolishCoreText("Club Description", 0.55f);
            PolishCoreText("Club Weekly", 0.75f);
            PolishCoreText("Garage Title", 0.80f);
            PolishCoreText("Garage Credits", 0.80f);
            PolishCoreText("Garage Vehicle", 0.75f);

            ApplyModalComposition(lastTouchLayout);
            PolishTouchButtons();

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
                        lastTouchLayout ? 486f : 590f,
                        34f);
            }

            RectTransform minimapTarget = FindRect("Minimap Target Label");
            if (minimapTarget != null && !lastTouchLayout)
            {
                minimapTarget.sizeDelta =
                    new Vector2(176f, minimapTarget.sizeDelta.y);
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
                SetRect(characterCard, new Vector2(14f, -14f), new Vector2(390f, 104f), 1f);
                SetRect(speedometer, new Vector2(0f, 6f), new Vector2(226f, 166f), 0.90f);
                SetRect(status, new Vector2(0f, 182f), new Vector2(520f, 46f), 0.94f);
                SetRect(minimap, new Vector2(-14f, -14f), new Vector2(202f, 202f), 0.92f);
            }
            else
            {
                SetRect(playerCard, new Vector2(22f, -22f), new Vector2(392f, 132f), 1f);
                SetRect(characterCard, new Vector2(22f, -22f), new Vector2(430f, 112f), 1f);
                SetRect(speedometer, new Vector2(0f, 16f), new Vector2(258f, 190f), 1f);
                SetRect(status, new Vector2(0f, 218f), new Vector2(620f, 50f), 1f);
                SetRect(minimap, new Vector2(-22f, -22f), new Vector2(232f, 232f), 1f);
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
                    new Vector2(480f, 232f),
                    0.94f);

                SetRect(
                    club,
                    Vector2.zero,
                    new Vector2(520f, 334f),
                    0.94f);

                SetRect(
                    garage,
                    Vector2.zero,
                    new Vector2(720f, 544f),
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
                    new Vector2(540f, 270f),
                    1f);

                SetRect(
                    club,
                    Vector2.zero,
                    new Vector2(580f, 380f),
                    1f);

                SetRect(
                    garage,
                    Vector2.zero,
                    new Vector2(780f, 594f),
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
                resultReward.sizeDelta =
                    new Vector2(
                        touchLayout ? 470f : 530f,
                        46f);
            }
        }

        private void PolishTouchButtons()
        {
            if (!MotorCityInput.PreferTouchPrompts)
                return;

            PolishTouchGroup(
                "Touch Driving Controls",
                new Color(
                    0.025f,
                    0.075f,
                    0.12f,
                    0.82f));

            PolishTouchGroup(
                "Touch Utility Controls",
                new Color(
                    0.025f,
                    0.075f,
                    0.12f,
                    0.90f));

            PolishTouchGroup(
                "Result Touch Controls",
                new Color(
                    0.025f,
                    0.075f,
                    0.12f,
                    0.94f));

            PolishTouchGroup(
                "Navigator Touch Controls",
                new Color(
                    0.025f,
                    0.075f,
                    0.12f,
                    0.94f));

            PolishTouchGroup(
                "Store Touch Controls",
                new Color(
                    0.025f,
                    0.075f,
                    0.12f,
                    0.94f));

            PolishTouchGroup(
                "Club Touch Controls",
                new Color(
                    0.025f,
                    0.075f,
                    0.12f,
                    0.94f));
        }

        private void PolishTouchGroup(
            string rootName,
            Color baseColor)
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
                    baseColor;

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

            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

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
            if (!MotorCityInput.PreferTouchPrompts)
                return false;

            if (Screen.height <= 0)
                return true;

            float aspect =
                Screen.width / (float)Screen.height;

            return aspect >= 1.25f;
        }
    }
}
