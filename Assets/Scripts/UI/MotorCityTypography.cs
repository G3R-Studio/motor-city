using UnityEngine;

namespace MotorCity.UI
{
    public static class MotorCityTypography
    {
        private static Font regular;
        private static Font bold;
        public static Font Regular => regular != null ? regular :
            regular = Resources.Load<Font>("MotorCity/Fonts/RobotoCondensed-Regular")
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font Bold => bold != null ? bold :
            bold = Resources.Load<Font>("MotorCity/Fonts/RobotoCondensed-Bold") ?? Regular;
    }
}