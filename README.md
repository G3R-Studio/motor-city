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
- runtime rear-tire smoke and persistent tire-mark trails emitted from actual WheelCollider ground-contact points while the rear tires are sliding;
- automatic integration for Mena's ARCADE: FREE Racing Car after that Asset Store package is imported into the project;
- runtime URP material conversion for the player car visual;
- a unified drift state based on Prometeo drift/traction state plus actual rear-wheel sideways slip, vehicle slip angle and grounded wheels; the same state drives drift scoring, smoke and tire marks, with free-roam drift series banked into КР when the drift ends;
- a timed drift challenge placed on the active city road network, with a score target, КР reward and a 3.5-second return grace period after leaving the activity area;
- a timed street sprint with moving checkpoints and a performance-based КР reward;
- a smooth orbiting chase camera with mouse look, zoom, speed-based look-ahead, distance and field-of-view response;
- automatic integration for **Demo City By Versatile Studio (Mobile Friendly)** when the free Asset Store package is imported locally; Motor City builds its runtime city from the package demo scene while removing the package camera/UI conflicts;
- legacy Community Core City 02 and Japanese Otaku City integrations have been removed;
- compact activity-specific world markers with lightweight built-in fallbacks;
- a compact floating garage waypoint using a CC0 Kenney Game Icons flag asset;
- moving delivery and sprint targets with a four-cone drift marker cluster;
- a compact HUD navigator that points toward the current delivery/sprint checkpoint and, during free roam, toward the nearest activity or garage with live distance;
- gameplay layout resolved dynamically from road/highway/street geometry in the active city: player spawn, garage, drift area, delivery route and sprint route are snapped to the detected road network;
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

A flat temporary test surface is used only as an emergency fallback when the Versatile Studio runtime city is unavailable.

## Run locally

1. Install Unity 6.6.1 (`6000.6.1f1`) with Web Build Support.
2. Clone this repository.
3. Open the repository root as a Unity project.
4. Import **ARCADE: FREE Racing Car** by Mena from the Unity Asset Store / Package Manager.
5. Import **PROMETEO: Car Controller** by Mena from the Unity Asset Store. Motor City does not redistribute the Prometeo package; the runtime bridge detects `PrometeoCarController` after Unity recompiles.
6. Import the free **Demo City By Versatile Studio (Mobile Friendly)** package from the Unity Asset Store. Its local `Assets/Versatile Studio Assets` source folder is intentionally ignored by Git; Motor City detects the package demo scene and generates `Assets/Resources/MotorCity/Environment/CityVisual.prefab` automatically.
7. Wait for the Motor City importers to generate the player-car visual and runtime city prefab.
8. The editor automatically prepares the runtime UI and marker sprites.
9. Wait for packages and external assets to finish importing. The setup script will create and open `Assets/Scenes/Prototype.unity` automatically and keep both Unity input backends enabled for Prometeo compatibility.
10. Press Play.

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

Drive freely through the prototype district with Prometeo-based drift handling and build drift score from the same physical slide state that produces tire smoke and road marks, or take part in one of the current activities. The blue crate marker starts the delivery route, the orange cone cluster starts a timed drift challenge at a city intersection, and the green race flag starts a timed street sprint. Only one activity can run at a time. Other mission markers are hidden while a mission is active; the purple garage marker stays visible and opening it cancels the current mission. Completing activities awards КР, which are stored locally between sessions. The purple garage marker lets the player spend those credits on persistent engine, grip and stability upgrades.
