using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    // Refresh static labels too: rebuilding a shared dynamic font atlas can
    // invalidate their cached UVs when the canvas grows or Best Fit runs.
    [DisallowMultipleComponent]
    public sealed class GarageCanvasRefresh : MonoBehaviour
    {
        private Text[] labels;
        private GarageReferenceGraphic[] graphics;
        private Canvas rootCanvas;
        private Vector2 screenSize;
        private float canvasScale = -1;
        private bool pending;

        private void OnEnable()
        {
            labels = GetComponentsInChildren<Text>(true);
            graphics = GetComponentsInChildren<GarageReferenceGraphic>(true);
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            rootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
            Font.textureRebuilt += OnFontTextureRebuilt;
            pending = true;
        }
        private void OnDisable() => Font.textureRebuilt -= OnFontTextureRebuilt;
        private void OnRectTransformDimensionsChange() => pending = true;
        private void OnFontTextureRebuilt(Font rebuilt)
        {
            if (labels == null) return;
            foreach (Text label in labels)
                if (label != null && label.font == rebuilt) { pending = true; break; }
        }
        private void LateUpdate()
        {
            Vector2 size = new(Screen.width, Screen.height);
            float scale = rootCanvas != null ? rootCanvas.scaleFactor : 1;
            bool resized = size != screenSize || !Mathf.Approximately(scale, canvasScale);
            if (!pending && !resized) return;
            screenSize = size; canvasScale = scale; pending = false;
            foreach (Text label in labels)
            {
                if (label == null) continue;
                label.cachedTextGenerator.Invalidate();
                label.cachedTextGeneratorForLayout.Invalidate();
                label.SetAllDirty();
            }
            if (resized)
                foreach (GarageReferenceGraphic graphic in graphics)
                    if (graphic != null) graphic.SetVerticesDirty();
        }
    }
}