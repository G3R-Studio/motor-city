# Motor City — AI Project Context

This file is intended for an external AI/reviewer that needs to understand the current Motor City project as completely as possible from the repository snapshot.

## What this repository is

Motor City is a browser-first open-world driving game prototype targeting Yandex Games.

Current Unity version:

- Unity 6.6.1
- Editor version: `6000.6.1f1`
- Universal Render Pipeline: `17.6.0`
- Unity Input System: `1.20.0`

The main playable scene is lightweight. Most of the runtime world, vehicle integration and gameplay objects are created or installed by Motor City scripts at runtime.

## Important limitation of this snapshot

This repository intentionally does **not** redistribute third-party Asset Store packages.

The snapshot can contain generated scenes/prefabs that reference those assets, but the original third-party source assets remain local.

Important local dependencies include:

- Fantastic City Generator
- PROMETEO: Car Controller by Mena
- ARCADE: FREE Racing Car by Mena

Do not assume a missing referenced mesh/material/prefab is a Motor City bug until you verify whether it belongs to one of those omitted packages.

The generated project state is still included where possible so the scene hierarchy, transforms, references and Motor City integration can be inspected.

## Files that should be treated as authoritative

Start with:

- `README.md` — current user-visible feature/state summary.
- `Assets/Scripts/` — runtime gameplay code.
- `Assets/Editor/` — import/build/fixer tools and generated-city pipeline.
- `Assets/Shaders/` — Motor City shaders.
- `ProjectSettings/` — Unity project configuration.
- `Packages/manifest.json` — Unity package dependencies.
- `Assets/Scenes/Prototype.unity` — lightweight bootstrap scene.

For the actual generated city state, inspect when present:

- `Assets/LocalGenerated/FCG_Workbench.unity`
- `Assets/Resources/MotorCity/Environment/CityVisual.prefab`
- `Assets/Resources/MotorCity/Environment/`
- `MotorCity_FCGSceneReport.txt`
- `MotorCity_CityAssetReport.txt`

For the generated player car/UI/markers, inspect when present:

- `Assets/Resources/MotorCity/PlayerCarVisual.prefab`
- `Assets/Resources/MotorCity/UI/`
- `Assets/Resources/MotorCity/Markers/`

These generated files are intentionally tracked for AI review even though they may be regenerated locally.

## Runtime architecture

The project is bootstrapped from Motor City code rather than a large hand-authored gameplay scene.

The high-level order is:

1. create runtime lighting;
2. install/load the generated FCG city;
3. initialize day/night behavior;
4. construct/install the player car;
5. create gameplay activities and markers;
6. create chase camera;
7. create HUD and garage UI.

The primary bootstrap code is under:

- `Assets/Scripts/Bootstrap/`

The active generated city is installed by:

- `Assets/Scripts/World/CityAssetRuntimeInstaller.cs`

## Vehicle

Vehicle physics comes from the locally imported Prometeo Car Controller.

Motor City wraps/integrates it so the rest of the game does not directly depend on the Asset Store implementation.

Important properties of the current vehicle setup:

- four WheelColliders are generated from measured wheel positions;
- the imported racing-car model is realigned before the physics rig is created;
- visual wheels remain separate from WheelColliders;
- imported visual colliders/rigidbodies are stripped where they would conflict with the Motor City chassis;
- throttle, braking, steering and handbrake are routed through the Motor City integration;
- drift state combines controller traction/drift state with actual wheel slip and vehicle slip angle;
- drift smoke and tire marks use that shared drift state.

Do not casually retune the current vehicle handling unless a specific bug requires it. The present tuning was accepted during development.

## Camera

The chase camera is:

- orbitable with RMB + mouse;
- zoomable with mouse wheel;
- speed-aware;
- non-physical;
- protected from walls/buildings with damped SphereCast obstacle avoidance.

Main file:

- `Assets/Scripts/Camera/ChaseCamera.cs`

The camera should never receive a Rigidbody or active Collider.

## Fantastic City Generator integration

The city is produced from a locally installed Fantastic City Generator package.

Motor City provides tooling to:

- find/use the saved FCG workbench scene;
- convert legacy FCG materials to URP;
- repair duplicate FCG material names by source GUID;
- use a dedicated two-sided alpha-cutout foliage shader;
- build a runtime `CityVisual.prefab`;
- preserve/generate Motor City-compatible street lighting;
- repair runtime city collision.

Relevant editor code lives under:

- `Assets/Editor/FantasticCityGenerator*.cs`

The generated workbench is normally:

- `Assets/LocalGenerated/FCG_Workbench.unity`

The generated runtime city is normally:

- `Assets/Resources/MotorCity/Environment/CityVisual.prefab`

## City collision rules

The city collision system has deliberately different behavior for different object classes.

Buildings:

- architectural meshes under building hierarchies receive MeshColliders where needed.

Decorative street furniture:

- street lights;
- park lamps;
- traffic lights;
- hydrants;
- trash bins;
- benches;
- poles;
- recognized road signs;

are intentionally non-blocking. Their colliders are disabled so the player can pass through them.

The previous breakable-object system was removed.

Roads/highways:

- legacy or unsuitable highway colliders are disabled;
- road collision is rebuilt from visible road geometry;
- the connecting highway is hilly and its physics must preserve the visual climbs/descents;
- `HW-F-400-01`, `HW-F-400-02` and `HW-F-400-04` are special-cased;
- their `HighWay`, `Grass` and `GuardRail` / `Guard-Rail` visual meshes receive exact MeshColliders at runtime.

There is also a hidden emergency safety floor placed below the lowest city geometry. It is not intended to replace road collision.

Relevant files:

- `Assets/Scripts/World/CityAssetRuntimeInstaller.cs`
- `Assets/Scripts/World/CityCollisionUtility.cs`

## Current connecting highway structure

The important generated highway sections are:

- `HW-F-400-01(Clone)`
- `HW-F-400-02(Clone)`
- `HW-F-400-04(Clone)`

Each section contains separate visual geometry for the road and roadside elements.

Known child names include:

- `Grass`
- `GuardRail` or `Guard-Rail`

Do not flatten this highway. Its hills, descents and rises are intentional.

If reviewing collision problems, compare the exact visual mesh, generated MeshCollider, hierarchy transform, scale and source mesh for each of those three sections.

## Vegetation

FCG foliage originally showed white/incorrectly shaded halves.

The current solution uses:

- source-GUID-aware material repair;
- `MotorCity/TwoSidedFoliage`;
- double-sided rendering;
- alpha cutoff;
- neutral foliage tint;
- removal of problematic inherited normal/specular settings.

Relevant files:

- `Assets/Editor/FantasticCityGeneratorUrpFixer.cs`
- `Assets/Shaders/MotorCityTwoSidedFoliage.shader`

Do not revert foliage to a generic one-sided URP/Lit setup.

## Day/night and street lights

The game has a runtime day/night cycle using FCG-derived sky/environment settings.

Motor City does not globally brighten city materials at night.

Street-lamp anchors named like:

- `Spot Light`
- `_Spot_Light`

receive/use dedicated realtime Spot Lights.

Lamp discovery is cached rather than repeatedly scanning the whole FCG scene, because repeated scene-wide scans caused periodic gameplay stalls.

Relevant files:

- `Assets/Scripts/World/DayNightCycleController.cs`
- `Assets/Scripts/World/DayNightSettings.cs`
- `Assets/Editor/FantasticCityGeneratorDayNightBuilder.cs`

## Traffic lights

Traffic lights are currently **static**.

A dynamic real-world traffic-light cycle was experimented with and then intentionally removed.

Pedestrian signal visuals are sanitized/frozen so overlapping walk/stop graphics should not flicker.

Do not reintroduce timed traffic-light phases unless explicitly requested.

Relevant file:

- `Assets/Scripts/World/TrafficSignalVisualUtility.cs`

## Gameplay

Current gameplay includes:

- free driving;
- drift scoring;
- timed drift challenge;
- delivery activity;
- street sprint;
- two-lap circuit race;
- KR currency;
- garage upgrades for engine, grip and stability;
- persistent local progress/personal bests;
- reset;
- HUD navigation and activity markers.

Gameplay systems live primarily under:

- `Assets/Scripts/Gameplay/`
- `Assets/Scripts/UI/`
- `Assets/Scripts/World/`
- `Assets/Scripts/Vehicle/`

## Audio

Game audio was intentionally removed.

Do not treat missing vehicle/audio systems as incomplete work unless audio is explicitly requested again.

## Generated snapshot workflow

Before handing this repository to another reviewer/model, regenerate the current local state in Unity when relevant:

1. `Motor City > Fantastic City Generator > Fix Materials in Saved FCG City`
2. `Motor City > Fantastic City Generator > Build Day-Night Settings`
3. `Motor City > Fantastic City Generator > Build Runtime City from Saved FCG City`

Then stage the generated files that are no longer ignored.

## What a reviewer should check

When doing a full technical review, inspect at least:

- compile errors and obsolete APIs;
- runtime allocations / scene-wide searches / GC spikes;
- WebGL/Yandex Games compatibility;
- runtime-generated GameObject/component count;
- number and cost of realtime lights;
- MeshCollider count and complexity;
- the exact connecting-highway collision;
- camera collision behavior;
- player-car physics setup and WheelCollider alignment;
- drift state consistency;
- activity state conflicts;
- save/persistence handling;
- UI scaling and mobile/browser input assumptions;
- null/missing asset behavior when local Asset Store packages are absent;
- generated material/shader compatibility with URP 17.6;
- whether generated assets contain accidental third-party source data that should not be redistributed.

## Review philosophy

Prefer evidence from the repository and generated files over assumptions.

When something is generated, inspect both:

1. the generator/editor code; and
2. the generated result.

Do not recommend replacing systems simply because the third-party source package itself is absent from GitHub. First determine whether the generated snapshot and integration code provide enough evidence to review the behavior.
