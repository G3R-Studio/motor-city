using MotorCity.Localization;
using MotorCity.Persistence;
using MotorCity.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MotorCity.Bootstrap
{
    public sealed class MotorCityBootController :
        MonoBehaviour
    {
        private const string BootSceneName =
            "Boot";
        private const string GameSceneName =
            "Prototype";

        private string status =
            "...";
        private bool loading = true;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForBootScene()
        {
            if (SceneManager.GetActiveScene().name !=
                BootSceneName)
            {
                return;
            }

            if (Object.FindAnyObjectByType<MotorCityBootController>() !=
                null)
            {
                return;
            }

            GameObject root =
                new("Motor City Runtime");

            DontDestroyOnLoad(
                root);

            root.AddComponent<MotorCityBootController>();
        }

        private void Awake()
        {
            MotorCityQualityRuntime.Initialize();
            MotorCityLocalization.SetLanguage(
                "ru");

#if UNITY_WEBGL && !UNITY_EDITOR
            YandexPlatformBridge bridge =
                gameObject.AddComponent<YandexPlatformBridge>();

            MotorCityPlatform.SetService(
                new YandexPlatformService(
                    bridge));
#else
            MotorCityPlatform.SetService(
                new LocalPlatformService());
#endif

            MotorCityPlatformRuntime platformRuntime =
                gameObject.AddComponent<MotorCityPlatformRuntime>();

            gameObject.AddComponent<MotorCitySaveRuntime>();

            MotorCityCloudSaveRuntime cloudRuntime =
                gameObject.AddComponent<MotorCityCloudSaveRuntime>();

            MotorCityRemoteConfigRuntime remoteConfig =
                gameObject.AddComponent<MotorCityRemoteConfigRuntime>();

            MotorCityPurchaseRuntime purchaseRuntime =
                gameObject.AddComponent<MotorCityPurchaseRuntime>();

            status =
                "...";

            platformRuntime.InitializePlatform(
                success =>
                {
                    if (!success)
                    {
                        MotorCityPlatform.SetService(
                            new LocalPlatformService());

                        platformRuntime.InitializePlatform(
                            ignored =>
                                LoadGameScene());

                        return;
                    }

                    MotorCityLocalization.SetLanguage(
                        MotorCityPlatform.LanguageCode);

                    status =
                        MotorCityLocalization.Text(
                            "boot.connecting");

                    status =
                        MotorCityLocalization.Text(
                            "boot.sync");

                    remoteConfig.Load(
                        () =>
                        {
                            purchaseRuntime.RefreshPending(
                                () =>
                                {
                                    if (!MotorCityPlatform.SupportsCloudSave)
                                    {
                                        LoadGameScene();
                                        return;
                                    }

                                    cloudRuntime.ResolveInitialCloud(
                                        LoadGameScene);
                                });
                        });
                });
        }

        private void LoadGameScene()
        {
            status =
                MotorCityLocalization.Text(
                    "boot.loading");

            AsyncOperation operation =
                SceneManager.LoadSceneAsync(
                    GameSceneName,
                    LoadSceneMode.Single);

            if (operation == null)
            {
                Debug.LogError(
                    "Motor City: Prototype scene could not be loaded.");

                status =
                    MotorCityLocalization.Text(
                        "boot.error");
                return;
            }

        }

        public void NotifyGameplayBuilt()
        {
            loading = false;
        }

        private void OnGUI()
        {
            if (!loading)
                return;

            GUIStyle title =
                new(GUI.skin.label)
                {
                    fontSize = 34,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };

            GUIStyle text =
                new(GUI.skin.label)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter
                };

            float width =
                Mathf.Min(
                    560f,
                    Screen.width - 32f);

            Rect titleRect =
                new(
                    (Screen.width - width) * 0.5f,
                    Screen.height * 0.40f,
                    width,
                    54f);

            Rect statusRect =
                new(
                    (Screen.width - width) * 0.5f,
                    titleRect.yMax + 8f,
                    width,
                    40f);

            GUI.Label(
                titleRect,
                "MOTOR CITY",
                title);

            GUI.Label(
                statusRect,
                status,
                text);
        }
    }
}
