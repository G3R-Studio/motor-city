using UnityEngine;

namespace MotorCity.UI
{
    [CreateAssetMenu(
        fileName = "MotorCityUiThemeAssets",
        menuName = "Motor City/UI Theme Assets")]
    public sealed class MotorCityUiThemeAssets : ScriptableObject
    {
        [Header("Minimap marker")]
        public Texture2D minimapPlayerPointer;

        [Header("Touch driving controls")]
        public Texture2D touchThrottle;
        public Texture2D touchBrake;
        public Texture2D touchHandbrake;
        public Texture2D touchWheel;
    }
}
