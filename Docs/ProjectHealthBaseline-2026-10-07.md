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

## 9. Baseline result table

Заполняется после ручного Unity прохода.

| Gate | Result | Notes |
| --- | --- | --- |
| Unity compile | PENDING | |
| Console before Play | PENDING | |
| Project audit missing scripts | PENDING | |
| Scene material audit | PENDING | |
| All-project material audit | PENDING | |
| Build dependency report | PENDING | |
| Main menu flow | PENDING | |
| Pause -> Main Menu -> Continue | PENDING | |
| Every vehicle visual | PENDING | |
| Keyboard controls | PENDING | |
| Touch arrows | PENDING | |
| Touch wheel | PENDING | |
| Save/restart | PENDING | |
| WebGL Development | PENDING | |
| WebGL Release | PENDING | |

## 10. Phase 0 exit criteria

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
