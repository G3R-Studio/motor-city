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

            RectTransform panel =
                CreatePanel(
                    pauseOverlay.transform,
                    "Pause Panel",
                    Vector2.zero,
                    new Vector2(
                        560f,
                        410f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    Color.clear);

            ApplyModalPanelTexture(
                panel);

            Text title =
                CreateText(
                    panel,
                    "Pause Title",
                    UiWindowTitleFontSize,
                    FontStyle.Bold,
                    TextAnchor.UpperCenter,
                    new Vector2(
                        0f,
                        -24f),
                    new Vector2(
                        500f,
                        38f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    TextColor);

            title.text =
                MotorCityLocalization.Text(
                    "pause.title");

            RectTransform qualityCard =
                CreatePanel(
                    panel,
                    "Pause Quality Card",
                    new Vector2(0f, 82f),
                    new Vector2(470f, 64f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        PanelSoftColor.r,
                        PanelSoftColor.g,
                        PanelSoftColor.b,
                        0.72f));

            StylePauseSettingsCard(
                qualityCard);

            Text qualityLabel =
                CreateText(
                    qualityCard,
                    "Pause Quality Label",
                    UiSectionLabelFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-194f, 0f),
                    new Vector2(126f, 36f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Color(
                        0.46f,
                        0.82f,
                        1f,
                        1f));

            qualityLabel.text =
                MotorCityLocalization.Text(
                    "pause.quality_label");

            pauseQualityText =
                CreateText(
                    qualityCard,
                    "Pause Quality",
                    UiValueFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(-6f, 0f),
                    new Vector2(170f, 40f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            CreatePauseButton(
                qualityCard,
                "Pause Quality Previous",
                "pause.minus",
                new Vector2(154f, 0f),
                new Vector2(46f, 38f),
                () =>
                    CycleQuality(-1));

            CreatePauseButton(
                qualityCard,
                "Pause Quality Next",
                "pause.plus",
                new Vector2(207f, 0f),
                new Vector2(46f, 38f),
                () =>
                    CycleQuality(1));

            RectTransform audioCard =
                CreatePanel(
                    panel,
                    "Pause Audio Card",
                    new Vector2(0f, 4f),
                    new Vector2(470f, 64f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        PanelSoftColor.r,
                        PanelSoftColor.g,
                        PanelSoftColor.b,
                        0.72f));

            StylePauseSettingsCard(
                audioCard);

            Text audioLabel =
                CreateText(
                    audioCard,
                    "Pause Audio Label",
                    UiSectionLabelFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-194f, 0f),
                    new Vector2(126f, 36f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Color(
                        0.46f,
                        0.82f,
                        1f,
                        1f));

            audioLabel.text =
                MotorCityLocalization.Text(
                    "pause.audio_label");

            pauseAudioText =
                CreateText(
                    audioCard,
                    "Pause Audio",
                    UiValueFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(-28f, 0f),
                    new Vector2(100f, 40f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            CreatePauseButton(
                audioCard,
                "Pause Volume Down",
                "pause.minus",
                new Vector2(50f, 0f),
                new Vector2(42f, 38f),
                () =>
                    AdjustAudioVolume(-1));

            CreatePauseButton(
                audioCard,
                "Pause Volume Up",
                "pause.plus",
                new Vector2(98f, 0f),
                new Vector2(42f, 38f),
                () =>
                    AdjustAudioVolume(1));

            CreatePauseButton(
                audioCard,
                "Pause Audio Toggle",
                "pause.toggle",
                new Vector2(177f, 0f),
                new Vector2(104f, 38f),
                ToggleAudioMute);

            RectTransform musicCard =
                CreatePanel(
                    panel,
                    "Pause Music Card",
                    new Vector2(0f, -74f),
                    new Vector2(470f, 64f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Color(
                        PanelSoftColor.r,
                        PanelSoftColor.g,
                        PanelSoftColor.b,
                        0.72f));

            StylePauseSettingsCard(
                musicCard);

            Text musicLabel =
                CreateText(
                    musicCard,
                    "Pause Music Label",
                    UiSectionLabelFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(-194f, 0f),
                    new Vector2(126f, 36f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0.5f),
                    new Color(
                        0.46f,
                        0.82f,
                        1f,
                        1f));

            musicLabel.text =
                MotorCityLocalization.Text(
                    "pause.music_label");

            pauseMusicText =
                CreateText(
                    musicCard,
                    "Pause Music",
                    UiValueFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(-28f, 0f),
                    new Vector2(100f, 40f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    TextColor);

            CreatePauseButton(
                musicCard,
                "Pause Music Volume Down",
                "pause.minus",
                new Vector2(50f, 0f),
                new Vector2(42f, 38f),
                () =>
                    AdjustMusicVolume(-1));

            CreatePauseButton(
                musicCard,
                "Pause Music Volume Up",
                "pause.plus",
                new Vector2(98f, 0f),
                new Vector2(42f, 38f),
                () =>
                    AdjustMusicVolume(1));

            CreatePauseButton(
                musicCard,
                "Pause Music Toggle",
                "pause.toggle",
                new Vector2(177f, 0f),
                new Vector2(104f, 38f),
                ToggleMusicMute);

            BuildPauseTouchActions(
                panel);

            RefreshPauseMenuText();
        }

        private void StylePauseSettingsCard(
            RectTransform card)
        {
            if (card == null)
                return;

            Image cardImage =
                card.GetComponent<Image>();

            if (cardImage != null)
            {
                cardImage.color =
                    new Color(
                        0.024f,
                        0.04f,
                        0.078f,
                        0.86f);
            }

            Texture2D glowTexture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.modalPanel;

            if (glowTexture != null)
            {
                CreatePanelEdgeGlow(
                    card,
                    glowTexture,
                    new Color(
                        0.28f,
                        0.58f,
                        1f,
                        1f),
                    "Pause Card Glow");
            }

            Outline outline =
                card.GetComponent<Outline>();

            if (outline == null)
            {
                outline =
                    card.gameObject
                        .AddComponent<Outline>();
            }

            outline.effectColor =
                new Color(
                    0.36f,
                    0.64f,
                    1f,
                    0.20f);
            outline.effectDistance =
                new Vector2(1f, -1f);
            outline.useGraphicAlpha =
                true;

            GameObject accentObject =
                new(
                    "Pause Card Accent",
                    typeof(RectTransform),
                    typeof(Image));

            accentObject.transform.SetParent(
                card,
                false);

            RectTransform accentRect =
                accentObject.GetComponent<RectTransform>();

            accentRect.anchorMin =
                new Vector2(0f, 1f);
            accentRect.anchorMax =
                new Vector2(1f, 1f);
            accentRect.pivot =
                new Vector2(0.5f, 1f);
            accentRect.anchoredPosition =
                Vector2.zero;
            accentRect.sizeDelta =
                new Vector2(0f, 3f);

            Image accentImage =
                accentObject.GetComponent<Image>();

            accentImage.color =
                new Color(
                    0.26f,
                    0.80f,
                    1f,
                    0.90f);
            accentImage.raycastTarget =
                false;
        }

        private void BuildPauseTouchActions(
            Transform panel)
        {
            CreatePauseButton(
                panel,
                "Pause Resume",
                "pause.resume",
                new Vector2(0f, -164f),
                new Vector2(300f, 50f),
                ClosePauseMenu);
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

            Sprite utilityIcon =
                objectName == "HUD Pause"
                    ? MotorCityIconLibrary.Pause
                    : objectName == "HUD More"
                        ? MotorCityIconLibrary.More
                        : null;

            if (utilityIcon != null)
            {
                CreateHudIcon(
                    rect,
                    "Action Icon",
                    utilityIcon,
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
                    utilityIcon != null
                        ? new Vector2(9f, 0f)
                        : Vector2.zero,
                    size -
                    new Vector2(
                        utilityIcon != null
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
        }

        private void OpenPauseMenu()
        {
            pauseMenuOpen =
                true;

            MotorCityPlatformRuntime.SetGameplayUiPaused(
                true);

            pauseStoredTimeScale =
                Time.timeScale;

            Time.timeScale =
                0f;

            FreezeWorldPresentation();

            AudioListener.pause =
                true;

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

            car?.SetDrivingEnabled(
                false);

            RefreshPauseMenuText();
        }

        private void ClosePauseMenu()
        {
            pauseMenuOpen =
                false;

            MotorCityPlatformRuntime.SetGameplayUiPaused(
                HasBlockingModalUi());

            pauseOverlay?.SetActive(
                false);

            Time.timeScale =
                pauseStoredTimeScale;

            ResumeWorldPresentation();

            if (Time.timeScale > 0f)
            {
                AudioListener.pause =
                    false;
            }

            car?.SetDrivingEnabled(
                Time.timeScale > 0f);

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

        private void FreezeWorldPresentation()
        {
            pausedAnimatorSpeeds.Clear();

            Animator[] animators =
                Object.FindObjectsByType<Animator>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (Animator animator in animators)
            {
                if (animator == null ||
                    (pauseOverlay != null &&
                     animator.transform.IsChildOf(
                         pauseOverlay.transform)))
                {
                    continue;
                }

                pausedAnimatorSpeeds[animator] =
                    animator.speed;

                animator.speed =
                    0f;
            }

            pausedParticleSystems.Clear();

            ParticleSystem[] particles =
                Object.FindObjectsByType<ParticleSystem>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (ParticleSystem particle in particles)
            {
                if (particle == null ||
                    !particle.isPlaying)
                {
                    continue;
                }

                particle.Pause(
                    true);

                pausedParticleSystems.Add(
                    particle);
            }
        }

        private void ResumeWorldPresentation()
        {
            foreach (KeyValuePair<Animator, float> pair in
                     pausedAnimatorSpeeds)
            {
                if (pair.Key != null)
                {
                    pair.Key.speed =
                        pair.Value;
                }
            }

            pausedAnimatorSpeeds.Clear();

            foreach (ParticleSystem particle in
                     pausedParticleSystems)
            {
                if (particle != null)
                {
                    particle.Play(
                        true);
                }
            }

            pausedParticleSystems.Clear();
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
