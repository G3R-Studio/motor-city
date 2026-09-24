using System.Collections;
using MotorCity.Input;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    /// <summary>
    /// Shared presentation layer for the runtime-built PrototypeHud.
    /// PrototypeHud owns gameplay state and element construction; this component
    /// only keeps the visual language and responsive composition consistent.
    /// </summary>
    public sealed class HudVisualPolish : MonoBehaviour
    {
        private const string HudRootName = "Motor City HUD";

        private static readonly Color Surface =
            new(0.030f, 0.042f, 0.066f, 0.94f);

        private static readonly Color SurfaceStrong =
            new(0.024f, 0.034f, 0.056f, 0.975f);

        private static readonly Color SurfaceSoft =
            new(0.050f, 0.064f, 0.094f, 0.92f);

        private static readonly Color Cyan =
            new(0.18f, 0.72f, 1f, 1f);

        private Transform hudRoot;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private bool lastTouchLayout;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            GameObject host =
                new(
                    "Motor City HUD Visual Polish",
                    typeof(HudVisualPolish));

            Object.DontDestroyOnLoad(
                host);
        }

        private IEnumerator Start()
        {
            while (hudRoot == null)
            {
                TryBind();
                yield return
                    new WaitForSecondsRealtime(
                        0.25f);
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

            bool touchLayout =
                UseLandscapeTouchLayout();

            if (lastScreenWidth != Screen.width ||
                lastScreenHeight != Screen.height ||
                lastTouchLayout != touchLayout)
            {
                ApplyVisualPass();
            }
        }

        private void TryBind()
        {
            GameObject root =
                GameObject.Find(
                    HudRootName);

            if (root == null)
                return;

            hudRoot =
                root.transform;

            ApplyVisualPass();
        }

        private void ApplyVisualPass()
        {
            if (hudRoot == null)
                return;

            lastScreenWidth =
                Screen.width;

            lastScreenHeight =
                Screen.height;

            lastTouchLayout =
                UseLandscapeTouchLayout();

            ApplyDrivingHudComposition(
                lastTouchLayout);

            // One surface family for all driving clusters. Information hierarchy
            // comes from spacing, icons and content accents, not decorative bars.
            ApplyPanelTreatment(
                "Character Card",
                SurfaceStrong);

            ApplyPanelTreatment(
                "Character Portrait Frame",
                SurfaceSoft);

            ApplyPanelTreatment(
                "Activity Status",
                Surface);

            ApplyPanelTreatment(
                "Drift HUD",
                Surface);

            ApplyPanelTreatment(
                "Speedometer",
                new Color(
                    Surface.r,
                    Surface.g,
                    Surface.b,
                    0.74f));

            ApplyPanelTreatment(
                "Minimap",
                Surface);

            ApplyPanelTreatment(
                "Navigation Target Strip",
                SurfaceStrong);

            ApplyPanelTreatment(
                "Speed Unit Plate",
                new Color(
                    Surface.r,
                    Surface.g,
                    Surface.b,
                    0.72f));

            ApplyPanelTreatment(
                "Activity Result",
                SurfaceStrong);

            ApplyPanelTreatment(
                "Navigator Menu",
                SurfaceStrong);

            ApplyPanelTreatment(
                "Club Panel",
                SurfaceStrong);

            ApplyPanelTreatment(
                "Garage Panel",
                SurfaceStrong);

            RemoveLegacyHighlights();

            PolishCoreText(
                "Character Source",
                0.45f);
            PolishCoreText(
                "Character Name",
                0.55f);
            PolishCoreText(
                "Character Mission Title",
                0.45f);
            PolishCoreText(
                "Character Line",
                0.55f);
            PolishCoreText(
                "Character Reward",
                0.45f);
            PolishCoreText(
                "Status Text",
                0.55f);
            PolishCoreText(
                "Speed",
                0.70f);
            PolishCoreText(
                "Drive Mode",
                0.55f);
            PolishCoreText(
                "Dial Unit",
                0.45f);
            PolishCoreText(
                "Minimap Target Label",
                0.55f);
            PolishCoreText(
                "Drift Score",
                0.70f);
            PolishCoreText(
                "Result Title",
                0.45f);
            PolishCoreText(
                "Result Headline",
                0.70f);
            PolishCoreText(
                "Result Details",
                0.55f);
            PolishCoreText(
                "Result Reward",
                0.70f);
            PolishCoreText(
                "Result Controls",
                0.45f);
            PolishCoreText(
                "Navigator Title",
                0.60f);
            PolishCoreText(
                "Navigator Selection",
                0.55f);
            PolishCoreText(
                "Navigator Controls",
                0.45f);
            PolishCoreText(
                "Club Title",
                0.60f);
            PolishCoreText(
                "Club Name",
                0.60f);
            PolishCoreText(
                "Club Description",
                0.45f);
            PolishCoreText(
                "Club Weekly",
                0.55f);
            PolishCoreText(
                "Garage Title",
                0.60f);
            PolishCoreText(
                "Garage Credits",
                0.55f);
            PolishCoreText(
                "Garage Reputation",
                0.55f);
            PolishCoreText(
                "Garage Vehicle",
                0.55f);
            PolishCoreText(
                "Garage Next Vehicle",
                0.50f);
            PolishCoreText(
                "Garage Vehicle Stats",
                0.45f);

            ApplyModalComposition(
                lastTouchLayout);

            PolishTouchButtons();
        }

        private void ApplyDrivingHudComposition(
            bool touchLayout)
        {
            if (touchLayout)
            {
                SetRect(
                    "Character Card",
                    new Vector2(14f, -14f),
                    new Vector2(316f, 90f));

                SetRect(
                    "Speedometer",
                    new Vector2(0f, 8f),
                    new Vector2(246f, 210f));

                SetRect(
                    "Activity Status",
                    new Vector2(0f, -14f),
                    new Vector2(392f, 38f));

                SetRect(
                    "Drift HUD",
                    new Vector2(0f, -58f),
                    new Vector2(236f, 38f));

                SetRect(
                    "Minimap",
                    new Vector2(-14f, -14f),
                    new Vector2(198f, 210f));
            }
            else
            {
                SetRect(
                    "Character Card",
                    new Vector2(18f, -18f),
                    new Vector2(342f, 96f));

                SetRect(
                    "Speedometer",
                    new Vector2(0f, 12f),
                    new Vector2(264f, 224f));

                SetRect(
                    "Activity Status",
                    new Vector2(0f, -18f),
                    new Vector2(430f, 40f));

                SetRect(
                    "Drift HUD",
                    new Vector2(0f, -66f),
                    new Vector2(258f, 40f));

                SetRect(
                    "Minimap",
                    new Vector2(-18f, -18f),
                    new Vector2(218f, 230f));
            }
        }

        private void ApplyModalComposition(
            bool touchLayout)
        {
            if (touchLayout)
            {
                SetRect(
                    "Activity Result",
                    Vector2.zero,
                    new Vector2(560f, 302f));

                SetRect(
                    "Navigator Menu",
                    Vector2.zero,
                    new Vector2(480f, 232f));

                SetRect(
                    "Club Panel",
                    Vector2.zero,
                    new Vector2(520f, 334f));

                SetRect(
                    "Garage Panel",
                    Vector2.zero,
                    new Vector2(720f, 544f));
            }
            else
            {
                SetRect(
                    "Activity Result",
                    Vector2.zero,
                    new Vector2(600f, 316f));

                SetRect(
                    "Navigator Menu",
                    Vector2.zero,
                    new Vector2(520f, 250f));

                SetRect(
                    "Club Panel",
                    Vector2.zero,
                    new Vector2(580f, 380f));

                SetRect(
                    "Garage Panel",
                    Vector2.zero,
                    new Vector2(760f, 570f));
            }
        }

        private void PolishTouchButtons()
        {
            if (!MotorCityInput.PreferTouchPrompts)
                return;

            Color touchSurface =
                new(
                    0.030f,
                    0.060f,
                    0.090f,
                    0.90f);

            PolishTouchGroup(
                "Touch Driving Controls",
                touchSurface);

            PolishTouchGroup(
                "Touch Utility Controls",
                touchSurface);

            PolishTouchGroup(
                "Result Touch Controls",
                touchSurface);

            PolishTouchGroup(
                "Navigator Touch Controls",
                touchSurface);

            PolishTouchGroup(
                "Store Touch Controls",
                touchSurface);

            PolishTouchGroup(
                "Club Touch Controls",
                touchSurface);
        }

        private void PolishTouchGroup(
            string rootName,
            Color baseColor)
        {
            RectTransform root =
                FindRect(
                    rootName);

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
                        Cyan.r,
                        Cyan.g,
                        Cyan.b,
                        0.18f);

                outline.effectDistance =
                    new Vector2(
                        1f,
                        -1f);

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

                ApplyTextShadow(
                    label,
                    0.60f);
            }
        }

        private void SetRect(
            string objectName,
            Vector2 position,
            Vector2 size)
        {
            RectTransform rect =
                FindRect(
                    objectName);

            if (rect == null)
                return;

            rect.anchoredPosition =
                position;

            rect.sizeDelta =
                size;

            // Parent scaling makes Legacy Text visibly softer in WebGL.
            rect.localScale =
                Vector3.one;
        }

        private void ApplyPanelTreatment(
            string objectName,
            Color color)
        {
            RectTransform rect =
                FindRect(
                    objectName);

            if (rect == null)
                return;

            Image image =
                rect.GetComponent<Image>();

            if (image != null)
            {
                image.color =
                    color;

                image.raycastTarget =
                    false;
            }

            Outline outline =
                rect.GetComponent<Outline>();

            if (outline == null)
            {
                outline =
                    rect.gameObject
                        .AddComponent<Outline>();
            }

            outline.enabled =
                true;

            outline.effectColor =
                new Color(
                    Cyan.r,
                    Cyan.g,
                    Cyan.b,
                    0.10f);

            outline.effectDistance =
                new Vector2(
                    1f,
                    -1f);

            outline.useGraphicAlpha =
                true;
        }

        private void RemoveLegacyHighlights()
        {
            if (hudRoot == null)
                return;

            Image[] images =
                hudRoot.GetComponentsInChildren<Image>(
                    true);

            foreach (Image image in images)
            {
                if (image == null ||
                    image.gameObject.name !=
                        "Visual Polish Highlight")
                {
                    continue;
                }

                image.gameObject.SetActive(
                    false);
            }
        }

        private void PolishCoreText(
            string objectName,
            float shadowAlpha)
        {
            RectTransform rect =
                FindRect(
                    objectName);

            if (rect == null)
                return;

            Text text =
                rect.GetComponent<Text>();

            if (text == null)
                return;

            text.resizeTextForBestFit =
                false;

            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;

            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            rect.localScale =
                Vector3.one;

            ApplyTextShadow(
                text,
                shadowAlpha);
        }

        private static void ApplyTextShadow(
            Text text,
            float alpha)
        {
            Shadow shadow =
                text.GetComponent<Shadow>();

            if (shadow == null ||
                shadow is Outline)
            {
                shadow =
                    text.gameObject
                        .AddComponent<Shadow>();
            }

            shadow.effectColor =
                new Color(
                    0f,
                    0f,
                    0f,
                    alpha);

            shadow.effectDistance =
                new Vector2(
                    1f,
                    -1f);

            shadow.useGraphicAlpha =
                true;
        }

        private RectTransform FindRect(
            string objectName)
        {
            if (hudRoot == null)
                return null;

            Transform target =
                FindRecursive(
                    hudRoot,
                    objectName);

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

            for (int i = 0;
                 i < parent.childCount;
                 i++)
            {
                Transform child =
                    parent.GetChild(i);

                Transform found =
                    FindRecursive(
                        child,
                        objectName);

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
                Screen.width /
                (float)Screen.height;

            return aspect >= 1.25f;
        }
    }
}
