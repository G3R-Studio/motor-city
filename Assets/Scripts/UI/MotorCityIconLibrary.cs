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
        StuntJump,
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
        WeekendEvent,
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
        TurboXp,
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
        private const string Root =
            "MotorCity/UI/Icons/Kenney/";

        private const string FallbackIconName =
            "star";

        private static readonly Dictionary<string, Sprite> Cache =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> MissingWarnings =
            new(StringComparer.OrdinalIgnoreCase);

        public static Sprite Get(
            string iconName)
        {
            if (string.IsNullOrWhiteSpace(
                    iconName))
            {
                return GetFallback();
            }

            if (Cache.TryGetValue(
                    iconName,
                    out Sprite cached))
            {
                return cached;
            }

            Sprite sprite =
                Resources.Load<Sprite>(
                    Root + iconName);

            if (sprite == null &&
                !string.Equals(
                    iconName,
                    FallbackIconName,
                    StringComparison.OrdinalIgnoreCase))
            {
                WarnMissingOnce(
                    iconName);

                sprite =
                    GetFallback();
            }

            Cache[iconName] =
                sprite;

            return sprite;
        }

        public static Sprite ForActivity(
            ActivityIcon icon)
        {
            return icon switch
            {
                ActivityIcon.Delivery => Get("car"),
                ActivityIcon.Drift => Get("car"),
                ActivityIcon.Sprint => Get("flag"),
                ActivityIcon.Circuit => Get("trophy"),
                ActivityIcon.SpeedTrap => Get("target"),
                ActivityIcon.DriftSpot => Get("target"),
                ActivityIcon.StuntJump => Get("star"),
                ActivityIcon.Discovery => Get("star"),
                ActivityIcon.PhotoHunt => Get("target"),
                ActivityIcon.Taxi => Get("car"),
                ActivityIcon.PizzaDelivery => Get("car"),
                ActivityIcon.Mail => Get("car"),
                ActivityIcon.CarWash => Get("car"),
                ActivityIcon.TowTruck => Get("key"),
                ActivityIcon.CityProfession => Get("gear"),
                ActivityIcon.Underground => Get("flag"),
                _ => GetFallback()
            };
        }

        public static Sprite ForActivity(
            string activityId,
            bool success = true)
        {
            if (!success)
                return Get("target");

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

                "stuntjump" or
                "stunt_jump" =>
                    ForActivity(ActivityIcon.StuntJump),

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
                SystemIcon.Garage => Get("gear"),
                SystemIcon.Upgrades => Get("plus"),
                SystemIcon.Shop => Get("shopping_cart"),
                SystemIcon.Achievements => Get("trophy"),
                SystemIcon.Club => Get("home"),
                SystemIcon.Season => Get("trophy"),
                SystemIcon.DailyAdventure => Get("star"),
                SystemIcon.WeekendEvent => Get("flag"),
                SystemIcon.Underground => Get("flag"),
                SystemIcon.Contracts => Get("check"),
                SystemIcon.CityEvents => Get("star"),
                SystemIcon.Collection => Get("star"),
                SystemIcon.CityLegend => Get("trophy"),
                SystemIcon.VehicleHistory => Get("car"),
                SystemIcon.Specialization => Get("gear"),
                SystemIcon.Credits => Get("coin"),
                SystemIcon.Reputation => Get("star"),
                SystemIcon.Level => Get("trophy"),
                SystemIcon.TurboXp => Get("star"),
                SystemIcon.Reward => Get("coin"),
                SystemIcon.Locked => Get("locked"),
                SystemIcon.Unlocked => Get("unlocked"),
                SystemIcon.Confirm => Get("check"),
                SystemIcon.Add => Get("plus"),
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
            Get("pause");

        public static Sprite More =>
            Get("menu_grid");

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

            if (normalized == "garage")
            {
                return Garage;
            }

            string resourceName =
                normalized switch
                {
                    "drift" =>
                        "DriftMarkerVfx",

                    "delivery" =>
                        "DeliveryMarkerVfx",

                    "sprint" =>
                        "SprintMarkerVfx",

                    "circuit" =>
                        "CircuitMarkerVfx",

                    "discovery" or
                    "speedtrap" or
                    "driftspot" =>
                        "DiscoveryMarkerVfx",

                    "underground" =>
                        "UndergroundMarkerVfx",

                    "profession" =>
                        "ProfessionMarkerVfx",

                    "carwash" =>
                        "CarWashMarkerVfx",

                    "tow" or
                    "towtruck" =>
                        "TowMarkerVfx",

                    _ =>
                        string.Empty
                };

            if (string.IsNullOrEmpty(
                    resourceName))
            {
                return GetFallback();
            }

            string cacheKey =
                "world:" +
                resourceName;

            if (Cache.TryGetValue(
                    cacheKey,
                    out Sprite cached))
            {
                return cached;
            }

            GameObject prefab =
                Resources.Load<GameObject>(
                    "MotorCity/Markers/" +
                    resourceName);

            Sprite sprite =
                null;

            if (prefab != null)
            {
                SpriteRenderer[] renderers =
                    prefab.GetComponentsInChildren<SpriteRenderer>(
                        true);

                foreach (SpriteRenderer renderer in renderers)
                {
                    if (renderer == null ||
                        renderer.sprite == null)
                    {
                        continue;
                    }

                    if (renderer.name.IndexOf(
                            "Icon Core",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        sprite =
                            renderer.sprite;
                        break;
                    }

                    sprite ??=
                        renderer.sprite;
                }
            }

            sprite ??=
                GetFallback();

            Cache[cacheKey] =
                sprite;

            return sprite;
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

                "carwash" =>
                    new Color(0.10f, 0.82f, 1f, 1f),

                "tow" or
                "towtruck" =>
                    new Color(1f, 0.56f, 0.06f, 1f),

                "garage" =>
                    new Color(0.72f, 0.20f, 1f, 1f),

                _ =>
                    new Color(1f, 0.70f, 0.10f, 1f)
            };
        }

        private static Sprite GetFallback()
        {
            if (Cache.TryGetValue(
                    FallbackIconName,
                    out Sprite cached))
            {
                return cached;
            }

            Sprite fallback =
                Resources.Load<Sprite>(
                    Root + FallbackIconName);

            Cache[FallbackIconName] =
                fallback;

            if (fallback == null)
            {
                WarnMissingOnce(
                    FallbackIconName);
            }

            return fallback;
        }

        private static void WarnMissingOnce(
            string iconName)
        {
            if (!MissingWarnings.Add(
                    iconName))
            {
                return;
            }

#if UNITY_EDITOR || DEBUG
            Debug.LogWarning(
                "[MotorCity][Icons] Missing icon: " +
                Root +
                iconName);
#endif
        }
    }
}
