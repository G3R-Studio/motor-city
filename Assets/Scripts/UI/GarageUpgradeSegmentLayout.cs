using UnityEngine;

namespace MotorCity.UI
{
    // Divide the whole track in screen pixels: identical gaps, no subpixel seams.
    [DisallowMultipleComponent]
    public sealed class GarageUpgradeSegmentLayout : MonoBehaviour
    {
        private RectTransform track;
        private Canvas parentCanvas;
        private int lastWidth = -1;
        private float lastScale = -1;
        private void OnEnable()
        {
            track = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            lastWidth = -1;
            Refresh();
        }
        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (track == null || track.childCount == 0) return;
            float scale = Mathf.Max(.01f, parentCanvas != null ? parentCanvas.scaleFactor : 1f);
            int width = Mathf.RoundToInt(track.rect.width * scale);
            if (width == lastWidth && Mathf.Approximately(scale, lastScale)) return;
            lastWidth = width; lastScale = scale;
            int count = track.childCount;
            int gap = Mathf.Max(2, Mathf.RoundToInt(3f * scale));
            gap = Mathf.Min(gap, Mathf.Max(0, (width-count)/Mathf.Max(1,count-1)));
            int available = Mathf.Max(0, width-gap*(count-1));
            for (int i = 0; i < count; i++)
            {
                int start = Mathf.RoundToInt((float)available*i/count) + gap*i;
                int end = Mathf.RoundToInt((float)available*(i+1)/count) + gap*i;
                RectTransform segment = track.GetChild(i) as RectTransform;
                segment.anchorMin = new Vector2(0,0); segment.anchorMax = new Vector2(0,1);
                segment.pivot = new Vector2(0,.5f);
                segment.anchoredPosition = new Vector2(start/scale,0);
                segment.sizeDelta = new Vector2((end-start)/scale,0);
            }
        }
    }
}