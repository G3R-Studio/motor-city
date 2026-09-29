using MotorCity.Gameplay;
using MotorCity.Localization;
using MotorCity.Persistence;
using MotorCity.Platform;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed class MotorCityFrontEndFlow : MonoBehaviour
    {
        private const string IntroCompleteKey = "MotorCity.FrontEnd.IntroCompleted";
        private const string LanguageKey = "MotorCity.Settings.LanguageOverride";
        private const string AudioMutedKey = "MotorCity.Settings.AudioMuted";
        private const string AudioVolumeKey = "MotorCity.Settings.AudioVolume";

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private FirstSessionOnboardingSystem onboarding;
        private PrototypeHud hud;

        private Canvas canvas;
        private Camera frontEndCamera;
        private bool pregameDebugVisible;

        private GameObject mainRoot;
        private GameObject aboutRoot;
        private GameObject settingsRoot;
        private GameObject introRoot;
        private Text primaryButtonText;
        private Text settingsQualityText;
        private Text settingsAudioText;
        private Text settingsLanguageText;
        private Text introCounterText;
        private Text introTitleText;
        private Text introBodyText;
        private RawImage introImage;
        private RectTransform introImageRect;
        private RawImage introPreviousImage;
        private RectTransform introPreviousImageRect;
        private CanvasGroup introTextGroup;

        private GameObject loadingRoot;
        private RectTransform loadingWheel;
        private Image loadingProgressFill;
        private RectTransform loadingRoad;
        private readonly RectTransform[] loadingRoadDashes =
            new RectTransform[8];
        private Text loadingStatusText;

        private System.Action gameplayLoadRequested;
        private bool loadRequestSent;
        private bool gameplayReady;
        private float gameplayReadyAt;
        private float loadingProgressWhenReady;

        private Font font;
        private MotorCityUiThemeAssets frontEndTheme;
        private Sprite frontEndButtonSprite;
        private Sprite frontEndPanelSprite;

        private int introIndex;
        private float introAutoTimer;
        private float introVisualTimer;
        private const float IntroAutoSeconds = 7f;
        private const float LoadingDurationSeconds = 2.6f;

        private bool hasExistingProgress;
        private bool loadingActive;
        private float loadingTimer;
        private bool frontEndAudioMuted;
        private float frontEndAudioVolume = 1f;

        private readonly IntroSlide[] slides =
        {
            new(
                "MOTOR CITY",
                "Ты приехал в город почти без денег и без громкого имени. Здесь всё придётся заработать самому.",
                "You arrived in the city with almost no money and no reputation. Everything here has to be earned.",
                "MotorCity/Intro/Intro_01"),
            new(
                "ДЯДЯ ВИТЯ",
                "Первым тебя встретил дядя Витя. У него есть мастерская, связи по всему городу и старая машина, которая слишком долго стояла без дела.",
                "Uncle Vitya was the first to meet you. He has a workshop, connections around the city, and an old car that has been sitting unused for too long.",
                "MotorCity/Intro/Intro_02"),
            new(
                "МАШИНА ИЗ МАСТЕРСКОЙ",
                "Витя оставил машину тебе. Не подарок за красивые глаза — сначала покажи, что умеешь обращаться с ней и не боишься работы.",
                "Vitya left the car for you. It is not a gift for nothing — first prove that you can handle it and are not afraid of work.",
                "MotorCity/Intro/Intro_03"),
            new(
                "ТУРБО",
                "Когда освоишься за рулём, Витя передаст тебя Турбо. Он живёт в навигаторе и знает, где новичку найти первое настоящее дело.",
                "Once you are comfortable behind the wheel, Vitya will hand you over to Turbo. He lives in the navigator and knows where a rookie can find a first real job.",
                "MotorCity/Intro/Intro_04"),
            new(
                "ПЕРВОЕ ДЕЛО",
                "Синяя доставка станет первым шагом. Потом будут гонки, дрифт, новые районы и люди, которые начнут запоминать твою машину.",
                "The blue delivery will be your first step. Then come races, drifting, new districts, and people who will start remembering your car.",
                "MotorCity/Intro/Intro_05"),
            new(
                "ТВОЙ ПУТЬ",
                "Дядя Витя, Турбо, Ника и инспектор Бублик ещё сыграют свою роль. Но сначала — разберись с машиной и пройди Путь новичка.",
                "Uncle Vitya, Turbo, Nika and Inspector Bublik will all play their part. But first, learn the car and complete the Rookie Path.",
                "MotorCity/Intro/Intro_06")
        };

        public void Initialize(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            PlayerReputation targetReputation,
            FirstSessionOnboardingSystem onboardingSystem,
            PrototypeHud prototypeHud,
            System.Action requestGameplayLoad = null)
        {
            car = targetCar;
            wallet = targetWallet;
            reputation = targetReputation;
            onboarding = onboardingSystem;
            hud = prototypeHud;
            gameplayLoadRequested = requestGameplayLoad;
            gameplayReady =
                car != null &&
                hud != null;

            EnsureUiEventSystem();
            EnsureFrontEndCamera();
            ApplyLanguageOverride();

            frontEndAudioMuted =
                MotorCitySaveService.GetInt(
                    AudioMutedKey,
                    0) != 0;

            frontEndAudioVolume =
                Mathf.Clamp01(
                    MotorCitySaveService.GetFloat(
                        AudioVolumeKey,
                        1f));

            ApplyFrontEndAudioVolume();

            hasExistingProgress =
                MotorCitySaveService.GetInt(IntroCompleteKey, 0) != 0 ||
                MotorCitySaveService.GetInt("MotorCity.Onboarding.Complete", 0) != 0 ||
                MotorCitySaveService.GetInt("MotorCity.PlayerCredits", 0) > 0 ||
                MotorCitySaveService.GetInt("MotorCity.Player.Reputation", 0) > 0 ||
                MotorCitySaveService.GetInt("MotorCity.Story.Mission", 0) > 0 ||
                MotorCitySaveService.GetInt("MotorCity.Story.Complete", 0) != 0 ||
                (onboarding != null && onboarding.IsComplete) ||
                (wallet != null && wallet.Credits > 0) ||
                (reputation != null && reputation.Reputation > 0);

            if (hasExistingProgress &&
                MotorCitySaveService.GetInt(IntroCompleteKey, 0) == 0)
            {
                MotorCitySaveService.SetInt(IntroCompleteKey, 1);
                MotorCitySaveService.Save();
            }

            BuildUi();
            ShowMainMenu();
        }


        private static void EnsureUiEventSystem()
        {
            EventSystem existing =
                Object.FindAnyObjectByType<EventSystem>();

            if (existing != null)
            {
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                {
                    existing.gameObject.AddComponent<InputSystemUIInputModule>();
                }

                return;
            }

            GameObject eventSystemObject =
                new(
                    "Motor City UI EventSystem",
                    typeof(EventSystem),
                    typeof(InputSystemUIInputModule));

            Object.DontDestroyOnLoad(
                eventSystemObject);
        }

        private void EnsureFrontEndCamera()
        {
            if (Camera.main != null)
                return;

            GameObject cameraObject =
                new("Motor City Front End Camera");

            cameraObject.transform.SetParent(
                transform,
                false);

            frontEndCamera =
                cameraObject.AddComponent<Camera>();

            frontEndCamera.clearFlags =
                CameraClearFlags.SolidColor;

            frontEndCamera.backgroundColor =
                Color.black;

            frontEndCamera.cullingMask =
                0;

            frontEndCamera.depth =
                -100f;

            frontEndCamera.allowHDR =
                false;

            frontEndCamera.allowMSAA =
                false;
        }

        private void BuildUi()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            frontEndTheme =
                Resources.Load<MotorCityUiThemeAssets>(
                    "MotorCity/UI/MotorCityUiThemeAssets");

            if (frontEndTheme != null)
            {
                frontEndButtonSprite =
                    CreateRuntimeUiSprite(
                        frontEndTheme.modalButton,
                        false);

                frontEndPanelSprite =
                    CreateRuntimeUiSprite(
                        frontEndTheme.modalPanel,
                        true);
            }

            GameObject canvasObject =
                new("Motor City Front End", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            mainRoot = CreateScreen("Main Menu", new Color(0.01f, 0.015f, 0.025f, 0.94f));
            aboutRoot = CreateScreen("About Screen", new Color(0.01f, 0.015f, 0.025f, 0.97f));
            settingsRoot = CreateScreen("Settings Screen", new Color(0.01f, 0.015f, 0.025f, 0.97f));
            introRoot = CreateScreen("Intro Screen", Color.black);
            loadingRoot = CreateScreen("Loading Screen", new Color(0.008f, 0.014f, 0.024f, 1f));

            BuildMainMenu();
            AddSharedBackground(aboutRoot, 0.78f);
            AddSharedBackground(settingsRoot, 0.78f);
            BuildAbout();
            BuildSettings();
            BuildIntro();
            BuildLoadingScreen();

            aboutRoot.SetActive(false);
            settingsRoot.SetActive(false);
            introRoot.SetActive(false);
            loadingRoot.SetActive(false);
        }

        private GameObject CreateScreen(string name, Color color)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return root;
        }


        private void AddSharedBackground(
            GameObject root,
            float shadeAlpha)
        {
            if (root == null)
                return;

            Texture2D background =
                Resources.Load<Texture2D>(
                    "MotorCity/Intro/MainMenu");

            if (background == null)
                return;

            GameObject bgObject =
                new(
                    "Front End Background",
                    typeof(RectTransform),
                    typeof(RawImage));

            bgObject.transform.SetParent(
                root.transform,
                false);

            RectTransform bgRect =
                bgObject.GetComponent<RectTransform>();

            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            RawImage bg =
                bgObject.GetComponent<RawImage>();

            bg.texture = background;
            bg.color = Color.white;
            bg.raycastTarget = false;

            GameObject shadeObject =
                new(
                    "Front End Shade",
                    typeof(RectTransform),
                    typeof(Image));

            shadeObject.transform.SetParent(
                root.transform,
                false);

            RectTransform shadeRect =
                shadeObject.GetComponent<RectTransform>();

            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = Vector2.zero;
            shadeRect.offsetMax = Vector2.zero;

            Image shade =
                shadeObject.GetComponent<Image>();

            shade.color =
                new Color(
                    0.005f,
                    0.012f,
                    0.022f,
                    shadeAlpha);

            shade.raycastTarget = false;

            bgObject.transform.SetAsFirstSibling();
            shadeObject.transform.SetSiblingIndex(1);
        }

        private void BuildMainMenu()
        {
            Texture2D background =
                Resources.Load<Texture2D>("MotorCity/Intro/MainMenu");

            if (background != null)
            {
                GameObject bgObject = new("Main Background", typeof(RectTransform), typeof(RawImage));
                bgObject.transform.SetParent(mainRoot.transform, false);
                RectTransform bgRect = bgObject.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                RawImage bg = bgObject.GetComponent<RawImage>();
                bg.texture = background;
                bg.color = Color.white;
                bg.raycastTarget = false;
                bgObject.transform.SetAsFirstSibling();
            }

            Text title = CreateText(
                mainRoot.transform,
                "MOTOR CITY",
                80,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Vector2(130f, 170f),
                new Vector2(800f, 120f),
                new Vector2(0f, 0.5f));

            title.color = new Color(0.92f, 0.96f, 1f, 1f);

            Text subtitle = CreateText(
                mainRoot.transform,
                IsRussian() ? "ТВОЙ ГОРОД. ТВОЯ МАШИНА. ТВОЙ ПУТЬ." : "YOUR CITY. YOUR CAR. YOUR ROAD.",
                24,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Vector2(136f, 96f),
                new Vector2(720f, 60f),
                new Vector2(0f, 0.5f));

            subtitle.color = new Color(0.48f, 0.72f, 1f, 1f);

            Button primary = CreateButton(mainRoot.transform, "", new Vector2(140f, -20f), new Vector2(400f, 70f), BeginPrimaryAction);
            primaryButtonText = primary.GetComponentInChildren<Text>();

            CreateButton(mainRoot.transform, IsRussian() ? "ОБ ИГРЕ" : "ABOUT", new Vector2(140f, -105f), new Vector2(400f, 62f), ShowAbout);
            CreateButton(mainRoot.transform, IsRussian() ? "НАСТРОЙКИ" : "SETTINGS", new Vector2(140f, -180f), new Vector2(400f, 62f), ShowSettings);

            RefreshMainMenuText();
        }

        private void BuildAbout()
        {
            RectTransform panel =
                CreateFrontEndPanel(
                    aboutRoot.transform,
                    "About Panel",
                    new Vector2(0f, 0f),
                    new Vector2(1320f, 780f));

            Text heading =
                CreateText(
                    panel,
                    IsRussian() ? "ОБ ИГРЕ" : "ABOUT MOTOR CITY",
                    44,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(56f, -52f),
                    new Vector2(760f, 72f),
                    new Vector2(0f, 1f));

            heading.color =
                new Color(0.90f, 0.95f, 1f, 1f);

            Text intro =
                CreateText(
                    panel,
                    IsRussian()
                        ? "MOTOR CITY — город, где имя зарабатывают за рулём."
                        : "MOTOR CITY is a city where your name is earned behind the wheel.",
                    22,
                    FontStyle.Normal,
                    TextAnchor.MiddleLeft,
                    new Vector2(58f, -118f),
                    new Vector2(1160f, 48f),
                    new Vector2(0f, 1f));

            intro.color =
                new Color(0.72f, 0.82f, 0.94f, 1f);

            float top = -205f;
            float columnWidth = 560f;
            float rowHeight = 122f;

            CreateAboutSection(
                panel,
                IsRussian() ? "ГОРОД" : "CITY",
                IsRussian()
                    ? "Исследуй районы, выполняй доставки и городские работы, находи активности и новые маршруты."
                    : "Explore districts, make deliveries, take city jobs, find activities and new routes.",
                new Vector2(58f, top),
                new Vector2(columnWidth, rowHeight));

            CreateAboutSection(
                panel,
                IsRussian() ? "МАШИНЫ" : "CARS",
                IsRussian()
                    ? "Открывай транспорт за репутацию, меняй внешний вид и улучшай характеристики в гараже."
                    : "Unlock vehicles through reputation, customize their look, and improve performance in the garage.",
                new Vector2(700f, top),
                new Vector2(columnWidth, rowHeight));

            CreateAboutSection(
                panel,
                IsRussian() ? "ЗАЕЗДЫ" : "RACING",
                IsRussian()
                    ? "Участвуй в спринтах, кольцевых гонках и дрифт-заездах."
                    : "Take part in sprints, circuit races and drift events.",
                new Vector2(58f, top - 150f),
                new Vector2(columnWidth, rowHeight));

            CreateAboutSection(
                panel,
                IsRussian() ? "ПУТЬ НОВИЧКА" : "ROOKIE PATH",
                IsRussian()
                    ? "Дядя Витя и Турбо познакомят тебя с машиной и первым делом. Дальше история продолжится через жителей города."
                    : "Uncle Vitya and Turbo introduce you to the car and your first job. The story continues through people across the city.",
                new Vector2(700f, top - 150f),
                new Vector2(columnWidth, rowHeight));

            CreateAboutSection(
                panel,
                IsRussian() ? "УПРАВЛЕНИЕ" : "CONTROLS",
                IsRussian()
                    ? "Основные действия доступны кнопками HUD. На телефоне используются экранные элементы управления."
                    : "Core actions are available through HUD buttons. Mobile uses on-screen driving controls.",
                new Vector2(58f, top - 300f),
                new Vector2(1202f, 100f));

            CreateButton(
                panel,
                IsRussian() ? "НАЗАД" : "BACK",
                new Vector2(56f, 46f),
                new Vector2(260f, 58f),
                ShowMainMenu,
                new Vector2(0f, 0f));
        }

        private void BuildSettings()
        {
            RectTransform panel =
                CreateFrontEndPanel(
                    settingsRoot.transform,
                    "Settings Panel",
                    Vector2.zero,
                    new Vector2(1180f, 670f));

            Text heading =
                CreateText(
                    panel,
                    IsRussian() ? "НАСТРОЙКИ" : "SETTINGS",
                    44,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(54f, -50f),
                    new Vector2(720f, 72f),
                    new Vector2(0f, 1f));

            heading.color =
                new Color(0.90f, 0.95f, 1f, 1f);

            CreateSettingsRow(
                panel,
                IsRussian() ? "ГРАФИКА" : "GRAPHICS",
                -165f,
                out settingsQualityText,
                () => ChangeQuality(-1),
                () => ChangeQuality(1),
                null);

            CreateSettingsRow(
                panel,
                IsRussian() ? "ЗВУК" : "AUDIO",
                -275f,
                out settingsAudioText,
                () => AdjustAudio(-1),
                () => AdjustAudio(1),
                ToggleAudio);

            CreateSettingsRow(
                panel,
                IsRussian() ? "ЯЗЫК" : "LANGUAGE",
                -385f,
                out settingsLanguageText,
                null,
                null,
                ToggleLanguage);

            CreateButton(
                panel,
                IsRussian() ? "НАЗАД" : "BACK",
                new Vector2(54f, 44f),
                new Vector2(260f, 58f),
                ShowMainMenu,
                new Vector2(0f, 0f));

            RefreshSettingsText();
        }

        private void BuildIntro()
        {
            GameObject previousImageObject = new("Intro Previous Image", typeof(RectTransform), typeof(RawImage));
            previousImageObject.transform.SetParent(introRoot.transform, false);
            introPreviousImageRect = previousImageObject.GetComponent<RectTransform>();
            introPreviousImageRect.anchorMin = Vector2.zero;
            introPreviousImageRect.anchorMax = Vector2.one;
            introPreviousImageRect.offsetMin = Vector2.zero;
            introPreviousImageRect.offsetMax = Vector2.zero;
            introPreviousImage = previousImageObject.GetComponent<RawImage>();
            introPreviousImage.color = Color.black;
            introPreviousImage.raycastTarget = false;

            GameObject imageObject = new("Intro Image", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(introRoot.transform, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            introImage = imageObject.GetComponent<RawImage>();
            introImage.color = new Color(0.18f, 0.22f, 0.28f, 1f);
            introImage.raycastTarget = false;

            introImageRect = rect;

            GameObject shadeObject = new("Intro Shade", typeof(RectTransform), typeof(Image));
            shadeObject.transform.SetParent(introRoot.transform, false);
            RectTransform shadeRect = shadeObject.GetComponent<RectTransform>();
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = Vector2.zero;
            shadeRect.offsetMax = Vector2.zero;
            shadeObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

            GameObject textGroupObject = new("Intro Text Group", typeof(RectTransform), typeof(CanvasGroup));
            textGroupObject.transform.SetParent(introRoot.transform, false);
            RectTransform textGroupRect = textGroupObject.GetComponent<RectTransform>();
            textGroupRect.anchorMin = Vector2.zero;
            textGroupRect.anchorMax = Vector2.one;
            textGroupRect.offsetMin = Vector2.zero;
            textGroupRect.offsetMax = Vector2.zero;
            introTextGroup = textGroupObject.GetComponent<CanvasGroup>();

            introCounterText = CreateText(textGroupObject.transform, "", 18, FontStyle.Bold, TextAnchor.UpperRight,
                new Vector2(-90f, -70f), new Vector2(300f, 50f), new Vector2(1f, 1f));
            introTitleText = CreateText(textGroupObject.transform, "", 46, FontStyle.Bold, TextAnchor.LowerLeft,
                new Vector2(120f, 270f), new Vector2(1100f, 80f), new Vector2(0f, 0f));
            introBodyText = CreateText(textGroupObject.transform, "", 25, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(120f, 245f), new Vector2(1100f, 160f), new Vector2(0f, 0f));

            CreateButton(introRoot.transform, IsRussian() ? "ПРОПУСТИТЬ" : "SKIP", new Vector2(-90f, 70f), new Vector2(240f, 58f), CompleteIntro, new Vector2(1f, 0f));
            CreateButton(introRoot.transform, IsRussian() ? "ДАЛЬШЕ" : "NEXT", new Vector2(-350f, 70f), new Vector2(240f, 58f), NextIntro, new Vector2(1f, 0f));
        }


        private void BuildLoadingScreen()
        {
            Text title = CreateText(
                loadingRoot.transform,
                "MOTOR CITY",
                54,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 118f),
                new Vector2(760f, 90f),
                new Vector2(0.5f, 0.5f));

            title.color =
                new Color(0.90f, 0.95f, 1f, 1f);

            loadingStatusText = CreateText(
                loadingRoot.transform,
                IsRussian() ? "ГОТОВИМ ГОРОД..." : "PREPARING THE CITY...",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 48f),
                new Vector2(760f, 52f),
                new Vector2(0.5f, 0.5f));

            loadingStatusText.color =
                new Color(0.48f, 0.72f, 1f, 1f);

            GameObject wheelObject =
                new(
                    "Loading Wheel",
                    typeof(RectTransform),
                    typeof(RawImage));

            wheelObject.transform.SetParent(
                loadingRoot.transform,
                false);

            loadingWheel =
                wheelObject.GetComponent<RectTransform>();

            loadingWheel.anchorMin =
                new Vector2(0.5f, 0.5f);
            loadingWheel.anchorMax =
                new Vector2(0.5f, 0.5f);
            loadingWheel.pivot =
                new Vector2(0.5f, 0.5f);
            loadingWheel.anchoredPosition =
                new Vector2(0f, -42f);
            loadingWheel.sizeDelta =
                new Vector2(112f, 112f);

            RawImage wheelImage =
                wheelObject.GetComponent<RawImage>();

            wheelImage.texture =
                Resources.Load<Texture2D>(
                    "MotorCity/UI/Loading/circle2");

            wheelImage.color =
                Color.white;

            wheelImage.raycastTarget =
                false;

            GameObject roadObject =
                new("Loading Road", typeof(RectTransform));

            roadObject.transform.SetParent(
                loadingRoot.transform,
                false);

            loadingRoad =
                roadObject.GetComponent<RectTransform>();

            loadingRoad.anchorMin =
                new Vector2(0.5f, 0.5f);
            loadingRoad.anchorMax =
                new Vector2(0.5f, 0.5f);
            loadingRoad.pivot =
                new Vector2(0.5f, 0.5f);
            loadingRoad.anchoredPosition =
                new Vector2(0f, -132f);
            loadingRoad.sizeDelta =
                new Vector2(620f, 24f);

            for (int i = 0; i < 8; i++)
            {
                GameObject dash =
                    new("Road Dash", typeof(RectTransform), typeof(Image));

                dash.transform.SetParent(
                    loadingRoad,
                    false);

                RectTransform dashRect =
                    dash.GetComponent<RectTransform>();

                dashRect.anchorMin =
                    new Vector2(0.5f, 0.5f);
                dashRect.anchorMax =
                    new Vector2(0.5f, 0.5f);
                dashRect.pivot =
                    new Vector2(0.5f, 0.5f);
                dashRect.anchoredPosition =
                    new Vector2(
                        -280f + i * 80f,
                        0f);
                dashRect.sizeDelta =
                    new Vector2(44f, 5f);

                loadingRoadDashes[i] =
                    dashRect;

                dash.GetComponent<Image>().color =
                    new Color(0.68f, 0.82f, 1f, 0.78f);
            }

            GameObject progressBack =
                new("Loading Progress Back", typeof(RectTransform), typeof(Image));

            progressBack.transform.SetParent(
                loadingRoot.transform,
                false);

            RectTransform progressBackRect =
                progressBack.GetComponent<RectTransform>();

            progressBackRect.anchorMin =
                new Vector2(0.5f, 0.5f);
            progressBackRect.anchorMax =
                new Vector2(0.5f, 0.5f);
            progressBackRect.pivot =
                new Vector2(0.5f, 0.5f);
            progressBackRect.anchoredPosition =
                new Vector2(0f, -188f);
            progressBackRect.sizeDelta =
                new Vector2(620f, 10f);

            progressBack.GetComponent<Image>().color =
                new Color(0.08f, 0.14f, 0.22f, 1f);

            GameObject progressFill =
                new("Loading Progress Fill", typeof(RectTransform), typeof(Image));

            progressFill.transform.SetParent(
                progressBack.transform,
                false);

            RectTransform progressFillRect =
                progressFill.GetComponent<RectTransform>();

            progressFillRect.anchorMin =
                new Vector2(0f, 0f);
            progressFillRect.anchorMax =
                new Vector2(1f, 1f);
            progressFillRect.offsetMin =
                Vector2.zero;
            progressFillRect.offsetMax =
                Vector2.zero;

            loadingProgressFill =
                progressFill.GetComponent<Image>();

            loadingProgressFill.color =
                new Color(0.24f, 0.62f, 1f, 1f);
            loadingProgressFill.type =
                Image.Type.Filled;
            loadingProgressFill.fillMethod =
                Image.FillMethod.Horizontal;
            loadingProgressFill.fillOrigin =
                0;
            loadingProgressFill.fillAmount =
                0f;
        }

        public void ResetForTesting()
        {
            MotorCitySaveService.DeleteKey(
                IntroCompleteKey);

            MotorCitySaveService.Save();

            hasExistingProgress = false;
            introIndex = 0;
            loadRequestSent = false;
            gameplayReady =
                car != null &&
                hud != null;

            RefreshMainMenuText();

            if (mainRoot != null &&
                canvas != null &&
                canvas.gameObject.activeSelf)
            {
                ShowMainMenu();
            }
        }

        private void BeginPrimaryAction()
        {
            if (hasExistingProgress)
            {
                StartLoadingTransition();
                return;
            }

            introIndex = 0;
            AudioListener.pause = true;
            mainRoot.SetActive(false);
            introRoot.SetActive(true);

            if (introPreviousImage != null)
            {
                introPreviousImage.texture = null;
                introPreviousImage.color = Color.black;
            }

            ResetIntroVisualState();
            RefreshIntro();
        }

        private void NextIntro()
        {
            if (introIndex >= slides.Length - 1)
            {
                CompleteIntro();
                return;
            }

            if (introPreviousImage != null &&
                introImage != null)
            {
                introPreviousImage.texture =
                    introImage.texture;

                introPreviousImage.uvRect =
                    introImage.uvRect;

                Color currentColor =
                    introImage.color;

                introPreviousImage.color =
                    new Color(
                        currentColor.r,
                        currentColor.g,
                        currentColor.b,
                        1f);
            }

            introIndex++;
            ResetIntroVisualState();
            RefreshIntro();
        }

        private void RefreshIntro()
        {
            IntroSlide slide = slides[Mathf.Clamp(introIndex, 0, slides.Length - 1)];

            introCounterText.text = $"{introIndex + 1} / {slides.Length}";
            introTitleText.text = slide.Title;
            introBodyText.text = IsRussian() ? slide.Russian : slide.English;

            Texture2D texture = Resources.Load<Texture2D>(slide.ResourcePath);
            introImage.texture = texture;
            introImage.color =
                texture == null
                    ? new Color(0.09f + introIndex * 0.012f, 0.12f, 0.18f, 0f)
                    : new Color(1f, 1f, 1f, 0f);
        }

        private void ResetIntroVisualState()
        {
            introAutoTimer = 0f;
            introVisualTimer = 0f;

            if (introTextGroup != null)
                introTextGroup.alpha = 0f;

            if (introImage != null)
                introImage.uvRect =
                    new Rect(0f, 0f, 1f, 1f);
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!gameplayReady &&
                Keyboard.current != null &&
                Keyboard.current.f10Key.wasPressedThisFrame)
            {
                pregameDebugVisible =
                    !pregameDebugVisible;
            }
#endif

            if (loadingActive)
            {
                UpdateLoadingAnimation();
                return;
            }

            if (introRoot == null ||
                !introRoot.activeSelf)
            {
                return;
            }

            introAutoTimer += Time.unscaledDeltaTime;
            introVisualTimer += Time.unscaledDeltaTime;

            float transition =
                Mathf.Clamp01(
                    introVisualTimer / 0.9f);

            if (introImage != null)
            {
                Color color =
                    introImage.color;

                color.a =
                    transition;

                introImage.color =
                    color;
            }

            if (introTextGroup != null)
            {
                introTextGroup.alpha =
                    Mathf.Clamp01(
                        (introVisualTimer - 0.15f) / 0.55f);
            }

            if (introImage != null)
            {
                float zoomProgress =
                    Mathf.Clamp01(
                        introVisualTimer /
                        IntroAutoSeconds);

                float easedZoom =
                    zoomProgress * zoomProgress *
                    (3f - 2f * zoomProgress);

                float crop =
                    Mathf.Lerp(
                        0f,
                        0.16f,
                        easedZoom);

                float uvSize =
                    1f - crop;

                float uvOffset =
                    crop * 0.5f;

                introImage.uvRect =
                    new Rect(
                        uvOffset,
                        uvOffset,
                        uvSize,
                        uvSize);
            }

            if (introAutoTimer >= IntroAutoSeconds)
                NextIntro();
        }

        private void CompleteIntro()
        {
            if (loadingActive)
                return;

            MotorCitySaveService.SetInt(IntroCompleteKey, 1);
            MotorCitySaveService.Save();
            hasExistingProgress = true;

            introRoot.SetActive(false);
            StartLoadingTransition();
        }

        private void StartLoadingTransition()
        {
            loadingTimer = 0f;
            loadingActive = true;
            loadRequestSent = false;
            gameplayReadyAt = 0f;
            loadingProgressWhenReady = 0f;

            AudioListener.pause = true;

            loadingRoot.SetActive(true);

            if (loadingProgressFill != null)
                loadingProgressFill.fillAmount = 0f;

            if (loadingWheel != null)
                loadingWheel.localRotation = Quaternion.identity;

            if (loadingRoad != null)
                loadingRoad.anchoredPosition =
                    new Vector2(0f, -132f);

            for (int i = 0; i < loadingRoadDashes.Length; i++)
            {
                if (loadingRoadDashes[i] == null)
                    continue;

                loadingRoadDashes[i].anchoredPosition =
                    new Vector2(
                        -280f + i * 80f,
                        0f);
            }
        }

        private void UpdateLoadingAnimation()
        {
            loadingTimer +=
                Time.unscaledDeltaTime;

            if (!loadRequestSent &&
                loadingTimer >= 0.15f)
            {
                loadRequestSent = true;
                gameplayLoadRequested?.Invoke();
            }

            float waitingProgress =
                Mathf.Min(
                    0.90f,
                    loadingTimer / 2.2f * 0.90f);

            float progress =
                waitingProgress;

            if (gameplayReady)
            {
                float finishProgress =
                    Mathf.Clamp01(
                        (loadingTimer - gameplayReadyAt) /
                        0.65f);

                progress =
                    Mathf.Lerp(
                        loadingProgressWhenReady,
                        1f,
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            finishProgress));
            }

            if (loadingWheel != null)
            {
                loadingWheel.Rotate(
                    0f,
                    0f,
                    -210f * Time.unscaledDeltaTime);
            }

            float roadOffset =
                Mathf.Repeat(
                    loadingTimer * 115f,
                    80f);

            for (int i = 0; i < loadingRoadDashes.Length; i++)
            {
                RectTransform dash =
                    loadingRoadDashes[i];

                if (dash == null)
                    continue;

                float x =
                    Mathf.Repeat(
                        (-280f + i * 80f) -
                        roadOffset +
                        320f,
                        640f) -
                    320f;

                dash.anchoredPosition =
                    new Vector2(
                        x,
                        0f);
            }

            if (loadingProgressFill != null)
            {
                loadingProgressFill.fillAmount =
                    progress;
            }

            if (loadingStatusText != null)
            {
                if (!gameplayReady &&
                    progress >= 0.88f)
                {
                    loadingStatusText.text =
                        IsRussian()
                            ? "ПОЧТИ ГОТОВО..."
                            : "ALMOST READY...";
                }
                else if (progress < 0.34f)
                {
                    loadingStatusText.text =
                        IsRussian()
                            ? "ГОТОВИМ ГОРОД..."
                            : "PREPARING THE CITY...";
                }
                else if (progress < 0.72f)
                {
                    loadingStatusText.text =
                        IsRussian()
                            ? "ЗАПУСКАЕМ МАРШРУТЫ..."
                            : "STARTING THE ROUTES...";
                }
                else
                {
                    loadingStatusText.text =
                        IsRussian()
                            ? "ПОЕХАЛИ."
                            : "LET'S DRIVE.";
                }
            }

            if (!gameplayReady ||
                progress < 0.999f)
            {
                return;
            }

            loadingActive = false;
            loadingRoot.SetActive(false);

            EnterGameplay();
            onboarding?.ShowPathPrompt();
        }

        public void AttachGameplay(
            ArcadeCarController targetCar,
            PlayerWallet targetWallet,
            PlayerReputation targetReputation,
            FirstSessionOnboardingSystem onboardingSystem,
            PrototypeHud prototypeHud)
        {
            car = targetCar;
            wallet = targetWallet;
            reputation = targetReputation;
            onboarding = onboardingSystem;
            hud = prototypeHud;

            if (frontEndCamera != null)
            {
                Object.Destroy(
                    frontEndCamera.gameObject);

                frontEndCamera = null;
            }

            pregameDebugVisible = false;

            if (hud != null)
            {
                frontEndAudioMuted =
                    hud.FrontEndAudioMuted;

                frontEndAudioVolume =
                    hud.FrontEndAudioVolume;
            }

            gameplayReady = true;
            gameplayReadyAt = loadingTimer;
            loadingProgressWhenReady =
                loadingProgressFill == null
                    ? 0.90f
                    : loadingProgressFill.fillAmount;
        }

        private void EnterGameplay()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (car != null)
                car.SetDrivingEnabled(true);

            canvas.gameObject.SetActive(false);
        }

        private void ShowMainMenu()
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;

            if (car != null)
                car.SetDrivingEnabled(false);

            mainRoot.SetActive(true);
            aboutRoot.SetActive(false);
            settingsRoot.SetActive(false);
            introRoot.SetActive(false);
            loadingActive = false;

            if (loadingRoot != null)
                loadingRoot.SetActive(false);

            RefreshMainMenuText();
        }

        private void ShowAbout()
        {
            AudioListener.pause = true;
            mainRoot.SetActive(false);
            settingsRoot.SetActive(false);
            introRoot.SetActive(false);
            aboutRoot.SetActive(true);
        }

        private void ShowSettings()
        {
            AudioListener.pause = true;
            mainRoot.SetActive(false);
            aboutRoot.SetActive(false);
            introRoot.SetActive(false);
            settingsRoot.SetActive(true);
            RefreshSettingsText();
        }

        private void RefreshMainMenuText()
        {
            if (primaryButtonText == null)
                return;

            primaryButtonText.text =
                hasExistingProgress
                    ? (IsRussian() ? "ПРОДОЛЖИТЬ" : "CONTINUE")
                    : (IsRussian() ? "НАЧАТЬ ИГРАТЬ" : "START GAME");
        }

        private void RefreshSettingsText()
        {
            if (settingsQualityText != null)
            {
                settingsQualityText.text =
                    MotorCityQualityRuntime.CurrentPreset switch
                    {
                        MotorCityQualityPreset.Low => IsRussian() ? "НИЗКОЕ" : "LOW",
                        MotorCityQualityPreset.High => IsRussian() ? "ВЫСОКОЕ" : "HIGH",
                        _ => IsRussian() ? "СРЕДНЕЕ" : "MEDIUM"
                    };
            }

            if (settingsAudioText != null)
            {
                bool muted =
                    hud != null
                        ? hud.FrontEndAudioMuted
                        : frontEndAudioMuted;

                float volume =
                    hud != null
                        ? hud.FrontEndAudioVolume
                        : frontEndAudioVolume;

                settingsAudioText.text =
                    muted
                        ? (IsRussian() ? "ВЫКЛ" : "OFF")
                        : $"{Mathf.RoundToInt(volume * 100f)}%";
            }

            if (settingsLanguageText != null)
                settingsLanguageText.text = IsRussian() ? "РУССКИЙ" : "ENGLISH";
        }

        private void ChangeQuality(int direction)
        {
            int next = ((int)MotorCityQualityRuntime.CurrentPreset + direction + 3) % 3;
            MotorCityQualityRuntime.Apply((MotorCityQualityPreset)next, true);
            RefreshSettingsText();
        }

        private void AdjustAudio(int direction)
        {
            if (hud != null)
            {
                hud.FrontEndAdjustAudio(
                    direction);

                frontEndAudioMuted =
                    hud.FrontEndAudioMuted;

                frontEndAudioVolume =
                    hud.FrontEndAudioVolume;
            }
            else
            {
                float stepped =
                    Mathf.Round(
                        (frontEndAudioVolume +
                         direction * 0.1f) *
                        10f) /
                    10f;

                frontEndAudioVolume =
                    Mathf.Clamp01(
                        stepped);

                MotorCitySaveService.SetFloat(
                    AudioVolumeKey,
                    frontEndAudioVolume);

                MotorCitySaveService.Save();

                ApplyFrontEndAudioVolume();
            }

            RefreshSettingsText();
        }

        private void ToggleAudio()
        {
            if (hud != null)
            {
                hud.FrontEndToggleAudio();

                frontEndAudioMuted =
                    hud.FrontEndAudioMuted;

                frontEndAudioVolume =
                    hud.FrontEndAudioVolume;
            }
            else
            {
                frontEndAudioMuted =
                    !frontEndAudioMuted;

                MotorCitySaveService.SetInt(
                    AudioMutedKey,
                    frontEndAudioMuted
                        ? 1
                        : 0);

                MotorCitySaveService.Save();

                ApplyFrontEndAudioVolume();
            }

            RefreshSettingsText();
        }

        private void ApplyFrontEndAudioVolume()
        {
            AudioListener.volume =
                frontEndAudioMuted
                    ? 0f
                    : frontEndAudioVolume;
        }

        private void ToggleLanguage()
        {
            string next = IsRussian() ? "en" : "ru";
            MotorCityLocalization.SetLanguage(next);
            MotorCitySaveService.SetString(LanguageKey, next);
            MotorCitySaveService.Save();

            Object.Destroy(canvas.gameObject);
            BuildUi();
            ShowSettings();
        }

        private void ApplyLanguageOverride()
        {
            string saved = MotorCitySaveService.GetString(LanguageKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(saved))
                MotorCityLocalization.SetLanguage(saved);
        }

        private static bool IsRussian() =>
            MotorCityLocalization.LanguageCode == "ru";


#if UNITY_EDITOR
        private void OnGUI()
        {
            if (gameplayReady ||
                !pregameDebugVisible)
            {
                return;
            }

            const float width = 360f;
            const float height = 190f;

            Rect box =
                new(
                    24f,
                    24f,
                    width,
                    height);

            GUI.Box(
                box,
                "MOTOR CITY — PRE-GAME DEBUG");

            GUI.Label(
                new Rect(
                    44f,
                    62f,
                    310f,
                    34f),
                "Игра ещё не загружена.");

            if (GUI.Button(
                    new Rect(
                        44f,
                        104f,
                        300f,
                        42f),
                    "ЧИСТЫЙ СТАРТ"))
            {
                ResetForTesting();

                MotorCitySaveService.DeleteKey(
                    "MotorCity.PlayerCredits");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Player.Reputation");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Onboarding.Step");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Onboarding.Complete");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Story.Mission");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Story.Progress");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Story.Complete");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Vehicle.Position.Has");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Vehicle.Position.X");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Vehicle.Position.Y");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Vehicle.Position.Z");
                MotorCitySaveService.DeleteKey(
                    "MotorCity.Vehicle.Position.Yaw");

                MotorCitySaveService.FlushNow();

                hasExistingProgress = false;
                RefreshMainMenuText();
            }

            GUI.Label(
                new Rect(
                    44f,
                    154f,
                    310f,
                    24f),
                "F10 — закрыть");
        }
#endif

        private Text CreateText(
            Transform parent,
            string value,
            int size,
            FontStyle style,
            TextAnchor alignment,
            Vector2 position,
            Vector2 dimensions,
            Vector2 anchor)
        {
            GameObject go = new("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            Text text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = false;
            text.alignByGeometry = true;
            return text;
        }

        private static Sprite CreateRuntimeUiSprite(
            Texture2D texture,
            bool sliced)
        {
            if (texture == null)
                return null;

            float border =
                sliced
                    ? Mathf.Clamp(
                        Mathf.Min(
                            texture.width,
                            texture.height) * 0.16f,
                        10f,
                        48f)
                    : 0f;

            return Sprite.Create(
                texture,
                new Rect(
                    0f,
                    0f,
                    texture.width,
                    texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(
                    border,
                    border,
                    border,
                    border));
        }

        private RectTransform CreateFrontEndPanel(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject go =
                new(
                    name,
                    typeof(RectTransform),
                    typeof(Image));

            go.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0.5f, 0.5f);
            rect.anchorMax =
                new Vector2(0.5f, 0.5f);
            rect.pivot =
                new Vector2(0.5f, 0.5f);
            rect.anchoredPosition =
                position;
            rect.sizeDelta =
                size;

            Image image =
                go.GetComponent<Image>();

            if (frontEndPanelSprite != null)
            {
                image.sprite =
                    frontEndPanelSprite;
                image.type =
                    Image.Type.Sliced;
                image.color =
                    Color.white;
            }
            else
            {
                image.color =
                    new Color(
                        0.035f,
                        0.03f,
                        0.07f,
                        0.96f);
            }

            Outline outline =
                go.AddComponent<Outline>();

            outline.effectColor =
                new Color(
                    0.42f,
                    0.50f,
                    0.92f,
                    0.42f);
            outline.effectDistance =
                new Vector2(2f, -2f);
            outline.useGraphicAlpha =
                true;

            return rect;
        }

        private void CreateAboutSection(
            Transform parent,
            string title,
            string body,
            Vector2 position,
            Vector2 size)
        {
            GameObject section =
                new(
                    title + " Section",
                    typeof(RectTransform),
                    typeof(Image));

            section.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                section.GetComponent<RectTransform>();

            rect.anchorMin =
                new Vector2(0f, 1f);
            rect.anchorMax =
                new Vector2(0f, 1f);
            rect.pivot =
                new Vector2(0f, 1f);
            rect.anchoredPosition =
                position;
            rect.sizeDelta =
                size;

            Image background =
                section.GetComponent<Image>();

            background.color =
                new Color(
                    0.035f,
                    0.055f,
                    0.10f,
                    0.72f);

            Text sectionTitle =
                CreateText(
                    rect,
                    title,
                    19,
                    FontStyle.Bold,
                    TextAnchor.UpperLeft,
                    new Vector2(18f, -14f),
                    new Vector2(size.x - 36f, 30f),
                    new Vector2(0f, 1f));

            sectionTitle.color =
                new Color(
                    0.40f,
                    0.82f,
                    1f,
                    1f);

            Text sectionBody =
                CreateText(
                    rect,
                    body,
                    18,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    new Vector2(18f, -48f),
                    new Vector2(size.x - 36f, size.y - 56f),
                    new Vector2(0f, 1f));

            sectionBody.color =
                new Color(
                    0.88f,
                    0.91f,
                    0.96f,
                    1f);
        }

        private void CreateSettingsRow(
            Transform parent,
            string label,
            float y,
            out Text valueText,
            UnityEngine.Events.UnityAction minusAction,
            UnityEngine.Events.UnityAction plusAction,
            UnityEngine.Events.UnityAction wideAction)
        {
            GameObject row =
                new(
                    label + " Row",
                    typeof(RectTransform),
                    typeof(Image));

            row.transform.SetParent(
                parent,
                false);

            RectTransform rowRect =
                row.GetComponent<RectTransform>();

            rowRect.anchorMin =
                new Vector2(0.5f, 1f);
            rowRect.anchorMax =
                new Vector2(0.5f, 1f);
            rowRect.pivot =
                new Vector2(0.5f, 1f);
            rowRect.anchoredPosition =
                new Vector2(0f, y);
            rowRect.sizeDelta =
                new Vector2(1050f, 84f);

            row.GetComponent<Image>().color =
                new Color(
                    0.028f,
                    0.045f,
                    0.085f,
                    0.74f);

            Text labelText =
                CreateText(
                    rowRect,
                    label,
                    19,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(24f, 0f),
                    new Vector2(240f, 60f),
                    new Vector2(0f, 0.5f));

            labelText.color =
                new Color(
                    0.64f,
                    0.78f,
                    1f,
                    1f);

            valueText =
                CreateText(
                    rowRect,
                    "",
                    25,
                    FontStyle.Bold,
                    TextAnchor.MiddleLeft,
                    new Vector2(282f, 0f),
                    new Vector2(270f, 60f),
                    new Vector2(0f, 0.5f));

            if (minusAction != null)
            {
                CreateButton(
                    rowRect,
                    "−",
                    new Vector2(610f, 0f),
                    new Vector2(72f, 50f),
                    minusAction,
                    new Vector2(0f, 0.5f));
            }

            if (plusAction != null)
            {
                CreateButton(
                    rowRect,
                    "+",
                    new Vector2(696f, 0f),
                    new Vector2(72f, 50f),
                    plusAction,
                    new Vector2(0f, 0.5f));
            }

            if (wideAction != null)
            {
                CreateButton(
                    rowRect,
                    label == (IsRussian() ? "ЯЗЫК" : "LANGUAGE")
                        ? (IsRussian() ? "СМЕНИТЬ" : "CHANGE")
                        : (IsRussian() ? "ВКЛ / ВЫКЛ" : "ON / OFF"),
                    new Vector2(790f, 0f),
                    new Vector2(210f, 50f),
                    wideAction,
                    new Vector2(0f, 0.5f));
            }
        }

        private Button CreateButton(
            Transform parent,
            string label,
            Vector2 position,
            Vector2 dimensions,
            UnityEngine.Events.UnityAction action,
            Vector2? anchorOverride = null)
        {
            Vector2 anchor =
                anchorOverride ??
                new Vector2(0f, 0.5f);

            GameObject go =
                new(
                    string.IsNullOrWhiteSpace(label)
                        ? "Primary Button"
                        : label + " Button",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            go.transform.SetParent(
                parent,
                false);

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition =
                position;
            rect.sizeDelta =
                dimensions;

            Image image =
                go.GetComponent<Image>();

            if (frontEndButtonSprite != null)
            {
                image.sprite =
                    frontEndButtonSprite;
                image.type =
                    Image.Type.Simple;
                image.preserveAspect =
                    false;
                image.color =
                    Color.white;
            }
            else
            {
                image.color =
                    new Color(
                        0.075f,
                        0.07f,
                        0.12f,
                        0.96f);
            }

            Button button =
                go.GetComponent<Button>();

            button.targetGraphic =
                image;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                Color.white;
            colors.highlightedColor =
                new Color(
                    0.96f,
                    0.96f,
                    1f,
                    1f);
            colors.pressedColor =
                new Color(
                    0.82f,
                    0.80f,
                    0.92f,
                    1f);
            colors.selectedColor =
                colors.highlightedColor;
            colors.colorMultiplier =
                1f;
            colors.fadeDuration =
                0.08f;

            button.colors =
                colors;
            button.onClick.AddListener(
                action);

            Outline outline =
                go.AddComponent<Outline>();

            outline.effectColor =
                new Color(
                    0.40f,
                    0.47f,
                    0.92f,
                    0.24f);
            outline.effectDistance =
                new Vector2(1f, -1f);
            outline.useGraphicAlpha =
                true;

            Text text =
                CreateText(
                    go.transform,
                    label,
                    dimensions.y <= 54f
                        ? 20
                        : 23,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Vector2.zero,
                    dimensions -
                    new Vector2(12f, 8f),
                    new Vector2(0.5f, 0.5f));

            text.name =
                "Label";

            text.color =
                new Color(
                    0.94f,
                    0.95f,
                    1f,
                    1f);

            Shadow shadow =
                text.gameObject.AddComponent<Shadow>();

            shadow.effectColor =
                new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance =
                new Vector2(1f, -2f);

            return button;
        }

        private readonly struct IntroSlide
        {
            public readonly string Title;
            public readonly string Russian;
            public readonly string English;
            public readonly string ResourcePath;

            public IntroSlide(string title, string russian, string english, string resourcePath)
            {
                Title = title;
                Russian = russian;
                English = english;
                ResourcePath = resourcePath;
            }
        }
    }
}
