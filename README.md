# Motor City

Browser-first open-world car game prototype for Yandex Games.

## Current prototype

The repository now contains the first Unity 6 foundation:

- Unity 6 + Universal Render Pipeline package;
- Web-oriented lightweight runtime scene;
- arcade vehicle controller with throttle, reverse, steering, grip and handbrake;
- smooth chase camera;
- procedural prototype city grid and drift pad;
- speed HUD and instant car reset;
- automatic editor setup that creates `Assets/Scenes/Prototype.unity` and configures URP/build settings on first import.

The current car and city are intentionally built from primitives. They are a technical test bed, not final art.

## Run locally

1. Install Unity 6 (6000.0 LTS or a newer compatible Unity 6 editor) with Web build support.
2. Clone this repository.
3. Open the repository root as a Unity project.
4. Wait for packages to import. The project setup script will create/open `Assets/Scenes/Prototype.unity` automatically.
5. Press Play.

Controls:

- `W/S` or arrows — throttle / reverse;
- `A/D` or arrows — steering;
- `Space` — handbrake;
- `R` — reset the vehicle.

## Direction

The next milestones are:

1. replace the placeholder rigidbody handling with raycast suspension / proper tire model;
2. add a real modular road test district and optimized environment art pipeline;
3. add garage, car data/configs and upgrade architecture;
4. create drift scoring, delivery and race activities;
5. profile the Web build early on desktop and mobile browsers;
6. add Yandex Games SDK integration;
7. add Colyseus multiplayer only after the local driving loop is stable.

The project is intentionally being designed for Web from day one: streamed content, aggressive LODs, lightweight shaders, limited runtime allocations and server-authoritative economy are part of the planned architecture.
