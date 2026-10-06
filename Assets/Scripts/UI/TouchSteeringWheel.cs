using MotorCity.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MotorCity.UI
{
    public sealed class TouchSteeringWheel :
        MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
    {
        [SerializeField] private float maximumRotation = 120f;
        [SerializeField] private float returnSpeed = 260f;
        [SerializeField] private float dragFollowSpeed = 720f;
        [SerializeField] private float pointerDeadRadiusPixels = 24f;
        [SerializeField] private float outputDeadZone = 0.025f;

        private RectTransform rect;
        private bool dragging;
        private int activePointerId = int.MinValue;
        private float pointerAngleAtPress;
        private float wheelAngleAtPress;
        private float targetWheelAngle;
        private float wheelAngle;

        private void Awake()
        {
            rect =
                transform as RectTransform;
        }

        private void Update()
        {
            float target =
                dragging
                    ? targetWheelAngle
                    : 0f;

            float speed =
                dragging
                    ? dragFollowSpeed
                    : returnSpeed;

            wheelAngle =
                Mathf.MoveTowardsAngle(
                    wheelAngle,
                    target,
                    speed *
                    Time.unscaledDeltaTime);

            if (!dragging &&
                Mathf.Abs(wheelAngle) < 0.05f)
            {
                wheelAngle =
                    0f;
            }

            Apply();
        }

        public void OnPointerDown(
            PointerEventData eventData)
        {
            if (dragging)
                return;

            dragging =
                true;

            activePointerId =
                eventData.pointerId;

            pointerAngleAtPress =
                PointerAngle(
                    eventData);

            wheelAngleAtPress =
                wheelAngle;

            targetWheelAngle =
                wheelAngle;
        }

        public void OnDrag(
            PointerEventData eventData)
        {
            if (!dragging ||
                eventData.pointerId != activePointerId)
            {
                return;
            }

            if (PointerRadius(
                    eventData) <
                pointerDeadRadiusPixels)
            {
                return;
            }

            float currentAngle =
                PointerAngle(
                    eventData);

            float delta =
                Mathf.DeltaAngle(
                    pointerAngleAtPress,
                    currentAngle);

            targetWheelAngle =
                Mathf.Clamp(
                    wheelAngleAtPress +
                    delta,
                    -maximumRotation,
                    maximumRotation);
        }

        public void OnPointerUp(
            PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
                return;

            dragging =
                false;

            activePointerId =
                int.MinValue;

            targetWheelAngle =
                0f;
        }

        private float PointerAngle(
            PointerEventData eventData)
        {
            if (rect == null)
                return 0f;

            Vector2 center =
                RectTransformUtility.WorldToScreenPoint(
                    eventData.pressEventCamera,
                    rect.position);

            Vector2 direction =
                eventData.position -
                center;

            return
                Mathf.Atan2(
                    direction.y,
                    direction.x) *
                Mathf.Rad2Deg;
        }

        private float PointerRadius(
            PointerEventData eventData)
        {
            if (rect == null)
                return 0f;

            Vector2 center =
                RectTransformUtility.WorldToScreenPoint(
                    eventData.pressEventCamera,
                    rect.position);

            return
                Vector2.Distance(
                    eventData.position,
                    center);
        }

        private void Apply()
        {
            if (rect != null)
            {
                rect.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        wheelAngle);
            }

            float steering =
                maximumRotation <= 0.01f
                    ? 0f
                    : -wheelAngle /
                      maximumRotation;

            if (Mathf.Abs(steering) <
                outputDeadZone)
            {
                steering =
                    0f;
            }

            MotorCityInput.SetVirtualSteering(
                steering);
        }

        private void OnDisable()
        {
            dragging =
                false;
            activePointerId =
                int.MinValue;
            targetWheelAngle =
                0f;
            wheelAngle =
                0f;

            if (rect != null)
            {
                rect.localRotation =
                    Quaternion.identity;
            }

            MotorCityInput.SetVirtualSteering(
                0f);
        }
    }
}
