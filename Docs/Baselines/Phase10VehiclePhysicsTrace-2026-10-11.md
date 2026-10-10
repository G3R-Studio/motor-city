# Phase 10 — vehicle physics, collision and mobile input trace (2026-10-11)

**Scope:** read-only source inspection of `ArcadeCarController`, `HandbrakePhysicsAssist`, `ArcadeRacingCarRuntimeInstaller`, `MotorCityInput`, `MotorCityVirtualInputRuntime`, and Prometeo's car controller. A new `Tools/check_phase10_vehicle_physics.py` checks key call-path contracts in CI. **No driving parameters, wheel sizes, physics materials, collider placement or vehicle prefab assets were modified.**

## Runtime chain and update ownership

| System | Timing / responsibility | Observed contract |
| --- | --- | --- |
| `MotorCityVirtualInputRuntime` | `Update` (-32000), `LateUpdate` | Promotes queued single-frame touch presses at frame start and clears them at frame end. Held buttons and analog steering remain separately stored in `MotorCityInput`. |
| `ArcadeCarController` | `Update` (-100) | Reads touch/keyboard/gamepad state into Prometeo touch-input proxies. Updates mode-specific `WheelCollider` friction periodically (~0.12 s); manages display/reset locks. |
| `PrometeoCarController` | `Update` (default order) | Uses touch input proxies for throttle/reverse/handbrake and MotorCity analog steering for wheel scheme. Applies its wheel drive, steering, traction and deceleration routines. This is a **render-frame-owned wheel-control path**, not proof of fixed-step determinism. |
| `ArcadeCarController` | `FixedUpdate` (-100) | Reset hold calls `HoldVehicleStill`; otherwise samples `WheelCollider.GetGroundHit` telemetry then applies additional `Rigidbody.AddForce(..., ForceMode.Acceleration)` when conditions permit. |
| `HandbrakePhysicsAssist` | `FixedUpdate` (+500) | Applies rear brake/torque cancellation, releases rear brakes on coasting after handbrake, then optional straight-line lateral/yaw stability; avoids drift mode correction. |

`DefaultExecutionOrder` establishes relative script invocation order within the same event phase, not an absolute order between `Update`, Unity physics ticks, and UI events. Do not claim zero touch latency from source inspection alone.

## Wheel, drive mode and collision ownership

- The player car owns one Rigidbody using `ContinuousDynamic` collision detection. `ConfigurePrometeoRig` creates/configures four `WheelCollider` objects in FL/FR/RL/RR order. Radius, spring, damper, mass, friction and wheel center derive from measured mesh/vehicle tuning.
- `Comfort`, `Sport` and `Drift` tune forward and sideways friction, Prometeo steering/acceleration and handbrake behavior. The drive-mode selection is persisted and player-triggered mode switching is gated by speed and held driving controls.
- The handbrake assistant locks/reduces rear drive torque and applies additional rear `brakeTorque`; on release while coasting, it clears rear-wheel `brakeTorque`, avoiding an indefinitely stuck rear brake. The reset/garage/presentation driving locks have separate code paths.
- `ArcadeRacingCarRuntimeInstaller` disables imported visual `WheelCollider`, `Rigidbody` and `Collider` components before deferred `Destroy`. It chooses the primary non-wheel body mesh, installs a `MotorCityBodyCollisionProxy` containing compound `BoxCollider` shapes from vehicle-specific profiles, or a bounds/mesh slicing fallback. The chassis has a fallback `BoxCollider` when required.
- The existing Phase 3 gate identifies nine authored player prefabs (Beatall, Peugeot306, ToyotaAE86, Hybrid, Porsche996, AmgGT, Camaro, Delorean, Bus) with four named pivots; `Street` is a fallback ID, not a tenth authored player prefab. The compound profile resolver also handles `Street`.
- City road/building collider geometry is authored separately in `CityVisual.prefab`. This audit does not modify the city or establish that every road junction/vehicle can collide correctly at speed.

## Risks requiring measured evidence before changes

1. **Frame pacing:** Prometeo writes wheel controls in `Update`, whereas Rigidbody power assist, handbrake and stabilization run in `FixedUpdate`. Compare real steering response, braking distances and slip under different frame rates before moving Prometeo logic or retuning forces.
2. **Friction ownership:** Prometeo traction recovery and Motor City’s ~0.12-second friction refresh both affect wheel grip. Source-level ordering is visible; the effective `WheelFrictionCurve` over a full drift/handbrake/release sequence must be sampled in Play Mode.
3. **Collision geometry:** Vehicle-specific compound boxes are proportions of the chosen mesh bounds. A passing source contract does not establish that bumper, roof, undercarriage or Bus body fit the visible mesh on all nine cars.
4. **Mobile controls:** A held touch input is stored independently from a per-frame press. Test rapid left/right reversal, analog wheel release, mixed pedals and pause/resume at high and low FPS.

## Acceptance gates remaining

- Unity Play Mode: for each of the nine vehicles, drive forward/reverse, brake, handbrake and release, test all three modes, collision with a wall/prop and a curb at controlled low speeds, and verify no fall-through, stuck wheel brake, explosive impulse or missing body collider. Observe the Bus tandem visual axle separately.
- Capture telemetry per vehicle/mode: FPS/frame timing, `SpeedKph`, `GroundedWheels`, front steer angles, rear-wheel brake torque, rear slip, effective forward/sideways `WheelFrictionCurve`, and collision contacts. Define comparable repeatable scenarios before tuning.
- Touch: arrows and wheel schemes, tap/drag/release, repositionable controls, pause/continue and steering neutral return; compare Desktop Editor Device Simulator and real mobile WebGL.
- Browser and release-specific regression belongs to Phase 12. Unity Editor batch compilation, source gates and prefab inventories **do not** replace these road-driving tests.

**Conclusion:** Phase 10 source-call-path inventory recorded and protected by a read-only CI gate. Physics tuning and real collision QA remain open. No gameplay physics changed.
