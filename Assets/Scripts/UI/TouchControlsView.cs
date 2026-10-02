using MotorCity.Input;
using MotorCity.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private void RefreshTouchLocalizedLabels()
        {
            foreach (TouchLocalizedLabel binding in
                     touchLocalizedLabels)
            {
                if (binding.Text == null)
                    continue;

                binding.Text.text =
                    MotorCityLocalization.Text(
                        binding.LocalizationKey);
            }
        }

        private void CreateTouchPulseButton(
            Transform parent,
            string name,
            string label,
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
                new Vector2(0.5f, 0f);
            rect.anchorMax =
                new Vector2(0.5f, 0f);
            rect.pivot =
                new Vector2(0.5f, 0.5f);
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            if (name == "Handbrake")
            {
                rect.localScale =
                    new Vector3(
                        -1f,
                        1f,
                        1f);
            }

            Image image =
                buttonObject.GetComponent<Image>();

            Texture2D buttonTexture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.modalButton;

            Sprite buttonSprite =
                GetModalButtonSprite(
                    buttonTexture);

            if (buttonSprite != null)
            {
                image.sprite =
                    buttonSprite;
                image.type =
                    Image.Type.Simple;
                image.preserveAspect =
                    false;
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
                        0.94f);
            }

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                Color.white;
            colors.highlightedColor =
                new Color(
                    0.96f,
                    0.96f,
                    1f,
                    1f);
            colors.pressedColor =
                new Color(
                    0.82f,
                    0.80f,
                    0.92f,
                    1f);
            colors.selectedColor =
                colors.highlightedColor;

            button.colors =
                colors;

            button.onClick.AddListener(
                () =>
                    MotorCityInput.PulseVirtual(
                        action));

            Text text =
                CreateText(
                    rect,
                    "Label",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    size -
                    new Vector2(8f, 6f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            text.text =
                label;

            MakeButtonTextCrisp(
                text);
        }

        private GameObject CreateLocalizedTouchPulseButton(
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
                new Vector2(0.5f, 0f);
            rect.anchorMax =
                new Vector2(0.5f, 0f);
            rect.pivot =
                new Vector2(0.5f, 0.5f);
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                buttonObject.GetComponent<Image>();

            Texture2D buttonTexture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.modalButton;

            Sprite buttonSprite =
                GetModalButtonSprite(
                    buttonTexture);

            if (buttonSprite != null)
            {
                image.sprite =
                    buttonSprite;
                image.type =
                    Image.Type.Simple;
                image.preserveAspect =
                    false;
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
                        0.94f);
            }

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                Color.white;
            colors.highlightedColor =
                new Color(
                    0.96f,
                    0.96f,
                    1f,
                    1f);
            colors.pressedColor =
                new Color(
                    0.82f,
                    0.80f,
                    0.92f,
                    1f);
            colors.selectedColor =
                colors.highlightedColor;

            button.colors =
                colors;

            button.onClick.AddListener(
                () =>
                {
                    MotorCityInput.PulseVirtual(
                        action);

                    if (hudQuickMenuRoot != null &&
                        parent == hudQuickMenuRoot.transform)
                    {
                        hudQuickMenuOpen =
                            false;

                        hudQuickMenuRoot.SetActive(
                            false);
                    }
                });

            bool suppressActionIcon =
                name ==
                "Navigator Touch Controls Close";

            Sprite actionIcon =
                suppressActionIcon
                    ? null
                    : TouchActionIcon(
                        action);

            if (actionIcon != null)
            {
                CreateHudIcon(
                    rect,
                    "Action Icon",
                    actionIcon,
                    new Vector2(
                        -size.x * 0.5f + 18f,
                        0f),
                    new Vector2(
                        19f,
                        19f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    TextColor);
            }

            bool compactGarageButton =
                garageTouchControlsRoot != null &&
                parent == garageTouchControlsRoot.transform;

            Text text =
                CreateText(
                    rect,
                    "Label",
                    compactGarageButton
                        ? 10
                        : 12,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    actionIcon != null
                        ? new Vector2(
                            9f,
                            0f)
                        : Vector2.zero,
                    size -
                    new Vector2(
                        actionIcon != null
                            ? 30f
                            : 8f,
                        6f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            text.text =
                MotorCityLocalization.Text(
                    localizationKey);

            MakeButtonTextCrisp(
                text);

            touchLocalizedLabels.Add(
                new TouchLocalizedLabel(
                    text,
                    localizationKey));

            return
                buttonObject;
        }

        private static Sprite TouchActionIcon(
            MotorCityInputAction action)
        {
            return
                action switch
                {
                    MotorCityInputAction.Interact =>
                        MotorCityIconLibrary.Confirm,

                    MotorCityInputAction.Upgrade1 or
                    MotorCityInputAction.Upgrade2 or
                    MotorCityInputAction.Upgrade3 =>
                        MotorCityIconLibrary.Upgrades,

                    MotorCityInputAction.CycleDriveMode or
                    MotorCityInputAction.CycleWheels or
                    MotorCityInputAction.CycleNeon =>
                        MotorCityIconLibrary.Garage,

                    MotorCityInputAction.CycleBodyColor or
                    MotorCityInputAction.CycleSticker or
                    MotorCityInputAction.CycleVinyl or
                    MotorCityInputAction.CyclePlate =>
                        MotorCityIconLibrary.Reputation,

                    MotorCityInputAction.TakePhoto =>
                        MotorCityIconLibrary.ForActivity(
                            ActivityIcon.PhotoHunt),

                    MotorCityInputAction.ToggleVehiclePassport =>
                        MotorCityIconLibrary.ForSystem(
                            SystemIcon.VehicleHistory),

                    MotorCityInputAction.ToggleClub =>
                        MotorCityIconLibrary.ForSystem(
                            SystemIcon.Club),

                    MotorCityInputAction.RewardedBonus =>
                        MotorCityIconLibrary.Reward,

                    MotorCityInputAction.ToggleStore =>
                        MotorCityIconLibrary.Store,

                    MotorCityInputAction.ToggleNavigator =>
                        MotorCityIconLibrary.ForActivity(
                            ActivityIcon.Discovery),

                    _ =>
                        null
                };
        }

        private static bool ShouldUseTouchUi()
        {
            if (MotorCityInput.PreferTouchPrompts)
            {
                return true;
            }

#if UNITY_EDITOR
            // Device Simulator can still run as Editor with platform/touch
            // flags unavailable. Keep the portrait fallback for quick phone
            // HUD testing while the shared runtime preference handles actual
            // mobile devices and landscape simulator sessions.
            return
                Screen.height > Screen.width;
#else
            return false;
#endif
        }

        private void ApplyResponsiveCanvasScale(
            bool force)
        {
            if (canvasScaler == null)
                return;

            // Author every runtime HUD screen in one deterministic 16:9
            // coordinate system. Device/aspect adaptation is intentionally
            // layered on top later instead of changing the design resolution.
            canvasScaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f);

            canvasScaler.matchWidthOrHeight =
                0.5f;
        }

        private void BuildTouchUtilityControls(
            Transform canvas)
        {
            touchUtilityRoot =
                new GameObject(
                    "Main HUD Quick Actions",
                    typeof(RectTransform));

            touchUtilityRoot.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                touchUtilityRoot.GetComponent<RectTransform>();

            // Full safe-area overlay: individual controls can live near the
            // speedometer and screen edge without creating a toolbar across
            // the middle of the HUD.
            root.anchorMin =
                Vector2.zero;
            root.anchorMax =
                Vector2.one;
            root.pivot =
                new Vector2(0.5f, 0.5f);
            root.anchoredPosition =
                Vector2.zero;
            root.sizeDelta =
                Vector2.zero;

            // Driving mode is the only always-visible gameplay utility.
            // Keep it next to the speedometer so the top-centre remains free
            // for dialogue, mission notifications and XP feedback.
            CreateLocalizedTouchPulseButton(
                root,
                "HUD Drive Mode",
                "touch.utility.mode",
                MotorCityInputAction.CycleDriveMode,
                new Vector2(142f, 38f),
                new Vector2(96f, 44f));

            GameObject railObject =
                new(
                    "HUD Utility Rail",
                    typeof(RectTransform));

            railObject.transform.SetParent(
                root,
                false);

            RectTransform rail =
                railObject.GetComponent<RectTransform>();

            rail.anchorMin =
                new Vector2(1f, 1f);
            rail.anchorMax =
                new Vector2(1f, 1f);
            rail.pivot =
                new Vector2(1f, 1f);
            rail.anchoredPosition =
                new Vector2(-18f, -250f);
            rail.sizeDelta =
                new Vector2(108f, 154f);

            CreatePauseButton(
                rail,
                "HUD Pause",
                "touch.utility.pause",
                new Vector2(0f, 52f),
                new Vector2(100f, 44f),
                OpenPauseMenu);

            GameObject rescueButton =
                CreateLocalizedTouchPulseButton(
                    rail,
                    "HUD Rescue",
                    "touch.utility.rescue",
                    MotorCityInputAction.Rescue,
                    new Vector2(0f, 0f),
                    new Vector2(100f, 44f));

            RectTransform rescueRect =
                rescueButton.GetComponent<RectTransform>();

            rescueRect.anchorMin =
                new Vector2(0.5f, 0.5f);
            rescueRect.anchorMax =
                new Vector2(0.5f, 0.5f);
            rescueRect.pivot =
                new Vector2(0.5f, 0.5f);
            rescueRect.anchoredPosition =
                Vector2.zero;

            CreatePauseButton(
                rail,
                "HUD More",
                "touch.utility.more",
                new Vector2(0f, -52f),
                new Vector2(100f, 44f),
                ToggleHudQuickMenu);

            hudQuickMenuRoot =
                new GameObject(
                    "HUD Secondary Actions",
                    typeof(RectTransform));

            hudQuickMenuRoot.transform.SetParent(
                root,
                false);

            RectTransform menu =
                hudQuickMenuRoot.GetComponent<RectTransform>();

            menu.anchorMin =
                new Vector2(1f, 1f);
            menu.anchorMax =
                new Vector2(1f, 1f);
            menu.pivot =
                new Vector2(1f, 1f);
            menu.anchoredPosition =
                new Vector2(-134f, -250f);
            menu.sizeDelta =
                new Vector2(120f, 198f);

            CreateLocalizedTouchPulseButton(
                menu,
                "HUD Photo",
                "touch.utility.photo",
                MotorCityInputAction.TakePhoto,
                new Vector2(0f, 22f),
                new Vector2(112f, 44f));

            CreateLocalizedTouchPulseButton(
                menu,
                "HUD Store",
                "touch.utility.store",
                MotorCityInputAction.ToggleStore,
                new Vector2(0f, 72f),
                new Vector2(112f, 44f));

            CreateLocalizedTouchPulseButton(
                menu,
                "HUD Club",
                "touch.utility.club",
                MotorCityInputAction.ToggleClub,
                new Vector2(0f, 122f),
                new Vector2(112f, 44f));

            CreateLocalizedTouchPulseButton(
                menu,
                "HUD Bonus",
                "touch.utility.bonus",
                MotorCityInputAction.RewardedBonus,
                new Vector2(0f, 172f),
                new Vector2(112f, 44f));

            hudQuickMenuOpen =
                false;

            hudQuickMenuRoot.SetActive(
                false);
        }

        private void ToggleHudQuickMenu()
        {
            bool unlocked =
                activityManager != null &&
                activityManager.SecondaryProgressionAllowed;

            if (!unlocked)
            {
                hudQuickMenuOpen =
                    false;

                hudQuickMenuRoot?.SetActive(
                    false);

                return;
            }

            hudQuickMenuOpen =
                !hudQuickMenuOpen;

            hudQuickMenuRoot?.SetActive(
                hudQuickMenuOpen);
        }

        private void BuildTouchPauseControl(
            Transform canvas)
        {
            touchPauseRoot =
                new GameObject(
                    "Touch Pause Control",
                    typeof(RectTransform));

            touchPauseRoot.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                touchPauseRoot.GetComponent<RectTransform>();

            root.anchorMin =
                new Vector2(0f, 1f);

            root.anchorMax =
                new Vector2(0f, 1f);

            root.pivot =
                new Vector2(0f, 1f);

            root.anchoredPosition =
                new Vector2(
                    18f,
                    -154f);

            root.sizeDelta =
                new Vector2(
                    54f,
                    44f);

            GameObject buttonObject =
                new(
                    "Touch Pause",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            buttonObject.transform.SetParent(
                root,
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
                Vector2.zero;

            rect.sizeDelta =
                new Vector2(50f, 40f);

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.075f,
                    0.07f,
                    0.12f,
                    0.94f);

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            button.onClick.AddListener(
                OpenPauseMenu);

            CreatePauseGlyph(
                rect);
        }

        private static void CreatePauseGlyph(
            Transform parent)
        {
            for (int i = 0;
                 i < 2;
                 i++)
            {
                GameObject bar =
                    new(
                        "Pause Bar",
                        typeof(RectTransform),
                        typeof(Image));

                bar.transform.SetParent(
                    parent,
                    false);

                RectTransform rect =
                    bar.GetComponent<RectTransform>();

                rect.anchorMin =
                    new Vector2(0.5f, 0.5f);

                rect.anchorMax =
                    new Vector2(0.5f, 0.5f);

                rect.pivot =
                    new Vector2(0.5f, 0.5f);

                rect.anchoredPosition =
                    new Vector2(
                        i == 0
                            ? -5f
                            : 5f,
                        0f);

                rect.sizeDelta =
                    new Vector2(
                        4f,
                        16f);

                Image image =
                    bar.GetComponent<Image>();

                image.color =
                    TextColor;

                image.raycastTarget =
                    false;
            }
        }

        private void BuildTouchActivityCancelControl(
            Transform canvas)
        {
            touchActivityCancelRoot =
                new GameObject(
                    "Touch Activity Cancel",
                    typeof(RectTransform));

            touchActivityCancelRoot.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                touchActivityCancelRoot.GetComponent<RectTransform>();

            root.anchorMin =
                new Vector2(1f, 0f);
            root.anchorMax =
                new Vector2(1f, 0f);
            root.pivot =
                new Vector2(1f, 0f);
            root.anchoredPosition =
                new Vector2(-52f, 438f);
            root.sizeDelta =
                new Vector2(118f, 48f);

            GameObject button =
                CreateLocalizedTouchPulseButton(
                    root,
                    "Touch Cancel Activity",
                    "touch.drive.cancel",
                    MotorCityInputAction.Cancel,
                    Vector2.zero,
                    new Vector2(118f, 48f));

            RectTransform buttonRect =
                button.GetComponent<RectTransform>();

            buttonRect.anchorMin =
                new Vector2(0.5f, 0f);
            buttonRect.anchorMax =
                new Vector2(0.5f, 0f);
            buttonRect.pivot =
                new Vector2(0.5f, 0f);

            touchActivityCancelRoot.SetActive(
                false);
        }

        private void BuildModalTouchControls(
            Transform canvas)
        {
            navigatorTouchControlsRoot =
                BuildNavigatorTouchControls(
                    canvas);

            storeTouchControlsRoot = null;

            clubTouchControlsRoot =
                BuildClubTouchControls(
                    canvas);

            navigatorTouchControlsRoot.SetActive(
                false);
            storeTouchControlsRoot?.SetActive(
                false);
            clubTouchControlsRoot.SetActive(
                false);
        }

        private GameObject BuildTouchModalRow(
            Transform canvas,
            string name,
            string leftLocalizationKey,
            MotorCityInputAction leftAction,
            string centerLocalizationKey,
            MotorCityInputAction centerAction,
            string rightLocalizationKey,
            MotorCityInputAction rightAction,
            string closeLocalizationKey,
            MotorCityInputAction closeAction)
        {
            GameObject rootObject =
                new(
                    name,
                    typeof(RectTransform));

            rootObject.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                rootObject.GetComponent<RectTransform>();

            root.anchorMin =
                new Vector2(0.5f, 0f);
            root.anchorMax =
                new Vector2(0.5f, 0f);
            root.pivot =
                new Vector2(0.5f, 0f);
            root.anchoredPosition =
                new Vector2(0f, 28f);
            root.sizeDelta =
                new Vector2(620f, 58f);

            CreateLocalizedTouchPulseButton(
                root,
                name + " Prev",
                leftLocalizationKey,
                leftAction,
                new Vector2(-225f, 26f),
                new Vector2(130f, 50f));

            CreateLocalizedTouchPulseButton(
                root,
                name + " Action",
                centerLocalizationKey,
                centerAction,
                new Vector2(-75f, 26f),
                new Vector2(150f, 50f));

            CreateLocalizedTouchPulseButton(
                root,
                name + " Next",
                rightLocalizationKey,
                rightAction,
                new Vector2(85f, 26f),
                new Vector2(130f, 50f));

            CreateLocalizedTouchPulseButton(
                root,
                name + " Close",
                closeLocalizationKey,
                closeAction,
                new Vector2(230f, 26f),
                new Vector2(130f, 50f));

            return
                rootObject;
        }

        private void BuildTouchControls(
            Transform canvas)
        {
            touchControlsRoot =
                new GameObject(
                    "Touch Driving Controls",
                    typeof(RectTransform));

            touchControlsRoot.transform.SetParent(
                canvas,
                false);

            RectTransform root =
                touchControlsRoot.GetComponent<RectTransform>();

            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            BuildLandscapeDrivingControls(
                root);
        }

        private void BuildLandscapeDrivingControls(
            RectTransform root)
        {
            // Keep the driving HUD close to the reference layout: steering on
            // the left, pedals on the right, handbrake above the pedals.
            // Artwork is intentionally translucent so it never hides the road.
            CreateTouchArtHoldButton(
                root,
                "Throttle",
                MotorCityInputAction.Throttle,
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.touchThrottle,
                new Vector2(-42f, 30f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(92f, 156f),
                new Color32(255, 255, 255, 0x64));

            CreateTouchArtHoldButton(
                root,
                "Reverse",
                MotorCityInputAction.Reverse,
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.touchBrake,
                new Vector2(-158f, 34f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(96f, 118f),
                new Color32(255, 255, 255, 0x64));

            Texture2D handbrakeTexture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.touchHandbrake;

            CreateTouchArtHoldButton(
                root,
                "Handbrake",
                MotorCityInputAction.Handbrake,
                handbrakeTexture,
                new Vector2(-36f, 208f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                FitTouchArtSize(
                    handbrakeTexture,
                    new Vector2(210f, 185f)),
                new Color32(255, 255, 255, 165));

            GameObject interactButton =
                CreateLocalizedTouchPulseButton(
                    root,
                    "Interact",
                    "touch.drive.action_short",
                    MotorCityInputAction.Interact,
                    Vector2.zero,
                    new Vector2(118f, 48f));

            Button interactUnityButton =
                interactButton.GetComponent<Button>();

            if (interactUnityButton != null)
            {
                interactUnityButton.onClick.RemoveAllListeners();
            }

            TouchPulseInputButton interactInput =
                interactButton.GetComponent<TouchPulseInputButton>();

            if (interactInput == null)
            {
                interactInput =
                    interactButton.AddComponent<TouchPulseInputButton>();
            }

            interactInput.Bind(
                MotorCityInputAction.Interact);

            RectTransform interactRect =
                interactButton.GetComponent<RectTransform>();

            interactRect.anchorMin =
                new Vector2(1f, 0f);
            interactRect.anchorMax =
                new Vector2(1f, 0f);
            interactRect.pivot =
                new Vector2(1f, 0f);
            interactRect.anchoredPosition =
                new Vector2(-52f, 382f);

            touchWheelSteeringRoot =
                CreateTouchSteeringWheelGroup(
                    root);

            touchArrowSteeringRoot =
                CreateTouchArrowSteeringGroup(
                    root);

            bool wheelScheme =
                MotorCityInput.CurrentControlScheme ==
                MotorCityControlScheme.Wheel;

            touchWheelSteeringRoot.SetActive(
                wheelScheme);

            touchArrowSteeringRoot.SetActive(
                !wheelScheme);
        }

        private static Vector2 FitTouchArtSize(
            Texture2D texture,
            Vector2 maxSize)
        {
            if (texture == null ||
                texture.width <= 0 ||
                texture.height <= 0)
            {
                return maxSize;
            }

            float scale =
                Mathf.Min(
                    maxSize.x / texture.width,
                    maxSize.y / texture.height);

            return new Vector2(
                Mathf.Max(
                    1f,
                    texture.width * scale),
                Mathf.Max(
                    1f,
                    texture.height * scale));
        }

        private RectTransform CreateTouchArtHoldButton(
            Transform parent,
            string name,
            MotorCityInputAction action,
            Texture2D texture,
            Vector2 anchoredPosition,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 size,
            Color color)
        {
            GameObject buttonObject =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(TouchHoldInputButton));

            buttonObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                buttonObject.GetComponent<Image>();

            image.sprite =
                CreateTouchControlSprite(
                    texture);
            image.preserveAspect =
                true;
            image.raycastTarget =
                true;
            image.color =
                color;

            TouchHoldInputButton input =
                buttonObject.GetComponent<TouchHoldInputButton>();

            input.Bind(
                action);

            return rect;
        }

        private GameObject CreateTouchSteeringWheelGroup(
            Transform parent)
        {
            GameObject group =
                new(
                    "Wheel Steering",
                    typeof(RectTransform));

            group.transform.SetParent(
                parent,
                false);

            RectTransform groupRect =
                group.GetComponent<RectTransform>();

            groupRect.anchorMin =
                new Vector2(0f, 0f);
            groupRect.anchorMax =
                new Vector2(0f, 0f);
            groupRect.pivot =
                new Vector2(0f, 0f);
            groupRect.anchoredPosition =
                new Vector2(32f, 24f);
            groupRect.sizeDelta =
                new Vector2(210f, 210f);

            GameObject wheelObject =
                new(
                    "Steering Wheel",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(TouchSteeringWheel));

            wheelObject.transform.SetParent(
                group.transform,
                false);

            RectTransform wheelRect =
                wheelObject.GetComponent<RectTransform>();

            wheelRect.anchorMin =
                new Vector2(0.5f, 0.5f);
            wheelRect.anchorMax =
                new Vector2(0.5f, 0.5f);
            wheelRect.pivot =
                new Vector2(0.5f, 0.5f);
            wheelRect.anchoredPosition =
                Vector2.zero;
            wheelRect.sizeDelta =
                new Vector2(194f, 194f);

            Image image =
                wheelObject.GetComponent<Image>();

            image.sprite =
                CreateTouchControlSprite(
                    uiThemeAssets == null
                        ? null
                        : uiThemeAssets.touchWheel);
            image.preserveAspect =
                true;
            image.raycastTarget =
                true;
            image.color =
                new Color32(
                    255,
                    255,
                    255,
                    0x96);

            return group;
        }

        private GameObject CreateTouchArrowSteeringGroup(
            Transform parent)
        {
            GameObject group =
                new(
                    "Arrow Steering",
                    typeof(RectTransform));

            group.transform.SetParent(
                parent,
                false);

            RectTransform groupRect =
                group.GetComponent<RectTransform>();

            groupRect.anchorMin =
                new Vector2(0f, 0f);
            groupRect.anchorMax =
                new Vector2(0f, 0f);
            groupRect.pivot =
                new Vector2(0f, 0f);
            groupRect.anchoredPosition =
                new Vector2(24f, 30f);
            groupRect.sizeDelta =
                new Vector2(310f, 126f);

            RectTransform left =
                CreateTouchHoldButton(
                    groupRect,
                    "Steer Left",
                    string.Empty,
                    MotorCityInputAction.SteerLeft,
                    new Vector2(18f, 10f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(128f, 104f),
                    1);

            Image leftHitArea =
                left.GetComponent<Image>();

            leftHitArea.sprite =
                null;
            leftHitArea.color =
                new Color32(255, 255, 255, 0x00);

            CreateTouchChevron(
                left,
                false);

            RectTransform right =
                CreateTouchHoldButton(
                    groupRect,
                    "Steer Right",
                    string.Empty,
                    MotorCityInputAction.SteerRight,
                    new Vector2(164f, 10f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(128f, 104f),
                    1);

            Image rightHitArea =
                right.GetComponent<Image>();

            rightHitArea.sprite =
                null;
            rightHitArea.color =
                new Color32(255, 255, 255, 0x00);

            CreateTouchChevron(
                right,
                true);

            return group;
        }

        private static void CreateTouchChevron(
            Transform parent,
            bool pointsRight)
        {
            float sign =
                pointsRight
                    ? 1f
                    : -1f;

            CreateTouchChevronSegment(
                parent,
                new Vector2(
                    sign * 11f,
                    19f),
                pointsRight
                    ? -45f
                    : 45f);

            CreateTouchChevronSegment(
                parent,
                new Vector2(
                    sign * 11f,
                    -19f),
                pointsRight
                    ? 45f
                    : -45f);
        }

        private static void CreateTouchChevronSegment(
            Transform parent,
            Vector2 position,
            float rotation)
        {
            GameObject segment =
                new(
                    "Arrow Stroke",
                    typeof(RectTransform),
                    typeof(Image));

            segment.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                segment.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0.5f, 0.5f);
            rect.anchorMax =
                new Vector2(0.5f, 0.5f);
            rect.pivot =
                new Vector2(0.5f, 0.5f);
            rect.anchoredPosition =
                position;
            rect.sizeDelta =
                new Vector2(62f, 7f);
            rect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    rotation);

            Image image =
                segment.GetComponent<Image>();

            image.color =
                new Color32(
                    255,
                    255,
                    255,
                    0xFF);
            image.raycastTarget =
                false;
        }

        private static Sprite CreateTouchControlSprite(
            Texture2D texture)
        {
            if (texture == null)
                return null;

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        texture.width,
                        texture.height),
                    new Vector2(
                        0.5f,
                        0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);

            sprite.name =
                texture.name +
                " Touch Control";
            sprite.hideFlags =
                HideFlags.DontSave;

            return sprite;
        }

        private void CreateTouchControlBackdrop(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 anchor,
            Vector2 pivot)
        {
            GameObject backdropObject =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image));

            backdropObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                backdropObject.GetComponent<RectTransform>();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                backdropObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.015f,
                    0.035f,
                    0.06f,
                    0.24f);

            image.raycastTarget =
                false;
        }

        private RectTransform CreateTouchHoldButton(
            Transform parent,
            string name,
            string label,
            MotorCityInputAction action,
            Vector2 anchoredPosition,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 size,
            int fontSize)
        {
            GameObject buttonObject =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(TouchHoldInputButton));

            buttonObject.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition =
                anchoredPosition;
            rect.sizeDelta =
                size;

            Image image =
                buttonObject.GetComponent<Image>();

            image.color =
                new Color(
                    0.035f,
                    0.085f,
                    0.13f,
                    0.76f);

            TouchHoldInputButton input =
                buttonObject.GetComponent<TouchHoldInputButton>();

            input.Bind(action);

            Text text =
                CreateText(
                    rect,
                    "Label",
                    fontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    size -
                    new Vector2(10f, 10f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            text.text =
                label;

            text.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(
                    label));

            return rect;
        }

        private void CreateTouchDirectionGlyph(
            Transform parent,
            float rotation)
        {
            if (parent == null)
                return;

            GameObject glyph =
                new(
                    "Direction Glyph",
                    typeof(RectTransform));

            glyph.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                glyph.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchoredPosition =
                Vector2.zero;

            rect.sizeDelta =
                new Vector2(
                    34f,
                    34f);

            rect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    rotation);

            CreateMinimapPlayerChevron(
                rect);
        }

        private void UpdateTouchControlsVisibility()
        {
            ApplyResponsiveCanvasScale(
                false);

            if (touchControlsRoot == null)
                return;

            bool touchUi =
                ShouldUseTouchUi();

            bool garageOpen =
                garage != null &&
                garage.IsOpen;

            if (!touchUi)
            {
                SetActiveIfChanged(
                    touchControlsRoot,
                    false);

                SetActiveIfChanged(
                    touchUtilityRoot,
                    !pauseMenuOpen &&
                    !HasBlockingModalUi());

                SetActiveIfChanged(
                    touchActivityCancelRoot,
                    false);

                SetActiveIfChanged(
                    touchPauseRoot,
                    false);

                SetActiveIfChanged(
                    resultTouchControlsRoot,
                    false);

                SetActiveIfChanged(
                    navigatorTouchControlsRoot,
                    navigatorMenuOpen);

                SetActiveIfChanged(
                    storeTouchControlsRoot,
                    storeOpen);

                SetActiveIfChanged(
                    clubTouchControlsRoot,
                    clubOpen);

                SetActiveIfChanged(
                    garageTouchControlsRoot,
                    garageOpen);

                return;
            }

            SetActiveIfChanged(
                touchControlsRoot,
                !HasBlockingModalUi());

            bool wheelScheme =
                MotorCityInput.CurrentControlScheme ==
                MotorCityControlScheme.Wheel;

            SetActiveIfChanged(
                touchWheelSteeringRoot,
                wheelScheme);

            SetActiveIfChanged(
                touchArrowSteeringRoot,
                !wheelScheme);

            SetActiveIfChanged(
                touchPauseRoot,
                false);

            // Pause / rescue / quick actions are core controls, not
            // post-onboarding unlocks. They must remain available during the
            // rookie path for wheel and arrow control schemes just like they
            // already are for keyboard input.
            SetActiveIfChanged(
                touchUtilityRoot,
                !pauseMenuOpen &&
                !HasBlockingModalUi());

            SetActiveIfChanged(
                touchActivityCancelRoot,
                !HasBlockingModalUi() &&
                activityManager != null &&
                activityManager.IsBusy);

            bool resultOpen =
                activityManager != null &&
                activityManager.HasResult;

            SetActiveIfChanged(
                resultTouchControlsRoot,
                resultOpen);

            if (resultRetryTouchButton != null)
            {
                SetActiveIfChanged(
                    resultRetryTouchButton,
                    resultOpen &&
                    IsReplayableResult(
                        activityManager.ResultActivityId));
            }

            SetActiveIfChanged(
                navigatorTouchControlsRoot,
                navigatorMenuOpen);

            SetActiveIfChanged(
                storeTouchControlsRoot,
                storeOpen);

            SetActiveIfChanged(
                clubTouchControlsRoot,
                clubOverlay != null &&
                clubOverlay.activeSelf);

            SetActiveIfChanged(
                garageTouchControlsRoot,
                garageOpen);

        }

        private RectTransform CreateSafeAreaRoot(
            Transform parent)
        {
            GameObject root =
                new(
                    "Safe Area",
                    typeof(RectTransform));

            root.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                root.GetComponent<RectTransform>();

            ApplySafeArea(
                rect);

            SafeAreaRuntimeUpdater updater =
                root.AddComponent<SafeAreaRuntimeUpdater>();

            updater.Bind(
                rect);

            return rect;
        }

        private static void ApplySafeArea(
            RectTransform rect)
        {
            if (rect == null ||
                Screen.width <= 0 ||
                Screen.height <= 0)
            {
                return;
            }

            Rect safe =
                Screen.safeArea;

            Vector2 min =
                safe.position;

            Vector2 max =
                safe.position +
                safe.size;

            min.x /=
                Screen.width;
            min.y /=
                Screen.height;
            max.x /=
                Screen.width;
            max.y /=
                Screen.height;

            rect.anchorMin =
                min;
            rect.anchorMax =
                max;
            rect.offsetMin =
                Vector2.zero;
            rect.offsetMax =
                Vector2.zero;
        }

    }
}
