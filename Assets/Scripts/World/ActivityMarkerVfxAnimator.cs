using UnityEngine;

namespace MotorCity.World
{
    public sealed class ActivityMarkerVfxAnimator : MonoBehaviour
    {
        [SerializeField] private Transform iconRoot;
        [SerializeField] private float bobAmplitude = 0.10f;
        [SerializeField] private float bobSpeed = 1.45f;
        [SerializeField] private float pulseAmplitude = 0.015f;
        [SerializeField] private float pulseSpeed = 1.65f;

        private float iconBaseHeight;
        private Vector3 iconBaseScale;
        private Camera mainCamera;
        private float cameraResolveTimer;

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

            ResolveCamera(
                true);

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

        private void LateUpdate()
        {
            if (iconRoot == null)
                return;

            ResolveCamera(
                false);

            if (mainCamera == null)
                return;

            Vector3 toCamera =
                mainCamera.transform.position -
                iconRoot.position;

            if (toCamera.sqrMagnitude <
                0.0001f)
            {
                return;
            }

            iconRoot.rotation =
                Quaternion.LookRotation(
                    toCamera.normalized,
                    mainCamera.transform.up);
        }

        private void ResolveCamera(
            bool force)
        {
            if (mainCamera != null)
                return;

            if (!force)
            {
                cameraResolveTimer -=
                    Time.unscaledDeltaTime;

                if (cameraResolveTimer > 0f)
                    return;
            }

            cameraResolveTimer =
                0.5f;

            mainCamera =
                Camera.main;
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
