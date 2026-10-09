# Motor City — Project Health / Cleanup Roadmap

Обновлено: **2026-10-09**. Исходный аудит: 2026-10-07. История промежуточных попыток и ошибок удалена из этого трекера; она остаётся в Git-коммитах и GitHub Actions.

**Правило:** `[x]` означает завершённый и подтверждённый пункт в заявленном объёме; `[ ]` — открытый gate. Статический анализ и CI не заменяют Play Mode, браузерную проверку и профилирование. Нельзя удалять Unity assets по отсутствию прямых C# или GUID-ссылок: важны Resources.Load, importer/builder, сцены, сериализация, сохранения и события. Ручной визуал уже подтверждён пользователем — повторять без конкретной регрессии не требуется.

## Статус фаз

| Фаза | Статус | Граница выполненного |
| --- | --- | --- |
| 0 — Baseline | ✅ Закрыта | исходники, GUID, Unity/Console, материалы, визуал и smoke |
| 1 — Release safety | ✅ Закрыта в рамках QA-изоляции | Desktop Release/Development подтверждены; полная матрица в Phase 12 |
| 2 — Automated gates | ✅ Закрыта | source, UI, Editor и on-demand WebGL CI |
| 3 — Vehicle contract | ✅ Закрыта | 9 prefab contracts, импортёры, колёса, визуал; дорожная физика — Phase 10 |
| 4 — Material/lamp roles | ✅ Безопасный контракт закрыт | неизвестные atlas/submesh роли остаются unclassified, с совместимым fallback |
| 5 — Dead cleanup | 🟡 Безопасная часть выполнена | удаление неподтверждённых assets запрещено; recovery tools сохраняются |
| 6 — Bootstrap/lifecycle | 🟡 Начата | статический audit подключён, refactor и runtime gates впереди |
| 7 — UI ownership | ⬜ Открыта | конфликтующие владельцы layout |
| 8 — Save/progression | ⬜ Открыта | ключи, миграции, cloud и restart |
| 9 — City/runtime | ⬜ Открыта | диагностика release, материалы, profiler |
| 10 — Vehicle physics | ⬜ Открыта | trace/steer/friction/mobile timing |
| 11 — Asset/packages | ⬜ Открыта | пакеты, дубликаты, источник города |
| 12 — Release matrix | ⬜ Открыта | два WebGL Release Build Profiles + браузер/устройство |

## Подтверждённые результаты

- [x] Phase 0: `check_cleanup.ps1`, статический GUID audit (без доказанных dangling/deleted ссылок), Unity Edit/Play Mode baseline.
- [x] Phase 0: исправлены два Missing Script в `Double-Block-09.prefab` (`4da9a30`); повторный Unity audit и визуал города/воды подтверждены.
- [x] Phase 0: Unity material audits — 767 материалов, 0 null/unsupported shaders; в проверенной сцене 0 пустых material slots.
- [x] Phase 0: Input System (New) в PlayerSettings и двух WebGL Release профилях; `check_input_backend.py` PASS, deprecated Input warning исчез.
- [x] Phase 0: ручной smoke меню → гараж → пауза → главное меню → Continue → выбор управления → город.
- [x] Phase 0/3: пользователь подтвердил работу и визуал **всех 10 машин**; фары, стоп-сигналы, окраску, диски, неон и мобильные органы управления. Архив отдельных скриншотов неполон, но не является основанием повторять уже пройденный visual smoke.
- [x] Phase 1: QA/Admin и тестовые точки входа изолированы guard `UNITY_EDITOR || DEBUG`; `check_release_qa.py` PASS. WebGL Desktop Release без QA/Admin и Development с QA/Admin проверены пользователем.
- [x] Phase 2: проверены CI source/GUID/Roslyn, UI, Unity Editor и отдельно on-demand WebGL: [run 37865444289](https://github.com/G3R-Studio/motor-city/actions/runs/37865444289).
- [x] Phase 3: девять Player prefabs загружаются; wheel snapshots и isolated production WheelCollider rig PASS; реальный rebuild импортёров в disposable CI checkout: [run 37933469484](https://github.com/G3R-Studio/motor-city/actions/runs/37933469484).
- [x] Phase 4: каталог и runtime mapping ролей, безопасная поддержка неразмеченных atlas slots; полный CI [run 37938850246](https://github.com/G3R-Studio/motor-city/actions/runs/37938850246). Это **не** утверждение, что каждый смешанный submesh художественно размечен.
- [x] Phase 5: удалены восемь мёртвых `BuildSessionKey` constants, сохранён используемый `SourceHashKey`; добавлены аудиты `Tools/check_phase5_cleanup.py` и `MotorCityPhase5DependencyAudit`.
- [x] Phase 5: исходные material assets и FCG migration tools сохранены как потенциально необходимые; статический/UI/Unity CI подтверждён [run 37977156229](https://github.com/G3R-Studio/motor-city/actions/runs/37977156229) на `24a2bac`. WebGL job в этом запуске **skipped**, не PASS.
- [x] Phase 6: baseline source-инвариантов bootstrap, постоянных hosts и кандидатов событийных подписок добавлен в `Tools/check_phase6_lifecycle.py` и CI (`e666174`). Результат этого нового CI не зафиксирован как PASS.

## Фазы 0–5 — результаты и ограничения

### Phase 0 — Baseline ✅

- [x] Source/GUID/Unity Editor audit, Console без ранее выявленных ошибок и предупреждений.
- [x] Git snapshot: [prefab/material inventory](Baselines/PrefabMaterialInventory-2026-10-08.json), [material baseline](Baselines/PrefabMaterialBaseline-2026-10-08.md).
- [x] [Driving/physics source audit](Baselines/Phase0DrivingPhysicsAudit-2026-10-09.md) — только source-level; измерение на дороге отдельно в Phase 10.
- [x] Визуальный и функциональный smoke всех автомобилей, UI, мобильного управления и материалов подтверждён.
- [ ] Формальный WebGL Desktop/Mobile Release profile matrix — перенесён в Phase 12, **не блокирует** Phase 0.

### Phase 1 — Release safety ✅

- [x] QA guards и статическая проверка `*ForTesting`, известных QA/mock/reset точек входа; исходники и Unity проверки выполнены.
- [x] Desktop WebGL Release и Development пользователь проверил, включая наличие/отсутствие QA.
- [ ] Полная отрицательная матрица всех release сборок и device tests — Phase 12. Это не повтор Phase 1.

### Phase 2 — Automated gates ✅

- [x] `.github/workflows/motor-city-source-gates.yml`: source/GUID, UI source layout, Unity 6 Editor на self-hosted runner.
- [x] Opt-in WebGL build/report подтверждён отдельным успешным запуском; автоматические push-запуски могут законно пропускать WebGL.
- [x] Отчёты Unity/static публикуются artifacts.
- [ ] Формальное тестирование обеих Release Build Profiles в браузерах — Phase 12.

### Phase 3 — Vehicle contract ✅

- [x] 9 prefab naming/visual contracts и Hybrid body OBJ.
- [x] 9 importer rebuild в отдельной CI-копии, проверка исходных prefab после восстановления.
- [x] Четыре wheel pivots per prefab + numeric snapshots + isolated WheelCollider validation.
- [x] User visual QA подтверждён; road physics tests оставлены Phase 10.

### Phase 4 — Vehicle material/lamp roles ✅ (безопасный scope)

- [x] `VehicleVisualRoles`, `VehicleMaterialRole`, `VehicleMaterialRoleCatalog`; exact material role mapping и runtime annotation без изменения исходных материалов.
- [x] Body/lamp integrations; Unity clone audit и WebGL/Editor CI.
- [x] Неидентифицируемые shared `chrome`, `plastic`, `Material.00x`, `Color`, `baseGradient` и atlas slots сознательно не получают ложных ролей; fallback сохранён.
- [ ] Пересоздание художественных submesh assignments и полное устранение legacy fallback **не выполнены**; разрешать только при отдельном доказанном asset mapping, не считать автоматическим требованием повторной визуальной проверки.
- Документ: [Phase 4 mapping](Baselines/Phase4VehicleMaterialRoles-2026-10-09.md).

### Phase 5 — Low-risk dead cleanup 🟡

- [x] Удалены восемь неиспользуемых constants, без изменений gameplay.
- [x] Проверка GUID/FBX importer metadata, защита Resources, material dependency report из Unity; раздельная классификация scene vs Resources (`b2f64ac`).
- [x] FCG Legacy FBX Importer Fixer и URP Fixer аудированы: остаются полезными ручными recovery средствами. CI охраняет их наличие и menu commands (`738d3e3`).
- [x] Решение по непроверенным кандидатам: **ничего не удалять**. Это выполненное решение безопасности, а не незавершённая попытка очистить весь Resources.
- [ ] Точные orphan materials/assets, подтверждённые Gate A–G, пока **не выявлены**. Не отмечать удаление выполненным и не создавать искусственную задачу «обязательно удалить что-нибудь».
- [ ] Просмотр содержимого Unity dependency artifact после последней ревизии и конкретные кандидаты (если появятся); WebGL сборка для последних tooling changes не подтверждена.

## Phase 6 — Bootstrap/lifecycle 🟡

- [x] Добавлен CI source baseline: SubsystemRegistration, AfterSceneLoad, sceneLoaded subscriptions, once-only flags и перечень постоянных hosts.
- [x] Сформирован автоматический inventory вероятных мест событийных подписок — **не** вывод об утечках.
- [ ] Полностью разделить `MotorCityBootstrap` на логические фазы без изменения порядка platform → remote config → pending purchases → cloud → frontend → gameplay. Первый этап [x]: `InitializeCoreAndWorld` и `InitializePlayerVehicle` выделены из `TryBuildPrototype` (`1aa1d21`), их порядок охраняется source CI (`1b98d75`); ожидается Unity CI.
- [ ] Проверить singleton/`DontDestroyOnLoad` инстансы в runtime, повторную загрузку `Prototype`, domain reload.
- [ ] Сопоставить подписки с OnDisable/OnDestroy и исправить подтверждённые утечки.
- [ ] Unity Editor и WebGL smoke после рефакторинга; новая Phase 6 CI проверка ожидает подтверждения.

## Phase 7 — UI ownership

- [ ] Устранить конкурирующие layout writes, определить единственного владельца.
- [ ] Уменьшить `HudVisualPolish` зависимости; безопасно разделить FrontEnd/Navigator/Touch UI.
- [ ] Regression: русские/английские надписи, масштаб, touch и меню.

## Phase 8 — Save/progression

- [ ] Единый реестр save keys и разграничение device/cloud/QA.
- [ ] Миграции legacy progress + тесты существующих сохранений.
- [ ] Restart, cloud conflict, fresh-save и offline scenarios.

## Phase 9 — City/runtime

- [ ] Убрать лишнюю startup diagnostics из release; не терять QA диагностику.
- [ ] Перенести безопасные runtime material repairs в Editor build шаги.
- [ ] Profiler-driven оптимизация города и проверка GC/CPU/памяти.

## Phase 10 — Vehicle physics

- [ ] Trace `FixedUpdate` (force/steer/friction/wheels/collisions).
- [ ] Проверить drive modes, handbrake, WheelCollider friction, mobile input timing; затем настройка параметров по измерениям.
- [ ] Play Mode road collision smoke всех релевантных машин.

## Phase 11 — Final asset/package cleanup

- [ ] Третьесторонние пакеты, точные binary duplicates и texture dependencies.
- [ ] Разделение source/generated runtime city, размер репозитория и LFS (только после решения).
- [ ] Удалять исключительно доказанные candidates отдельными revertable commits.

## Phase 12 — Release matrix

- [ ] WebGL Desktop Release и WebGL Mobile Release из соответствующих **Build Profiles**, версия/хеш каждой сборки, браузер/устройство и Console.
- [ ] Editor / WebGL Development / WebGL Release: fresh/existing save, restart/cloud, intro, keyboard, touch arrows/wheel, все машины/визуал, paint/wheels/neon, lights, garage/menu/continue, activities, pause/resume.
- [ ] Негативные проверки: QA/Admin/QA reset **отсутствуют** в Release; missing scripts/materials/shaders = 0.
- [ ] После последнего релевантного кода — финальный WebGL/Unity regression gate.

## Реестр обнаруженных проблем и отложенных проверок (сохранён из аудита 2026-10-07)

Этот раздел — **не список подтверждённых текущих багов**. Это исходные находки, риски и отложенные проверки, которым назначена следующая фаза. Не путать обнаруженный архитектурный риск с воспроизведённым дефектом.

| ID | Обнаруженная проблема или риск | Состояние / куда вернуться |
| --- | --- | --- |
| F0-01 | 2 Missing Script на `Double-Block-09/Water` и `Water-B` | **Исправлено и проверено**, `4da9a30`, Unity audit + визуал; не открывать без регрессии |
| F0-02 | Deprecated Input Manager + legacy Input API в части C# и WebGL profiles | **Исправлено**, Input System New и Unity/источники PASS; отдельная матрица Release — Phase 12 |
| F1-01 | QA/Admin раньше попадала в production WebGL; test/reset/mock API | **Основная изоляция исправлена и проверена**; полный отрицательный Release API/call-site gate проверить в Phase 12 |
| F3-01 | Hybrid `i8_body/i8_misc`, legacy `wheels1/wheels2`, различия role naming | Контракт/импортёры проверены. **Legacy Hybrid wheels не удалять**, точные authored dependencies сохранять; дорожная физика в Phase 10 |
| F4-01 | Общие atlas/material slots `chrome`, `plastic`, `Material.00x`, `Color`, `baseGradient`; Hybrid `Material.004/.005` | Безопасная role mapping миграция готова. **Неразмеченные слоты/fallback остаются**; полное устранение name magic только после submesh mapping и отдельных lamp regressions |
| F4-02 | `PlayerVehicleRearEmission` (~2,2k строк) объединяет binding/overlay/mesh heuristics/night | Отложен модульный split `LampRoleResolver` / `LampMaterialBinding` / `LampOverlayFactory` / `RearLampController`, после visual role tests |
| F5-01 | Вероятно старые `BeatallMaterials/{BeatallBody,BeatallGlass,BeatallEmission}.mat`, аналогичные `AmgGTMaterials` и `DeloreanMaterials` | **Кандидаты, не подтверждённые orphan assets**. Проверить Unity dependency, реальные импортёры, prefab slots, Resources/load, день/ночь/brake/customization, WebGL перед удалением |
| F5-02 | FCG Legacy FBX Importer Fixer и URP Fixer могут казаться мёртвыми по C# refs | **Сохранять**: ручные восстановительные инструменты, включая возможный повторный импорт старых FCG assets; `Assets/Simple Garage` в прежнем дереве отсутствовала |
| F6-01 | `MotorCityBootstrap` вручную создаёт многие системы и зависит от порядка platform/persistence → world/vehicle → progression/activities → UI/QA | Phase 6: модульный split с сохранением порядка и проверкой повторной загрузки |
| F6-02 | `DontDestroyOnLoad` в UI EventSystem, VirtualInput, HudVisualPolish, SFX/Music, Bootstrap, Yandex | Phase 6: таблица creation/duplicate guard/static reset/OnDestroy, тест domain reload и отсутствия двух EventSystem/AudioListener |
| F6-03 | Stale event subscriptions/static caches и старый HUD polish после scene reload | Phase 6: сравнить конкретные subscribe/unsubscribe, Unity reload/restart tests; текстовый счёт `+=/-=` сам по себе не доказательство |
| F7-01 | `HudVisualPolish` меняет размеры/позиции поверх `PrototypeHud`, `GarageReferenceLayout`, `NavigatorView`, `TouchControlsView`, ищет GO по строкам | Phase 7: единый владелец RectTransform; `GarageReferenceLayout` является активным builder, **не удалять** |
| F8-01 | Прямые `PlayerPrefs` в `MotorCityInput` и `MotorCityQualityRuntime` при существующем SaveService | Phase 8: registry и классы device-local/cloud/entitlement/QA/legacy, не переносить device settings в cloud случайно |
| F8-02 | QA `ResetProgressForTesting` и legacy/cloud revision metadata | QA entrypoint изолирован; Phase 8: тест сохранения/миграции/cloud resolver после QA reset и restart |
| F9-01 | `CityAssetRuntimeInstaller.RepairMissingPlantMaterials`: hash-suffixed `Trees-01` paths, поиск `Plant-01` и массовый renderer scan | Phase 9: до удаления repair перенести коррекцию в Editor builder, validate `CityVisual.prefab` в Unity/WebGL |
| F9-02 | `MotorCityWebMaterialDiagnostics.Run(activeCity)` выполняет обход большого города в production WebGL | Phase 9: проверить фактический release guard и перенести подробный scan в QA/Editor; не удалять диагностику, пока не сохранены полезные проверки |
| F9-03 | Крупный runtime city (исходный аудит: 22 559 renderers, 788 lights) | Phase 9: Profiler CPU/GC/batches/physics/memory/loading Low/Medium/High; никаких оптимизаций вслепую |
| F10-01 | `ArcadeRacingCarRuntimeInstaller` объединяет visual/collision/wheels/material/cache/legacy ARCADE | Phase 10: выделение `VehicleVisualLoader`, `VehicleWheelBinder`, `VehicleCollisionBuilder`, `VehicleMaterialPipeline`, `VehicleVisualCache` без изменения поведения |
| F10-02 | `ArcadeCarController` объединяет steering, wheel rig, friction, force, drive modes, handbrake, locks и mobile input | Phase 10: `FixedUpdate` tracing `steerAngle`, sideways/forward friction, AddForce/AddTorque, cadence (~0.12s), input Update → FixedUpdate; не вводить новый yaw assist до трассировки |
| F10-03 | `MotorCityVirtualInputRuntime` BeforeSceneLoad/Update/LateUpdate управляет pressed/held semantics | **Не удалять**; Phase 6/10 проверка жизненного цикла и сброса virtual state при menu/garage/pause |
| F11-01 | Дубликат `Eric VFX .../circle2.PNG` и `Resources/MotorCity/UI/Loading/circle2.PNG` | **Оба имеют разных consumers** (GUID и `Resources.Load` соответственно). Не дедуплицировать без миграции ссылок |
| F11-02 | Совпадающие `AmgGT/all.png`, `Beatall/all.png`, `Delorean/all.png`; Hybrid `wheels1/2.mtl`; Porsche/Toyota wheel MTL | Phase 11: exact duplicates ещё не основание для удаления; требуются reimport, refs и vehicle visual regressions |
| F11-03 | FCG source / Workbench / runtime Environment — разные уровни pipeline; Workbench не входит в build | Phase 11: не удалять Workbench, пока не доказана воспроизводимость; решение по LFS отдельно |
| F11-04 | SpringBone, ToonShader, SpriteLess UI, SapphiArt, Fantasy Skybox, Eric VFX, FCG, ARCADE FREE, Prometeo | Phase 11: dependency report + serialized GUID + shader/prefab usage + licences до package удаления |
| F12-01 | Недостаточно подтверждения отдельно `Web - Desktop - Release` и `Web - Mobile - Release` после всех изменений | Phase 12: фактические сборки, браузерные keyboard/touch tests, Console, QA absence, save scenarios |

### Отдельные functional regression gates

- **Activities/onboarding/navigation (Phase 7–8 / финальная Phase 12):** для `ActivityManager`, `ActivityStartFlow`, StreetSprint, CircuitRace, DriftChallenge, Delivery, `FirstSessionOnboardingSystem`, `ResultNextGoalResolver`, `NavigatorView` проверить idle → start/ad → countdown → active → cancel/result → reward/save **ровно один раз** → next goal; interruption по pause/menu и onboarding override. Не удалять «лишние» состояния без transition tests.
- **Lighting (Phase 4 contract + Phase 12):** day/off, night/on, brake, reverse (при наличии), отсутствие свечения кузова/зеркал; убрать старые material-name branches только после проверки всех автомобилей.
- **Touch settings (Phase 8/10/12):** keyboard/arrows/wheel transitions, положение кнопок, сброс virtual state, WebGL mobile detection и device simulation; не совмещать рискованную миграцию input state с tuning physics.

## Dependency / deletion gate (обязателен для каждого удаления)

1. **A — C# symbols/calls:** прямые, косвенные и reflection-ссылки.
2. **B — Unity serialized:** GUID, prefab, scene, animation, asset, material и зависимости.
3. **C — Dynamic resources:** `Resources.Load`, строковые имена, addressables, runtime generation.
4. **D — Import/source:** FBX, .meta, Editor builder/migrator, generated assets.
5. **E — Persistence:** PlayerPrefs/save keys, облако, локализация, аналитика.
6. **F — Lifecycle:** singleton, scene transitions, event subscriptions, setup order.
7. **G — Verification:** source/Unity compile, audit, relevant Play Mode, WebGL, no new missing script/material/shader; отдельный откатываемый commit.

Список unreferenced GUID сам по себе **не доказывает**, что файл можно удалять. При отсутствии надёжного доказательства — оставить asset и закрыть только безопасный audit, без выдуманного «успешного удаления».
