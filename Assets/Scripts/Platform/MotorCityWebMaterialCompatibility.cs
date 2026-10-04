using UnityEngine;
using UnityEngine.SceneManagement;

namespace MotorCity.Platform
{
    public static class MotorCityWebMaterialCompatibility
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= FixScene;
            SceneManager.sceneLoaded += FixScene;
        }

        private static void FixScene(Scene scene, LoadSceneMode mode)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == null || material.shader.name != "Toon/Toon") continue;
                Texture texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                string textureProperty = material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                Vector2 scale = material.HasProperty(textureProperty) ? material.GetTextureScale(textureProperty) : Vector2.one;
                Vector2 offset = material.HasProperty(textureProperty) ? material.GetTextureOffset(textureProperty) : Vector2.zero;
                material.shader = lit;
                material.shaderKeywords = new string[0];
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", color);
                material.SetTextureScale("_BaseMap", scale);
                material.SetTextureOffset("_BaseMap", offset);
            }
        }
#endif
    }
}
