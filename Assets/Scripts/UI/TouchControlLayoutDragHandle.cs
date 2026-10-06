using MotorCity.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MotorCity.UI
{
    public sealed class TouchControlLayoutDragHandle :
        MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private RectTransform target;
        private RectTransform boundsRoot;
        private MotorCityControlScheme scheme;
        private string controlKey;
        private Canvas canvas;

        public void Bind(
            RectTransform targetRect,
            RectTransform rootRect,
            MotorCityControlScheme controlScheme,
            string key)
        {
            target =
                targetRect;

            boundsRoot =
                rootRect;

            scheme =
                controlScheme;

            controlKey =
                key;

            canvas =
                GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(
            PointerEventData eventData)
        {
            MotorCityInput.ClearVirtualState();
        }

        public void OnDrag(
            PointerEventData eventData)
        {
            if (target == null ||
                boundsRoot == null)
            {
                return;
            }

            float scale =
                canvas == null
                    ? 1f
                    : Mathf.Max(
                        0.01f,
                        canvas.scaleFactor);

            target.anchoredPosition +=
                eventData.delta /
                scale;

            ClampInsideRoot();
        }

        public void OnEndDrag(
            PointerEventData eventData)
        {
            if (target == null ||
                string.IsNullOrWhiteSpace(
                    controlKey))
            {
                return;
            }

            TouchControlLayoutStore.Save(
                scheme,
                controlKey,
                target.anchoredPosition);
        }

        private void ClampInsideRoot()
        {
            Bounds bounds =
                RectTransformUtility.CalculateRelativeRectTransformBounds(
                    boundsRoot,
                    target);

            Rect rootRect =
                boundsRoot.rect;

            Vector2 correction =
                Vector2.zero;

            if (bounds.min.x <
                rootRect.xMin)
            {
                correction.x +=
                    rootRect.xMin -
                    bounds.min.x;
            }
            else if (bounds.max.x >
                     rootRect.xMax)
            {
                correction.x -=
                    bounds.max.x -
                    rootRect.xMax;
            }

            if (bounds.min.y <
                rootRect.yMin)
            {
                correction.y +=
                    rootRect.yMin -
                    bounds.min.y;
            }
            else if (bounds.max.y >
                     rootRect.yMax)
            {
                correction.y -=
                    bounds.max.y -
                    rootRect.yMax;
            }

            target.anchoredPosition +=
                correction;
        }
    }
}
