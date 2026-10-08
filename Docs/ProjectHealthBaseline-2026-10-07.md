# Motor City — Phase 0 Baseline

Дата фиксации: **2026-10-07**

Этот документ фиксирует стартовое состояние перед выполнением
`Docs/ProjectHealthRoadmap-2026-10-07.md`.

## 1. Freeze point

- Repository: `G3R-Studio/motor-city`
- Baseline branch source: `main`
- Baseline commit: `5d67e12acec3265b93329e0464c67394ad78385b`
- Commit title: `Add dependency-aware project health roadmap`
- Unity Editor: **6000.6.1f1**
- Unity revision: `7efac9f6c10e`
- Production scene: `Assets/Scenes/Prototype.unity`

Phase 0 не меняет gameplay/runtime behavior. Любые runtime-правки должны
начинаться только после прохождения verification gate ниже.

## 2. Static baseline

Локально из корня проекта выполнить:

```powershell
pwsh ./Tools/check_cleanup.ps1
```

Ожидаемый результат: syntax/source checks проходят без exception.

Затем:

```powershell
python Tools/audit_project.py --output Temp/MotorCityAudit/static-audit.json
```

Сохранить:
- полный stdout;
- `Temp/MotorCityAudit/static-audit.json`;
- количество duplicate GUID;
- количество dangling deleted assets;
- список первых подозрительных unreferenced candidates только как audit data,
  а не как список на удаление.

После будущих cleanup-коммитов запускать GUID audit относительно freeze point:

```powershell
python Tools/audit_project.py --baseline 5d67e12acec3265b93329e0464c67394ad78385b --output Temp/MotorCityAudit/static-audit-after.json
```

## 3. Unity Editor baseline

Открывать проект только в Unity **6000.6.1f1**.

### 3.1 Console baseline

1. Открыть `Assets/Scenes/Prototype.unity`.
2. Дождаться полного импорта/компиляции.
3. Открыть **Window -> General -> Console**.
4. Нажать **Clear**.
5. Убедиться, что после компиляции нет новых красных compile errors.
6. Зафиксировать количество Errors / Warnings до Play Mode.
7. После каждого smoke-pass сохранять скрин Console.

### 3.2 Project/scene dependency audit

В открытой `Prototype.unity` выполнить:

**Motor City -> Audit -> Write project and loaded scene report**

Результат:
`Temp/MotorCityAudit/project-audit.txt`

Проверить в отчёте:
- нет `ERROR missing prefab scripts`;
- нет `ERROR: ... missing scripts` в loaded scene;
- production build scene существует;
- повторяющиеся MonoBehaviour `REVIEW repeated component` изучить отдельно,
  но не удалять автоматически.

### 3.3 Material baseline

В открытой `Prototype.unity` выполнить:

**Motor City -> Diagnostics -> Audit Materials In Open Scene**

Зафиксировать:
- Missing material slots;
- Unsupported/null-shader materials;
- все warnings с hierarchy path.

Затем выполнить:

**Motor City -> Diagnostics -> Audit All Project Materials**

Зафиксировать:
- Materials scanned;
- Null shaders;
- Unsupported shaders.

Любая найденная проблема должна быть записана как baseline-existing issue,
чтобы после следующих phase не принять старый дефект за регрессию.

### 3.4 Build dependency baseline

Выполнить:

**Tools -> Motor City -> Build -> Generate Dependency Report**

Результат:
`Library/MotorCityBuildDependencyReport.txt`

Сохранить:
- Enabled build scenes;
- Unique scene dependencies;
- Raw dependency file size;
- секцию FCG / CITY;
- секцию PLAYER VEHICLES;
- top largest dependency files.

## 4. Play Mode smoke baseline

Запустить Play Mode из `Prototype.unity`.

Пройти без пропусков:

1. Главное меню.
2. Выбор управления.
3. Вход в город.
4. Газ / тормоз / руль.
5. Ручник.
6. Пауза.
7. Главное меню из паузы.
8. Continue.
9. Убедиться, что после Continue снова появляется ожидаемый экран выбора управления.
10. Войти в гараж.
11. Переключить каждую доступную машину.
12. Проверить body paint.
13. Проверить колёса/rims.
14. Проверить neon.
15. Вернуться в город.
16. Проверить headlights.
17. Проверить brake/rear lights.
18. Проверить визуал в ночном режиме.
19. Сохранить/выйти.
20. Повторно запустить Play Mode и проверить восстановление состояния.

## 5. Vehicle visual snapshot

Для **каждой машины** сделать минимум три baseline-скрина:

1. Garage — стандартный ракурс.
2. Day — в городе.
3. Night — headlights + rear/brake light visible.

Отдельно записывать:
- body material визуально корректен;
- glass не стал прозрачным/голубым/неестественно синим;
- mirror визуально корректен;
- wheels находятся на правильных местах;
- headlights/rear lights используют правильные material slots;
- body customization меняет только кузов, а не glass/lights/misc.

Эти скрины являются visual truth для Phase 3/4.

## 6. Steering / physics baseline

Проверить минимум:

### Keyboard
- straight acceleration;
- full left/right steering;
- steering release;
- brake;
- handbrake;
- reverse.

### Touch arrows
- удержание steering;
- быстрые left/right taps;
- одновременный gas + steer;
- brake + steer;
- handbrake.

### Touch wheel
- медленный поворот;
- быстрый поворот;
- удержание угла;
- отпускание;
- проверить наличие дрожания руля;
- проверить, не скачет ли steering value около центра.

Для каждого режима записать:
- есть/нет jitter;
- есть/нет input lag;
- есть/нет самопроизвольный steering;
- поведение после Pause/Resume.

## 7. Save/restart baseline

Проверить отдельно:

1. Fresh save.
2. Изменить машину/цвет/настройку управления.
3. Выйти в главное меню.
4. Continue.
5. Перезапустить Play Mode.
6. Убедиться, что ожидаемые persistent настройки восстановились.
7. Выполнить QA reset только в Editor/QA окружении и убедиться,
   что reset действительно очищает ожидаемые данные.

## 8. WebGL baseline

До Phase 1 нужен хотя бы один Development WebGL baseline build.

Проверить:
- compile/build success;
- загрузка сцены;
- меню;
- управление;
- garage;
- каждая машина;
- pause -> main menu -> Continue;
- save/restart;
- материалы;
- headlights/rear lights;
- Console browser/runtime errors.

Release WebGL baseline особенно важен для Phase 1, потому что именно там
будет удаляться production exposure QA/Admin функций.

## 9. Captured baseline results — 2026-10-08

Получены Unity-generated отчёты с baseline `main`.

### Project audit

- Imported project assets: **2007**
- Enabled build scene: `Assets/Scenes/Prototype.unity`
- Existing source-prefab Missing Script:
  - `Assets/Fantastic City Generator/Roads/Prefab/Double-Block-09.prefab`
  - `Double-Block-09/Meshes/Water`: 1 missing script
  - `Double-Block-09/Meshes/Water-B`: 1 missing script
- Edit-mode loaded `Prototype`: **0 scripts, 0 lights, 0 renderers**

The `Double-Block-09.prefab` GUID is
`117dc5da96c6fef48a8f4f0f03a504b9`. Static repository search finds its
direct serialized consumer in `Assets/Fantastic City Generator/Generate.prefab`.
Do not remove or repair the components blindly: first verify the runtime baked
city in Play Mode.

Because `Prototype` has zero renderers in Edit Mode, the open-scene material
audit before Play Mode does not validate the dynamically installed city and
vehicle renderers. Repeat **Motor City -> Diagnostics -> Audit Materials In Open Scene**
while Play Mode is running and the city/vehicle have finished loading.

### Material audit

Open scene material audit:
- Missing material slots: **0**
- Unsupported/null-shader materials: **0**

All-project material audit:
- Materials scanned: **766**
- Null shaders: **0**
- Unsupported shaders: **0**

Result: **PASS**, with the runtime Play Mode re-check still required because the
edit-mode scene itself contains no renderers.

### Runtime Play Mode audit

Runtime audit captured after the dynamically installed city/vehicle loaded:

- Prototype scripts: **3479**
- Prototype lights: **788**
- Prototype renderers: **22597**
- No additional loaded-scene Missing Script errors were reported.
- The two FCG `Double-Block-09` Missing Script entries remain source-prefab baseline issues.

Runtime material audit found a confirmed vehicle-cache lifecycle issue.

Observed while switching vehicles:

- first audit: **4 missing slots** in cached AmgGT legacy rear-lamp overlays;
- next audit: **6 missing slots** after cached Bus was added;
- next audit: **8 missing slots** with cached Delorean legacy rear-lamp overlays;
- affected paths are all below
  `MotorCityVehicleVisual_Runtime_Cached_.../MotorCityRearLampEmission`.

Confirmed lifecycle:

1. `ArcadeRacingCarRuntimeInstaller.ClearRuntimeVisual()` deactivates the
   current vehicle and preserves it in `RuntimeVehicleVisualCache`, renaming it
   to `MotorCityVehicleVisual_Runtime_Cached_<resource>`.
2. `VehicleRosterSystem.ApplySelectedVehicle()` installs/activates the next
   visual, then calls `PlayerVehicleRearEmission.SetVehicleId()`.
3. `SetVehicleId()` calls `RefreshVisual()`.
4. `RefreshVisual()` calls `RestoreAndClearBindings()` before resolving the
   newly active visual.
5. The old legacy overlay GameObjects are not tracked as removable overlay roots.
   Their Materials are tracked in `runtimeMaterials` / bindings and are
   destroyed.
6. Because the old visual has already been renamed/deactivated into the cache,
   `RemoveLegacyOverlays()` runs against the new active visual, not the old
   cached visual.
7. The cached vehicle therefore keeps `MotorCityRearLampEmission` renderers
   whose material references now resolve as destroyed/null.

There is also a policy contradiction in `PlayerVehicleRearEmission`:
`EnableMeshLampOverlays = false`, but the zero-binding fallback still calls
`CreateTexturedRearLampOverlay()`, and that method itself does not check the
flag.

Status: **CONFIRMED BASELINE RUNTIME ISSUE** affecting multiple cached vehicles,
not a Bus-prefab defect. Do not hand-edit vehicle prefabs/materials to hide it.
The future fix belongs in `PlayerVehicleRearEmission` lifecycle/fallback logic
and must be verified across all cached vehicle switches.

### Runtime smoke observation — no regressions

Manual Play Mode observation after the runtime audits:

- no red Console errors observed;
- no visible gameplay/visual breakages observed during the current smoke pass;
- confirmed cached rear-lamp missing-material warnings remain the only known
  runtime material/lifecycle defect from this pass.

This is recorded as a baseline observation only. Save/restart and WebGL gates
remain separate and must still be completed explicitly.

### Build dependency report

- Enabled build scenes: **1**
- Scene: `Assets/Scenes/Prototype.unity`
- Direct scene dependencies reported: **1**
- Direct scene raw size: **3.58 KiB**
- FCG / CITY roots: **1**
- FCG / CITY unique dependencies: **526**
- FCG / CITY raw dependency size: **339.64 MiB**
- `CityVisual.prefab`: **99.47 MiB**
- PLAYER VEHICLES roots: **9**
- PLAYER VEHICLES unique dependencies: **41**
- PLAYER VEHICLES raw dependency size: **2.31 MiB**

The scene dependency count of 1 is expected to be incomplete for runtime-loaded
content: Motor City installs major content through Resources/runtime bootstrap.
Use the dedicated FCG/CITY and PLAYER VEHICLES sections for cleanup decisions,
not the scene-only count.

## 10. Baseline result table

| Gate | Result | Notes |
| --- | --- | --- |
| Unity compile | PENDING | |
| Console before Play | PASS | No red errors reported during the current Unity run |
| Project audit missing scripts | BASELINE ISSUE | 2 missing components in FCG Double-Block-09 Water/Water-B |
| Scene material audit | CONFIRMED BASELINE ISSUE | Missing slots grow 4 -> 6 -> 8 across cached AmgGT/Bus/Delorean legacy rear-lamp overlays |
| All-project material audit | PASS | 766 materials, 0 null shaders, 0 unsupported shaders |
| Build dependency report | PASS | City 339.64 MiB / 526 deps; vehicles 2.31 MiB / 41 deps |
| Runtime scene audit | PASS WITH BASELINE ISSUES | 3479 scripts / 788 lights / 22597 renderers; no runtime scene missing-script additions; no red runtime errors observed |\n| Main menu flow | PENDING | |
| Pause -> Main Menu -> Continue | PENDING | |
| Every vehicle visual | PENDING | |
| Keyboard controls | PENDING | |
| Touch arrows | PENDING | |
| Touch wheel | PENDING | |
| Save/restart | PENDING | |
| WebGL Development | PENDING | |
| WebGL Release | PENDING | |

## 11. Phase 0 exit criteria

Phase 0 можно считать закрытым, когда:

- static checks сохранены;
- Unity compile clean либо все существующие ошибки зафиксированы как baseline;
- project audit сохранён;
- material audit сохранён;
- dependency report сохранён;
- Play Mode smoke выполнен;
- screenshots всех машин сохранены;
- steering/physics notes сохранены;
- save/restart проверен;
- WebGL baseline зафиксирован.

Только после этого начинать **Phase 1 — Release safety**.
