using System;
using MotorCity.Input;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    // Visual-only tokens for game menus and modal action buttons.
    // This does not restyle the completed vehicle customization interface.
    internal enum MotorCityButtonRole
    {
        Primary,
        Secondary,
        Utility,
        Back
    }

    internal static class MotorCityUiSkin
    {
        internal static readonly Color Panel = new(0.14f, 0.10f, 0.26f, 0.94f);
        internal static readonly Color Raised = new(0.19f, 0.14f, 0.31f, 0.96f);
        internal static readonly Color TextPrimary = new(0.95f, 0.97f, 1f, 1f);
        internal static readonly Color TextSecondary = new(0.72f, 0.77f, 0.87f, 1f);
        internal static readonly Color AccentCyan = new(0.33f, 0.78f, 1f, 1f);
        internal static readonly Color AccentLilac = new(0.65f, 0.55f, 1f, 1f);

        internal const float SpaceSmall = 8f;
        internal const float SpaceMedium = 16f;
        internal const float SpaceLarge = 24f;
        internal const int TitleFontSize = 28;
        internal const int BodyFontSize = 15;

        // Only apply to a modal's owned surface: never restyle the separate
        // completed vehicle customization UI or replace layout-owned geometry.
        internal static void ApplyModalSurface(
            RectTransform panel,
            string surfaceName)
        {
            if (panel == null)
                return;

            Transform surfaceNode = panel.Find(surfaceName);
            if (surfaceNode == null)
                return;

            GarageReferenceGraphic surface =
                surfaceNode.GetComponent<GarageReferenceGraphic>();
            if (surface != null)
            {
                surface.color = Panel;
                surface.raycastTarget = false;
            }

            foreach (Outline outline in panel.GetComponents<Outline>())
                outline.enabled = false;
        }

        internal static void AddModalHeaderDivider(
            RectTransform panel,
            float headerHeight,
            float horizontalInset)
        {
            if (panel == null || panel.Find("Modal Header Divider") != null)
                return;

            GameObject divider = new(
                "Modal Header Divider",
                typeof(RectTransform),
                typeof(Image));
            divider.transform.SetParent(panel, false);
            RectTransform rect = divider.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -headerHeight);
            rect.sizeDelta = new Vector2(-horizontalInset * 2f, 1.5f);

            Image image = divider.GetComponent<Image>();
            image.color = new Color(
                AccentLilac.r, AccentLilac.g, AccentLilac.b, 0.28f);
            image.raycastTarget = false;
        }

        internal static void ApplyButton(
            Button button,
            Graphic surface,
            MotorCityButtonRole role)
        {
            if (button == null || surface == null)
                return;

            // Tint the EXISTING target graphic instead of adding overlays
            // or changing RectTransform hit areas and their input handlers.
            surface.color = role switch
            {
                MotorCityButtonRole.Primary =>
                    new Color(0.18f, 0.39f, 0.57f, 0.98f),
                MotorCityButtonRole.Utility =>
                    new Color(0.14f, 0.13f, 0.24f, 0.96f),
                MotorCityButtonRole.Back =>
                    new Color(0.20f, 0.14f, 0.28f, 0.96f),
                _ =>
                    new Color(0.20f, 0.16f, 0.34f, 0.96f)
            };
            surface.raycastTarget = true;

            button.targetGraphic = surface;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.10f, 1.16f, 1f);
            colors.pressedColor = new Color(0.77f, 0.84f, 0.94f, 1f);
            colors.selectedColor = new Color(1.10f, 1.10f, 1.18f, 1f);
            colors.disabledColor = new Color(0.48f, 0.50f, 0.56f, 0.75f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.09f;
            button.colors = colors;
        }
    }

    internal static class MotorCityButtonVisuals
    {
        public static bool TryForAction(
            MotorCityInputAction action,
            out GarageReferenceGraphic.Symbol symbol,
            out Color color)
        {
            switch (action)
            {
                case MotorCityInputAction.Interact:
                case MotorCityInputAction.Cancel:
                    symbol = GarageReferenceGraphic.Symbol.Check;
                    color = Green;
                    return true;

                case MotorCityInputAction.Retry:
                    symbol = GarageReferenceGraphic.Symbol.NavigationRight;
                    color = Blue;
                    return true;

                case MotorCityInputAction.EliteModifier:
                    symbol = GarageReferenceGraphic.Symbol.Crown;
                    color = Magenta;
                    return true;

                case MotorCityInputAction.PreviousVehicle:
                    symbol = GarageReferenceGraphic.Symbol.NavigationLeft;
                    color = Cyan;
                    return true;

                case MotorCityInputAction.NextVehicle:
                    symbol = GarageReferenceGraphic.Symbol.NavigationRight;
                    color = Cyan;
                    return true;

                case MotorCityInputAction.Upgrade1:
                case MotorCityInputAction.Upgrade2:
                case MotorCityInputAction.Upgrade3:
                    symbol = GarageReferenceGraphic.Symbol.Up;
                    color = Green;
                    return true;

                case MotorCityInputAction.CycleDriveMode:
                    symbol = GarageReferenceGraphic.Symbol.Gear;
                    color = Cyan;
                    return true;

                case MotorCityInputAction.Rescue:
                    symbol = GarageReferenceGraphic.Symbol.Wrench;
                    color = Orange;
                    return true;

                case MotorCityInputAction.CycleBodyColor:
                    symbol = GarageReferenceGraphic.Symbol.Paint;
                    color = Pink;
                    return true;

                case MotorCityInputAction.CycleWheels:
                    symbol = GarageReferenceGraphic.Symbol.Rim;
                    color = LightBlue;
                    return true;

                case MotorCityInputAction.CycleNeon:
                    symbol = GarageReferenceGraphic.Symbol.Neon;
                    color = Magenta;
                    return true;

                case MotorCityInputAction.TakePhoto:
                    symbol = GarageReferenceGraphic.Symbol.Camera;
                    color = Cyan;
                    return true;

                case MotorCityInputAction.ToggleVehiclePassport:
                    symbol = GarageReferenceGraphic.Symbol.Check;
                    color = Lilac;
                    return true;

                case MotorCityInputAction.ToggleClub:
                    symbol = GarageReferenceGraphic.Symbol.Crown;
                    color = Purple;
                    return true;

                case MotorCityInputAction.RewardedBonus:
                    symbol = GarageReferenceGraphic.Symbol.Gift;
                    color = Gold;
                    return true;

                case MotorCityInputAction.ToggleStore:
                    symbol = GarageReferenceGraphic.Symbol.Shop;
                    color = Green;
                    return true;

                case MotorCityInputAction.ToggleNavigator:
                    symbol = GarageReferenceGraphic.Symbol.Right;
                    color = Cyan;
                    return true;

                default:
                    symbol = GarageReferenceGraphic.Symbol.Check;
                    color = Cyan;
                    return false;
            }
        }

        public static bool TryForButton(
            string semantic,
            MotorCityInputAction action,
            out GarageReferenceGraphic.Symbol symbol,
            out Color color)
        {
            if (TryForSemantic(
                    semantic,
                    out symbol,
                    out color))
            {
                return true;
            }

            return TryForAction(
                action,
                out symbol,
                out color);
        }

        public static bool TryForSemantic(
            string semantic,
            out GarageReferenceGraphic.Symbol symbol,
            out Color color)
        {
            string value =
                string.IsNullOrWhiteSpace(semantic)
                    ? string.Empty
                    : semantic.Trim().ToLowerInvariant();

            if (ContainsAny(
                    value,
                    "prev",
                    "previous",
                    "back",
                    "назад"))
            {
                symbol =
                    GarageReferenceGraphic.Symbol.NavigationLeft;
                color =
                    LightBlue;
                return true;
            }

            if (ContainsAny(
                    value,
                    "next",
                    "дальше",
                    "след"))
            {
                symbol =
                    GarageReferenceGraphic.Symbol.NavigationRight;
                color =
                    LightBlue;
                return true;
            }

            if (ContainsAny(
                    value,
                    "select",
                    "join",
                    "choose",
                    "выбрать",
                    "вступ"))
            {
                symbol =
                    GarageReferenceGraphic.Symbol.Check;
                color =
                    Green;
                return true;
            }

            if (ContainsAny(value, "close", "закрыть"))
            {
                symbol = GarageReferenceGraphic.Symbol.Close;
                color = Orange;
                return true;
            }

            if (ContainsAny(value, "navigator", "навига"))
            {
                symbol = GarageReferenceGraphic.Symbol.Right;
                color = Cyan;
                return true;
            }

            if (ContainsAny(value, "more", "ещё", "еще"))
            {
                symbol = GarageReferenceGraphic.Symbol.MenuGrid;
                color = Lilac;
                return true;
            }

            if (ContainsAny(value, "pause", "пауза"))
            {
                symbol = GarageReferenceGraphic.Symbol.Pause;
                color = Blue;
                return true;
            }

            if (ContainsAny(value, "bonus", "бонус"))
            {
                symbol = GarageReferenceGraphic.Symbol.Gift;
                color = Gold;
                return true;
            }

            if (ContainsAny(value, "club", "клуб"))
            {
                symbol = GarageReferenceGraphic.Symbol.Crown;
                color = Purple;
                return true;
            }

            if (ContainsAny(value, "rescue", "спасти"))
            {
                symbol = GarageReferenceGraphic.Symbol.Wrench;
                color = Orange;
                return true;
            }

            if (ContainsAny(value, "store", "shop", "магазин"))
            {
                symbol = GarageReferenceGraphic.Symbol.Shop;
                color = Green;
                return true;
            }

            if (ContainsAny(value, "photo", "фото"))
            {
                symbol = GarageReferenceGraphic.Symbol.Camera;
                color = Cyan;
                return true;
            }

            if (ContainsAny(value, "elite", "элита"))
            {
                symbol = GarageReferenceGraphic.Symbol.Crown;
                color = Magenta;
                return true;
            }

            if (ContainsAny(value, "mode", "режим"))
            {
                symbol = GarageReferenceGraphic.Symbol.Gear;
                color = Cyan;
                return true;
            }

            if (ContainsAny(value, "settings", "настрой"))
            {
                symbol = GarageReferenceGraphic.Symbol.Wrench;
                color = Cyan;
                return true;
            }

            if (ContainsAny(value, "keyboard", "клавиат"))
            {
                symbol = GarageReferenceGraphic.Symbol.Keyboard;
                color = Cyan;
                return true;
            }

            if (ContainsAny(value, "wheel", "колес", "колёс"))
            {
                symbol = GarageReferenceGraphic.Symbol.Steering;
                color = LightBlue;
                return true;
            }

            if (ContainsAny(value, "retry", "повтор"))
            {
                symbol = GarageReferenceGraphic.Symbol.NavigationRight;
                color = Blue;
                return true;
            }

            if (ContainsAny(value, "continue", "resume", "продолж"))
            {
                symbol = GarageReferenceGraphic.Symbol.Check;
                color = Green;
                return true;
            }

            if (ContainsAny(value, "start", "play", "начать"))
            {
                symbol = GarageReferenceGraphic.Symbol.Right;
                color = Green;
                return true;
            }

            symbol = GarageReferenceGraphic.Symbol.Star;
            color = Cyan;
            return false;
        }

        private static bool ContainsAny(
            string value,
            params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (value.IndexOf(
                        needle,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static readonly Color Cyan =
            new(0.22f, 0.82f, 1f, 1f);

        private static readonly Color LightBlue =
            new(0.54f, 0.76f, 1f, 1f);

        private static readonly Color Blue =
            new(0.38f, 0.62f, 1f, 1f);

        private static readonly Color Green =
            new(0.30f, 1f, 0.58f, 1f);

        private static readonly Color Gold =
            new(1f, 0.78f, 0.18f, 1f);

        private static readonly Color Orange =
            new(1f, 0.56f, 0.16f, 1f);

        private static readonly Color Purple =
            new(0.72f, 0.34f, 1f, 1f);

        private static readonly Color Lilac =
            new(0.72f, 0.56f, 1f, 1f);

        private static readonly Color Magenta =
            new(0.94f, 0.30f, 1f, 1f);

        private static readonly Color Pink =
            new(1f, 0.36f, 0.86f, 1f);
    }
}
