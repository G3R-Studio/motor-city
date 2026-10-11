#!/usr/bin/env python3
"""Phase 12 regression guard: replay must release result and modal drive locks.

Static contract check only; Unity Play Mode and browser smoke are still needed.
"""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
GAMEPLAY = ROOT / "Assets/Scripts/Gameplay"
UI = ROOT / "Assets/Scripts/UI/ActivityResultView.cs"
CAR = ROOT / "Assets/Scripts/Vehicle/ArcadeCarController.cs"
ERRORS: list[str] = []

REPLAY_OWNERS = {
    "delivery": "DeliveryActivity.cs",
    "drift": "DriftChallenge.cs",
    "sprint": "StreetSprintActivity.cs",
    "circuit": "CircuitRaceActivity.cs",
    "profession_carwash": "CarWashJobSystem.cs",
    "profession_tow": "TowTruckJobSystem.cs",
    "profession_pizza": "CityProfessionSystem.cs",
    "profession_taxi": "CityProfessionSystem.cs",
    "profession_mail": "CityProfessionSystem.cs",
    "profession_icecream": "CityProfessionSystem.cs",
}
RELEASE_RESULT = re.compile(
    r'SetDrivingBlocked\s*\(\s*"ActivityResult"\s*,\s*false\s*\)'
)
RELEASE_COUNTDOWN = re.compile(
    r'SetDrivingBlocked\s*\(\s*"ActivityCountdown"\s*,\s*false\s*\)'
)


def method_text(source: str, name: str) -> str:
    """Extract a C# method by braces; includes nested if/switch blocks."""
    match = re.search(
        rf"\b(?:public|private)\s+(?:void|bool)\s+{re.escape(name)}\s*\([^)]*\)\s*\{{",
        source,
    )
    if not match:
        return ""
    start = match.end() - 1
    depth = 0
    for i in range(start, len(source)):
        if source[i] == "{":
            depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0:
                return source[start:i + 1]
    return ""


def main() -> int:
    ui = UI.read_text(encoding="utf-8-sig")
    handle = method_text(ui, "HandleActivityResultInput")
    if not handle:
        ERRORS.append("ActivityResultView: replay input handler not found")
    else:
        # Keep the result locked while restart validation fails, but release
        # both causes of an immobilized vehicle once an activity dismisses it.
        switch_at = handle.find("switch (activityManager.ResultActivityId)")
        post_success = handle.find("if (!activityManager.HasResult)", switch_at)
        if switch_at < 0 or post_success <= switch_at:
            ERRORS.append("UI: successful replay must be checked after switch dispatch")
        else:
            cleanup = handle[post_success:]
            if not RELEASE_RESULT.search(cleanup):
                ERRORS.append("UI: replay must release ActivityResult after dismiss")
            if "RefreshDrivingEnabledForUi();" not in cleanup:
                ERRORS.append("UI: replay must recompute the ModalUi driving lock")
        for activity_id in REPLAY_OWNERS:
            if f'case "{activity_id}":' not in handle:
                ERRORS.append(f"UI: retry action missing activity ID {activity_id}")

    owners = set(REPLAY_OWNERS.values())
    for file_name in sorted(owners):
        path = GAMEPLAY / file_name
        if not path.is_file():
            ERRORS.append(f"Missing activity file: {file_name}")
            continue
        source = path.read_text(encoding="utf-8-sig")
        replay = method_text(source, "RestartFromResult")
        if not replay:
            ERRORS.append(f"{file_name}: RestartFromResult missing")
            continue
        dismiss = replay.find("DismissResult(false)")
        cleared = RELEASE_RESULT.search(replay)
        if dismiss < 0 or not cleared or cleared.start() <= dismiss:
            ERRORS.append(
                f"{file_name}: ActivityResult must unlock after result dismissal"
            )

    for file_name in (
        "StreetSprintActivity.cs",
        "CircuitRaceActivity.cs",
        "DriftChallenge.cs",
        "DeliveryActivity.cs",
        "UndergroundSceneSystem.cs",
    ):
        source = (GAMEPLAY / file_name).read_text(encoding="utf-8-sig")
        method_name = (
            "BeginPhysicalRunGameplay"
            if file_name == "UndergroundSceneSystem.cs"
            else "BeginGameplay"
        )
        if not RELEASE_COUNTDOWN.search(method_text(source, method_name)):
            ERRORS.append(f"{file_name}: countdown lock not released at gameplay start")

    car = CAR.read_text(encoding="utf-8-sig")
    lock = method_text(car, "SetDrivingBlocked")
    if "drivingBlockers.Add(" not in lock or "drivingBlockers.Remove(" not in lock:
        ERRORS.append("ArcadeCarController: reason-based driving lock contract changed")
    if "drivingBlockers.Count == 0" not in lock:
        ERRORS.append("ArcadeCarController: cannot verify unlock when all blockers cleared")

    if ERRORS:
        print("Phase 12 replay-drive contract FAILED:")
        for message in ERRORS:
            print(" -", message)
        return 1
    print(
        "Phase 12 replay-drive source contracts PASS: "
        f"{len(REPLAY_OWNERS)} result IDs / {len(owners)} owners, "
        "5 countdown handoffs, shared result + ModalUi cleanup."
    )
    print("Actual replay driving behavior still requires Unity Play Mode / WebGL smoke.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
