using System.Collections.Generic;
using MotorCity.Audio;
using MotorCity.Input;
using MotorCity.Localization;
using MotorCity.Persistence;
using MotorCity.Platform;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        // Pause owns time/audio state while the shared front-end view is open.
        private MotorCityFrontEndFlow pauseExperienceFrontEnd;
        private bool pauseExperienceOpen;

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
                    0.76f);

            backdrop.raycastTarget =
                true;

            MotorCityModalWindow window =
                CreateModalWindow(
                    pauseOverlay.transform,
                    "Pause Panel",
                    "pause.title",
                    new Vector2(
                        560f,
                        488f));

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

            RectTransform experienceActions =
                CreateButtonRow(
                    window.Content,
                    "Pause Player Experience Actions");

            CreatePrimaryButton(
                experienceActions,
                "Pause Player Experience",
                "ux.home.button",
                OpenPausePlayerExperience,
                role: MotorCityButtonRole.Secondary);

            RectTransform actions =
                CreateButtonRow(
                    window.Content,
                    "Pause Actions");

            CreatePrimaryButton(
                actions,
                "Pause Main Menu",
                "pause.main_menu",
                OpenPauseMainMenu,
                role: MotorCityButtonRole.Secondary);

            CreatePrimaryButton(
                actions,
                "Pause Resume",
                "pause.resume",
                ClosePauseMenu,
                role: MotorCityButtonRole.Primary);

            RefreshPauseMenuText();
        }

        private void OpenPausePlayerExperience()
        {
            if (!pauseMenuOpen || pauseExperienceOpen)
                return;

            // Use the exact same read-only front-end screen as the main menu.
            // Keep PauseMenu's Time.timeScale/audio/driving block unchanged.
            MotorCityFrontEndFlow frontEnd =
                Object.FindAnyObjectByType<MotorCityFrontEndFlow>();

            if (frontEnd == null ||
                !frontEnd.OpenPlayerExperienceFromPause(
                    ReturnFromPausePlayerExperience))
            {
                return;
            }

            pauseExperienceFrontEnd = frontEnd;
            pauseExperienceOpen = true;
            pauseOverlay?.SetActive(false);
            MotorCityInput.ClearVirtualState();
        }

        private void ReturnFromPausePlayerExperience()
        {
            pauseExperienceOpen = false;
            pauseExperienceFrontEnd = null;
            MotorCityInput.ClearVirtualState();

            if (pauseMenuOpen)
            {
                pauseOverlay?.SetActive(true);
                RefreshPauseMenuText();

                // When the shared Canvas closes, EventSystem can still
                // point at a now-hidden Player Experience action. Restore
                // the pause action that opened it for keyboard/gamepad.
                if (EventSystem.current != null && pauseOverlay != null)
                {
                    foreach (Button button in
                        pauseOverlay.GetComponentsInChildren<Button>())
                    {
                        if (button.gameObject.name !=
                            "Pause Player Experience" ||
                            !button.IsActive() || !button.interactable)
                            continue;

                        EventSystem.current.SetSelectedGameObject(
                            button.gameObject);
                        break;
                    }
                }
            }
        }

        private void OpenPauseMainMenu()
        {
            MotorCityInput.ClearVirtualState();

            // Return from the shared career modal before changing menu
            // ownership. Both pause and career callbacks are now cleared.
            if (pauseExperienceOpen && pauseExperienceFrontEnd != null)
                pauseExperienceFrontEnd.ClosePlayerExperienceFromPause();

            pauseExperienceOpen = false;
            pauseExperienceFrontEnd = null;

            MotorCityFrontEndFlow frontEnd =
                Object.FindAnyObjectByType<
                    MotorCityFrontEndFlow>();

            // If the front-end has disappeared, use the normal resume
            // path rather than leaving pause/audio flags half-cleared.
            if (frontEnd == null)
            {
                ClosePauseMenu();
                return;
            }

            pauseMenuOpen = false;
            pauseOverlay?.SetActive(false);

            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != null &&
                pauseOverlay != null &&
                EventSystem.current.currentSelectedGameObject.transform
                    .IsChildOf(pauseOverlay.transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            MotorCityMusicRuntime.SetPauseMenuPaused(false);
            car?.SetDrivingBlocked("PauseMenu", false);
            MotorCityPlatformRuntime.SetGameplayUiPaused(false);

            frontEnd.ShowMainMenuFromGarage();
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

            bool hudUtilityButton =
                objectName == "HUD Pause" ||
                objectName == "HUD More";

            Button button =
                buttonObject.GetComponent<Button>();

            MotorCityUiSkin.ApplyVectorButton(
                button,
                MotorCityButtonRole.Utility);

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
            if (pauseExperienceOpen &&
                pauseExperienceFrontEnd != null)
            {
                pauseExperienceFrontEnd.ClosePlayerExperienceFromPause();
            }

            // The front-end can already have been destroyed on a scene
            // transition. Never carry a stale modal owner into next pause.
            pauseExperienceOpen = false;
            pauseExperienceFrontEnd = null;

            pauseMenuOpen =
                false;

            pauseOverlay?.SetActive(
                false);

            // No hidden menu button should remain selected while driving.
            if (EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != null &&
                pauseOverlay != null &&
                EventSystem.current.currentSelectedGameObject.transform
                    .IsChildOf(pauseOverlay.transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

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

            // OpenPauseMenu marks gameplay UI as paused. Restore that
            // platform flag on normal Resume as well as on Main Menu,
            // including the route through Player Experience.
            MotorCityPlatformRuntime.SetGameplayUiPaused(
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
            if (pauseExperienceOpen)
            {
                // A scene transition can destroy the shared front-end
                // while PauseMenu is still alive. Never trap the player
                // behind a hidden overlay with Cancel swallowed.
                if (pauseExperienceFrontEnd == null)
                {
                    pauseExperienceOpen = false;
                    pauseOverlay?.SetActive(true);
                    RefreshPauseMenuText();
                }
                else
                {
                    if (MotorCityInput.CancelPressed)
                        pauseExperienceFrontEnd.OnPlayerExperienceBackPressed();
                    return;
                }
            }

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
