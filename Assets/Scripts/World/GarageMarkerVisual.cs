using System.Collections.Generic;
using MotorCity.Gameplay;
using UnityEngine;

namespace MotorCity.World
{
    public sealed class GarageMarkerVisual : MonoBehaviour
    {
        private GarageUpgradeSystem garage;
        private Renderer[] markerRenderers;
        private Material[] markerMaterials;

        public void Bind(GarageUpgradeSystem target)
        {
            garage = target;
            CacheVisuals();

            if (garage != null)
                transform.position = garage.GarageCenter;
        }

        private void CacheVisuals()
        {
            markerRenderers = GetComponentsInChildren<Renderer>(true);
            List<Material> materials = new();

            foreach (Renderer renderer in markerRenderers)
            {
                if (renderer == null) continue;
                materials.AddRange(renderer.materials);
            }

            markerMaterials = materials.ToArray();
        }

        private void Update()
        {
            if (garage == null) return;

            transform.position = garage.GarageCenter;

            Color tint = garage.IsOpen
                ? new Color(0.95f, 0.42f, 1f)
                : new Color(0.78f, 0.42f, 1f);

            if (markerMaterials == null) return;

            foreach (Material material in markerMaterials)
            {
                if (material == null) continue;

                Color current = material.HasProperty("_BaseColor")
                    ? material.GetColor("_BaseColor")
                    : material.color;

                Color next =
                    Color.Lerp(
                        current,
                        tint,
                        Time.deltaTime * 3f);

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", next);
                else if (material.HasProperty("_Color"))
                    material.color = next;
            }
        }
    }
}
