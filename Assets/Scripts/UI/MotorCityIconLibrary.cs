using System.Collections.Generic;
using UnityEngine;

namespace MotorCity.UI
{
    public static class MotorCityIconLibrary
    {
        private const string Root =
            "MotorCity/UI/Icons/Kenney/";

        private static readonly Dictionary<string, Sprite> Cache =
            new();

        public static Sprite Get(
            string iconName)
        {
            if (string.IsNullOrWhiteSpace(
                    iconName))
            {
                return null;
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

            Cache[iconName] =
                sprite;

            if (sprite == null)
            {
                Debug.LogWarning(
                    "[MotorCity][Icons] Missing icon: " +
                    Root +
                    iconName);
            }

            return sprite;
        }

        public static Sprite ForActivity(
            string activityId,
            bool success = true)
        {
            if (!success)
                return Get("target");

            return activityId switch
            {
                "delivery" => Get("car"),
                "drift" => Get("car"),
                "sprint" => Get("flag"),
                "circuit" => Get("trophy"),
                "speedtrap" => Get("target"),
                "driftspot" => Get("target"),
                "stuntjump" => Get("star"),
                "discovery" => Get("star"),
                "profession_taxi" => Get("car"),
                "profession_pizza" => Get("car"),
                "profession_mail" => Get("car"),
                "profession_carwash" => Get("car"),
                "profession_icecream" => Get("coin"),
                "towtruck" => Get("key"),
                "photo_hunt" => Get("target"),
                _ => Get("star")
            };
        }

        public static Sprite Garage =>
            Get("gear");

        public static Sprite Store =>
            Get("shopping_cart");

        public static Sprite Credits =>
            Get("coin");

        public static Sprite Achievement =>
            Get("trophy");

        public static Sprite Locked =>
            Get("locked");

        public static Sprite Unlocked =>
            Get("unlocked");

        public static Sprite Confirm =>
            Get("check");

        public static Sprite Add =>
            Get("plus");
    }
}
