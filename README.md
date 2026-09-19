# Motor City

Browser-first open-world car game prototype for Yandex Games.

## About the game

Motor City is an open-world driving game built around free driving, car control, drifting and activities in a city environment. The current version is an early playable prototype focused on vehicle feel, a compact urban area and the first gameplay loop.

## Current version

The project currently includes:

- Unity 6.6 + Universal Render Pipeline;
- a lightweight Web-oriented runtime scene;
- Prometeo Car Controller as the vehicle physics source, connected at runtime through a Motor City integration bridge so gameplay systems do not depend directly on the Asset Store script; Motor City feeds Prometeo through runtime touch-input proxies driven by the Unity Input System, so keyboard/gamepad controls do not depend on Prometeo's legacy Input.GetKey path;
- four Prometeo WheelColliders generated from the actual imported wheel centers and measured wheel radius instead of guessed wheelbase/track values;
- Prometeo acceleration, steering, service braking, coasting and handbrake traction-loss behavior, with Motor City garage upgrades mapped onto the controller tuning;
- the imported racing-car visual automatically realigned so its measured wheelbase follows the Motor City vehicle forward axis before the physics rig is created;
- wheel meshes kept separate from their WheelColliders, as required by Prometeo, with the visual wheel roots driven by Prometeo wheel poses;
- runtime rear-tire smoke with dense, high-opacity particles and tire-mark trails emitted from actual WheelCollider ground-contact points while the rear tires are sliding;
- automatic integration for Mena's ARCADE: FREE Racing Car after that Asset Store package is imported into the project;
- runtime URP material conversion for the player car visual;
- a unified drift state based on Prometeo drift/traction state plus actual rear-wheel sideways slip, vehicle slip angle and grounded wheels; the same state drives drift scoring, smoke and tire marks, with free-roam drift series banked into КР when the drift ends;
- a timed drift challenge placed on the active city road network, with a score target, КР reward and a 3.5-second return grace period after leaving the activity area;
- a timed street sprint with moving checkpoints and a performance-based КР reward;
- a two-lap circuit race around the large district with moving checkpoints, time-based КР reward and a locally saved personal best time;
- a smooth orbiting chase camera with mouse look, zoom, speed-based look-ahead, distance and field-of-view response; the runtime camera is explicitly stripped of colliders and rigidbodies so it cannot hit street props;
- editor tooling for **Fantastic City Generator**: generated FCG renderers in the active scene can be converted from legacy/Built-in materials to URP/Lit while preserving base textures, normal maps, occlusion, emission and transparent/cutout behavior; the generated `City-Maker` can then be baked into a local runtime `CityVisual.prefab`;
- legacy Community Core City 02 and Japanese Otaku City integrations have been removed;
- compact activity-specific world markers with lightweight built-in fallbacks;
- a compact floating garage waypoint using a CC0 Kenney Game Icons flag asset;
- moving delivery and sprint targets with a four-cone drift marker cluster;
- a compact HUD navigator that points toward the current delivery/sprint checkpoint and, during free roam, toward the nearest activity or garage with live distance;
- the active city uses the latest locally generated Fantastic City Generator layout with a large main district, a compact remote district and a three-section connecting highway; the runtime installer resolves `FCG_Roads` and `FCG_HighWay` by exact MeshCollider triangle/submesh material, explicitly excludes highway guardrails from driveable surfaces, keeps delivery inside the large district, places the garage on a real parking surface, and routes the street sprint across the highway into the compact district;
- breakable street furniture is baked into the generated city: street lights, park lamps, traffic lights, hydrants, benches, poles and recognized road signs use non-blocking trigger colliders, take only a small amount of vehicle speed on impact, fall without spinning, fade away and stop colliding with the player car; FCG street/park lamp Light components are preserved in the runtime city with shadows disabled for performance and switch off immediately when the pole is knocked down;
- a delivery route with visible checkpoints and a credit reward;
- a locally persistent player wallet and credit counter;
- a purple garage zone with three persistent upgrade paths: engine, grip and stability, each with three paid levels and clearly noticeable per-level effects;
- activity coordination so delivery, drift challenge, street sprint and garage cannot overlap;
- mission markers hide while another mission is active, while the garage marker remains visible;
- entering the garage cancels the active mission;
- Russian in-game HUD, garage text, activity prompts and status messages;
- a compact dark racing-style Canvas HUD with responsive text fitting, separate speedometer, activity status, controls hint and contextual drift panel;
- a redesigned garage overlay with three clearly separated upgrade rows and automatic text resizing so long labels remain inside their panels;
- instant vehicle reset;
- automatic editor setup for the prototype scene and URP configuration.

A flat temporary test surface is currently used when no generated runtime city prefab is available.

## Run locally

1. Install Unity 6.6.1 (`6000.6.1f1`) with Web Build Support.
2. Clone this repository.
3. Open the repository root as a Unity project.
4. Import **ARCADE: FREE Racing Car** by Mena from the Unity Asset Store / Package Manager.
5. Import **PROMETEO: Car Controller** by Mena from the Unity Asset Store. Motor City does not redistribute the Prometeo package; the runtime bridge detects `PrometeoCarController` after Unity recompiles.
6. Import **Fantastic City Generator** locally under `Assets/Fantastic City Generator`, generate the city in an editor scene, then use `Motor City > Fantastic City Generator > Fix Materials in Saved FCG City` to convert the generated city materials for URP.
7. Save the generated scene, then use `Motor City > Fantastic City Generator > Build Runtime City from Saved FCG City` to bake the local generated city into `Assets/Resources/MotorCity/Environment/CityVisual.prefab`. The generated runtime environment folder is intentionally ignored by Git.
8. The FCG material/build tools can be launched while `Prototype.unity` is open: they automatically find the saved `City-Maker` scene under `Assets/LocalGenerated`, open it temporarily, process/save it, rebuild `CityVisual.prefab`, then return to the previous scene.
9. The editor automatically prepares the runtime UI and marker sprites.
10. Wait for packages and external assets to finish importing. The setup script will create and open `Assets/Scenes/Prototype.unity` automatically and keep both Unity input backends enabled for Prometeo compatibility.
11. Press Play.

The car importer prefers a matching racing-car prefab with usable body colliders and falls back to another matching prefab if needed.

## Controls

- `W/S` or arrow keys — throttle / reverse;
- `A/D` or arrow keys — steering;
- `Space` — Prometeo handbrake / traction break for initiating and sustaining a drift;
- hold right mouse button and move the mouse — rotate the camera;
- mouse wheel — camera zoom;
- `E` — open/close the garage while stopped in the purple garage marker;
- `1/2/3` — buy engine/grip/stability upgrades while the garage is open;
- `R` — reset the vehicle.

## Current gameplay

Drive freely through the prototype district with Prometeo-based drift handling and build drift score from the same physical slide state that produces tire smoke and road marks, or take part in one of the current activities. The blue crate marker starts the delivery route, the orange cone cluster starts a timed drift challenge at a city intersection, the green race flag starts a timed street sprint, and the cyan flag starts a two-lap circuit race around the large district. Only one activity can run at a time. Other mission markers are hidden while a mission is active; the purple garage marker stays visible and opening it cancels the current mission. Completing activities awards КР, which are stored locally between sessions. The purple garage marker lets the player spend those credits on persistent engine, grip and stability upgrades.

- editor diagnostics include a deep local cleanup audit (`Motor City > Diagnostics > Export Local Project Audit`) that scans empty/stale folders, runtime/saved-city dependency closure, literal Resources references, unreferenced Resources candidates, and FCG assets not used by the current saved city. It reports candidates only and does not delete project files.
