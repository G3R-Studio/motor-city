# Motor City

Browser-first open-world car game prototype for Yandex Games.

## About the game

Motor City is an open-world driving game built around free driving, car control, drifting and activities in a city environment. The current version is an early playable prototype focused on vehicle feel, a compact urban area and the first gameplay loop.

## Current version

The project currently includes:

- Unity 6.6 + Universal Render Pipeline;
- a lightweight Web-oriented runtime scene;
- drift-capable rear-wheel-drive WheelCollider vehicle physics adapted to Unity 6 and the Input System, with a 0.02 s physics timestep, higher solver precision and smoothed keyboard/gamepad input;
- a 1480 kg Rigidbody chassis with rear-wheel drive, four WheelColliders, speed-sensitive steering, four-wheel service braking, anti-roll forces and a deliberately lowered center of mass;
- separate forward drive, service-brake and reverse behavior, limited reverse speed, speed-sensitive steering and dynamic rear grip for throttle and handbrake drifts;
- retuned WheelCollider suspension, damping and tyre friction, with the collider radius matched to the imported visual wheel size;
- wheel visuals synchronized to the physical WheelColliders;
- runtime rear-tire smoke and persistent tire-mark trails emitted from actual WheelCollider ground-contact points while the rear tires are sliding;
- automatic integration for Mena's ARCADE: FREE Racing Car after that Asset Store package is imported into the project;
- runtime URP material conversion for the player car visual;
- a unified drift state based on actual rear-wheel sideways slip, vehicle slip angle and grounded wheels; the same state drives drift scoring, smoke and tire marks, with free-roam drift series banked into КР when the drift ends;
- a timed drift challenge centered on a reviewed City 02 road intersection, with a score target, КР reward and a 3.5-second return grace period after leaving the activity area;
- a timed street sprint with moving checkpoints and a performance-based КР reward;
- a smooth orbiting chase camera with mouse look, zoom, speed-based look-ahead, distance and field-of-view response;
- automatic editor download and preparation of the CC0 Community Core Stack / Kenney city environment;
- an asset-based city with real road, building, vehicle, tree, street-light and prop meshes replacing the procedural primitive city when the CC0 pack is available;
- compact activity-specific world markers: a delivery crate, drift cones and a race flag, prepared from CC0 Kenney assets;
- a compact floating garage waypoint using a CC0 Kenney Game Icons flag asset;
- moving delivery and sprint targets with a four-cone drift marker cluster;
- gameplay layout resolved from named road objects in the City 02 prefab: player spawn, garage, drift area, delivery route and sprint route all follow reviewed city geometry instead of guessed world coordinates;
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

Drive freely through the prototype district with rear-wheel-drive drift handling and build drift score from the same WheelCollider slip state that produces tire smoke and road marks, or take part in one of the current activities. The blue crate marker starts the delivery route, the orange cone cluster starts a timed drift challenge at a city intersection, and the green race flag starts a timed street sprint. Only one activity can run at a time. Other mission markers are hidden while a mission is active; the purple garage marker stays visible and opening it cancels the current mission. Completing activities awards КР, which are stored locally between sessions. The purple garage marker lets the player spend those credits on persistent engine, grip and stability upgrades.
