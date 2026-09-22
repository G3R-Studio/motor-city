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
            ApplyPanelTreatment("Speedometer", new Color(0.010f, 0.017f, 0.029f, 0.58f), false);
            ApplyPanelTreatment("Minimap", new Color(0.010f, 0.017f, 0.027f, 0.44f), false);

            PolishCoreText("Credits", 1.0f);
            PolishCoreText("Reputation", 0.65f);
            PolishCoreText("Active Objective", 0.75f);
            PolishCoreText("Character Name", 0.75f);
            PolishCoreText("Character Line", 0.70f);
            PolishCoreText("Status Text", 0.70f);
            PolishCoreText("Speed", 0.90f);
            PolishCoreText("Minimap Target Label", 0.70f);

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
                SetRect(playerCard, new Vector2(14f, -14f), new Vector2(340f, 116f), 0.94f);
                SetRect(characterCard, new Vector2(14f, -138f), new Vector2(340f, 78f), 0.94f);
                SetRect(speedometer, new Vector2(0f, 6f), new Vector2(226f, 166f), 0.90f);
                SetRect(status, new Vector2(0f, 182f), new Vector2(520f, 46f), 0.94f);
                SetRect(minimap, new Vector2(-14f, -14f), new Vector2(202f, 202f), 0.92f);
            }
            else
            {
                SetRect(playerCard, new Vector2(22f, -22f), new Vector2(392f, 132f), 1f);
                SetRect(characterCard, new Vector2(22f, -162f), new Vector2(392f, 88f), 1f);
                SetRect(speedometer, new Vector2(0f, 16f), new Vector2(258f, 190f), 1f);
                SetRect(status, new Vector2(0f, 218f), new Vector2(620f, 50f), 1f);
                SetRect(minimap, new Vector2(-22f, -22f), new Vector2(232f, 232f), 1f);
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
            rect.localScale = new Vector3(scale, scale, 1f);
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
