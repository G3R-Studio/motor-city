# Motor City — Gameplay & UX Polish Roadmap

> Этот документ фиксирует согласованный план ближайшей переработки игры.  
> Он предназначен как рабочая карта: по нему можно последовательно открывать нужные файлы, понимать текущую систему, вносить изменения и проверять регрессии.
>
> **Важно:** полную новую архитектуру автомобилей пока не строим, потому что модели машин будут ещё заменяться/переделываться. Ниже допускаются только безопасные технические улучшения, которые переживут замену моделей.

---

# 0. Порядок выполнения

Рекомендуемый порядок:

1. **Камера на скорости**
2. **Звуки переключения передач и сброса газа**
3. **Осмотр машины в гараже мышью/пальцем**
4. **Общий UI-kit для модальных окон**
5. **Перенос паузы и стандартных окон на общий UI-kit**
6. **Новый интерактивный первый запуск**
7. **Машина игрока в главном меню**
8. **Технический аудит взаимного влияния систем и magic-checks**

Почему именно так:

- камера и звук можно улучшать почти независимо;
- гаражное вращение опирается на уже существующую ChaseCamera;
- UI-kit лучше сделать до нового tutorial, чтобы обучение не пришлось потом переписывать второй раз;
- главное меню с автомобилем логичнее делать после стабилизации кастомизации и presentation-логики;
- технический pass проводится последним, когда основные новые связи уже понятны.

---

# 1. Камера: динамический FOV и лёгкое отставание в поворотах

## Цель

Убрать нынешнее ощущение, что на большой скорости камера физически “отстаёт” от автомобиля.

На скорости машина должна оставаться примерно одного размера на экране, а чувство скорости должно формироваться в основном через:

- плавное увеличение FOV;
- небольшой look-ahead;
- лёгкое угловое запаздывание камеры в повороте;
- отдельное поведение при дрифте.

## Что есть сейчас

Основной файл:

- `Assets/Scripts/Camera/ChaseCamera.cs`

Ключевые поля:

- `distance`
- `positionSharpness`
- `rotationSharpness`
- `speedLookAhead`
- `speedDistanceBonus`
- `baseFieldOfView`
- `highSpeedFieldOfView`
- `fieldOfViewSharpness`
- `driftLookInfluence`
- `maxDriftLookAngle`

Ключевой метод:

- `LateUpdate()`

Сейчас внутри `LateUpdate()`:

- считается `speed01`;
- затем:
  - `dynamicDistance = distance + speedDistanceBonus * speed01`
  - `dynamicLookAhead` увеличивается со скоростью;
  - FOV плавно идёт от `baseFieldOfView` к `highSpeedFieldOfView`.

Именно `speedDistanceBonus` создаёт заметное физическое увеличение дистанции до машины.

## Что менять

### Шаг 1 — почти убрать физическое отдаление на скорости

В `ChaseCamera.cs`:

- либо полностью удалить скорость из расчёта `dynamicDistance`;
- либо оставить очень маленький бонус, только чтобы камера не казалась “прилипшей”.

Рекомендуемая логика:

```csharp
float dynamicDistance = distance;
```

или очень слабая:

```csharp
float dynamicDistance = distance + smallSpeedDistanceBonus * speed01;
```

где бонус не больше примерно 0.2–0.4 м.

### Шаг 2 — сделать FOV главным эффектом скорости

Оставить:

- `baseFieldOfView`
- `highSpeedFieldOfView`

Но проверить диапазон.

Стартовая рекомендация:

- базовый FOV: ~62
- высокий FOV: ~76–78

Не ставить слишком высокий FOV, чтобы не получить “GoPro”-эффект.

### Шаг 3 — разделить вход и выход FOV

Сейчас используется один `fieldOfViewSharpness`.

Лучше добавить:

- `fieldOfViewGainSharpness`
- `fieldOfViewReturnSharpness`

Чтобы:

- при ускорении FOV чуть быстрее расширялся;
- при сбросе скорости возвращался мягче.

### Шаг 4 — добавить лёгкое запаздывание камеры при повороте

Не делать это через физическое отставание позиции.

Добавить отдельный угол:

- `turnLagYaw`
- `turnLagTarget`

Источник:

- steer input;
- или угловая скорость автомобиля;
- или комбинация.

Предпочтительно использовать реальное вращение автомобиля между кадрами, а не только input, потому что машина может скользить.

Логика:

1. получить изменение yaw автомобиля;
2. преобразовать его в ограниченный противоположный camera offset;
3. плавно фильтровать;
4. добавить к итоговому yaw камеры.

Ограничение:

- примерно 3–8 градусов;
- не больше, чтобы камера не выглядела “сломавшейся”.

### Шаг 5 — не смешивать с drift look

Существующая логика:

- `car.SlipAngleDegrees`
- `driftLookInfluence`
- `maxDriftLookAngle`

должна остаться отдельной.

Поворот камеры и дрифт — два разных эффекта:

- turn lag = вес/инерция камеры;
- drift look = камера смотрит туда, куда движется автомобиль.

## Что ещё смотреть

- `Assets/Scripts/Vehicle/ArcadeCarController.cs`
  - `SpeedKph`
  - steering state
  - доступные данные о yaw/angular velocity
- `Assets/Scripts/Input/MotorCityInput.cs`
  - если понадобится прямое значение steering input
- `Assets/Scripts/Bootstrap/MotorCityBootstrap.cs`
  - начальный `camera.fieldOfView`

## Риски

- камера может начать раскачиваться;
- turn lag может конфликтовать с ручной orbit-камерой;
- при дрифте могут суммироваться слишком большие углы;
- после телепорта камера не должна пытаться интерполировать старый yaw.

## Обязательные тесты

- 0–30 км/ч;
- 60–100 км/ч;
- 150+ км/ч;
- резкий разгон;
- резкое торможение;
- плавный поворот;
- резкий поворот;
- ручник/дрифт;
- orbit мышью;
- orbit пальцем;
- телепорт;
- гаражный режим;
- камера рядом со стеной.

## Критерий готовности

- машина визуально почти не уменьшается на высокой скорости;
- скорость чувствуется через FOV;
- камера слегка “весит” в поворотах, но не болтается;
- дрифт остаётся читаемым;
- нет конфликтов с ручной orbit-камерой.

---

# 2. Звук: переключения передач и сброс газа

## Цель

Добавить ощущение настоящего разгона без изменения физики автомобиля.

Пока коробка передач нужна прежде всего как **аудиосимуляция**, а не как новая физическая трансмиссия.

## Основной файл

- `Assets/Scripts/Vehicle/PlayerVehicleAudio.cs`

## Что есть сейчас

В `Update()`:

- считается `speed01`;
- читается `AverageWheelRpm`;
- строится `rpm01`;
- из этого считается `engineLoad01`;
- звук двигателя меняет:
  - volume;
  - pitch.

Газ определяется через:

- `MotorCityInput.ThrottleHeld`
- `MotorCityInput.ReverseHeld`

То есть уже есть хорошая база для виртуальной коробки.

## Что добавить

### Виртуальное состояние двигателя

Добавить поля:

- `currentGear`
- `simulatedRpm01`
- `previousThrottle`
- `shiftTimer`
- `isShifting`
- `lastShiftTime`

### Аудиопрофиль передач

Внутри существующего engine profile или рядом добавить:

- количество передач;
- speed threshold на каждую;
- shift duration;
- shift pitch drop;
- shift volume dip;
- минимальный интервал между переключениями;
- hysteresis для downshift.

Важно: не делать одну жёсткую таблицу на все машины, если текущий `PlayerVehicleAudio` уже имеет per-vehicle profiles.

### Логика upshift

Когда текущая виртуальная передача дошла до верхнего диапазона:

1. стартует shift state;
2. pitch кратко снижается;
3. volume слегка падает;
4. currentGear увеличивается;
5. после shift duration звук возвращается.

### Логика downshift

При сильном снижении скорости:

- не переключать передачу ровно на том же пороге, что и вверх;
- использовать hysteresis.

Иначе около границы коробка будет дёргаться:

`3 → 4 → 3 → 4`.

### Звук переключения

Нужно проверить/добавить ресурс:

- вероятное место:
  - `Assets/Resources/MotorCity/Audio/`

Если подходящего файла нет:

- сначала написать поддержку отдельного `AudioClip shiftClip`;
- не подменять случайным неподходящим звуком.

### Сброс газа

Отслеживать переход:

```
previousThrottle == true
currentThrottle == false
```

При сбросе:

- кратко менять engine tone;
- проигрывать отдельный throttle-off clip, если есть;
- можно кратко снижать low-pass cutoff/volume;
- не добавлять пока агрессивные хлопки выхлопа без отдельного решения.

## Что ещё смотреть

- `Assets/Scripts/Vehicle/ArcadeCarController.cs`
  - `AverageWheelRpm`
  - `SpeedKph`
- `Assets/Scripts/Input/MotorCityInput.cs`
- `Assets/PROMETEO - Car Controller/Scripts/PrometeoCarController.cs`
  - только для понимания исходной физики, не использовать его встроенную аудиосистему как вторую параллельную систему
- `Assets/Resources/MotorCity/Audio/`
  - существующие клипы

## Риски

- слишком частые переключения;
- pitch скачет при wheelspin;
- reverse может ошибочно восприниматься как обычная передача;
- звук продолжает переключаться в гараже;
- WebGL может требовать осторожной загрузки AudioClip.

## Тесты

- плавный разгон;
- полный газ;
- отпускание газа;
- торможение;
- движение назад;
- пробуксовка;
- прыжок/разгрузка колёс;
- пауза;
- гараж;
- WebGL.

## Критерий готовности

- разгон слышится ступенчато;
- shift не раздражает;
- сброс газа ощущается отдельно;
- физика машины не меняется.

---

# 3. Гараж: осмотр машины мышью и пальцем

## Цель

В гараже игрок должен иметь возможность свободно осмотреть машину.

Визуально это может выглядеть как “вращение машины”, но технически безопаснее вращать камеру вокруг неё.

## Основные файлы

- `Assets/Scripts/Camera/ChaseCamera.cs`
- `Assets/Scripts/Gameplay/GarageUpgradeSystem.cs`
- `Assets/Scripts/UI/GarageView.cs`
- `Assets/Scripts/UI/GarageReferenceLayout.cs`

## Что уже есть

`ChaseCamera` уже поддерживает:

- mouse orbit;
- touch orbit;
- zoom;
- `garageMode`;
- отдельный garage FOV;
- garage framing.

`GarageUpgradeSystem.SetGaragePresentationSystems()` включает:

```csharp
chaseCamera.SetGarageMode(true, ...);
```

## Что менять

### Шаг 1 — отдельное поведение orbit в garageMode

В `ChaseCamera`:

- при `garageMode == true` не использовать обычный auto-recenter;
- горизонтальная orbit остаётся там, где игрок её отпустил;
- вертикальный диапазон сделать отдельным и более узким.

### Шаг 2 — touch/mouse должны игнорировать UI

Уже есть `EventSystem` / raycast logic.

Проверить:

- касание началось на UI → не забирать его под orbit;
- drag кнопки не вращает камеру.

### Шаг 3 — горизонтальный осмотр

Основное управление:

- ЛКМ drag — осмотр;
- touch swipe — осмотр.

Правую кнопку мыши в garageMode можно заменить на обычную ЛКМ, если сейчас orbit требует ПКМ.

### Шаг 4 — zoom

Оставить mouse wheel.

Pinch можно добавить отдельно, если будет нужно.

### Шаг 5 — смена автомобиля

При выборе следующей машины:

- камера не должна резко сбрасываться в другой угол;
- новый автомобиль должен появляться в том же пользовательском ракурсе.

## Риски

- UI крадёт drag;
- orbit конфликтует с кнопками смены машины;
- камера может залезать в стены гаража;
- разные размеры машин могут плохо помещаться в кадр.

## Тесты

- desktop mouse;
- touch;
- нажать/потянуть по кнопкам;
- сменить автомобиль во время повернутой камеры;
- менять цвет/диски/неон;
- открыть/закрыть гараж несколько раз.

## Критерий готовности

- осмотр машины ощущается естественно;
- UI не мешает;
- физика авто не вращается;
- состояние камеры не скачет после кастомизации.

---

# 4. UI-kit: единая система стандартных окон

## Цель

Убрать ручное позиционирование каждой кнопки и предотвратить проблемы вроде элементов, вылезающих за рамки окна.

## Основные файлы для изучения

- `Assets/Scripts/UI/PauseMenuView.cs`
- `Assets/Scripts/UI/PrototypeHud.cs`
- `Assets/Scripts/UI/ActivityResultView.cs`
- `Assets/Scripts/UI/NavigatorView.cs`
- `Assets/Scripts/UI/TouchControlsView.cs`
- `Assets/Scripts/UI/GarageView.cs`
- `Assets/Scripts/UI/GarageReferenceLayout.cs`
- `Assets/Scripts/UI/MotorCityButtonVisuals.cs`
- `Assets/Scripts/UI/UiButtonFeedback.cs`
- `Assets/Scripts/UI/MotorCityUiThemeAssets.cs` (если присутствует)
- `Assets/Scripts/UI/HudVisualPolish.cs`

## Что происходит сейчас

Многие элементы создаются вручную через:

- `CreatePanel(...)`
- `CreateText(...)`
- `CreatePauseButton(...)`
- абсолютные `anchoredPosition`
- абсолютные `sizeDelta`.

Это приводит к хрупкости layout.

## Что создать

### MotorCityModalWindow

Ответственность:

- backdrop;
- modal panel;
- title;
- content area;
- стандартные padding;
- безопасная внутренняя ширина.

Не должен знать логику конкретного окна.

### MotorCitySettingsRow

Стандартная строка:

- label;
- value;
- minus;
- plus;
- optional toggle.

Использовать прежде всего в pause/settings.

### MotorCityPrimaryButton

Единый стиль:

- фон;
- рамка;
- высота;
- текст;
- pressed/highlighted;
- feedback.

### MotorCityButtonRow

Контейнер, который:

- принимает 1–3 кнопки;
- сам делит доступную ширину;
- использует spacing;
- не требует ручных `x = ±121`.

## Как переносить

Не переписывать весь UI сразу.

Порядок:

1. pause menu;
2. front-end settings;
3. control choice;
4. result window;
5. небольшие стандартные modal окна.

Гараж пока не унифицировать полностью: у него отдельный reference layout.

## Layout правила

Вместо случайных чисел в каждом экране ввести общие константы:

- modal padding;
- row height;
- row spacing;
- button height;
- button spacing;
- title margin;
- small/medium/large modal width.

## Тестовые разрешения/aspect

Обязательно:

- 16:9
- 16:10
- 4:3
- 20:9
- узкий mobile viewport

## Критерии

- стандартные окна строятся из общих компонентов;
- кнопки не выходят за рамки;
- изменение ширины modal не требует ручного пересчёта каждой кнопки;
- визуальный стиль игры сохраняется.

---

# 5. Пауза и стандартные интерфейсы

## Цель

После создания UI-kit перевести существующие окна на него.

## Первый кандидат

- `Assets/Scripts/UI/PauseMenuView.cs`

Сейчас там вручную задаются:

- panel size;
- quality/audio/music cards;
- позиции кнопок;
- нижние action buttons.

## Переработка Pause Menu

Структура:

```
ModalWindow
 ├─ Title
 ├─ SettingsRow: Graphics
 ├─ SettingsRow: Sound
 ├─ SettingsRow: Music
 └─ ButtonRow
     ├─ Resume
     └─ Main Menu
```

После этого окно должно автоматически держать одинаковые:

- горизонтальные поля;
- vertical spacing;
- ширину кнопок.

## Следующие окна

После паузы найти аналогичные ручные modal layouts в:

- `MotorCityFrontEndFlow.cs`
- `ActivityResultView.cs`
- `NavigatorView.cs`
- других UI partial-классах `PrototypeHud`.

## Не менять

Пока не трогать полностью:

- уникальную компоновку GarageReferenceLayout;
- HUD driving controls;
- minimap.

---

# 6. Новый первый запуск / интерактивное обучение

## Цель

Заменить большой объём вступительного текста на короткое обучение внутри игры:

**газ → тормоз → поворот → доехать до гаража → изменить машину → первая короткая гонка**

## Главный файл

- `Assets/Scripts/Gameplay/FirstSessionOnboardingSystem.cs`

## Связанные файлы

- `Assets/Scripts/UI/MotorCityFrontEndFlow.cs`
- `Assets/Scripts/UI/DrivingHudView.cs`
- `Assets/Scripts/UI/CharacterMissionCardView.cs`
- `Assets/Scripts/UI/NavigatorView.cs`
- `Assets/Scripts/UI/GarageView.cs`
- `Assets/Scripts/Gameplay/GarageUpgradeSystem.cs`
- `Assets/Scripts/Gameplay/ActivityManager.cs`
- `Assets/Scripts/Gameplay/DeliveryActivity.cs`
- `Assets/Scripts/Gameplay/StreetSprintActivity.cs` или другой файл текущей короткой гонки
- `Assets/Scripts/Gameplay/ResultNextGoalResolver.cs`
- `Assets/Scripts/Localization/MotorCityLocalization.cs`
- `Assets/Scripts/Bootstrap/MotorCityBootstrap.cs`

## Что есть сейчас

`FirstSessionOnboardingSystem` уже имеет step-based state.

Текущая последовательность примерно:

0. throttle
1. steering
2. проехать 80 м
3. reward/Turbo handoff
4. первая activity
5. гараж
6. кастомизация
7. завершение

Также `MotorCityFrontEndFlow` содержит 6 больших intro slides.

## Новая последовательность

### Step 0 — Газ

Условие:

- реальный throttle input.

Не считать просто движение автомобиля.

### Step 1 — Тормоз

Добавить отдельный шаг.

Условие:

- машина реально движется;
- игрок нажал brake/reverse/braking control в корректном контексте.

Нужно внимательно проверить, как в текущей игре разделены:

- reverse;
- brake;
- touch brake pedal.

### Step 2 — Поворот

Условие:

- steering input;
- машина движется.

### Step 3 — Доехать до гаража

Вместо “просто проехать 80 м”:

- дать objective на garage marker;
- Navigator/HUD должны ясно показать точку;
- завершение по открытию гаража или входу в garage trigger.

### Step 4 — Изменить машину

В гараже:

- запомнить состояние кастомизации до шага;
- любое реальное изменение:
  - кузов;
  - диски;
  - неон;
- засчитывает шаг.

### Step 5 — Первая короткая гонка

После выхода из гаража:

- предложить специальную beginner race;
- она должна быть короткой и простой;
- tutorial заканчивается по успешному финишу.

## Intro slides

`MotorCityFrontEndFlow.cs` сейчас содержит массив `slides`.

При новой схеме:

- новые игроки не должны быть вынуждены читать все 6 слайдов перед управлением;
- старые сюжетные тексты можно:
  - убрать из обязательного flow;
  - или оставить отдельным optional presentation;
  - или разнести короткими репликами по tutorial.

Не удалять контент без решения, если он может понадобиться позже.

## Сохранение

Существующие ключи:

- `MotorCity.Onboarding.Complete`
- step key внутри onboarding system.

Нужно сохранить совместимость.

Существующие игроки с complete не должны запускать tutorial заново.

## Локализация

Все новые подсказки добавить в:

- `Assets/Scripts/Localization/MotorCityLocalization.cs`

Для каждого шага:

- RU;
- EN;
- варианты keyboard/arrows/wheel при необходимости.

## Риски

- существующие saves;
- tutorial может застрять;
- garage flow может конфликтовать с activity manager;
- реклама не должна перебивать rookie path;
- touch control choice должен быть сделан до первого задания.

## Тесты

- полностью новый save;
- существующий save;
- keyboard;
- arrows;
- wheel;
- выйти в главное меню в середине tutorial;
- перезапустить игру между каждым шагом;
- зайти в garage;
- изменить только диски;
- изменить только neon;
- закончить first race.

## Критерий готовности

Новый игрок за первые минуты:

1. едет;
2. тормозит;
3. поворачивает;
4. понимает навигацию;
5. находит гараж;
6. меняет машину;
7. проходит первую гонку.

Без обязательного чтения длинной инструкции.

---

# 7. Машина игрока в главном меню

## Цель

Главное меню должно показывать текущую выбранную машину игрока с её реальной сохранённой внешностью:

- выбранная модель;
- цвет кузова;
- цвет дисков;
- неон.

## Основной файл

- `Assets/Scripts/UI/MotorCityFrontEndFlow.cs`

## Связанные системы

- `Assets/Scripts/Gameplay/VehicleRosterSystem.cs`
- `Assets/Scripts/Gameplay/VehicleCustomizationSystem.cs`
- `Assets/Scripts/Vehicle/ArcadeRacingCarRuntimeInstaller.cs`
- `Assets/Scripts/Camera/ChaseCamera.cs`
- `Assets/Scripts/Persistence/MotorCitySaveService.cs`
- `Assets/Scripts/Bootstrap/MotorCityBootstrap.cs`

## Что не делать

Не использовать напрямую игровой Rigidbody-автомобиль как menu prop.

Это создаст зависимости от:

- physics;
- WheelCollider;
- runtime wheel reparenting;
- gameplay camera;
- teleports.

## Что создать

Рекомендуемый отдельный компонент:

- `MainMenuVehiclePresenter.cs`

Ответственность:

1. определить выбранный vehicle id;
2. создать presentation visual;
3. применить сохранённую кастомизацию;
4. поставить на специальный anchor;
5. отключить gameplay physics;
6. обновить при смене выбора;
7. удалить/скрыть при входе в gameplay.

## Presentation anchor

Создать понятную точку:

- `MainMenuVehicleAnchor`

Можно разместить:

- в существующей front-end сцене;
- или в специально подготовленной 3D-зоне.

Не завязывать положение на случайные координаты UI.

## Кастомизация

Нельзя копировать логику цвета вручную в menu presenter.

Нужно вынести/переиспользовать безопасный общий способ:

- прочитать saved customization;
- применить визуальную часть к presentation vehicle.

Это хороший кандидат для маленького reusable customization applicator, но не для полной новой VehicleDefinition архитектуры.

## Камера меню

Front-end camera должна:

- смотреть на anchor;
- иметь отдельный FOV;
- не зависеть от gameplay ChaseCamera.

Допустимо добавить:

- медленный idle orbit;
- лёгкий cinematic movement.

## Неон

Если включён:

- должен быть виден;
- желательно реально подсвечивать поверхность;
- но menu lighting не должно создавать десятки лишних lights.

## Риски

- двойное создание тяжёлых vehicle prefabs;
- материалы могут влиять друг на друга через sharedMaterial;
- runtime installer может ожидать ArcadeCarController;
- menu presenter должен работать до полноценного gameplay init.

## Тесты

- новый save;
- существующий save;
- каждая машина;
- разные body colors;
- wheel colors;
- neon on/off;
- зайти gameplay → garage → поменять машину → main menu;
- перезапуск игры.

## Критерий готовности

Главное меню всегда отражает последнюю сохранённую внешность текущей машины.

---

# 8. Технический pass без полной переделки автомобилей

## Цель

Снизить количество багов “одна система перезаписала другую”.

Полную архитектуру автомобилей пока не делать.

## Что искать

### Magic vehicle id checks

Например:

```csharp
if (vehicleId == "street")
```

Поиск по проекту:

- `"street"`
- другие vehicle ids.

Каждый случай классифицировать:

1. временная совместимость с текущим asset;
2. настоящая gameplay special-case;
3. технический долг;
4. editor/import pipeline.

Не удалять все проверки механически.

### Проверки имён mesh

Поиск:

- `transform.name`
- `renderer.transform.name`
- `sharedMesh.name`
- `Contains("wheel")`
- `Contains("body")`

Особенно смотреть:

- `Assets/Scripts/Gameplay/VehicleCustomizationSystem.cs`
- `Assets/Scripts/Vehicle/ArcadeRacingCarRuntimeInstaller.cs`
- `Assets/Scripts/World/PlayerVehicleRearEmission.cs`
- `Assets/Scripts/World/PlayerHeadlights.cs`

### Проверки имён materials

Поиск:

- `material.name`
- `IsRimMaterial`
- `IsRubberMaterial`
- lamp/emission material matching.

Опять же: editor importer может законно работать по именам. Главная цель — уменьшить runtime guessing там, где можно хранить прямые ссылки/role metadata.

## Главная проблема, которую уже встретили

Порядок инициализации visual systems.

Пример:

1. customization применяет MaterialPropertyBlock;
2. позже другая система заменяет materials;
3. кастомизация визуально пропадает.

Текущее временное решение с deferred apply допустимо, но долгосрочно нужен явный жизненный цикл.

## Предлагаемый lifecycle

Нужно постепенно прийти к событиям:

```
VehicleVisualCreated
VehicleVisualPrepared
VehicleAppearanceReady
VehicleVisualChanged
```

Или более компактной схеме.

Главное:

- runtime installer сообщает, когда hierarchy окончательно готова;
- headlights/rear emission делают binding;
- customization применяется после material replacements;
- UI не пытается угадывать момент через frame delay.

## Файлы для полного просмотра

- `Assets/Scripts/Gameplay/VehicleRosterSystem.cs`
- `Assets/Scripts/Gameplay/VehicleCustomizationSystem.cs`
- `Assets/Scripts/Vehicle/ArcadeRacingCarRuntimeInstaller.cs`
- `Assets/Scripts/Vehicle/ArcadeCarController.cs`
- `Assets/Scripts/Vehicle/PlayerVehicleAudio.cs`
- `Assets/Scripts/World/PlayerVehicleRearEmission.cs`
- `Assets/Scripts/World/PlayerHeadlights.cs`
- `Assets/Scripts/Gameplay/GarageUpgradeSystem.cs`
- `Assets/Scripts/Bootstrap/MotorCityBootstrap.cs`
- editor importers в `Assets/Editor/*VehicleImporter.cs`

## Что пока НЕ делать

До замены моделей не строить окончательную систему, где вручную прописываются для каждой нынешней модели:

- body renderer;
- wheel renderer;
- wheel anchors;
- exact mesh roles;
- окончательные paint profiles.

Иначе после замены моделей половину работы придётся выбросить.

## Что можно делать сейчас безопасно

- заменить runtime Find/guess на сохранённую прямую ссылку после первого resolve;
- добавить события жизненного цикла;
- убрать дублирование save/read/apply;
- сделать reusable presentation customization;
- разделить UI-layout от gameplay logic;
- добавить diagnostics/assertions, если нужная часть машины не найдена.

---

# 9. Зависимости между этапами

## Камера

Практически независима.

## Звук

Зависит от:

- ArcadeCarController;
- MotorCityInput.

Не зависит от UI pass.

## Garage orbit

Зависит от ChaseCamera, поэтому лучше после camera pass.

## Tutorial

Зависит от:

- controls;
- garage;
- activities;
- localization;
- UI.

Лучше делать после базового UI-kit.

## Main menu vehicle

Зависит от:

- roster;
- customization;
- save;
- presentation visual creation.

Желательно после технической стабилизации customization apply flow.

## UI pass

Нужно закончить основу до нового tutorial.

---

# 10. Открытые вопросы

Перед реализацией конкретных этапов нужно получить ответы.

## Камера

- Полностью убрать speed distance bonus или оставить 5–10% текущего эффекта?
- FOV делать умеренным или заметным?
- Turn lag должен слегка оставаться с внешней стороны поворота?

## Передачи

- Только автоматическая коробка?
- Показывать текущую передачу на HUD?
- Есть ли готовые shift/throttle-off звуки?
- Нужен ли просто throttle-off или ещё exhaust burble?

## Garage

- Только горизонтальный orbit или также небольшой vertical?
- ЛКМ drag должен работать без ПКМ?
- Нужен ли pinch zoom?

## Tutorial

- Первая activity точно становится короткой гонкой?
- Что делать со старой доставкой?
- Оставлять ли Витю/Турбо в коротких репликах?
- Старые 6 intro slides удалить из обязательного flow или оставить optional?

## Main menu

- Где именно должна стоять машина?
- Статичная машина или idle orbit?
- Неон должен реально освещать пол?

## UI

- Сохраняем нынешний визуальный стиль полностью?
- Делать 2–3 стандартных размера modal windows?

---

# 11. Progress Checklist

## Камера

> Этап завершён и проверен в игре. Реализация: `e6bf6ecef82aeb283d499c20e4cf177daaf554e0`.

- [x] Убрать сильный speed distance bonus и искусственное позиционное отставание
- [x] Настроить FOV curve
- [x] Разделить FOV gain/return smoothing
- [x] Добавить turn lag
- [x] Проверить drift look в игре
- [x] Проверить orbit input в игре
- [x] Проверить collision camera в игре

## Audio

> Этап завершён и проверен в игре. Реализация: `215f96625b7918f63d6677c86e293e0bf58f4523`. Отдельных shift/throttle-off AudioClip в Resources сейчас нет, поэтому эффект реализован через engine loop.

- [x] Добавить simulated gear state
- [x] Добавить shift thresholds
- [x] Добавить hysteresis
- [x] Добавить pitch drop
- [x] Добавить volume dip
- [ ] Поддержать отдельный shift clip, когда появится подходящий аудиофайл
- [x] Добавить throttle-off detection
- [x] Проверить WebGL audio

## Garage orbit

> Этап завершён и проверен в игре. Реализация: `f7f9f12438b0a5b4b67e12bb7f37e30f21fed0eb`.

- [x] Сделать garage-specific orbit behavior
- [x] ЛКМ drag
- [x] Touch swipe
- [x] UI raycast protection
- [x] Проверить смену авто в игре
- [x] Проверить zoom в игре

## UI-kit

> Общий modal UI-kit добавлен в `Assets/Scripts/UI/MotorCityModalUiKit.cs`. Меню паузы переведено на него и проверено в игре. Front-end settings/control choice переведены на layout helpers в `Assets/Scripts/UI/MotorCityFrontEndModalUi.cs`; действия окна результата переведены на общий `ButtonRow + PrimaryButton`. Текущая реализация ожидает игровую проверку settings/control choice/result.

- [x] MotorCityModalWindow
- [x] MotorCitySettingsRow
- [x] MotorCityPrimaryButton
- [x] MotorCityButtonRow
- [x] Общие layout constants
- [x] Pause migration
- [x] Settings migration
- [x] Control choice migration
- [x] Result modal actions migration
- [x] Проверить settings/control choice/result в игре

## Tutorial

- [ ] Новый step enum/state
- [ ] Gas step
- [ ] Brake step
- [ ] Steering step
- [ ] Garage route
- [ ] Customization step
- [ ] Beginner race
- [ ] Save compatibility
- [ ] Localization
- [ ] Remove mandatory long intro

## Main menu vehicle

- [ ] MainMenuVehiclePresenter
- [ ] Presentation anchor
- [ ] Selected vehicle load
- [ ] Body color apply
- [ ] Wheel color apply
- [ ] Neon apply
- [ ] Menu camera framing
- [ ] Cleanup on gameplay start

## Technical pass

- [ ] Audit vehicle id magic checks
- [ ] Audit mesh-name runtime checks
- [ ] Audit material-name runtime checks
- [ ] Audit initialization order
- [ ] Introduce visual lifecycle events
- [ ] Remove redundant delayed re-apply where possible
- [ ] Add diagnostics for unresolved vehicle parts

---

# 12. Definition of Done для всего roadmap

Roadmap можно считать выполненным, когда:

- на скорости камера больше не “уезжает назад” от машины;
- переключения передач и отпускание газа слышны;
- машину удобно осматривать в гараже на desktop и mobile;
- стандартные окна строятся из общего UI-kit;
- первый запуск обучает через действия, а не большой текст;
- главное меню показывает реальную текущую машину игрока с кастомизацией;
- runtime systems меньше зависят от случайного порядка Start/LateUpdate;
- замена будущих моделей автомобилей не требует переписывать весь новый UI/camera/tutorial/audio code.
