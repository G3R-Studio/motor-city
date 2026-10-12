#!/usr/bin/env python3
"""Gameplay Polish GP-1..GP-3 regression contract.

Source + small deterministic math guards. No Unity runtime dependency.
This does NOT replace Unity Editor compilation, traffic simulation or WebGL QA.
"""
from __future__ import annotations

from pathlib import Path
import math
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
ERRORS: list[str] = []


def source(path: str) -> str:
    file = ROOT / path
    if not file.is_file():
        ERRORS.append(f"Missing source file: {path}")
        return ""
    return file.read_text(encoding="utf-8-sig")


def compact(text: str) -> str:
    return re.sub(r"\s+", "", text)


def require(condition: bool, message: str) -> None:
    if not condition:
        ERRORS.append(message)


def route_distances(lengths: tuple[float, ...], spacing: float) -> list[float]:
    """Mirror NavigatorView segment carry logic on an unbent route."""
    remaining = spacing * 0.6
    travelled = 0.0
    dots = []
    for length in lengths:
        distance = remaining
        while distance <= length:
            dots.append(travelled + distance)
            distance += spacing
        remaining -= length
        while remaining <= 0:
            remaining += spacing
        travelled += length
    return dots


def stopping_probe(kph: float) -> float:
    metres_per_second = kph / 3.6
    if kph < 3:
        return 9.0
    return max(10.0, min(60.0,
        8 + metres_per_second * 1.5
        + metres_per_second ** 2 / (2 * 6)))


def main() -> int:
    challenge = compact(source("Assets/Scripts/Gameplay/DriftChallenge.cs"))
    spot = compact(source("Assets/Scripts/Gameplay/DriftSpotSystem.cs"))
    tracker = compact(source("Assets/Scripts/Gameplay/DriftTracker.cs"))
    map_source = compact(source("Assets/Scripts/UI/NavigatorView.cs"))
    schematic = compact(source("Assets/Scripts/World/CitySchematicMap.cs"))
    car = compact(source("Assets/Scripts/Vehicle/ArcadeCarController.cs"))
    traffic = compact(source("Assets/Fantastic City Generator/Scripts/TrafficCar.cs"))
    speed = compact(source("Assets/Scripts/World/TrafficSpeedAdapter.cs"))
    bootstrap = compact(source("Assets/Scripts/Bootstrap/MotorCityBootstrap.cs"))
    audio = compact(source("Assets/Scripts/Audio/MotorCitySfxRuntime.cs"))
    touch = compact(source("Assets/Scripts/UI/TouchControlsView.cs"))
    store = compact(source("Assets/Scripts/UI/TouchControlLayoutStore.cs"))
    drag = compact(source("Assets/Scripts/UI/TouchControlLayoutDragHandle.cs"))

    require("activityHalfExtent" not in challenge
            and "outsideGraceSeconds" not in challenge,
            "Drift challenge must not fail outside the start zone")
    require("FinishByScore();" in challenge
            and "TimeRemaining=Mathf.Max(0f,TimeRemaining-Time.deltaTime)" in challenge,
            "Drift challenge must remain timed and score-based")
    require("!drift.ComboInProgress" in spot
            and "FinishSpot(activeSpot,liveScore)" in spot,
            "Free-roam drift spot must finish on banked combo, not distance")
    require("publicboolComboInProgress=>comboInProgress;" in tracker,
            "DriftTracker must expose combo state")
    require("RearmRadius*RearmRadius" in spot and "spot.Armed=false;" in spot,
            "Drift spot start trigger must not award repeated medals in place")

    require("constfloatworldRadius=500f;" in map_source,
            "Minimap radius drifted from the GP-1 500m value")
    require("constfloatDotSpacingPixels=13f;" in map_source,
            "Minimap route dots must remain visually separated")
    require("distanceToNextDot-=length;" in map_source,
            "Minimap dots must keep spacing across waypoint joins")
    require("routeColor.a=0.96f;" in map_source
            and "minimapRouteOpacity" not in map_source
            and "routeColor.a=Mathf.MoveTowards" not in map_source,
            "Route dots should appear immediately, without fade")
    require("if(!targetChanged&&!offRoute)return;" in map_source,
            "Reroutes must update without waiting for a fade animation")
    require("returnnewRect(center.x-width*0.5f," in schematic,
            "Road UV must stay aligned with markers at map edges")
    require("if(streetSprint!=null&&" in map_source
            and 'return"sprint";' in map_source,
            "Active sprint marker must not be guessed from nearby activities")

    sample = route_distances((7.0, 11.0, 33.0, 19.0), 12.0)
    require(len(sample) >= 5 and all(
            math.isclose(b - a, 12.0, abs_tol=1e-6)
            for a, b in zip(sample, sample[1:])),
            "Route dot distances double up at segment boundaries")

    for item in ("DriveMode.Sport=>27,", "DriveMode.Drift=>62,", "_=>34"):
        require(item in car, f"Global drive-mode steering tune missing: {item}")

    require("CruiseSpeedKph=60f" in speed
            and "speedAdapter.Bind();" in bootstrap
            and "foreach(TransformchildincachedContainer)" in speed,
            "Newly spawned and existing traffic must use the common 60 km/h tune")
    require("Physics.RaycastNonAlloc(" in traffic
            and "speedMetresPerSecond*speedMetresPerSecond/(2f*6f)" in traffic,
            "Traffic braking probes must account for stopping distance")
    require("motorTorque=brake>0f?0f:" in traffic,
            "Traffic motors must stop applying torque while braking")
    require("candidate.collider.GetComponentInParent<TrafficCar>()" in traffic,
            "Traffic obstacle detection must handle nested car colliders")
    require(stopping_probe(60) > stopping_probe(30) >= 20,
            "Traffic stopping probe does not scale with speed")

    require("BuildUiClick()" in audio
            and "instance.Play(instance.uiClick,0.12f,1f)" in audio
            and "softenedNoise" not in audio,
            "Accepted soft UI tap or its volume has changed")
    require("lastUiClickTime<0.06f" in audio,
            "UI click double-play suppression is missing")
    for key in ("SteerLeft", "SteerRight", "Throttle",
                "Brake", "Handbrake", "Action"):
        require(f'"{key}"' in touch, f"Resizable mobile control missing: {key}")
    require("LoadScale(" in store and "SaveScale(" in store
            and 'DeleteKey(key+".scale")' in store,
            "Scaled touch controls must be persisted and reset correctly")
    require("ClampToBounds()" in touch and "publicvoidClampToBounds()" in drag,
            "Scaled mobile buttons must be constrained inside screen bounds")

    if ERRORS:
        print("Gameplay Polish regression FAILED:")
        for error in ERRORS:
            print(" -", error)
        return 1
    print("Gameplay Polish regression source/math guards PASS.")
    print("Unity Editor compilation, live NPC braking and touch WebGL QA still required.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
