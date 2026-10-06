using System.Collections.Generic;
using MotorCity.Audio;
using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Persistence;
using MotorCity.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private void BuildPauseMenu(
            Transform canvas)
        {
            pauseOverlay =
                new GameObject(
                    "Pause Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            pauseOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                pauseOverlay.GetComponent<RectTransform>();

            overlay.anchorMin =
                Vector2.zero;

            overlay.anchorMax =
                Vector2.one;

            overlay.offsetMin =
                Vector2.zero;

            overlay.offsetMax =
                Vector2.zero;

            Image backdrop =
                pauseOverlay.GetComponent<Image>();

            backdrop.color =
                new Color(
                    0.005f,
                    0.008f,
                    0.012f,
                    0.82f);

            backdrop.raycastTarget =
                true;

            MotorCityModalWindow window =
                CreateModalWindow(
                    pauseOverlay.transform,
                    "Pause Panel",
                    "pause.title",
                    new Vector2(
                        560f,
                        410f));

            MotorCitySettingsRow qualityRow =
                CreateSettingsRow(
                    window.Content,
                    "Pause Quality",
                    "pause.quality_label");

            pauseQualityText =
                qualityRow.Value;

            CreatePrimaryButton(
                qualityRow.Controls,
                "Pause Quality Previous",
                "pause.minus",
                () =>
                    CycleQuality(-1),
                46f,
                true);

            CreatePrimaryButton(
                qualityRow.Controls,
                "Pause Quality Next",
                "pause.plus",
                () =>
                    CycleQuality(1),
                46f,
                true);

            MotorCitySettingsRow audioRow =
                CreateSettingsRow(
                    window.Content,
                    "Pause Audio",
                    "pause.audio_label");

            pauseAudioText =
                audioRow.Value;

            CreatePrimaryButton(
                audioRow.Controls,
                "Pause Volume Down",
                "pause.minus",
                () =>
                    AdjustAudioVolume(-1),
                42f,
                true);

            CreatePrimaryButton(
                audioRow.Controls,
                "Pause Volume Up",
                "pause.plus",
                () =>
                    AdjustAudioVolume(1),
                42f,
                true);

            CreatePrimaryButton(
                audioRow.Controls,
                "Pause Audio Toggle",
                "pause.toggle",
                ToggleAudioMute,
                104f);

            MotorCitySettingsRow musicRow =
                CreateSettingsRow(
                    window.Content,
                    "Pause Music",
                    "pause.music_label");

            pauseMusicText =
                musicRow.Value;

            CreatePrimaryButton(
                musicRow.Controls,
                "Pause Music Volume Down",
                "pause.minus",
                () =>
                    AdjustMusicVolume(-1),
                42f,
                true);

            CreatePrimaryButton(
                musicRow.Controls,
                "Pause Music Volume Up",
                "pause.plus",
                () =>
                    AdjustMusicVolume(1),
                42f,
                true);

            CreatePrimaryButton(
                musicRow.Controls,
                "Pause Music Toggle",
                "pause.toggle",
                ToggleMusicMute,
                104f);

            RectTransform actions =
                CreateButtonRow(
                    window.Content,
                    "Pause Actions");

            CreatePrimaryButton(
                actions,
                "Pause Resume",
                "pause.resume",
                ClosePauseMenu);

            CreatePrimaryButton(
                actions,
                "Pause Main Menu",
                "pause.main_menu",
                OpenPauseMainMenu);

            RefreshPauseMenuText();
        }

        private void OpenPauseMainMenu()
        {
            MotorCityInput.ClearVirtualState();

            pauseMenuOpen =
                false;

            pauseOverlay?.SetActive(
                false);

            MotorCityMusicRuntime.SetPauseMenuPaused(
                false);

            car?.SetDrivingBlocked(
                "PauseMenu",
                false);

            MotorCityPlatformRuntime.SetGameplayUiPaused(
                false);

            MotorCityFrontEndFlow frontEnd =
                Object.FindAnyObjectByType<
                    MotorCityFrontEndFlow>();

            if (frontEnd != null)
            {
                frontEnd.ShowMainMenuFromGarage();
                return;
            }

            ClosePauseMenu();
        }

        private void CreatePauseButton(
            Transform parent,
            string objectName,
            string localizationKey,
            Vector2 anchoredPosition,
            Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject =
                new(
                    objectName,
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

            bool hudUtilityButton =
                objectName == "HUD Pause" ||
                objectName == "HUD More";

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
                        hudUtilityButton
                            ? 0.94f
                            : 0.96f);
            }

            Button button =
                buttonObject.GetComponent<Button>();

            button.targetGraphic =
                image;

            ColorBlock buttonColors =
                button.colors;

            buttonColors.normalColor =
                Color.white;
            buttonColors.highlightedColor =
                new Color(
                    0.96f,
                    0.96f,
                    1f,
                    1f);
            buttonColors.pressedColor =
                new Color(
                    0.82f,
                    0.80f,
                    0.92f,
                    1f);
            buttonColors.selectedColor =
                buttonColors.highlightedColor;

            button.colors =
                buttonColors;

            if (buttonObject.GetComponent<UiButtonFeedback>() == null)
            {
                buttonObject.AddComponent<UiButtonFeedback>();
            }

            button.onClick.AddListener(
                action);

            bool allowUtilityIcon =
                objectName == "HUD Pause" ||
                objectName == "HUD More";

            GarageReferenceGraphic.Symbol utilityIcon =
                GarageReferenceGraphic.Symbol.Pause;

            Color utilityIconColor =
                TextColor;

            bool hasUtilityIcon =
                allowUtilityIcon &&
                MotorCityButtonVisuals.TryForSemantic(
                    objectName + " " + localizationKey,
                    out utilityIcon,
                    out utilityIconColor);

            if (hasUtilityIcon)
            {
                RectTransform iconRect =
                    GarageObject(
                        rect,
                        "Action Icon");

                iconRect.anchorMin =
                    iconRect.anchorMax =
                    iconRect.pivot =
                        new Vector2(
                            0f,
                            0.5f);

                iconRect.anchoredPosition =
                    new Vector2(
                        10f,
                        0f);

                iconRect.sizeDelta =
                    new Vector2(
                        20f,
                        20f);

                GarageReferenceGraphic icon =
                    iconRect.gameObject.AddComponent<
                        GarageReferenceGraphic>();

                icon.symbol =
                    utilityIcon;

                icon.color =
                    utilityIconColor;

                icon.raycastTarget =
                    false;
            }

            Text text =
                CreateText(
                    rect,
                    "Label",
                    objectName == "Pause Quality Previous" ||
                    objectName == "Pause Quality Next" ||
                    objectName == "Pause Volume Down" ||
                    objectName == "Pause Volume Up"
                        ? UiAdjustButtonFontSize
                        : hudUtilityButton
                            ? UiHudButtonFontSize
                            : UiButtonFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    hasUtilityIcon
                        ? new Vector2(10f, 0f)
                        : Vector2.zero,
                    size -
                    new Vector2(
                        hasUtilityIcon
                            ? 32f
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
        }

        private void OpenPauseMenu()
        {
            pauseMenuOpen =
                true;

            MotorCityInput.ClearVirtualState();

            pauseStoredTimeScale =
                Time.timeScale;

            Time.timeScale =
                0f;

            AudioListener.pause =
                true;

            MotorCityMusicRuntime.SetPauseMenuPaused(
                true);

            pauseOverlay?.SetActive(
                true);

            touchControlsRoot?.SetActive(
                false);

            touchUtilityRoot?.SetActive(
                false);

            hudQuickMenuOpen =
                false;

            hudQuickMenuRoot?.SetActive(
                false);

            touchActivityCancelRoot?.SetActive(
                false);

            touchPauseRoot?.SetActive(
                false);

            car?.SetDrivingBlocked(
                "PauseMenu",
                true);

            MotorCityPlatformRuntime.SetGameplayUiPaused(
                true);

            RefreshPauseMenuText();
        }

        private void ClosePauseMenu()
        {
            pauseMenuOpen =
                false;

            pauseOverlay?.SetActive(
                false);

            Time.timeScale =
                MotorCityPlatformRuntime.IsGameplayResumeReady
                    ? pauseStoredTimeScale
                    : 0f;

            if (Time.timeScale > 0f)
            {
                AudioListener.pause =
                    false;
            }

            MotorCityMusicRuntime.SetPauseMenuPaused(
                false);

            MotorCityInput.ClearVirtualState();

            car?.SetDrivingBlocked(
                "PauseMenu",
                false);

            RefreshDrivingEnabledForUi();

            UpdateTouchControlsVisibility();
        }

        private void HandlePauseMenuInput()
        {
            if (MotorCityInput.CancelPressed)
            {
                ClosePauseMenu();
                return;
            }

            if (MotorCityInput.PreviousVehiclePressed)
            {
                CycleQuality(
                    -1);
            }

            if (MotorCityInput.NextVehiclePressed)
            {
                CycleQuality(
                    1);
            }

        }

        private void ToggleAudioMute()
        {
            audioMuted =
                !audioMuted;

            ApplyAudioVolume();

            MotorCitySaveService.SetInt(
                AudioMutedSaveKey,
                audioMuted
                    ? 1
                    : 0);

            MotorCitySaveService.Save();

            RefreshPauseMenuText();
        }

        private void AdjustAudioVolume(
            int direction)
        {
            float stepped =
                Mathf.Round(
                    (audioVolume +
                     direction * 0.1f) *
                    10f) /
                10f;

            audioVolume =
                Mathf.Clamp01(
                    stepped);

            ApplyAudioVolume();

            MotorCitySaveService.SetFloat(
                AudioVolumeSaveKey,
                audioVolume);

            MotorCitySaveService.Save();

            RefreshPauseMenuText();
        }

        private void AdjustMusicVolume(
            int direction)
        {
            MotorCityMusicRuntime.AdjustVolume(
                direction);

            RefreshPauseMenuText();
        }

        private void ToggleMusicMute()
        {
            MotorCityMusicRuntime.ToggleMute();
            RefreshPauseMenuText();
        }

        private void ApplyAudioVolume()
        {
            AudioListener.volume =
                audioMuted
                    ? 0f
                    : audioVolume;
        }

        private void CycleQuality(
            int direction)
        {
            int current =
                (int)MotorCityQualityRuntime.CurrentPreset;

            int next =
                (current + direction + 3) %
                3;

            MotorCityQualityRuntime.Apply(
                (MotorCityQualityPreset)next,
                true);

            RefreshPauseMenuText();
        }

        private void RefreshPauseMenuText()
        {
            if (pauseQualityText != null)
            {
                string quality =
                    MotorCityQualityRuntime.CurrentPreset switch
                    {
                        MotorCityQualityPreset.Low =>
                            MotorCityLocalization.Text(
                                "pause.quality_low"),

                        MotorCityQualityPreset.High =>
                            MotorCityLocalization.Text(
                                "pause.quality_high"),

                        _ =>
                            MotorCityLocalization.Text(
                                "pause.quality_medium")
                    };

                pauseQualityText.text =
                    quality;
            }

            if (pauseAudioText != null)
            {
                int percent =
                    Mathf.RoundToInt(
                        audioVolume *
                        100f);

                pauseAudioText.text =
                    MotorCityLocalization.Format(
                        audioMuted
                            ? "pause.audio_muted_value"
                            : "pause.audio_value",
                        percent);
            }

            if (pauseMusicText != null)
            {
                int percent =
                    Mathf.RoundToInt(
                        MotorCityMusicRuntime.Volume *
                        100f);

                pauseMusicText.text =
                    MotorCityLocalization.Format(
                        MotorCityMusicRuntime.Muted
                            ? "pause.audio_muted_value"
                            : "pause.audio_value",
                        percent);
            }
        }

    }
}
