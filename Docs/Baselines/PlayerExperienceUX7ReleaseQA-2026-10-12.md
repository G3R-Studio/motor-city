# UX-7 - финальная релизная приёмка Motor City

Дата подготовки: 2026-10-12. Репозиторий: `G3R-Studio/motor-city`, `main`.

## Текущее состояние

UX-0 - UX-6 реализованы; пользователь подтвердил ручной Unity smoke для UX-5 и UX-6 («всё чётко»). Это **не** равно успешной Unity Editor CI, двум WebGL Release binaries, опубликованным GitHub Pages или браузерному smoke.

**UX-7 НЕ закрывать до реальной проверки.** Source/UI preflight, Editor CI, две WebGL Release сборки, Pages и Desktop/Mobile browsers имеют разные gate и требуют доказательств на одном SHA. Нельзя подменять эти результаты source-only проверкой или старыми артефактами.

## 1. Фиксация исходного SHA и локальный preflight

Перед запуском на рабочем ПК:

```powershell
git pull --rebase origin main
git status --short
git rev-parse HEAD
python Tools/check_release_qa.py
python Tools/check_ux7_release_readiness.py
python Tools/check_phase7_ui_ownership.py
python Tools/check_phase12_release_profiles.py
pwsh -NoProfile -File Tools/check_ui_layout.ps1
pwsh -NoProfile -File Tools/check_cleanup.ps1
```

Не запускать release по незакоммиченным изменениям. Сохранить SHA, ошибочные команды починить и повторить до зелёного preflight. `Tools/check_release_qa.py` проверяет тестовые entrypoints по `UNITY_WEBGL`, но не выполняет IL2CPP.

## 2. Ручной GitHub Actions: Editor, Desktop/Mobile WebGL

Открыть Actions → **Motor City Source Gates** → **Run workflow** → branch `main`. Проверить `UNITY_CI_ENABLED=true` в Settings → Secrets and variables → Actions → Variables; runner с метками `self-hosted, Windows, X64, unity6000` должен быть online, Unity 6000.6.1f1 активирован. Windows runner использует `D:\GitHub\motor-city` как **read-only seed** и готовит точный commit SHA в отдельном workspace, никогда не мутируя пользовательский clone. Источник seed должен иметь доступ ко всем Git LFS объектам.

Сначала запустить с `build_webgl_release=false`, `build_legacy_webgl=false`. Дождаться:
- `source-gates`: success, включая source/UI/C# parse, выпуск без тестовых методов;
- `ui-layout`: success;
- `unity-editor`: success, файл `motor-city-unity-validation` доступен.

Если редакторский job skipped из-за переменной / offline runner или упал, **не считать Editor CI successful**.

Когда Editor CI подтверждён, на **том же исходном SHA** запустить Source Gates повторно, теперь с `build_webgl_release=true`, `build_legacy_webgl=false`. Дождаться `unity-phase12-releases=success`. В этом **одном** run должны появиться:
- `motor-city-webgl-desktop-release`;
- `motor-city-webgl-mobile-release`;
- `motor-city-phase12-release-reports`.

Сохранить release run URL, run ID и `head_sha`; проверить `Build result: Succeeded` для обоих профилей и логи импорта/компиляции. Если между прогонами менялся main, повторить Editor/release именно для нового SHA. Артефакты имеют короткий retention, не откладывать браузерную QA.

## 3. Строго ручная публикация Pages

Открыть Settings → Pages → Source: **GitHub Actions**. Затем Actions → **Motor City GitHub Pages Preview** → Run workflow → `main` и `release_run_id` из **успешного** шага выше. Pipeline обязан отказать, если run не Source Gates, не ручной, не из `main`, не success, если отсутствует любой Desktop/Mobile release artifact или SHA релиза не равен актуальному `main` и SHA dispatch. `Tools/prepare_pages_webgl.py` разворачивает Brotli WebGL binaries в обычные `.wasm/.data/.framework.js`, пишет `release.txt` с SHA/run ID, проверяет предел 1 GiB.

Только `deploy=success` и содержимое опубликованного `release.txt` с **точным SHA** подтверждают публикацию. Не подставлять устаревший `release_run_id`.

## 4. Desktop браузер - ручная QA на опубликованном URL

Отдельно чистый/new и старый/persisted save:
- главное меню → «МОЙ MOTOR CITY» → «АКТИВНОСТИ» (read-only, нет кнопок маршрутов) → «ЖУРНАЛ» и все семь вкладок → Автоклуб / Инспектор / Пикси / гараж / достижения / ночной клуб → «НАЗАД» / Esc;
- старт активности из игры/навигатора (каталог не открывает маршрут), реальный результат с суммой КР/REP, фактическим before/after, `Retry` (где допустимо), `В ГОРОД`, отсутствие двойной выплаты;
- пауза → главное меню → Continue → новый выбор управления; пауза не оставляет `Time.timeScale` в неправильном состоянии; смена языка RU/EN;
- длинные списки и scroll; 1920×1080/узкое окно браузера; старый save после F5, смена автомобиля, активная миссия, поражение и повтор без нового рекорда.

Записать Console (Unity WebGL errors, network, Javascript) и скриншоты главного окна, журнала, гаража и результата.

## 5. Mobile браузер - отдельно Desktop/Mobile build

На реальном телефоне открыть **mobile** URL; убедиться, что нет ошибки загрузки `.wasm/.data` и воспроизводится Unity scene. Пройти выбор управления:
- колесо: устойчивое управление, не дрожит, газ/тормоз/ручник/action; drag настройка расположения и сохранение layout;
- стрелки: те же действия и редактирование, не конфликтует с scroll;
- safe area, повороты экрана, узкий ландшафт/portrait, окна и «НАЗАД», touch scroll журнала/результата, меню/пауза/навигатор;
- reload страницы и восстановление личного прогресса без повторного начисления наград.

Вписать модель устройства, браузер, версию ОС, снимки экрана и ошибки Console/remote DevTools.

## 6. Отдельная Yandex Games QA

GitHub Pages **не подтверждает** Yandex SDK. Проверить рекламу, сохранения/авторизацию, IAP/supporter, облачное восстановление, leaderboards/remote config и платёжные ограничения только в реальном QA Яндекс Игр. При отсутствии SDK на Pages не показывать поддельную успешную операцию. До реального Yandex QA этот checklist остаётся `[ ]`.

## 7. Приёмка/артефакты: заполнить после запуска

| Gate | Итог | Доказательство |
| --- | --- | --- |
| Local UI/owner/C# source gates | CI: FAIL на Phase 3, исправлена отсутствующая зависимость; полный повтор ожидается | [run #38190619538](https://github.com/G3R-Studio/motor-city/actions/runs/38190619538), `FileNotFoundError` wheel-baseline; восстановлен исходный JSON в `72399da` |
| Unity Editor CI on exact release SHA | Старый SHA `a6b52e1`: SUCCESS; новый release SHA: требуется повтор | [run #38190619538](https://github.com/G3R-Studio/motor-city/actions/runs/38190619538) - Unity compilation/scene import PASS, `motor-city-unity-validation` |
| Desktop & Mobile Build Profiles | НЕ ПРОВЕРЕНО | один release run URL, 2 artifacts, 2 build reports |
| GitHub Pages exact release SHA | НЕ ПРОВЕРЕНО | deploy run URL + `release.txt` |
| Desktop browser clean/old save | НЕ ПРОВЕРЕНО | URL, screenshot + notes |
| Mobile browser touch & safe area | НЕ ПРОВЕРЕНО | URL, device, screenshots + notes |
| Yandex SDK QA | НЕ ПРОВЕРЕНО | отдельные platform QA logs |

**Не создавать фальшивые скриншоты/ссылки/указания PASS.** Публиковать screenshots и отметить checklist UX-7 только после исполнения сценариев на текущем SHA. На непрошедшем gate остановить выпуск, исправить и перезапустить сборки на новом SHA.

## 8. Первый CI run 2026-10-12 и исправление

[Motor City Source Gates #38190619538](https://github.com/G3R-Studio/motor-city/actions/runs/38190619538) на SHA `a6b52e122033cffd583635a0a2130c6c755afcc4` завершился `failure`. Джобы:
- `Detect Unity-relevant changes`: success;
- `UI source layout gate`: success;
- `Unity 6 Editor batch validation`: success, “Unity Editor compilation and scene import completed”, “Unity Phase 2 gate PASSED”; есть артефакт `motor-city-unity-validation`;
- `Static, GUID and C# source gates`: failure на `Tools/check_vehicle_contract.py`, потому что отсутствовал `Docs/Baselines/VehicleWheelTransforms-2026-10-09.json`;
- `Phase 12 WebGL Desktop + Mobile Release profiles`: skipped из-за source gate.

Оригинальный `VehicleWheelTransforms-2026-10-09.json` взят **из истории Git** (коммит `2ead32c`, blob `1579453c49a1de2aa264ad4c51e781e9add75f26`), восстановлен без регенерации в `72399da`. Source-only сверка с текущими девятью player prefabs: все 36 позиций и quaternion wheel pivots совпали. Требуется **запустить НОВЫЙ workflow на текущем main**, а не rerun старого SHA. Пока новый Source Gates и оба WebGL Release не success, выпуск не публиковать.

## 9. Разделение Unity player build и проверок CI

Проверено 2026-10-12 по `ProjectSettings/EditorBuildSettings.asset`, двум `Assets/Settings/Build Profiles/Web - {Desktop,Mobile} - Release.asset` и `Assets/Editor/MotorCityPhase12ReleaseBuild.cs`: **в WebGL входит единственная включённая сцена `Assets/Scenes/Prototype.unity` с её игровыми зависимостями**. Python/PowerShell в корневых `Tools/`, документы в `Docs/`, и прочие файлы вне `Assets/` и `Packages/` не становятся Unity runtime assets. Папка `Assets/Editor/` содержит инструменты редактора, но они не включаются в player runtime.

`motor-city-source-gates.yml` специально выполняет Python/PowerShell source regressions в **отдельном** job `source-gates`. Его failure прерывает релиз **до** WebGL build, но не означает, что скрипты или документы попадают в сборку. Сама сборка Unity запускается в `unity-phase12-releases` только после успешных проверок исходников, layout и Unity Editor.

JSON с 36 эталонными локальными позами колёс девяти prefab - **fixture для `Tools/check_vehicle_contract.py`**, а не игровой конфиг. Чтобы не держать зависимость теста в `Docs/`, исходный неизменённый blob `1579453c49a1de2aa264ad4c51e781e9add75f26` перенесён в `Tools/Tests/Fixtures/VehicleWheelTransforms-2026-10-09.json`. В `Assets/` не добавлялся; runtime код, модели/префабы, `.meta`, сцены и настройки Build Profiles не менялись. Source checker по-прежнему проверяет именно неизменённый эталон и при отсутствии fixture выдаёт явную ошибку.

## 10. Run #38191383883 и сбор нескольких source-ошибок

[Run #38191383883](https://github.com/G3R-Studio/motor-city/actions/runs/38191383883) (`bbe1e6f`): `ui-layout` success; `unity-editor` success; Phase 3 wheel JSON (перенесён в `Tools/Tests/Fixtures`) success. `source-gates` failure на `Tools/check_ux7_release_readiness.py`, 13/14 source checks, из-за ложного ожидания `".head_sha`. В Pages реально есть `jq -r '.head_sha'`, поэтому проверка SHA не менялась, исправлен только тест исходников.

Workflow регистрирует все 30 Python/Roslyn source checks с `continue-on-error: true`, затем `Aggregate all source-gate outcomes` собирает `steps.*.outcome` и возвращает ошибку, если хоть один source check не success. В одном CI запуске будут видны все потенциальные проблемы без обхода защитных gate релиза. Только полный `source-gates=success` разблокирует WebGL. Требуется новый запуск на обновлённом SHA с `build_webgl_release=true`.
