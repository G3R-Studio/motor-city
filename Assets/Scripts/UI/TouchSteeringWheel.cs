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

        private RectTransform rect;
        private bool dragging;
        private float pointerAngleAtPress;
        private float wheelAngleAtPress;
        private float wheelAngle;

        private void Awake()
        {
            rect =
                transform as RectTransform;
        }

        private void Update()
        {
            if (!dragging)
            {
                wheelAngle =
                    Mathf.MoveTowards(
                        wheelAngle,
                        0f,
                        returnSpeed *
                        Time.unscaledDeltaTime);

                Apply();
            }
        }

        public void OnPointerDown(
            PointerEventData eventData)
        {
            dragging =
                true;

            pointerAngleAtPress =
                PointerAngle(
                    eventData);

            wheelAngleAtPress =
                wheelAngle;
        }

        public void OnDrag(
            PointerEventData eventData)
        {
            if (!dragging)
                return;

            float currentAngle =
                PointerAngle(
                    eventData);

            float delta =
                Mathf.DeltaAngle(
                    pointerAngleAtPress,
                    currentAngle);

            wheelAngle =
                Mathf.Clamp(
                    wheelAngleAtPress +
                    delta,
                    -maximumRotation,
                    maximumRotation);

            Apply();
        }

        public void OnPointerUp(
            PointerEventData eventData)
        {
            dragging =
                false;
        }

        private float PointerAngle(
            PointerEventData eventData)
        {
            if (rect == null)
                return 0f;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 local))
            {
                return 0f;
            }

            return
                Mathf.Atan2(
                    local.y,
                    local.x) *
                Mathf.Rad2Deg;
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

            MotorCityInput.SetVirtualSteering(
                maximumRotation <= 0.01f
                    ? 0f
                    : -wheelAngle /
                      maximumRotation);
        }

        private void OnDisable()
        {
            dragging =
                false;
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
