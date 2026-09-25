using System.IO;
using UnityEditor;
using UnityEngine;

namespace MotorCity.Editor
{
    public static class AmaneKisoraPixieInstaller
    {
        private const string SourceModel =
            "Assets/SapphiArt/SapphiArtchan/FBX/Sapphiart_model.fbx";

        private const string SourceController =
            "Assets/SapphiArt/SapphiArtchan/Animation/SapphiArtchanAnimController.controller";

        private const string RunningClip =
            "Assets/SapphiArt/SapphiArtchan/Animation/running.anim";

        private const string IdleClip =
            "Assets/SapphiArt/SapphiArtchan/Animation/idle.anim";

        private const string OutputFolder =
            "Assets/Resources/MotorCity/Pixie";

        private const string OutputPrefab =
            OutputFolder +
            "/AmaneKisoraVisual.prefab";

        [InitializeOnLoadMethod]
        private static void QueueAutomaticInstall()
        {
            EditorApplication.delayCall +=
                TryAutomaticInstall;
        }

        private static void TryAutomaticInstall()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    SourceModel) == null ||
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    SourceController) == null)
            {
                return;
            }

            ConfigureLoopingClip(
                RunningClip);
            ConfigureLoopingClip(
                IdleClip);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(
                    OutputPrefab) != null)
            {
                return;
            }

            InstallInternal(
                false);
        }

        [MenuItem(
            "Motor City/Pixie/Install Amane Kisora Exclusive")]
        public static void Install()
        {
            InstallInternal(
                true);
        }

        private static void ConfigureLoopingClip(
            string assetPath)
        {
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    assetPath);

            if (clip == null)
                return;

            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(
                    clip);

            if (settings.loopTime)
                return;

            settings.loopTime =
                true;

            AnimationUtility.SetAnimationClipSettings(
                clip,
                settings);

            EditorUtility.SetDirty(
                clip);

            AssetDatabase.SaveAssets();
        }

        private static void InstallInternal(
            bool showDialogs)
        {
            ConfigureLoopingClip(
                RunningClip);
            ConfigureLoopingClip(
                IdleClip);

            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    SourceModel);

            RuntimeAnimatorController controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    SourceController);

            if (source == null ||
                controller == null)
            {
                if (showDialogs)
                {
                    EditorUtility.DisplayDialog(
                        "Motor City",
                        "Не найден импортированный пакет Amane Kisora-chan в Assets/SapphiArt.",
                        "OK");
                }

                return;
            }

            Directory.CreateDirectory(
                OutputFolder);

            GameObject instance =
                PrefabUtility.InstantiatePrefab(
                    source) as GameObject;

            if (instance == null)
            {
                instance =
                    Object.Instantiate(
                        source);
            }

            try
            {
                instance.name =
                    "AmaneKisoraVisual";

                instance.transform.position =
                    Vector3.zero;
                instance.transform.rotation =
                    Quaternion.identity;
                instance.transform.localScale =
                    Vector3.one;

                Animator animator =
                    instance.GetComponent<Animator>();

                if (animator == null)
                {
                    animator =
                        instance.GetComponentInChildren<Animator>(
                            true);
                }

                if (animator == null)
                {
                    animator =
                        instance.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController =
                    controller;
                animator.applyRootMotion =
                    false;
                animator.cullingMode =
                    AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode =
                    AnimatorUpdateMode.Normal;

                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    OutputPrefab);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                GameObject result =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        OutputPrefab);

                if (showDialogs)
                {
                    Selection.activeObject =
                        result;

                    EditorUtility.DisplayDialog(
                        "Motor City",
                        "Kisora подключена как эксклюзивный облик Пикси.\n" +
                        OutputPrefab,
                        "Готово");
                }
                else
                {
                    Debug.Log(
                        "[MotorCity][Pixie] Amane Kisora exclusive prefab generated: " +
                        OutputPrefab);
                }
            }
            finally
            {
                Object.DestroyImmediate(
                    instance);
            }
        }
    }
}
