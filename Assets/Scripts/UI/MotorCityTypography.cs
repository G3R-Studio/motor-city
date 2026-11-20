using UnityEngine;

namespace MotorCity.UI
{
    internal static class MotorCityVolumeSlider
    {
        private static Sprite circle;
        private static Sprite Circle()
        {
            if (circle != null) return circle;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Volume Handle Circle";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = new Vector2(x + .5f - size * .5f, y + .5f - size * .5f).magnitude;
                    pixels[y * size + x] = new Color32(255,255,255,(byte)Mathf.RoundToInt(255 * Mathf.Clamp01(size * .5f - 1f - distance)));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            circle = Sprite.Create(texture, new Rect(0,0,size,size), new Vector2(.5f,.5f));
            return circle;
        }

        private static Sprite rounded;
        public static void RoundTrack(UnityEngine.UI.Image image, bool vertical = false)
        {
            if (rounded == null)
            {
                // A bordered circle keeps the end caps round when stretched.
                var original = Circle();
                rounded = Sprite.Create(original.texture, original.rect, new Vector2(.5f,.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(31,31,31,31));
            }
            image.sprite = rounded;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 10f;
        }

        public static UnityEngine.UI.Slider Create(Transform parent, float value, System.Action<float> changed)
        {
            var root = new GameObject("Volume Slider", typeof(RectTransform), typeof(UnityEngine.UI.Slider), typeof(UnityEngine.UI.LayoutElement), typeof(UnityEngine.UI.Image));
            root.transform.SetParent(parent, false);
            var layout = root.GetComponent<UnityEngine.UI.LayoutElement>();
            layout.minWidth = 64f;
            layout.preferredWidth = 180f;
            layout.flexibleWidth = 1f;
            layout.preferredHeight = 36f;
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0);
            RectTransform Part(string name, Transform owner, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(owner, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = min; rect.anchorMax = max;
                rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.color = color; image.raycastTarget = false;
                return rect;
            }
            var track = Part("Track", root.transform, new Vector2(0,.5f), new Vector2(1,.5f), new Vector2(11,-3), new Vector2(-11,3), new Color(.20f,.26f,.38f,1));
            var fill = Part("Fill", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.12f,.82f,1,1));
            RoundTrack(track.GetComponent<UnityEngine.UI.Image>());
            RoundTrack(fill.GetComponent<UnityEngine.UI.Image>());
            var area = Part("Handle Area", root.transform, Vector2.zero, Vector2.one, new Vector2(11,0), new Vector2(-11,0), Color.clear);
            var handle = Part("Handle", area, new Vector2(0,.5f), new Vector2(0,.5f), new Vector2(-8,-10), new Vector2(8,10), new Color(.88f,.94f,1,1));
            handle.anchorMin = handle.anchorMax = new Vector2(0f,.5f);
            handle.sizeDelta = new Vector2(22f,22f);
            handle.anchoredPosition = Vector2.zero;
            var handleImage = handle.GetComponent<UnityEngine.UI.Image>();
            handleImage.sprite = Circle();
            handleImage.preserveAspect = true;
            // Slider drives both x anchors; keep y fixed at the centre.
            var slider = root.GetComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>();
            slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(Mathf.RoundToInt(value * 100));
            slider.onValueChanged.AddListener(v => changed(v * .01f));
            return slider;
        }
    }

    internal static class MotorCityControlSettings
    {
        public static void Create(Transform parent, System.Action changed, System.Action<Transform,string,string,string,System.Action,System.Action<Transform>> rowFactory)
        {
            var root = new GameObject("Control Settings", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup), typeof(UnityEngine.UI.LayoutElement));
            root.transform.SetParent(parent, false);
            var layout = root.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 14; layout.childControlHeight = true; layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = root.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            string L(string a, string b) => MotorCity.Localization.MotorCityLocalization.LanguageCode == "ru" ? a : b;
            void Button(string caption,string value,string actionCaption,System.Action action)
            {
                rowFactory(root.transform, caption, value, actionCaption, action, null);
            }
            bool expanded = false;
            void Refresh()
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--) { var child = root.transform.GetChild(i); child.gameObject.SetActive(false); Object.Destroy(child.gameObject); }
                var scheme = MotorCity.Input.MotorCityInput.CurrentControlScheme;
                bool enabled = scheme != MotorCity.Input.MotorCityControlScheme.Keyboard;
                Button(L("Мобильное управление", "Mobile controls"), enabled ? L("Вкл", "On") : L("Выкл", "Off"), L("Вкл / Выкл", "On / Off"), () => {
                    MotorCity.Input.MotorCityInput.SetControlScheme(enabled ? MotorCity.Input.MotorCityControlScheme.Keyboard :
                        (MotorCity.Input.MotorCityControlScheme)PlayerPrefs.GetInt("MotorCity.LastTouchScheme", (int)MotorCity.Input.MotorCityControlScheme.Wheel));
                    changed?.Invoke(); Refresh();
                });
                if (!enabled) return;
                Button(L("Управление", "Controls"), scheme == MotorCity.Input.MotorCityControlScheme.Wheel ? L("Руль", "Wheel") : L("Стрелки", "Arrows"), L("Сменить", "Switch"), () => {
                    var next = scheme == MotorCity.Input.MotorCityControlScheme.Wheel ? MotorCity.Input.MotorCityControlScheme.Arrows : MotorCity.Input.MotorCityControlScheme.Wheel;
                    PlayerPrefs.SetInt("MotorCity.LastTouchScheme", (int)next);
                    MotorCity.Input.MotorCityInput.SetControlScheme(next); changed?.Invoke(); Refresh();
                });
                Button("HUD", "", expanded ? L("Свернуть", "Collapse") : L("Изменить", "Customize"), () => { expanded = !expanded; Refresh(); });
                if (!expanded) return;
                var keys = scheme == MotorCity.Input.MotorCityControlScheme.Wheel ?
                    new[] {"Steering","Throttle","Brake","Handbrake","Action","Cancel"} :
                    new[] {"SteerLeft","SteerRight","Throttle","Brake","Handbrake","Action","Cancel"};
                foreach (var key in keys)
                {
                    string caption = key == "Cancel" ? L("Отмена", "Cancel") : MotorCity.Localization.MotorCityLocalization.Text("touch.layout." + (key == "SteerLeft" ? "left" : key == "SteerRight" ? "right" : key.ToLowerInvariant()));
                    rowFactory(root.transform, caption, Mathf.RoundToInt(TouchControlLayoutStore.LoadScale(scheme,key)*100)+"%", "", null, controls =>
                    {
                        MotorCityVolumeSlider.Create(controls, (TouchControlLayoutStore.LoadScale(scheme,key)-0.7f)/1.3f, v => {
                            float scale = 0.7f + v*1.3f; TouchControlLayoutStore.SaveScale(scheme,key,scale);
                            var row = controls.parent;
                            var label = row.Find("Value") ?? row.Find(row.name+" Value");
                            if(label != null) label.GetComponent<UnityEngine.UI.Text>().text = Mathf.RoundToInt(scale*100)+"%";
                            changed?.Invoke();
                        });
                    });
                }
            }
            root.AddComponent<MotorCityControlSettingsRefresh>().Bind(Refresh);
            Refresh();
        }
    }

    internal sealed class MotorCityControlSettingsRefresh : MonoBehaviour
    {
        private System.Action refresh;
        private string language;
        private MotorCity.Input.MotorCityControlScheme scheme;
        public void Bind(System.Action action) { refresh = action; Snapshot(); }
        private void Snapshot() { language = MotorCity.Localization.MotorCityLocalization.LanguageCode; scheme = MotorCity.Input.MotorCityInput.CurrentControlScheme; }
        private void Update()
        {
            if (language == MotorCity.Localization.MotorCityLocalization.LanguageCode && scheme == MotorCity.Input.MotorCityInput.CurrentControlScheme) return;
            Snapshot(); refresh?.Invoke();
        }
    }

    internal sealed class MotorCityRankingWindow : MonoBehaviour
    {
        private static MotorCityRankingWindow current;
        private static readonly string[] Ids = {"reputation","drift_score","race_time","completed_jobs","distance"};
        private static readonly string[] Ru = {"Лучшие водители","Короли дрифта","Лучшие гонщики","Профессионалы","Активные водители"};
        private static readonly string[] En = {"Best drivers","Drift kings","Best racers","Professionals","Active drivers"};
        [System.Serializable] private sealed class Entry { public int rank; public string name; public int score; }
        [System.Serializable] private sealed class Response { public bool ok; public Entry[] rows; }
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void MotorCityRankingFetch(string id);
        [System.Runtime.InteropServices.DllImport("__Internal")] private static extern string MotorCityRankingPoll();
#endif
        private RectTransform results;
        private UnityEngine.UI.ScrollRect scroll;
        private UnityEngine.UI.Button[] tabs;
        private int selected;
        private bool pending;
        private float deadline;
        private System.Action closed;
        private static bool Russian => MotorCity.Localization.MotorCityLocalization.LanguageCode == "ru";
        private static string L(string ru,string en) => Russian?ru:en;
        public static void Open() => Open(null);
        public static void Open(System.Action onClosed)
        {
            if(current != null) return;
            var root = new GameObject("Motor City Rankings",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=30000;
            var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
            current=root.AddComponent<MotorCityRankingWindow>(); current.closed=onClosed; current.Build();
        }
        private static RectTransform RectNode(Transform parent,string name,Vector2 min,Vector2 max,Vector2 low,Vector2 high)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform; rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=low;rect.offsetMax=high;return rect;
        }
        private static void Surface(RectTransform rect)
        {
            var graphic=rect.gameObject.AddComponent<GarageReferenceGraphic>();graphic.symbol=GarageReferenceGraphic.Symbol.Surface;graphic.color=MotorCityUiSkin.Panel;graphic.raycastTarget=false;
        }
        private static UnityEngine.UI.Text TextAt(Transform owner,string caption,int size,TextAnchor align)
        {
            var rect=RectNode(owner,"Label",Vector2.zero,Vector2.one,new Vector2(12,3),new Vector2(-12,-3));
            var t=rect.gameObject.AddComponent<UnityEngine.UI.Text>();t.font=MotorCityTypography.Regular;t.fontSize=size;t.color=MotorCityUiSkin.TextPrimary;t.text=caption;t.alignment=align;t.raycastTarget=false;t.supportRichText=false;
            MotorCityTextLayout.Configure(t);return t;
        }
        private static UnityEngine.UI.Button ButtonAt(RectTransform rect,string caption,System.Action click)
        {
            rect.gameObject.AddComponent<UnityEngine.UI.Image>();var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            MotorCityUiSkin.ApplyVectorButton(button,MotorCityButtonRole.Secondary);TextAt(rect,caption,22,TextAnchor.MiddleCenter);
            button.onClick.AddListener(()=>click());return button;
        }
        private void Build()
        {
            var shade=RectNode(transform,"Backdrop",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            shade.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.005f,.01f,.025f,.86f);
            var panel=RectNode(transform,"Ranking Panel",new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-630,-455),new Vector2(630,455));Surface(panel);
            TextAt(RectNode(panel,"Title",new Vector2(0,1),Vector2.one,new Vector2(30,-82),new Vector2(-30,-16)),L("Рейтинг игроков","Leaderboards"),38,TextAnchor.MiddleCenter);
            tabs=new UnityEngine.UI.Button[Ids.Length];
            for(int i=0;i<Ids.Length;i++)
            {
                int index=i;
                var tab=RectNode(panel,"Tab "+i,new Vector2(0,1),new Vector2(0,1),new Vector2(34+i*239,-154),new Vector2(260+i*239,-94));
                tabs[i]=ButtonAt(tab,(Russian?Ru:En)[i],()=>Select(index));
            }
            var header=RectNode(panel,"Columns",new Vector2(0,1),new Vector2(1,1),new Vector2(44,-216),new Vector2(-44,-168));
            TextAt(RectNode(header,"Rank",Vector2.zero,new Vector2(.15f,1),Vector2.zero,Vector2.zero),L("Место","Rank"),23,TextAnchor.MiddleCenter);
            TextAt(RectNode(header,"Player",new Vector2(.15f,0),new Vector2(.72f,1),Vector2.zero,Vector2.zero),L("Игрок","Player"),23,TextAnchor.MiddleLeft);
            TextAt(RectNode(header,"Score",new Vector2(.72f,0),Vector2.one,Vector2.zero,Vector2.zero),L("Результат","Score"),23,TextAnchor.MiddleRight);
            var viewport=RectNode(panel,"Results Viewport",Vector2.zero,Vector2.one,new Vector2(32,110),new Vector2(-32,-220));
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.clear;viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            scroll=viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
            results=RectNode(viewport,"Results",new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero);results.pivot=new Vector2(.5f,1);
            var group=results.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();group.padding=new RectOffset(12,12,12,12);group.spacing=12;group.childControlHeight=true;group.childControlWidth=true;group.childForceExpandHeight=false;
            results.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;scroll.content=results;
            ButtonAt(RectNode(panel,"Refresh",new Vector2(0,0),new Vector2(.5f,0),new Vector2(44,30),new Vector2(-12,88)),L("Обновить","Refresh"),()=>Select(selected));
            ButtonAt(RectNode(panel,"Close",new Vector2(.5f,0),new Vector2(1,0),new Vector2(12,30),new Vector2(-44,88)),L("Закрыть","Close"),Close);
            Select(0);
        }
        private RectTransform Row()
        {
            var row=RectNode(results,"Result Row",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var layout=row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();layout.minHeight=layout.preferredHeight=60;Surface(row);return row;
        }
        private void Clear()
        {
            for(int i=results.childCount-1;i>=0;i--){var child=results.GetChild(i);child.gameObject.SetActive(false);Destroy(child.gameObject);}
            scroll.verticalNormalizedPosition=1;
        }
        private void Status(string value){Clear();TextAt(Row(),value,24,TextAnchor.MiddleCenter);}
        private void Select(int index)
        {
            selected=index; for(int i=0;i<tabs.Length;i++)tabs[i].interactable=i!=index;
#if UNITY_WEBGL && !UNITY_EDITOR
            Status(L("Загрузка...","Loading..."));pending=true;deadline=Time.realtimeSinceStartup+20;MotorCityRankingFetch(Ids[index]);
#else
            Status(L("Онлайн-рейтинг доступен в WebGL через Playgama.","Online rankings are available in WebGL via Playgama."));
#endif
        }
        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if(!pending)return;
            string json=MotorCityRankingPoll();
            if(string.IsNullOrEmpty(json)){if(Time.realtimeSinceStartup>deadline){pending=false;Status(L("Не удалось загрузить. Обновите таблицу.","Unable to load. Refresh the table."));}return;}
            pending=false;
            Response response;
            try{response=JsonUtility.FromJson<Response>(json);}catch{response=null;}
            if(response==null||!response.ok){Status(L("Ошибка загрузки рейтинга","Leaderboard unavailable"));return;}
            if(response.rows==null||response.rows.Length==0){Status(L("Пока нет результатов","No scores yet"));return;}
            Clear();
            foreach(var entry in response.rows)
            {
                var row=Row();
                TextAt(RectNode(row,"Rank",Vector2.zero,new Vector2(.15f,1),Vector2.zero,Vector2.zero),entry.rank.ToString(),26,TextAnchor.MiddleCenter);
                TextAt(RectNode(row,"Player",new Vector2(.15f,0),new Vector2(.72f,1),Vector2.zero,Vector2.zero),string.IsNullOrEmpty(entry.name)?L("Игрок","Player"):entry.name,24,TextAnchor.MiddleLeft);
                string score=selected==2?(entry.score/1000f).ToString("F2")+L(" с"," s"):selected==4?(entry.score/1000f).ToString("F2")+L(" км"," km"):entry.score.ToString("N0");
                TextAt(RectNode(row,"Score",new Vector2(.72f,0),Vector2.one,Vector2.zero,Vector2.zero),score,26,TextAnchor.MiddleRight);
            }
#endif
        }
        private void Close(){var callback=closed;closed=null;current=null;Destroy(gameObject);callback?.Invoke();}
        private void OnDestroy(){if(current==this)current=null;}
    }

    public static class MotorCityTypography
    {
        private static Font regular;
        private static Font bold;
        public static Font Regular => regular != null ? regular :
            regular = Resources.Load<Font>("MotorCity/Fonts/RobotoCondensed-Regular")
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font Bold => bold != null ? bold :
            bold = Resources.Load<Font>("MotorCity/Fonts/RobotoCondensed-Bold") ?? Regular;
    }
}