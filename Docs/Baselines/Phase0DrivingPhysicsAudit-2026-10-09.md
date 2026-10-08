# Phase 0 — driving physics source audit (2026-10-09)

Scope: `Assets/Scripts/Vehicle/ArcadeCarController.cs` and `HandbrakePhysicsAssist.cs`. This is a read-only code-path review, not a reproducible physics simulation or numerical handling benchmark.

## Findings

- **Physics tick:** `ArcadeCarController.FixedUpdate` runs `UpdateTelemetry` and `ApplyPowerAssist` unless the post-teleport/reset hold is active. That branch invokes `HoldVehicleStill`, clearing velocities and applying wheel braking. The separate `HandbrakePhysicsAssist.FixedUpdate` applies rear-wheel handbraking and straight-line stability; the component has `DefaultExecutionOrder(500)`.
- **Acceleration:** `ApplyPowerAssist` adds Rigidbody acceleration with `ForceMode.Acceleration`, gated by vehicle enablement, grounded-wheel count, throttle, speed limits, and drive mode. Prometeo's wheel-torque system remains a separate contributor. Combined force curves were **not** calibrated by measurement.
- **Steering/stability:** front wheel angles return toward neutral only when player steering is below a dead zone and handbrake is inactive. `ApplyStraightLineStability` skips Drift mode, excessive slip and airborne states; it applies lateral correction and yaw torque in Sport/Comfort when conditions allow. Reads live `MotorCityInput.SteeringAxis` in the physics tick to avoid a stale input cache.
- **Friction and drive modes:** `ApplyDriveModeTuning` calls Prometeo tuning and `ApplyWheelFriction`; wheel `forwardFriction` and `sidewaysFriction` are assigned by drive mode. Drift limits progression-grip influence to preserve rear-wheel slide. No runtime wheel-friction curves were captured.
- **Braking/handbrake:** `ApplyPhysicalHandbrake` clamps wheel drive torque to zero and increases rear-wheel brake torque when held, with reduced torque in Drift. At low speed, with throttle/reverse released, it zeroes Rigidbody velocities. On release while coasting it explicitly clears rear brake torque, avoiding sticky brakes. The reset/garage/teleport paths manage driving locks, wheel torque and velocities separately.
- **Suspension/wheels:** four `WheelCollider` references are created/configured when necessary; suspension limits are clamped, and telemetry derives grounded wheel count and rear slip from `GetGroundHit`. Per-vehicle profile changes clamp mass, grip, steering, brakes and drift multipliers before retuning.

## Audit conclusion and limits

No unguarded direct teleport/reset motion loop or obvious missing handbrake-release path was identified in the reviewed code paths. The reviewed systems deliberately separate `Update` input sampling from `FixedUpdate` physics actions. This closes **Phase 0 source-level physics inventory**; it does not prove handling performance under every frame rate, road surface, car profile, WebGL frame pacing or collision circumstance. Quantitative vehicle dynamics, reproducible lap/force tests and collision stress tests belong to Phase 10. Player-confirmed gameplay and mobile controls are separate smoke evidence.

No gameplay physics code was changed in this audit.
