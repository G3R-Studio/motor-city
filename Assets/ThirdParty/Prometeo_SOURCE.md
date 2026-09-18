# PROMETEO: Car Controller

Motor City uses the **PROMETEO: Car Controller** physics package by Mena as an external Unity Asset Store dependency.

- Unity Asset Store package ID: 209444
- Package name: PROMETEO: Car Controller
- License: Standard Unity Asset Store EULA (Extension Asset)
- The Prometeo package itself is **not redistributed** in this repository.

Motor City connects to the imported `PrometeoCarController` at runtime through `ArcadeCarController`. The bridge assigns the Motor City car wheel meshes and generated WheelColliders, maps garage upgrade values to Prometeo tuning fields, and exposes vehicle/drift telemetry to the rest of the game.

The game's existing URP car visual, HUD, activities, drift scoring, smoke and tire-mark effects remain Motor City systems; Prometeo is used as the vehicle movement/steering/braking/traction controller.
