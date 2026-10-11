# Motor City — UX-0 Source Audit and UI Data Contract

**Дата:** 2026-10-11  
**Область:** `Docs/PlayerExperienceRoadmap-2026-10-11.md`, этап UX-0  
**Метод:** чтение действующих C# источников ветки `main`; **Unity Editor/Play Mode/WebGL не запускались**.  
**Цель:** обеспечить первый UI-инкремент «Мой Motor City» достоверными данными без побочных эффектов.

## 1. Проверенные точки входа и жизненный цикл

- Главный экран: `Assets/Scripts/UI/MotorCityFrontEndFlow.cs` (partial `MotorCityFrontEndModalUi.cs`). `BuildUi()` создаёт отдельные `mainRoot`, `aboutRoot`, `settingsRoot`, `controlChoiceRoot`, `introRoot` и `loadingRoot`. `BuildMainMenu()` строит кнопки «Продолжить / Начать», «Об игре», «Настройки». `ShowMainMenu()` устанавливает `Time.timeScale = 0`, паузу аудио, блокировку вождения `FrontEnd`; переключает экраны. `ShowMainMenuFromGarage()` возвращает в существующую сессию, а `ContinuePrimaryAction()` по возврату повторно показывает выбор управления. **Нельзя** обходить этот путь новой кнопкой.
- Front-end создаёт canvas с sorting order 5000 и масштабированием от 1920×1080; `CreateFrontEndModalWindow()` уже создаёт единообразное окно. Новый раздел главного меню должен быть отдельным front-end screen/partial с **одним владельцем** RectTransform, закрываться обратно в `ShowMainMenu()` и поддерживать длинные надписи/мобильный масштаб.
- HUD: `Assets/Scripts/UI/PrototypeHud.cs` и partial views. `BuildUi()` создаёт pause, navigator, club, store, garage и result overlays. `HasBlockingModalUi()` учитывает `pauseMenuOpen`, `navigatorMenuOpen`, `clubOpen`, `storeOpen`, гараж и окно результатов; `RefreshDrivingEnabledForUi()` запрещает управление машиной и сообщает платформе паузу игрового UI. Новый городской экран должен войти в **этот единый контракт**, иначе возникнут двойные модалки/застрявший ввод.
- `Assets/Scripts/UI/PauseMenuView.cs` управляет `Time.timeScale`, `AudioListener.pause`, `MotorCityMusicRuntime` и виртуальным вводом. Новый экран из паузы должен либо быть дочерним экраном существующей паузы, либо иметь явно согласованный переход; нельзя создавать параллельного владельца паузы.
- `Assets/Scripts/UI/ClubView.cs` уже рисует окно автоклуба и работает с `ClubSystem`. Новая «подробная страница клуба» должна **переиспользовать данные и при возможности view**, а не создавать второй неконтролируемый overlay.
- Навигация: `NavigatorView.cs`: `NavigatorDestinationCount()` = `7 + professions.StartCount`, `TryResolveNavigatorDestination()` и `NavigatorMarkerId()` приватны внутри partial `PrototypeHud`. `ToggleNavigatorMenu()` уже отказывается открываться при `activityManager.IsBusy`. Для UX-2 нужна **узкая внутренняя функция** «поставить маршрут по существующему ID», с теми же проверками. Не добавлять вымышленные карты/позиции и не перетирать активные чекпоинты.
- Front-end видит `wallet`, `reputation`, `onboarding` и `hud`. **Не все игровые системы готовы до создания gameplay**. Раздел до первого входа должен честно показывать доступные данные/«начните игру», а не искать компоненты в сцене без проверки жизненного цикла.

## 2. Матрица source-of-truth для экранов

Столбец «доступ сейчас» — существующий безопасный read-only API или выводимая строка. «Не хватает» означает задачу UX-1…UX-5: добавить **только getter/snapshot**, а не читать private-ключи сохранений из UI.

| Домен / будущий экран | Файл-источник | Уже доступно (read-only) | Что уточнить или добавить | Обновление / ограничение |
| --- | --- | --- | --- | --- |
| КР/REP / главная | `PlayerWallet.cs`, `PlayerReputation.cs` | `Credits`, `Reputation`, `Level` | Нет | Читать при открытии/событии; `CreditsEarned/Spent` — не полная история всех выплат |
| Главная цель | `AdventureDirector.cs`, `MissionDefinition.cs` | `CurrentMission`, `ObjectiveLine`, `HasMission` | Snapshot следующего рубежа при необходимости | `RefreshMission()` уже выбирает приоритетную цель; не выбирать свою конкурирующую HUD-цель |
| Обучение | `FirstSessionOnboardingSystem.cs` | `IsComplete`, `CurrentStep`, `CurrentStepNumber`, `StepCount`, `ObjectiveLine` | Модель заблокированных разделов | До завершения ограничена вторичная прогрессия |
| Сюжет / журнал | `StoryMissionSystem.cs` | `IsComplete`, `MissionCount`, `CurrentMissionNumber`, `RequiredActivityId`, `CurrentCharacterName`, `CurrentMissionTitle`, `CurrentCharacterLine`, `CurrentCreditsReward`, `CurrentReputationReward`, `CurrentPixieXpReward`, `ObjectiveLine` | Числовой `current/target` пока хранится private; добавить read-only getter при реализации | Завершение подписано на события активности |
| Сезон / журнал | `SeasonSystem.cs` | `IsComplete`, `MissionCount`, `CurrentMissionNumber`, `CurrentMissionProgress`, `CurrentMissionTarget`, `CurrentMissionTitle`, rewards, `DaysRemaining` | Явный статус периода и блокировки | Время зависит от платформенного времени; не жестко задавать локальную дату |
| Ежедневные / журнал | `DailyAdventureSystem.cs` | `CompletedTasks`, `CompletedDays`, `DayCompleted`, `ObjectiveLine` | Чтение всех трёх задач (тип, current/target, complete) без выдачи наград | Есть ротация дня; обновлять при открытии/смене дня |
| Карьера / журнал | `CareerProgressionSystem.cs` | `Stage`, `HudLine`, `StatusText` | Read-only счётчики четырёх дисциплин и цели текущей ступени | Reward выдаётся владельцем карьеры |
| Контракты / журнал | `CityContractSystem.cs` | `HudLine`, `AdminLine` | Структурированная карточка: cycle, requirements, progress, rewards — поля private | Контракт отслеживается автоматически; отдельное «взять контракт» не придумывать |
| Городские события / журнал | `CityLiveEventSystem.cs` | `HudLine`, `AdminLine` | Snapshot таймера, целей и cooldown без разбора локализованной строки | Временное событие, возможно истекло |
| Легенды / журнал | `CityLegendSystem.cs` | `AllLegendsDefeated`, `HudLine`, `AdminLine` | Структурированная карточка unlock/requirements/current/target/reward | «Легенды» — прогресс, не физические NPC-гонки |
| Ночной клуб / журнал | `UndergroundSceneSystem.cs` | `StreetCred`, `IsActive`, `IsNearMeeting`, `IsCountingDown`, `TryGetNextTarget()`, `StatusText` | Текущий этап, требования доверия, награды; private definition | Маршрут только в ночное время/при открытии |
| Автоклуб | `ClubSystem.cs` | `HasClub`, `JoinedClubIndex`, `CurrentClubName`, `CurrentClubEmblem`, `WeeklyContribution`, `WeeklyTarget`, `WeeklyRewardAvailable`, `WeeklyLine`, browse-строки | Явный статус «награда уже получена» и список действий клуба при необходимости | Недельная награда глобальная; числа из действующего баланса/remote config |
| Инспектор | `CityRiskSystem.cs` | `Attention`, `RiskLevel`, `PursuitActive`, `HudLine` | Read-only причины роста/падения как описания действующей логики | `PursuitActive` — состояние наблюдения, не подтверждение физической погони |
| Пикси (летающий спутник) | `PixieCompanionSystem.cs` | `Level`, `Xp`, `DailyObjectiveLine`, `MoodName` | Read-only next level XP, daily current/target, unlocks | Не выдумывать способности после реального порога |
| Гараж / машины | `VehicleRosterSystem.cs`, `VehicleMasterySystem.cs`, `VehicleHistorySystem.cs`, `VehicleSpecializationSystem.cs` | `VehicleCount`, `SelectedId`, `SelectedName`, `SelectedRequiredRep`, `GetVehicleId()`, `GetOwnedVehicleCount()`, `CurrentLevel`, `CurrentXp`, `NextLevelXp`, `GetLevelForVehicle()`, история и `GarageLine` | Read-only данные по произвольному авто без переключения выбранной машины, если нужны подробные карточки | `VehicleChanged`/`VehicleVisualReady` доступны; смену машины не выполнять лишь ради просмотра |
| Дисциплины | `DisciplineReputationSystem.cs` | `RacingReputation`/`Level`, `DriftReputation`/`Level`, `DeliveryReputation`/`Level`, `GetReputation()` и `GetLevel()` | Следующий порог/доступность premium/elite в структурированном виде | Не смешивать уровни водителя и мастерство машины |
| Достижения | `AchievementSystem.cs` | `UnlockedCount`, `StatusText` | Read-only по каждому достижению: условие, статус, однократная награда | Нельзя вызывать testing unlock из UI |
| Коллекция | `CollectionProgressionSystem.cs` | `Rating`, `GarageLine` | Пороги и история полученных ступеней как read-only | Reward не должен повторно начисляться при просмотре |
| Профессии | `CityProfessionSystem.cs` | `StartCount`, `TotalCompleted`, `ProfessionLevel`, `GetStartPoint()`, `TryGetStartForActivity()`, `GetStartActivityId()`, `CurrentLabel` | Карточки профессий: требования, актуальные награды и цель | Вариант действия зависит от профессии и текущего tutorial |
| Кольцо / спринт / доставка | `CircuitRaceActivity.cs`, `StreetSprintActivity.cs`, `DeliveryActivity.cs` | `IsActive`, `IsNearStart`, `ElapsedSeconds`, `BestTimeSeconds`, `CheckpointIndex/Count`, `TryGetNextTarget()` | Structured thresholds для медалей и текущего режима | Старт сохраняет ограничения `ActivityManager.RequestStart` |
| Дрифт | `DriftChallenge.cs`, `DriftSpotSystem.cs` | `TimeRemaining`, `CurrentScore`, `TargetScore`, `SpotCount`, `GetSpotPosition/Name` | Цели всех медалей и рекорды по зоне (частично private) | Экран лишь описывает условия, не запускает испытание напрямую |
| Радары | `SpeedTrapSystem.cs` | `TrapCount`, `GetTrapPosition()`, `GetTrapName()`, `GetTrapGoalText()`, `GetBestSpeed()` | Определить полученные медали и одноразовые награды | Проезд обрабатывается текущей системой |
| Исследование | `DiscoverySystem.cs` | `DiscoveryCount`, `IsFound()`, `GetDiscoveryName/Position()` | Нет для первых карточек | Не выдавать награду за повторный просмотр |
| Фотоальбом | `PhotoHuntSystem.cs` | `TotalCaptured`, `AlbumLine`, `StatusText` | Read-only 17 слотов/категории/статусы | Нынешнее коллекционное фото — не обещание скачиваемого PNG |
| Результат активности | `ActivityManager.cs`, `ActivityResultView.cs` | `HasResult`, `ResultSequence`, `ResultTitle`, `ResultHeadline`, `ResultDetails`, `ResultRewardCredits`, `ResultReputationReward`, `ResultMasteryXp`, `ResultSecondaryProgress`, `ResultNextGoal`, `ResultSuccess`, `ResultIsNewRecord` | Протокол фактических изменений других систем (см. п. 4) | Read-only UI, сам результат не начисляет награды |

**Подтверждённое ограничение:** многие системы дают локализованную `HudLine`/`StatusText`, но *не* структурированный read-only прогресс. **Не парсить строки**, не читать `MotorCitySaveService.GetInt` напрямую в новых экранах для обхода этого ограничения; добавить небольшие публичные getter/snapshot в конкретные источники, когда понадобится.

## 3. Карта действий и навигации

| Действие игрока | Владелец | Правило |
| --- | --- | --- |
| Открыть «Мой Motor City» в главном меню | `MotorCityFrontEndFlow` | Только переключение экранов `mainRoot` ↔ новый screen; не трогать gameplay/`Time.timeScale` самостоятельно |
| Открыть центр во время езды | `PrototypeHud` + pause/modal contract | Не допускать одновременный показ с результатом, гаражом, навигатором, club/store; вход только через существующую контролируемую точку |
| Просмотреть сюжет/задания | `AdventureDirector` + source systems | Один главный objective; список остальных задач без второго HUD |
| Построить маршрут | `NavigatorView` | Использовать существующие назначения и проверки `ActivityManager.IsBusy`; неподдерживаемые цели — без фиктивного route |
| Открыть клуб / гараж | Существующие partial views | Для интерактивных действий (вступить/сменить машину) использовать действующие окна, новые страницы только показывают status |
| Результат / повторить | `ActivityManager` + `ActivityResultView` | `Retry/Continue` остаются за существующей системой; вкладки не переиздают награды |

## 4. Важный порядок событий в ActivityManager.ShowResult()

Проверено по `Assets/Scripts/Gameplay/ActivityManager.cs`:

1. Сохраняются **before**: mastery XP, completed daily tasks, сезонный номер/прогресс, клубный вклад.
2. Вызывается `End(activityId)`; создаётся новый `ResultSequence` и заполняются основные поля.
3. Рассчитывается `ResultReputationReward`, начисляется REP.
4. Вызывается **`ActivityResultShown(activityId, success)` ДО `ActivityCompleted(activityId)`**.
5. Только при `success` вызывается `ActivityCompleted`; подписанные системы меняют сюжет, клуб, mastery, achievements и т. п.
6. Текущий `BuildResultSecondaryProgress` добавляет **текстом** изменившиеся daily/season/club, а также отдельный mastery XP.
7. `ResultNextGoal` выбирается через `ResultNextGoalResolver`.

**Следствие для UX-5:** нельзя считать `ActivityResultShown` окончательным состоянием всех вторичных прогрессов. Для детального отчёта нужен синхронный безопасный snapshot **после завершения всех подписчиков**, или явная «result finalized» граница, и before-снимок до выплаты. Доработать прежде, чем рисовать «+90 XP», «+1 клуб» и т. п.; не выводить теоретические награды вместо фактических. Определить handling failed/retry/new record/max levels без ложных изменений.

## 5. Выбранный UI data contract

**Рабочее решение для UX-1…UX-5, пока без изменения C#**:

1. Один read-only `PlayerExperienceSnapshot` или небольшие типизированные снимки по секциям, которые строятся из полученных ссылок на игровые системы. Цель — **адаптер UI, не новая gameplay-система**. Достаточно существующего `AdventureDirector.ObjectiveLine` для заголовка первой версии.
2. Null-safe: до создания gameplay и при отсутствии отдельных сервисов показывать честные empty/locked states, не `FindAnyObjectByType()` на каждом кадре.
3. Обновлять снимки **при открытии окна**, возвращении в раздел и при известных событиях (смена авто, завершение активности, смена языка/дня). Не опрашивать все системы каждый `Update()`.
4. UI не вызывает `AddCredits`, `AddReputation`, `CompleteCurrent`, `ResetForTesting`, `JoinBrowseClub` или методы старта активности для простого просмотра. Действия доступны только через существующие UI entrypoints с правилами блокировки.
5. Внутри `PrototypeHud` — одна modal flag, одна корневая панель и передача блокировки в `HasBlockingModalUi()`. На главном меню — отдельный `CreateScreen`, скрываемый `ShowMainMenu()`. Ответственный за геометрию — только владелец view, не `HudVisualPolish`.
6. Текст переводить через `MotorCityLocalization`. Данные наград — из живых настроек/систем и remote config; не переносить базовые цифры справочника в UI константами.
7. Предпочесть небольшие partial-файлы для UI, чтобы не расширять дальше `PrototypeHud.cs` и `MotorCityFrontEndFlow.cs` тысячами строк. Все новые Unity C# файлы требуют корректных `.meta` в проекте (не генерировать случайные ссылки на сценовые компоненты).
8. По умолчанию экран **не хранит состояние прогресса**; оно уже сохранено gameplay. Можно сохранять только чисто визуальный выбор вкладки, если это понадобится отдельно.

## 6. UX flow / smoke в Unity Editor

**Новый игрок:** запустил игру → главное меню → «Мой Motor City» → видит вводную/состояние «начни игру», если gameplay не готов → «Назад» → обычная интро/выбор управления/обучение. Недоступные дисциплины отображены заблокированными.

**Уже играющий:** пауза/город → центр → главный objective, прогресс машины, разделы; «Назад» возвращает к тому же контексту без изменения `Time.timeScale`, звука и блокировок; основной поток `Continue` по-прежнему вызывает выбор управления.

**Активная гонка:** центр либо недоступен, либо read-only; не заменяет чекпоинты и маршрут. **Результат:** центр не перекрывает Retry/Continue; повторное открытие не выдаёт награды.

**Узкий экран:** панель прокручивается, back/close доступен, текст RU/EN помещается, touch controls остаются работоспособными после закрытия.

**Обязательная матрица ручных проверок каждой UI-итерации:** чистый и существующий save; Pause → Main Menu → Continue → выбор управления; back/cancel и тач; гараж/навигатор/другие модалки; результат успех/провал/Retry; Unity Console. **WebGL не запускаем до UX-7 по решению пользователя.**

## 7. Приоритет первого изменения (UX-1)

Минимально безопасный первый UI-инкремент:

- Новый пункт `МОЙ MOTOR CITY` в главном меню, экран-оболочка с существующим `CreateFrontEndModalWindow()` и корректным `Back`, без прямых записей в SaveService.
- Показ **только подтверждённых доступных значений** (например `wallet.Credits`, `reputation.Reputation`, `onboarding.ObjectiveLine` и, когда gameplay создан, `adventureDirector.ObjectiveLine` через контролируемый read-only доступ).
- Пока подробных API нет, остальные секции — честные заголовки/условия, а не случайные захардкоженные числа.
- Сначала ручной Unity Editor smoke; только после подтверждения расширять каталог UX-2. **Никакой сборки WebGL в ходе текущих итераций**.

## 8. CI / релизный режим, установленный на время разработки UX

Коммит `e0b09e2989a7e169ef15cf7f1a1f7152db0b0ec1`: для `motor-city-source-gates.yml` оставлен только `workflow_dispatch`; обычный ручной запуск делает проверки без WebGL. Опции `build_webgl_release` и `build_legacy_webgl` по умолчанию `false`. В `motor-city-pages.yml` удалён `workflow_run`: публикация только вручную с явным ID успешного финального Release-run. Текущие GitHub Pages бинарники не обновляются при UI-коммитах.

Если исходно запущенная ранее Action уже выполняется, удаление триггера не отменяет её задним числом. На новой версии workflow push не должен создавать run.

## 9. Итог UX-0

Сделан **source-only аудит**, документирован источник истины для каждого раздела, события результатов, старый путь меню и единственный владелец UI. Не обнаружено необходимости в новой системе сохранений или новых валютах.

**Остаётся проверить при реализации:** фактический контракт новых getter/snapshot, настройки длинного текста и safe area, Play Mode и браузер. Это задачи UX-1…UX-7, а не доказательство готового интерфейса.

**Следующая работа:** UX-1 — каркас «Мой Motor City»; строго без WebGL до финального gate.

## Уточнение канона (2026-10-11)

[Турбо — отдельный навигационный помощник, Пикси — летающий спутник](../CharacterCanon-Turbo-Pixie.md). В UX-0 таблица «Турбо» означала летающего спутника по устаревшему имени класса; техническое имя исправлено на `PixieCompanionSystem`, поля наград — на `CurrentPixieXpReward`. **`MotorCity.Turbo.*` в SaveService/контроллере спутника — неизменяемые legacy-ключи**, их сохранение проверяется отдельно. Не считать переименование доказательством прохождения Unity Editor Play Mode.
