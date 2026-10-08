# Motor City — baseline префабов и материалов (2026-10-08)

**Состояние Git сохранено без изменения игровых ассетов.** Источник: `G3R-Studio/motor-city` commit `ec98dc91d8fcbbae4ce23d1953aeddcaa1097382`.

Полный машинный инвентарь: [`PrefabMaterialInventory-2026-10-08.json`](PrefabMaterialInventory-2026-10-08.json). Он содержит пути, размеры Git-объектов, Git blob SHA ассетов и их `.meta`. Это **отпечатки содержимого**, не оценка неиспользуемых файлов.

## Масштаб

| Источник | Префабы | Материалы |
| --- | ---: | ---: |
| Fantastic City Generator | 607 | 47 |
| Resources/MotorCity | 28 | 83 |
| Остальной проект | 12 | 77 |
| **Всего** | **647** | **207** |

Для всех 854 файлов найдены `.meta` в текущем Git tree; это не гарантирует правильного Unity импорта или поддержки шейдеров.

## 10 игровых префабов

В `Assets/Scripts/Gameplay/VehicleRosterSystem.cs` указаны `Resources`-пути всех 10 машин: Street использует `PlayerCarVisual.prefab`, остальные — `Vehicles/Player/*.prefab`.

| Путь относительно Assets/Resources/MotorCity | Unity GUID | Git SHA (первые 12) |
| --- | --- | --- |
| `PlayerCarVisual.prefab` | `0c72412c581de1d40a0f835642abd265` | `02fc591e61ff` |
| `Vehicles/Player/AmgGT.prefab` | `f736c5f7a1afd06468e807d070ee15dd` | `c4f57221bbfa` |
| `Vehicles/Player/Beatall.prefab` | `c6736dddb5681304da56c6a1bddb3bdf` | `33fdbc78e5ec` |
| `Vehicles/Player/Bus.prefab` | `239ab3148fc733448bfa0ae6cd47414c` | `74ba477066b7` |
| `Vehicles/Player/Camaro.prefab` | `e625efb079f1a7e46ae899f726055e2d` | `0b18d9f86c60` |
| `Vehicles/Player/Delorean.prefab` | `a3c23f9fcf4403848b8595317e29a49e` | `7234b04ffcb5` |
| `Vehicles/Player/Hybrid.prefab` | `911dc3f29abf4e62ab8ff9c13baf5485` | `69bc2b1e8c93` |
| `Vehicles/Player/Peugeot306.prefab` | `3d3724b85ddd57749ba0d84a8099d097` | `5a2f2b729948` |
| `Vehicles/Player/Porsche996.prefab` | `5d81eb900ff390e4c9b2cd80de637640` | `8acd214d4e0b` |
| `Vehicles/Player/ToyotaAE86.prefab` | `bc44e1f09259e1545ad4694f96e243f7` | `040ee128e330` |

## Критические ресурсы города и стёкол

| Путь (относительный путь от Resources/MotorCity или Assets) | Unity GUID | Git SHA (первые 12) |
| --- | --- | --- |
| `VehicleGlass.mat` | `2d9a47bfc0ef4d91ba0ea3c4f833762f` | `7b7e594219ba` |
| `Environment/CityVisual.prefab` | `42155fb3da765714e8e49ad62354877d` | `69ed670bcde2` |
| `Assets/Fantastic City Generator/Roads/Prefab/Double-Block-09.prefab` | `117dc5da96c6fef48a8f4f0f03a504b9` | `70e7354a07c5` |
| `Assets/Fantastic City Generator/Generate.prefab` | `913769814a248de4db031a83080046b2` | `0b4140f2b63b` |

Важные ограничения: `CityVisual.prefab` (~104 MB) не трогали; шесть `Assets/Resources/MotorCity/Environment/FCGTrafficCars/*.prefab` представлены в Git через **LFS pointer** и их Git SHA — хеш указателя, не больших бинарных данных. GUID перечисленных 14 файлов взяты из `.meta`; SHA материала, SHA `.meta` и Unity GUID — разные идентификаторы.

`Double-Block-09.prefab` включён уже **после** минимального исправления двух пустых Missing Script-компонентов (`4da9a30`). В Unity пользователь подтвердил отсутствие двух предупреждений и нормальную работу города, дороги и воды.

## Как применять baseline

1. Перед изменением/удалением найти GUID-входящие ссылки, `AssetDatabase.GetDependencies`, динамические `Resources.Load`, имена в скриптах, исходники FBX/OBJ, импортеры, runtime-материалы, зависимые префабы/сцены и связанные сохранения.
2. Сравнить `git diff --name-status ec98dc91d8fcbbae4ce23d1953aeddcaa1097382 HEAD -- "*.prefab" "*.mat" "*.meta"`. Изменение `.meta` проверять отдельно: оно может повлиять на GUID.
3. После изменений повторить `Tools/check_cleanup.ps1`, `Tools/audit_project.py`, Unity project/scene audit и материал-аудит через `Motor City → Diagnostics → Audit All Project Materials`. Для runtime-материалов использовать `Audit Materials In Open Scene` в Play Mode.
4. Нулевые текстовые/serialized ссылки **не являются разрешением** на удаление. Фактические shader slots, render roles и корректность WebGL требуют проверки в Unity.

## Всё ещё открыто

Полный архив скриншотов день/ночь/гараж всех машин, детальный `FixedUpdate`/physics profiling, приёмка материалов в Unity и Input Manager deprecation warning. Отдельное подтверждение пользователей о работоспособности всех 10 машин, фар, стопов, косметики и мобильного управления уже получено; повторять его без причины не нужно.

**Назначение:** точная точка сравнения для 647 префабов и 207 материалов, а не список кандидатов для удаления.
