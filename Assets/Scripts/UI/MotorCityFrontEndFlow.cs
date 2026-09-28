using MotorCity.Gameplay;
using MotorCity.Localization;
using MotorCity.Persistence;
using MotorCity.Platform;
using MotorCity.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace MotorCity.UI
{
    public sealed class MotorCityFrontEndFlow : MonoBehaviour
    {
        private const string IntroCompleteKey = "MotorCity.FrontEnd.IntroCompleted";
        private const string LanguageKey = "MotorCity.Settings.LanguageOverride";

        private ArcadeCarController car;
        private PlayerWallet wallet;
        private PlayerReputation reputation;
        private FirstSessionOnboardingSystem onboarding;
        private PrototypeHud hud;

        private Canvas canvas;
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
        private Font font;

        private int introIndex;
        private bool hasExistingProgress;

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
            PrototypeHud prototypeHud)
        {
            car = targetCar;
            wallet = targetWallet;
            reputation = targetReputation;
            onboarding = onboardingSystem;
            hud = prototypeHud;

            ApplyLanguageOverride();

            hasExistingProgress =
                MotorCitySaveService.GetInt(IntroCompleteKey, 0) != 0 ||
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

        private void BuildUi()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasObject =
                new("Motor City Front End", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            mainRoot = CreateScreen("Main Menu", new Color(0.01f, 0.015f, 0.025f, 0.94f));
            aboutRoot = CreateScreen("About Screen", new Color(0.01f, 0.015f, 0.025f, 0.97f));
            settingsRoot = CreateScreen("Settings Screen", new Color(0.01f, 0.015f, 0.025f, 0.97f));
            introRoot = CreateScreen("Intro Screen", Color.black);

            BuildMainMenu();
            BuildAbout();
            BuildSettings();
            BuildIntro();

            aboutRoot.SetActive(false);
            settingsRoot.SetActive(false);
            introRoot.SetActive(false);
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
                74,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Vector2(130f, 170f),
                new Vector2(800f, 120f),
                new Vector2(0f, 0.5f));

            title.color = new Color(0.92f, 0.96f, 1f, 1f);

            Text subtitle = CreateText(
                mainRoot.transform,
                IsRussian() ? "ТВОЙ ГОРОД. ТВОЯ МАШИНА. ТВОЙ ПУТЬ." : "YOUR CITY. YOUR CAR. YOUR ROAD.",
                22,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Vector2(136f, 100f),
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
            CreateText(aboutRoot.transform, IsRussian() ? "ОБ ИГРЕ" : "ABOUT MOTOR CITY", 42, FontStyle.Bold, TextAnchor.UpperLeft,
                new Vector2(120f, -90f), new Vector2(900f, 70f), new Vector2(0f, 1f));

            string body = IsRussian()
                ? "MOTOR CITY — городская автомобильная игра о пути от новичка до известного гонщика.\n\n" +
                  "ГОРОД\nИсследуй районы, выполняй доставки и городские работы, находи активности и новые маршруты.\n\n" +
                  "МАШИНЫ\nОткрывай транспорт за репутацию, меняй цвет и внешний вид, улучшай характеристики в гараже.\n\n" +
                  "ЗАЕЗДЫ\nУчаствуй в спринтах, кольцевых гонках и дрифт-заездах.\n\n" +
                  "ПУТЬ НОВИЧКА\nДядя Витя и Турбо познакомят тебя с машиной и первым делом. Затем история продолжится через задания Ники, инспектора Бублика и других жителей города.\n\n" +
                  "УПРАВЛЕНИЕ\nОсновные действия доступны кнопками HUD. На телефоне используются экранные элементы управления."
                : "MOTOR CITY is an urban driving game about going from rookie to a known driver.\n\n" +
                  "CITY\nExplore districts, make deliveries, take city jobs, find activities and new routes.\n\n" +
                  "CARS\nUnlock vehicles through reputation, customize their look, and improve performance in the garage.\n\n" +
                  "RACING\nTake part in sprints, circuit races and drift events.\n\n" +
                  "ROOKIE PATH\nUncle Vitya and Turbo introduce you to the car and your first job. The story then continues through missions from Nika, Inspector Bublik and other people in the city.\n\n" +
                  "CONTROLS\nCore actions are available through HUD buttons. Mobile uses on-screen driving controls.";

            Text text = CreateText(aboutRoot.transform, body, 22, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(120f, -180f), new Vector2(1160f, 700f), new Vector2(0f, 1f));
            text.verticalOverflow = VerticalWrapMode.Overflow;

            CreateButton(aboutRoot.transform, IsRussian() ? "НАЗАД" : "BACK", new Vector2(120f, 70f), new Vector2(260f, 58f), ShowMainMenu, new Vector2(0f, 0f));
        }

        private void BuildSettings()
        {
            CreateText(settingsRoot.transform, IsRussian() ? "НАСТРОЙКИ" : "SETTINGS", 42, FontStyle.Bold, TextAnchor.UpperLeft,
                new Vector2(120f, -90f), new Vector2(900f, 70f), new Vector2(0f, 1f));

            CreateText(settingsRoot.transform, IsRussian() ? "ГРАФИКА" : "GRAPHICS", 20, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(120f, -220f), new Vector2(300f, 50f), new Vector2(0f, 1f));
            settingsQualityText = CreateText(settingsRoot.transform, "", 26, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(420f, -220f), new Vector2(300f, 50f), new Vector2(0f, 1f));
            CreateButton(settingsRoot.transform, "−", new Vector2(760f, -220f), new Vector2(70f, 52f), () => ChangeQuality(-1), new Vector2(0f, 1f));
            CreateButton(settingsRoot.transform, "+", new Vector2(845f, -220f), new Vector2(70f, 52f), () => ChangeQuality(1), new Vector2(0f, 1f));

            CreateText(settingsRoot.transform, IsRussian() ? "ЗВУК" : "AUDIO", 20, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(120f, -310f), new Vector2(300f, 50f), new Vector2(0f, 1f));
            settingsAudioText = CreateText(settingsRoot.transform, "", 26, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(420f, -310f), new Vector2(300f, 50f), new Vector2(0f, 1f));
            CreateButton(settingsRoot.transform, "−", new Vector2(760f, -310f), new Vector2(70f, 52f), () => AdjustAudio(-1), new Vector2(0f, 1f));
            CreateButton(settingsRoot.transform, "+", new Vector2(845f, -310f), new Vector2(70f, 52f), () => AdjustAudio(1), new Vector2(0f, 1f));
            CreateButton(settingsRoot.transform, IsRussian() ? "ВКЛ / ВЫКЛ" : "ON / OFF", new Vector2(930f, -310f), new Vector2(190f, 52f), ToggleAudio, new Vector2(0f, 1f));

            CreateText(settingsRoot.transform, IsRussian() ? "ЯЗЫК" : "LANGUAGE", 20, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(120f, -400f), new Vector2(300f, 50f), new Vector2(0f, 1f));
            settingsLanguageText = CreateText(settingsRoot.transform, "", 26, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(420f, -400f), new Vector2(300f, 50f), new Vector2(0f, 1f));
            CreateButton(settingsRoot.transform, IsRussian() ? "СМЕНИТЬ" : "CHANGE", new Vector2(760f, -400f), new Vector2(240f, 52f), ToggleLanguage, new Vector2(0f, 1f));

            CreateButton(settingsRoot.transform, IsRussian() ? "НАЗАД" : "BACK", new Vector2(120f, 70f), new Vector2(260f, 58f), ShowMainMenu, new Vector2(0f, 0f));
            RefreshSettingsText();
        }

        private void BuildIntro()
        {
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

            GameObject shadeObject = new("Intro Shade", typeof(RectTransform), typeof(Image));
            shadeObject.transform.SetParent(introRoot.transform, false);
            RectTransform shadeRect = shadeObject.GetComponent<RectTransform>();
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = Vector2.zero;
            shadeRect.offsetMax = Vector2.zero;
            shadeObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

            introCounterText = CreateText(introRoot.transform, "", 18, FontStyle.Bold, TextAnchor.UpperRight,
                new Vector2(-90f, -70f), new Vector2(300f, 50f), new Vector2(1f, 1f));
            introTitleText = CreateText(introRoot.transform, "", 46, FontStyle.Bold, TextAnchor.LowerLeft,
                new Vector2(120f, 270f), new Vector2(1100f, 80f), new Vector2(0f, 0f));
            introBodyText = CreateText(introRoot.transform, "", 25, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(120f, 245f), new Vector2(1100f, 160f), new Vector2(0f, 0f));

            CreateButton(introRoot.transform, IsRussian() ? "ПРОПУСТИТЬ" : "SKIP", new Vector2(-90f, 70f), new Vector2(240f, 58f), CompleteIntro, new Vector2(1f, 0f));
            CreateButton(introRoot.transform, IsRussian() ? "ДАЛЬШЕ" : "NEXT", new Vector2(-350f, 70f), new Vector2(240f, 58f), NextIntro, new Vector2(1f, 0f));
        }

        public void ResetForTesting()
        {
            MotorCitySaveService.DeleteKey(
                IntroCompleteKey);

            MotorCitySaveService.Save();

            hasExistingProgress = false;
            introIndex = 0;

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
                EnterGameplay();
                return;
            }

            introIndex = 0;
            mainRoot.SetActive(false);
            introRoot.SetActive(true);
            RefreshIntro();
        }

        private void NextIntro()
        {
            if (introIndex >= slides.Length - 1)
            {
                CompleteIntro();
                return;
            }

            introIndex++;
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
                    ? new Color(0.09f + introIndex * 0.012f, 0.12f, 0.18f, 1f)
                    : Color.white;
        }

        private void CompleteIntro()
        {
            MotorCitySaveService.SetInt(IntroCompleteKey, 1);
            MotorCitySaveService.Save();
            hasExistingProgress = true;
            EnterGameplay();
        }

        private void EnterGameplay()
        {
            Time.timeScale = 1f;

            if (car != null)
                car.SetDrivingEnabled(true);

            canvas.gameObject.SetActive(false);
        }

        private void ShowMainMenu()
        {
            Time.timeScale = 0f;

            if (car != null)
                car.SetDrivingEnabled(false);

            mainRoot.SetActive(true);
            aboutRoot.SetActive(false);
            settingsRoot.SetActive(false);
            introRoot.SetActive(false);
            RefreshMainMenuText();
        }

        private void ShowAbout()
        {
            mainRoot.SetActive(false);
            settingsRoot.SetActive(false);
            introRoot.SetActive(false);
            aboutRoot.SetActive(true);
        }

        private void ShowSettings()
        {
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

            if (settingsAudioText != null && hud != null)
            {
                settingsAudioText.text =
                    hud.FrontEndAudioMuted
                        ? (IsRussian() ? "ВЫКЛ" : "OFF")
                        : $"{Mathf.RoundToInt(hud.FrontEndAudioVolume * 100f)}%";
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
            hud?.FrontEndAdjustAudio(direction);
            RefreshSettingsText();
        }

        private void ToggleAudio()
        {
            hud?.FrontEndToggleAudio();
            RefreshSettingsText();
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
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(13, size - 8);
            text.resizeTextMaxSize = size;
            return text;
        }

        private Button CreateButton(
            Transform parent,
            string label,
            Vector2 position,
            Vector2 dimensions,
            UnityEngine.Events.UnityAction action,
            Vector2? anchorOverride = null)
        {
            Vector2 anchor = anchorOverride ?? new Vector2(0f, 0.5f);

            GameObject go = new(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.055f, 0.11f, 0.19f, 0.96f);

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.11f, 0.28f, 0.5f, 1f);
            colors.pressedColor = new Color(0.07f, 0.18f, 0.34f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(action);

            Text text = CreateText(go.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter,
                Vector2.zero, dimensions, new Vector2(0.5f, 0.5f));
            text.name = "Label";
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
