# Motor City

Browser-first open-world car game prototype for Yandex Games.

## About the game

Motor City is an open-world driving game built around free driving, car control, drifting and activities in a city environment. The current version is an early playable prototype focused on vehicle feel, a compact urban area and the first gameplay loop.

## Current version

The project currently includes:

- Unity 6.6 + Universal Render Pipeline;
- a lightweight Web-oriented runtime scene;
- road-focused WheelCollider vehicle physics adapted to Unity 6 and the Input System, with a 0.02 s physics timestep, higher solver precision and smoothed keyboard/gamepad input;
- a 1480 kg Rigidbody chassis with rear-wheel drive, four WheelColliders, speed-sensitive steering, four-wheel service braking, anti-roll forces and a deliberately lowered center of mass;
- separate forward drive, service-brake and reverse behavior, limited reverse speed, aerodynamic downforce and more progressive high-speed steering;
- retuned WheelCollider suspension, damping and tyre friction, with the collider radius matched to the imported visual wheel size;
- wheel visuals synchronized to the physical WheelColliders;
- automatic integration for Mena's ARCADE: FREE Racing Car after that Asset Store package is imported into the project;
- runtime URP material conversion for the player car visual;
- drift scoring based on actual vehicle slip and movement angle, with free-roam drift series banked into КР when the drift ends;
- a timed drift challenge in the parking area with a score target, КР reward, a larger square activity area and a 3.5-second return grace period after leaving the zone;
- a timed street sprint with moving checkpoints and a performance-based КР reward;
- a smooth orbiting chase camera with mouse look, zoom, speed-based look-ahead, distance and field-of-view response;
- automatic editor download and preparation of the CC0 Community Core Stack / Kenney city environment;
- an asset-based city with real road, building, vehicle, tree, street-light and prop meshes replacing the procedural primitive city when the CC0 pack is available;
- compact activity-specific world markers: a delivery crate, drift cones and a race flag, prepared from CC0 Kenney assets;
- a compact floating garage waypoint using a CC0 Kenney Game Icons flag asset;
- moving delivery and sprint targets with a four-cone drift marker cluster;
- automatic road-mesh snapping for the player spawn, delivery checkpoints, sprint checkpoints and garage position;
- a delivery route with visible checkpoints and a credit reward;
- a locally persistent player wallet and credit counter;
- a purple garage zone with three persistent upgrade paths: engine, grip and stability, each with three paid levels and clearly noticeable per-level effects;
- activity coordination so delivery, drift challenge, street sprint and garage cannot overlap;
- mission markers hide while another mission is active, while the garage marker remains visible;
- entering the garage cancels the active mission;
- Russian in-game HUD, garage text, activity prompts and status messages;
- a Canvas-based runtime HUD and garage interface using automatically prepared Kenney CC0 UI panels instead of IMGUI;
- a HUD with speed, credits, current activity, drift score and installed upgrade levels;
- instant vehicle reset;
- automatic editor setup for the prototype scene and URP configuration.

Primitive geometry remains only as an emergency fallback if the external CC0 asset installer cannot prepare the city assets.

## Run locally

1. Install Unity 6.6.1 (`6000.6.1f1`) with Web Build Support.
2. Clone this repository.
3. Open the repository root as a Unity project.
4. Import **ARCADE: FREE Racing Car** by Mena from the Unity Asset Store / Package Manager.
5. Wait for the Motor City importer to generate `Assets/Resources/MotorCity/PlayerCarVisual.prefab`.
6. The editor will also automatically download the CC0 Community Core Stack City 02 / Kenney environment and generate the runtime city and activity prop prefabs under `Assets/Resources/MotorCity/Environment`.
7. The editor automatically downloads the Kenney CC0 UI Pack and prepares the runtime UI sprites under `Assets/Resources/MotorCity/UI`.
8. Wait for packages and external assets to finish importing. The setup script will create and open `Assets/Scenes/Prototype.unity` automatically.
9. Press Play.

The car importer prefers a matching racing-car prefab with usable body colliders and falls back to another matching prefab if needed.

## Controls

- `W/S` or arrow keys — throttle / reverse;
- `A/D` or arrow keys — steering;
- `Space` — rear-wheel handbrake at speed and four-wheel parking brake near a stop;
- hold right mouse button and move the mouse — rotate the camera;
- mouse wheel — camera zoom;
- `E` — open/close the garage while stopped in the purple garage marker;
- `1/2/3` — buy engine/grip/stability upgrades while the garage is open;
- `R` — reset the vehicle.

## Current gameplay

Drive freely through the prototype district with rear-wheel-drive road-car handling and build drift score from real WheelCollider slip, or take part in one of the current activities. The blue route marker starts the delivery route, the orange parking-lot zone starts a timed drift challenge, and the green marker starts a timed street sprint. Only one activity can run at a time. Other mission markers are hidden while a mission is active; the purple garage marker stays visible and opening it cancels the current mission. Completing activities awards КР, which are stored locally between sessions. The purple garage marker lets the player spend those credits on persistent engine, grip and stability upgrades.
