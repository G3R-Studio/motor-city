# Motor City — технический roadmap и передача контекста

**Актуально на 19.11.2026.** Репозиторий `G3R-Studio/motor-city`, ветка `main`. Снимок перед созданием документа: `edf88981aa87aecafb488b474a25d510583ec107`. Это единственная актуальная очередь технической работы; исторические измерения — в [Audit-1.1.md](Audit-1.1.md). Статусы: [x] реализовано или исследовано в указанном объёме; **реализация** и **подтверждённый прирост** — разные вещи. По запросу владельца чек-лист ручного QA удалён; исторические результаты сохранены.

## 0. Контекст для нового чата

Motor City — открытый городской автомобильный симулятор G3R Studio на Unity 6000.6.1f1, URP 17.6.0, WebGL 2 / Playgama. Единственная включённая сцена: `Assets/Scenes/Prototype.unity`. Основной авторский город — Fantastic City Generator, исходная рабочая сцена FCG_Workbench, запечённый префаб `Assets/Resources/MotorCity/Environment/CityVisual.prefab`. Локальный проект владельца: `D:\GitHub\motor-city`. Версия 1.1 доступна в Sandbox: https://playgama.ai/play/nkvqxlch7s76z6 ; это не подтверждение полной публикации.

Владелец тестирует Unity и Windows-браузер локально. Ассистент читает приватный GitHub, проверяет актуальный HEAD, вносит малые согласованные изменения в main, сообщает SHA и точные действия. Не утверждать, что Unity запускалась дистанционно. После каждого принятого эксперимента обновлять этот roadmap: дата, SHA, методика, до/после, результат, известные ошибки, следующий шаг. По решению владельца от 19.11.2026 промежуточные локальные тесты не обязательны; итоговая проверка всех изменений перед релизом обязательна. Различать реализацию и подтверждённый результат.

**Актуальная точка остановки (19.11.2026, серия предрелизных реализаций):** P0–P4 исследованы в согласованном объёме; P5 district streaming + отдельный HLOD candidate, P6 CPU/physics/UI/GC и опциональный traffic pool реализованы. Добавлены adaptive render scale (opt-in), визуальный LOD колёс, companion animator culling, отдельные release candidate команды. Владелец попросил убрать пункты ручных проверок из roadmap и выполнить единую проверку самостоятельно перед релизом. Ни компиляция, ни результат новых вариантов сборки пока не подтверждены. Несовместимые или не реализованные направления перечислены отдельным текстом, не считать их выполненными.

## 1. Цели и исходная база

- Устойчивый FPS и время кадра на слабых ПК и мобильных браузерах без принудительного лимита FPS.
- Снизить пиковую память (в том числе при загрузке/создании города), draw calls, SetPass, число обработанных треугольников и overdraw, не ухудшая заметно вид и физику.
- Сохранить достигнутое ускорение первого меню, Bridge, сохранения и игровые механики. Не пытаться оценивать мобильную пригодность только по размеру файла.
- Новые автомобили, районы, сюжет, мультиплеер не входят в данный технический этап.

### Подтверждённые результаты

| Метрика | До разделения | После |
|---|---:|---:|
| Локальная контрольная data + wasm | 45,08 МБ | 22,11 МБ |
| Локальный data.br | 38,78 МБ | 15,76 МБ |
| Отдельный городской LZ4 bundle | — | 41,07 МБ |
| Sandbox data.br (Network) | 39,19 МБ / 36,38 с | 15,759 МБ / 4,06 с |
| Sandbox wasm | 6,30 МБ / 14,52 с | 6,348 МБ / 2,16 с |
| Sandbox bundle | — | 41,074 МБ / 7,55 с |
| Playgama QA initial download | ранее 43 МБ | 21 МБ |
| QA Total loading time | — | 3,02 с |

МБ — десятичные, если не указано МиБ. 51% экономии стартовых data+wasm; фактические времена зависят от сети и кэша, не считать 9-кратный эффект причинно доказанным. QA — отдельный тест, не полное время загрузки на всех устройствах. Общая загрузка с сырым LZ4 больше исходной; музыку считать отдельно. QA больше не показывает heavy initial load, но остаётся условное предупреждение про риск мобильных сбоев и низкий playtime. Мобильный QA, кэш, память, межбраузерная стабильность и статистика после обновления не подтверждены.

Исторические исходные цели 1.1 (предварительные, не требования платформы): загрузка 10–12 с, готовность 95%+, отказы <50%, среднее время игры 8–10 минут. Старые 20,3 с, 66,7%, 88,9%, 4:47 и архив 103,4 МБ — предварительная статистика 1.0; не использовать как свежие данные.

## 2. Что сделано и что не повторять

- [x] Brotli для основного WebGL, Disk Size with LTO, DXT Desktop, Development Build off.
- [x] Выборочные уменьшения размеров текстур FCG/UI/intro/sky; исходники и GUID сохранились. Атласы уже используются.
- [x] Музыка в StreamingAssets, в WebGL играет через HTMLAudioElement. Не перекладывать обратно в Resources.
- [x] Разделение города и меню: 43 корневых ресурса, CityVisual, FCGMaterials, FCGTrafficCars, TrafficVehicleCatalog. На время специальной сборки переносятся из Resources, затем восстанавливаются с проверкой GUID. Общие зависимости, нужные другим объектам, остаются в основном пакете.
- [x] `MotorCityCityBundle.cs`: скачивание после показа меню, versioned bundle/hash, UnityWebRequestAssetBundle, прогресс, повтор, резервный Resources-путь в обычной сборке. `CityAssetRuntimeInstaller.cs`, `MotorCityFrontEndFlow.cs`, FCG TrafficSystem и TowTruckJobSystem адаптированы. Локальный браузер и Sandbox проверены владельцем. Фактический повторный запуск из кэша ещё проверить.
- [x] SRP Batcher, дистанционное отсечение мелких деталей и припаркованных автомобилей, fog/far clip, настройки плотности трафика.
- [x] Парковка: историческое контролируемое сравнение Medium ~48,26 → 69,42 FPS, не универсальная оценка. Не дублировать этот механизм.
- [x] Low mesh compression пилот на Double-Block-06/10/13: сериализованный размер ~5,87 → 2,40 МиБ, общий выход ~98,55 → 98,14 МиБ. Слабый эффект после Brotli; не распространять на все меши. Более ранний BB-001–012 пилот отменён.
- [x] Восстановление LZ4 после провала managed Gzip (RuntimeError: unreachable после скачивания; точная причина не доказана). Коммит `edf8898` удалил экспериментальный режим. Не восстанавливать его и не увеличивать heap вслепую.
- [x] HTTP Brotli sidecar локально: 41,073,250 → 28,423,457 байт, владелец подтвердил работу локального теста. Playgama отдаёт сырой bundle; дополнительные заголовки не подтверждены. Не публиковать .bundle.br вместо .bundle без подтверждения.
- [x] Введены команды Build District Release Candidate и Build Split Release Candidate (Fallback), существующий обычный Build не равнозначен им (`4cdd184`).

Ключевые SHA: `144a917` mesh pilot; `ce7e69b` диагностика; `945c458` расширение корней; `ffb0c01` split loader; `f8dd6fc` HTTP sidecar; `7f777cd` неудачный Gzip; `edf8898` откат. Сборка, проверенная в Sandbox: `Builds/CityBundleExperiment/20261119-044000-262/SplitCityTest` (локальная, не в Git). Report: Library/MotorCitySplitCityTest.txt. Состояние main после отката соответствует рабочему загрузчику, повторная сборка main отдельно не подтверждена.

## 3. Техническая карта рендера

| Файл/компонент | Назначение |
|---|---|
| Assets/Settings/MotorCityURP.asset | Базовый URP: HDR on, MSAA 2x, scale .92, дополнительные lights, soft shadows, 2 каскада, SRP Batcher on, GPU Resident Drawer off |
| Assets/Settings/MotorCityRenderer.asset | Renderer, SSAO feature active в asset; фактические проходы проверить |
| Assets/Scripts/Platform/MotorCityQualityRuntime.cs | Runtime Low/Medium/High: scale .60/.72/.85, MSAA 1/1/2, дистанция теней 0/0/65, каскады 1/1/2, shadow atlas 256/256/2048, LOD bias .50/.70/.85, maxLOD 1/1/0; traffic budget 10/18/24, radius 105/125/140; FPS cap отсутствует |
| World/MotorCityVisibilityRuntime.cs | Fog, дальность, подключение отсечения/HLOD |
| World/CityDistanceDetailCuller.cs | Ограниченные бюджеты сканирования, screen-space + distance culling, защищённые объекты |
| World/CityParkedVehicleCullingExperiment.cs | Припаркованные машины, активен для всех пресетов |
| World/CityFoliageCullingExperiment.cs / CityDistantTreeCullingExperiment.cs | Существующая оптимизация растительности, проверять активность по пресету |
| World/CityHlodRuntime.cs, CityHlodData.cs | Опциональный HLOD, данных HLOD сейчас нет в Environment; старый baker удалён |
| World/FcgRuntimeGlassMaterialFactory.cs | Материалы окон/эмиссия, общие ссылки; не ломать день/ночь |
| World/DayNightCycleController.cs, PlayerHeadlights.cs | Свет, дневной/ночной цикл и фары |
| World/TrafficQualityAdapter.cs | Бюджеты трафика |
| Assets/Editor/FantasticCityGeneratorRuntimeBuilder.cs | Авторский pipeline города, сохранять соответствие workbench |
| Assets/Scripts/Diagnostics/MotorCityWebRenderDiagnostics.cs | Существующие браузерные диагностики; проверить доступные метрики, не дублировать |

World/ — относительно Assets/Scripts/. Базовые URP настройки не равны действующим runtime; Editor Play Mode применяет runtime preset к временной копии URP asset (см. P4); исходный asset не меняется. Release player остаётся итоговым критерием.

## 4. Очередь оптимизации рендера

### Сводный статус на 19.11.2026

Отметка [x] означает выполнение конкретной задачи или завершение эксперимента, а не принятие экспериментальной оптимизации в релиз. Подробные цифры и ограничения сохранены ниже в журнале.

- [x] Визуальное оформление: адаптивное главное меню, резкость текста, отступы glow в настройках, удаление лишних разделителей настроек/паузы. Владелец завершил визуальный этап. Коммиты dd81bea, 0222f2f, 82db998, 944853d.
- [x] P0: Render Audit (d3ba106, fix 1cc31e9, результат 58c5eb6); Editor baseline (6f99d53, f2b71fe), WebGL baseline (f1b00d5, fix 91f56a1, результат 09b4040). Editor 256.612 FPS, WebGL 78.705 FPS (Medium, 1512×911, RTX2080, p95 16 ms, p99 18 ms, 383391981 B peak Unity memory). Разрешения/маршруты различались. GPU time/Draw Calls/Batches не подтверждены; полная диагностика открыта.
- [x] P1: Mesh Readability Audit (6cefb29, результат 4486d99); 94 FBX, 82 HOLD, 12 REVIEW без прямого использования в CityVisual. Декор не меняли. Runtime CPU-copy pilot (1be168b, результат 14836f0): 10 мешей, 71.286 FPS, peak Unity memory 378345709 B; per-mesh оценка не изменилась. Не доказана экономия, нет выигрыша FPS. Не включать автоматически.
- [x] P2: native Mesh LOD, первые 10 моделей/16 renderer (b6cbb01, 3b05aa3, fix fcf8239, отчёты 1f0f44f, ba6975d): OFF 250.087 → ON 261.103 FPS, но один A/B и −0.7% triangles. Расширение (c584a97, результат 8053205): 28/30 новых моделей, 114 renderer; OFF 248.766 → ON 247.842 FPS, triangles не снизились. Не внедрять; native LOD selection не подтверждён.
- [x] P3: Lamp Instancing Pilot (445d403, результат 47e2b44), 489 renderer; OFF 251.478 → ON 248.273 FPS; SetPass 181.704 → 199.467. Реальные instanced draws не подтверждены. Не внедрять.
- [x] P4 — закрыт в согласованном объёме:
  - [x] P4.1 SSAO A/B и реализация по пресетам; WebGL checkpoint открыт.
  - [x] P4.2 HDR A/B — без выигрыша, rollout отклонён.
  - [x] P4.3 Дополнительные lights — закрыто по решению владельца, без A/B; Forward+/Forward не измерялся, отдельный пилот отложен.
  - [x] P4.4 Аудит шейдеров и материалов — Simple Lit A/B завершён, rollout отклонён.
  - [x] P4.5 Прозрачность и overdraw — закрыто по решению владельца, без отдельного A/B.
- [x] P5.1 — пространственный аудит (21690 renderer, 103 ячейки 128 м).
- [x] P5.2 — visual cells Low/Medium реализованы (`1ea0642`), проверка отложена.
- [x] P5.3 — Double-Block district mesh streaming и отдельная WebGL build-команда реализованы (`4618860`, `84f20e4`, `7af4f47`, `e610296`), проверка отложена.
- [x] P5.4 — HLOD builder + интеграция отдельного HLOD split candidate, самостоятельные упрощённые index buffers с fallback (`0e22edd`, `6f0e119`, `63387b1`, `cac25aa`).
- [x] P6.1 — CPU traffic и detail culling (`4789cbf`, `731264c`, `a515125`), реализация завершена.
- [x] P6.2 — road RaycastNonAlloc с fallback (`8885f52`), полный fallback насыщенного obstacle buffer (`e88468c`); частота физики сохранена.
- [x] P6.3 — кеш HUD upgrades/локализации и idle UI transforms (`728231a`, `175d582`).
- [x] P6.4 — повторное использование road hits и массива waypoints, лишние UI format/SetActive убраны (`8885f52`, `e88468c`, `728231a`). GC и FPS не измерены.
- [x] Сборочный preflight временно переносит две локальные папки с standalone Mesh pilot assets из Resources и восстанавливает при завершении (`c3710e5`); локальную проверку восстановления GUID провести в финальном QA.

**Рабочий процесс:** по последнему решению владельца промежуточные ручные тесты отменены. Реализации фиксировать в main, итоговые компиляцию, Play Mode, WebGL, A/B по необходимости и Sandbox проверяет владелец перед релизом. Пункты, требующие данных локального профайлера, или неподтверждённые рискованные оптимизации не отмечать как внедрённые.


### P0 — аудит и контрольная точка (базовые проверки выполнены)
- [x] Проверить добавленный Tools > Motor City > Audit > Render Audit (название предложено), вывод Library/MotorCityRenderAudit.txt; **только чтение**, никаких SaveAssets, перемещений, правок импорта.

**Результат P0 asset inventory, 19.11.2026:** отчёт COMPLETE, WebGL, 245 roots / 862 paths. 193 Texture2D (66 101 910 editorRuntimeBytes, все readable=False); 1104 Mesh (168 258 072 editorRuntimeBytes, 791 666 уникальных треугольников), 966 readable (151 012 904 байт суммарной оценки этих мешей, не потенциальная экономия). CityVisual: 21 582 renderer, 8 504 collider, 3 LODGroup, 777 Light; это авторские компоненты, не активные одновременно. Группы mesh/material/submesh: корпус StreetLight-01 489 экземпляров (2 submeshes), Flowerbed-07 394 с Atlas-1, части светофоров до 900. Светофоры динамические — не первый кандидат. Тяжёлые меши: garage.fbx ~1.83 MB (readable false), Border-Block-05 Trees ~1.78 MB, BR-003 ~1.55 MB, EC-009 ~1.53 MB. Atlas-1 2048 DXT1 ~5.62 MB — оставить пока. Все цифры — редакторская оценка, не замеры WebGL GPU/heap/draw calls. Инструмент исправлен в 1cc31e9 (TryGetComponent). Отчёт не даёт доказательства безопасности отключения Mesh Read/Write. Editor и WebGL baseline затем выполнены, см. журнал. Текстурный Read/Write уже отключён; не повторять.

### P1 — память и текстуры (Read/Write исследован, rollout отложен)
- [x] Проверить Read/Write у Texture2D: все 193 текстуры в Render Audit уже non-readable. Не повторять массовое отключение.

### P1a — шрифты (низкий приоритет)
- [x] Проверить пять TTF в Assets/Resources/MotorCity/Fonts: RobotoCondensed Regular/Bold (1,019,060 байт суммарно, используются MotorCityTypography), Roboto Regular и Ubuntu Regular/Bold (1,198,596 байт суммарно, кандидаты). GitHub search не обнаружил ссылок на последние три по имени/GUID, но не является доказательством отсутствия зависимостей.
- [x] Запустить Tools > Motor City > Audit > Font Dependencies; отчёт Library/MotorCityFontAudit.txt. Проверить ссылки по AssetDatabase.GetDependencies и динамические загрузки, fallback/локализацию. Аудит ничего не меняет.
- [x] Вынести три неиспользуемых TTF вместе с .meta из Resources, сохранив GUID и лицензии (`e97745a`).

19.11.2026: локальный Font Audit завершён (COMPLETE), прямых ссылок на три кандидата нет; по именам/GUID и динамическим путям найдены только Roboto Condensed Regular/Bold. Roboto-Regular, Ubuntu-Regular и Ubuntu-Bold перемещены в Assets/ArchivedFonts с исходными .meta и blob SHA. Лицензии не изменены. 1 198 596 байт исходников исключены из Resources; фактический выигрыш и локализация требуют проверки следующей сборки.

### P2 — LOD геометрии (пилоты зданий завершены, rollout отложен)
- [x] Пилот LOD зданий и расширенный пилот завершены; rollout отклонён. Остальные категории не тестировались.

### P3 — draw calls и повторяющиеся объекты (пилот фонарей завершён, rollout отклонён)
- [x] Пилот instancing фонарей завершён; rollout отклонён, фактический batching не подтверждён.

### P4 — свет, шейдеры, overdraw и разрешение (закрыт в согласованном объёме)
- [x] Adaptive render scale: `motorcity_adaptive=on`, WebGL opt-in, 4s измерение, 12s cooldown, шаг .05, нижняя граница max(.50, preset-.15), исходный preset по умолчанию (`5dd81d5`).
- [x] SSAO и HDR: A/B завершены, результаты ниже.
- [x] Дополнительные lights: отдельный эксперимент пропущен по решению владельца (активны ночью), настройки сохранены.
- [x] Simple Lit A/B завершён на трёх материалах; rollout отклонён (−2.22% FPS).
- [x] Прозрачность/overdraw — пропущено по решению владельца, без замеров.

### P5 — пространственный стриминг, HLOD и видимость кварталов
- [x] HLOD: генерация proxy по ячейкам 128 м из крупных opaque meshes, снижение index count на 15–55% для подходящих групп, точная объединённая геометрия иначе; отдельная сборка, не совместно с district streaming.
- [x] Пространственный аудит и выбор 128 м.
- [x] Conservative visual cells реализованы.
- [x] Double-Block district mesh streaming реализован отдельным вариантом сборки.

Приоритет владельца: держать в рендере только необходимую видимую геометрию, удалённую визуальную часть города по возможности выгружать из памяти. Unity уже выполняет frustum culling; не дублировать его на каждом renderer в C#. Сначала инвентаризация пространственных ячеек и текущих cullers, затем пилот сектора с оригинальной геометрией и предзагрузкой вокруг игрока; occlusion и физическая выгрузка визуальных assets — отдельные этапы. Не выгружать коллизии, дорожный граф, трафик/миссии или видимые отражения/тени. Учитывать повороты камеры, высокую скорость, телепорты и дальность тумана. SetActive/renderer.enabled не равны освобождению памяти; общий CityVisual bundle нельзя считать независимыми секторами.

### P6 — CPU, трафик, физика, анимация, UI
- [x] P6.5 — визуальные wheel poses дальнего трафика 25 Гц вместо 50 Гц; сокращение длины повторных obstacle rays без изменения частоты AI/физики (`72e6265`).
- [x] P6.6 — ограниченный traffic pool с reset и защитой жизненного цикла (`b0de973`, `03cfd56`, `9580b94`, `6114eed`), opt-in `motorcity_traffic_pool=on`, максимум 6 машин всего, 2 на prefab.
- [x] P6.7 — Pixie animator CullUpdateTransforms после выравнивания исходной позы (`daed864`).
- [x] P6.2 — road hit buffer и защитный полный obstacle fallback.
- [x] P6.3 — кеширование upgrades HUD, idle button transforms.
- [x] P6.4 — переиспользование буферов/waypoints и устранение лишних аллокаций форматирования.
- [x] P6.1 — локальный поиск колёс, кеширование компонентов и позиции, squared distances при спавне, кеш коэффициента detail culling. Реализовано, проверка отложена.

## 5. Референсы и ограничения WebGL

Идеи из Unity Megacity Metro (группировка рендера), Fantasy Kingdom / Unity 6 (повторное использование объектов, GPU Resident Drawer), мобильных проектов типа Rainbow Six Mobile (видимость, память, сложность окружения). Это направления для изучения, не доказательство того, что конкретная коммерческая игра использует каждую технику из списка. WebGL 2 не равен native: GPU Resident Drawer/GPU occlusion и другие compute-зависимые решения не планировать как прямой перенос. Официальная документация для проверки совместимости версий: https://docs.unity3d.com/Manual/ ; https://docs.unity3d.com/Manual/webgl-assetbundles.html ; https://docs.unity3d.com/Manual/urp/urp-feature-list.html . Перед реализацией сверить актуальный Unity API и WebGL ограничения, особенно под Unity 6.6.

## 6. Правила работы, чтобы ничего не сломать

1. Перед изменением проверить HEAD, git status у владельца при необходимости, прочитать реальный код и зависимости. Не затирать пользовательские правки. Существующие авторские FBX, текстуры, префабы и GUID сохранять; не редактировать vendor/model source без причины. Не делать массовых правок Resources, LFS, ProjectSettings и .meta; только точечные согласованные изменения. Не удалять ассеты из-за отсутствия статических ссылок.
2. Один эксперимент — один отдельный коммит с понятным SHA и контрольным сценарием. Опциональный dev/URL A/B switch — если нужен; не оставлять мусорные инструменты и логи в релизе. При регрессе вернуть только изменения эксперимента новым коммитом; не force push. При работе с GitHub использовать актуальный parent/expected SHA.
3. Сравнивать на идентичных условиях и записывать реальные цифры. Не принимать уменьшение raw FBX/packed bytes за HTTP экономию, а уменьшение загрузки за FPS. Внешний вид/ссылки и игровая логика важнее теоретической оптимизации. Если выигрыш в пределах шума — отказаться.
4. Не менять Playgama Bridge startup gate, game_ready, storage keys, облачные сохранения, ads pause/audio flow, язык, управление. Отдельно тестировать рекламу, восстановление прогресса, гараж, задания, навигацию, гонки, дрифт, эвакуатор, трафик, день/ночь и фары. Не сбрасывать прогресс ради эксперимента; не тестировать запись пустых сохранений.
5. Не менять runtime-геометрию дорог/позиции/коллайдеры без специального согласования. Сохранять авторский FCG layout. Предупреждение sharp U-turn detour уже принято владельцем; не исправлять попутно.
6. После завершения согласованной серии изменений владелец один раз делает git pull origin main, открывает Unity, проверяет compile, Play Mode и нужные сценарии. Чистая WebGL сборка в отдельной папке, затем браузер, потом Sandbox только после подтверждения владельца. Не заменять работающую сборку без тестов. Не коммитить Library/Builds, архивы, диагностические отчёты с персональными данными, случайные сгенерированные settings.
7. Если скомпилированный WebGL падает — сначала Console/Network и reproduction, не гадать; отделять предупреждение от ошибки, не увеличивать память вслепую. Не утверждать поддержку WebGL технологии, пока она не проверена. Не обещать FPS или сроки без измерения.

## 7. Сборка и выпуск

- [x] Release candidate команды в main: Tools > Motor City > Build > Build District Release Candidate; Build Split Release Candidate (Fallback). Дополнительный независимый вариант Build HLOD Split City Candidate.
- [x] Сборщик автоматически временно исключает локальные mesh LOD pilots, восстанавливает исходные пути и проверяет комплект bundle-файлов.
- [x] Версионированная загрузка city/district bundles и fallback обычного split сохранены.

Единый ручной QA владелец выполняет самостоятельно; отдельные пункты с проверками по его просьбе убраны. Код не равен проверенной сборке: публикация только после решения владельца. Unity Editor, браузер, реальные устройства и Playgama здесь не запускались. Аналитика/монетизация — отдельное продуктовое направление, не часть текущей оптимизации.

## 8. Журнал / следующий запрос

19.11.2026 — раздельная загрузка города подтверждена в Sandbox, QA 21 МБ/3,02 с. managed Gzip отменён. Рендер-аудит исходников показал существующие culling/quality системы и кандидатов на оптимизацию. Фактический полный runtime-аудит не выполнен. Read-only Render Audit завершён владельцем, результаты внесены в P0. Базовое runtime-профилирование завершено; исторические шаги ниже. Актуальная задача — итоговая предрелизная проверка после P6.4.

Для нового чата достаточно: «Открой G3R-Studio/motor-city, Docs/Roadmap-1.1.md, проверь текущий HEAD и продолжи с точки остановки. Не меняй ассеты без аудита; итоговую сборку проверяем целиком».

### Контрольный замер 19.11.2026 — Editor

Performance Baseline COMPLETE: Unity 6000.6.1f1, target WebGL, RTX 2080, 1319×742, qualityIndex=5 (runtime preset отдельно не подтверждён), 60.007 с, 15399 samples. FPS 256.612; frame mean 3.897 ms, median 3.604, p95 5.340, p99 9.686, max 138.712. Main Thread mean 3.891 ms; SetPass mean 146.697 peak 356; triangles mean 1242404 peak 3124115; vertices mean 2086772 peak 5410660; Total Used Memory peak 2550295669 bytes (Editor, не браузер); GC allocated mean 17899 bytes/frame. Draw Calls/Batches вернули нули при ненулевой геометрии — недостоверны; Render Thread/GPU N/A. Владелец отметил существенно более низкий FPS в браузере. Editor только для локализации; критерий принятия оптимизаций — одинаковый WebGL Release маршрут и браузер. Следующий шаг — получить браузерную контрольную точку до изменения ресурсов.

WebGL baseline: добавлен opt-in motorCityBaseline.start() / stop() / save() в консоли браузера. 60 с после прогрева, одинаковый маршрут, пресет и разрешение. Сбор счётчиков только при замере; GPU time и недоступные/нулевые счётчики N/A. Проверка сборки и отчёт владельца ожидаются.

### Контрольный WebGL Release замер 19.11.2026

COMPLETE: Chrome 158, Windows, ANGLE D3D11 RTX 2080, 1512×911, DPR=1, Medium, 60.008 s / 4723 frames. FPS 78.705; mean frame 12.706 ms, median 13, p95 16, p99 18, max 29. Main Thread mean 12.700 ms; SetPass mean 114.111 / peak 156; triangles mean 1055527 / peak 1419744; vertices mean 1787270 / peak 2436811; Unity Total Used Memory peak 383391981 bytes, GC Used Memory peak 33665024 bytes. Draw Calls/Batches/Render Thread/GC allocation/GPU time N/A. Не полная память браузера. Editor и WebGL имеют разное разрешение и маршрут, прямое сравнение ограничено. WebGL baseline зафиксирован; повторять на том же браузере, маршруте, Medium и разрешении. Для выбора CPU/GPU bottleneck требуется дополнительная диагностика. Следующий безопасный эксперимент: аудит потребителей readable meshes перед отключением Read/Write небольшого набора; коллайдеры и динамические потребители исключить. Не изменять ассеты до проверки.

P1: добавлен read-only Mesh Readability Audit. Ограничен FBX декора FCG Objects, проверяет все подмеши, префабы Assets и зависимости сцен; HOLD при MeshCollider/SkinnedMeshRenderer/динамических признаках/зависимости сцены. REVIEW требует ручной проверки скриптов и теста. Поиск исходников выявил ArcadeRacingCarRuntimeInstaller (sourceMesh.isReadable, geometry-based collision), Mesh_CombineUtility и редакторный FCityGenerator; транспорт/дороги/здания исключены из первого пилота. Отчёт Library/MotorCityMeshReadabilityAudit.txt ожидается. Импорт не изменён.

P1 Mesh Readability Audit COMPLETE: 94 FBX, 82 HOLD, 12 REVIEW; ни один REVIEW не имеет обнаруженного использования в CityVisual. 54 FBX с CityVisual, без MeshCollider/SkinnedMeshRenderer и без флагов кроме зависимости FCG_Workbench.unity (3 582 370 editorBytes суммарно, не экономия). Первый пилот по декору отложен: малый потенциальный эффект и необходимость защищать authoring. Не изменять импорт. Следующий шаг — оценить крупные readable runtime меши с учетом всех потребителей и отдельно проверить LOD/instancing для FPS; не менять коллайдеры.

P1 building mesh pilot: Render Audit join by GUID/local ID identifies 286 readable building render meshes not listed as MeshCollider (91 588 416 editorRuntimeBytes, not savings). Ten largest selected BR-003, EC-009, BR-002, QD-14, BR-014, BR-027, BC-P07, BR-025, BC-053, EC-006 (13 284 740 editorRuntimeBytes). No importer changes. WebGL-only opt-in motorCityBaseline.meshPilot() after city load checks mesh name/vertex count/submesh count, excludes all loaded collider/skin refs, animated/dynamic consumers and ambiguous matches; UploadMeshData(true) on selected runtime meshes only. Potential unobserved future/dynamic users remain a risk, not a production feature. Reload page to reset; compare baseline fresh page then pilot page. Report meshPilotReleased; log per-mesh estimated memory before/after. Validate visuals, missions, collisions, garage, console. Browser used memory metric not total heap. Uncompiled/unverified until owner test.

P1 building mesh pilot tested: released=10; mesh estimated before=6633304 after=6633304 bytes. WebGL COMPLETE 60.012s / 4278 frames, Medium 1512×911 DPR1 RTX2080: FPS 71.286, mean 14.028 ms, p95 17, p99 19; triangles mean 1094973, SetPass 112.806; Unity memory peak 378345709 bytes versus baseline 383391981 (−5046272 bytes). Route variation and differing geometry confound attribution; no demonstrated FPS benefit or directly measured per-mesh savings. Keep opt-in only; do not expand as production optimization. Next prioritize P2 LOD / repeated render geometry using controlled browser A/B. No importer changes.

P2: opt-in Building LOD pilot uses Unity 6 MeshLodUtility on local mesh copies (ten audited visual meshes, max two extra levels), never changes FBX, colliders, materials or prefab. Tools > Motor City > Audit > Generate Building LOD Pilot, local Assets/Resources/MotorCityLodPilot, report Library/MotorCityLodPilot.txt. Generated copies are extra test data, not part of production commit; do not commit generated assets yet. Same test build with browser motorCityBaseline.lodPilot(false/true), no meshPilot(), repeat stationary camera and route, check silhouette/UVs/day/night, report lodPilot and lodRenderers. Native automatic selection, original mesh restored on off. Not verified in Unity/WebGL; generated copies add test payload and memory. Remove only generated pilot folder after tests, not original resources.

Workflow change (owner requested): intermediate experiments in Unity Play Mode, WebGL Release validation grouped at phase checkpoints, not every small change. Shared building LOD pilot accessible via Tools > Motor City > Audit > Building LOD Pilot ON/OFF; Editor baseline includes pilot state and renderer count. Generate local copies first; compare same stationary viewpoint, then route. No production enablement; Editor speedup is not proof of browser speedup.

P2 generation confirmed: 10 meshes, 3 levels each, 16 runtime renderers matched. Editor LOD ON 964x542, 266.211 FPS, p95 4.729 ms, p99 5.736 ms, triangles mean 1278871. OFF report overwritten; no valid A/B conclusion. Preserve timestamped baseline reports and repeat same viewpoint. No WebGL rebuild requested.

P2 Editor A/B COMPLETE: 1292×727, RTX2080, 60 s each, 16 renderers ON. OFF 250.087 FPS, mean 3.999 ms, p95 5.145, p99 7.048, triangles 1555659, peak memory 2841487605 bytes. ON 261.103 FPS, mean 3.830 ms, p95 4.766, p99 6.891, triangles 1544536, peak memory 2850182792 bytes. Observed +4.4% FPS, −0.7% triangles, +8.7 MB peak editor memory; single pair, not proof of repeatability or browser gain. Next verify visual LOD transitions and expand a bounded group of frequently repeated building meshes with per-level triangle counts; keep opt-in, preserve originals. No WebGL rebuild until P2 checkpoint.

P2 expanded pilot: Generate Expanded Building LOD Pilot (Edit Mode), rank unique readable building visual meshes by authored CityVisual instance count × LOD0 triangles, at least 2 occurrences and 1000 triangles, max 30 new copies. Exclude city collider/skin mesh references, dynamic/LOD ancestors, alpha/transparent materials, ambiguous source names; preserve original assets. Local separate Resources/MotorCityLodExpanded; existing pilot ON uses both groups (expanded wins duplicate name), OFF restores original. Report Library/MotorCityLodExpanded.txt lists generation, levels and LOD0 weighted triangles (not actual frame or lower-level triangle totals). Do not commit generated assets until acceptance. Editor A/B only; verify visuals, repeat same viewpoint. No compilation or generation verification yet.

P2 expanded Editor A/B: selected 30, generated 28 (BB-017, EC-010 topology mismatch skipped), 114 runtime renderers. 1292×727, 60 s each. OFF 248.766 FPS, 4.020 ms, p95 5.018, p99 7.813, triangles 1528740, peak memory 2841865302. ON 247.842 FPS, 4.035 ms, p95 5.077, p99 7.924, triangles 1531954, peak memory 2848252875. No demonstrated gain; native automatic mesh LOD selection not independently verified. Do not expand or ship generated pilot assets; keep OFF. Next verify actual per-level geometry/selection if revisiting LOD, otherwise prioritize P3 repeated renderer instancing pilot in Editor; WebGL rebuild only after a meaningful phase result. No source asset changes.

P3: Editor-only Lamp Instancing Pilot ON/OFF. Exact StreetLight-01 body mesh from authored prefab; exclude static batches, existing property blocks, inactive and alpha renderers; shared temporary material clones with enableInstancing and per-renderer unused marker MPB to remove SRP Batcher compatibility for selected objects only. Restore materials/blocks on OFF / play exit / assembly reload. Light components, _LightV, colliders and authored assets unchanged. Actual instancing requires shader support and Frame Debugger confirmation (Draw Mesh (Instanced)); marker/block may prevent batching, so no performance assertion. Baseline logs lampInstancingRenderers. Compare OFF/ON at same viewpoint, LOD OFF; inspect Frame Debugger outside timed captures. Local compile/test pending, no WebGL build.

P3 lamp Editor A/B COMPLETE: 489 body renderers, LOD OFF, 1292×727. OFF 251.478 FPS, 3.976 ms, p95 4.989, p99 7.959, SetPass 181.704, peak memory 2845017830. ON 248.273 FPS, 4.028 ms, p95 4.970, p99 7.732, SetPass 199.467, peak memory 2854375446. No demonstrated benefit; instanced draws not confirmed, Draw Calls/Batches unavailable. Keep OFF; do not roll out. Next P4 controlled Editor A/B of expensive rendering features, starting SSAO, check visual tradeoffs; WebGL validation at phase checkpoint only.

### P4 — подготовка SSAO A/B, 19.11.2026

Коммит `d12531380cef4626821b33af47b00947567560bc`: Editor-only SSAO Pilot ON/OFF в существующем MotorCityPerformanceBaseline. MotorCityRenderer содержит одну активную SSAO feature, Downsample=1, Intensity=1.15, Radius=0.05, AfterOpaque=0; runtime quality не отключает SSAO. Активный URP проверяется через единственный renderer data и точный тип feature; SetActive меняет состояние в памяти, исходное восстанавливается при выходе из Play Mode, reload и quitting. Не сохранять ассеты во время пилота; после выхода проверить git status. Baseline пишет ssao=ON/OFF/N/A, runtime preset и renderScale; переключение заблокировано во время замера. Фактические passes проверить в Frame Debugger вне замеров. Средний preset, то же положение/разрешение, LOD и lamp OFF, прогрев и 60 секунд ON затем OFF (по возможности ON повторно); сравнить timestamped reports, визуал дня/ночи. На момент SSAO pilot Editor URP scale .92 не равнялся WebGL Medium .72; исправлено следующим коммитом (см. ниже). Компиляция и измерения владельцем ещё не выполнены, результат и rollout не подтверждены. WebGL не пересобирать на данном шаге.

### P4 — приближение Editor Play Mode к WebGL, 19.11.2026

`1c8e718f`: по запросу владельца runtime URP quality применяется в Editor Play Mode к временной копии активного pipeline (HideAndDontSave, QualitySettings.renderPipeline), прежняя ссылка восстанавливается при выходе, reload и quitting. Renderer data остаётся общим; существующий SSAO pilot восстанавливает feature state отдельно. Low/Medium/High используют те же scale, MSAA, shadow distance/cascades/resolution, что player. Выбрать Medium вручную; проверить renderScale=0.72 в отчёте и чистый git status после выхода. Для сопоставимости с прежним WebGL baseline использовать Game View 1512×911, одинаковый маршрут. Это не эмуляция WebAssembly, WebGL backend или browser/Bridge; абсолютные FPS не приравнивать. Компиляция и проверка владельцем ожидаются. Старые Editor baseline получены с другими URP settings, не переоткрывать завершённые пилоты без причины. P4 SSAO ON/OFF остаётся следующим тестом; WebGL не пересобирать.

### P4 — SSAO Editor A/B, 19.11.2026

COMPLETE, два отчёта 151341-611 и 151534-139: RTX2080, Medium, scale .72, 1512×911, target WebGL, LOD/lamp OFF. ON 60.005s: 334.546 FPS, mean 2.989 ms, p95 3.509, p99 3.981, max 13.813, SetPass 134.988, triangles 1228957.926, peak Unity memory 1635900840 B. OFF 60.000s: 363.040 FPS, mean 2.755 ms, p95 3.283, p99 3.705, max 163.390, SetPass 105.169, triangles 678702.169, peak memory 1624006435 B. Observed +8.52% FPS, −44.8% counted triangles; removal of geometry prepass plausible but not verified in Frame Debugger. Draw Calls/Batches zero unreliable, GPU N/A. One pair only; visual comparison absent, OFF outlier 163 ms. Browser-aligned Editor scale confirmed, not browser FPS. Next compare same viewpoint screenshots day/night and inspect SSAO passes outside measurement, repeat ON if needed. Do not change production defaults until visual acceptance; no WebGL rebuild yet.

### P4 — решение по SSAO, 19.11.2026

Владелец сообщил, что визуально игра выглядит одинаково при ON/OFF. Принято внедрение Low/Medium OFF, High ON (`0ff7e317`); используется публичный URP rendererDataList / rendererFeatures / SetActive, исходная feature сохраняется активной в asset для сборки. Editor восстанавливает исходное состояние на exit/reload/quitting. Ручной SSAO pilot удалён (`63623df`), baseline сохраняет поле ssao. Проверить компиляцию, Medium→High→Low→Medium, состояние в baseline/Frame Debugger, день/ночь, чистый git status после выхода. Отдельная проверка WebGL и shader variants на checkpoint обязательна; визуальная оценка владельца не доказывает равенство всех сцен. Следующий P4 эксперимент после проверки — HDR A/B отдельно, без изменения SSAO и остальных настроек. WebGL пока не пересобирать.

### P4.2 — HDR pilot, 19.11.2026

`b04e413`: Editor-only HDR Pilot ON/OFF в MotorCityPerformanceBaseline; переключает supportsHDR только на непостоянной копии URP Play Mode, блокируется во время замера, копия уничтожается существующим cleanup. Baseline пишет hdr и cameraHDR. Medium, SSAO OFF (проверить автоматическое переключение пресетов перед замером), LOD/lamp OFF, 1512×911, одинаковая камера, прогрев и 60s ON/OFF. Проверить визуал отдельно днём и ночью (эмиссия, фары, окна, небо), cameraHDR и реальный формат render target в Frame Debugger вне замера. Production HDR не изменён. Владелец ещё не предоставил подтверждение SSAO rollout compile/switch; проверить вместе с HDR. Локальная компиляция и HDR A/B ожидаются; WebGL не пересобирать.

### P4.2 — HDR A/B COMPLETE, 19.11.2026

Medium, SSAO OFF, scale .72, 1512×911, RTX2080, LOD/lamp OFF. ON: 341.577 FPS, 2.928 ms, p95 3.704, p99 4.283, SetPass 105.246, peak memory 1626722783 B. OFF: 341.294 FPS, 2.930 ms, p95 3.592, p99 4.112, SetPass 103.949, peak memory 1615801291 B, max 196.644 ms. Both cameraHDR=False (Camera.main), so effective HDR target change not demonstrated. Owner reports no visual difference. No demonstrated FPS gain; retain original production HDR, do not repeat pilot without evidence of HDR camera cost. SSAO OFF on Medium confirmed in both reports; other presets and WebGL checkpoint still pending. Next audit additional lights / Forward+ cost before choosing single reversible pilot; MSAA already off on Medium. No WebGL rebuild.

### P4.3 — дополнительные источники света, 19.11.2026

Владелец сообщил, что источники света включаются только ночью, и попросил закрыть этот пункт без дополнительных экспериментов. Изменений освещения или renderer mode нет, стоимость не измерялась. Forward+/Forward отдельно не проверен. Следующий шаг — P4.4 аудит шейдеров/материалов, затем overdraw. WebGL пока не пересобирать.

### P4.4 — аудит шейдеров города, 19.11.2026

`37dd6ab`: Tools > Motor City > Audit > City Shader Audit в Play Mode после загрузки MotorCity_FCGCity. Read-only, Library/MotorCityCityShaderAudit.txt: группировка shader, уникальные material instances, слоты и enabled slots, nonMesh/MPB, asset path, queue, surface/clip, свойства и карты, keywords. Слоты не равны draw calls, enabled не означает видимость камерой. Scope только runtime city root, не весь игровой мир. Отдельно учитывать FcgRuntimeGlassMaterialFactory и night emission; исключить стекло, листву, специальные материалы и динамические потребители перед пилотом Simple Lit. Отчёт и компиляция ожидаются; никаких asset/shader изменений, WebGL не пересобирать.

### P4.4 — результаты City Shader Audit и Simple Lit pilot, 19.11.2026

Audit завершён: URP Lit 52 материала, 12920 slots / 6939 enabled; WebStreetLight 9548/219; TwoSidedFoliage 2002/1455; Simple Lit 98/98; Backdrop 1/1. Слоты не draw calls и не видимость. `c80a6a4`: Editor-only Simple Lit Pilot ON/OFF для трёх точных путей Metal_5dd313eb, Tires_2fcd0fa9, Grass-01_f9a48a42 (2046 enabled slots на момент аудита). Только MeshRenderer внутри runtime city, исключены static batch, MPB, keywords, metallic, alpha clip и nonopaque. Временные общие копии с base map/color/tiling, smoothness, cull, receive shadows и приблизительным specular; исходные материалы не изменяются, ссылки восстанавливаются на OFF/exit/reload. Возможны отличия освещения из-за иной BRDF, проверять результат визуально; общие материалы могут использоваться на разных объектах города, точные потребители не классифицированы. Baseline поля simpleLitSlots/simpleLitMaterials. Medium, SSAO OFF, HDR ON, LOD/lamp OFF, 1512×911, 60s OFF/ON после прогрева; визуал день/ночь. Компиляция и замер ожидаются. WebGL не пересобирать.

### P4.4 — Simple Lit A/B COMPLETE, 19.11.2026

Medium, SSAO OFF, HDR ON, scale .72, 1512×911, RTX2080, LOD/lamp OFF. ON первым: 6618 slots, 3 materials, 318.627 FPS, mean 3.138 ms, p95 4.048, p99 5.255, SetPass 110.783, triangles 679163.606, peak memory 1657729211 B. OFF: 325.850 FPS, 3.069 ms, p95 3.800, p99 4.553, SetPass 105.098, triangles 682228.624, peak memory 1678224951 B, max 321.875 ms. Владелец не заметил визуальных отличий. −2.22% FPS, один A/B, GPU time N/A, не доказан выигрыш; временные копии дополнительных материалов не считать экономией GPU memory. Rollout отклонён, оставить оригинальные Lit, pilot OFF. Следующий шаг P4.5: аудит прозрачности/overdraw с учётом уже существующего foliage culling; не повторять Simple Lit без новых оснований. WebGL пока не пересобирать.

### P4.5 — прозрачность и overdraw, 19.11.2026

По решению владельца отдельную оптимизацию прозрачности и растительности пропускаем: владелец не считает их существенной нагрузкой. Измерений overdraw не проводилось; шейдеры, материалы и существующий foliage culling не изменены. P4.1–P4.5 закрыты в согласованном объёме, необязательные тесты MSAA/Forward+/render scale отложены, WebGL checkpoint остаётся открытым. Следующий этап P5 — аудит существующего HLOD и видимости кварталов перед пилотом. Не повторять завершённые эксперименты.

### P5 — направление по запросу владельца, 19.11.2026

Проверены CityHlodRuntime, CityDistanceDetailCuller и MotorCityVisibilityRuntime: HLOD требует запечённых данных, detail culler уже отсеивает малые объекты, Medium Auto far clip 1250 м / fog end 1140 м. Следующий шаг — read-only spatial inventory (bounds, renderers, meshes/material dependencies, protected objects, active/culling state) и выбор размера ячейки по распределению; не вводить второй конфликтующий culler. Никаких изменений runtime или измерений на этом шаге. Цель — видимость + streaming при сохранении исходного визуала и механик.

### P5.1 — City Spatial Audit, 19.11.2026

`3adc29d`: Editor-only Tools > Motor City > Audit > City Spatial Audit после загрузки города. Read-only отчёт Library/MotorCityCitySpatialAudit.txt: сетки 64/128/256 м, renderer counts, enabled и camera frustum AABB, triangle references, crossing bounds, консервативные guarded flags, уникальные и разделяемые меши/материалы внутри города, расстояния до bounds ячейки. Группировка по центру renderer; крупные пересекающие границы объекты требуют отдельной обработки. Не измеряет occlusion, память или внешних потребителей, flags не доказывают безопасность выгрузки. Выбрать размер ячейки по отчёту перед пилотом; физику и runtime не изменять. Компиляция/аудит ожидаются, WebGL не пересобирать.

### P5.1/P5.2 — пространственный аудит и runtime visual cells, 19.11.2026

Audit COMPLETE: 21690 MeshRenderer, 2 missing meshes; 64m 364 cells/1900 crossing, 128m 103 cells/1192 crossing/5452 guarded, 256m 30 cells/661 crossing. 348/550 mesh assets shared between 128m cells, 30 shared materials. Camera far=600, fogEnd=552. Выбрано 128 м как компромисс. `1ea0642`: MotorCityVisibilityRuntime группирует крупные opaque Atlas-1_59aa769b renderer в ячейки, bounds union, frustum с запасом и hysteresis (Expand 256/384), LateUpdate восстанавливает при повороте/смене дистанции. Только Low/Medium, одна активная камера, без HLOD и теней; opt-out motorcity_cells=off. Исключает мелкие объекты, static batches, MPB, Rigidbody/Animator/LODGroup и специальные материалы. Меняет renderer.enabled, не GameObject/коллайдеры. Никакой реальной выгрузки mesh/texture/bundle из памяти пока нет; для неё требуется разделение визуального ассета и независимая загрузка с сохранением физики, общих dependencies. Риски: стоимость LateUpdate, одновременные владельцы renderer.enabled, особые камеры и видимость; итоговая проверка обязательна. По запросу владельца промежуточные локальные тесты отменены, все изменения проверяются вместе перед релизом (компиляция, Play Mode, WebGL, Sandbox); не выдавать непроверенные изменения за подтверждённые оптимизации. Следующий шаг — архитектура раздельных визуальных bundles / дальнейшие CPU оптимизации. 

### P5.3 — Double-Block district mesh streaming, 19.11.2026

По указанию владельца использовать существующие районы Double-Block-* вместо искусственной сетки для физической выгрузки. Реализован отдельный Tools > Motor City > Build > Build District Streaming Test (`adcbc05`, `ec0e6ac`, `4618860`, `84f20e4`, `7af4f47`, `e610296`). Оригинальный split build сохранён. Временная копия CityVisual с сохранёнными иерархией/коллизиями/скриптами; только крупные непрозрачные Atlas-1 архитектурные меши внутри Double-Block, исключены меши, используемые collider/skinned renderer/другим районом или нестриминговым MeshFilter. Отдельные mesh bundles LZ4, versioned filename/hash, catalog с индексами renderer и bounds, загрузка ближайших к spawn до Ready, затем по камере с 360° запасом far clip +160 м, unload при превышении ещё 128 м. MeshFilter.sharedMesh очищается до AssetBundle.Unload(true); общие материалы и физика остаются resident. Ранее добавленный клеточный culler не применяется в district build. Не заявлять выгрузку всех объектов/текстур или экономию памяти до профилирования; внешние ссылки на исходные FBX могут удерживать общие ресурсы. Отсутствие подходящих районов приводит к отказу новой сборки, не изменяет обычный split. Риски: визуальные пропуски при неудачной загрузке/телепорте; прогресс и время prefetch; стабильность renderer traversal, исходные prefab dependencies, bounds и shader stripping. Проверка компиляции, генерации bundles, спавна, смены камер, полного города/миссий/трафика, день/ночь, HTTP/cache, memory и Sandbox отложена владельцем до итоговой проверки. Не публиковать district build до проверки. Следующий этап P6 либо завершение интеграции по итоговой проверке. 

### P6.1 — CPU traffic / detail culling, 19.11.2026

Реализовано без промежуточных локальных тестов по решению владельца: `4789cbf` TrafficSystem — squared distance для spawn candidates, кеширование позиции игрока в синхронном проходе и TrafficCar при инициализации/спавне; порядок обхода, лимиты, минимальная дистанция, Linecast и FOV сохранены. `731264c` TrafficCar — поиск колёс только среди активных дочерних Transform вместо глобального FindObjectsOfType; Rigidbody lookup один раз, масса и interpolation сохранены. При дублирующихся именах колёс порядок выбора может отличаться, проверить префабы. `a515125` CityDistanceDetailCuller — кеш проекционного коэффициента и refresh при изменении FOV/preset, thresholds сохранены. Не изменены FixedUpdate, частота physics queries, настройки collision, AI, дорожный граф. Компиляция и эффект не подтверждены; проверить spawn/despawn, повороты, колёса, столкновения, GC и CPU перед выпуском. Следующие P6: аудит physics hot path, UI updates и аллокаций. 

### P6.2–P6.4 и предрелизная подготовка, 19.11.2026

- [x] `8885f52` CityAssetRuntimeInstaller.TryGetRoadHit: 64-hit NonAlloc buffer, сортировка только заполненной части, полный RaycastAll при насыщении; исходные слои, triggers и поиск road surface сохранены.
- [x] `e88468c` TrafficCar: reuse массива nodes того же размера, brake light SetActive только при изменении, RaycastAll fallback если NonAlloc буфер полон; частота probe, физика и движение неизменны.
- [x] `728231a` PrototypeHud: cache upgrade values + invalidation при смене языка, не пересчитывать неизменный текст или повторно скрывать панель. `175d582` UiButtonFeedback: не записывать transform.localScale в покое, завершать анимацию в epsilon.
- [x] `c3710e5` split/district build: временный перенос двух папок LOD pilot с проверкой типов standalone Mesh .asset, возврат путей при finally/Restore City After Experiment, без удаления. `fc2bfe5` проверяет точный список raw bundle, ненулевой размер, index.html, разделяет SplitCityTest и DistrictStreamingTest.
- [x] `0aa7549`, `81a8d2e` district catalog: проверка renderer count, диапазона/уникальности indices и наличия MeshFilter без меша; при отказе загрузки стартового района не выставлять Ready, освобождать warm bundles и повторять загрузку. Кеш чтения marker.

**Единая проверка владельца перед выпуском:** git pull origin main; Unity compile; обычный Play Mode, день/ночь, пресеты, быстрый проезд/телепорт, трафик, гараж, все задания, UI/языки, сохранения и реклама. Для нового стриминга Tools > Motor City > Build > Build District Streaming Test с профилем Web - Desktop - Release; отчёт Library/MotorCitySplitCityTest.txt, путь к DistrictStreamingTest в отчёте. Если district build fails (включая отсутствие подходящих уникальных мешей) либо есть пропуски геометрии/пики загрузки, не публиковать его; отдельный проверяемый резерв Build Split City Test. Проверить чистый git status и восстановление всех исходных путей; локальный HTTP, затем Sandbox, повторный запуск/кэш, память, реальные мобильные браузеры. Решение о публикации принимает владелец после QA.

### Дополнительная серия реализаций, 19.11.2026

- [x] HLOD candidate: `0e22edd`, `6f0e119`, `63387b1`, `cac25aa`. Build-time merging крупных статических одно-материальных readable meshes, ограничение 60000 vertices, отдельный уменьшенный index buffer из Unity Mesh LODs (при подходящем числе индексов; иначе полная геометрия). HLOD доступен в отдельной split сборке; не используется одновременно с district streaming. Не гарантирована экономия памяти: proxy добавляет данные, упрощение может не сработать на конкретной группе. Не утверждать измеренную экономию.
- [x] `5dd81d5` — bounded adaptive scale по URL, исходный масштаб без opt-in.
- [x] `72e6265` — визуальная частота обновления колёс дальних машин ниже, физика и дорожные правила остаются 50 Гц, длина ray ограничена уже принятым ближайшим попаданием.
- [x] `b0de973`, `03cfd56`, `9580b94`, `6114eed` — traffic pooling по URL, только обычный дистанционный despawn машин без дополнительных lifecycle scripts, junction ownership, Animator/ParticleSystem/AudioSource. Сброс состояния, rigidbody и света; очистка ссылок позади и известных резервирований. По умолчанию OFF. Может не подобрать подходящих машин; не подтверждён реальный reuse.
- [x] `daed864` — cull offscreen bone transform writes Pixie, не CullCompletely.
- [x] `4cdd184` — команды сборки кандидата и резервного split.

### Невыполненные или отклонённые направления (не скрывать)

Не реализованы: истинный occlusion culling за зданиями (runtime-instantiated city, отсутствуют совместимые baked occlusion data), explicit GPU instancing (предыдущий lamp A/B без выигрыша), AI/physics LOD с урезанием частоты принятия решений, упрощение MeshCollider, ASTC/mobile texture variant. Не внедрены глобальная дедупликация, новые изменения alpha/normal maps, массовое переключение Mesh Read/Write, новые пороги native LOD, дополнительные изменения MSAA/Forward+/теней. Причины — отсутствие доказанной безопасной замены или ранее отклонённый эксперимент; эти направления не выдавать за выполненные. Инвентаризация не была полным runtime dependency/GPU audit. Это не чек-лист ручных проверок и не обязательные блокеры выпуска. Продуктовые аналитика/монетизация не изменялись.

### Коррекция качества теней High, 19.11.2026

Владелец заметил плохое качество теней на High. `531000c`: только High — directional shadow atlas 512→2048, shadow distance 40→65 м, cascades 1→2 (существующий split 0.32), QualitySettings.shadows HardOnly→All. DayNightCycleController уже выбирает LightShadows.Soft для High, URP поддерживает soft shadows. Low/Medium, render scale, MSAA, освещение и настройки исходных ассетов не изменены. Это осознанное повышение качества с возможной дополнительной GPU-нагрузкой; результат и FPS не измерены.

### Передача в новый чат — актуальная инструкция

Прочитать актуальный roadmap из main G3R-Studio/motor-city; не повторять завершённые пилоты и не выдавать неподтверждённые реализации за проверенные. По запросу владельца отдельные пункты ручных проверок удалены. Владелец сам проверяет итоговую сборку, далее исправлять конкретные ошибки/регрессии по логам. Команды Build District Release Candidate, Build Split Release Candidate (Fallback), Build HLOD Split City Candidate. HLOD и district сейчас отдельные варианты. Сохранять физику, игровой процесс, GUID, Bridge и исходные ассеты. Непроверенные дополнительные функции по URL по умолчанию не активны.


### Стартовая загрузка — 20.11.2026

- `dbe804d`: исключение Intro_01–06 из Resources на время штатного split WebGL Build. WebGL уже пропускает сюжетные слайды; MainMenu сохранён. Восстановление исходников и GUID в finally и после прерывания. Прямые зависимости могут оставить данные в player. Размер и компиляция не проверены.
- Гараж, дополнительные автомобили и компаньоны пока не вынесены. Требуется адаптация каталога автомобилей, сохранений и async gates с учётом общих зависимостей.


### Перенос игровых prefab — 20.11.2026

- `194eb33`, `6b53fba`, `b022b41`, `87e3c07`, `abdc9ff`: девять prefab автомобилей (Beatall, Peugeot306, ToyotaAE86, Hybrid, Porsche996, AmgGT, Camaro, Delorean, Bus) и два companion prefab временно перемещаются из Resources при split Build и входят в существующий городской bundle. Сохраняются resource addresses, GUID, recovery и Resources fallback для Editor/native. Street/PlayerCarVisual оставлен в Resources. Каталог и загрузчик визуалов используют общий lookup; сохранения, unlock и физика не менялись.
- Это промежуточный этап уменьшения initial DATA, не независимая загрузка каждого автомобиля по требованию: пакет скачивается целиком при входе в город. Общие зависимости внутри city bundle не разделяются; дублирование с оставшимися Resources/district bundles возможно. Размер и Unity compile не подтверждены. Гараж связан с CityVisual; отдельный перенос требует анализа геометрии и коллизий. Отдельные пакеты и точечные async gates остаются открытыми.
- `21b884e`: TMP LiberationSans importer metadata обновлена до версии 4, GUID сохранён, Unity reimport не проверен.


### Расширение split-пакета, 20.11.2026

- `7bd3617`: оставшийся Street/PlayerCarVisual и девять prefab маркеров (CarWash, Tow, Profession, Discovery, Delivery, Sprint, Underground, Circuit, Drift) включены в существующий городской пакет. Проверено наличие их .meta в репозитории. Потребители маркеров, динамические checkpoint/static marker и debug lookup переведены на LoadGameplay (коммиты 2f1403a–de1677e). Нативные сборки и Editor сохраняют Resources fallback. Исходные prefab, эффекты, сохранения и параметры импорта не изменены.
- Film Grain найден в DefaultVolumeProfile и URP global settings; не удалён без проверки фактической зависимости/влияния. Гараж, независимые пакеты автомобилей/компаньонов, полный аудит шрифтов остаются открытыми. Прирост DATA, компиляция, работа в Unity и отсутствие дублирования с player не подтверждены. Промежуточная сборка не требуется.


### Аудит дублирования, 20.11.2026

- `d6fda47`: штатный split Build автоматически пишет Library/MotorCityStreamOverlap.tsv — пересечение зависимостей city bundle с фактически упакованными player assets, GUID и packed bytes. Это диагностика, не дедупликация и не измерение сетевого размера; district-only зависимости отдельно не анализируются.
- Film Grain: DefaultVolumeProfile intensity=0, но URP global settings содержат ресурсы эффекта. Изменения не внесены без подтверждения безопасности. Компиляция и размер не проверены; отдельные пакеты гаража и автомобилей остаются открытыми.


### Портреты и окраска, 20.11.2026

- `236aaaf`, `81ef36f`, `20ed870`: четыре PNG-портрета персонажей и пять StarterPaint .mat включены в общий городской bundle при штатном split Build. Сохранены исходные resource addresses, типы файлов, GUID и Resources fallback. Проверены исходные пути портретов. Компиляция, фактическое исключение зависимостей из DATA и отображение Sprite не проверены. Независимая загрузка по требованию остаётся открытой.


### Игровые звуки, 20.11.2026

- `c1e1813`, `b036741`: CarEngine.wav и TireSkid.ogg перенесены в общий городской bundle при split Build, загрузчик PlayerVehicleAudio адаптирован. GUI SFX оставлены в Resources, параметры импорта и звук не изменены. Unity compile, playback и фактическая экономия DATA не проверены. Независимые пакеты и гараж остаются открытыми.


### Шрифты и TMP, 20.11.2026

- RobotoCondensed Regular/Bold и TMP Settings необходимы для UI главного меню и динамических SDF-атласов; оставлены в DATA. `415fff3`: кеш успешного lookup TMP Settings вместо повторных Resources.Load при настройке и обновлении текста, с повторным поиском при отсутствии ассета. Это небольшая CPU-оптимизация, не уменьшение DATA. Полный анализ TMP fallback assets и packed sizes остаётся открытым; не удалять шрифты без проверки кириллицы. Unity compile не проверена.


### Разделение пакетов, 20.11.2026

- `7186185`, `8f1d3e4`: штатный split Build формирует city, gameplay и при наличии подходящих пересечений shared bundle. Явное назначение общих Texture/Material/Mesh/AudioClip/AnimationClip из Assets уменьшает риск дублирования внутри этих пакетов; не гарантирует отсутствия дублей с player, district или прочими типами. Manifest dependency DAG задаёт порядок загрузки, marker содержит name|hash, версионированные имена и проверка inventory при копировании сохранены. Старый marker с одним hash поддерживается. Runtime загружает пакеты до создания CityVisual, повторяет загрузку при сетевой ошибке, освобождает загруженные пакеты на неудачной попытке. Существующий district warmup сохранён.
- Все три пакета пока загружаются перед входом в игровой мир, а не индивидуально при выборе машины/появлении персонажа. Разделение само по себе не обещает дополнительной экономии DATA; отдельные async gates, гараж, фактический размер, отсутствие дублирования и Unity compile остаются открытыми. Packages не изменены.


### Отложенная десериализация автомобилей, 20.11.2026

- `a62a6a5`, `e86d163`: проверка наличия prefab в загруженных AssetBundle через Contains вместо LoadAsset; сохранён Resources fallback. При обычной смене машины проверяется фактический prefab до записи выбора. Сброс списка gameplay bundles при SubsystemRegistration. Это уменьшает преждевременную десериализацию, не сетевую загрузку: gameplay bundle пока скачивается целиком. Unity compile, память и время не проверены. Индивидуальные загрузки и garage остаются открытыми.


### Отложенная загрузка автопарка, 20.11.2026

- `50b8067`, `4229523`, `3ecc93f`, `618bd2e`, `c9ab5e5`: девять дополнительных машин выделены в fleet bundle, Beatall остаётся в startup gameplay bundle. Для нового сохранения/мигрированного сохранения с индексом 0 fleet скачивается перед входом в гараж; для остальных сохранений загружается до создания игрока, включая старые индексы. Build проверяет отсутствие зависимостей startup bundles от fleet; общие ресурсы анализируются между тремя группами. Прогресс, две попытки, разблокировка управления и завершение активности при ошибке. RU/EN сообщения. Каталог сообщает наличие известных моделей до загрузки, реальный prefab проверяется при выборе.
- Это пакет всего дополнительного автопарка, не индивидуальные скачивания. Геометрия гаража и компаньоны пока не отделены. Видимость статуса загрузки/ошибки в HUD, все пути входа и сохранения, компиляция, размер и сетевые повторы не проверены в Unity. Packages не изменены. Не проводить промежуточную сборку.


- `1a2a99e` (20.11.2026): удержание сообщения об ошибке fleet 8 секунд в зоне гаража, уведомление Changed только при изменении целого процента, явное снятие блокировки управления при RestoreDriving. Проверено по исходникам, Unity QA не выполнялась. Геометрия гаража и независимый companion streaming остаются открытыми.


- `d918228`, `25c0020`, `9576b2a`: Amane Kisora выделена в supporter bundle; загрузка при запросе supporter skin, две попытки, обычный Haon Byte до успешного скачивания, автоматическая перестройка визуала. Проверка отсутствия зависимости resident bundles от supporter. Геометрия гаража остаётся в городе. Отдельный прогресс/ручной retry для skin не реализован; компиляция, зависимости, переключение и размер не проверены в Unity. Packages не изменены.


## Контрольная сборка

- `7f92ba9`: автоматический повтор загрузки supporter skin; `1c84341`: кеш версий; `0860601`: отчёт межпакетных зависимостей; `b822f06`: startup DATA+WASM и суммарный размер пакетов; `9f1aab6`: валидация marker. Все изменения требуют проверки компилятором Unity.
- Git pull, сохранить локальные Packages/manifest.json и packages-lock.json. Открыть Unity, дождаться импорта, проверить Console. Штатный Build профиля Web - Desktop - Release в пустую папку. Не Build And Run.
- Собрать Library/MotorCityCityBundleExperiment.txt, Library/MotorCityPlayerPackedAssets.tsv, Library/MotorCityStreamOverlap.tsv, Library/MotorCityBundleDependencies.tsv. Сравнить DATA+WASM с исходными 19 570 145 байт. 10 MiB = 10 485 760 байт; 10 MB = 10 000 000 байт.
- Проверить меню RU/EN, новую игру, старое сохранение с нестартерным авто, первый вход в гараж, смену машины, повторный вход, сохранение и перезагрузку, supporter skin, маркеры, портреты, окраску, звук двигателя и шин. Проверить районный стриминг и восстановление после сетевого сбоя. Проверить отсутствие розовых материалов и корректность теней, шрифтов, кириллицы, Playgama bridge и GameplayStart/Stop.
- Ограничения: fleet скачивается целиком, не по машине; гараж является частью города, Haon Byte входит в обязательный gameplay bundle; Film Grain и TMP fallback не удалены. Manifest передаётся в BuildPlayer для защиты типов от stripping, это не доказательство сохранности всех shader variants. Отчёты выявляют потенциальные пересечения, не доказывают отсутствие дублей. Unity и контрольная сборка удалённо не выполнялись. Размер 10 MiB не подтверждён.
