using System.Collections.Generic;
using MotorCity.World;
using MotorCity.Input;
using MotorCity.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed partial class PrototypeHud
    {
        private float minimapRouteOpacity;
        private void CloseNavigatorMenuVisualOnly()
        {
            SetNavigatorMenuOpen(
                false);
        }

        private void BuildNavigatorMenu(
            Transform canvas)
        {
            navigatorMenuOverlay =
                new GameObject(
                    "Navigator Menu Overlay",
                    typeof(RectTransform),
                    typeof(Image));

            navigatorMenuOverlay.transform.SetParent(
                canvas,
                false);

            RectTransform overlay =
                navigatorMenuOverlay.GetComponent<RectTransform>();

            overlay.anchorMin =
                Vector2.zero;
            overlay.anchorMax =
                Vector2.one;
            overlay.offsetMin =
                Vector2.zero;
            overlay.offsetMax =
                Vector2.zero;

            Image backdrop =
                navigatorMenuOverlay.GetComponent<Image>();

            backdrop.color =
                new Color(
                    0.005f,
                    0.008f,
                    0.012f,
                    0.72f);
            backdrop.raycastTarget =
                false;

            RectTransform panel =
                CreatePanel(
                    navigatorMenuOverlay.transform,
                    "Navigator Menu",
                    Vector2.zero,
                    new Vector2(
                        560f,
                        300f),
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
                    "Navigator Title",
                    26,
                    FontStyle.Bold,
                    TextAnchor.UpperCenter,
                    new Vector2(
                        0f,
                        -22f),
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
                    "navigator.title");

            navigatorIndexText =
                CreateText(
                    panel,
                    "Navigator Index",
                    13,
                    FontStyle.Bold,
                    TextAnchor.UpperCenter,
                    new Vector2(
                        0f,
                        -56f),
                    new Vector2(
                        220f,
                        26f),
                    new Vector2(
                        0.5f,
                        1f),
                    new Vector2(
                        0.5f,
                        1f),
                    SecondaryTextColor);

            RectTransform card =
                CreatePanel(
                    panel,
                    "Navigator Destination Card",
                    new Vector2(
                        0f,
                        -5f),
                    new Vector2(
                        478f,
                        140f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Color(
                        0.024f,
                        0.04f,
                        0.078f,
                        0.88f));

            StyleNavigatorDestinationCard(
                card);

            RectTransform iconPlate =
                CreatePanel(
                    card,
                    "Navigator Icon Plate",
                    new Vector2(
                        26f,
                        0f),
                    new Vector2(
                        88f,
                        88f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Color(
                        GarageAccent.r * 0.28f,
                        GarageAccent.g * 0.28f,
                        GarageAccent.b * 0.34f,
                        0.92f));

            navigatorIconPlateImage =
                iconPlate.GetComponent<Image>();

            navigatorIconImage =
                CreateHudIcon(
                    iconPlate,
                    "Navigator Icon",
                    MotorCityIconLibrary.Garage,
                    Vector2.zero,
                    new Vector2(
                        48f,
                        48f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    TextColor);

            navigatorCategoryText =
                CreateText(
                    card,
                    "Navigator Category",
                    12,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(
                        126f,
                        38f),
                    new Vector2(
                        314f,
                        22f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Vector2(
                        0f,
                        0.5f),
                    GarageAccent);

            navigatorMenuText =
                CreateText(
                    card,
                    "Navigator Selection",
                    24,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(
                        126f,
                        7f),
                    new Vector2(
                        320f,
                        36f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Vector2(
                        0f,
                        0.5f),
                    TextColor);

            navigatorMenuText.resizeTextForBestFit =
                false;
            navigatorMenuText.resizeTextMinSize =
                16;
            navigatorMenuText.resizeTextMaxSize =
                24;
            navigatorMenuText.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            navigatorMenuText.verticalOverflow =
                VerticalWrapMode.Overflow;

            navigatorDescriptionText =
                CreateText(
                    card,
                    "Navigator Description",
                    13,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    new Vector2(
                        126f,
                        -34f),
                    new Vector2(
                        320f,
                        48f),
                    new Vector2(
                        0f,
                        0.5f),
                    new Vector2(
                        0f,
                        0.5f),
                    SecondaryTextColor);

            navigatorDescriptionText.resizeTextForBestFit =
                false;
            navigatorDescriptionText.resizeTextMinSize =
                10;
            navigatorDescriptionText.resizeTextMaxSize =
                13;
            navigatorDescriptionText.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            navigatorDescriptionText.verticalOverflow =
                VerticalWrapMode.Overflow;

            navigatorDistanceText =
                CreateText(
                    panel,
                    "Navigator Distance",
                    13,
                    FontStyle.Bold,
                    TextAnchor.LowerCenter,
                    new Vector2(
                        0f,
                        22f),
                    new Vector2(
                        360f,
                        28f),
                    new Vector2(
                        0.5f,
                        0f),
                    new Vector2(
                        0.5f,
                        0f),
                    SecondaryTextColor);

            UpdateNavigatorMenuText();
        }

        private void StyleNavigatorDestinationCard(
            RectTransform card)
        {
            if (card == null)
                return;

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
                    0.40f,
                    0.66f,
                    1f,
                    0.25f);
            outline.effectDistance =
                new Vector2(1f, -1f);
            outline.useGraphicAlpha =
                true;


        }

        private void HandleNavigatorMenu()
        {
            if (MotorCityInput.ToggleNavigatorPressed)
            {
                ToggleNavigatorMenu();
                return;
            }

            if (!navigatorMenuOpen)
                return;

            if (MotorCityInput.CancelPressed)
            {
                CloseNavigatorMenu();
                return;
            }

            int count =
                NavigatorDestinationCount();

            if (count <= 0)
                return;

            if (MotorCityInput.PreviousVehiclePressed)
            {
                navigatorSelection =
                    (navigatorSelection - 1 + count) %
                    count;

                UpdateNavigatorMenuText();
            }

            if (MotorCityInput.NextVehiclePressed)
            {
                navigatorSelection =
                    (navigatorSelection + 1) %
                    count;

                UpdateNavigatorMenuText();
            }

            if (MotorCityInput.RetryPressed)
            {
                if (TryResolveNavigatorDestination(
                        navigatorSelection,
                        out Vector3 target,
                        out string label))
                {
                    manualNavigationTarget =
                        target;
                    manualNavigationLabel =
                        label;
                    manualNavigationMarkerId =
                        NavigatorMarkerId(
                            navigatorSelection);
                    manualNavigationActive =
                        true;
                }

                CloseNavigatorMenu();
            }
        }

        private void ToggleNavigatorMenu()
        {
            if (navigatorMenuOpen)
            {
                CloseNavigatorMenu();
                return;
            }

            if (activityManager != null &&
                activityManager.IsBusy)
            {
                return;
            }

            if (garage != null &&
                garage.IsOpen)
            {
                return;
            }

            SetNavigatorMenuOpen(
                true);

            SetClubOpen(
                false);

            SetStoreOpen(
                false);

            RefreshDrivingEnabledForUi();

            UpdateNavigatorMenuText();
        }

        private void CloseNavigatorMenu()
        {
            CloseNavigatorMenuVisualOnly();
            RefreshDrivingEnabledForUi();
        }

        private int NavigatorDestinationCount()
        {
            return
                7 +
                (professions != null
                    ? professions.StartCount
                    : 0);
        }

        private void UpdateNavigatorMenuText()
        {
            if (navigatorMenuText == null)
                return;

            int count =
                NavigatorDestinationCount();

            if (count <= 0)
            {
                navigatorMenuText.text =
                    MotorCityLocalization.Text(
                        "navigator.empty");

                if (navigatorIndexText != null)
                    navigatorIndexText.text = string.Empty;
                if (navigatorCategoryText != null)
                    navigatorCategoryText.text = string.Empty;
                if (navigatorDescriptionText != null)
                    navigatorDescriptionText.text = string.Empty;
                if (navigatorDistanceText != null)
                    navigatorDistanceText.text = string.Empty;
                if (navigatorIconImage != null)
                    navigatorIconImage.enabled = false;

                return;
            }

            navigatorSelection =
                Mathf.Clamp(
                    navigatorSelection,
                    0,
                    count - 1);

            bool resolved =
                TryResolveNavigatorDestination(
                    navigatorSelection,
                    out Vector3 target,
                    out string label);

            navigatorMenuText.text =
                resolved
                    ? label
                    : MotorCityLocalization.Text(
                        "navigator.empty");

            if (navigatorIndexText != null)
            {
                navigatorIndexText.text =
                    MotorCityLocalization.Format(
                        "navigator.counter",
                        navigatorSelection + 1,
                        count);
            }

            string markerId =
                NavigatorMarkerId(
                    navigatorSelection);

            if (navigatorCategoryText != null)
            {
                navigatorCategoryText.text =
                    MotorCityLocalization.Text(
                        NavigatorCategoryKey(
                            markerId));
            }

            if (navigatorDescriptionText != null)
            {
                navigatorDescriptionText.text =
                    MotorCityLocalization.Text(
                        NavigatorDescriptionKey(
                            markerId));
            }

            Color markerColor =
                MotorCityIconLibrary.WorldMarkerColor(
                    markerId);

            if (navigatorCategoryText != null)
            {
                navigatorCategoryText.color =
                    markerColor;
            }

            if (navigatorIconPlateImage != null)
            {
                navigatorIconPlateImage.color =
                    new Color(
                        markerColor.r,
                        markerColor.g,
                        markerColor.b,
                        0.14f);
            }

            if (navigatorIconImage != null)
            {
                Sprite markerSprite =
                    MotorCityIconLibrary.ForWorldMarker(
                        markerId);

                navigatorIconImage.sprite =
                    markerSprite;

                navigatorIconImage.color =
                    markerColor;

                navigatorIconImage.enabled =
                    markerSprite != null;
            }

            if (navigatorDistanceText != null)
            {
                if (!resolved ||
                    car == null)
                {
                    navigatorDistanceText.text =
                        string.Empty;
                }
                else
                {
                    float distance =
                        FlatDistance(
                            car.transform.position,
                            target);

                    navigatorDistanceText.text =
                        distance >= 1000f
                            ? MotorCityLocalization.Format(
                                "navigator.distance_km",
                                distance / 1000f)
                            : MotorCityLocalization.Format(
                                "navigator.distance_m",
                                Mathf.RoundToInt(
                                    distance));
                }
            }
        }

        private static string NavigatorCategoryKey(
            string markerId)
        {
            return markerId switch
            {
                "garage" =>
                    "navigator.category.service",
                "delivery" or
                "tow" or
                "carwash" or
                "profession" =>
                    "navigator.category.job",
                "drift" or
                "sprint" or
                "circuit" =>
                    "navigator.category.activity",
                _ =>
                    "navigator.category.place"
            };
        }

        private static string NavigatorDescriptionKey(
            string markerId)
        {
            return markerId switch
            {
                "garage" =>
                    "navigator.desc.garage",
                "delivery" =>
                    "navigator.desc.delivery",
                "drift" =>
                    "navigator.desc.drift",
                "sprint" =>
                    "navigator.desc.sprint",
                "circuit" =>
                    "navigator.desc.circuit",
                "tow" =>
                    "navigator.desc.tow",
                "carwash" =>
                    "navigator.desc.carwash",
                _ =>
                    "navigator.desc.profession"
            };
        }

        private bool TryResolveNavigatorDestination(
            int index,
            out Vector3 target,
            out string label)
        {
            target =
                car != null
                    ? car.transform.position
                    : Vector3.zero;

            label =
                MotorCityLocalization.Text(
                    "hud.free_drive");

            switch (index)
            {
                case 0:
                    if (garage == null)
                        return false;

                    target =
                        garage.GarageCenter;
                    label =
                        MotorCityLocalization.Text(
                            "hud.garage");
                    return true;

                case 1:
                    if (delivery == null)
                        return false;

                    target =
                        delivery.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "activity.delivery");
                    return true;

                case 2:
                    if (driftChallenge == null)
                        return false;

                    target =
                        driftChallenge.ZoneCenter;
                    label =
                        MotorCityLocalization.Text(
                            "activity.drift");
                    return true;

                case 3:
                    if (streetSprint == null)
                        return false;

                    target =
                        streetSprint.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.sprint");
                    return true;

                case 4:
                    if (circuitRace == null)
                        return false;

                    target =
                        circuitRace.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.circuit");
                    return true;

                case 5:
                    if (towTruck == null)
                        return false;

                    target =
                        towTruck.StartPoint;
                    label =
                        MotorCityLocalization.Text(
                            "tow.title");
                    return true;

                case 6:
                    if (carWash == null)
                        return false;

                    target =
                        carWash.StartPoint;
                    label =
                        MotorCityLocalization.Text(
                            "carwash.title");
                    return true;
            }

            int professionIndex =
                index - 7;

            if (professions == null ||
                professionIndex < 0 ||
                professionIndex >=
                professions.StartCount)
            {
                return false;
            }

            target =
                professions.GetStartPoint(
                    professionIndex);

            label =
                professions.GetStartName(
                    professionIndex);

            return true;
        }

        private string NavigatorMarkerId(
            int index)
        {
            return index switch
            {
                0 => "garage",
                1 => "delivery",
                2 => "drift",
                3 => "sprint",
                4 => "circuit",
                5 => "tow",
                6 => "carwash",
                _ => "profession"
            };
        }

        private string ResolveMinimapMarkerId(
            Vector3 target)
        {
            const float matchDistance =
                12f;

            if (manualNavigationActive &&
                FlatDistance(
                    target,
                    manualNavigationTarget) <=
                matchDistance)
            {
                return string.IsNullOrWhiteSpace(
                        manualNavigationMarkerId)
                    ? "profession"
                    : manualNavigationMarkerId;
            }

            if (towTruck != null &&
                towTruck.IsActive)
            {
                return "tow";
            }

            if (carWash != null &&
                carWash.IsActive)
            {
                return "carwash";
            }

            if (professions != null &&
                professions.IsActive)
            {
                return "profession";
            }

            if (underground != null &&
                (underground.HasActiveInvitation ||
                 underground.IsActive ||
                 underground.IsCountingDown))
            {
                return "underground";
            }

            if (delivery != null &&
                FlatDistance(
                    target,
                    delivery.CurrentTarget) <=
                matchDistance)
            {
                return "delivery";
            }

            if (driftChallenge != null &&
                FlatDistance(
                    target,
                    driftChallenge.ZoneCenter) <=
                matchDistance)
            {
                return "drift";
            }

            if (streetSprint != null &&
                FlatDistance(
                    target,
                    streetSprint.CurrentTarget) <=
                matchDistance)
            {
                return "sprint";
            }

            if (circuitRace != null &&
                FlatDistance(
                    target,
                    circuitRace.CurrentTarget) <=
                matchDistance)
            {
                return "circuit";
            }

            if (towTruck != null &&
                FlatDistance(
                    target,
                    towTruck.StartPoint) <=
                matchDistance)
            {
                return "tow";
            }

            if (carWash != null &&
                FlatDistance(
                    target,
                    carWash.StartPoint) <=
                matchDistance)
            {
                return "carwash";
            }

            if (garage != null &&
                FlatDistance(
                    target,
                    garage.GarageCenter) <=
                matchDistance)
            {
                return "garage";
            }

            if (speedTraps != null)
            {
                for (int i = 0;
                     i < speedTraps.TrapCount;
                     i++)
                {
                    if (FlatDistance(
                            target,
                            speedTraps.GetTrapPosition(i)) <=
                        matchDistance)
                    {
                        return "speedtrap";
                    }
                }
            }

            if (driftSpots != null)
            {
                for (int i = 0;
                     i < driftSpots.SpotCount;
                     i++)
                {
                    if (FlatDistance(
                            target,
                            driftSpots.GetSpotPosition(i)) <=
                        matchDistance)
                    {
                        return "driftspot";
                    }
                }
            }

            if (discoveries != null)
            {
                for (int i = 0;
                     i < discoveries.DiscoveryCount;
                     i++)
                {
                    if (FlatDistance(
                            target,
                            discoveries.GetDiscoveryPosition(i)) <=
                        matchDistance)
                    {
                        return "discovery";
                    }
                }
            }

            if (professions != null)
            {
                for (int i = 0;
                     i < professions.StartCount;
                     i++)
                {
                    if (FlatDistance(
                            target,
                            professions.GetStartPoint(i)) <=
                        matchDistance)
                    {
                        return "profession";
                    }
                }
            }

            return "discovery";
        }

        private float MinimapWorldScale(
            float worldRadius)
        {
            if (minimapImage == null ||
                worldRadius <= 0.01f)
            {
                return 1f;
            }

            float diameter =
                worldRadius * 2f;

            return
                minimapImage.rectTransform.rect.width /
                diameter;
        }

        private void UpdateRoadRoute(
            Vector3 carPosition,
            Vector3 target,
            float yaw,
            float worldRadius,
            bool showRoadRoute)
        {
            if (!showRoadRoute ||
                minimapRouteDots.Length == 0)
            {
                minimapRouteUpdateTimer =
                    0f;
                minimapDistanceUpdateTimer =
                    0f;

                HideRouteDots();
                ClearFixedRoadRoute();
                return;
            }

            EnsureFixedRoadRoute(
                carPosition,
                target);

            if (!fixedRoadRouteValid ||
                fixedRoadRoute.Count < 2)
            {
                HideRouteDots();
                return;
            }

            AdvanceFixedRoadRouteProgress(
                carPosition);

            const float markerRadius =
                74f;

            float mapScale =
                MinimapWorldScale(
                    worldRadius);

            int placed =
                0;

            bool reachedEdge =
                false;

            int firstSegment =
                Mathf.Clamp(
                    fixedRoadRouteProgress,
                    0,
                    fixedRoadRoute.Count - 2);

            for (int segment = firstSegment;
                 segment < fixedRoadRoute.Count - 1 &&
                 placed < minimapRouteDots.Length &&
                 !reachedEdge;
                 segment++)
            {
                Vector3 a =
                    fixedRoadRoute[segment];

                Vector3 b =
                    fixedRoadRoute[segment + 1];

                if (segment == firstSegment)
                {
                    a =
                        ClosestPointOnFlatSegment(
                            carPosition,
                            a,
                            b);
                }

                float length =
                    FlatDistance(
                        a,
                        b);

                int steps =
                    Mathf.Max(
                        1,
                        Mathf.CeilToInt(
                            length /
                            3f));

                for (int step = 0;
                     step <= steps &&
                     placed < minimapRouteDots.Length;
                     step++)
                {
                    Vector3 point =
                        Vector3.Lerp(
                            a,
                            b,
                            step /
                            (float)steps);

                    Vector3 delta =
                        point -
                        carPosition;

                    delta.y =
                        0f;

                    Vector3 local =
                        Quaternion.Euler(
                            0f,
                            -yaw,
                            0f) *
                        delta;

                    Vector2 offset =
                        new(
                            local.x *
                            mapScale,
                            local.z *
                            mapScale);

                    if (offset.sqrMagnitude >
                        markerRadius *
                        markerRadius)
                    {
                        offset =
                            offset.normalized *
                            markerRadius;

                        reachedEdge =
                            true;
                    }

                    RectTransform dot =
                        minimapRouteDots[
                            placed];

                    if (!dot.gameObject.activeSelf)
                    {
                        dot.gameObject.SetActive(
                            true);
                    }

                    dot.anchoredPosition = offset;
                    Image routeImage = minimapRouteImages[placed++];
                    float distance = offset.magnitude;
                    float nearFade = Mathf.SmoothStep(0f, 1f, distance / 9f);
                    float edgeFade = Mathf.SmoothStep(0f, 1f, (markerRadius - distance) / 9f);
                    Color routeColor = routeImage.color;
                    routeColor.a = Mathf.MoveTowards(routeColor.a, nearFade * edgeFade * minimapRouteOpacity * 0.96f,
                        Time.unscaledDeltaTime * 4f);
                    routeImage.color = routeColor;

                    if (reachedEdge)
                        break;
                }
            }

            for (int i = placed;
                 i < minimapRouteDots.Length;
                 i++)
            {
                RectTransform dot =
                    minimapRouteDots[i];

                if (dot != null &&
                    dot.gameObject.activeSelf)
                {
                    Image routeImage = minimapRouteImages[i];
                    Color routeColor = routeImage.color;
                    routeColor.a = Mathf.MoveTowards(routeColor.a, 0f, Time.unscaledDeltaTime * 4f);
                    routeImage.color = routeColor;
                    if (routeColor.a <= 0f) dot.gameObject.SetActive(false);
                }
            }

            visibleRouteDotCount =
                placed;
        }

        private void EnsureFixedRoadRoute(
            Vector3 carPosition,
            Vector3 target)
        {
            bool targetChanged =
                !fixedRoadRouteValid ||
                FlatDistance(
                    fixedRoadRouteTarget,
                    target) >
                RouteTargetChangeDistance;

            bool offRoute =
                fixedRoadRouteValid &&
                DistanceToRemainingRoute(
                    carPosition) >
                RouteRebuildOffPathDistance;

            if (!targetChanged &&
                !offRoute)
            {
                minimapRouteOpacity = Mathf.MoveTowards(minimapRouteOpacity, 1f, Time.unscaledDeltaTime * 4f);
                return;
            }

            // Fade the old route before replacing its geometry on a detour.
            if (fixedRoadRouteValid && minimapRouteOpacity > 0f)
            {
                minimapRouteOpacity = Mathf.MoveTowards(minimapRouteOpacity, 0f, Time.unscaledDeltaTime * 4f);
                return;
            }

            List<Vector3> route =
                CityRoadNavigator.BuildRoute(
                    carPosition,
                    target);

            fixedRoadRoute.Clear();

            if (route == null ||
                route.Count < 2)
            {
                fixedRoadRouteValid =
                    false;

                return;
            }

            foreach (Vector3 point in route)
            {
                if (fixedRoadRoute.Count == 0 ||
                    FlatDistance(
                        fixedRoadRoute[
                            fixedRoadRoute.Count - 1],
                        point) >
                    1.5f)
                {
                    fixedRoadRoute.Add(
                        point);
                }
            }

            fixedRoadRouteTarget =
                target;

            fixedRoadRouteProgress =
                0;

            fixedRoadRouteValid =
                fixedRoadRoute.Count >= 2;
        }

        private void AdvanceFixedRoadRouteProgress(
            Vector3 carPosition)
        {
            if (!fixedRoadRouteValid ||
                fixedRoadRoute.Count < 2)
            {
                return;
            }

            int maxSegment =
                fixedRoadRoute.Count - 2;

            int searchEnd =
                Mathf.Min(
                    maxSegment,
                    fixedRoadRouteProgress + 6);

            int bestSegment =
                fixedRoadRouteProgress;

            float bestDistance =
                float.PositiveInfinity;

            for (int segment =
                     fixedRoadRouteProgress;
                 segment <= searchEnd;
                 segment++)
            {
                Vector3 closest =
                    ClosestPointOnFlatSegment(
                        carPosition,
                        fixedRoadRoute[segment],
                        fixedRoadRoute[segment + 1]);

                float distance =
                    FlatDistance(
                        carPosition,
                        closest);

                if (distance <
                    bestDistance)
                {
                    bestDistance =
                        distance;

                    bestSegment =
                        segment;
                }
            }

            fixedRoadRouteProgress =
                bestSegment;

            while (fixedRoadRouteProgress <
                   maxSegment &&
                   FlatDistance(
                       carPosition,
                       fixedRoadRoute[
                           fixedRoadRouteProgress + 1]) <=
                   RouteAdvanceDistance)
            {
                fixedRoadRouteProgress++;
            }
        }

        private float DistanceToRemainingRoute(
            Vector3 carPosition)
        {
            if (!fixedRoadRouteValid ||
                fixedRoadRoute.Count < 2)
            {
                return
                    float.PositiveInfinity;
            }

            float best =
                float.PositiveInfinity;

            int startSegment =
                Mathf.Clamp(
                    fixedRoadRouteProgress,
                    0,
                    fixedRoadRoute.Count - 2);

            int endSegment =
                Mathf.Min(
                    fixedRoadRoute.Count - 2,
                    startSegment + 10);

            for (int segment = startSegment;
                 segment <= endSegment;
                 segment++)
            {
                Vector3 closest =
                    ClosestPointOnFlatSegment(
                        carPosition,
                        fixedRoadRoute[segment],
                        fixedRoadRoute[segment + 1]);

                best =
                    Mathf.Min(
                        best,
                        FlatDistance(
                            carPosition,
                            closest));
            }

            return best;
        }

        private void ClearFixedRoadRoute()
        {
            fixedRoadRoute.Clear();

            fixedRoadRouteValid =
                false;

            fixedRoadRouteProgress =
                0;

            fixedRoadRouteTarget =
                Vector3.zero;
        }

        private void HideRouteDots()
        {
            int count = minimapRouteDots.Length;
            minimapRouteOpacity = 0f;

            for (int i = 0;
                 i < count;
                 i++)
            {
                RectTransform dot =
                    minimapRouteDots[i];

                if (dot != null &&
                    dot.gameObject.activeSelf)
                {
                    Image routeImage = minimapRouteImages[i];
                    Color routeColor = routeImage.color;
                    routeColor.a = Mathf.MoveTowards(routeColor.a, 0f, Time.unscaledDeltaTime * 4f);
                    routeImage.color = routeColor;
                    if (routeColor.a <= 0f) dot.gameObject.SetActive(false);
                }
            }

            visibleRouteDotCount =
                0;
        }

        private Sprite CreateCircularMinimapSprite(
            int size)
        {
            minimapMaskTexture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false,
                    true)
                {
                    name =
                        "MotorCity_MinimapCircleMask",
                    filterMode =
                        FilterMode.Bilinear,
                    wrapMode =
                        TextureWrapMode.Clamp
                };

            Color[] pixels =
                new Color[
                    size *
                    size];

            float center =
                (size - 1) *
                0.5f;

            float radius =
                center -
                1f;

            float feather =
                Mathf.Max(1.5f, size / 168f);

            for (int y = 0;
                 y < size;
                 y++)
            {
                for (int x = 0;
                     x < size;
                     x++)
                {
                    float dx =
                        x -
                        center;

                    float dy =
                        y -
                        center;

                    float distance =
                        Mathf.Sqrt(
                            dx * dx +
                            dy * dy);

                    float alpha =
                        Mathf.Clamp01(
                            (radius -
                             distance) /
                            feather);

                    pixels[
                        y *
                        size +
                        x] =
                        new Color(
                            1f,
                            1f,
                            1f,
                            alpha);
                }
            }

            minimapMaskTexture.SetPixels(
                pixels);

            minimapMaskTexture.Apply(
                false,
                true);

            return
                Sprite.Create(
                    minimapMaskTexture,
                    new Rect(
                        0f,
                        0f,
                        size,
                        size),
                    new Vector2(
                        0.5f,
                        0.5f),
                    100f,
                    0u,
                    SpriteMeshType.FullRect);
        }

        private void CreateMinimapPlayerChevron(
            Transform parent)
        {
            Texture2D pointerTexture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.minimapPlayerPointer;

            if (pointerTexture != null)
            {
                GameObject pointerObject =
                    new(
                        "Minimap Player Pointer",
                        typeof(RectTransform),
                        typeof(RawImage));

                pointerObject.transform.SetParent(
                    parent,
                    false);

                RectTransform rect =
                    pointerObject.GetComponent<RectTransform>();

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
                        24f,
                        24f);
                rect.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        -45f);

                RawImage image =
                    pointerObject.GetComponent<RawImage>();

                image.texture =
                    pointerTexture;
                image.color =
                    new Color(
                        0.12f,
                        0.78f,
                        1f,
                        1f);
                image.raycastTarget =
                    false;

                return;
            }

            Image fallback =
                CreateHudIcon(
                    parent,
                    "Minimap Player Icon",
                    MotorCityIconLibrary.Get(
                        "car"),
                    Vector2.zero,
                    new Vector2(
                        24f,
                        24f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Color(
                        0.12f,
                        0.78f,
                        1f,
                        1f));

            if (fallback != null)
            {
                fallback.raycastTarget =
                    false;
            }
        }

        private void BuildNavigator(Transform canvas)
        {
            RectTransform panel =
                CreatePanel(
                    canvas,
                    "Minimap",
                    new Vector2(-22f, -22f),
                    new Vector2(214f, 268f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    Color.clear);

            navigatorPanel =
                panel.gameObject;

            ClearPanelChrome(panel);

            minimapMaskSprite =
                CreateCircularMinimapSprite(
                    512);

            GameObject rimObject =
                new(
                    "Minimap Rim",
                    typeof(RectTransform),
                    typeof(Image));

            rimObject.transform.SetParent(
                panel,
                false);

            RectTransform rimRect =
                rimObject.GetComponent<RectTransform>();

            rimRect.anchorMin =
                new Vector2(0.5f, 1f);

            rimRect.anchorMax =
                new Vector2(0.5f, 1f);

            rimRect.pivot =
                new Vector2(0.5f, 1f);

            rimRect.anchoredPosition =
                new Vector2(0f, -10f);

            rimRect.sizeDelta =
                new Vector2(178f, 178f);

            Image rimImage =
                rimObject.GetComponent<Image>();

            rimImage.sprite =
                minimapMaskSprite;

            rimImage.color =
                new Color(
                    0.09f,
                    0.08f,
                    0.14f,
                    0.98f);

            rimImage.raycastTarget =
                false;

            GameObject viewportObject =
                new(
                    "Minimap Viewport",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Mask));

            viewportObject.transform.SetParent(
                panel,
                false);

            RectTransform viewportRect =
                viewportObject.GetComponent<RectTransform>();

            viewportRect.anchorMin =
                new Vector2(0.5f, 1f);

            viewportRect.anchorMax =
                new Vector2(0.5f, 1f);

            viewportRect.pivot =
                new Vector2(0.5f, 1f);

            viewportRect.anchoredPosition =
                new Vector2(0f, -15f);

            viewportRect.sizeDelta =
                new Vector2(168f, 168f);

            Image viewportImage =
                viewportObject.GetComponent<Image>();

            viewportImage.sprite =
                minimapMaskSprite;

            viewportImage.color =
                Color.white;

            viewportImage.raycastTarget =
                false;

            Mask viewportMask =
                viewportObject.GetComponent<Mask>();

            viewportMask.showMaskGraphic =
                false;

            GameObject mapObject =
                new(
                    "Minimap View",
                    typeof(RectTransform),
                    typeof(RawImage));

            mapObject.transform.SetParent(
                viewportRect,
                false);

            RectTransform mapRect =
                mapObject.GetComponent<RectTransform>();

            mapRect.anchorMin =
                new Vector2(0.5f, 0.5f);

            mapRect.anchorMax =
                new Vector2(0.5f, 0.5f);

            mapRect.pivot =
                new Vector2(0.5f, 0.5f);

            mapRect.anchoredPosition =
                Vector2.zero;

            mapRect.sizeDelta =
                new Vector2(258f, 258f);

            minimapImage =
                mapObject.GetComponent<RawImage>();

            minimapImage.raycastTarget =
                false;

            schematicMap =
                new CitySchematicMap();

            if (schematicMap.Build())
            {
                minimapImage.texture =
                    schematicMap.Texture;
            }
            else
            {
                minimapImage.color =
                    new Color(
                        0.12f,
                        0.14f,
                        0.15f,
                        1f);
            }

            for (int i = 0;
                 i < minimapRouteDots.Length;
                 i++)
            {
                GameObject dot =
                    new(
                        "Route Dot " + i,
                        typeof(RectTransform),
                        typeof(Image));

                dot.transform.SetParent(
                    viewportRect,
                    false);

                RectTransform dotRect =
                    dot.GetComponent<RectTransform>();

                dotRect.anchorMin =
                    new Vector2(0.5f, 0.5f);
                dotRect.anchorMax =
                    new Vector2(0.5f, 0.5f);
                dotRect.pivot =
                    new Vector2(0.5f, 0.5f);
                dotRect.sizeDelta =
                    new Vector2(6f, 6f);

                Image dotImage =
                    dot.GetComponent<Image>();

                dotImage.sprite = minimapMaskSprite;
                dotImage.color =
                    new Color(
                        0.16f,
                        0.82f,
                        1f,
                        0.96f);
                dotImage.raycastTarget =
                    false;

                dotImage.color = new Color(.16f, .82f, 1f, 0f);
                dot.SetActive(false);
                minimapRouteImages[i] = dotImage;
                minimapRouteDots[i] =
                    dotRect;
            }

            GameObject playerArrowObject =
                new(
                    "Minimap Player",
                    typeof(RectTransform));

            playerArrowObject.transform.SetParent(
                viewportRect,
                false);

            minimapPlayerArrow =
                playerArrowObject.GetComponent<RectTransform>();

            minimapPlayerArrow.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            minimapPlayerArrow.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f);

            minimapPlayerArrow.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            minimapPlayerArrow.anchoredPosition =
                Vector2.zero;

            minimapPlayerArrow.sizeDelta =
                new Vector2(
                    28f,
                    28f);

            CreateMinimapPlayerChevron(
                minimapPlayerArrow);

            minimapTargetIcon =
                CreateHudIcon(
                    viewportRect,
                    "Minimap Target",
                    MotorCityIconLibrary.ForActivity(
                        activityManager != null
                            ? activityManager.ActiveId
                            : string.Empty),
                    Vector2.zero,
                    new Vector2(
                        26f,
                        26f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Color(
                        1f,
                        0.70f,
                        0.10f,
                        1f));

            minimapTargetBlip =
                minimapTargetIcon.rectTransform;
            // Cover the stencil edge with one continuous antialiased ring.
            rimImage.enabled = false;
            RectTransform smoothRim = GarageObject(panel, "Minimap Smooth Rim");
            smoothRim.anchorMin = smoothRim.anchorMax = new Vector2(.5f, 1f);
            smoothRim.pivot = new Vector2(.5f, 1f);
            smoothRim.anchoredPosition = new Vector2(0f, -10f);
            smoothRim.sizeDelta = new Vector2(178f, 178f);
            GarageReferenceGraphic rimGraphic = smoothRim.gameObject.AddComponent<GarageReferenceGraphic>();
            rimGraphic.symbol = GarageReferenceGraphic.Symbol.MinimapRim;
            rimGraphic.color = new Color(.65f, .58f, 1f, .95f);
            rimGraphic.raycastTarget = false;

            RectTransform targetStrip =
                CreatePanel(
                    panel,
                    "Navigation Target Strip",
                    new Vector2(
                        8f,
                        6f),
                    new Vector2(
                        198f,
                        58f),
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        0f,
                        0f),
                    Color.clear);

            ApplyReferenceHudSurface(targetStrip, .96f);

            minimapTargetText =
                CreateText(
                    targetStrip,
                    "Minimap Target Label",
                    11,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Vector2(-23f, 0f),
                    new Vector2(
                        140f,
                        50f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    TextColor);

            minimapTargetText.resizeTextForBestFit =
                false;
            minimapTargetText.fontSize = 18;
            minimapTargetText.resizeTextMinSize =
                16;
            minimapTargetText.resizeTextMaxSize =
                18;
            minimapTargetText.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            minimapTargetText.verticalOverflow =
                VerticalWrapMode.Overflow;

            GameObject navigatorButtonObject =
                new(
                    "Navigator Button",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            navigatorButtonObject.transform.SetParent(
                panel,
                false);

            RectTransform navigatorButtonRect =
                navigatorButtonObject.GetComponent<RectTransform>();

            navigatorButtonRect.anchorMin =
                new Vector2(1f, 0f);
            navigatorButtonRect.anchorMax =
                new Vector2(1f, 0f);
            navigatorButtonRect.pivot =
                new Vector2(1f, 0f);
            navigatorButtonRect.anchoredPosition =
                new Vector2(-8f, 20f);

            navigatorButtonRect.sizeDelta =
                new Vector2(40f, 30f);

            bool touchUi =
                ShouldUseTouchUi();

            Image navigatorButtonImage =
                navigatorButtonObject.GetComponent<Image>();

            Texture2D navigatorButtonTexture =
                uiThemeAssets == null
                    ? null
                    : uiThemeAssets.modalButton;

            Sprite navigatorButtonSprite =
                GetModalButtonSprite(
                    navigatorButtonTexture);

            if (navigatorButtonSprite != null)
            {
                navigatorButtonImage.sprite =
                    navigatorButtonSprite;
                navigatorButtonImage.type =
                    Image.Type.Simple;
                navigatorButtonImage.preserveAspect =
                    false;
                navigatorButtonImage.color =
                    Color.white;
            }
            else
            {
                navigatorButtonImage.color =
                    new Color(
                        PanelColor.r,
                        PanelColor.g,
                        PanelColor.b,
                        0.94f);
            }

            Button navigatorButton =
                navigatorButtonObject.GetComponent<Button>();

            navigatorButton.targetGraphic =
                navigatorButtonImage;

            ColorBlock navigatorColors =
                navigatorButton.colors;

            navigatorColors.normalColor =
                Color.white;
            navigatorColors.highlightedColor =
                new Color(
                    0.88f,
                    0.94f,
                    1f,
                    1f);
            navigatorColors.pressedColor =
                new Color(
                    0.68f,
                    0.78f,
                    0.96f,
                    1f);
            navigatorColors.selectedColor =
                navigatorColors.highlightedColor;

            navigatorButton.colors =
                navigatorColors;

            navigatorButton.onClick.AddListener(
                ToggleNavigatorMenu);

            RectTransform navigatorVectorIcon =
                GarageObject(
                    navigatorButtonRect,
                    "Navigator Vector Icon");

            navigatorVectorIcon.anchorMin =
                navigatorVectorIcon.anchorMax =
                navigatorVectorIcon.pivot =
                    new Vector2(
                        0.5f,
                        0.5f);

            navigatorVectorIcon.anchoredPosition =
                Vector2.zero;

            navigatorVectorIcon.sizeDelta =
                new Vector2(
                    18f,
                    18f);

            GarageReferenceGraphic navigatorIcon =
                navigatorVectorIcon.gameObject.AddComponent<
                    GarageReferenceGraphic>();

            navigatorIcon.symbol =
                GarageReferenceGraphic.Symbol.NavigationRight;

            navigatorIcon.color =
                TextColor;

            navigatorIcon.raycastTarget =
                false;
        }

        private void UpdateNavigator(
            bool garageOpen)
        {
            if (navigatorPanel == null ||
                car == null)
            {
                return;
            }

            if (garageOpen)
            {
                SetActiveIfChanged(
                    navigatorPanel,
                    false);
                return;
            }

            SetActiveIfChanged(
                navigatorPanel,
                true);

            Vector3 carPosition =
                car.transform.position;

            const float worldRadius =
                260f;

            if (schematicMap != null &&
                schematicMap.IsValid &&
                minimapImage != null)
            {
                minimapImage.uvRect =
                    schematicMap.UvWindow(
                        carPosition,
                        worldRadius);
            }

            float yaw =
                car.transform.eulerAngles.y;

            if (minimapImage != null)
            {
                minimapImage.rectTransform.localEulerAngles =
                    new Vector3(
                        0f,
                        0f,
                        yaw);
            }

            if (minimapPlayerArrow != null)
            {
                minimapPlayerArrow.localEulerAngles =
                    Vector3.zero;
            }

            minimapTargetResolveTimer -=
                Time.unscaledDeltaTime;

            if (minimapTargetResolveTimer <= 0f)
            {
                minimapTargetResolveTimer =
                    MinimapTargetResolveInterval;

                ResolveMinimapTarget(
                    out cachedMinimapTarget,
                    out cachedMinimapLabel,
                    out cachedMinimapHasTarget,
                    out cachedMinimapShowRoadRoute);

                cachedMinimapMarkerId =
                    cachedMinimapHasTarget
                        ? ResolveMinimapMarkerId(
                            cachedMinimapTarget)
                        : string.Empty;
            }

            Vector3 target =
                cachedMinimapTarget;

            string label =
                cachedMinimapLabel;

            bool hasTarget =
                cachedMinimapHasTarget;

            bool showRoadRoute =
                cachedMinimapShowRoadRoute;

            string markerId =
                cachedMinimapMarkerId;

            if (!hasTarget)
            {
                if (minimapTargetBlip != null)
                {
                    SetActiveIfChanged(
                        minimapTargetBlip.gameObject,
                        false);
                }

                if (minimapTargetText != null &&
                    !string.IsNullOrEmpty(
                        minimapTargetText.text))
                {
                    minimapTargetText.text =
                        string.Empty;
                }

                lastMinimapDistance =
                    int.MinValue;
                lastMinimapDistanceLabel =
                    string.Empty;

                HideRouteDots();
                ClearFixedRoadRoute();
                return;
            }

            Vector3 delta =
                target -
                carPosition;

            delta.y = 0f;

            Vector3 local =
                Quaternion.Euler(
                    0f,
                    -yaw,
                    0f) *
                delta;

            const float markerRadius =
                78f;

            float mapScale =
                MinimapWorldScale(
                    worldRadius);

            Vector2 mapOffset =
                new Vector2(
                    local.x * mapScale,
                    local.z * mapScale);

            if (mapOffset.sqrMagnitude >
                markerRadius * markerRadius)
            {
                mapOffset =
                    mapOffset.normalized *
                    markerRadius;
            }

            minimapRouteUpdateTimer = 0f;

            if (minimapRouteUpdateTimer <= 0f)
            {
                minimapRouteUpdateTimer =
                    MinimapRouteUpdateInterval;

                UpdateRoadRoute(
                    carPosition,
                    target,
                    yaw,
                    worldRadius,
                    showRoadRoute);
            }

            if (minimapTargetBlip != null)
            {
                SetActiveIfChanged(
                    minimapTargetBlip.gameObject,
                    true);

                minimapTargetBlip.anchoredPosition =
                    mapOffset;

                if (minimapTargetIcon != null)
                {
                    Sprite targetSprite =
                        MotorCityIconLibrary.ForWorldMarker(
                            markerId);

                    if (targetSprite != null &&
                        minimapTargetIcon.sprite !=
                            targetSprite)
                    {
                        minimapTargetIcon.sprite =
                            targetSprite;
                    }

                    minimapTargetIcon.color =
                        MotorCityIconLibrary.WorldMarkerColor(
                            markerId);
                }
            }

            if (minimapTargetText != null)
            {
                minimapDistanceUpdateTimer -=
                    Time.unscaledDeltaTime;

                bool labelChanged =
                    !string.Equals(
                        label,
                        lastMinimapDistanceLabel,
                        System.StringComparison.Ordinal);

                if (labelChanged ||
                    minimapDistanceUpdateTimer <= 0f)
                {
                    minimapDistanceUpdateTimer =
                        MinimapTargetResolveInterval;

                    int roundedDistance =
                        Mathf.RoundToInt(
                            Mathf.Sqrt(
                                delta.sqrMagnitude));

                    if (roundedDistance !=
                            lastMinimapDistance ||
                        labelChanged)
                    {
                        lastMinimapDistance =
                            roundedDistance;
                        lastMinimapDistanceLabel =
                            label ?? string.Empty;

                        minimapTargetText.text =
                            MotorCityLocalization.Format(
                                "hud.distance",
                                label,
                                roundedDistance).Replace("   ", "\n");
                    }
                }
            }
        }

        private void ResolveMinimapTarget(
            out Vector3 target,
            out string label,
            out bool hasTarget,
            out bool showRoadRoute)
        {
            hasTarget = true;
            showRoadRoute = false;

            if (activityManager != null &&
                !activityManager.IsBusy &&
                onboarding != null &&
                !onboarding.IsComplete)
            {
                if (onboarding.CurrentStep == 4 &&
                    delivery != null)
                {
                    target =
                        delivery.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.first_activity_target");
                    showRoadRoute = true;
                    return;
                }

                if (onboarding.CurrentStep == 5 &&
                    garage != null)
                {
                    target =
                        garage.GarageCenter;
                    label =
                        MotorCityLocalization.Text(
                            "hud.garage");
                    showRoadRoute = true;
                    return;
                }
            }

            if ((activityManager == null ||
                 !activityManager.IsBusy) &&
                manualNavigationActive)
            {
                target =
                    manualNavigationTarget;
                label =
                    manualNavigationLabel;
                showRoadRoute =
                    true;

                if (FlatDistance(
                        car.transform.position,
                        manualNavigationTarget) <=
                    16f)
                {
                    manualNavigationActive =
                        false;
                }
                else
                {
                    return;
                }
            }

            if (activityManager != null &&
                !activityManager.IsBusy &&
                (onboarding == null ||
                 onboarding.IsComplete) &&
                story != null &&
                !story.IsComplete)
            {
                string required =
                    story.RequiredActivityId;

                if (required == "delivery" &&
                    delivery != null)
                {
                    target =
                        delivery.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.story_delivery_target");
                    showRoadRoute = true;
                    return;
                }

                if (required == "drift" &&
                    driftChallenge != null)
                {
                    target =
                        driftChallenge.ZoneCenter;
                    label =
                        MotorCityLocalization.Text(
                            "hud.story_drift_target");
                    showRoadRoute = true;
                    return;
                }

                if (required == "sprint" &&
                    streetSprint != null)
                {
                    target =
                        streetSprint.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.story_sprint_target");
                    showRoadRoute = true;
                    return;
                }

                if (required == "circuit" &&
                    circuitRace != null)
                {
                    target =
                        circuitRace.CurrentTarget;
                    label =
                        MotorCityLocalization.Text(
                            "hud.story_circuit_target");
                    showRoadRoute = true;
                    return;
                }

                if (required == "*")
                {
                    ResolveNearestStoryActivityTarget(
                        out target,
                        out label);

                    hasTarget =
                        target !=
                        car.transform.position;

                    showRoadRoute =
                        hasTarget;

                    if (hasTarget)
                        return;
                }
            }

            if (activityManager != null &&
                !activityManager.IsBusy &&
                (onboarding == null ||
                 onboarding.IsComplete) &&
                (story == null ||
                 story.IsComplete) &&
                season != null &&
                !season.IsComplete &&
                season.IsSeasonOneActive)
            {
                string required =
                    season.RequiredActivityId;

                if (required == "delivery" &&
                    delivery != null)
                {
                    target =
                        delivery.CurrentTarget;

                    label =
                        MotorCityLocalization.Text(
                            "activity.delivery");

                    showRoadRoute =
                        true;

                    return;
                }

                if (required == "drift" &&
                    driftChallenge != null)
                {
                    target =
                        driftChallenge.ZoneCenter;

                    label =
                        MotorCityLocalization.Text(
                            "activity.drift");

                    showRoadRoute =
                        true;

                    return;
                }

                if (required == "sprint" &&
                    streetSprint != null)
                {
                    target =
                        streetSprint.CurrentTarget;

                    label =
                        MotorCityLocalization.Text(
                            "hud.sprint");

                    showRoadRoute =
                        true;

                    return;
                }

                if (required == "circuit" &&
                    circuitRace != null)
                {
                    target =
                        circuitRace.CurrentTarget;

                    label =
                        MotorCityLocalization.Text(
                            "hud.circuit");

                    showRoadRoute =
                        true;

                    return;
                }

                if (required == "profession_carwash" &&
                    carWash != null)
                {
                    target =
                        carWash.StartPoint;

                    label =
                        MotorCityLocalization.Text(
                            "carwash.title");

                    showRoadRoute =
                        true;

                    return;
                }

                if (professions != null &&
                    professions.TryGetStartForActivity(
                        required,
                        out Vector3 professionTarget,
                        out string professionLabel))
                {
                    target =
                        professionTarget;

                    label =
                        professionLabel;

                    showRoadRoute =
                        true;

                    return;
                }

                if (required == "*")
                {
                    ResolveNearestStoryActivityTarget(
                        out target,
                        out label);

                    hasTarget =
                        target !=
                        car.transform.position;

                    showRoadRoute =
                        hasTarget;

                    if (hasTarget)
                        return;
                }
            }

            if (underground != null &&
                (underground.HasActiveInvitation ||
                 underground.IsActive ||
                 underground.IsCountingDown))
            {
                target =
                    underground.CurrentTarget;
                label =
                    underground.IsActive ||
                    underground.IsCountingDown
                        ? MotorCityLocalization.Text(
                            "hud.underground")
                        : MotorCityLocalization.Text(
                            "hud.secret_meeting");
                showRoadRoute = true;
                return;
            }

            if (towTruck != null &&
                towTruck.IsActive)
            {
                target =
                    towTruck.CurrentTarget;

                label =
                    MotorCityLocalization.Text(
                        "tow.title");
                showRoadRoute = true;

                return;
            }

            if (carWash != null &&
                carWash.IsActive)
            {
                target =
                    carWash.CurrentTarget;

                label =
                    MotorCityLocalization.Text(
                        "carwash.title");
                showRoadRoute = true;

                return;
            }

            if (professions != null &&
                professions.IsActive)
            {
                target =
                    professions.CurrentTarget;

                label =
                    professions.CurrentLabel;
                showRoadRoute = true;

                return;
            }

            if (delivery != null &&
                (delivery.IsActive ||
                 delivery.IsCountingDown))
            {
                target =
                    delivery.CurrentTarget;
                label =
                    MotorCityLocalization.Text(
                        "activity.delivery");
                showRoadRoute = true;
                return;
            }

            if (streetSprint != null &&
                (streetSprint.IsActive ||
                 streetSprint.IsCountingDown))
            {
                target =
                    streetSprint.CurrentTarget;
                label =
                    MotorCityLocalization.Text(
                        "hud.sprint");
                showRoadRoute = true;
                return;
            }

            if (circuitRace != null &&
                (circuitRace.IsActive ||
                 circuitRace.IsCountingDown))
            {
                target =
                    circuitRace.CurrentTarget;
                label =
                    MotorCityLocalization.Format("hud.circuit_lap", circuitRace.CurrentLap, circuitRace.LapCount);
                showRoadRoute = true;
                return;
            }

            if (driftChallenge != null &&
                (driftChallenge.IsActive ||
                 driftChallenge.IsCountingDown))
            {
                target =
                    driftChallenge.ZoneCenter;
                label =
                    MotorCityLocalization.Text(
                        "activity.drift");
                showRoadRoute = true;
                return;
            }

            ResolveNearestFreeRoamTarget(
                out target,
                out label);

            hasTarget =
                target !=
                car.transform.position;
        }

        private GameObject BuildNavigatorTouchControls(
            Transform canvas)
        {
            GameObject rootObject =
                new(
                    "Navigator Touch Controls",
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
                new Vector2(0f, 18f);
            root.sizeDelta =
                new Vector2(470f, 118f);

            CreateTouchPulseButton(
                root,
                "Navigator Touch Controls Prev",
                "◀",
                MotorCityInputAction.PreviousVehicle,
                new Vector2(-135f, 82f),
                new Vector2(76f, 46f));

            CreateLocalizedTouchPulseButton(
                root,
                "Navigator Touch Controls Action",
                "touch.modal.select",
                MotorCityInputAction.Retry,
                new Vector2(0f, 82f),
                new Vector2(178f, 46f));

            CreateTouchPulseButton(
                root,
                "Navigator Touch Controls Next",
                "▶",
                MotorCityInputAction.NextVehicle,
                new Vector2(135f, 82f),
                new Vector2(76f, 46f));

            CreateLocalizedTouchPulseButton(
                root,
                "Navigator Touch Controls Close",
                "touch.modal.close",
                MotorCityInputAction.ToggleNavigator,
                new Vector2(0f, 24f),
                new Vector2(178f, 40f));

            return
                rootObject;
        }

        private static Vector3 ClosestPointOnFlatSegment(
            Vector3 point,
            Vector3 a,
            Vector3 b)
        {
            Vector2 p =
                new(
                    point.x,
                    point.z);

            Vector2 av =
                new(
                    a.x,
                    a.z);

            Vector2 bv =
                new(
                    b.x,
                    b.z);

            Vector2 ab =
                bv -
                av;

            float lengthSquared =
                ab.sqrMagnitude;

            float t =
                lengthSquared <=
                    0.0001f
                    ? 0f
                    : Mathf.Clamp01(
                        Vector2.Dot(
                            p - av,
                            ab) /
                        lengthSquared);

            return new Vector3(
                Mathf.Lerp(
                    a.x,
                    b.x,
                    t),
                Mathf.Lerp(
                    a.y,
                    b.y,
                    t),
                Mathf.Lerp(
                    a.z,
                    b.z,
                    t));
        }

        private static float FlatDistance(
            Vector3 a,
            Vector3 b)
        {
            a.y =
                0f;
            b.y =
                0f;

            return
                Vector3.Distance(
                    a,
                    b);
        }

        private void ResolveNearestStoryActivityTarget(
            out Vector3 target,
            out string label)
        {
            target =
                car.transform.position;

            label =
                MotorCityLocalization.Text(
                    "hud.free_drive");

            float bestDistance =
                float.PositiveInfinity;

            ConsiderNavigationTarget(
                delivery != null
                    ? delivery.CurrentTarget
                    : Vector3.zero,
                MotorCityLocalization.Text(
                    "activity.delivery"),
                delivery != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                driftChallenge != null
                    ? driftChallenge.ZoneCenter
                    : Vector3.zero,
                MotorCityLocalization.Text(
                    "activity.drift"),
                driftChallenge != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                streetSprint != null
                    ? streetSprint.CurrentTarget
                    : Vector3.zero,
                MotorCityLocalization.Text(
                    "hud.sprint"),
                streetSprint != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                circuitRace != null
                    ? circuitRace.CurrentTarget
                    : Vector3.zero,
                MotorCityLocalization.Text(
                    "hud.circuit"),
                circuitRace != null,
                ref target,
                ref label,
                ref bestDistance);

            if (speedTraps != null)
            {
                for (int i = 0;
                     i < speedTraps.TrapCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        speedTraps.GetTrapPosition(i),
                        MotorCityLocalization.Text(
                            "hud.radar"),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            if (driftSpots != null)
            {
                for (int i = 0;
                     i < driftSpots.SpotCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        driftSpots.GetSpotPosition(i),
                        MotorCityLocalization.Text(
                            "hud.drift_spot"),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            if (discoveries != null)
            {
                for (int i = 0;
                     i < discoveries.DiscoveryCount;
                     i++)
                {
                    if (discoveries.IsFound(i))
                        continue;

                    ConsiderNavigationTarget(
                        discoveries.GetDiscoveryPosition(i),
                        MotorCityLocalization.Text(
                            "hud.discovery"),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            if (towTruck != null)
            {
                ConsiderNavigationTarget(
                    towTruck.StartPoint,
                    MotorCityLocalization.Text(
                        "tow.title"),
                    true,
                    ref target,
                    ref label,
                    ref bestDistance);
            }

            if (carWash != null)
            {
                ConsiderNavigationTarget(
                    carWash.StartPoint,
                    MotorCityLocalization.Text(
                        "carwash.title"),
                    true,
                    ref target,
                    ref label,
                    ref bestDistance);
            }

            if (professions != null)
            {
                for (int i = 0;
                     i < professions.StartCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        professions.GetStartPoint(i),
                        professions.GetStartName(i),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }
        }

        private void ResolveNearestFreeRoamTarget(
            out Vector3 target,
            out string label)
        {
            target =
                car.transform.position;
            label =
                MotorCityLocalization.Text("hud.free_drive");

            float bestDistance =
                float.PositiveInfinity;

            ConsiderNavigationTarget(
                delivery != null
                    ? delivery.CurrentTarget
                    : Vector3.zero,
                MotorCityLocalization.Text("activity.delivery"),
                delivery != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                driftChallenge != null
                    ? driftChallenge.ZoneCenter
                    : Vector3.zero,
                MotorCityLocalization.Text("activity.drift"),
                driftChallenge != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                streetSprint != null
                    ? streetSprint.CurrentTarget
                    : Vector3.zero,
                MotorCityLocalization.Text("hud.sprint"),
                streetSprint != null,
                ref target,
                ref label,
                ref bestDistance);

            ConsiderNavigationTarget(
                circuitRace != null
                    ? circuitRace.CurrentTarget
                    : Vector3.zero,
                MotorCityLocalization.Text("hud.circuit"),
                circuitRace != null,
                ref target,
                ref label,
                ref bestDistance);

            if (speedTraps != null)
            {
                for (int i = 0;
                     i < speedTraps.TrapCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        speedTraps.GetTrapPosition(i),
                        MotorCityLocalization.Text("hud.radar"),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            if (driftSpots != null)
            {
                for (int i = 0;
                     i < driftSpots.SpotCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        driftSpots.GetSpotPosition(i),
                        MotorCityLocalization.Text("hud.drift_spot"),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            if (discoveries != null)
            {
                for (int i = 0;
                     i < discoveries.DiscoveryCount;
                     i++)
                {
                    if (discoveries.IsFound(i))
                        continue;

                    ConsiderNavigationTarget(
                        discoveries.GetDiscoveryPosition(i),
                        MotorCityLocalization.Text("hud.discovery"),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            if (towTruck != null)
            {
                ConsiderNavigationTarget(
                    towTruck.StartPoint,
                    MotorCityLocalization.Text(
                        "tow.title"),
                    true,
                    ref target,
                    ref label,
                    ref bestDistance);
            }

            if (carWash != null)
            {
                ConsiderNavigationTarget(
                    carWash.StartPoint,
                    MotorCityLocalization.Text(
                        "carwash.title"),
                    true,
                    ref target,
                    ref label,
                    ref bestDistance);
            }

            if (professions != null)
            {
                for (int i = 0;
                     i < professions.StartCount;
                     i++)
                {
                    ConsiderNavigationTarget(
                        professions.GetStartPoint(i),
                        professions.GetStartName(i),
                        true,
                        ref target,
                        ref label,
                        ref bestDistance);
                }
            }

            ConsiderNavigationTarget(
                garage != null
                    ? garage.GarageCenter
                    : Vector3.zero,
                MotorCityLocalization.Text("hud.garage"),
                garage != null,
                ref target,
                ref label,
                ref bestDistance);
        }

        private void ConsiderNavigationTarget(
            Vector3 candidate,
            string candidateLabel,
            bool valid,
            ref Vector3 target,
            ref string label,
            ref float bestDistance)
        {
            if (!valid)
                return;

            Vector3 delta =
                candidate -
                car.transform.position;

            delta.y = 0f;

            float distance =
                delta.sqrMagnitude;

            if (distance >= bestDistance)
                return;

            bestDistance =
                distance;
            target =
                candidate;
            label =
                candidateLabel;
        }

        private void OnDestroy()
        {
            if (garage != null)
            {
                garage.Changed -=
                    MarkGarageUiDirty;
            }

            car?.SetDrivingBlocked(
                "ModalUi",
                false);

            car?.SetDrivingBlocked(
                "PauseMenu",
                false);

            MotorCity.Platform.MotorCityPlatformRuntime.SetGameplayUiPaused(
                false);

            MotorCity.Audio.MotorCityMusicRuntime.SetPauseMenuPaused(
                false);

            if (schematicMap != null &&
                schematicMap.Texture != null)
            {
                Destroy(
                    schematicMap.Texture);
            }

            if (minimapMaskSprite != null)
            {
                Destroy(
                    minimapMaskSprite);
            }

            if (minimapMaskTexture != null)
            {
                Destroy(
                    minimapMaskTexture);
            }

            for (int i = 0;
                 i < runtimeOwnedSprites.Count;
                 i++)
            {
                Sprite sprite =
                    runtimeOwnedSprites[i];

                if (sprite != null)
                {
                    Destroy(
                        sprite);
                }
            }

            runtimeOwnedSprites.Clear();
        }

    }
}
