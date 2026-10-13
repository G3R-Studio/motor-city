#!/usr/bin/env python3
"""Regression contract for Sprint waypoint chevrons in world and minimap.

The SprintMarkerVfx/Tabler arrow-right sprite points toward local +X. Both
presentations must rotate that sprite using the SAME outgoing segment from
StreetSprintActivity.TryGetNextTarget(), not a fixed right-facing sprite.

Source contract + geometric examples, not a substitute for Unity visual smoke.
"""
from __future__ import annotations

import math
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WORLD = ROOT / "Assets/Scripts/World/StreetSprintMarkerVisual.cs"
MAP = ROOT / "Assets/Scripts/UI/NavigatorView.cs"
ACTIVITY = ROOT / "Assets/Scripts/Gameplay/StreetSprintActivity.cs"
BUILDER = ROOT / "Assets/Editor/MotorCityActivityMarkerBuilder.cs"


def check(condition: bool, message: str, errors: list[str]) -> None:
    if not condition:
        errors.append(message)


def angle_minimap(dx: float, dz: float, yaw_degrees: float) -> float:
    # A car heading +Z at yaw=0 has a minimap whose +Y means straight.
    yaw = math.radians(yaw_degrees)
    local_x = math.cos(yaw) * dx - math.sin(yaw) * dz
    local_z = math.sin(yaw) * dx + math.cos(yaw) * dz
    return math.degrees(math.atan2(local_z, local_x))


def angle_world(dx: float, dz: float, camera_right: tuple[float, float], camera_up: tuple[float, float]) -> float:
    projected_x = dx * camera_right[0] + dz * camera_right[1]
    projected_y = dx * camera_up[0] + dz * camera_up[1]
    return math.degrees(math.atan2(projected_y, projected_x))


def angular_error(actual: float, expected: float) -> float:
    return abs((actual - expected + 180) % 360 - 180)


def main() -> int:
    errors: list[str] = []
    world = WORLD.read_text(encoding="utf-8-sig")
    nav = MAP.read_text(encoding="utf-8-sig")
    activity = ACTIVITY.read_text(encoding="utf-8-sig")
    builder = BUILDER.read_text(encoding="utf-8-sig")
    check('"arrow-right.png"' in builder, "Sprint marker must use the right-facing Tabler waypoint arrow", errors)
    check(
        "public bool TryGetNextTarget(" in activity
        and "checkpointIndex + 1" in activity,
        "Sprint outgoing waypoint contract missing",
        errors,
    )

    compact_world = re.sub(r"\s+", "", world)
    compact_nav = re.sub(r"\s+", "", nav)

    for token in (
        "sprint.TryGetNextTarget(out nextTarget)",
        "nextTarget - sprint.CurrentTarget",
        "Camera.main",
        "cameraTransform.right",
        "cameraTransform.up",
        "Mathf.Atan2(",
        "Mathf.Rad2Deg",
        "Quaternion.Euler(0f, 0f, angle)",
        'renderer.name == "Sprint Icon Core"',
        'renderer.name == "Sprint Icon Glow"',
        "chevron.enabled = hasDirection",
    ):
        check(re.sub(r"\s+", "", token) in compact_world, f"World Sprint chevron contract missing: {token}", errors)

    for token in (
        'markerId == "sprint"',
        "streetSprint.TryGetNextTarget(",
        "nextCheckpoint -",
        "streetSprint.CurrentTarget",
        "0f,\n                                -yaw,",
        "Mathf.Atan2(",
        "localOutgoing.z,",
        "localOutgoing.x)",
        "Quaternion.Euler(0f, 0f, angle)",
        "minimapTargetIcon.rectTransform.localRotation",
        "minimapTargetIcon.sprite =\n                            minimapMaskSprite",
    ):
        check(re.sub(r"\s+", "", token) in compact_nav, f"Minimap Sprint chevron contract missing: {token}", errors)

    # Sanity checks for rotation of a +X arrow to the expected player-relative
    # up/left/right direction. These examples include a 90-degree car turn
    # to detect sign inversions between world yaw and minimap yaw.
    scenarios = (
        (0, 10, 0, 90, "north ahead / straight"),
        (10, 0, 90, 90, "east ahead / straight after turn"),
        (10, 0, 0, 0, "east / right"),
        (-10, 0, 0, 180, "west / left"),
        (0, 10, 90, 180, "north / left when facing east"),
        (0, -10, 90, 0, "south / right when facing east"),
        (-10, 0, 270, 90, "west ahead / straight when facing west"),
    )
    for dx, dz, yaw, expected, label in scenarios:
        angle = angle_minimap(dx, dz, yaw)
        check(
            angular_error(angle, expected) < 0.0001,
            f"Minimap angle mismatch ({label}): expected {expected}, got {angle}",
            errors,
        )

    # The same route vector projected to camera basis: world +Z should show
    # upward for a camera looking north, east should show right.
    check(
        angular_error(angle_world(0, 10, (1, 0), (0, 1)), 90) < 0.0001,
        "World +Z arrow must point up with north-up camera",
        errors,
    )
    check(
        angular_error(angle_world(10, 0, (1, 0), (0, 1)), 0) < 0.0001,
        "World +X arrow must point right with north-up camera",
        errors,
    )
    check(
        angular_error(angle_world(-10, 0, (1, 0), (0, 1)), 180) < 0.0001,
        "World -X arrow must point left with north-up camera",
        errors,
    )

    if errors:
        print("Sprint waypoint direction regression FAILED:")
        for error in errors:
            print(" -", error)
        return 1
    print(
        "Sprint waypoint direction source/math contract PASS: "
        "world billboards + minimap use outgoing checkpoint route, "
        "7 car headings / 3 world projections checked."
    )
    print("Unity Editor visual and browser WebGL smoke still required.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
