using UnityEngine;
using UnityEngine.EventSystems;

namespace MotorCity.UI
{
    [DisallowMultipleComponent]
    public sealed class UiButtonFeedback :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [SerializeField]
        private float hoverScale = 1.018f;

        [SerializeField]
        private float pressedScale = 0.975f;

        [SerializeField]
        private float responseSpeed = 18f;

        private Vector3 baseScale;
        private bool hovered;
        private bool pressed;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            baseScale = transform.localScale;
            hovered = false;
            pressed = false;
        }

        private void OnDisable()
        {
            transform.localScale = baseScale;
            hovered = false;
            pressed = false;
        }

        private void Update()
        {
            float multiplier =
                pressed
                    ? pressedScale
                    : hovered
                        ? hoverScale
                        : 1f;

            Vector3 targetScale =
                baseScale * multiplier;

            transform.localScale =
                Vector3.Lerp(
                    transform.localScale,
                    targetScale,
                    1f - Mathf.Exp(
                        -responseSpeed *
                        Time.unscaledDeltaTime));
        }

        public void OnPointerEnter(
            PointerEventData eventData)
        {
            hovered = true;
        }

        public void OnPointerExit(
            PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }

        public void OnPointerDown(
            PointerEventData eventData)
        {
            pressed = true;
        }

        public void OnPointerUp(
            PointerEventData eventData)
        {
            pressed = false;
        }
    }
}
