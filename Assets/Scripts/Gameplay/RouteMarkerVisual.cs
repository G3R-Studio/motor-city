using UnityEngine;

namespace MotorCity.Gameplay
{
    public sealed class RouteMarkerVisual : MonoBehaviour
    {
        private DeliveryActivity activity;
        private Renderer markerRenderer;
        private Material markerMaterial;

        public void Bind(DeliveryActivity targetActivity)
        {
            activity = targetActivity;
            markerRenderer = GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                markerMaterial = markerRenderer.material;
            }
        }

        private void Update()
        {
            if (activity == null) return;

            Vector3 target = activity.CurrentTarget;
            transform.position = new Vector3(target.x, 1.25f + Mathf.Sin(Time.time * 3f) * 0.18f, target.z);
            transform.Rotate(0f, 55f * Time.deltaTime, 0f, Space.World);

            if (markerMaterial != null)
            {
                Color baseColor = activity.IsActive ? new Color(1f, 0.62f, 0.08f) : new Color(0.08f, 0.5f, 1f);
                markerMaterial.color = baseColor;
                if (markerMaterial.HasProperty("_EmissionColor"))
                {
                    markerMaterial.EnableKeyword("_EMISSION");
                    markerMaterial.SetColor("_EmissionColor", baseColor * 2.2f);
                }
            }
        }
    }
}
