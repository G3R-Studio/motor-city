# Motor City — переход на Input System (New), 2026-10-08

**Статус:** код и настройки в `main` подготовлены; Unity compile, Play Mode и WebGL проверки ещё **не подтверждены** пользователем. Никаких галочек за runtime-работоспособность до проверки не ставить.

## Почему появилась жёлтая строка Input Manager deprecation

- Unity 6000.6.1f1, пакет `com.unity.inputsystem: 1.20.0` уже был установлен.
- `ProjectSettings/ProjectSettings.asset` имел `activeInputHandler: 2` (**Both**). Оба `Assets/Settings/Build Profiles/Web - * - Release.asset` также содержали собственное сериализованное `activeInputHandler: 2`.
- Текущая игровая система уже использовала новый Input System: `MotorCityInput` — `Keyboard.current`, `Gamepad.current`, `Touchscreen.current`; `MotorCityUiEventSystem` — `InputSystemUIInputModule`; `ChaseCamera` — `EnhancedTouch`; мобильный интерфейс передаёт сигналы в `MotorCityInput` через `IPointer...`.
- Но остались вызовы старого `UnityEngine.Input` в трёх местах. **Особенно важно:** `PrometeoCarController` — не мёртвый сторонний пакет: `ArcadeCarController.TryBindPrometeo()` создаёт его динамически и подключает touch input proxy. Его старый `Input.GetKey` оставался **резервной веткой**, когда proxy недоступны.

## Точечные изменения

| Файл | Изменение | Что осталось прежним |
| --- | --- | --- |
| `Assets/Scripts/Input/MotorCityInput.cs` | `UnityEngine.Input.touchSupported` убран; используется `Touchscreen.current != null` плюс прежние проверки мобильной платформы/Device Simulator | Все управляющие действия, PlayerPrefs, виртуальный руль и кнопки |
| `Assets/PROMETEO - Car Controller/Scripts/PrometeoCarController.cs` | Только legacy keyboard fallback переведён с `Input.GetKey/GetKeyUp` на `Keyboard.current[key].isPressed/wasReleasedThisFrame` через два метода | Физика, прокси-путь, трение, колёсные коллайдеры и обработка руля |
| `Assets/Fantastic City Generator/Scripts/FreeCamera.cs` | Сторонняя вспомогательная камера использует `Mouse.current`, `Keyboard.current`; для движения сохранено приблизительное сглаживание осей 3/с | Позиции/повороты игрового chase camera не затронуты |
| `ProjectSettings/ProjectSettings.asset` | `activeInputHandler: 2 → 1` | Остальные PlayerSettings |
| `Assets/Settings/Build Profiles/Web - Mobile - Release.asset` | Сериализованный `activeInputHandler: 2 → 1` | Остальные настройки WebGL Mobile Release |
| `Assets/Settings/Build Profiles/Web - Desktop - Release.asset` | Сериализованный `activeInputHandler: 2 → 1` | Остальные настройки WebGL Desktop Release |
| `Tools/check_input_backend.py` | Read-only проверка глобального/двух WebGL профилей, наличия пакета и legacy API ссылок в `Assets/**/*.cs` | Не запускает Unity и не меняет файлы |

**Не удалять** `ProjectSettings/InputManager.asset` и сторонний `PrometeoCarController`: они могут быть частью исходных пакетов/импортёров, а физика использует Prometeo.

## Обязательные verification gates

1. Закрыть Unity и выполнить `git pull --ff-only origin main`. Открыть Unity снова и дождаться компиляции/импорта. При смене Active Input Handling редактор должен использовать обновлённую конфигурацию после перезапуска.
2. В корне проекта выполнить `py -3 Tools/check_input_backend.py` и `pwsh -NoProfile -File Tools/check_cleanup.ps1`. Результат каждого сохранить; **статический тест не равен Unity compile**.
3. Unity Play Mode, Console: отсутствие compile errors и `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class`. Проверить меню, UI-клики мышкой, `Continue`, выбор управления.
4. В городе: клавиатура **W/A/S/D/стрелки/пробел**, сенсорные режимы **стрелки и рулевое колесо**, **газ/тормоз/ручник**, настройка расположения; быстрый smoke камеры/гаража и сохранения выбора. Раньше это всё работало — теперь нужен лишь регрессионный тест после изменения backend, не полный проход по всем десяти машинам.
5. При возможности отдельно проверить WebGL Desktop и WebGL Mobile сборки из **своих профилей**, включая клавиатуру, тач, UI и отсутствие ошибок в браузерной консоли. Без этого нельзя считать WebGL подтверждённым.
6. Если старое предупреждение всё равно появляется, сохранить дословное сообщение и активный Build Profile: не править руками InputManager.asset и не отключать готовые обработчики UI без расследования.

## Риски и возврат

- Переход `Both → New` меняет backend ввода **для всего проекта**, а не только для одной машины; поэтому проектная настройка и два WebGL-профиля обновлены согласованно.
- Сторонние пакеты, подключённые позже, могут использовать legacy API; новый read-only guard должен останавливать это до сборки.
- Истинная доступность `Mouse/Keyboard/Touchscreen/Gamepad` и поведение новой Input System проверяются только в Editor/Device Simulator/реальном WebGL.
- Если возникнет регрессия, **не делать `git reset --hard`**: сообщить об ошибке, после чего исправлять конкретный путь или откатить миграцию отдельным revert-коммитом. Исходное состояние до миграции: `3ea33c07207279a8606e36edd96123f322c83b4a`.

Официальная документация Unity: https://docs.unity.cn/Packages/com.unity.inputsystem@1.19/manual/Installation.html и https://docs.unity.cn/Packages/com.unity.inputsystem@1.13/manual/Migration.html .
