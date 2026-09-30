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

            Vector3 endPosition =
                transform.position;

            Quaternion endRotation =
                transform.rotation;

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

            // Three authored legs plus a final dynamic hand-off to the
            // current gameplay camera pose. The exact authored points are
            // reached at 0%, 30% and 62% of the shot.
            if (progress < 0.30f)
            {
                float leg =
                    SmoothLeg(
                        progress / 0.30f);

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
            else if (progress < 0.62f)
            {
                float leg =
                    SmoothLeg(
                        (progress - 0.30f) /
                        0.32f);

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
                float leg =
                    SmoothLeg(
                        (progress - 0.62f) /
                        0.38f);

                position =
                    Vector3.Lerp(
                        AuthoredPoint3Position,
                        endPosition,
                        leg);

                rotation =
                    Quaternion.Slerp(
                        point3Rotation,
                        endRotation,
                        leg);
            }

            cinematicCamera.transform.SetPositionAndRotation(
                position,
                rotation);

            cinematicCamera.fieldOfView =
                Mathf.Lerp(
                    54f,
                    gameplayCamera.fieldOfView,
                    Mathf.SmoothStep(
                        0.62f,
                        1f,
                        progress));

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

        private static float SmoothLeg(
            float value)
        {
            value =
                Mathf.Clamp01(
                    value);

            return
                value * value *
                (3f - 2f * value);
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
