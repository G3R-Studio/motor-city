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
        private float visualProgress = 0.04f;
        private float targetProgress = 0.08f;
        private AsyncOperation sceneLoadOperation;
        private float overlayAlpha = 1f;
        private bool fadingOut;
        private bool gameSceneLoadRequested;
        private float platformWatchdogRemaining = 12f;

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
                MotorCityLocalization.Text(
                    "boot.connecting");

            targetProgress =
                0.16f;

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
                            "boot.sync");

                    targetProgress =
                        0.34f;

                    remoteConfig.Load(
                        () =>
                        {
                            targetProgress =
                                0.46f;

                            purchaseRuntime.RefreshPending(
                                () =>
                                {
                                    targetProgress =
                                        0.58f;

                                    if (!MotorCityPlatform.SupportsCloudSave)
                                    {
                                        LoadGameScene();
                                        return;
                                    }

                                    cloudRuntime.ResolveInitialCloud(
                                        () =>
                                        {
                                            targetProgress =
                                                0.70f;

                                            LoadGameScene();
                                        });
                                });
                        });
                });
        }

        private void LoadGameScene()
        {
            if (gameSceneLoadRequested)
                return;

            gameSceneLoadRequested =
                true;

            status =
                MotorCityLocalization.Text(
                    "boot.loading");

            sceneLoadOperation =
                SceneManager.LoadSceneAsync(
                    GameSceneName,
                    LoadSceneMode.Single);

            targetProgress =
                0.74f;

            if (sceneLoadOperation == null)
            {
                Debug.LogError(
                    "Motor City: Prototype scene could not be loaded.");

                status =
                    MotorCityLocalization.Text(
                        "boot.error");
                return;
            }

        }

        private void Update()
        {
            if (!loading)
                return;

            if (!gameSceneLoadRequested &&
                platformWatchdogRemaining > 0f)
            {
                platformWatchdogRemaining -=
                    Time.unscaledDeltaTime;

                if (platformWatchdogRemaining <= 0f)
                {
                    Debug.LogWarning(
                        "Motor City: platform startup timed out. Continuing with local startup.");

                    status =
                        MotorCityLocalization.Text(
                            "boot.loading");

                    MotorCityPlatform.SetService(
                        new LocalPlatformService());

                    MotorCityPlatform.Initialize(
                        ignored =>
                            LoadGameScene());

                    return;
                }
            }

            if (fadingOut)
            {
                overlayAlpha =
                    Mathf.MoveTowards(
                        overlayAlpha,
                        0f,
                        Time.unscaledDeltaTime *
                        2.8f);

                if (overlayAlpha <= 0.001f)
                {
                    overlayAlpha = 0f;
                    loading = false;
                }

                return;
            }

            if (sceneLoadOperation != null)
            {
                float sceneProgress =
                    Mathf.Clamp01(
                        sceneLoadOperation.progress /
                        0.9f);

                targetProgress =
                    Mathf.Max(
                        targetProgress,
                        Mathf.Lerp(
                            0.74f,
                            0.96f,
                            sceneProgress));
            }

            visualProgress =
                Mathf.MoveTowards(
                    visualProgress,
                    targetProgress,
                    Time.unscaledDeltaTime *
                    0.32f);
        }

        public void NotifyGameplayBuilt()
        {
            visualProgress =
                1f;

            targetProgress =
                1f;

            status =
                string.Empty;

            fadingOut =
                true;
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

            Color oldColor =
                GUI.color;

            GUI.color =
                new Color(
                    0.035f,
                    0.045f,
                    0.065f,
                    overlayAlpha);

            GUI.DrawTexture(
                new Rect(
                    0f,
                    0f,
                    Screen.width,
                    Screen.height),
                Texture2D.whiteTexture);

            GUI.color =
                new Color(
                    oldColor.r,
                    oldColor.g,
                    oldColor.b,
                    oldColor.a *
                    overlayAlpha);

            float width =
                Mathf.Min(
                    560f,
                    Screen.width - 32f);

            Rect titleRect =
                new(
                    (Screen.width - width) * 0.5f,
                    Screen.height * 0.38f,
                    width,
                    54f);

            Rect statusRect =
                new(
                    (Screen.width - width) * 0.5f,
                    titleRect.yMax + 10f,
                    width,
                    40f);

            Rect trackRect =
                new(
                    (Screen.width - width) * 0.5f,
                    statusRect.yMax + 18f,
                    width,
                    8f);

            GUI.Label(
                titleRect,
                "MOTOR CITY",
                title);

            GUI.Label(
                statusRect,
                status,
                text);

            GUI.color =
                new Color(
                    0.12f,
                    0.15f,
                    0.20f,
                    1f);

            GUI.DrawTexture(
                trackRect,
                Texture2D.whiteTexture);

            GUI.color =
                new Color(
                    0.18f,
                    0.72f,
                    1f,
                    1f);

            GUI.DrawTexture(
                new Rect(
                    trackRect.x,
                    trackRect.y,
                    trackRect.width *
                    Mathf.Clamp01(
                        visualProgress),
                    trackRect.height),
                Texture2D.whiteTexture);

            GUI.color =
                oldColor;
        }
    }
}
