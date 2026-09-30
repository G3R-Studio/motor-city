using MotorCity.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MotorCity.UI
{
    public sealed class TouchPulseInputButton :
        MonoBehaviour,
        IPointerDownHandler
    {
        private MotorCityInputAction action;

        public void Bind(
            MotorCityInputAction inputAction)
        {
            action =
                inputAction;
        }

        public void OnPointerDown(
            PointerEventData eventData)
        {
            MotorCityInput.PulseVirtual(
                action);
        }
    }
}
