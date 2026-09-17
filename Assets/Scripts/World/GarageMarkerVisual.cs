using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class GarageMarkerVisual : MonoBehaviour
    {
        private GarageUpgradeSystem garage;
        private Renderer markerRenderer;
        private Material markerMaterial;
        private Vector3 baseScale;

        public void Bind(GarageUpgradeSystem target)
        {
            garage = target;
            markerRenderer = GetComponent<Renderer>();
            markerMaterial = markerRenderer != null ? markerRenderer.material : null;
            baseScale = transform.localScale;
            if (garage != null)
                transform.position = garage.GarageCenter + Vector3.up * 0.1f;
        }

        private void Update()
        {
            if (garage == null) return;

            float pulse = 1f + Mathf.Sin(Time.time * 3.8f) * 0.075f;
            transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z * pulse);
            transform.position = garage.GarageCenter + Vector3.up * (0.1f + Mathf.Sin(Time.time * 2.5f) * 0.04f);

            if (markerMaterial != null)
            {
                Color target = garage.IsOpen
                    ? new Color(0.95f, 0.28f, 1f, 1f)
                    : new Color(0.72f, 0.16f, 1f, 1f);
                markerMaterial.color = Color.Lerp(markerMaterial.color, target, Time.deltaTime * 6f);
            }
        }
    }
}
