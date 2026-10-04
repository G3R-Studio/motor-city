# Motor City

Motor City is a browser-first open-world driving game for Yandex Games, built with Unity 6.6 and URP.

This repository describes the current playable project. It is not a development roadmap.

## Current project state

- Unity `6000.6.1f1`;
- Universal Render Pipeline `17.6.0`;
- production scene: `Assets/Scenes/Prototype.unity`;
- `Prototype.unity` is the only enabled build scene;
- runtime systems are assembled primarily by `MotorCityBootstrap`;
- WebGL/Yandex Games is the main release target;
- Git LFS is required for large source/runtime assets.

The game currently includes:

- open-city driving with keyboard/gamepad and mobile touch controls;
- Prometeo-based vehicle physics behind Motor City's `ArcadeCarController`;
- Comfort / Sport / Drift driving modes;
- vehicle rescue and recovery;
- persistent credits, REP, upgrades, mastery, history and specialization;
- delivery, drift, sprint, circuit, speed-trap, drift-spot, discovery, car-wash, tow-truck and profession gameplay;
- garage vehicle selection, upgrades, color/rim/neon customization and vehicle presentation camera;
- navigator/minimap, HUD, pause, store, club and result interfaces;
- first-session onboarding and story/front-end flow;
- Yandex/WebGL platform integration code, saves, purchases, analytics and remote configuration;
- baked Fantastic City Generator city with a tracked editable workbench;
- dynamic day/night cycle, sky transitions, street lighting, headlights and rear-light emission;
- runtime activity and garage markers;
- Pixie companion visuals including Haon SD and supporter-only Amane Kisora variants;
- developer/admin tools that remain excluded from normal release gameplay where appropriate.

## Runtime architecture

`MotorCityBootstrap` creates and wires most gameplay systems after `Prototype.unity` loads. The production scene is intentionally lightweight.

The runtime city is loaded from:

`Assets/Resources/MotorCity/Environment/CityVisual.prefab`

The editable Fantastic City Generator source scene is:

`Assets/LocalGenerated/FCG_Workbench.unity`

Both files are currently close to GitHub's per-file size limit. They remain tracked in their existing form; moving them to LFS requires a deliberate `git lfs migrate` operation rather than only adding an attribute rule.

Vehicle movement is provided by Prometeo. Motor City owns the higher-level gameplay layer around it, including input bridging, wheel-rig setup, handling profiles, upgrades, drift state, effects, persistence, vehicle switching and telemetry.

## Vehicles

The playable garage contains ten vehicles:

1. **BEATALL** - starting compact classic.
2. **STREETER** - balanced city car.
3. **PUG 306** - light compact hatchback.
4. **TORO 86** - lively classic coupe.
5. **HYBRED** - quick modern sports car.
6. **STUTT 996** - compact sports coupe.
7. **AMGON GT** - modern grand tourer.
8. **CAMARON** - modern muscle car.
9. **DELOREON** - supporter-pack exclusive.
10. **BUSIK** - final vehicle in the reputation ladder.

The permanent **MOTOR CITY SUPPORTER PACK** includes 5,000 credits, **DELOREON** and the exclusive **Pixie EX (Amane Kisora)** visual.

Current runtime vehicle resources:

- STREETER: `Assets/Resources/MotorCity/PlayerCarVisual.prefab`;
- HYBRED: `Assets/Resources/MotorCity/Vehicles/Player/Hybrid.prefab`;
- BEATALL: `Assets/Resources/MotorCity/Vehicles/Player/Beatall.prefab`;
- PUG 306: `Assets/Resources/MotorCity/Vehicles/Player/Peugeot306.prefab`;
- TORO 86: `Assets/Resources/MotorCity/Vehicles/Player/ToyotaAE86.prefab`;
- STUTT 996: `Assets/Resources/MotorCity/Vehicles/Player/Porsche996.prefab`;
- AMGON GT: `Assets/Resources/MotorCity/Vehicles/Player/AmgGT.prefab`;
- CAMARON: `Assets/Resources/MotorCity/Vehicles/Player/Camaro.prefab`;
- DELOREON: `Assets/Resources/MotorCity/Vehicles/Player/Delorean.prefab`;
- BUSIK: `Assets/Resources/MotorCity/Vehicles/Player/Bus.prefab`.

Most non-STREETER vehicle prefabs are prepared from project-tracked OBJ/source assets and keep their authored mesh hierarchy for paint, wheel and lighting logic.

## Garage

Garage gameplay is owned by `GarageUpgradeSystem`, the runtime HUD and `ChaseCamera`.

The current garage source content is retained under:

`Assets/Resources/MotorCity/Garage`

That folder currently contains the garage FBX/MTL and its texture set. The obsolete duplicate `SimpleGarage.prefab` and its meta file have been removed.

The older `MotorCity_PlayerGarage_Runtime` and `MotorCity_SimpleGarage` runtime-installer systems are no longer part of the game.

Current garage interaction uses the city garage marker and a deliberately small interaction radius so the player must be close to the entrance to open the garage.

## City and visuals

The city uses a baked FCG runtime prefab while the editable source remains in the tracked workbench.

Current visual systems include:

- URP materials and project-specific runtime material handling;
- architectural window emission for night lighting;
- dedicated city-backdrop shader without runtime metallic/specular treatment;
- dynamic morning/day/evening/night sky transitions;
- Fantasy Skybox FREE `FS013` sky materials for the four authored time-of-day states;
- realtime reflection-probe refreshes at controlled day/night and quality transitions;
- city fog and post-processing;
- street lights, player headlights and rear emission;
- activity/garage marker VFX based on the imported Magic Circle asset;
- foliage cutout handling for FCG vegetation.

## Input

The project currently has Unity Input System `1.20.0` installed.

`ProjectSettings` uses **Active Input Handling = Both**, because current gameplay still contains compatibility paths for the legacy Input Manager while Motor City's own input layer also handles virtual/touch actions.

Current driving controls include:

- `W/S` or Up/Down - throttle / reverse;
- `A/D` or Left/Right - steering;
- `Space` - handbrake;
- right mouse drag - camera orbit;
- mouse wheel - camera zoom;
- `E` - contextual interaction;
- `Esc` - cancel/close;
- `Enter` - retry/restart where applicable;
- `F10` or backquote - editor/development admin panel.

Drive-mode switching, rescue, pause, store, club, rewarded bonus, navigator, garage selection/upgrades and customization are exposed through the runtime HUD/touch-action layer.

## Persistence and platform

Motor City keeps persistent progression through the project save-service layer.

Tracked gameplay state includes credits, reputation, selected vehicle, upgrades, mastery and other progression systems.

The repository also contains Yandex/WebGL integration for platform readiness, cloud/save workflows, purchases, analytics and remote configuration.

## Important repository paths

- `Assets/Scripts` - runtime game code;
- `Assets/Editor` - current editor/build/import tooling;
- `Assets/Resources/MotorCity` - runtime-loaded Motor City resources;
- `Assets/Resources/MotorCity/Garage` - current garage FBX/MTL/texture source set;
- `Assets/Resources/MotorCity/Environment` - runtime city, day/night settings and generated environment materials;
- `Assets/Art/MotorCity` - project-owned source art;
- `Assets/VehicleAssets` - vehicle source meshes used by project import/build tooling;
- `Assets/LocalGenerated` - tracked editable FCG workbench;
- `Assets/Settings` - URP and graphics settings;
- `ProjectSettings` - Unity project configuration;
- `Packages` - Unity package manifest and embedded packages.

Unity-generated caches and build output such as `Library`, `Temp`, `obj`, `Logs`, `UserSettings` and local build folders are intentionally ignored.

## Third-party packages currently used

The project still depends on imported content or code from:

- **ARCADE: FREE Racing Car** - STREETER source visual/material variants;
- **PROMETEO: Car Controller** - vehicle-controller foundation;
- **Fantasy Skybox FREE** - current FS013 morning/day/evening/night sky materials;
- **Free Game VFX - Magic Circle** - base particle prefab used by Motor City activity and garage markers;
- **Haon SD Series Free Bundle** - standard Pixie visual source;
- **Amane Kisora-chan (FREE ver)** - supporter-exclusive Pixie EX source;
- **Fantastic City Generator** - city authoring source/toolchain;
- **Gudamore / Free Sports Car content** - retained vehicle source content where referenced;
- **Kenney CC0 artwork** - UI/icon source content where referenced.

Generated Motor City prefabs can still reference source prefabs, meshes, materials or animations inside these imported packages. Do not delete an entire third-party package solely because a Motor City copy exists under `Resources`; verify prefab/GUID dependencies first.

## Opening the project

1. Install Unity `6000.6.1f1` with Web Build Support.
2. Clone the repository with Git LFS installed.
3. Run `git lfs pull` if large objects were not materialized automatically.
4. Open the repository root in Unity Hub.
5. Allow Unity to recreate local caches.
6. Open `Assets/Scenes/Prototype.unity`.
7. Press Play.

Do not commit Unity's generated `Library`, build output, Asset Store download cache or downloaded `.unitypackage` archives.

## Project audit and cleanup

The native-game source/dependency audit and its verified cleanup are documented in [docs/AUDIT-2026-10-04.md](docs/AUDIT-2026-10-04.md). Source checks are repeatable with `pwsh -NoProfile -File Tools/check_cleanup.ps1` and `python Tools/audit_project.py --baseline 7bfc91e4`.

For imported dependencies and missing/repeated components, use **Motor City → Audit → Write project and loaded scene report** in Unity. The report is written under `Temp/MotorCityAudit`; it does not modify project assets or scenes. Source checks do not replace a Unity compilation, Play Mode validation or Profiler measurements.
