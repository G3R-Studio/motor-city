# Motor City — переход на Input System (New), 2026-10-08

**Статус:** **Unity Editor Input System gate пройден** (2026-10-08 21:25 UTC): управление работает, `Tools/check_cleanup.ps1` — PASS (242 C#), исправленный `py -3 Tools/check_input_backend.py` — PASS, предупреждение Input Manager deprecation исчезло. На последующий запрос проверить WebGL Desktop в браузере пользователь ответил: **«в webgl всё нормально, уже проверял»** (21:28 UTC). Это **подтверждение работоспособности WebGL по пользователю**, но без лога новой сборки, URL, точного Build Profile, ревизии сборки и отдельного результата мобильного WebGL Release; не приравнивать его к формальной release matrix. Общая чистота всей Console (все возможные предупреждения) также не доказана.

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
| `Assets/Fantastic City Generator/DayNight/ShiftAtRuntime.cs` | Оставшийся `Input.GetKeyDown(KeyCode.N)` заменён на `Keyboard.current.nKey.wasPressedThisFrame` (commit `73010b9`) | Переключение ночи клавишей N и методы `DayNight` сохранены |
| `ProjectSettings/ProjectSettings.asset` | `activeInputHandler: 2 → 1` | Остальные PlayerSettings |
| `Assets/Settings/Build Profiles/Web - Mobile - Release.asset` | Сериализованный `activeInputHandler: 2 → 1` | Остальные настройки WebGL Mobile Release |
| `Assets/Settings/Build Profiles/Web - Desktop - Release.asset` | Сериализованный `activeInputHandler: 2 → 1` | Остальные настройки WebGL Desktop Release |
| `Tools/check_input_backend.py` | Read-only проверка трёх настроек backend, пакета, legacy API и самопроверки regex/detector; исправлена изначально переэкранированная строка `activeInputHandler` (commit `f03f3d4`) | Не запускает Unity и не меняет файлы |

**Не удалять** `ProjectSettings/InputManager.asset` и сторонний `PrometeoCarController`: они могут быть частью исходных пакетов/импортёров, а физика использует Prometeo.

## Промежуточный прогон 2026-10-08 21:22 UTC

Пользователь подтвердил **«управление работает»** после установки новой версии и прислал результаты:

- `pwsh -NoProfile -File Tools/check_cleanup.ps1` — **PASS**: `All 242 C# sources passed syntax checks ... All cleanup syntax and source checks passed.` Это не заменяет сборку WebGL.
- `py -3 Tools/check_input_backend.py` — **FAIL** (до исправления): 3× `found []`, хотя в Git и проектных настройках `activeInputHandler: 1`, поскольку в Python raw regex ошибочно были удвоены `\\b` и `\\s`; 1× `Assets/Fantastic City Generator/DayNight/ShiftAtRuntime.cs:24: Input.GetKeyDown(KeyCode.N)`.
- Исправления в `main`: `f03f3d4` (правильный regex и self-tests), `73010b9` (новая система ввода в `ShiftAtRuntime`). **Этот промежуточный FAIL был снят успешным повторным запуском 21:25 UTC**, см. следующий раздел.
- На момент 21:22 UTC ещё не было подтверждения исчезновения предупреждения. **Получено в 21:25 UTC** (см. следующий раздел). Скриншота всей Console нет, WebGL Desktop/Mobile build gates остаются открытыми.

## Повторная проверка 2026-10-08 21:25 UTC — Unity Editor PASS

После коммитов `f03f3d4` и `73010b9` пользователь повторно запустил `py -3 Tools/check_input_backend.py`. Результат: `Motor City input backend source/settings checks passed.`; глобальный PlayerSettings и оба WebGL release профиля с Input System (New); пакет установлен; legacy Input API в `Assets/**/*.cs` не найдены. Пользователь также подтвердил: **«предупреждение исчезло»** — в контексте предупреждения Unity `Input Manager deprecation`.

- [x] Guard и внутренняя самопроверка regex/detector — PASS по выводу пользователя.
- [x] `check_cleanup.ps1` — PASS (242 C#), результат от 21:22 UTC.
- [x] Управление в Unity Editor работает по подтверждению пользователя после смены backend.
- [x] Известное предупреждение Input Manager deprecation исчезло по подтверждению пользователя.
- [ ] Нет отдельной гарантии отсутствия **всех** других предупреждений Console; новый полный скриншот не предоставлен.
- [x] **WebGL smoke по подтверждению пользователя:** на вопрос о сборке WebGL Desktop, запуске в браузере, меню и управлении одной машиной пользователь ответил «в webgl всё нормально, уже проверял» (2026-10-08 21:28 UTC). Не просить повторять общий smoke без конкретной регрессии.
- [ ] **Формальная WebGL release matrix:** не приложены логи/артефакты сборок именно `Web - Desktop - Release` и `Web - Mobile - Release` после смены backend; мобильный браузер/тач и конкретная сборка не подтверждены отдельно. Не выводить `[x]` для обоих профилей из одного сообщения.

## WebGL smoke — подтверждение 2026-10-08 21:28 UTC

После предложения сделать WebGL Desktop Release build и проверить запуск в браузере, меню и управление пользователь ответил: **«в webgl всё нормально, уже проверял»**. Отмечаем положительный **пользовательский WebGL smoke**, не заставляем повторять одно и то же. В сообщении нет названия Build Profile, build commit/даты, консоли браузера или отдельного подтверждения мобильного тача в WebGL; поэтому формальный Desktop/Mobile Release gate остаётся открытым до появления таких данных в рамках release matrix. Это ограничение документирования, а не утверждение о проблеме в WebGL.

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
