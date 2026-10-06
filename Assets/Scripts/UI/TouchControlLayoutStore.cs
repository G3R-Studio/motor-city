using MotorCity.Input;
using MotorCity.Persistence;
using UnityEngine;

namespace MotorCity.UI
{
    internal static class TouchControlLayoutStore
    {
        private const string Prefix =
            "MotorCity.TouchLayout.v1.";

        public static Vector2 Load(
            MotorCityControlScheme scheme,
            string controlKey,
            Vector2 fallback)
        {
            string key =
                BaseKey(
                    scheme,
                    controlKey);

            return new Vector2(
                MotorCitySaveService.GetFloat(
                    key + ".x",
                    fallback.x),
                MotorCitySaveService.GetFloat(
                    key + ".y",
                    fallback.y));
        }

        public static void Save(
            MotorCityControlScheme scheme,
            string controlKey,
            Vector2 position)
        {
            string key =
                BaseKey(
                    scheme,
                    controlKey);

            MotorCitySaveService.SetFloat(
                key + ".x",
                position.x);

            MotorCitySaveService.SetFloat(
                key + ".y",
                position.y);

            MotorCitySaveService.Save();
        }

        public static void Reset(
            MotorCityControlScheme scheme,
            string controlKey)
        {
            string key =
                BaseKey(
                    scheme,
                    controlKey);

            MotorCitySaveService.DeleteKey(
                key + ".x");

            MotorCitySaveService.DeleteKey(
                key + ".y");

            MotorCitySaveService.Save();
        }

        private static string BaseKey(
            MotorCityControlScheme scheme,
            string controlKey)
        {
            return
                Prefix +
                scheme +
                "." +
                controlKey;
        }
    }
}
