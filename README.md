# Motor City

Browser-first open-world car game prototype for Yandex Games.

## About the game

Motor City is an open-world driving game built around free driving, car control, drifting and activities in a city environment. The current version is an early playable prototype focused on vehicle feel, a compact urban area and the first gameplay loop.

## Current version

The project currently includes:

- Unity 6.6 + Universal Render Pipeline;
- a lightweight Web-oriented runtime scene;
- PhysX WheelCollider-based four-wheel suspension with springs and dampers;
- slip-based tire friction, rear-wheel drive, traction control and speed-dependent Ackermann steering;
- synchronized visual wheels driven by WheelCollider world poses;
- handbrake behavior and drift scoring with a combo multiplier;
- the imported Cartoon Sports Car Carrera visual with its original texture atlas converted for URP at runtime;
- a smooth orbiting chase camera with mouse look and zoom;
- a procedural city district with road lanes, sidewalks, buildings, storefronts, parking areas and street lights;
- a delivery route with visible checkpoints and a credit reward;
- a basic player wallet and credit counter;
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
- `Space` — handbrake;
- hold right mouse button and move the mouse — rotate the camera;
- mouse wheel — camera zoom;
- `R` — reset the vehicle.

## Current gameplay

Drive freely through the prototype district, use the handbrake and throttle to build drift score, or drive to the blue route marker to begin a delivery. Follow the moving route marker through the city to complete the delivery and earn credits.
