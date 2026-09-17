# Motor City

Browser-first open-world car game prototype for Yandex Games.

## About the game

Motor City is an open-world driving game built around free driving, car control and a city environment. The current version is an early technical prototype focused on the core driving experience and browser performance.

## Current version

The project currently includes:

- Unity 6.6 + Universal Render Pipeline;
- a lightweight Web-oriented runtime scene;
- an arcade vehicle controller with throttle, reverse, steering, grip and handbrake;
- a smooth chase camera;
- a procedural prototype city grid with roads, buildings and a drift pad;
- a speed HUD;
- instant vehicle reset;
- automatic editor setup for the prototype scene and URP configuration.

The current car and environment are built from simple primitives and are used as a technical prototype rather than final game art.

## Run locally

1. Install Unity 6.6.1 (`6000.6.1f1`) with Web Build Support.
2. Clone this repository.
3. Open the repository root as a Unity project.
4. Wait for packages to import. The setup script will create and open `Assets/Scenes/Prototype.unity` automatically.
5. Press Play.

## Controls

- `W/S` or arrow keys — throttle / reverse;
- `A/D` or arrow keys — steering;
- `Space` — handbrake;
- `R` — reset the vehicle.
