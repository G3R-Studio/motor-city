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

        private Vector3 startPosition;
        private Vector3 controlPointA;
        private Vector3 controlPointB;

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
                target == null ||
                gameplayCamera == null)
            {
                return;
            }

            timer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer /
                    duration);

            float eased =
                progress * progress *
                (3f - 2f * progress);

            Vector3 endPosition =
                transform.position;

            Vector3 position =
                CubicBezier(
                    startPosition,
                    controlPointA,
                    controlPointB,
                    endPosition,
                    eased);

            cinematicCamera.transform.position =
                position;

            Vector3 lookPoint =
                target.position +
                Vector3.up * 0.9f;

            Quaternion lookRotation =
                Quaternion.LookRotation(
                    lookPoint - position,
                    Vector3.up);

            Quaternion endRotation =
                transform.rotation;

            cinematicCamera.transform.rotation =
                Quaternion.Slerp(
                    lookRotation,
                    endRotation,
                    Mathf.SmoothStep(
                        0.72f,
                        1f,
                        progress));

            cinematicCamera.fieldOfView =
                Mathf.Lerp(
                    54f,
                    gameplayCamera.fieldOfView,
                    eased);

            if (progress >= 1f)
            {
                FinishCinematic();
            }
        }

        private void BeginCinematic()
        {
            if (target == null)
                return;

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

            Vector3 forward =
                target.forward;

            Vector3 right =
                target.right;

            startPosition =
                target.position -
                forward * 22f -
                right * 13f +
                Vector3.up * 8.5f;

            controlPointA =
                target.position -
                forward * 15f +
                right * 3f +
                Vector3.up * 6.5f;

            controlPointB =
                target.position -
                forward * 7f -
                right * 2f +
                Vector3.up * 3.4f;

            cinematicCamera.transform.position =
                startPosition;

            cinematicCamera.transform.rotation =
                Quaternion.LookRotation(
                    target.position +
                    Vector3.up * 0.9f -
                    startPosition,
                    Vector3.up);

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

        private static Vector3 CubicBezier(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            float t)
        {
            float oneMinusT =
                1f - t;

            return
                oneMinusT * oneMinusT * oneMinusT * a +
                3f * oneMinusT * oneMinusT * t * b +
                3f * oneMinusT * t * t * c +
                t * t * t * d;
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
