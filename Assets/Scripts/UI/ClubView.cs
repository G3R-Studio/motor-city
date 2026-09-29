using MotorCity.Input;
using MotorCity.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private void BuildClubOverlay(
            Transform canvas)
        {
            clubOverlay =
                new GameObject(
                    "Club Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            clubOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                clubOverlay.GetComponent<RectTransform>();

            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            Image backdrop =
                clubOverlay.GetComponent<Image>();

            backdrop.color =
                new Color(
                    0.005f,
                    0.008f,
                    0.014f,
                    0.78f);

            backdrop.raycastTarget =
                false;

            RectTransform panel =
                CreatePanel(
                    clubOverlay.transform,
                    "Club Panel",
                    Vector2.zero,
                    new Vector2(
                        560f,
                        360f),
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
                    "Club Title",
                    25,
                    FontStyle.Bold,
                    TextAnchor.UpperCenter,
                    new Vector2(
                        0f,
                        -22f),
                    new Vector2(
                        500f,
                        36f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    TextColor);

            title.text =
                MotorCityLocalization.Text(
                    "club.title");

            clubEmblemText =
                CreateText(
                    panel,
                    "Club Emblem",
                    54,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -92f),
                    new Vector2(
                        92f,
                        92f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    BlueAccent);

            clubNameText =
                CreateText(
                    panel,
                    "Club Name",
                    22,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -178f),
                    new Vector2(
                        500f,
                        34f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    TextColor);

            clubDescriptionText =
                CreateText(
                    panel,
                    "Club Description",
                    14,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -220f),
                    new Vector2(
                        480f,
                        48f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    SecondaryTextColor);

            clubWeeklyText =
                CreateText(
                    panel,
                    "Club Weekly",
                    16,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(
                        0f,
                        -270f),
                    new Vector2(
                        480f,
                        34f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Color(
                        0.24f,
                        0.88f,
                        1f,
                        1f));

        }

        private void HandleClubInput()
        {
            if (clubOverlay == null ||
                club == null)
            {
                return;
            }

            if (activityManager == null ||
                !activityManager.SecondaryProgressionAllowed)
            {
                if (clubOverlay.activeSelf)
                {
                    clubOverlay.SetActive(
                        false);

                    RefreshDrivingEnabledForUi();
                }

                return;
            }

            if (MotorCityInput.ToggleClubPressed)
            {
                bool open =
                    !clubOverlay.activeSelf;

                if (open &&
                    ((activityManager != null &&
                      (activityManager.IsBusy ||
                       activityManager.HasResult)) ||
                     (garage != null &&
                      garage.IsOpen)))
                {
                    return;
                }

                clubOverlay.SetActive(
                    open);

                if (open)
                {
                    CloseNavigatorMenuVisualOnly();

                    storeOpen =
                        false;

                    garageOverlay?.SetActive(
                        false);

                    UpdateClubOverlay();
                }

                RefreshDrivingEnabledForUi();
                return;
            }

            if (!clubOverlay.activeSelf)
                return;

            if (MotorCityInput.CancelPressed)
            {
                clubOverlay.SetActive(
                    false);

                RefreshDrivingEnabledForUi();
                return;
            }

            if (MotorCityInput.PreviousVehiclePressed)
            {
                club.CycleBrowse(
                    -1);

                UpdateClubOverlay();
            }

            if (MotorCityInput.NextVehiclePressed)
            {
                club.CycleBrowse(
                    1);

                UpdateClubOverlay();
            }

            if (MotorCityInput.RetryPressed ||
                MotorCityInput.InteractPressed)
            {
                club.JoinBrowseClub();
                UpdateClubOverlay();
            }
        }

        private void UpdateClubOverlay()
        {
            if (club == null ||
                clubOverlay == null ||
                !clubOverlay.activeSelf)
            {
                return;
            }

            clubEmblemText.text =
                club.BrowseClubEmblem;

            clubNameText.text =
                club.BrowseClubName;

            clubDescriptionText.text =
                club.BrowseClubDescription;

            clubWeeklyText.text =
                club.HasClub &&
                club.BrowseClubIndex ==
                    club.JoinedClubIndex
                    ? club.WeeklyLine
                    : club.BrowseClubGoalLine;

            // Navigation/join actions are represented by the buttons below.

        }

        private GameObject BuildClubTouchControls(
            Transform canvas)
        {
            return BuildTouchModalRow(
                canvas,
                "Club Touch Controls",
                "touch.modal.prev",
                MotorCityInputAction.PreviousVehicle,
                "touch.club.join",
                MotorCityInputAction.Interact,
                "touch.modal.next",
                MotorCityInputAction.NextVehicle,
                "touch.modal.close",
                MotorCityInputAction.ToggleClub);
        }

    }
}
