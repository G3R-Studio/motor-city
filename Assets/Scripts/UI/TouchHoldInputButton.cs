using MotorCity.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MotorCity.UI
{
    public sealed class TouchHoldInputButton :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        private MotorCityInputAction action;
        private bool held;

        public void Bind(
            MotorCityInputAction inputAction)
        {
            action = inputAction;
        }

        public void OnPointerDown(
            PointerEventData eventData)
        {
            held = true;

            MotorCityInput.SetVirtualHeld(
                action,
                true);
        }

        public void OnPointerUp(
            PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(
            PointerEventData eventData)
        {
            Release();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Release()
        {
            if (!held)
                return;

            held = false;

            MotorCityInput.SetVirtualHeld(
                action,
                false);
        }
    }
}
