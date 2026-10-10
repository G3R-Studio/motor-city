#!/usr/bin/env python3
"""Phase 10 source-only guards for the vehicle physics/input/collision pipeline.

These checks protect integration boundaries. They do not simulate Unity
WheelCollider physics or certify handling/road collision behavior.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]


def source(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


def compact(value):
    return re.sub(r"\\s+", "", value)


def method(code, name, owner):
    pattern = r"\\b(?:private|public|internal)\\s+(?:static\\s+)?(?:void|bool)\\s+" + re.escape(name) + r"\\s*\\("
    found = re.search(pattern, code)
    if not found:
        errors.append(f"{owner}: missing method {name}")
        return ""
    start = code.find("{", found.end())
    if start < 0:
        errors.append(f"{owner}: no method body for {name}")
        return ""
    depth = 0
    for index in range(start, len(code)):
        if code[index] == "{":
            depth += 1
        elif code[index] == "}":
            depth -= 1
            if depth == 0:
                return compact(code[start:index + 1])
    errors.append(f"{owner}: unclosed method {name}")
    return ""


def require(body, token, message):
    if compact(token) not in body:
        errors.append(message)


errors = []
car = source("Assets/Scripts/Vehicle/ArcadeCarController.cs")
handbrake = source("Assets/Scripts/Vehicle/HandbrakePhysicsAssist.cs")
installer = source("Assets/Scripts/Vehicle/ArcadeRacingCarRuntimeInstaller.cs")
input_source = source("Assets/Scripts/Input/MotorCityInput.cs")
input_host = source("Assets/Scripts/Input/MotorCityVirtualInputRuntime.cs")
prometeo = source("Assets/PROMETEO - Car Controller/Scripts/PrometeoCarController.cs")

# Order of source sampling and physics helpers matters: the latter runs
# after the main car FixedUpdate; no controller parameter is modified here.
require(compact(car), "[DefaultExecutionOrder(-100)]", "Car controller execution order changed")
require(compact(handbrake), "[DefaultExecutionOrder(500)]", "Handbrake assist execution order changed")
require(method(car, "FixedUpdate", "car"), "UpdateTelemetry();ApplyPowerAssist();", "Physics tick lost telemetry/power assist")
require(method(handbrake, "FixedUpdate", "handbrake"), "car.ApplyPhysicalHandbrake(", "Physics tick lost rear handbrake")
require(method(handbrake, "FixedUpdate", "handbrake"), "car.ApplyStraightLineStability();", "Physics tick lost straight-line stability")
require(method(car, "Update", "car"), "UpdatePrometeoInputProxies();", "Frame tick lost input proxy sampling")
require(method(car, "UpdateTelemetry", "car"), "GetGroundHit(", "Grounded-wheel telemetry no longer uses WheelCollider hits")
require(method(car, "ApplyPowerAssist", "car"), "ForceMode.Acceleration", "Power assist no longer uses physical acceleration")
require(method(car, "ApplyStraightLineStability", "car"), "MotorCityInput.SteeringAxis", "Stability no longer reads live steering in the physics tick")

brakes = method(car, "ApplyPhysicalHandbrake", "car")
for token, reason in [
    ("if(!handbrakeHeld)", "Handbrake-release path missing"),
    ("wheel.brakeTorque=0f;", "Rear brake torque release missing"),
    ("wheel.motorTorque=0f;", "Handbrake rear drive torque cancellation missing"),
]:
    require(brakes, token, reason)

require(method(car, "ConfigureWheelCollider", "car"), "AddComponent<WheelCollider>()", "WheelCollider rig construction missing")
require(method(car, "ApplyWheelFriction", "car"), "wheel.sidewaysFriction=sideways;", "Mode-specific lateral wheel grip missing")
require(method(car, "ApplyWheelFriction", "car"), "wheel.forwardFriction=forward;", "Mode-specific longitudinal wheel grip missing")
require(compact(car), "CollisionDetectionMode.ContinuousDynamic", "Player Rigidbody continuous collision mode changed")

# Imported FBX/FCG physics is disabled before delayed Destroy, so the
# player root owns the active Rigidbody and the generated body proxies.
clean = method(installer, "StripImportedPhysics", "installer")
for kind in ("WheelCollider", "Rigidbody", "Collider"):
    require(clean, f"GetComponentsInChildren<{kind}>(true)", f"Imported {kind} cleanup missing")
require(clean, "collider.enabled=false;", "Imported colliders must be disabled before deferred Destroy")
require(clean, "rigidbody.detectCollisions=false;", "Imported Rigidbody contacts must be disabled")
require(method(installer, "ConfigureVisualMeshCollider", "installer"), "BuildCompoundBodyCollider(", "Compound visual body collision construction missing")
require(method(installer, "BuildCompoundBodyCollider", "installer"), "TryBuildVehicleCollisionProfile(", "Authored vehicle collision profile path missing")
require(method(installer, "BuildCompoundBodyCollider", "installer"), "BuildBoundsOnlyCompoundBodyCollider(", "Unreadable/empty body mesh fallback missing")
require(method(installer, "EnsureFallbackChassisCollider", "installer"), "AddComponent<BoxCollider>()", "Fallback chassis contact collider missing")

# Held touch controls persist across frame boundaries; single-press
# actions are latched for Update and cleared in LateUpdate.
require(compact(input_host), "[DefaultExecutionOrder(-32000)]", "Virtual input host early execution order changed")
require(method(input_host, "Update", "input"), "MotorCityInput.BeginVirtualInputFrame();", "Virtual press frame begin missing")
require(method(input_host, "LateUpdate", "input"), "MotorCityInput.EndVirtualInputFrame();", "Virtual press frame end missing")
require(method(input_source, "BeginVirtualInputFrame", "input"), "VirtualPressedThisFrame[i]=VirtualPendingPress[i];", "Virtual pending press consumption missing")
require(method(input_source, "SetVirtualSteering", "input"), "virtualSteering=Mathf.Clamp(", "Virtual steering input clamp missing")
require(compact(prometeo), "ApplySteeringInput(MotorCityInput.SteeringAxis);", "Prometeo analog steering bridge missing")

print("Phase 10 source-level vehicle physics contracts: " + ("PASS" if not errors else "FAIL"))
if errors:
    for error in errors:
        print("ERROR:", error)
    raise SystemExit(1)
print("Controller, handbrake, wheel friction, body collision and touch input bridges are present.")
print("Not a WheelCollider simulation, handling benchmark or Play Mode collision smoke.")
