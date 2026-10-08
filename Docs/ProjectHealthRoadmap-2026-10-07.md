# Motor City — Project Health / Cleanup Roadmap

Дата статического аудита: **2026-10-07**

Этот документ — не список «подозрительных файлов», а план безопасной технической чистки проекта.
Главное правило: **ничего не удалять и не переписывать только потому, что GitHub search не показывает прямую ссылку**.
Unity-зависимость может жить в GUID, prefab/scene YAML, Resources.Load, строковом имени объекта/материала/mesh,
UnityEvent, Editor importer, PlayerPrefs/save key или compile define.

> Статический аудит сделан по текущему main. Unity Editor / Play Mode / WebGL build / Profiler в рамках этого аудита не запускались.
> Поэтому пункты, требующие Unity, явно помечены как verification gate, а не как уже подтверждённый runtime-факт.

---

# 0. Трекер выполнения (обновляется после каждого этапа)

> **Дата начала трекинга:** 2026-10-08. **Текущая фаза:** Phase 0 — Freeze / baseline.
> **Правило отметок:** `[x]` ставится только после выполнения работы **и** проверки применимых verification gates, включая подтверждение в Unity от пользователя. Работа в GitHub без проверки в Unity не считается завершённой фазой. `[ ]` — не завершено, даже если код уже подготовлен.
> **Рабочий процесс:** одна фаза (или отдельно оговорённый подпункт) за раз → проверка ссылок/GUID и зависимостей по Gate A–F → изменение отдельным коммитом в `main` → статические проверки → инструкция для Unity → подтверждение пользователя → обновление галочек и журнала. Никаких удалений без Gate G.

## Обзор фаз

- [ ] **Phase 0 — Freeze / baseline** — В РАБОТЕ: зафиксировать состояние до чистки
- [ ] **Phase 1 — Release safety** — P0.1, убрать QA/admin из production
- [ ] **Phase 2 — Automated gates** — P0.2, CI и аудиты
- [ ] **Phase 3 — Vehicle contract** — P0.3, единый контракт моделей
- [ ] **Phase 4 — Vehicle material/lamp roles** — P0.4, материал/роль/владелец
- [ ] **Phase 5 — Low-risk dead cleanup** — только доказанные неиспользуемые файлы
- [ ] **Phase 6 — Bootstrap/lifecycle** — жизненный цикл и события
- [ ] **Phase 7 — UI ownership** — единый владелец интерфейса
- [ ] **Phase 8 — Save/progression** — сохранения и миграции
- [ ] **Phase 9 — City/runtime** — город, материалы, производительность
- [ ] **Phase 10 — Vehicle physics** — физика и мобильное управление
- [ ] **Phase 11 — Final asset/package cleanup** — ассеты, пакеты, дубликаты
- [ ] **Phase 12 — Release matrix** — итоговая Editor/WebGL Development/WebGL Release матрица

## Журнал выполнения

| Дата | Фаза | Действие | Статус / доказательство |
| --- | --- | --- | --- |
| 2026-10-08 | Phase 0 | Введён трекер этапов по существующей roadmap, код игры не менялся | Подготовка выполнена; baseline, Unity smoke и WebGL ещё не проверены |

---

## 1. Текущий масштаб проекта

На момент аудита:

- файлов в repo tree: ~4454;
- C# файлов проекта: ~182;
- runtime C# под Assets/Scripts: ~132;
- Editor C#: ~32;
- файлов в Resources: ~451;
- prefab: ~647;
- сцен: 2;
- production build scene: Assets/Scenes/Prototype.unity;
- source/workbench scene: Assets/LocalGenerated/FCG_Workbench.unity;
- .github/workflows: **нет**;
- Motor City asmdef: **нет**; отдельные asmdef есть только у SpringBone package.

Самые крупные и рискованные центральные runtime-файлы:

| Файл | Примерный размер/роль | Риск |
| --- | --- | --- |
| Assets/Scripts/Vehicle/ArcadeRacingCarRuntimeInstaller.cs | ~3772 строк, установка visual/material/wheels/collision | очень высокий |
| Assets/Scripts/UI/MotorCityFrontEndFlow.cs | ~3183 строк | очень высокий |
| Assets/Scripts/UI/NavigatorView.cs | ~3155 строк | высокий |
| Assets/Scripts/Gameplay/AdminDebugPanel.cs | ~2833 строк | высокий |
| Assets/Scripts/Vehicle/ArcadeCarController.cs | ~2626 строк | очень высокий |
| Assets/Scripts/World/PlayerVehicleRearEmission.cs | ~2194 строк | высокий |
| Assets/Scripts/UI/TouchControlsView.cs | ~2095 строк | высокий |
| Assets/Scripts/Bootstrap/MotorCityBootstrap.cs | ~1772 строк | очень высокий |
| Assets/Scripts/World/DayNightCycleController.cs | ~1783 строк | высокий |
| Assets/Scripts/Gameplay/TurboPetSystem.cs | ~1724 строк | средний/высокий |
| Assets/Scripts/UI/PrototypeHud.cs | ~1502 строк + partial UI | высокий |
| Assets/Scripts/Gameplay/GarageUpgradeSystem.cs | ~1402 строк | средний/высокий |

Размер крупных asset-зон в git tree:

- Assets/Resources: ~121 MiB;
- Assets/Resources/MotorCity/Environment: ~99.6 MiB;
- Assets/Fantastic City Generator: ~95.6 MiB;
- Assets/LocalGenerated: ~94.6 MiB;
- Assets/SapphiArt: ~82.8 MiB;
- Assets/Fantasy Skybox FREE: ~60.4 MiB.

Это не означает, что перечисленные source-паки надо удалять. Это означает, что repo хранит одновременно source,
workbench и runtime-представление города, и это нужно контролировать осознанно.

---

# 2. Обязательный Dependency Gate перед ЛЮБЫМ удалением

Каждый cleanup-ticket считается готовым к удалению только после всех применимых проверок ниже.

## Gate A — C# symbol dependency

1. Найти имя типа, метода, поля, enum.
2. Найти прямые вызовы.
3. Найти AddComponent/GetComponent/FindAnyObjectByType.
4. Найти typeof/nameof/reflection.
5. Проверить partial-классы.
6. Проверить #if ветки отдельно для Editor, WebGL release и development build.

## Gate B — Unity serialized dependency

Для asset/prefab/material/texture/shader:

1. взять GUID из .meta;
2. найти GUID во всех tracked YAML/text assets;
3. проверить scene/prefab/material/controller/asset;
4. запустить Unity dependency report;
5. проверить Missing Script / missing material / missing shader.

Нулевой текстовый GUID-reference — сильный сигнал, но для Resources всё ещё недостаточен.

## Gate C — dynamic/resource dependency

Обязательно искать:

- Resources.Load;
- Resources.LoadAll;
- путь, собранный конкатенацией;
- material.name;
- renderer/transform/gameObject.name;
- sharedMesh.name;
- shader name;
- Animator parameter;
- localization key;
- save key;
- PlayerPrefs key.

## Gate D — authored/import dependency

Для машины/OBJ/MTL:

1. кто импортирует source;
2. какие OBJ object/group names являются contract;
3. какие MTL material names используются runtime;
4. какой importer генерирует prefab;
5. какие runtime systems затем модифицируют material slots;
6. кто использует конкретные wheel holder coordinates/offsets.

## Gate E — persistence dependency

Перед удалением gameplay feature:

- найти все save keys;
- cloud-save mapping;
- migration/legacy read;
- QA reset preservation;
- analytics/event IDs;
- localization keys.

Удаление кода без миграции старых save-ключей может не падать, но оставлять мусор или ломать cloud conflict resolution.

## Gate F — lifecycle dependency

Для MonoBehaviour/service:

- кто создаёт;
- кто Initialize;
- кто подписывает события;
- где unsubscribe;
- DontDestroyOnLoad;
- RuntimeInitializeOnLoadMethod;
- static cache reset;
- что происходит при повторном входе в scene / domain reload disabled.

## Gate G — verification after change

Минимум:

1. C# compile;
2. открыть Prototype;
3. Play Mode smoke;
4. открыть главное меню → управление → город → гараж → пауза → главное меню → Continue;
5. переключить все машины;
6. сохранить/перезапустить;
7. WebGL development build;
8. WebGL release build;
9. повторить audit на dangling GUID;
10. сравнить Console до/после.

Удаление всегда отдельным commit от архитектурной переделки. Тогда rollback однозначный.

---

# 3. P0 — исправить до большой чистки

## P0.1 — QA/Admin код попадает в production WebGL

### Найдено

AdminDebugPanel.cs целиком находится под:

    #if UNITY_EDITOR || UNITY_WEBGL

MotorCityBootstrap.cs создаёт AdminDebugPanel под тем же условием.

MotorCityFrontEndFlow содержит pre-game debug окно с кнопкой:

    QA RESET SAVE

тоже под:

    #if UNITY_EDITOR || UNITY_WEBGL

MotorCityInput.AdminTogglePressed также активен для UNITY_WEBGL.

Release build profiles Web - Mobile - Release и Web - Desktop - Release не имеют custom scripting defines.
Следовательно обычный production WebGL удовлетворяет UNITY_WEBGL и получает QA/admin код.

Дополнительно ряд gameplay-систем содержит public методы *ForTesting, а некоторые testing/mock API также собраны под
UNITY_EDITOR || UNITY_WEBGL.

### Почему это опасно

- release содержит destructive save reset;
- release содержит cheat/test actions;
- лишний OnGUI/runtime код;
- увеличивается поверхность багов;
- QA и gameplay API становятся неразделимы.

### Исправить

Ввести один явный policy:

    UNITY_EDITOR || DEVELOPMENT_BUILD

или custom define:

    MOTORCITY_QA

Лучше: отдельный Development build profile с MOTORCITY_QA, а Release без него.

### Dependency gate

Перед сменой define найти:

- AdminDebugPanel;
- AdminTogglePressed;
- ResetProgressForTesting;
- ForceNextEditorMock;
- все *ForTesting;
- pregameDebugVisible;
- mock-ad paths.

После — проверить, что Development WebGL имеет QA, Release WebGL не содержит/не создаёт её.

---

## P0.2 — сделать compile/audit baseline обязательным

Сейчас CI нет вообще.

Уже есть хорошие локальные инструменты:

- Tools/check_cleanup.ps1;
- Tools/audit_project.py;
- Motor City -> Audit -> Write project and loaded scene report;
- MotorCityBuildDependencyReport;
- MotorCityMaterialAudit.

### Сделать

1. Зафиксировать новый baseline после текущей серии vehicle changes.
2. Добавить CI или хотя бы единый local pre-merge script:
   - Roslyn parse для Editor/native/WebGL;
   - source tests;
   - static GUID audit.
3. Если runner позволяет Unity licensing — добавить batchmode compile + EditMode tests.
4. Отдельно smoke-build WebGL Development и WebGL Release.

Без этого массовая чистка не должна начинаться.

---

## P0.3 — унифицировать vehicle visual contract

### Найден конфликт

Сейчас понятие body/body_misc/wheel role определяется несколькими реализациями:

- VehiclePaintMeshNames;
- StandardVehicleImportUtility;
- VehicleObjPaintPostprocessor;
- локальная NormalizeMeshName/IsBodyMeshName в BusVehicleImporter;
- дополнительные name/material heuristics в runtime installer/customization/lights.

Нормализация имён различается.

### Отдельный явный нарушитель — Hybrid

Большинство новых кузовов:

- body;
- body_misc.

Hybrid source:

- i8_body;
- i8_misc.

VehiclePaintMeshNames.IsBody ожидает body, а не i8_body.
Это необходимо проверить в Unity: сейчас Hybrid customization может зависеть от случайной структуры imported hierarchy,
а не от единого контракта.

Hybrid importer также держит две схемы колёс:

- стандартную front_wheels/rear_wheels;
- legacy wheels1/wheels2.

В текущем Assets/VehicleAssets/Hybrid standard wheel files отсутствуют, поэтому реально используется legacy branch.

### Цель

Создать один VehicleVisualContract/role resolver, который используют:

- import validation;
- paint postprocessor;
- customization;
- collision role selection;
- wheel role selection;
- lamps.

Никаких отдельных копий правил в Bus importer.

### Не делать пока

Не удалять Hybrid wheels1/wheels2.
Не переименовывать i8_body вслепую — сначала проверить lamp material mapping и prefab rebuild.

---

## P0.4 — один source of truth для материалов машины

Недавние проблемы Camaro/Bus/Delorean показали конфликт слоёв:

1. OBJ/MTL authoring;
2. Unity importer;
3. generated player prefab;
4. UpgradeMaterialsForCurrentPipeline;
5. customization property blocks;
6. headlights/rear emission;
7. mirror/glass heuristics.

Сейчас разные systems могут менять один material slot после друг друга.

### Особенно хрупкие места

Hybrid lights завязаны на:

- Material.004;
- Material.005.

Другие машины распознают:

- blackGlass;
- gradientEmmisive / gradientEmissive;
- BeatallEmission;
- AmgGTEmission;
- DeloreanEmission;
- headlights/rearLights и похожие строки.

### Roadmap

Сначала ничего глобально не «чинить по имени».

Для каждой машины составить table:

| role | renderer/mesh | material slot | authored material | texture | runtime owner |
| --- | --- | --- | --- | --- | --- |
| body paint | ... | ... | ... | ... | Customization |
| misc | ... | ... | ... | ... | none |
| glass | ... | ... | ... | ... | visual/import |
| mirror | ... | ... | ... | ... | visual/import |
| front lamp | ... | ... | ... | ... | Headlights |
| rear lamp | ... | ... | ... | ... | RearEmission |
| rim | ... | ... | ... | ... | Customization |
| tire | ... | ... | ... | ... | none |

После этого переходить от name guessing к explicit role metadata.

---

# 4. P1 — архитектурные конфликтные зоны

## P1.1 — MotorCityBootstrap

Bootstrap вручную создаёт почти весь runtime через AddComponent + Initialize.
Это даёт понятный single entry point, но создаёт сильную зависимость от порядка.

### Проверить

- порядок persistence/platform/world/car;
- activity manager/start flow;
- roster до customization;
- mastery/specialization/history/collection;
- onboarding;
- HUD/front-end;
- admin;
- day/night/city;
- уничтожение/повторная инициализация.

### Исправить

Не внедрять большой DI framework.
Сначала разрезать на фазовые методы:

1. Core/Persistence;
2. World;
3. Vehicle;
4. Progression;
5. Activities;
6. UI;
7. QA.

Сохранить текущий порядок буквально и покрыть smoke test.

---

## P1.2 — DontDestroyOnLoad/static lifecycle audit

DontDestroyOnLoad используется минимум в:

- MotorCityUiEventSystem;
- MotorCityVirtualInputRuntime;
- HudVisualPolish;
- MotorCitySfxRuntime;
- MotorCityMusicRuntime;
- MotorCityBootstrap;
- YandexPlatformService.

### Риск

При reload scene/domain reload disabled можно получить:

- duplicate instance;
- stale event subscription;
- static cache со ссылкой на destroyed object;
- два AudioListener/EventSystem;
- старый HUD polish, смотрящий на новый HUD.

### Проверка

Для каждого singleton оформить таблицу:

- creation hook;
- duplicate guard;
- static reset;
- OnDestroy;
- scene reload expectation.

---

## P1.3 — UI имеет несколько владельцев layout

HudVisualPolish:

- сам создаётся как persistent runtime object;
- ищет HUD через GameObject.Find;
- затем ищет дочерние rect по строковым именам;
- переписывает размеры/позиции.

При этом PrototypeHud/GarageReferenceLayout/NavigatorView/TouchControlsView сами строят layout.

### Риск

Создатель UI задаёт одно, post-polish pass — другое.
Изменение имени GameObject может тихо отключить polish.
Изменение координат в builder может быть перетёрто позже.

### Исправить

1. Инвентаризация: для каждого RectTransform определить одного owner.
2. HudVisualPolish оставить только для реально responsive/runtime-specific поведения.
3. Статические размеры перенести в builder/layout data.
4. Отказаться от GameObject.Find/FindRect по строке там, где можно передать ссылки.
5. После переноса удалить соответствующую post-layout ветку, но не весь HudVisualPolish сразу.

GarageReferenceLayout сейчас **не мусор** — это активный current garage builder.

---

## P1.4 — City runtime делает repair на запуске

CityAssetRuntimeInstaller выполняет runtime material repair/rebinding.

Особенно хрупко RepairMissingPlantMaterials:

- пытается загрузить несколько конкретных hash-suffixed Trees-01 material paths;
- сканирует renderers;
- сопоставляет object names вроде Plant-01.

Это историческая repair-логика, а не здоровый долгосрочный pipeline.

### Исправить

1. В Editor builder/fixer гарантировать правильные материалы до build.
2. Добавить validation, который fail-fast сообщает broken city prefab.
3. После нескольких чистых rebuild убрать runtime repair.
4. Не удалять runtime repair до сравнения CityVisual.prefab в Editor + WebGL.

---

## P1.5 — production Web material diagnostics

MotorCityWebMaterialDiagnostics.Run(activeCity) сейчас вызывается на реальном:

    UNITY_WEBGL && !UNITY_EDITOR

Он один раз делает GetComponentsInChildren<Renderer>(true) по всему runtime city,
строит словари, hierarchy paths и пишет подробные logs.

Для города с десятками тысяч объектов это лишний startup pass в production.

### Исправить

- оставить только в DEVELOPMENT_BUILD/MOTORCITY_QA;
- либо заменить на editor-time build validation.

Удалить файл полностью можно только после переноса полезных проверок в Editor audit.

---

## P1.6 — city scale / performance

Предыдущий Unity-аудит фиксировал крайне большой runtime city:
десятки тысяч GameObject/MeshRenderer и тысячи collider/light/MonoBehaviour.

Это нужно повторно измерить после текущих изменений.

### Profiler gates

- CPU: scripts/render/culling/physics;
- GC alloc/frame;
- batches/setpass;
- visible renderers;
- lights;
- physics broadphase;
- memory;
- WebGL loading time;
- mobile browser memory.

Не «оптимизировать» город на глаз — сначала профиль Low/Medium/High.

---

# 5. P1 — Save / persistence / progression

## P1.7 — прямой PlayerPrefs вне SaveService

Основной progression уже идёт через MotorCitySaveService, но прямой PlayerPrefs остаётся минимум в:

- MotorCityInput — control scheme;
- MotorCityQualityRuntime — quality preset;
- MotorCitySaveService — storage/legacy bridge.

Control/quality могут намеренно быть device-local settings.
Нужно это **задокументировать**, чтобы будущая чистка не попыталась перенести их в cloud save случайно.

### Проверить

Разделить ключи на:

- cloud progression;
- account entitlement;
- device-local settings;
- QA-only;
- legacy migration.

Для каждого ключа иметь owner + retention policy.

---

## P1.8 — ResetProgressForTesting и legacy migration

MotorCitySaveService содержит реальную legacy migration и cloud revision metadata.
ResetProgressForTesting сохраняет часть QA/cloud metadata.

Эту функцию нельзя просто удалить вместе с AdminDebugPanel, пока не решено,
нужен ли reset в development QA.

Правильнее:
- gate entry point из release;
- сам reset оставить доступным QA build/tests;
- отдельно проверить cloud resolver после reset.

---

# 6. P1/P2 — Vehicles / physics / customization

## P1.9 — vehicle importers привести к одному шаблону

Сейчас importers исторически разные.

У восьми importers остались мёртвые BuildSessionKey constants — объявлены, но не используются:

- AmgGTVehicleImporter;
- DeloreanVehicleImporter;
- BusVehicleImporter;
- Porsche996VehicleImporter;
- HybridVehicleImporter;
- ToyotaAE86VehicleImporter;
- Peugeot306VehicleImporter;
- CamaroVehicleImporter.

Beatall и Haon BuildSessionKey реально используют — их не трогать вместе с этими.

### Safe cleanup

После compile gate удалить только восемь мёртвых constants.
Это не меняет runtime/import behavior.

### Следом

Общий importer helper должен отвечать только за:
- dependency hash;
- validation;
- instantiate/strip physics;
- wheel visual creation;
- save prefab.

Но **wheel coordinates/rotations/local visual offsets остаются per-car data**.
Не переносить их в «универсальную магию».

---

## P1.10 — collision ownership

ArcadeRacingCarRuntimeInstaller сейчас совмещает:

- visual install/cache;
- material conversion;
- wheel detection;
- collision mesh choice/fallback;
- vehicle cache;
- mirror/material repair;
- legacy ARCADE handling.

Это слишком много ответственности.

### Split без изменения поведения

1. VehicleVisualLoader;
2. VehicleWheelBinder;
3. VehicleCollisionBuilder;
4. VehicleMaterialPipeline;
5. VehicleVisualCache.

Сначала extract pure/helper methods, потом менять behavior.

---

## P2.1 — driving/physics audit

ArcadeCarController ~2600 строк и объединяет:

- input;
- Prometeo wheel rig;
- drive modes;
- grip/friction;
- stability;
- handbrake;
- upgrades/mastery;
- teleport/presentation locks.

После недавних steering/drift симптомов нужна трассировка силы по FixedUpdate:

- кто пишет WheelCollider.steerAngle;
- кто меняет sideways/forward friction;
- кто AddForce/AddTorque;
- какой код включён по drive mode;
- какие значения refresh каждые 0.12 сек;
- handbrake ownership;
- input update vs FixedUpdate timing.

Никаких новых yaw-assist до такой трассировки.

---

# 7. P2 — Lighting / emission / day-night

## P2.2 — убрать material-name magic постепенно

PlayerHeadlights и PlayerVehicleRearEmission содержат vehicle-specific special cases.

Особенно Hybrid Material.004/Material.005 — плохой долгосрочный contract.

### План

1. Создать VehicleLampRole metadata на generated prefab/import step.
2. До миграции написать regression test на каждую машину:
   - headlights day/off;
   - headlights night/on;
   - brake lights;
   - reverse если есть;
   - no body/mirror emission.
3. Мигрировать машину за машиной.
4. Только после последней машины удалить старые material-name fallback branches.

---

## P2.3 — PlayerVehicleRearEmission слишком большой

~2194 строки для rear emission указывает, что туда накопились:
- material bindings;
- overlays;
- mesh heuristics;
- vehicle exceptions;
- night state.

Split:
- LampRoleResolver;
- LampMaterialBinding;
- LampOverlayFactory;
- RearLampController.

Но только после visual role tests.

---

# 8. P2 — Input / touch

## P2.4 — control scheme persistence

MotorCityInput хранит control scheme напрямую в PlayerPrefs и сам содержит static state.
Это допустимо как device setting, но нужно:

- один settings owner;
- test смены keyboard/arrows/wheel;
- reset virtual state при menu/garage/pause;
- device simulator path;
- WebGL mobile detection.

Touch layout customization и input mode нельзя рефакторить одновременно с physics steering.

---

## P2.5 — MotorCityVirtualInputRuntime

Этот файл не мусор.

Он:
- ставится BeforeSceneLoad;
- DontDestroyOnLoad;
- BeginVirtualInputFrame в Update;
- EndVirtualInputFrame в LateUpdate.

Удаление сломает virtual pressed/held semantics.
Можно оптимизировать FindFirstObjectByType только после lifecycle test, но приоритет низкий.

---

# 9. P2 — Activities / onboarding / navigation

Для каждой activity/system проверить одинаковый lifecycle:

- idle;
- can start;
- start flow / ad;
- countdown;
- active;
- cancel;
- success/fail;
- result UI;
- reward exactly once;
- save exactly once;
- navigation next goal;
- pause/main menu interruption;
- onboarding override.

Особое внимание:
- ActivityManager;
- ActivityStartFlow;
- StreetSprintActivity;
- CircuitRaceActivity;
- DriftChallenge;
- DeliveryActivity;
- FirstSessionOnboardingSystem;
- ResultNextGoalResolver;
- NavigatorView.

Удалять «unused state» только после exhaustive transition test.

---

# 10. P2 — Asset / Resources cleanup

## P2.6 — Resources нельзя чистить только GUID-анализом

Пример из текущего проекта:

Assets/Resources/MotorCity/UI/Loading/circle2.PNG имеет ноль serialized GUID refs,
но реально загружается:

    Resources.Load<Texture2D>("MotorCity/UI/Loading/circle2")

Одновременно точная копия:

Assets/Eric VFX Studio/Resource/Textures/circle2.PNG

используется Eric material через GUID.

То есть обе копии сейчас имеют разных consumers.
Простое «дубликат => удалить один» сломает что-то.

---

## P2.7 — точные binary duplicates

Найдены exact-content duplicates:

1. Eric VFX circle2.PNG / Resources loading circle2.PNG;
2. AmgGT/all.png / Beatall/all.png / Delorean/all.png;
3. Hybrid wheels1.mtl / wheels2.mtl;
4. Porsche996 rear_wheels.mtl / ToyotaAE86 front_wheels.mtl.

### Решение

Не удалять автоматически.

Дедуп возможен только если:
- переназначить MTL/material texture reference;
- либо изменить Resources consumer;
- reimport;
- rebuild соответствующей машины;
- проверить prefab/material GUID;
- визуально проверить.

Для authored vehicle source отдельные одинаковые MTL допустимо оставить: они локализуют source dependency машины.

---

## P2.8 — вероятно устаревшие generated vehicle materials

В Resources всё ещё есть:

- BeatallMaterials/BeatallBody.mat;
- BeatallMaterials/BeatallGlass.mat;
- BeatallMaterials/BeatallEmission.mat;
- AmgGTMaterials/AmgGTBody.mat;
- AmgGTMaterials/AmgGTGlass.mat;
- AmgGTMaterials/AmgGTEmission.mat;
- DeloreanMaterials/DeloreanBody.mat;
- DeloreanMaterials/DeloreanGlass.mat;
- DeloreanMaterials/DeloreanEmission.mat.

Статически их GUID сейчас имеют **0 inbound refs**.
По точным Resources.Load paths они также не найдены.
Текущие importers для этих машин в основном сохраняют authored OBJ/MTL slots.

### Статус

**Кандидат на полное удаление, но не удалять до Unity Dependency Report.**

Перед delete:
- Unity Select Dependencies / audit;
- rebuild Beatall/AMG/Delorean;
- inspect generated prefab slots;
- Play Mode day/night/brake/customization;
- WebGL smoke.

После delete:
- повторить GUID dangling audit.

После подтверждения можно удалить material files + .meta + пустые directories.

---

# 11. P2/P3 — Editor/source tooling cleanup

## P2.9 — FantasticCityGeneratorLegacyImporterFixer

Файл самодостаточный Editor menu tool.

Он чинит старое:

    materialLocation: 0

и сканирует:
- Assets/Fantastic City Generator;
- Assets/Simple Garage.

Assets/Simple Garage в текущем tree уже нет.
Поиск materialLocation: 0 в текущем repo находит только сам текст fixer.

### Статус

**Условно safe-delete candidate.**

Удалить полностью можно, если принято решение:
«мы больше не импортируем старую исходную версию FCG/Simple Garage, требующую этой миграции».

Если есть вероятность повторного импорта старого package — оставить как maintenance tool.
Runtime dependency отсутствует.

---

## P2.10 — audit/editor tools пока НЕ удалять

Не удалять до конца cleanup:

- MotorCityProjectAudit;
- MotorCityBuildDependencyReport;
- MotorCityMaterialAudit;
- GarageHudDiagnostics;
- Tools/audit_project.py;
- Tools/check_cleanup.ps1;
- Tools/Tests/*.

Даже если на них нет runtime refs — они нужны для доказательства безопасной чистки.

После финального cleanup можно решить, какие оставить постоянными.

---

# 12. P2/P3 — repo / generated city source

FCG source (~95 MiB), Workbench (~95 MiB), runtime environment (~100 MiB) — разные уровни pipeline.

Не удалять Workbench только потому, что он не в build: это source of truth для runtime city builder.

.gitattributes уже предупреждает, что FCG_Workbench.unity и CityVisual.prefab близки к лимиту GitHub и не переведены
в LFS без исторической миграции.

### План

1. Решить: Workbench — долгосрочный source или одноразовый baked source?
2. Если долгосрочный:
   - сделать правильную git-lfs history migration отдельной операцией;
   - не смешивать с gameplay commits.
3. Если одноразовый:
   - сначала доказать, что runtime city можно воспроизвести иначе.
4. Только потом архивировать source package.

---

# 13. P3 — Packages / third-party

Проверить, что реально используется:

- SpringBone;
- ToonShader;
- SpriteLess UI;
- SapphiArt assets;
- Fantasy Skybox;
- Eric VFX;
- FCG;
- ARCADE FREE;
- Prometeo.

Package/asset нельзя удалять по отсутствию C# symbol refs:
shader/material/prefab могут ссылаться сериализованно.

Для каждого:
1. GUID dependency count;
2. build dependency report;
3. package API search;
4. shader usage;
5. prefab/material usage;
6. license retained if asset remains.

---

# 14. Что можно удалить первым

## GREEN — после обычного compile gate

1. Неиспользуемые BuildSessionKey constants в 8 importers:
   AMG, Delorean, Bus, Porsche, Hybrid, Toyota, Peugeot, Camaro.
   Это code cleanup, не file delete.

## GREEN/YELLOW — отдельным commit после одного Unity dependency check

2. Старые generated Beatall/AmgGT/Delorean material assets с нулём GUID refs
   и без найденного Resources.Load path.
   Нужен visual smoke, потому что light code всё ещё содержит legacy material-name fallback.

## YELLOW — policy decision

3. FantasticCityGeneratorLegacyImporterFixer.cs.
   Удалять только если старые FCG packages больше не будут импортироваться.

## RED — не удалять сейчас

- Hybrid wheels1/wheels2;
- FCG_Workbench.unity;
- CityVisual.prefab;
- runtime Loading/circle2.PNG;
- Eric circle2.PNG;
- Tools/audit_project.py;
- Tools/check_cleanup.ps1;
- audit Editor tools;
- GarageReferenceLayout/GarageReferenceGraphic;
- MotorCityVirtualInputRuntime;
- any Resources asset только на основании «0 GUID refs».

---

# 15. Рекомендуемый порядок выполнения cleanup

## Phase 0 — Freeze / baseline

**Цель:** зафиксировать текущее состояние после обновлений машин, света и стёкол **до любых удалений**. Старый аудит от 2026-10-07 — исходное описание проблем, но не подтверждённый baseline текущего кода.

**Проверка Phase 0 (последовательно):**

1. В PowerShell 7 из корня проекта выполните `git status` — не должно быть незавершённого merge или конфликтов. Сохраните короткий hash команды `git rev-parse --short HEAD`. Если есть локальные незакоммиченные изменения, ничего не сбрасывайте; сначала завершите merge/commit.
2. Выполните `pwsh -NoProfile -File Tools/check_cleanup.ps1` и сохраните весь вывод. Это Roslyn/source-тесты, **не** Unity compile.
3. Выполните `py -3 Tools/audit_project.py --output Temp/MotorCityAudit/static-audit.json` и сохраните JSON. Кандидаты без GUID-reference **не разрешены к удалению**.
4. В Unity откройте `Assets/Scenes/Prototype.unity`, дождитесь компиляции, откройте `Motor City → Audit → Write project and loaded scene report`, сохраните отчёт. Зафиксируйте ошибки Console, включая существующие предупреждения, отдельно от новых.
5. Play Mode: главное меню → выбор управления → город → гараж → пауза → главное меню → Continue; переключите все машины. Сделайте для каждой скриншот в гараже, днём и ночью, отдельно отметьте paint/glass/headlights/brake/wheels и поведение руля, газа, тормоза, ручника.
6. Зафиксируйте результаты в журнале выше; проверенный статус `Phase 0` отмечается `[x]` только после выполнения всех применимых подпунктов ниже.

- [ ] новый audit baseline;
- [ ] Console clean;
- [ ] записать current prefab/material state;
- [ ] сохранить screenshots каждой машины день/ночь/гараж;
- [ ] сохранить steering/physics smoke notes.

## Phase 1 — Release safety

- [ ] MOTORCITY_QA / DEVELOPMENT_BUILD;
- [ ] убрать AdminDebugPanel и QA RESET SAVE из release;
- [ ] mock-ad/testing entrypoints из release;
- [ ] release WebGL smoke.

## Phase 2 — Automated gates

- [ ] CI;
- [ ] Roslyn/static tests;
- [ ] GUID audit;
- [ ] Unity compile/test;
- [ ] dependency/build report.

## Phase 3 — Vehicle contract

- [ ] один visual naming resolver;
- [ ] validation всех моделей;
- [ ] Hybrid body contract;
- [ ] importers;
- [ ] wheel transforms snapshot tests.

## Phase 4 — Vehicle material/lamp roles

- [ ] explicit body/glass/mirror/lamp/rim roles;
- [ ] migrate one vehicle at a time;
- [ ] remove numeric/material-name magic only after migration.

## Phase 5 — Low-risk dead cleanup

- [ ] dead constants;
- [ ] stale generated materials;
- [ ] obsolete editor migration tools;
- [ ] exact orphan assets confirmed by all gates.

## Phase 6 — Bootstrap/lifecycle

- [ ] split bootstrap by phases;
- [ ] singleton/DontDestroy audit;
- [ ] event subscribe/unsubscribe audit.

## Phase 7 — UI ownership

- [ ] remove double layout ownership;
- [ ] HudVisualPolish dependency reduction;
- [ ] split FrontEnd/Navigator/Touch UI files.

## Phase 8 — Save/progression

- [ ] key registry;
- [ ] device/cloud/QA separation;
- [ ] migration tests;
- [ ] save restart/cloud conflict tests.

## Phase 9 — City/runtime

- [ ] remove startup diagnostics from release;
- [ ] migrate runtime material repairs to Editor builder;
- [ ] profiler-driven city optimization.

## Phase 10 — Vehicle physics

- [ ] FixedUpdate force/steer/friction trace;
- [ ] drive modes;
- [ ] handbrake;
- [ ] mobile input timing;
- [ ] only then tuning.

## Phase 11 — final asset/package cleanup

- [ ] third-party packages;
- [ ] duplicate textures;
- [ ] source vs runtime city storage;
- [ ] LFS migration if chosen.

## Phase 12 — release matrix

- [ ] Матрица Editor / WebGL Dev / WebGL Release ниже полностью проверена; все отрицательные проверки (QA в Release, missing scripts/materials) прошли.

Проверить минимум:

| Area | Editor | WebGL Dev | WebGL Release |
| --- | --- | --- | --- |
| compile | yes | yes | yes |
| fresh save | yes | yes | yes |
| existing save | yes | yes | yes |
| cloud/save restart | yes | yes | yes |
| intro/tutorial | yes | yes | yes |
| keyboard | yes | yes | yes |
| touch arrows | simulator/device | yes | yes |
| touch wheel | simulator/device | yes | yes |
| every vehicle visual | yes | yes | yes |
| body paint/wheels/neon | yes | yes | yes |
| headlights/brake/night | yes | yes | yes |
| garage/menu/continue | yes | yes | yes |
| all activities | yes | yes | yes |
| pause/resume | yes | yes | yes |
| QA admin present | yes | yes | **NO** |
| QA reset present | yes | yes | **NO** |
| missing materials | 0 | 0 | 0 |
| missing scripts | 0 | 0 | 0 |

---

# 16. Definition of Done для удаления

Файл/система считается безопасно удалённой, если:

- symbol references = 0 либо мигрированы;
- serialized GUID inbound references = 0;
- dynamic Resources/name dependency проверена;
- importer/source dependency проверена;
- save/localization/analytics dependency проверена;
- compile clean;
- project audit clean;
- no new missing script/material/shader;
- relevant Play Mode smoke clean;
- relevant WebGL build clean;
- dangling GUID audit clean;
- удаление сделано отдельным revertable commit.

Именно этот критерий должен применяться к каждому cleanup PR/commit, а не субъективное «похоже, файл больше не нужен».
