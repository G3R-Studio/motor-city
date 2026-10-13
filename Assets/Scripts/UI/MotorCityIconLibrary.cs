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

        private const string TablerRoot = "MotorCity/UI/Icons/Tabler/";

        // Tabler PNGs are generated from the committed MIT-licensed SVG pack.
        // Existing Kenney icons remain a safe fallback until export is run.
        public static Sprite TryGetTabler(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            string cacheKey = "tabler:" + name;
            if (Cache.TryGetValue(cacheKey, out Sprite cached))
                return cached;

            Sprite sprite = Resources.Load<Sprite>(TablerRoot + name);
            if (sprite != null)
                Cache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite TablerOrKenney(string tabler, string kenney)
        {
            return TryGetTabler(tabler) ?? Get(kenney);
        }

        public static Sprite ForActivity(
            ActivityIcon icon)
        {
            return icon switch
            {
                ActivityIcon.Delivery => TablerOrKenney("truck-delivery", "car"),
                ActivityIcon.Drift => TablerOrKenney("car-turbine", "car"),
                ActivityIcon.Sprint => TablerOrKenney("flag-3", "flag"),
                ActivityIcon.Circuit => TablerOrKenney("trophy", "trophy"),
                ActivityIcon.SpeedTrap => TablerOrKenney("gauge", "target"),
                ActivityIcon.DriftSpot => TablerOrKenney("target", "target"),
                ActivityIcon.Discovery => TablerOrKenney("map-pin", "star"),
                ActivityIcon.PhotoHunt => TablerOrKenney("camera", "target"),
                ActivityIcon.Taxi => TablerOrKenney("car", "car"),
                ActivityIcon.PizzaDelivery => TablerOrKenney("pizza", "car"),
                ActivityIcon.Mail => TablerOrKenney("mail", "car"),
                ActivityIcon.CarWash => TablerOrKenney("spray", "car"),
                ActivityIcon.TowTruck => TablerOrKenney("car-crane", "key"),
                ActivityIcon.CityProfession => TablerOrKenney("briefcase", "gear"),
                ActivityIcon.Underground => TablerOrKenney("moon-stars", "flag"),
                _ => GetFallback()
            };
        }

        public static Sprite ForActivity(
            string activityId,
            bool success = true)
        {
            if (!success)
                return TablerOrKenney("target", "target");

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
                SystemIcon.Garage => TablerOrKenney("car-garage", "gear"),
                SystemIcon.Upgrades => TablerOrKenney("tools", "plus"),
                SystemIcon.Shop => TablerOrKenney("shopping-cart", "shopping_cart"),
                SystemIcon.Achievements => TablerOrKenney("trophy", "trophy"),
                SystemIcon.Club => TablerOrKenney("users-group", "home"),
                SystemIcon.Season => TablerOrKenney("calendar-event", "trophy"),
                SystemIcon.DailyAdventure => TablerOrKenney("sun", "star"),
                SystemIcon.Underground => TablerOrKenney("moon-stars", "flag"),
                SystemIcon.Contracts => TablerOrKenney("clipboard-list", "check"),
                SystemIcon.CityEvents => TablerOrKenney("calendar-event", "star"),
                SystemIcon.Collection => TablerOrKenney("star", "star"),
                SystemIcon.CityLegend => TablerOrKenney("crown", "trophy"),
                SystemIcon.VehicleHistory => TablerOrKenney("history", "car"),
                SystemIcon.Specialization => TablerOrKenney("certificate", "gear"),
                SystemIcon.Credits => TablerOrKenney("coins", "coin"),
                SystemIcon.Reputation => TablerOrKenney("star", "star"),
                SystemIcon.Level => TablerOrKenney("medal", "trophy"),
                SystemIcon.PixieXp => TablerOrKenney("sparkles", "star"),
                SystemIcon.Reward => TablerOrKenney("gift", "coin"),
                SystemIcon.Locked => TablerOrKenney("lock", "locked"),
                SystemIcon.Unlocked => TablerOrKenney("lock-open", "unlocked"),
                SystemIcon.Confirm => TablerOrKenney("check", "check"),
                SystemIcon.Add => TablerOrKenney("plus", "plus"),
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
            TablerOrKenney("player-pause", "pause");

        public static Sprite More =>
            TablerOrKenney("layout-grid", "menu_grid");

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

            // All in-game markers, the navigator and the minimap now share
            // the Tabler collection. For the sprint checkpoint, use a
            // directional Tabler arrow: the rotation is gameplay-relevant.
            // Keep the old prefab sprite lookup below as a migration fallback.
            string tablerName = normalized switch
            {
                "garage" => "car-garage",
                "delivery" => "truck-delivery",
                "drift" => "car-turbine",
                "sprint" => "arrow-right",
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

            if (tablerName != null)
            {
                Sprite tablerSprite = TryGetTabler(tablerName);
                if (tablerSprite != null)
                    return tablerSprite;
            }

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

                    "carwash" or
                    "profession_carwash" =>
                        "CarWashMarkerVfx",

                    "tow" or
                    "towtruck" or
                    "profession_tow" =>
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
            if (Cache.TryGetValue(
                    FallbackIconName,
                    out Sprite cached))
            {
                return cached;
            }

            Sprite fallback =
                TryGetTabler(FallbackIconName) ??
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
