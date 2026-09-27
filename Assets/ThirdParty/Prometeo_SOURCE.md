# PROMETEO: Car Controller

Motor City uses **PROMETEO: Car Controller** by Mena as its underlying vehicle controller.

- Unity Asset Store package ID: 209444
- Package name: PROMETEO: Car Controller
- Imported package source used by the project is currently stored under `Assets/PROMETEO - Car Controller`.
- Motor City's gameplay code talks to `PrometeoCarController` and `PrometeoTouchInput` through the `ArcadeCarController` bridge.
- Motor City builds its own wheel rig, HUD, upgrades, drift scoring, effects and input proxy layer around the third-party controller.

The original package demo scenes/documentation are not required by the game and are intentionally excluded from the cleaned project.
