using UnityEngine;

namespace MotorCity.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class OpeningCinematicCamera : MonoBehaviour
    {
        private Camera gameplayCamera;
        private Transform target;
        private Camera cinematicCamera;
        private ChaseCamera chaseCamera;
        private bool chaseWasEnabled;

        private bool armed;
        private bool active;
        private float timer;
        private float duration = 5f;

        private Vector3 handoffPosition;
        private Vector3 handoffEuler;

        private static readonly Vector3 AuthoredPoint1Position =
            new(-400.104065f, 6.555434576f, 298.150064f);

        private static readonly Vector3 AuthoredPoint1Rotation =
            new(2.92249942f, 302.455048f, 359.967285f);

        private static readonly Vector3 AuthoredPoint2Position =
            new(-420.660156f, 8.148517476f, 300.633828f);

        private static readonly Vector3 AuthoredPoint2Rotation =
            new(24.9240837f, 331.835785f, 359.967957f);

        private static readonly Vector3 AuthoredPoint3Position =
            new(-431.887939f, 4.592940676f, 309.985818f);

        private static readonly Vector3 AuthoredPoint3Rotation =
            new(19.939352f, 47.2972336f, 359.968933f);

        public bool IsActive =>
            active;

        public void Initialize(
            Transform followTarget)
        {
            gameplayCamera =
                GetComponent<Camera>();

            chaseCamera =
                GetComponent<ChaseCamera>();

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

            Vector3 position;
            Vector3 euler;

            Vector3 rotation1 =
                AuthoredPoint1Rotation;

            Vector3 rotation2 =
                UnwrapEuler(
                    AuthoredPoint2Rotation,
                    rotation1);

            Vector3 rotation3 =
                UnwrapEuler(
                    AuthoredPoint3Rotation,
                    rotation2);

            Vector3 rotation4 =
                UnwrapEuler(
                    handoffEuler,
                    rotation3);

            if (progress < 1f / 3f)
            {
                float leg =
                    progress * 3f;

                Vector3 position0 =
                    AuthoredPoint1Position * 2f -
                    AuthoredPoint2Position;

                Vector3 rotation0 =
                    rotation1 * 2f -
                    rotation2;

                position =
                    CatmullRom(
                        position0,
                        AuthoredPoint1Position,
                        AuthoredPoint2Position,
                        AuthoredPoint3Position,
                        leg);

                euler =
                    CatmullRom(
                        rotation0,
                        rotation1,
                        rotation2,
                        rotation3,
                        leg);
            }
            else if (progress < 2f / 3f)
            {
                float leg =
                    (progress - 1f / 3f) *
                    3f;

                position =
                    CatmullRom(
                        AuthoredPoint1Position,
                        AuthoredPoint2Position,
                        AuthoredPoint3Position,
                        handoffPosition,
                        leg);

                euler =
                    CatmullRom(
                        rotation1,
                        rotation2,
                        rotation3,
                        rotation4,
                        leg);
            }
            else
            {
                float leg =
                    (progress - 2f / 3f) *
                    3f;

                Vector3 position5 =
                    handoffPosition * 2f -
                    AuthoredPoint3Position;

                Vector3 rotation5 =
                    rotation4 * 2f -
                    rotation3;

                position =
                    CatmullRom(
                        AuthoredPoint2Position,
                        AuthoredPoint3Position,
                        handoffPosition,
                        position5,
                        leg);

                euler =
                    CatmullRom(
                        rotation2,
                        rotation3,
                        rotation4,
                        rotation5,
                        leg);
            }

            cinematicCamera.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(
                    euler));

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

            handoffEuler =
                transform.eulerAngles;

            if (chaseCamera == null)
            {
                chaseCamera =
                    GetComponent<ChaseCamera>();
            }

            chaseWasEnabled =
                chaseCamera != null &&
                chaseCamera.enabled;

            if (chaseCamera != null)
            {
                chaseCamera.enabled =
                    false;
            }

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

            if (chaseCamera != null)
            {
                chaseCamera.enabled =
                    chaseWasEnabled;
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

        private static Vector3 CatmullRom(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            Vector3 p3,
            float t)
        {
            t =
                Mathf.Clamp01(
                    t);

            float t2 =
                t * t;

            float t3 =
                t2 * t;

            return
                0.5f *
                ((2f * p1) +
                 (-p0 + p2) * t +
                 (2f * p0 -
                  5f * p1 +
                  4f * p2 -
                  p3) * t2 +
                 (-p0 +
                  3f * p1 -
                  3f * p2 +
                  p3) * t3);
        }

        private static Vector3 UnwrapEuler(
            Vector3 value,
            Vector3 reference)
        {
            return
                new Vector3(
                    reference.x +
                    Mathf.DeltaAngle(
                        reference.x,
                        value.x),
                    reference.y +
                    Mathf.DeltaAngle(
                        reference.y,
                        value.y),
                    reference.z +
                    Mathf.DeltaAngle(
                        reference.z,
                        value.z));
        }

        private void OnDisable()
        {
            armed =
                false;

            if (!active)
                return;

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

            if (chaseCamera != null)
            {
                chaseCamera.enabled =
                    chaseWasEnabled;
            }
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
