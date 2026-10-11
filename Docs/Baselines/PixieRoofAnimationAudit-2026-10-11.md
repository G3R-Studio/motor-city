# Пикси на крыше: позы и визуальный контракт (2026-10-11)

**Текущая UX-1 итерация. Проверка по исходникам; Unity Editor Play Mode ожидается.**

## Запрос

Пикси находится **над центром крыши** каждого автомобиля, не сбоку. Во время езды **нет ни бега, ни ходьбы на месте** у обычного и supporter-скина. Вместо локомоции используется последовательность спокойных поз и краткая радостная реакция.

## Положение и движение

- `PixieCompanionSystem.ResolveVisualAnchor()`: `(bounds.center.x, bounds.max.y + 0.12 м, bounds.center.z)` в локальных координатах машины. Основа — **только** authored vehicle body bounds; ограничители геометрии и fallback BoxCollider оставлены.
- `LateUpdate()`: маленькое вертикальное покачивание `sin(t * 1.65) * 0.055 м`. Изменения высоты не участвуют в physics/raycast. Следование кадр-в-кадр и коррекция при телепорте сохранены.
- `externalVisual`: scale обычного 0.66, эксклюзивного 0.58, Y-rotation 180° и `Animator.applyRootMotion = false` — **не менялись**.

## Реально подключённые клипы и состояния

| Событие | Стандартный HAON (модель Misaki) | Эксклюзивный SapphiArt |
| --- | --- | --- |
| Поза 1 | `Pixie Idle` → `StandA_idleA.FBX` | `idle.anim` |
| Поза 2 | `Pixie Roof Pose` → **`StandB.FBX` (новое подключение)** | `idle.anim` |
| Поза 3 | `Pixie Idle Alt` → `StandC.FBX` | **`default.anim`** |
| Спокойное движение по городу | Цикл трёх поз с интервалом 6.3 с | Чередование `idle` и `default` |
| Машина стоит | Те же позы с интервалом 7.4 с | Те же позы |
| Редкая радость во время езды | `Emotion_Clap(StandC).FBX` (1.7 с раз в 4 позиции) | `winpose.anim` |
| Завершение активности | `Emotion_Clap(StandC).FBX` | `winpose.anim` |
| Получен уровень | `VictoryPose_UTCStyleSpin.fbx` | `winpose.anim` |
| Буст Пикси | `Jump_One-cycle-Type(root).FBX` | `jump.anim` |
| Бег/ходьба при движении | **Отключены**. `WalkA_Front.FBX` не входит в controller | **Отключены**. Параметр `param_idletorunning` всегда false; нет маппинга на `running` |

У обычного скина исходная библиотека HAON содержит и `WalkA_Front.FBX`; файл **не удалён** (могут использовать другие ассеты), но не связан с runtime Animator Пикси.

У эксклюзивного исходный `SapphiArtchanAnimController` также содержит `walk`, `running`, `hit01/02/03`, `damage`, `KO_big`; они **не используются** Пикси. Не менять сторонний исходный контроллер, вместо этого runtime сообщает `param_idletorunning = false` и не запрашивает locomotion.

## Изменённые файлы

- `Assets/Scripts/Gameplay/PixieCompanionSystem.cs`: якорь по центру крыши, hover, ambient state cycle, маппинг эксклюзивного скина, реакции.
- `Assets/Editor/HaonByteVisualImporter.cs`: генерация нового состояния `Pixie Roof Pose` на основе StandB, без WalkA.
- `Assets/Resources/MotorCity/Byte/HaonPixie.controller`: committed Animator с изменённым клипом/состоянием — **ручной rebuild не требуется**.
- `Tools/check_pixie_roof_animation.py`: регрессионные source checks, добавлены к *ручным* Source Gates.

## Ручная проверка (ожидается)

- [ ] `git pull`, Unity Editor compile без ошибок.
- [ ] Обычный Пикси расположен посередине крыши разных машин; не проваливается и не закрывает критические элементы камеры.
- [ ] Три позы стандартного скина, короткая реакция при движении; **без бегущих ног**.
- [ ] Эксклюзивный скин: `idle` / `default` / `winpose` без `running`, в том числе после переключения скина на ходу.
- [ ] Реакции на завершение активности, получение уровня и буст не потеряны.
- [ ] Не сломались старые уровни, XP и save/skin entitlements.
- [ ] Если у конкретной машины геометрический центр не совпадает с крышей — корректировать только offset якоря после визуальной проверки, не переписывать bounds всей машины.

**GitHub Actions / WebGL:** автоматические триггеры отключены; WebGL до финального UX-7 не собирать.
