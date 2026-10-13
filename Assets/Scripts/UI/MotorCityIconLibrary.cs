using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.UI
{
    public enum ActivityIcon
    {
        Delivery,
        Drift,
        Sprint,
        Circuit,
        SpeedTrap,
        DriftSpot,
        Discovery,
        PhotoHunt,
        Taxi,
        PizzaDelivery,
        Mail,
        CarWash,
        TowTruck,
        CityProfession,
        Underground
    }

    public enum SystemIcon
    {
        Garage,
        Upgrades,
        Shop,
        Achievements,
        Club,
        Season,
        DailyAdventure,
        Underground,
        Contracts,
        CityEvents,
        Collection,
        CityLegend,
        VehicleHistory,
        Specialization,
        Credits,
        Reputation,
        Level,
        PixieXp,
        Reward,
        Locked,
        Unlocked,
        Confirm,
        Add
    }

    /// <summary>
    /// Centralized Resources-backed icon access for the runtime-built HUD.
    /// Keep UI components going through this class so icon paths, caching and
    /// fallbacks stay consistent across WebGL and touch layouts.
    /// </summary>
    public static class MotorCityIconLibrary
    {
        private const string FallbackIconName = "star";

        private static readonly Dictionary<string, Sprite> Cache =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> MissingWarnings =
            new(StringComparer.OrdinalIgnoreCase);

        public static Sprite Get(string iconName) => TablerIcon(iconName);

        private const string TablerRoot = "MotorCity/UI/Icons/Tabler/";

        // The checked-in, MIT-licensed Tabler sprites are the sole source
        // for all game pictograms. No procedural or other asset fallback.
        public static Sprite TryGetTabler(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            string cacheKey = "tabler:" + name;
            if (Cache.TryGetValue(cacheKey, out Sprite cached))
                return cached;

            Sprite sprite = Resources.Load<Sprite>(TablerRoot + name);
            if (sprite == null)
            {
                Sprite[] candidates = Resources.LoadAll<Sprite>(
                    TablerRoot + name);
                if (candidates != null && candidates.Length > 0)
                    sprite = candidates[0];
            }

            if (sprite != null)
                Cache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite TablerIcon(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return GetFallback();

            Sprite sprite = TryGetTabler(name);
            if (sprite != null)
                return sprite;

            WarnMissingOnce(name);
            return GetFallback();
        }

        public static Sprite ForActivity(
            ActivityIcon icon)
        {
            return icon switch
            {
                ActivityIcon.Delivery => TablerIcon("truck-delivery"),
                ActivityIcon.Drift => TablerIcon("car-turbine"),
                ActivityIcon.Sprint => TablerIcon("flag-3"),
                ActivityIcon.Circuit => TablerIcon("trophy"),
                ActivityIcon.SpeedTrap => TablerIcon("gauge"),
                ActivityIcon.DriftSpot => TablerIcon("target"),
                ActivityIcon.Discovery => TablerIcon("map-pin"),
                ActivityIcon.PhotoHunt => TablerIcon("camera"),
                ActivityIcon.Taxi => TablerIcon("car"),
                ActivityIcon.PizzaDelivery => TablerIcon("pizza"),
                ActivityIcon.Mail => TablerIcon("mail"),
                ActivityIcon.CarWash => TablerIcon("spray"),
                ActivityIcon.TowTruck => TablerIcon("car-crane"),
                ActivityIcon.CityProfession => TablerIcon("briefcase"),
                ActivityIcon.Underground => TablerIcon("moon-stars"),
                _ => GetFallback()
            };
        }

        public static Sprite ForActivity(
            string activityId,
            bool success = true)
        {
            if (!success)
                return TablerIcon("target");

            if (string.IsNullOrWhiteSpace(
                    activityId))
            {
                return GetFallback();
            }

            string normalized =
                activityId.Trim().ToLowerInvariant();

            return normalized switch
            {
                "delivery" =>
                    ForActivity(ActivityIcon.Delivery),

                "drift" or
                "drift_challenge" =>
                    ForActivity(ActivityIcon.Drift),

                "sprint" or
                "street_sprint" or
                "festival_sprint" =>
                    ForActivity(ActivityIcon.Sprint),

                "circuit" or
                "circuit_race" =>
                    ForActivity(ActivityIcon.Circuit),

                "speedtrap" or
                "speed_trap" =>
                    ForActivity(ActivityIcon.SpeedTrap),

                "driftspot" or
                "drift_spot" =>
                    ForActivity(ActivityIcon.DriftSpot),

                "discovery" =>
                    ForActivity(ActivityIcon.Discovery),

                "photo_hunt" or
                "photohunt" =>
                    ForActivity(ActivityIcon.PhotoHunt),

                "profession_taxi" or
                "taxi" =>
                    ForActivity(ActivityIcon.Taxi),

                "profession_pizza" or
                "pizza_delivery" =>
                    ForActivity(ActivityIcon.PizzaDelivery),

                "profession_mail" or
                "mail" =>
                    ForActivity(ActivityIcon.Mail),

                "profession_carwash" or
                "carwash" or
                "car_wash" =>
                    ForActivity(ActivityIcon.CarWash),

                "towtruck" or
                "tow_truck" or
                "profession_tow" =>
                    ForActivity(ActivityIcon.TowTruck),

                "profession" or
                "city_profession" or
                "profession_icecream" =>
                    ForActivity(ActivityIcon.CityProfession),

                "underground" =>
                    ForActivity(ActivityIcon.Underground),

                _ =>
                    GetFallback()
            };
        }

        public static Sprite ForSystem(
            SystemIcon icon)
        {
            return icon switch
            {
                SystemIcon.Garage => TablerIcon("car-garage"),
                SystemIcon.Upgrades => TablerIcon("tools"),
                SystemIcon.Shop => TablerIcon("shopping-cart"),
                SystemIcon.Achievements => TablerIcon("trophy"),
                SystemIcon.Club => TablerIcon("users-group"),
                SystemIcon.Season => TablerIcon("calendar-event"),
                SystemIcon.DailyAdventure => TablerIcon("sun"),
                SystemIcon.Underground => TablerIcon("moon-stars"),
                SystemIcon.Contracts => TablerIcon("clipboard-list"),
                SystemIcon.CityEvents => TablerIcon("calendar-event"),
                SystemIcon.Collection => TablerIcon("star"),
                SystemIcon.CityLegend => TablerIcon("crown"),
                SystemIcon.VehicleHistory => TablerIcon("history"),
                SystemIcon.Specialization => TablerIcon("certificate"),
                SystemIcon.Credits => TablerIcon("coins"),
                SystemIcon.Reputation => TablerIcon("star"),
                SystemIcon.Level => TablerIcon("medal"),
                SystemIcon.PixieXp => TablerIcon("sparkles"),
                SystemIcon.Reward => TablerIcon("gift"),
                SystemIcon.Locked => TablerIcon("lock"),
                SystemIcon.Unlocked => TablerIcon("lock-open"),
                SystemIcon.Confirm => TablerIcon("check"),
                SystemIcon.Add => TablerIcon("plus"),
                _ => GetFallback()
            };
        }

        public static void PrewarmCore()
        {
            _ = ForSystem(SystemIcon.Garage);
            _ = ForSystem(SystemIcon.Shop);
            _ = ForSystem(SystemIcon.Credits);
            _ = ForSystem(SystemIcon.Achievements);
            _ = ForSystem(SystemIcon.Locked);
            _ = ForSystem(SystemIcon.Unlocked);
            _ = ForSystem(SystemIcon.Confirm);
            _ = ForActivity(ActivityIcon.Delivery);
            _ = ForActivity(ActivityIcon.Sprint);
            _ = ForActivity(ActivityIcon.Circuit);
            _ = ForActivity(ActivityIcon.SpeedTrap);
        }

        public static Sprite Garage =>
            ForSystem(SystemIcon.Garage);

        public static Sprite Store =>
            ForSystem(SystemIcon.Shop);

        public static Sprite Upgrades =>
            ForSystem(SystemIcon.Upgrades);

        public static Sprite Credits =>
            ForSystem(SystemIcon.Credits);

        public static Sprite Reputation =>
            ForSystem(SystemIcon.Reputation);

        public static Sprite Achievement =>
            ForSystem(SystemIcon.Achievements);

        public static Sprite Reward =>
            ForSystem(SystemIcon.Reward);

        public static Sprite Locked =>
            ForSystem(SystemIcon.Locked);

        public static Sprite Unlocked =>
            ForSystem(SystemIcon.Unlocked);

        public static Sprite Confirm =>
            ForSystem(SystemIcon.Confirm);

        public static Sprite Add =>
            ForSystem(SystemIcon.Add);

        public static Sprite Pause =>
            TablerIcon("player-pause");

        public static Sprite More =>
            TablerIcon("layout-grid");

        public static Sprite ForWorldMarker(
            string markerId)
        {
            if (string.IsNullOrWhiteSpace(
                    markerId))
            {
                return GetFallback();
            }

            string normalized =
                markerId.Trim().ToLowerInvariant();

            // Same sprite mapping for navigator, HUD and world markers.
            string tablerName = normalized switch
            {
                "garage" => "car-garage",
                "delivery" => "truck-delivery",
                "drift" => "car-turbine",
                "sprint" => "flag-3",
                "circuit" => "trophy",
                "discovery" => "map-pin",
                "speedtrap" or "speed_trap" => "gauge",
                "driftspot" or "drift_spot" => "target",
                "photo_hunt" or "photohunt" => "camera",
                "underground" => "moon-stars",
                "profession" or "city_profession" or
                "profession_icecream" => "briefcase",
                "taxi" or "profession_taxi" => "car",
                "pizza" or "pizza_delivery" or "profession_pizza" => "pizza",
                "mail" or "profession_mail" => "mail",
                "carwash" or "car_wash" or "profession_carwash" => "spray",
                "tow" or "towtruck" or "tow_truck" or "profession_tow" => "car-crane",
                _ => null
            };

            return tablerName == null
                ? GetFallback()
                : TablerIcon(tablerName);
        }

        public static Color WorldMarkerColor(
            string markerId)
        {
            string normalized =
                string.IsNullOrWhiteSpace(markerId)
                    ? string.Empty
                    : markerId.Trim().ToLowerInvariant();

            return normalized switch
            {
                "drift" =>
                    new Color(1f, 0.30f, 0.035f, 1f),

                "delivery" =>
                    new Color(0.06f, 0.48f, 1f, 1f),

                "sprint" =>
                    new Color(0.08f, 1f, 0.28f, 1f),

                "circuit" =>
                    new Color(0.04f, 0.86f, 1f, 1f),

                "discovery" or
                "speedtrap" or
                "driftspot" =>
                    new Color(0.72f, 0.28f, 1f, 1f),

                "underground" =>
                    new Color(0.78f, 0.18f, 1f, 1f),

                "profession" =>
                    new Color(1f, 0.68f, 0.10f, 1f),

                "carwash" or
                "profession_carwash" =>
                    new Color(0.10f, 0.82f, 1f, 1f),

                "tow" or
                "towtruck" or
                "profession_tow" =>
                    new Color(1f, 0.56f, 0.06f, 1f),

                "garage" =>
                    new Color(0.72f, 0.20f, 1f, 1f),

                _ =>
                    new Color(1f, 0.70f, 0.10f, 1f)
            };
        }

        private static Sprite GetFallback()
        {
            Sprite fallback = TryGetTabler(FallbackIconName);
            if (fallback == null)
                WarnMissingOnce(FallbackIconName);
            return fallback;
        }

        private static void WarnMissingOnce(string iconName)
        {
            if (!MissingWarnings.Add(iconName))
                return;
#if UNITY_EDITOR || DEBUG
            Debug.LogWarning("[MotorCity][Icons] Missing Tabler sprite: " +
                TablerRoot + iconName);
#endif
        }
    }
}
