# Phase 4 — проверенный каталог ролей материалов (2026-10-09)

Источники: закоммиченные `Assets/VehicleAssets/*/*.mtl`, Unity prefab audit и ранее подтверждённая пользователем визуальная проверка машин. Этот документ фиксирует **однозначные** соответствия, а не пытается угадать назначение общих material slots.

| Модель | Кузов (явный) | Стекло | Передние лампы | Задние лампы | Неоднозначные материалы |
| --- | --- | --- | --- | --- | --- |
| AmgGT | — | blackGlass.008 | MC_Headlight | MC_Brake | baseGradient.008, gradientEmmisive.008 |
| Beatall | — | blackGlass.002 | MC_Headlight | MC_Brake | baseGradient.001, gradientEmmisive.001 |
| Bus | — | blackGlass.003 | MC_Headlight | MC_Brake | Material.006 |
| Camaro | — | blackGlass.004 | MC_Headlight | MC_Brake | Color, Color_Bloom |
| Delorean | — | blackGlass.001 | MC_Headlight_L/R | MC_Brake_L/R | baseGradient.003, gradientEmmisive.002, MC_Cyan |
| Hybrid | — | blackGlass | — | — | Material.001–005, Material.005_RearUnlit |
| Peugeot306 | carPaint | blackGlass | headlights | rearLights | chrome, plastic, empty |
| Porsche996 | carPaint | blackGlass | headlights | rearLights | chrome, plastic, empty, indicators |
| ToyotaAE86 | carPaint.002 | blackGlass.002 | не выделены в body MTL | rearLights.002 | chrome.003, plastic.003, empty.003, indicators.001 |

Ключевые решения:

- `VehicleMaterialRole` перечисляет Body, Glass, Mirror, FrontLamp, RearLamp, Rim, Rubber. `VehicleVisualRoles` поддерживает роли отдельных renderer material slots.
- `VehicleMaterialRoleCatalog` безопасно сопоставляет точные исходные названия. При установке визуальной модели `ArcadeRacingCarRuntimeInstaller` добавляет только **метаданные ролей к runtime-клону**, не меняя ни материалы, ни shader, ни prefab asset.
- `VehicleAuthoredLampLights` использует явные роли для известных lamp materials; для нераспознанных и нестандартных источников сохраняется legacy fallback. `VehicleCustomizationSystem` учитывает Body, не перекрашивая все material slots смешанного mesh.
- Для некоторых моделей колёса и зеркала используют общие/неописательные материалы. Обозначать их как Rim, Rubber или Mirror на основании `chrome`, `plastic`, `Material.00x`, `Color` или `baseGradient` было бы недостоверно. Явные теги для этих slots требуют проверки фактических submesh границ в Unity.
- QA исходного внешнего вида всех машин ранее подтверждён пользователем. Source MTL contract проверяет `Tools/check_phase4_material_roles.py`; Unity material audit проверяет runtime tagging на временных копиях и неизменность `sharedMaterials`.

**Граница доказательств:** этот этап реализует и проверяет безопасный runtime migration layer, но **не** является доказательством завершённой serialized-prefab tagging миграции всех slots. До окончания полной Phase 4 остаются неоднозначные материал-слоты и только после их разметки возможно удалить соответствующие legacy эвристики.
