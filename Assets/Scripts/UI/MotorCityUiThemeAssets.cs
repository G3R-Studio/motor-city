using UnityEngine;

namespace MotorCity.UI
{
    [CreateAssetMenu(
        fileName = "MotorCityUiThemeAssets",
        menuName = "Motor City/UI Theme Assets")]
    public sealed class MotorCityUiThemeAssets : ScriptableObject
    {
        [Header("Ville Seppanen Racing HUD")]
        public Texture2D speedometerPrimary;
        public Texture2D speedometerSecondary;
        public Texture2D needleLong;
        public Texture2D rectanglePanel;
        public Texture2D characterPanel;
        public Texture2D statusPanel;
        public Texture2D driftPanel;
        public Texture2D targetPanel;
        public Texture2D driveModePanel;
        public Texture2D minimapPlayerPointer;
        public Texture2D warning;
        public Texture2D engine;
        public Texture2D nitrous;
    }
}
