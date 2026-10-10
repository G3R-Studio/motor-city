# Motor City — Project Health / Cleanup Roadmap

Обновлено: **2026-10-11**. Исходный аудит: 2026-10-07. История промежуточных попыток и ошибок удалена из этого трекера; она остаётся в Git-коммитах и GitHub Actions.

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
| 6 — Bootstrap/lifecycle | ✅ Закрыта в проверенном scope | source/UI/Unity/WebGL CI PASS; обычный Play Mode и Domain Reload подтверждены; браузерная матрица — Phase 12 |
| 7 — UI ownership | ✅ Закрыта в проверенном объёме | UI/source/Unity CI PASS, итоговый Play Mode PASS; WebGL browser matrix — Phase 12 |
| 8 — Save/progression | 🟡 Локальный scope PASS, cloud pending | CI Unity/source PASS (#38069503126); Play Mode + restart PASS по пользователю; Yandex cloud/device QA ещё открыта |
| 9 — City/runtime | 🟡 Начата | release diagnostic guard и Editor prefab audit; profiler впереди |
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
- [x] Phase 6: baseline source-инвариантов bootstrap, постоянных hosts и кандидатов событийных подписок добавлен в `Tools/check_phase6_lifecycle.py` и CI (`e666174`). Source gate подтверждён GitHub Actions #107, Unity Editor PASS; WebGL остался skipped.

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

## Phase 6 — Bootstrap/lifecycle ✅ (проверенный scope)

- [x] Добавлен CI source baseline: SubsystemRegistration, AfterSceneLoad, sceneLoaded subscriptions, once-only flags и перечень постоянных hosts.
- [x] Сформирован автоматический inventory вероятных мест событийных подписок — **не** вывод об утечках.
- [x] Разделить `MotorCityBootstrap` на логические фазы без изменения порядка platform → remote config → pending purchases → cloud → frontend → gameplay. Выполнен крупный source split: `InitializeCoreAndWorld`, `InitializePlayerVehicle` и единый `InitializeGameplayStages` с последовательными `InitializeGameplayServices`, `InitializeVehicleProgression`, `InitializeActivitiesAndRewards`, `InitializePresentationAndHud` (`6c0719a`); порядок теперь охраняет CI (`160af05`). Unity CI и повторная загрузка подтверждены; ownership основных hosts описан.
- [x] Source hardening persistent lifecycle: `MotorCityVirtualInputRuntime` и `HudVisualPolish` добавлены duplicate guards в Awake; `MotorCityBootstrap.ResetStaticState` снимает sceneLoaded handler (`0bef984`, `d86244d`, `d24c650`). Source gate дополнен `a9ca501`; Unity CI подтверждён.
- [x] Пользователь проверил повторные запуски Play Mode при отключённом Domain Reload и переходы между экранами — PASS (2026-10-10). Это ручной smoke, а не автоматический подсчёт всех экземпляров.
- [x] Зафиксирована таблица ownership семи persistent-host систем и platform systems, отдельный файл `Docs/Baselines/Phase6LifecycleAudit-2026-10-09.md`. Проверено по исходникам: точечный `sceneLoaded` handler, audio `OnDestroy`, singleton `Awake` guards. Расширен source gate (`ab85b71`). **Это не runtime leak-free certification.**
- [x] Проверен явный `sceneLoaded` callback bootstrap (снятие при reset и повторном install), singleton `Awake` guards и `OnDestroy` очистка ссылок аудио; расширенная source gate PASS. Инвентарь прочих `+=/-=` (74 файла-кандидата) сам по себе не доказывает утечки: непроверенные владельцы остаются предметом дальнейшего компонентного аудита, без заявления о полном отсутствии утечек.
- [x] Unity Editor CI после рефакторинга: последняя подтверждённая проверка [run 37995242929](https://github.com/G3R-Studio/motor-city/actions/runs/37995242929) — source/UI/Unity Editor PASS. Ручной Play Mode после singleton fixes подтверждён пользователем.
- [x] WebGL **сборка** прошла в [workflow 37996659979](https://github.com/G3R-Studio/motor-city/actions/runs/37996659979): также PASS source/UI/Unity Editor на `b4c2d4a`. Браузерный runtime WebGL и полная матрица Desktop/Mobile принадлежат Phase 12.
- [ ] Автоматическая инструментальная проверка точного количества live singleton объектов не реализована; ручной Domain Reload smoke PASS, при будущих регрессиях добавить диагностику вместо необоснованных утверждений.
- [x] **Ручной Unity Play Mode smoke после Phase 6 fix:** 2026-10-09 пользователь подтвердил отсутствие ошибок и новых багов после повторного открытия проекта. Это подтверждение ручного прогона, а не автоматического WebGL build.

## Phase 7 — UI ownership ✅

- [x] Начальный source inventory `Tools/check_phase7_ui_ownership.py`: роли HUD polish, PrototypeHud, GarageReferenceLayout, NavigatorView, TouchControlsView; отчёт по позициям/размерам и named targets, подключён в CI (`fd9790a`). Это **не** доказательство конкретного двойного owner на каждом элементе; Unity CI по новому gate ожидается.

- [x] Исправлено повторное использование отсоединённых `RectTransform` из кеша `HudVisualPolish.FindRect`: повторная проверка `IsChildOf(hudRoot)` и инвалидирование. Новые source guards отделяют touch-control geometry (`TouchControlsView`/персонализация) от внешнего HUD polish. Ожидается CI и проверка Play Mode после кода (`3c21ca9`, `6258e5a`).
- [x] Устранено подтверждённое дублирование координат `HUD Utility Rail` / `HUD Secondary Actions`: единственный владелец позиций — `TouchControlsView`; записи `HudVisualPolish` удалены без изменения чисел; CI запрещает возврат двойного layout owner (`89bf443`, `f864333`). Ожидается итоговый CI и Play Mode после этого изменения.
- [x] Сняты повторные записи `Minimap` и `Navigation Target Strip` из `HudVisualPolish`: десктопные координаты уже задаёт `NavigatorView`, `UseLandscapeTouchLayout()` сейчас всегда возвращает `false`. Настроен source gate на единственного владельца (`70adc745`, `13cad558`). CI и итоговый Play Mode по этому пакету ещё требуются.
- [x] Корневые modal panels `Activity Result`, `Navigator Menu`, `Club Panel` переданы соответственно `ActivityResultView`, `NavigatorView`, `ClubView`; размеры перенесены из действующего desktop visual polish в builder без изменения итоговой геометрии. `HudVisualPolish` оставлен для содержимого и текста. CI-контракт `c402642`; финальный Unity CI/Play Mode ещё требуются.
- [x] Удалены повторные записи геометрии `Speedometer`, `Activity Status` и `Minimap Target Label` из `HudVisualPolish`: `DrivingHudView` и `NavigatorView` уже создают desktop layout с теми же значениями. Добавлен source gate (`26626cc`, `0d994da`); проверка CI/Play Mode после общего пакета остаётся.
- [x] Финальный source-level перенос: `Player Card` / `Character Card`, `Active Objective`, `Status Text`, `Result Details`, `Result Reward`, `Result Reward Icon` больше не получают geometry writes от `HudVisualPolish`. Значения desktop-layout перенесены в builder, добавлен ownership gate. Итоговый CI и единый ручной smoke ещё ожидаются.
- [x] Устранить подтверждённые конкурирующие root/child layout writes, определить владельцев в проверенном объёме. Не утверждаем отсутствие всех возможных runtime конфликтов.
- [x] Уменьшить geometry-зависимости `HudVisualPolish`: root layouts закреплены за DrivingHudView/CharacterMissionCardView, модальные панели — за собственными builder, навигация — NavigatorView, utility/touch — TouchControlsView. Полный split FrontEnd не требуется для доказанных конфликтов.
- [x] Исправлена выявленная пользователем регрессия окна результата: при одновременном mastery/secondary progress/next event кнопки перекрывали строку события. Высота Activity Result увеличена до 520, размеры и позиция footer закреплены за ActivityResultView, CI проверяет минимальный зазор 12 единиц (коммиты `c319cef`, `b3eb257`). Runtime QA ещё требуется.
- [x] Итоговый Play Mode после устранения наложения текста и кнопок результатов — PASS по подтверждению пользователя (2026-10-10); последний CI [run 38005440827](https://github.com/G3R-Studio/motor-city/actions/runs/38005440827): source/UI/Unity Editor PASS, WebGL skipped. Полная браузерная матрица и отдельные device/localization сценарии остаются в Phase 12.

## Phase 8 — Save/progression 🟡 (локальные сохранения проверены, Yandex cloud ожидает QA)

- [x] Итоговая приёмка локального прогресса: пользователь подтвердил Play Mode/restart PASS 2026-10-10 после CI [#38069503126](https://github.com/G3R-Studio/motor-city/actions/runs/38069503126) (Unity Editor, source, UI PASS; WebGL skipped). Это не подтверждает реальную синхронизацию Yandex Games между устройствами.
- [ ] Пройти Yandex Games login/cloud persistence и cross-device/offline conflict сценарии на отдельном тестовом аккаунте; только после этого закрыть Phase 8 целиком.

- [x] Финальный автоматический пакет: production cloud конфликт-резолвер протестирован в Unity Editor на фиктивных метаданных; GUID-изолированный PlayerPrefs проверяет ACK ревизии при изменении данных во время загрузки, неподходящий облачный импорт и неизменность локального значения. CI ожидается. Реальное облако Yandex, настоящий restart и ручная приёмка ещё открыты.


- [x] Интеграционный Editor тест PlayerPrefs: временные GUID-слоты проверяют corrupt backup, migration-on-read legacy int, сохранение и имитацию reload; тестовый namespace удаляется в finally и production ключи не используются. CI ожидается; это не полноценный перезапуск процесса и не cloud runtime тест.


- [x] Добавлена валидация JSON-конверта сохранения перед локальной загрузкой и облачным импортом; неподходящие JSON-объекты не должны заменять прогресс, локальный повреждённый конверт попадает в backup. Добавлены Unity Editor fixtures (`{}`, foreign, `Version=0`) и source gate; интеграционные PlayerPrefs/restart тесты и CI ещё ожидаются.


- [x] CI Unity фильтр теперь сравнивает git diff с последним успешно завершившимся **Unity Editor job**, включая C# изменения из отменённых промежуточных запусков; при сбое GitHub API фильтр консервативно запускает Unity (`Tools/check_unity_ci_changes.py`). Итоговый CI ожидается.


- [x] Обнаружен и исправлен немедленный повторный вызов cloud save после неудачного callback с queued-запросом. При ошибке отправка переносится на интервал (15 с), unsynced локальная ревизия сохраняется; добавлен source gate. Unity/runtime QA ещё ожидается.


- [x] Оптимизация CI: для изменений только `Docs/` и Python-аудитов Editor job пропускается, но быстрые source/UI gates выполняются; Unity source/assets/settings и ручной `workflow_dispatch` запускают полный Editor gate. Во время импорта CI печатает последние строки Unity лога раз в минуту. Полный прогон после изменения workflow ожидается.


- [x] Добавлен изолированный Unity Editor gate `MotorCityPhase8SaveSerializationGate`: реальные `JsonUtility` и `TryReadCloudMetadata` на фиктивных v1/v2 данных, malformed JSON, revision flags; не обращается к `PlayerPrefs` или облаку. Интегрирован в `MotorCityPhase2BatchGate` (CI ещё ожидается).


- [x] Выполнен первый baseline audit: `Docs/Baselines/Phase8SaveProgressionAudit-2026-10-10.md`, автоматический реестр literal save keys + source contracts локального слота, backup, схемы и cloud revision (`Tools/check_phase8_save_progression.py`). Добавлен gate в CI; runtime миграции и облако не проверялись.

- [ ] Единый реестр save keys и разграничение device/cloud/QA.
- [x] Добавлены изолированные модельные сценарии cloud conflict и legacy JSON fixture в `Tools/test_phase8_save_fixtures.py`, подключены в CI (`61d8d0b`). Это Python-модели правил, **не** фактические Unity JsonUtility/PlayerPrefs тесты и не интеграционная проверка Yandex.
- [ ] Миграции legacy progress + тесты существующих сохранений.
- [x] Локальный restart/save в Play Mode — PASS по сообщению пользователя (2026-10-10); изолированные конфликтные сценарии прошли Unity Editor CI. Реальные cloud conflict, fresh device и offline-синхронизация Yandex остаются непроверенными.

## Phase 9 — City/runtime 🟡

- [x] **Unity CI #222** ([run 38091108464](https://github.com/G3R-Studio/motor-city/actions/runs/38091108464), `b9583f4`) прошёл: source/UI/Unity Editor batch. Точный read-only audit текущего `CityVisual.prefab`: **25 634 renderers; 0 missing material slots; 12 Plant-01 renderers; 0 Plant-01 missing slots; 0 unexpected non-plant missing slots**. Цифры получены из журнала Unity job; WebGL job был **skipped**. По сравнению с прежним снимком (26 123 renderers / 12 missing slots) количество рендереров изменилось на −489; причины изменения здесь не установлены, объекты не удалять автоматически. Отчёт CI теперь печатается в логах и сохраняется как артефакт.
- [x] Проверено по текущему исходному коду: `RepairMissingPlantMaterials` **уже отсутствует** в `CityAssetRuntimeInstaller`; runtime не осуществляет принудительный plant material repair. `FantasticCityGeneratorRuntimeBuilder` сохраняет авторские ссылки материалов 1:1; `4 - Fix Materials` — отдельный ручной инструмент, который может изменить сгенерированные материалы. **Решение: не добавлять автоисправление слотов в Editor и не возвращать runtime repair**, поскольку текущий префаб без дыр. Ночное свечение окон через runtime-клоны материалов оставить как отдельную нужную функцию.

- [ ] **2026-10-11 async city prefab preload experiment** (`ccae1e7`, `7a239b9`, source guard `6568739`): старт `Resources.LoadAsync<GameObject>` при появлении loading screen, вызов оригинального gameplay bootstrap после `isDone`, сохранены синхронный fallback и полный порядок создания города; Domain Reload reset очищает ссылку на запрос. Цель — убрать паузу `Resources.Load` из кадра создания города, а не обещать снижение полного времени загрузки. **Unity compile/Play Mode/WebGL smoke и замеры ожидаются**: сравнить `MotorCity.City.LoadPrefab`, `MotorCity.City.Instantiate`, максимальный frame time и реальную длительность loading screen; убедиться в появлении выбора управления и сохранении освещения/FCG traffic.

- [ ] **2026-10-11 Editor Profiler baseline (первый въезд в город, кадр 165):** `MotorCity.City.LoadPrefab` 1 370,27 мс / 2,6 МБ GC, `MotorCity.City.Instantiate` 703,52 мс / 2,6 МБ GC; `MotorCity.Minimap.Build` 145,09 мс / 16,9 МБ GC. Это измерения в Editor, не WebGL. Основная задержка загрузки — Unity `Resources.Load` / `Instantiate`; без доказательств не трогать prefab/сцену ради фиктивного ускорения.
- [ ] **Minimap allocation experiment** (`ce99542`, `8d32d53`): кеширование классификации имён по Transform, точечный поиск `FCG.FCGWaypointsContainer` с legacy fallback и `Color32` для RGBA32 texture. Source-level проверки выполнены; ещё нужны Unity compile/Play Mode, визуальная сверка мини-карты (дороги и здания) и повтор `MotorCity.Minimap.Build` CPU/GC на том же маршруте. SRP Batcher оставить включённым по умолчанию до WebGL benchmark.

- [x] Unity CI #38073086084 — исторический снимок CityVisual: **26 123 renderers, 12 missing slots (Plant-01)**. Актуальное состояние обновлено по CI #222 выше; старую цифру не использовать как новый baseline. Аудит сейчас информационный, без изменения prefab и без автоматического fail по missing slots.
- [x] Загрузчик делит один snapshot renderers между runtime-клонированием night-window emission и расчётом bounds (`a8c38b9`), а диагностика WebGL release отдельно ограничена.
- [ ] Дополнительную консолидацию обходов **25 634** renderers делать только при новом Profiler-доказательстве и после WebGL regression; прежнего runtime plant fix/rebinding больше нет.


- [x] CI checkout workaround для Windows self-hosted: отдельный Git cache `_work` заполняется из `D:\\GitHub\\motor-city` без изменений пользовательской репы; отсутствующие коммиты инкрементально загружаются в CI-копию без shallow fetch, LFS hydration из локального кэша, проверка точного SHA, сохранение Library/Temp. Маленький загрузочный скрипт берётся по immutable SHA через GitHub API. Это обход медленного checkout: фактические Unity Editor batch-запуски [#221](https://github.com/G3R-Studio/motor-city/actions/runs/38090497826) и [#226](https://github.com/G3R-Studio/motor-city/actions/runs/38092018558) успешно прошли с точной проверкой commit SHA.


- [x] После подтверждённого checkout stall (Git fetch, pack 0 MB, Assets отсутствует) CI #38070735540 отменён. Self-hosted Unity checkout ограничен 12 минутами, Git HTTP low-speed лимитами, `clean: false` для сохранения рабочего кэша и выводом `git count-objects -vH`; source/UI проверки остаются отдельными. Новый CI ожидается, корневая причина сетевого зависания пока не доказана.


- [x] Детальный `MotorCityWebMaterialDiagnostics.Run` защищён runtime `Debug.isDebugBuild` либо явным `MOTORCITY_CITY_MATERIAL_AUDIT` внутри WebGL player-ветки. Обычный Release не запускает массовый диагностический обход рендереров. Совместимость WebGL материалов не менялась; прежний runtime plant repair отсутствует и не восстанавливается.
- [x] Read-only Unity Editor аудит материалов исходного `CityVisual.prefab` сохраняет `unity-phase9-city-materials.txt`. Актуальный отчёт CI #222 просмотрен в логах; 0 пустых слотов. Изоляция диагностики сохранена; никаких исправлений материалов и изменений Workbench не выполнялось.
- [x] После сверки CI #222 с исходниками решено не переносить plant repairs — соответствующего runtime метода уже нет, в текущем prefab проблемных слотов нет.


- [x] **2026-10-11 Release diagnostics scope** (`a08a361`, `a7d777a`): предупреждение о количестве FCG StreetLight/ParkLamp ограничено `Application.isEditor || Debug.isDebugBuild`, при этом учёт/управление источниками света не менялись. WebGL full-city `MotorCityWebMaterialDiagnostics.Run` по-прежнему выполняется лишь в development (`Debug.isDebugBuild`) или при явном `MOTORCITY_CITY_MATERIAL_AUDIT`. Source gate теперь охраняет оба условия. [CI #226](https://github.com/G3R-Studio/motor-city/actions/runs/38092018558) **PASS**: source/UI/Unity 6 Editor; WebGL build **skipped**, поэтому браузерное подтверждение Release остаётся в Phase 12. Это закрывает изоляцию найденной диагностики, а не заявляет об оптимизации загрузки всего города.
- [x] Не переносить устаревший plant repair в Editor: он уже удалён из runtime и текущий префаб целостный. Для новых дефектов сначала воспроизвести и доказать конкретное нарушение authoring в FCG Workbench; автоматические изменения prefab не разрешены.
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
| F9-01 | Исторический `RepairMissingPlantMaterials` и 12 пустых Plant-01 material slots | **Нет в текущем runtime**; CI #222: 0 missing slots, перенос repair в Editor не требуется. WebGL smoke — отдельно в Phase 12 |
| F9-02 | Ранее подробный `MotorCityWebMaterialDiagnostics.Run` обходил город в release WebGL | **Исправлено в коде**: подробный scan запускается только в WebGL debug или при `MOTORCITY_CITY_MATERIAL_AUDIT`; release WebGL smoke остаётся в Phase 12 |
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
