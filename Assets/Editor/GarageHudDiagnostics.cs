using MotorCity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.EditorTools
{
    public static class GarageHudDiagnostics
    {
        [MenuItem("Motor City/Debug/Validate Garage UI")]
        private static void Validate()
        {
            GarageReferenceGraphic[] graphics = Object.FindObjectsByType<GarageReferenceGraphic>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (graphics.Length == 0)
            {
                Debug.Log("Garage UI: enter Play mode and open the garage first.");
                return;
            }

            Canvas.ForceUpdateCanvases();
            int active = 0;
            int problems = 0;            Transform garageRoot = graphics[0].transform;
            while (garageRoot.parent != null && garageRoot.name != "Garage Panel")
                garageRoot = garageRoot.parent;
            foreach (Shadow effect in garageRoot.GetComponentsInChildren<Shadow>(true))
            {
                Debug.LogError($"Garage UI: unexpected shadow/outline on {effect.name}.", effect);
                problems++;
            }
            foreach (SpriteLessUI.SpriteLessImage surface in garageRoot.GetComponentsInChildren<SpriteLessUI.SpriteLessImage>(true))
            {
                Debug.LogError($"Garage UI: legacy garage surface remains on {surface.name}.", surface);
                problems++;
            }
            foreach (GarageReferenceGraphic graphic in graphics)
            {
                CanvasRenderer renderer = graphic.GetComponent<CanvasRenderer>();
                if (renderer == null)
                {
                    Debug.LogError($"Garage UI: missing CanvasRenderer on {graphic.name}.", graphic);
                    problems++;
                    continue;
                }
                if (!graphic.isActiveAndEnabled) continue;
                active++;
                Rect rect = graphic.rectTransform.rect;
                if (rect.width <= 0 || rect.height <= 0 || graphic.depth < 0 || renderer.cull)
                {
                    Debug.LogWarning($"Garage UI: {graphic.symbol} on {graphic.name}: " +
                        $"size={rect.size}, depth={graphic.depth}, culled={renderer.cull}.", graphic);
                    problems++;
                }
                if (graphic.raycastTarget)
                {
                    Button button = graphic.GetComponentInParent<Button>();
                    if (button == null || button.targetGraphic != graphic)
                    {
                        Debug.LogError($"Garage UI: navigation hit target is not bound on {graphic.name}.", graphic);
                        problems++;
                    }
                }
            }
            Debug.Log($"Garage UI: {active}/{graphics.Length} graphics active, {problems} problems found.");
        }
    }
}
