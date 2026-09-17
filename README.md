# Motor City

Browser-first open-world car game prototype for Yandex Games.

## About the game

Motor City is an open-world driving game built around free driving, car control, drifting and activities in a city environment. The current version is an early playable prototype focused on vehicle feel, a compact urban area and the first gameplay loop.

## Current version

The project currently includes:

- Unity 6.6 + Universal Render Pipeline;
- a lightweight Web-oriented runtime scene;
- Pro Drift Controller v1 vehicle physics adapted to Unity 6 and the Input System, including the source project's 0.02 s physics timestep, solver settings and legacy Input.GetAxis snap behavior;
- a Rigidbody chassis with four driven WheelColliders, a decoupled physical wheelbase/track derived from the source prefab proportions and optional body colliders from the imported racing-car prefab;
- the source controller's steering interpolation, four-wheel motor torque, reverse drag and acceleration behavior;
- the source prefab's WheelCollider suspension and friction settings;
- wheel visuals synchronized to the physical WheelColliders;
- automatic integration for Mena's ARCADE: FREE Racing Car after that Asset Store package is imported into the project;
- runtime URP material conversion for the player car visual;
- drift scoring based on actual vehicle slip and movement angle;
- a timed drift challenge in the parking area with a score target and CR reward;
- a timed street sprint with moving checkpoints and a performance-based CR reward;
- a smooth orbiting chase camera with mouse look and zoom;
- a procedural city district with road lanes, sidewalks, buildings, storefronts, parking areas and street lights;
- a delivery route with visible checkpoints and a credit reward;
- a locally persistent player wallet and credit counter;
- a HUD with speed, credits, delivery state and drift score;
- instant vehicle reset;
- automatic editor setup for the prototype scene and URP configuration.

The city environment is still generated from lightweight prototype geometry and is not final game art.

## Run locally

1. Install Unity 6.6.1 (`6000.6.1f1`) with Web Build Support.
2. Clone this repository.
3. Open the repository root as a Unity project.
4. Import **ARCADE: FREE Racing Car** by Mena from the Unity Asset Store / Package Manager.
5. Wait for the Motor City importer to generate `Assets/Resources/MotorCity/PlayerCarVisual.prefab`.
6. Wait for packages to import. The setup script will create and open `Assets/Scenes/Prototype.unity` automatically.
7. Press Play.

The car importer prefers the blue mesh-only racing-car prefab and falls back to another matching racing-car prefab if that exact variant is unavailable.

## Controls

- `W/S` or arrow keys — throttle / reverse;
- `A/D` or arrow keys — steering;
- hold right mouse button and move the mouse — rotate the camera;
- mouse wheel — camera zoom;
- `R` — reset the vehicle.

## Current gameplay

Drive freely through the prototype district and build drift score from the WheelCollider vehicle slip, or take part in one of the current activities. The blue route marker starts the delivery route, the orange parking-lot zone starts a timed drift challenge, and the green marker starts a timed street sprint. Completing activities awards credits, which are stored locally between sessions.
