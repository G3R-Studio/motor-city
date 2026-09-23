using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class HaonByteVisualImporter
{
    private const string OutputDirectory =
        "Assets/Resources/MotorCity/Byte";

    private const string OutputPrefab =
        OutputDirectory +
        "/HaonByteVisual.prefab";

    private const string OutputController =
        OutputDirectory +
        "/HaonPixie.controller";

    private const string PreferredSource =
        "Assets/Haons SD series Pack/Prefab/CharacterSet/prf_Set Costume03 Misaki.prefab";

    private const string BuildSessionKey =
        "MotorCity.HaonByteVisualBuilt.V8";

    static HaonByteVisualImporter()
    {
        EditorApplication.delayCall +=
            TryAutoBuild;
    }

    [MenuItem("Motor City/Pixie/Rebuild From HAON SD Bundle")]
    private static void RebuildFromMenu()
    {
        Build(true);
    }

    [MenuItem("Motor City/Pixie/Locate HAON SD Bundle")]
    private static void LocateBundle()
    {
        string path =
            FindBestSourcePath();

        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogWarning(
                "Motor City: HAON SD Series Free Bundle was not found. " +
                "Import Asset Store package 84992 first.");
            return;
        }

        UnityEngine.Object asset =
            AssetDatabase.LoadMainAssetAtPath(
                path);

        Selection.activeObject =
            asset;

        EditorGUIUtility.PingObject(
            asset);

        Debug.Log(
            "Motor City: HAON SD source candidate: " +
            path);
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!HasHaonAssets())
            return;

        if (SessionState.GetBool(
                BuildSessionKey,
                false))
        {
            return;
        }

        SessionState.SetBool(
            BuildSessionKey,
            true);

        // Rebuild once per editor session so fixes to the selected HAON
        // character set propagate even when an older generated prefab exists.
        Build(false);
    }

    private static bool Build(
        bool verbose)
    {
        string sourcePath =
            FindBestSourcePath();

        if (string.IsNullOrWhiteSpace(
                sourcePath))
        {
            if (verbose)
            {
                Debug.LogWarning(
                    "Motor City: HAON SD Series Free Bundle was not found. " +
                    "Open Package Manager > My Assets, import " +
                    "'Haon SD series Free Bundle' (Asset Store 84992), " +
                    "then run this command again.");
            }

            return false;
        }

        GameObject source =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                sourcePath);

        if (source == null)
            return false;

        Directory.CreateDirectory(
            OutputDirectory);

        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                source) as GameObject;

        if (instance == null)
        {
            instance =
                UnityEngine.Object.Instantiate(
                    source);
        }

        if (instance == null)
            return false;

        instance.name =
            "HaonByteVisual";

        try
        {
            RemoveGameplayComponents(
                instance);

            Animator animator =
                instance.GetComponentInChildren<Animator>(
                    true);

            if (animator == null)
            {
                animator =
                    instance.AddComponent<Animator>();
            }

            RuntimeAnimatorController controller =
                BuildAnimatorController();

            if (controller != null)
            {
                // CharacterSet prefabs can ship with their own controller.
                // Byte must always use our generated controller because the
                // runtime code addresses Pixie-specific state names.
                animator.runtimeAnimatorController =
                    controller;
            }

            EnsureCompleteCharacterVisible(
                instance);

            GameObject saved =
                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    OutputPrefab);

            if (saved == null)
                return false;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (verbose)
            {
                Debug.Log(
                    "Motor City: Pixie HAON SD visual built from '" +
                    sourcePath +
                    "'. Runtime path: MotorCity/Byte/HaonByteVisual");
            }

            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                instance);
        }
    }

    private static bool HasHaonAssets()
    {
        return
            AssetDatabase.GetAllAssetPaths()
                .Any(IsHaonPath);
    }

    private static string FindBestSourcePath()
    {
        GameObject preferred =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PreferredSource);

        if (preferred != null &&
            preferred.GetComponentsInChildren<SkinnedMeshRenderer>(
                true).Length >= 3)
        {
            return PreferredSource;
        }

        string[] guids =
            AssetDatabase.FindAssets(
                "t:GameObject");

        string bestPath = null;
        int bestScore =
            int.MinValue;

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (!IsHaonPath(path) ||
                path.StartsWith(
                    OutputDirectory,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path);

            if (prefab == null)
                continue;

            int score = 0;

            if (path.EndsWith(
                    ".prefab",
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 40;
            }

            if (path.IndexOf(
                    "/Prefab/CharacterSet/",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 200;
            }

            if (path.IndexOf(
                    "/Prefab/CharacterPart/",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score -= 180;
            }

            if (prefab.GetComponentInChildren<Animator>(
                    true) != null)
            {
                score += 35;
            }

            int skinned =
                prefab.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true).Length;

            score +=
                Mathf.Min(
                    30,
                    skinned * 5);

            string lower =
                path.ToLowerInvariant();

            if (lower.Contains("sample") ||
                lower.Contains("demo"))
            {
                score += 12;
            }

            if (lower.Contains("sd"))
                score += 8;

            if (lower.Contains("unitychan") ||
                lower.Contains("unity-chan"))
            {
                score += 8;
            }

            if (lower.Contains("weapon") ||
                lower.Contains("prop") ||
                lower.Contains("accessory"))
            {
                score -= 25;
            }

            if (score >
                bestScore)
            {
                bestScore =
                    score;

                bestPath =
                    path;
            }
        }

        return
            bestPath;
    }

    private static bool IsHaonPath(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string lower =
            path.ToLowerInvariant();

        return
            lower.Contains("haon") ||
            (lower.Contains("sd") &&
             (lower.Contains("unitychan") ||
              lower.Contains("unity-chan")));
    }

    private static void RemoveGameplayComponents(
        GameObject root)
    {
        foreach (Collider collider in
                 root.GetComponentsInChildren<Collider>(
                     true))
        {
            UnityEngine.Object.DestroyImmediate(
                collider);
        }

        foreach (Rigidbody rigidbody in
                 root.GetComponentsInChildren<Rigidbody>(
                     true))
        {
            UnityEngine.Object.DestroyImmediate(
                rigidbody);
        }

        foreach (MonoBehaviour behaviour in
                 root.GetComponentsInChildren<MonoBehaviour>(
                     true))
        {
            if (behaviour == null)
                continue;

            string typeName =
                behaviour.GetType().Name;

            // Keep visual-only spring/dynamic-bone style behaviours.
            if (typeName.IndexOf(
                    "spring",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf(
                    "bone",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            UnityEngine.Object.DestroyImmediate(
                behaviour);
        }
    }

    private static RuntimeAnimatorController BuildAnimatorController()
    {
        AnimationClip idle =
            LoadClip(
                "Assets/Haons SD series Pack/Animation/Common/StandA_idleA.FBX");

        AnimationClip follow =
            LoadClip(
                "Assets/Haons SD series Pack/Animation/Common/RunA_Front.FBX");

        AnimationClip clap =
            LoadClip(
                "Assets/Haons SD series Pack/Animation/Action Adventure(Adv)/Emotion_Clap(StandC).FBX");

        AnimationClip victory =
            LoadClip(
                "Assets/Haons SD series Pack/Animation/Action Adventure(Adv)/VictoryPose_UTCStyleSpin.fbx");

        AnimationClip jump =
            LoadClip(
                "Assets/Haons SD series Pack/Animation/Action Adventure(Adv)/Jump_One-cycle-Type(root).FBX");

        if (idle == null)
        {
            Debug.LogWarning(
                "Motor City: HAON idle animation was not found for Pixie.");

            return null;
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(
                OutputController) != null)
        {
            AssetDatabase.DeleteAsset(
                OutputController);
        }

        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(
                OutputController);

        AnimatorStateMachine machine =
            controller.layers[0].stateMachine;

        AnimatorState idleState =
            AddState(
                machine,
                "Byte Idle",
                idle);

        AddState(
            machine,
            "Byte Follow",
            follow ?? idle);

        AddState(
            machine,
            "Byte Clap",
            clap ?? idle);

        AddState(
            machine,
            "Byte Victory",
            victory ?? clap ?? idle);

        AddState(
            machine,
            "Byte Boost",
            jump ?? follow ?? idle);

        machine.defaultState =
            idleState;

        EditorUtility.SetDirty(
            controller);

        return controller;
    }

    private static AnimatorState AddState(
        AnimatorStateMachine machine,
        string name,
        Motion motion)
    {
        AnimatorState state =
            machine.AddState(
                name);

        state.motion =
            motion;

        return state;
    }

    private static AnimationClip LoadClip(
        string assetPath)
    {
        return
            AssetDatabase.LoadAllAssetsAtPath(
                    assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(
                    clip =>
                        clip != null &&
                        !clip.name.StartsWith(
                            "__preview__",
                            StringComparison.OrdinalIgnoreCase));
    }

    private static int IdleScore(
        AnimationClip clip)
    {
        if (clip == null)
            return int.MinValue;

        string name =
            clip.name.ToLowerInvariant();

        int score = 0;

        if (name.Contains("idle"))
            score += 100;

        if (name.Contains("stand"))
            score += 90;

        if (name.Contains("calm"))
            score += 70;

        if (name.Contains("wait"))
            score += 60;

        if (name.Contains("loop"))
            score += 30;

        if (name.Contains("run") ||
            name.Contains("walk") ||
            name.Contains("attack") ||
            name.Contains("die") ||
            name.Contains("damage"))
        {
            score -= 80;
        }

        return score;
    }

    private static void EnsureCompleteCharacterVisible(
        GameObject root)
    {
        if (root == null)
            return;

        // CharacterSet prefabs are already assembled by the asset author.
        // Do not treat costume/body nodes as mutually-exclusive "skins":
        // disabling those nodes can remove arms, legs, or other body pieces.
        foreach (SkinnedMeshRenderer renderer in
                 root.GetComponentsInChildren<SkinnedMeshRenderer>(
                     true))
        {
            if (renderer == null)
                continue;

            renderer.enabled =
                true;

            if (!renderer.gameObject.activeSelf)
            {
                renderer.gameObject.SetActive(
                    true);
            }
        }
    }

}
