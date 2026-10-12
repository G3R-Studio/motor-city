using UnityEngine;

namespace MotorCity.UI
{
    [CreateAssetMenu(
        fileName = "MotorCityTouchArtAssets",
        menuName = "Motor City/Touch Control Art")]
    public sealed class MotorCityTouchArtAssets : ScriptableObject
    {
        [Header("Touch driving controls")]
        public Texture2D touchThrottle;
        public Texture2D touchBrake;
        public Texture2D touchHandbrake;
        public Texture2D touchWheel;
    }
}
