# Motor City

Browser-first open-world driving game prototype for Yandex Games, built with Unity 6.6 and URP.

## Current project state

The repository contains the current playable project rather than a roadmap. The active gameplay build is centered on `Assets/Scenes/Prototype.unity`; the scene itself stays intentionally lightweight and `MotorCityBootstrap` assembles the runtime systems in code.

Current core features include:

- Unity `6000.6.1f1` + URP;
- Prometeo-based vehicle physics behind the Motor City `ArcadeCarController` bridge;
- keyboard/gamepad driving plus runtime HUD/touch controls;
- Comfort / Sport / Drift driving modes;
- persistent credits, REP, upgrades, vehicle mastery/history/specialization and save data;
- delivery, drift, sprint, circuit, speed-trap, drift-spot, stunt, discovery and additional city activity systems;
- runtime HUD, garage, navigator, pause, store, club and result interfaces;
- Yandex/WebGL platform, cloud-save, purchase, analytics and remote-config integration code;
- a baked Fantastic City Generator runtime city plus a tracked editable FCG workbench;
- day/night, street lighting, player headlights, rear-light emission and traffic optimization;
- runtime-built activity/garage markers;
- Pixie/Byte companion visual prepared from the tracked Haon source assets;
- a temporary admin/debug panel for development testing.

## Vehicles

The playable garage currently contains nine vehicles:

1. **STREET** — the ARCADE: FREE Racing Car based starter vehicle.
2. **HYBRID** — the Gudamore Free Sports Car based vehicle.
3. **BEATALL** — a compact classic player car imported from the standalone OBJ source.
4. **DELOREAN** — a low sports coupe assembled from its body plus separate front/rear wheel meshes.
5. **AMG GT** — a modern grand-touring coupe assembled from its body plus separate front/rear wheel meshes.
6. **PORSCHE 996** — a compact sports coupe assembled from its body plus separate front/rear wheel meshes.
7. **PEUGEOT 306** — a compact hatchback assembled from its body plus a shared wheel mesh.
8. **TOYOTA AE86** — a lightweight classic coupe assembled from its body plus a shared wheel mesh.
9. **CAMARO** — a wide modern muscle coupe assembled from its body plus a shared wheel mesh.

No old Designersoup/PolyPack/Muscle/GT/Apex player-car roster is part of the current project.

Runtime vehicle assets:

- STREET: `Assets/Resources/MotorCity/PlayerCarVisual.prefab`
- HYBRID: `Assets/Resources/MotorCity/Vehicles/Player/Hybrid.prefab`
- BEATALL: `Assets/Resources/MotorCity/Vehicles/Player/Beatall.prefab` (generated from `Assets/VehicleAssets/Beatall/beatall.obj`)
- DELOREAN: `Assets/Resources/MotorCity/Vehicles/Player/Delorean.prefab` (generated from `Assets/VehicleAssets/Delorean/delorean.obj` plus front/rear wheel OBJ files)
- AMG GT: `Assets/Resources/MotorCity/Vehicles/Player/AmgGT.prefab` (generated from `Assets/VehicleAssets/AmgGT/amggt.obj` plus front/rear wheel OBJ files)
- PORSCHE 996: `Assets/Resources/MotorCity/Vehicles/Player/Porsche996.prefab` (generated from `Assets/VehicleAssets/Porsche996/996.obj` plus front/rear wheel OBJ files)
- PEUGEOT 306: `Assets/Resources/MotorCity/Vehicles/Player/Peugeot306.prefab` (generated from `Assets/VehicleAssets/Peugeot306/306.obj` plus `all_wheels.obj`)
- TOYOTA AE86: `Assets/Resources/MotorCity/Vehicles/Player/ToyotaAE86.prefab` (generated from `Assets/VehicleAssets/ToyotaAE86/ae86.obj` plus `all_wheels.obj`)
- CAMARO: `Assets/Resources/MotorCity/Vehicles/Player/Camaro.prefab` (generated from `Assets/VehicleAssets/Camaro/camaro.obj` plus `all_wheels.obj`)

## City

The runtime city is stored at:

`Assets/Resources/MotorCity/Environment/CityVisual.prefab`

The editable Fantastic City Generator source scene is:

`Assets/LocalGenerated/FCG_Workbench.unity`

The workbench is deliberately tracked because it is the editable source for the currently baked city. The runtime builder keeps `CityVisual.prefab` as the gameplay-facing baked copy.

The production FCG editor toolchain retained by the project covers:

- opening/creating the workbench;
- locating the saved source scene;
- URP material conversion;
- day/night settings generation;
- baking the runtime city prefab.

Historical one-off FCG repair/diagnostic scripts and package demo scenes are not part of the cleaned production project.

## Runtime architecture

`MotorCityBootstrap` creates and wires the gameplay systems after the Prototype scene loads. This is why the build scene contains very little authored scene hierarchy.

Vehicle movement is provided by Prometeo, while Motor City owns the higher-level behavior: input proxies, wheel-rig creation, handling profiles, upgrades, drift state, smoke/tire marks, persistence and gameplay telemetry.

`Prototype.unity` is the production bootstrap scene and the only enabled build scene.

## Controls

Keyboard/gamepad driving controls that remain active:

- `W/S` or Up/Down — throttle / reverse;
- `A/D` or Left/Right — steering;
- `Space` — handbrake;
- right mouse drag — camera orbit;
- mouse wheel — camera zoom;
- `E` — contextual interaction;
- `Esc` — cancel/close;
- `Enter` — retry/restart where applicable;
- `F10` or backquote — temporary admin/debug panel.

Actions such as drive-mode switching, rescue, pause, store, club, rewarded bonus, navigator, garage vehicle selection/upgrades and customization are exposed through the runtime HUD/touch-button input layer rather than dedicated keyboard bindings.

## Repository layout

Important project-owned paths:

- `Assets/Scripts` — runtime game code;
- `Assets/Editor` — current editor/build/import tooling;
- `Assets/Resources/MotorCity` — runtime-loaded Motor City content;
- `Assets/Art/MotorCity` — Motor City source UI/marker/garage art;
- `Assets/MotorCity` — clean Motor City authoring prototypes;
- `Assets/LocalGenerated` — tracked editable FCG workbench;
- `Assets/Settings` — URP/build profile assets;
- `ProjectSettings` — Unity project configuration;
- `Packages` — Unity package manifest plus the embedded spring-bone package.

Third-party source folders still present are retained because current generated/runtime content or editor rebuild tooling depends on them. Their demo scenes, guide assets and clearly unused sample material have been removed where safe.

## Opening the project

1. Install Unity `6000.6.1f1` with Web Build Support.
2. Clone the repository with Git LFS available.
3. Run `git lfs pull` if LFS objects were not materialized automatically.
4. Open the repository root in Unity Hub.
5. Allow Unity to recreate `Library`, shader caches and generated IDE files.
6. Open `Assets/Scenes/Prototype.unity` if it is not already open.
7. Press Play.

The repository intentionally does not track Unity-generated caches such as `Library`, `Temp`, `obj`, `Logs`, `UserSettings`, IDE project files or editor caches.

## Third-party dependencies

The project currently uses source/content from several third-party packages, including:

- Mena — ARCADE: FREE Racing Car;
- Mena — PROMETEO: Car Controller;
- Fantastic City Generator;
- Gudamore — Free Sports Car;
- Haon SD Series Free Bundle;
- Eric VFX Studio Magic Circle;
- Kenney CC0 UI/icon artwork.

Project-specific source notes are stored under `Assets/ThirdParty`. Original package/license terms continue to apply to their respective assets.
