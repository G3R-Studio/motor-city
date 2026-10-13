using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    // Prevent non-uniform UI-parent scaling from stretching icon artwork.
    // This lives on the visual child, not on the button hit area.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MotorCityIconAspectGuard : MonoBehaviour
    {
        private RectTransform iconRect;
        private Image spriteImage;
        private RawImage rawImage;
        private Vector3 initialScale;
        private Vector2 slotSize;

        private void Awake()
        {
            iconRect = (RectTransform)transform;
            spriteImage = GetComponent<Image>();
            rawImage = GetComponent<RawImage>();
            initialScale = iconRect.localScale;
            slotSize = iconRect.sizeDelta;
        }

        private void OnEnable()
        {
            UpdateAspect();
        }

        private void LateUpdate()
        {
            // Parent animations and CanvasScaler changes may happen at runtime.
            UpdateAspect();
        }

        private void UpdateAspect()
        {
            if (iconRect == null || iconRect.parent == null)
                return;

            if (spriteImage != null && spriteImage.sprite != null &&
                !spriteImage.preserveAspect)
                spriteImage.preserveAspect = true;

            float parentX = iconRect.parent.TransformVector(Vector3.right).magnitude;
            float parentY = iconRect.parent.TransformVector(Vector3.up).magnitude;
            if (parentX <= 0.0001f || parentY <= 0.0001f)
                return;

            // Shrink the stretched axis instead of enlarging the other one.
            float x = Mathf.Min(1f, parentY / parentX);
            float y = Mathf.Min(1f, parentX / parentY);
            Vector3 targetScale = new Vector3(
                initialScale.x * x, initialScale.y * y, initialScale.z);
            if ((iconRect.localScale - targetScale).sqrMagnitude > 0.000001f)
                iconRect.localScale = targetScale;

            // RawImage has no preserveAspect flag. Fit the source texture
            // inside its authored, non-stretched icon slot.
            if (rawImage == null || rawImage.texture == null ||
                iconRect.anchorMin != iconRect.anchorMax)
                return;

            float width = Mathf.Abs(slotSize.x);
            float height = Mathf.Abs(slotSize.y);
            if (width < 0.001f || height < 0.001f)
                return;

            Texture texture = rawImage.texture;
            float sourceAspect = texture.width /
                (float)Mathf.Max(1, texture.height);
            width = Mathf.Min(width, height * sourceAspect);
            height = width / sourceAspect;
            Vector2 fitted = new Vector2(width, height);
            if ((iconRect.sizeDelta - fitted).sqrMagnitude > 0.0001f)
                iconRect.sizeDelta = fitted;
        }
    }
}
