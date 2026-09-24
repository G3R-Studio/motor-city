using UnityEngine;

namespace MotorCity.World
{
    public sealed class ActivityMarkerVfxAnimator : MonoBehaviour
    {
        [SerializeField] private Transform iconRoot;
        [SerializeField] private float bobAmplitude = 0.16f;
        [SerializeField] private float bobSpeed = 1.75f;
        [SerializeField] private float yawSpeed = 34f;
        [SerializeField] private float pulseAmplitude = 0.035f;
        [SerializeField] private float pulseSpeed = 2.1f;

        private float iconBaseHeight;
        private Vector3 iconBaseScale;

        public void Configure(
            Transform targetIcon)
        {
            iconRoot =
                targetIcon;

            CaptureBasePose();
        }

        private void Awake()
        {
            if (iconRoot == null)
            {
                iconRoot =
                    transform.Find(
                        "Mission Icon");
            }

            CaptureBasePose();
        }

        private void Update()
        {
            if (iconRoot == null)
                return;

            float time =
                Time.unscaledTime;

            Vector3 position =
                iconRoot.localPosition;

            position.y =
                iconBaseHeight +
                Mathf.Sin(
                    time *
                    bobSpeed) *
                bobAmplitude;

            iconRoot.localPosition =
                position;

            iconRoot.Rotate(
                0f,
                yawSpeed *
                Time.unscaledDeltaTime,
                0f,
                Space.Self);

            float pulse =
                1f +
                Mathf.Sin(
                    time *
                    pulseSpeed) *
                pulseAmplitude;

            iconRoot.localScale =
                iconBaseScale *
                pulse;
        }

        private void CaptureBasePose()
        {
            if (iconRoot == null)
                return;

            iconBaseHeight =
                iconRoot.localPosition.y;

            iconBaseScale =
                iconRoot.localScale;
        }
    }
}
