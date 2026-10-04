using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Runtime effects create materials by name, so scene dependency analysis
// cannot retain these shaders. Keep their variants, including transparency.
public sealed class MotorCityRuntimeShaderBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        Object settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
        var serialized = new SerializedObject(settings);
        SerializedProperty shaders = serialized.FindProperty("m_AlwaysIncludedShaders");
        foreach (string name in new[] {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Particles/Unlit"
        })
        {
            Shader shader = Shader.Find(name);
            if (shader == null)
                throw new BuildFailedException("Motor City runtime shader is missing: " + name);
            bool found = false;
            for (int i = 0; i < shaders.arraySize; i++)
                found |= shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader;
            if (found) continue;
            int index = shaders.arraySize;
            shaders.InsertArrayElementAtIndex(index);
            shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }
}
