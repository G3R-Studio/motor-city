using UnityEngine;

namespace MotorCity.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class OpeningCinematicCamera : MonoBehaviour
    {
        private Camera gameplayCamera;
        private Transform target;
        private Camera cinematicCamera;

        private bool armed;
        private bool active;
        private float timer;
        private float duration = 5f;

        private Vector3 handoffPosition;
        private Quaternion handoffRotation;
        private float segment1Length;
        private float segment2Length;
        private float segment3Length;
        private float totalPathLength;

        private static readonly Vector3 AuthoredPoint1Position =
            new(-541.669434f, 6.59657431f, 487.766144f);

        private static readonly Vector3 AuthoredPoint1Rotation =
            new(2.92249942f, 302.455048f, 359.967285f);

        private static readonly Vector3 AuthoredPoint2Position =
            new(-562.225525f, 8.18965721f, 490.249908f);

        private static readonly Vector3 AuthoredPoint2Rotation =
            new(24.9240837f, 331.835785f, 359.967957f);

        private static readonly Vector3 AuthoredPoint3Position =
            new(-573.453308f, 4.63408041f, 499.601898f);

        private static readonly Vector3 AuthoredPoint3Rotation =
            new(19.939352f, 47.2972336f, 359.968933f);

        public bool IsActive =>
            active;

        public void Initialize(
            Transform followTarget)
        {
            gameplayCamera =
                GetComponent<Camera>();

            target =
                followTarget;
        }

        public void Arm(
            float seconds = 5f)
        {
            duration =
                Mathf.Max(
                    1f,
                    seconds);

            timer =
                0f;

            armed =
                true;

            active =
                false;
        }

        public void Replay(
            float seconds = 5f)
        {
            duration =
                Mathf.Max(
                    1f,
                    seconds);

            armed =
                false;

            BeginCinematic();
        }

        private void Update()
        {
            if (armed &&
                Time.timeScale > 0f)
            {
                armed =
                    false;

                BeginCinematic();
            }

            if (!active ||
                gameplayCamera == null ||
                cinematicCamera == null)
            {
                return;
            }

            timer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer /
                    duration);

            float travelledDistance =
                totalPathLength *
                progress;

            Quaternion point1Rotation =
                Quaternion.Euler(
                    AuthoredPoint1Rotation);

            Quaternion point2Rotation =
                Quaternion.Euler(
                    AuthoredPoint2Rotation);

            Quaternion point3Rotation =
                Quaternion.Euler(
                    AuthoredPoint3Rotation);

            Vector3 position;
            Quaternion rotation;

            if (travelledDistance <=
                segment1Length)
            {
                float leg =
                    segment1Length <= 0.001f
                        ? 1f
                        : travelledDistance /
                          segment1Length;

                position =
                    Vector3.Lerp(
                        AuthoredPoint1Position,
                        AuthoredPoint2Position,
                        leg);

                rotation =
                    Quaternion.Slerp(
                        point1Rotation,
                        point2Rotation,
                        leg);
            }
            else if (travelledDistance <=
                     segment1Length +
                     segment2Length)
            {
                float localDistance =
                    travelledDistance -
                    segment1Length;

                float leg =
                    segment2Length <= 0.001f
                        ? 1f
                        : localDistance /
                          segment2Length;

                position =
                    Vector3.Lerp(
                        AuthoredPoint2Position,
                        AuthoredPoint3Position,
                        leg);

                rotation =
                    Quaternion.Slerp(
                        point2Rotation,
                        point3Rotation,
                        leg);
            }
            else
            {
                float localDistance =
                    travelledDistance -
                    segment1Length -
                    segment2Length;

                float leg =
                    segment3Length <= 0.001f
                        ? 1f
                        : localDistance /
                          segment3Length;

                position =
                    Vector3.Lerp(
                        AuthoredPoint3Position,
                        handoffPosition,
                        leg);

                rotation =
                    Quaternion.Slerp(
                        point3Rotation,
                        handoffRotation,
                        leg);
            }

            cinematicCamera.transform.SetPositionAndRotation(
                position,
                rotation);

            cinematicCamera.fieldOfView =
                Mathf.Lerp(
                    54f,
                    gameplayCamera.fieldOfView,
                    progress);

            if (progress >= 1f)
            {
                FinishCinematic();
            }
        }

        private void BeginCinematic()
        {
            if (gameplayCamera == null)
            {
                gameplayCamera =
                    GetComponent<Camera>();
            }

            EnsureCinematicCamera();

            timer =
                0f;

            active =
                true;

            handoffPosition =
                transform.position;

            handoffRotation =
                transform.rotation;

            segment1Length =
                Vector3.Distance(
                    AuthoredPoint1Position,
                    AuthoredPoint2Position);

            segment2Length =
                Vector3.Distance(
                    AuthoredPoint2Position,
                    AuthoredPoint3Position);

            segment3Length =
                Vector3.Distance(
                    AuthoredPoint3Position,
                    handoffPosition);

            totalPathLength =
                Mathf.Max(
                    0.001f,
                    segment1Length +
                    segment2Length +
                    segment3Length);

            cinematicCamera.transform.SetPositionAndRotation(
                AuthoredPoint1Position,
                Quaternion.Euler(
                    AuthoredPoint1Rotation));

            cinematicCamera.fieldOfView =
                54f;

            gameplayCamera.enabled =
                false;

            cinematicCamera.enabled =
                true;
        }

        private void FinishCinematic()
        {
            active =
                false;

            if (cinematicCamera != null)
            {
                cinematicCamera.enabled =
                    false;
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.enabled =
                    true;
            }
        }

        private void EnsureCinematicCamera()
        {
            if (cinematicCamera != null)
                return;

            GameObject cameraObject =
                new(
                    "Opening Cinematic Camera");

            cinematicCamera =
                cameraObject.AddComponent<Camera>();

            if (gameplayCamera != null)
            {
                cinematicCamera.CopyFrom(
                    gameplayCamera);
            }

            cinematicCamera.tag =
                "Untagged";

            cinematicCamera.enabled =
                false;
        }

        private void OnDestroy()
        {
            if (cinematicCamera != null)
            {
                Destroy(
                    cinematicCamera.gameObject);
            }
        }
    }
}
