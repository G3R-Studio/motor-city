# Motor City — технический roadmap и передача контекста

**Актуально на 19.11.2026.** Репозиторий `G3R-Studio/motor-city`, ветка `main`. Снимок перед созданием документа: `edf88981aa87aecafb488b474a25d510583ec107`. Это единственная актуальная очередь технической работы; исторические измерения — в [Audit-1.1.md](Audit-1.1.md). Статусы: [x] подтверждено в указанном объёме; [ ] предстоит; **эксперимент** не означает готовность к релизу.

## 0. Контекст для нового чата

Motor City — открытый городской автомобильный симулятор G3R Studio на Unity 6000.6.1f1, URP 17.6.0, WebGL 2 / Playgama. Единственная включённая сцена: `Assets/Scenes/Prototype.unity`. Основной авторский город — Fantastic City Generator, исходная рабочая сцена FCG_Workbench, запечённый префаб `Assets/Resources/MotorCity/Environment/CityVisual.prefab`. Локальный проект владельца: `D:\GitHub\motor-city`. Версия 1.1 доступна в Sandbox: https://playgama.ai/play/nkvqxlch7s76z6 ; это не подтверждение полной публикации.

Владелец тестирует Unity и Windows-браузер локально. Ассистент читает приватный GitHub, проверяет актуальный HEAD, вносит малые согласованные изменения в main, сообщает SHA и точные действия. Не утверждать, что Unity запускалась дистанционно. После каждого принятого эксперимента обновлять этот roadmap: дата, SHA, методика, до/после, результат, известные ошибки, следующий шаг. Не начинать новый этап без результатов предыдущего.

**Точка остановки:** загрузка оптимизирована и проверена в Sandbox. Следующая задача — измерить runtime baseline после успешного Render Audit; затем определить CPU/GPU/память-узкое место и выбрать первый измеримый рендер-эксперимент. Никаких массовых изменений импорта, LOD, материалов или коллайдеров до аудита.

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
- [ ] Ввести безопасный штатный процесс split-релизов после QA; обычный Build всё ещё включает город в Resources. Не путать его со специальной командой.

Ключевые SHA: `144a917` mesh pilot; `ce7e69b` диагностика; `945c458` расширение корней; `ffb0c01` split loader; `f8dd6fc` HTTP sidecar; `7f777cd` неудачный Gzip; `edf8898` откат. Сборка, проверенная в Sandbox: `Builds/CityBundleExperiment/20261119-044000-262/SplitCityTest` (локальная, не в Git). Report: Library/MotorCitySplitCityTest.txt. Состояние main после отката соответствует рабочему загрузчику, повторная сборка main отдельно не подтверждена.

## 3. Техническая карта рендера

| Файл/компонент | Назначение |
|---|---|
| Assets/Settings/MotorCityURP.asset | Базовый URP: HDR on, MSAA 2x, scale .92, дополнительные lights, soft shadows, 2 каскада, SRP Batcher on, GPU Resident Drawer off |
| Assets/Settings/MotorCityRenderer.asset | Renderer, SSAO feature active в asset; фактические проходы проверить |
| Assets/Scripts/Platform/MotorCityQualityRuntime.cs | Runtime Low/Medium/High: scale .60/.72/.85, MSAA 1/1/2, дистанция теней 0/0/40, каскады 1, LOD bias .50/.70/.85, maxLOD 1/1/0; traffic budget 10/18/24, radius 105/125/140; FPS cap отсутствует |
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

World/ — относительно Assets/Scripts/. Базовые URP настройки не равны действующим runtime; Editor Play Mode намеренно не изменяет URP asset. Измерять именно release player.

## 4. Очередь оптимизации рендера

### P0 — аудит и контрольная точка (следующий шаг)
- [x] Проверить добавленный Tools > Motor City > Audit > Render Audit (название предложено), вывод Library/MotorCityRenderAudit.txt; **только чтение**, никаких SaveAssets, перемещений, правок импорта.
- [ ] Сканировать сцены, CityVisual и все реальные runtime roots/Resources, включая содержимое отдельного bundle, автомобили, гараж, персонажей, UI, динамические пути Resources.Load/LoadAll. Отличать authoring от runtime. Не путать исходный размер/packed/HTTP/память.
- [ ] Текстуры: GUID, пути, тип, размер импортированного изображения, формат и platform overrides Desktop/Mobile, mipmaps, Read/Write, alpha/sRGB, estimated GPU/CPU memory; одинаковые GUID и отдельные GUID с визуально одинаковым содержимым — различные случаи. Отчёт по потребителям (материалы, префабы, сцены); не удалять.
- [ ] Меши: GUID + local file ID (subassets FBX), число вершин/индексов/треугольников, submeshes, bounds, readable, UV channels, compression, usage count; разделить MeshFilter/SkinnedMeshRenderer и MeshCollider. Считать уникальные меши и экземпляры отдельно, не читать vertex buffers ценой включения Read/Write.
- [ ] Объекты: renderer counts, active/inactive, LODGroup, shadow casting/receiving, материал/шейдер, transparent/alpha-clipped, instancing flags/shared mesh+material+submesh, static flags, collider types, rigidbodies, animator, particle systems, lights. Найти крупные группы кандидатов и исключить интерактивные/движущиеся элементы.
- [ ] Измерить реальный кадр: FPS median/1% low или p95/p99 frame time, CPU main/render time, GPU если доступен, batches/draw calls, SetPass, triangles, vertices, peak memory/heap, stalls и context loss. Editor Profiler/Frame Debugger — для локализации, WebGL Release — итог. Если счётчик недоступен, указать N/A, не ноль. Memory Profiler/более тяжёлую диагностику при необходимости использовать только на отдельном тесте.
- [ ] Контрольный маршрут: одинаковые сохранение, положение/камера, пресет, разрешение/DPR, дальность, транспорт, день и ночь, одинаковая сцена трафика и 30–60 с измерений после прогрева. Отдельно холодная загрузка и повторный запуск, мобильный браузер. Не включать DevTools throttling или FPS limit только в одном из вариантов.
- [ ] По итогам выбрать направление на основе bottleneck. CPU draw calls → instancing/HLOD; GPU triangles → LOD; GPU fill → SSAO/overdraw/render scale; память → Read/Write/форматы. Не обещать прирост заранее.

**Результат P0 asset inventory, 19.11.2026:** отчёт COMPLETE, WebGL, 245 roots / 862 paths. 193 Texture2D (66 101 910 editorRuntimeBytes, все readable=False); 1104 Mesh (168 258 072 editorRuntimeBytes, 791 666 уникальных треугольников), 966 readable (151 012 904 байт суммарной оценки этих мешей, не потенциальная экономия). CityVisual: 21 582 renderer, 8 504 collider, 3 LODGroup, 777 Light; это авторские компоненты, не активные одновременно. Группы mesh/material/submesh: корпус StreetLight-01 489 экземпляров (2 submeshes), Flowerbed-07 394 с Atlas-1, части светофоров до 900. Светофоры динамические — не первый кандидат. Тяжёлые меши: garage.fbx ~1.83 MB (readable false), Border-Block-05 Trees ~1.78 MB, BR-003 ~1.55 MB, EC-009 ~1.53 MB. Atlas-1 2048 DXT1 ~5.62 MB — оставить пока. Все цифры — редакторская оценка, не замеры WebGL GPU/heap/draw calls. Инструмент исправлен в 1cc31e9 (TryGetComponent). Отчёт не даёт доказательства безопасности отключения Mesh Read/Write. Следующий шаг: Tools > Motor City > Audit > Start Performance Baseline (60s) в Play Mode; Library/MotorCityPerformanceBaseline.txt. Сначала 60 секунд после прогрева на одном пресете и разрешении. Это Editor baseline, не WebGL. Недоступные счётчики N/A; затем отдельный WebGL Release контроль.  затем проверка потребителей readable meshes и один малый обратимый пилот. Текстурный Read/Write уже отключён; не повторять.

### P1 — память и текстуры
- [ ] Проверить Read/Write у Texture2D: отключать маленькими группами только при отсутствии CPU доступа (GetPixels/SetPixels, LoadRawTextureData, packing, runtime generation и т. п.). Не менять normal/sRGB/alpha.
- [ ] Проверить Read/Write у Mesh: поиск vertices/triangles/GetVertices, CombineMeshes, MeshCollider, Physics.BakeMesh, nonuniform/negative scale, cooking; не отключать у коллизий без доказательства. Учитывать предупреждение Unity о 335 мешах с отключённым pre-baked collision — отдельная совместимость, не массовый переключатель.
- [ ] Mipmaps для 3D сохранить; проверять UI/иконки без mipmaps; texture mip limits/streaming в WebGL проверять на поддержку. Разрешение уменьшать только по screen coverage и визуальному A/B. Отдельный ASTC Mobile кандидат, измерить реальную поддержку устройств, fallback в несжатую память и сборку; Desktop DXT оставить.
- [ ] Проверить неиспользуемые alpha-каналы, normal maps, варианты материалов, реальные ссылки. Сначала отчёт. Изменение альфа может сломать cutout-растительность/стёкла/эмиссию.
- [ ] Не считать одинаковые картинки ненужными дублями: GUID могут использоваться разными объектами, importer settings могут отличаться. Не удалять клоны; если когда-либо согласуем дедупликацию — сначала полный список ссылок, эквивалентность импорта, remap всех ссылок, обратимый коммит, отдельный тест. Shared assets между player и bundle можно дублировать ради корректности.

### P1a — шрифты (низкий приоритет)
- [x] Проверить пять TTF в Assets/Resources/MotorCity/Fonts: RobotoCondensed Regular/Bold (1,019,060 байт суммарно, используются MotorCityTypography), Roboto Regular и Ubuntu Regular/Bold (1,198,596 байт суммарно, кандидаты). GitHub search не обнаружил ссылок на последние три по имени/GUID, но не является доказательством отсутствия зависимостей.
- [x] Запустить Tools > Motor City > Audit > Font Dependencies; отчёт Library/MotorCityFontAudit.txt. Проверить ссылки по AssetDatabase.GetDependencies и динамические загрузки, fallback/локализацию. Аудит ничего не меняет.
- [ ] Только после проверки вынести неиспользуемые TTF вместе с .meta из Resources, не удаляя исходники/лицензии. Сохранить GUID. Сравнить фактический .data.br и интерфейс на всех языках, не ожидать существенного FPS-выигрыша. Проверять Canvas rebuild и обновления текста отдельно.

19.11.2026: локальный Font Audit завершён (COMPLETE), прямых ссылок на три кандидата нет; по именам/GUID и динамическим путям найдены только Roboto Condensed Regular/Bold. Roboto-Regular, Ubuntu-Regular и Ubuntu-Bold перемещены в Assets/ArchivedFonts с исходными .meta и blob SHA. Лицензии не изменены. 1 198 596 байт исходников исключены из Resources; фактический выигрыш и локализация требуют проверки следующей сборки.

### P2 — LOD геометрии
- [ ] Инвентаризация существующих LODGroup и фактического влияния maxLOD=1 на Low/Medium.
- [ ] Пилот одной группы повторяемых статических зданий, затем деревья, фонари/заборы, машины трафика, припаркованные авто. LOD1/2 отдельными render-мешами с сохранением UV/normal/material/submesh, bounds, силуэта. Существующий LOD0 сохранить, не менять геометрию коллизий, размеры, позиции, трафик и дороги.
- [ ] Screen-relative thresholds + hysteresis при необходимости; проверить pop-in, повороты камеры, границы тумана, ночь, разные пресеты, близкие/далёкие объекты. Проверять, что cross-fade не удваивает стоимость слишком долго.
- [ ] Не создавать LOD всему городу разом. Проверить размер отдельного пакета и память: дополнительные LOD тоже занимают место.

### P3 — draw calls и повторяющиеся объекты
- [ ] После отчёта выбрать крупную группу одинаковых mesh+submesh+material (деревья, фонари, знаки, ограждения, здания). Сравнить SRP Batcher baseline и instancing под WebGL2. Enable Instancing не гарантирует GPU batching; учесть shader passes, SRP Batcher precedence, material properties, прозрачность и тени.
- [ ] При реальном выигрыше рассмотреть explicit instancing с ограниченными группами/пространственными ячейками, корректной видимостью и исходными коллайдерами. Не использовать каждую технику одновременно без A/B.
- [ ] Не создавать уникальные renderer.material на каждый экземпляр без необходимости; проверить shared materials и свойства. MaterialPropertyBlock может ухудшить совместимость с SRP Batcher.
- [ ] Static batching/CombineMeshes только после оценки роста памяти, возможности отсечения и статической природы объектов; не объединять весь город.

### P4 — свет, шейдеры, overdraw и разрешение
- [ ] Проверить реальную стоимость SSAO (особенно Low/Medium), HDR, MSAA, постобработки, URP Forward+/Forward, render targets, дополнительные lights. Сравнение в Frame Debugger и release. Не менять все сразу.
- [ ] Тени: Low/Medium уже имеют нулевую дальность, High 40 м; исключать мелкие второстепенные shadow casters выборочно, оставлять главные здания и авто. Не ломать ночные фары, окна и фонари.
- [ ] Simple Lit вместо Lit только на подходящих второстепенных материалах; не заменять оригиналы глобально. Проверить normal/emission, отражения, прозрачность.
- [ ] Прозрачность/overdraw: стёкла, листва, дым, частицы, fog shell; alpha clip вместо blend только если соответствует виду. Уменьшать перекрывающиеся слои, число источников света, расходы эффекта. Эмиссия вместо динамического света — только если не страдает читаемость ночного города.
- [ ] Адаптивный render scale — отдельный опциональный эксперимент с плавными границами, стабильным UI, без GPU-вычислительного backend, без искусственного FPS cap.

### P5 — HLOD и видимость кварталов
- [ ] Использовать существующий CityHlodRuntime как потенциальную основу, но валидировать данные и совместимость с новым split build; baker ранее удалён. Не считать HLOD активным.
- [ ] Пилот одного квартала: оригиналы вблизи, лёгкие прокси вдали, culling за пределами видимости; отсутствие изменений коллайдеров/waypoints/interactive/traffic. Небольшие ячейки вместо одного гигантского меша. Сохранять материалы и вид днём/ночью.
- [ ] Исключить двойное управление forceRenderingOff со стороны существующих cullers, проверять восстановление renderers и устаревшие индексы. HLOD данные/прокси тоже должны приходить после меню; не вернуть город в стартовый player.
- [ ] Occlusion culling: обычный Unity baked occlusion требует совместимых сцен и статических данных; текущий город создаётся в runtime. Только отдельный proof-of-concept с измерением, не включать вслепую.

### P6 — CPU, трафик, физика, анимация, UI
- [ ] Измерить Instantiate/Destroy, GC allocations, количество активных транспортных объектов. Object pooling только там, где есть churn; корректно сбрасывать AI, rigidbody, коллизии, фонари и состояние заданий.
- [ ] AI/physics LOD дальнего трафика: уменьшить частоту дорогостоящих обновлений, сохранив спавн, навигацию, правила движения, отсутствие резких исчезновений. Не трогать физику игрока.
- [ ] Выявить дорогие MeshCollider pairs и cooking, пробовать упрощённые collision meshes только с проверкой дорожных швов, рамп, мостов, путей и границ.
- [ ] Animator culling / update rate для невидимых персонажей; не отключать gameplay-события анимаций. UI Canvas rebuilds, Text layout, избыточные обновления HUD; эффект измерять.

## 5. Референсы и ограничения WebGL

Идеи из Unity Megacity Metro (группировка рендера), Fantasy Kingdom / Unity 6 (повторное использование объектов, GPU Resident Drawer), мобильных проектов типа Rainbow Six Mobile (видимость, память, сложность окружения). Это направления для изучения, не доказательство того, что конкретная коммерческая игра использует каждую технику из списка. WebGL 2 не равен native: GPU Resident Drawer/GPU occlusion и другие compute-зависимые решения не планировать как прямой перенос. Официальная документация для проверки совместимости версий: https://docs.unity3d.com/Manual/ ; https://docs.unity3d.com/Manual/webgl-assetbundles.html ; https://docs.unity3d.com/Manual/urp/urp-feature-list.html . Перед реализацией сверить актуальный Unity API и WebGL ограничения, особенно под Unity 6.6.

## 6. Правила работы, чтобы ничего не сломать

1. Перед изменением проверить HEAD, git status у владельца при необходимости, прочитать реальный код и зависимости. Не затирать пользовательские правки. Существующие авторские FBX, текстуры, префабы и GUID сохранять; не редактировать vendor/model source без причины. Не делать массовых правок Resources, LFS, ProjectSettings и .meta; только точечные согласованные изменения. Не удалять ассеты из-за отсутствия статических ссылок.
2. Один эксперимент — один отдельный коммит с понятным SHA и контрольным сценарием. Опциональный dev/URL A/B switch — если нужен; не оставлять мусорные инструменты и логи в релизе. При регрессе вернуть только изменения эксперимента новым коммитом; не force push. При работе с GitHub использовать актуальный parent/expected SHA.
3. Сравнивать на идентичных условиях и записывать реальные цифры. Не принимать уменьшение raw FBX/packed bytes за HTTP экономию, а уменьшение загрузки за FPS. Внешний вид/ссылки и игровая логика важнее теоретической оптимизации. Если выигрыш в пределах шума — отказаться.
4. Не менять Playgama Bridge startup gate, game_ready, storage keys, облачные сохранения, ads pause/audio flow, язык, управление. Отдельно тестировать рекламу, восстановление прогресса, гараж, задания, навигацию, гонки, дрифт, эвакуатор, трафик, день/ночь и фары. Не сбрасывать прогресс ради эксперимента; не тестировать запись пустых сохранений.
5. Не менять runtime-геометрию дорог/позиции/коллайдеры без специального согласования. Сохранять авторский FCG layout. Предупреждение sharp U-turn detour уже принято владельцем; не исправлять попутно.
6. После каждого шага владелец делает git pull origin main, открывает Unity, проверяет compile, Play Mode и нужные сценарии. Чистая WebGL сборка в отдельной папке, затем браузер, потом Sandbox только после подтверждения владельца. Не заменять работающую сборку без тестов. Не коммитить Library/Builds, архивы, диагностические отчёты с персональными данными, случайные сгенерированные settings.
7. Если скомпилированный WebGL падает — сначала Console/Network и reproduction, не гадать; отделять предупреждение от ошибки, не увеличивать память вслепую. Не утверждать поддержку WebGL технологии, пока она не проверена. Не обещать FPS или сроки без измерения.

## 7. Сборка, проверка и оставшиеся релизные задачи

- [ ] Проверить чистый checkout/main и новый `Tools > Motor City > Build > Build Split City Test` с `Web - Desktop - Release`, Brotli preflight. Обычная Build не равнозначна split. Эксперимент `Measure City AssetBundle` создаёт непригодный stripped player — не публиковать. При прерывании `Restore City After Experiment`; убедиться, что все 4 корня вернулись, version marker отсутствует.
- [ ] Локально: `python Tools/serve_webgl.py --directory "PATH_TO_SPLIT_BUILD"`. Проверить холодный старт, появление меню перед городом, быстрый Continue, intro, медленное соединение, ошибку загрузки/повтор, повторный запуск, паузу/возврат, resize, день/ночь.
- [ ] Проверить, что в StreamingAssets есть один versioned raw .bundle, музыка, нет .bundle.br/.pack или экспериментальных артефактов в публикуемом ZIP. index.html и Bridge файлы в корне, Build/ и TemplateData/ на месте. Упаковать только содержимое нужной папки, не весь проект. Сохранять последний рабочий ZIP для отката.
- [ ] В DevTools Network без cache для холодного измерения и с cache для повторного, проверить timing waterfall, bytes, Content-Encoding, cache. QA Tool https://developer.playgama.com/qa-tool: game_ready, initial download, crash warnings. Не считать отсутствие heavy warning гарантией мобильной пригодности.
- [ ] Проверить Chrome/Firefox/Safari по возможности, Android/iOS реальные устройства, память при создании мира, длительный заезд (утечка/пики/GC), background/foreground, аудио/ads, touch. Проверить мобильный профиль отдельно; не считать desktop DXT универсальным.
- [ ] После локального и hosted QA оформить production split build command / документированный процесс, согласовать публикацию, повторно проверить сохранения/рекламу/громкость, статистику Sandbox и критерии Playgama. Не менять произвольно версию или выпускать в полный каталог.
- [ ] Позднее: оптимизация первого игрового опыта, аналитика входа в вождение/обучения, показатели отказов и playtime, рекламные бонусы и межстраничная реклама; Payments только если отдельно согласовано. Не смешивать с рендер-экспериментами.

## 8. Журнал / следующий запрос

19.11.2026 — раздельная загрузка города подтверждена в Sandbox, QA 21 МБ/3,02 с. managed Gzip отменён. Рендер-аудит исходников показал существующие culling/quality системы и кандидатов на оптимизацию. Фактический полный runtime-аудит не выполнен. Read-only Render Audit завершён владельцем, результаты внесены в P0. Runtime-профилирование остаётся следующим шагом. После запуска владельцем разобрать отчёт, записать данные здесь и выбрать P1/P2/P3/P4 по bottleneck.

Для нового чата достаточно: «Открой G3R-Studio/motor-city, Docs/Roadmap-1.1.md, проверь текущий HEAD и продолжи с точки остановки. Не меняй ассеты без аудита, тестируем по одному эксперименту».

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
