# Motor City

Browser-first open-world car game prototype for Yandex Games.

## About the game

Motor City is an open-world driving game built around free driving, car control, drifting and activities in a city environment. The current version is an early playable prototype focused on vehicle feel, a compact urban area and the first gameplay loop.

## Current version

The project currently includes:

- Unity 6.6 + Universal Render Pipeline;
- a lightweight Web-oriented runtime scene;
- transform-based car movement ported from the supplied CarController.cs;
- acceleration through a persistent movement vector, drag, a hard maximum speed and steering proportional to current movement speed;
- drift-like sliding produced by gradually aligning the movement vector back toward the car's forward direction with the Traction parameter;
- visual wheel steering and rotation without Rigidbody, WheelCollider or suspension simulation;
- drift scoring derived from the angle between the car's forward direction and its movement vector;
- a timed drift challenge in the parking area with a score target and CR reward;
- a timed street sprint with moving checkpoints and a performance-based CR reward;
- the imported Cartoon Sports Car Carrera visual with its original texture atlas converted for URP at runtime and installed before the first rendered frame;
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
4. Wait for packages to import. The setup script will create and open `Assets/Scenes/Prototype.unity` automatically.
5. Press Play.

## Controls

- `W/S` or arrow keys — throttle / brake and reverse;
- `A/D` or arrow keys — steering;
- hold right mouse button and move the mouse — rotate the camera;
- mouse wheel — camera zoom;
- `R` — reset the vehicle.

## Current gameplay

Drive freely through the prototype district and build drift score from the car's transform-based sliding, or take part in one of the current activities. The blue route marker starts the delivery route, the orange parking-lot zone starts a timed drift challenge, and the green marker starts a timed street sprint. Completing activities awards credits, which are stored locally between sessions.
