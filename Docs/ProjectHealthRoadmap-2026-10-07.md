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

> **Дата начала трекинга:** 2026-10-08. **Текущий статус:** Phase 0 — Freeze / baseline (открыта); Phase 1 — Release safety / P0.1 (частично проверена, не закрыта).
> **Правило отметок:** `[x]` ставится только после выполнения работы **и** проверки применимых verification gates, включая подтверждение в Unity от пользователя. Работа в GitHub без проверки в Unity не считается завершённой фазой. `[ ]` — не завершено, даже если код уже подготовлен.
> **Рабочий процесс:** одна фаза (или отдельно оговорённый подпункт) за раз → проверка ссылок/GUID и зависимостей по Gate A–F → изменение отдельным коммитом в `main` → статические проверки → инструкция для Unity → подтверждение пользователя → обновление галочек и журнала. Никаких удалений без Gate G.

## Обзор фаз

- [ ] **Phase 0 — Freeze / baseline** — В РАБОТЕ: зафиксировать состояние до чистки
- [ ] **Phase 1 — Release safety** — P0.1, QA UI изолирован; финальный аудит testing API открыт
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

## Подтверждённые проверки (это не закрытие всей фазы)

- [x] Phase 0: source/regression проверки и статический GUID audit; Unity Editor/Play Mode baseline.
- [x] Phase 0: устранены два Missing Script; проверены материалы в Unity и миграция Input System (New).
- [x] Phase 1 / P0.1: Unity Console — предупреждения `UAC0009` исчезли по подтверждению пользователя.
- [x] Phase 1 / P0.1: WebGL Desktop Release — запуск, управление, отсутствие QA/Admin по подтверждению пользователя.
- [x] Phase 1 / P0.1: WebGL Development — запуск и доступность QA/Admin по подтверждению пользователя.
- [ ] Phase 1 / P0.1: итоговая проверка всех QA/mock/reset путей перед закрытием фазы.
- [ ] Phase 12: формальная матрица сборок, включая WebGL Mobile Release; не считать её выполненной по Desktop smoke.

## Журнал выполнения

| Дата | Фаза | Действие | Статус / доказательство |
| --- | --- | --- | --- |
| 2026-10-08 | Phase 0 | Введён трекер этапов по существующей roadmap, код игры не менялся | Подготовка выполнена; baseline, Unity smoke и WebGL ещё не проверены |
| 2026-10-08 | Phase 0 | Пользователь подтвердил `git fetch` → `git pull --ff-only` → `git status` → `HEAD 3db9cf2a` | Git синхронизирован, рабочее дерево чистое; source-тесты и аудит впереди |
| 2026-10-08 | Phase 0 | `check_cleanup.ps1`: 242 C# синтаксически корректны (Editor/native/WebGL), reflection-проверки прошли. Source-тест VehicleLampMaterialUtility остановился на `CS0234` из-за отсутствующей зависимости | Исправлен только тестовый harness (`fac2c5f`, `85725b4`); повторный запуск ожидается, полный gate открыт |
| 2026-10-08 | Phase 0 | Повторный запуск: 242 C# / reflection / field cache / lamp checks пройдены; cosmetic source-test остановился на `ClearStaleVehiclePropertyBlocks` (`CS0103`) | В тесты добавлен реальный метод и его Unity stub/регрессии; ожидаем повторный запуск, полный gate ещё не пройден |
| 2026-10-08 | Phase 0 | Третий запуск: предыдущие проверки пройдены, но cosmetic `Add-Type` не видел namespace `UnityEngine` из другого динамического assembly (`CS0246`) | Устранена зависимость между отдельными `Add-Type` компиляциями; статически проверены все семь методов и типы. Запуск PowerShell у пользователя **ещё не подтверждён** |
| 2026-10-08 | Phase 0 | Четвёртый запуск: кастомизационный `Add-Type` попытался повторно объявить `UnityEngine.Transform` (конфликт типов) | Использован уникальный namespace `MotorCityCleanupTests.Customization`; все тестовые типы и извлечённый исходный C# проверены статически. Полный runtime-прогон ожидается |
| 2026-10-08 | Phase 0 | Пользователь подтвердил **полностью успешный** `pwsh -NoProfile -File Tools/check_cleanup.ps1` на 242 C# файлах, включая reflection, field/lamp и cosmetic тесты | **[x] Статическая/source проверка закрыта**; Unity compile, static-audit JSON и прочие Phase 0 пункты ещё впереди |
| 2026-10-08 | Phase 0 | Принят и проверен `static-audit.json`: 4471 файлов, 2329 GUID, 3232 serialized assets, 246 C#; 0 дубли GUID, 0 deleted/dangling refs, 241 кандидатов без входящих GUID | **[x] Статический baseline записан**. Кандидатов не удалять: требуются динамические/импортные и Unity dependency gates. Отчёт не содержит Git SHA; Unity/Console/visual baseline ещё ожидаются |
| 2026-10-08 | Phase 0 | Пользователь прислал Unity Console и `project-audit.txt`. Найдены 2 Missing Script в `Double-Block-09.prefab` (Water/Water-B) на одном GUID `296cbc687256a5b47bc5c557f8bdd095`; найден inbound reference из `Generate.prefab`. В Console предупреждение Input Manager (deprecated); активный `Prototype` в Edit Mode пустой (`0/0/0`). | **[x] Edit Mode Unity audit baseline записан**, но Missing Script/Console warning **не исправлены**; Play Mode аудит, снимки машин и physics smoke впереди. Никаких удалений/изменений префаба |
| 2026-10-08 | Phase 0 | Play Mode Unity Audit + Console: `Prototype` 3486 scripts, 788 lights, 22559 renderers; в отчёте loaded scene нет новых `ERROR` или `REVIEW`; prefab-скан по-прежнему находит два `Missing Script` (`Water`/`Water-B`), Console показывает два соответствующих `Unknown Behaviour is missing` warning. | **[x] Play Mode audit baseline принят**; структурные missing-script findings остаются открытыми, предупреждения не игнорируются. Smoke menu/garage/continue, снимки машин и управление ещё не подтверждены |
| 2026-10-08 | Phase 0 | Пользователь подтвердил «всё нормально» после Play Mode smoke: главное меню → выбор управления → город → гараж → пауза → главное меню → Continue → повторный выбор управления → город | **[x] Сквозной игровой smoke подтверждён**; ранее найденные 2 missing script и Input Manager warning остаются открытыми; визуальный/физический baseline впереди |
| 2026-10-08 | Phase 0 | Получены и осмотрены скриншоты гаража Street (Стритер), Beatall (Битл), Peugeot306 (Пыж 306); видимые кузов, стёкла и диски без явных дефектов на одном ракурсе | **[x] 3/10 гаражных визуальных снимков**; остальные 7 машин, изменение косметики, фары/стопы день-ночь ещё не проверены |
| 2026-10-08 | Phase 0 | Пользователь дополнительно сообщил: «все 10 машин нормально работают, я уже проверял их» | **[x] Базовая работоспособность 10/10 машин подтверждена пользователем**; не требовать повторных гаражных скриншотов. Из 10 есть 3 изображения, детальные проверки день/ночь, ламп, косметики и physics/controls не подразумеваются автоматически |
| 2026-10-08 | Phase 0 | На вопрос о двух режимах мобильного управления, газе, тормозе, ручнике и настройке положения кнопок пользователь ответил «проверял» | **[x] Факт ручного smoke-тестирования управления зафиксирован**, новых проблем не сообщено. Глубокий аудит физики (`FixedUpdate`, friction/steer/handbrake) и визуальные день/ночь проверки не завершены |
| 2026-10-08 | Phase 0 | На прямой вопрос «проверял работу фар ночью, стоп-сигналов при торможении и изменение цвета, дисков и неона?» пользователь ответил «да» | **[x] Функциональная проверка ночных фар, тормозных огней, цвета, дисков и неона отмечена по подтверждению пользователя.** Скриншоты со всех углов/ночного режима не представлены; missing scripts и Input Manager warning остаются открытыми |
| 2026-10-08 | Phase 0 / исправление найденного дефекта | Сверены 2 отсутствующих MonoBehaviour на `Water`/`Water-B` и входящая ссылка в `Generate.prefab`; минимальным изменением префаба удалены ровно 2 пустые нерабочие компоненты (`4da9a30`). Git diff проверен, прочие 454 GO/408 Mesh компонентов/25 Behaviour сохранены. | **[x] Исправление в исходниках**; **[ ] Unity gate** (повторный Edit/Play аудит и Console) ожидает пользователя. Input Manager warning ведётся отдельно. |
| 2026-10-08 | Phase 0 / Missing Script | Получен отчёт Unity `project-audit.txt` от `2026-10-08 21:00:08Z`: отсутствуют строки `ERROR missing prefab scripts`; loaded `Prototype` 3486 MonoBehaviour / 788 Light / 22559 Renderer, нет отчётных `ERROR` или `REVIEW`. | **[x] Префаб и загруженная сцена прошли повторный Unity audit** после коммита `4da9a30`. **[ ]** Скриншот Console и подтверждение внешнего вида города/воды после исправления ещё ожидаются; Input Manager deprecated ведётся отдельно |
| 2026-10-08 | Phase 0 / Missing Script | Пользователь подтвердил «да»: после обновления город, дороги и вода отображаются нормально, в Console нет новых красных ошибок и двух прежних `Missing Script` warning. | **[x] Дефект `Double-Block-09` полностью закрыт**: исходник исправлен, Unity audit чист, Play Mode визуально подтверждён. Warning Input Manager, архив снимков, prefab/material inventory и глубокий physics audit остаются отдельными открытыми задачами |
| 2026-10-08 | Phase 0 | Из Git HEAD `ec98dc91` создан baseline для **647 префабов и 207 материалов** (`Docs/Baselines/PrefabMaterialInventory-2026-10-08.json` и человекочитаемая инструкция `PrefabMaterialBaseline-2026-10-08.md`); записаны Git SHA содержимого и `.meta` всех 854 файлов, UUID 14 критических префабов/материалов, отмечены LFS pointer и большой CityVisual | **[x] Git-state prefab/material зафиксирован и оба отчёта проверены чтением из main**. Рендер/шейдеры Unity, архив всех машин, warning Input Manager и глубокая физика остаются отдельными gates; никаких игровых ассетов не меняли |
| 2026-10-08 | Phase 0 | Пользователь выполнил `Motor City → Diagnostics → Audit All Project Materials`: Unity нашла 767 Material; `Null shaders: 0`, `Unsupported shaders: 0`. Скрипт `MotorCityMaterialAudit.cs` использует `AssetDatabase.FindAssets("t:Material")` и `shader.isSupported`. | **[x] Project-wide shader compatibility audit закрыт в Unity Editor**; проверка runtime Renderer material slots в Play Mode, WebGL shader-совместимость и общий Phase 0 ещё открыты |
| 2026-10-08 | Phase 0 | Пользователь выполнил `Motor City → Diagnostics → Audit Materials In Open Scene` по загруженной сцене `Prototype`: `Missing material slots: 0`, `Unsupported/null-shader materials: 0` (отчёт из Console Unity). | **[x] Play Mode scene material audit пройден**. В сочетании с `Audit All Project Materials` (767/0/0) проверки материалов в Unity Editor зафиксированы. Shader-проверка WebGL и глобальное закрытие Phase 0 остаются отдельно |
| 2026-10-08 | Phase 0 / Input | Обнаружено `activeInputHandler: 2` у global PlayerSettings и двух WebGL release Build Profile; пакет Input System 1.20.0 уже был. Проверены зависимости: активный Prometeo добавляется через `ArcadeCarController` и может попасть в резервный legacy keyboard path. Мигрированы touchSupported/fallback клавиши/FCG FreeCamera, режимы настроек изменены на `1` во всех трёх местах; добавлен static guard `Tools/check_input_backend.py` и подробный отчёт `Docs/InputSystemMigration-2026-10-08.md`. | **[x] Исходники и конфигурация миграции подготовлены**, **[ ] Unity compile/Play Mode после смены backend, [ ] WebGL сборки**. Не утверждать, что warning исчез, без Console пользователя |
| 2026-10-08 | Phase 0 / Input | Пользователь сообщил «управление работает» после смены backend; `check_cleanup.ps1` прошёл: 242 C# Editor/native/WebGL и все source/regression checks. Новый `check_input_backend.py` первоначально FAIL: настройки `activeInputHandler: 1` ошибочно не распознаны (`found []`) из-за двойного экранирования Python regex, плюс найден четвёртый legacy вызов `ShiftAtRuntime.cs:24: Input.GetKeyDown(KeyCode.N)`. Исправлены regex + внутренние самопроверки (`f03f3d4`) и переключатель дня/ночи на Input System (`73010b9`); документ миграции дополнен (`7cf267d`). | **[x] Существующий gameplay smoke пользователем подтверждён; [x] cleanup source test PASS; [ ] повторный input backend guard; [ ] новая проверка Console; [ ] WebGL.** Фазу не закрывать до результатов |
| 2026-10-08 | Phase 0 / Input | Пользователь повторно запустил исправленный `py -3 Tools/check_input_backend.py` — **PASS**, все три настройки Active Input Handling = New, нет legacy Input API; подтвердил: «предупреждение исчезло» (Input Manager deprecation). Управление после смены backend подтверждено, ранее `check_cleanup.ps1` PASS (242 C#). Обновлён `Docs/InputSystemMigration-2026-10-08.md` (`af384d8`). | **[x] Unity Editor Input System migration gate закрыт** по проверке пользователя; **[ ] WebGL Desktop/Mobile Release build smoke**, **[ ] общая Console без любых предупреждений**, Phase 0 не закрывать автоматически |
| 2026-10-08 | Phase 0 / Input / WebGL | В ответ на просьбу проверить WebGL Desktop в браузере пользователь сообщил: «в webgl всё нормально, уже проверял». Дополнен `Docs/InputSystemMigration-2026-10-08.md` (`ecd2960`). | **[x] WebGL работоспособность подтверждена пользователем**; не требовать повторного общего smoke. **[ ]** Отдельных артефактов/отчётов для Desktop и Mobile Release после миграции нет; эти формальные gates остаются в Phase 12. Phase 0 ещё открыт по иным документационным/техническим пунктам |
| 2026-10-09 | Phase 1 / P0.1 | QA/Admin guards in `AdminDebugPanel`, `MotorCityInput`, `MotorCityFrontEndFlow`, `MotorCityBootstrap` and interstitial mock switched from `UNITY_EDITOR || UNITY_WEBGL` to `UNITY_EDITOR || DEVELOPMENT_BUILD` (10 guards across 5 files). Post-write source read-back: no old guards in these files; preprocessor #if/#endif balanced. | **[x] Source patch prepared**; **[ ] Unity Editor compile/Play Mode smoke**, **[ ] WebGL Development QA visibility**, **[ ] WebGL Release QA absence**. Do not close Phase 1 until user runtime confirmation. |
| 2026-10-09 | Phase 1 / P0.1 follow-up | Unity Console reported 10 `UAC0009` warnings from deprecated `DEVELOPMENT_BUILD` guards. Replaced all 10 in 5 QA-related C# files with `UNITY_EDITOR || DEBUG`; post-write reads show 0 deprecated guards and balanced preprocessor directives. | **[x] Source fix**; **[ ] Unity Console warning-clear confirmation**, **[ ] Development QA present / Release QA absent**. `DEBUG` behavior still must be verified in actual build profiles. |
| 2026-10-09 | Phase 1 / WebGL Desktop Release | Пользователь подтвердил «всё чётко» после инструкции собрать `Web - Desktop - Release`, проверить запуск, управление и отсутствие QA/Admin-панели. | **[x] Desktop Release ручной smoke по подтверждению пользователя**; **[ ] Development WebGL проверка наличия QA**, **[ ] Mobile Release и формальная build matrix**. |
| 2026-10-09 | Phase 1 / WebGL Development | После проверки Desktop Release пользователь подтвердил «всё чётко» для WebGL Development: QA/Admin доступна, игра работает. | **[x] Development WebGL ручной smoke по подтверждению пользователя**; **[x] Desktop Release QA isolation по предыдущему подтверждению**. **[ ]** Phase 1 окончательно закрывать только после сверки всех P0.1 QA/mock/reset paths и релевантных gates; **[ ]** формальная Mobile Release матрица. |
| 2026-10-09 | Phase 0/1 ревизия | Сверены Phase 0 и Phase 1 checkpoints: выполненные source/Unity smoke подтверждены; незакрыты визуальный архив, полная Console, physics audit, Mobile Release matrix; в Phase 1 проверены guards и выявлен открытый runtime API `MotorCitySaveService.ResetProgressForTesting()`. | **[x] Ревизия статусов**; **[ ]** Phase 0 не закрыта из-за оставшихся gates; **[ ]** Phase 1 не закрыта до полного QA API/call-site audit. Не трактовать наличие метода как доказанный exploit. |
| 2026-10-09 | Phase 1 / QA save reset | `MotorCitySaveService.ResetProgressForTesting()` защищён `#if UNITY_EDITOR || DEBUG`; единственный прямой caller в `MotorCityFrontEndFlow` защищён тем же условием. | **[x] Source guard**; **[x] Unity compile и запуск игры подтверждены пользователем**; **[ ]** Development и Release WebGL smoke после изменения; прочие testing API требуют проверки. |
| 2026-10-09 | Phase 1 / Unity verification | После коммита `59ec7e9` (QA-only guard для `ResetProgressForTesting`) пользователь сообщил «всё работает» после просьбы проверить компиляцию и запуск Unity. | **[x] Unity Editor compile/Play Mode smoke по подтверждению пользователя**; **[ ]** не засчитывать WebGL Development/Release пересборки без отдельного подтверждения. |
| 2026-10-09 | Phase 1 / QA API inventory | Проведён source review публичных `*ForTesting` в 24 gameplay/UI/world системах. Подтверждено, что UI-изоляция не равнозначна удалению всех тестовых методов из release assembly; пользователь также повторно подтвердил, что WebGL работает. | **[x] API inventory / generic WebGL smoke**; **[ ]** call-site/dependency audit и безопасная изоляция методов, проверка профильных сборок. Phase 1 пока не закрыта. |
| 2026-10-09 | Phase 1 / test API isolation batch 1 | Проверены GitHub call-site результаты для `CareerProgressionSystem.SetStageForTesting`, `DiscoverySystem.DiscoverForTesting`, `DiscoverAllForTesting`, `ResetForTesting`: вызовы находятся в QA/Admin; добавлены `UNITY_EDITOR || DEBUG` guards только на эти методы (`e46d05e`, `a887f67`). Баланс #if/#endif проверен. | **[x] Исходники первого безопасного пакета подготовлены**; **[ ]** Unity compile и release/development smoke после изменений; остальные тестовые API ещё не изолированы. |
| 2026-10-09 | Phase 1 / batch 1 Unity verification | Пользователь подтвердил «ошибок нет» после синхронизации первого пакета (`CareerProgressionSystem`, `DiscoverySystem`). | **[x] Unity Console без ошибок компиляции для первого пакета по сообщению пользователя**; остальные QA API и формальные Phase 0 gates остаются открыты. |


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

**Проверенные подготовительные шаги:**

- [x] Git-синхронизация и чистый рабочий каталог подтверждены пользователем: `git fetch origin`, `git pull --ff-only origin main`, `git status` (clean), `git rev-parse --short HEAD` → `3db9cf2a` (2026-10-08). Это **только подготовка**, не завершённый audit baseline.
- [x] Roslyn синтаксис: 242 C# файла, `UNITY_EDITOR`, native player, `UNITY_WEBGL`; пользователь подтвердил успешный вывод 2026-10-08. Тесты очереди/снимка/тумана/отключения reflection также прошли.
- [x] Полный `Tools/check_cleanup.ps1` — **подтверждён пользователем 2026-10-08**: `All 242 C# sources passed syntax checks (Editor, native player, WebGL).`; reflection queue / caller snapshot / fog restoration / disable; shared field cache; lamp material/UV/projection; cosmetic dispatch / property-block cleanup / save/events / vehicle initialization / color cycle. Финальная строка: `All cleanup syntax and source checks passed.` Это source/stub testing, **не** Unity compile и не WebGL build.
- [x] Сформирован и проверен пользователем предоставленный `static-audit.json` (2026-10-08; локальный отчёт `Temp/MotorCityAudit/static-audit.json`): 4 471 файлов, 2 329 asset GUID, 3 232 serialized files, 246 C# файлов (включая тестовые), 0 duplicate GUID, 0 deleted assets, 0 dangling references к удалённым ассетам, 241 unreferenced candidates. JSON корректен, кандидаты уникальны. **Список кандидатов не является разрешением на удаление.** Отчёт не содержит hash HEAD, поэтому конкретный анализируемый коммит нельзя подтвердить по одному JSON.
- [x] Unity Edit Mode audit и скриншот Console **зафиксированы 2026-10-08**: `project-audit.txt` (2026-10-08 20:39:45Z), `Assets/Scenes/Prototype.unity` — единственная build scene, imported assets = 2014. Console: 1 warning про Input Manager deprecation, 1 info о записи отчёта, красных ошибок на предоставленном снимке не видно. Audit обнаружил **2 missing script**, оба в `Assets/Fantastic City Generator/Roads/Prefab/Double-Block-09.prefab`, объектах `Water` и `Water-B`; оригинальный GUID скрипта `296cbc687256a5b47bc5c557f8bdd095`. Оба ссылаются на один GUID; `Generate.prefab` содержит serialized ссылку на `Double-Block-09.prefab` GUID `117dc5da96c6fef48a8f4f0f03a504b9` — **не удалять вслепую**. `Prototype: 0 scripts, 0 lights, 0 renderers` относится к пустой сцене в Edit Mode; runtime Bootstrap создаёт содержимое при Play. Эти findings — зафиксированный исходный baseline, **не исправленные проблемы**.

- [x] Unity **Play Mode** audit зафиксирован пользователем 2026-10-08 (`project-audit.txt`, UTC 20:43:32): загруженная `Prototype` — **3 486 MonoBehaviour, 788 Light, 22 559 Renderer**, без строк `ERROR`/`REVIEW` для загруженной сцены. В prefab-сканировании сохраняются **2 missing script** (`Double-Block-09/Water`, `Water-B`), что соответствует 2 жёлтым предупреждениям Console `The referenced script (Unknown) on this Behaviour is missing!`; причинную связь с самим сканированием считать вероятной, а не полностью доказанной. **Это baseline, а не устранение дефекта.**
- [x] Сквозной Play Mode smoke: меню → выбор управления → город → гараж → пауза → главное меню → Continue → повторный выбор управления и вход в город. Пользователь 2026-10-08 ответил «всё нормально» на просьбу проверить сценарий и новые красные ошибки Console. Проверка отдельных визуальных ролей всех машин и physics smoke остаётся открытой.

### Базовая проверка всех автомобилей (10/10 — подтверждено пользователем)

> **Два вида доказательств:** для трёх машин есть скриншоты и визуальная проверка одного ракурса; для остальных семи пользователь 2026-10-08 подтвердил, что **все 10 машин уже проверены и нормально работают**, но отдельных новых скриншотов нет. `[x]` у машины подтверждает **базовую работоспособность**, а не детальные тесты фар, торможения, изменения цвета/дисков/неона и день/ночь. Не заставлять пользователя повторно проверять все машины.

- [x] Street / Стритер (2/10) — скриншот 2026-10-08: красный кузов с чёрными полосами, тёмное остекление, серебристые диски; явных пропавших частей не видно.
- [x] Beatall / Битл (1/10) — скриншот 2026-10-08: зелёный кузов, тёмное остекление, жёлтые диски, розовый неон; явных отсутствующих материалов не видно.
- [x] Peugeot306 / Пыж 306 (3/10) — скриншот 2026-10-08: синий кузов, тёмные стёкла, золотистые диски, розовый неон; явных пропавших элементов нет.
- [x] ToyotaAE86 / Торо 86 — базовая работа подтверждена пользователем 2026-10-08; отдельного скриншота нет.
- [x] Hybrid — базовая работа подтверждена пользователем 2026-10-08; отдельного скриншота нет.
- [x] Porsche996 — базовая работа подтверждена пользователем 2026-10-08; отдельного скриншота нет.
- [x] AMG GT — базовая работа подтверждена пользователем 2026-10-08; отдельного скриншота нет.
- [x] Camaro — базовая работа подтверждена пользователем 2026-10-08; отдельного скриншота нет.
- [x] DeLorean — базовая работа подтверждена пользователем 2026-10-08; отдельного скриншота нет.
- [x] Bus — базовая работа подтверждена пользователем 2026-10-08; отдельного скриншота нет.
- [x] Фары ночью и стоп-сигналы при торможении — пользователь 2026-10-08 подтвердил «да» на прямой вопрос о выполненной проверке. Подтверждение ручное; дополнительных новых ночных скриншотов нет.
- [x] Цвет кузова, смена дисков и неона — пользователь 2026-10-08 подтвердил «да» на прямой вопрос о выполненной проверке. Не требовать повторять проверку без конкретной причины.
- [ ] Полный визуальный архив день/ночь и обзор остекления со всех сторон (360°) — отдельно не подтверждены; 3 скриншота гаража приложены. Это ограничение полноты **архива доказательств**, не опровержение пользовательской проверки функциональности.

**Проверка Phase 0 (последовательно):**

1. В PowerShell 7 из корня проекта выполните `git status` — не должно быть незавершённого merge или конфликтов. Сохраните короткий hash команды `git rev-parse --short HEAD`. Если есть локальные незакоммиченные изменения, ничего не сбрасывайте; сначала завершите merge/commit.
2. Выполните `pwsh -NoProfile -File Tools/check_cleanup.ps1` и сохраните весь вывод. Это Roslyn/source-тесты, **не** Unity compile.
3. Выполните `py -3 Tools/audit_project.py --output Temp/MotorCityAudit/static-audit.json` и сохраните JSON. Кандидаты без GUID-reference **не разрешены к удалению**.
4. В Unity откройте `Assets/Scenes/Prototype.unity`, дождитесь компиляции, откройте `Motor City → Audit → Write project and loaded scene report`, сохраните отчёт. Зафиксируйте ошибки Console, включая существующие предупреждения, отдельно от новых.
5. Play Mode: главное меню → выбор управления → город → гараж → пауза → главное меню → Continue; переключите все машины. Сделайте для каждой скриншот в гараже, днём и ночью, отдельно отметьте paint/glass/headlights/brake/wheels и поведение руля, газа, тормоза, ручника.
6. Зафиксируйте результаты в журнале выше; проверенный статус `Phase 0` отмечается `[x]` только после выполнения всех применимых подпунктов ниже.

- [x] новый **статический** audit baseline: результаты JSON зафиксированы в журнале; Unity asset/scene report и визуальный baseline остаются отдельными незакрытыми проверками;
- [ ] Console clean **от любых** предупреждений: пользователь ранее подтвердил отсутствие новых красных ошибок и Missing Script после `Double-Block-09`, а 2026-10-08 21:25 UTC — исчезновение известного **Input Manager deprecation warning** после миграции. Таким образом два известных дефекта/предупреждения сняты; для отметки общей Console без любых предупреждений пока нет полного снимка/журнала. Не требовать повторных smoke-тестов всех машин.
- [x] **Prefab/material Git baseline зафиксирован**: [`Docs/Baselines/PrefabMaterialInventory-2026-10-08.json`](Baselines/PrefabMaterialInventory-2026-10-08.json) — 647 prefab, 207 `.mat`, у всех 854 есть `.meta`, сохранены Git SHA asset/.meta, размеры и 14 критических GUID. Источник: `ec98dc91d8fcbbae4ce23d1953aeddcaa1097382`; подробности и правила проверки зависимостей в [`PrefabMaterialBaseline-2026-10-08.md`](Baselines/PrefabMaterialBaseline-2026-10-08.md). Только **Git baseline**: Unity shader/material slots и runtime-состояние требуют отдельного аудита. Ни один игровой ассет не изменён;
- [x] **Unity Audit All Project Materials** — пользователь предоставил 2026-10-08 вывод `Motor City → Diagnostics → Audit All Project Materials`: **Materials scanned: 767, Null shaders: 0, Unsupported shaders: 0**. Это проверка всех материалов, найденных через `AssetDatabase.FindAssets("t:Material")` в Unity, включая импортированные материалы, а не только 207 отдельных Git `.mat`. Аудит не проверяет пустые слоты `Renderer.sharedMaterials` и не доказывает отсутствие shader-проблем в WebGL;
- [x] **Unity Audit Materials In Open Scene** — пользователь предоставил 2026-10-08 результат `Motor City → Diagnostics → Audit Materials In Open Scene` для `Prototype` после запроса на проверку загруженного города: `Missing material slots: 0`, `Unsupported/null-shader materials: 0`. Это подтверждает, что у проверенных Renderer в загруженной сцене нет пустых слотов и неподдерживаемых/нулевых шейдеров. Метод проверки — `Renderer.sharedMaterials` и `material.shader.isSupported`; WebGL shader compatibility, будущие динамические объекты и сравнение материала между разными режимами рендера отдельно не проверялись;
- [ ] архив скриншотов каждой машины день/ночь/гараж: есть 3 снимка гаража (Street/Beatall/Peugeot306); остальные скриншоты не приложены. **Работоспособность всех 10 машин отдельно уже подтверждена**, архивная полнота — отдельный gate;
- [x] Steering / mobile input smoke notes записаны 2026-10-08: пользователь подтвердил, что **уже проверял** оба режима мобильного управления (стрелки и рулевое колесо), газ, тормоз, ручник и настройку расположения кнопок. Новых замечаний в ответе не сообщил; повторного тестирования не требовать.
- [ ] Глубокий driving/physics audit: отдельная проверка `FixedUpdate`, сил/поворота/трения, режима езды и влияния ручника по Phase 10; факт пользовательской проверки органов управления **не закрывает** эту задачу.

### Исправление Missing Script в Double-Block-09 (2026-10-08)

- [x] До изменения подтверждены 2 missing MonoBehaviour на `Double-Block-09/Meshes/Water` и `Water-B`, оба с GUID `296cbc687256a5b47bc5c557f8bdd095`; в сериализации компоненты без настраиваемых данных. `Generate.prefab` ссылается на исходный prefab GUID `117dc5da96c6fef48a8f4f0f03a504b9`, поэтому **сам префаб и объекты не удаляем**.
- [x] Исправлены **только две нерабочие компонентные ссылки и две соответствующие записи MonoBehaviour** в `Assets/Fantastic City Generator/Roads/Prefab/Double-Block-09.prefab` — коммит `4da9a30`. Проверен Git diff: другие строки не менялись. По структурной проверке сохранены 454 GameObject, 408 MeshFilter/MeshRenderer и 25 действующих MonoBehaviour, а GUID отсутствующего скрипта больше не встречается в префабе.
- [x] **Unity audit после исправления префаба**: пользователь предоставил `project-audit.txt` с отметкой UTC `2026-10-08 21:00:08Z`. Импортированных ассетов 2014, build scene `Prototype.unity`, в отчёте **нет** строк `ERROR missing prefab scripts` и ошибок/repeated-component findings по загруженной сцене. Play Mode статистика: 3486 MonoBehaviour, 788 Light, 22559 Renderer (совпадает с baseline до исправления). **Доказано отсутствие Missing Script в сканировании Unity**, а не отсутствие любых предупреждений Console.
- [x] **Завершение Unity verification gate**: пользователь 2026-10-08 ответил «да» на прямой вопрос, что после исправления город, дороги и вода нормально отображаются, нет новых красных ошибок Console и двух прежних предупреждений `The referenced script (Unknown) on this Behaviour is missing!`. Вместе с повторным `project-audit.txt` (2026-10-08 21:00:08Z, нет missing scripts) это **закрывает дефект `Double-Block-09`**. Предупреждение Input Manager deprecation — отдельный открытый вопрос.
- [x] **Input Manager deprecation: анализ зависимостей и подготовка миграции** (2026-10-08): global PlayerSettings + WebGL Mobile/Desktop Release профили имели `activeInputHandler: 2` (Both), хотя `com.unity.inputsystem: 1.20.0` подключён, игровой `MotorCityInput` использует Keyboard/Gamepad/Touchscreen, UI — InputSystemUIInputModule, камера — EnhancedTouch. Найдены 3 legacy-потребителя: `MotorCityInput.touchSupported`, резервная клавиатурная ветка **активно используемого** `PrometeoCarController`, вспомогательная `FreeCamera`. Эти участки переведены на New Input System; глобальный и оба WebGL профиля переключены с `2` на `1`. Добавлен read-only `Tools/check_input_backend.py`, инструкция: [`Docs/InputSystemMigration-2026-10-08.md`](InputSystemMigration-2026-10-08.md). **Это исходники, не подтверждённый Unity результат!**
- [x] **Input System migration — Unity Editor runtime gate** (пользователь подтвердил 2026-10-08 21:25 UTC): после смены backend управление работает; `pwsh -NoProfile -File Tools/check_cleanup.ps1` — PASS (242 C#); повторный `py -3 Tools/check_input_backend.py` — **PASS** (`Motor City input backend source/settings checks passed`, глобальный PlayerSettings и два WebGL профиля `Input System (New)`, нет legacy Input API в `Assets/**/*.cs`); ранее известное **Input Manager deprecation warning исчезло** по подтверждению пользователя. Изначальный FAIL guard (regex и `ShiftAtRuntime`) исправлен (`f03f3d4`, `73010b9`). Это Unity Editor проверка, **не WebGL build**. Не утверждать, что вся Console полностью свободна от иных предупреждений; подробности: [`Docs/InputSystemMigration-2026-10-08.md`](InputSystemMigration-2026-10-08.md).
- [x] **Input System migration — пользовательский WebGL smoke:** на просьбу проверить WebGL Desktop build/браузер, меню и управление одной машиной пользователь 2026-10-08 21:28 UTC ответил: «в webgl всё нормально, уже проверял». Работоспособность WebGL **со слов пользователя подтверждена**; повторный общий smoke без регрессии не нужен. Не приписывать этому ответу конкретные Build Profile и хеш сборки.
- [ ] **Input System migration — формальный WebGL Release gate:** отдельное подтверждение сборок именно `Web - Desktop - Release` и `Web - Mobile - Release`, проверка браузера с клавиатурой и мобильным тачем, версия сборок/Console; из общей фразы «в webgl всё нормально» **не** следует, что оба Release-профиля проверены после смены backend. Этот gate отложен до Phase 12 release matrix, если нет новых признаков неисправности.

## Phase 1 — Release safety

**Ревизия 2026-10-09:** `DEVELOPMENT_BUILD` вызвал 10 предупреждений `UAC0009`, поэтому фактический guard теперь `UNITY_EDITOR || DEBUG`, а не `MOTORCITY_QA`/`DEVELOPMENT_BUILD`. Не возвращать deprecated define. Проверки ниже относятся к уже подтверждённым сценариям, а не к автоматическому доказательству полного исключения всех тестовых API из release.

- [x] Выбран и внедрён QA guard `UNITY_EDITOR || DEBUG` в пяти связанных C# файлах (10 preprocessor guards); пользователь подтвердил исчезновение `UAC0009` в Unity.
- [x] `AdminDebugPanel`, hotkey и pregame `QA RESET SAVE` защищены QA guard; в WebGL Desktop Release пользователь подтвердил отсутствие доступной QA-панели.
- [x] Вызов `ForceNextEditorMock`, mock-поля и mock-сценарии interstitial runtime защищены таким же QA guard; исходники проверены.
- [x] WebGL Desktop Release: пользователь подтвердил успешный запуск, управление и отсутствие QA/Admin.
- [x] WebGL Development: пользователь подтвердил запуск и доступность QA/Admin.
- [ ] Заключительный аудит **всех** `*ForTesting`, mock/reset и косвенных путей вызова: `MotorCitySaveService.ResetProgressForTesting()` дополнительно помещён под `UNITY_EDITOR || DEBUG` (коммит `59ec7e9`); остаётся полный аудит других `*ForTesting`, mock/reset call sites и Unity verification после этого изменения.
- [ ] Отдельная проверка WebGL Mobile Release и полная формальная матрица остаются в Phase 12; не подменять их Desktop-проверкой.

**Инвентаризация тестовых API (2026-10-09):** GitHub source review обнаружил публичные методы `*ForTesting` в `DiscoverySystem`, `DisciplineReputationSystem`, `CityLegendSystem`, `AchievementSystem`, `CollectionProgressionSystem`, `CityRiskSystem`, `VehicleMasterySystem`, `ClubSystem`, `SeasonSystem`, `DriftSpotSystem`, `SpeedTrapSystem`, `CityLiveEventSystem`, `CityContractSystem`, `UndergroundSceneSystem`, `PhotoHuntSystem`, `VehicleHistorySystem`, `StoryMissionSystem`, `DailyAdventureSystem`, `VehiclePositionPersistence`, `GarageUpgradeSystem`, `CareerProgressionSystem`, `MotorCityFrontEndFlow`, `FirstSessionOnboardingSystem` и `VehicleRosterSystem`. Их определение само по себе **не доказывает**, что игрок может вызвать их через интерфейс, но и UI-guard не удаляет их из release assembly. Перед массовой изоляцией необходим source/call-site аудит каждого метода: часть тестовых функций может иметь runtime consumers; только затем применять guards/перенос в debug partial и проверять компиляцию Editor/Release/Development. `ResetProgressForTesting` в `MotorCitySaveService` уже изолирован отдельно. Пользователь повторно сообщил «webgl я проверял уже, всё нормально» — **[x] общий WebGL smoke**, без вывода о каждой конкретной пересборке/профиле после последнего коммита.

**Критерий закрытия Phase 1:** аудит тестовых путей завершён, все QA entrypoints корректно изолированы; соответствующие source/Unity smoke подтверждены.

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
