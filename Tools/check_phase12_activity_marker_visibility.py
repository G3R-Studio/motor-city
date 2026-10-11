#!/usr/bin/env python3
"""Activity-only marker presentation regression guard (world and minimap).

Static source/layout contract. Actual Unity and GitHub Pages browser behavior
must still be confirmed after building both Release profiles.
"""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORLD = ROOT / "Assets/Scripts/World"
GAMEPLAY = ROOT / "Assets/Scripts/Gameplay"
BOOTSTRAP = ROOT / "Assets/Scripts/Bootstrap/MotorCityBootstrap.cs"
NAV = ROOT / "Assets/Scripts/UI/NavigatorView.cs"
ERRORS: list[str] = []


def check(value: bool, message: str) -> None:
    if not value:
        ERRORS.append(message)


def source(path: Path) -> str:
    if not path.is_file():
        ERRORS.append(f"Missing source: {path}")
        return ""
    return path.read_text(encoding="utf-8-sig")


def compact(s: str) -> str:
    # Not a parser: guard stable literal code contracts without depending on
    # Unity/C# multiline formatting.
    return re.sub(r"\s+", "", s)


def main() -> int:
    bootstrap = compact(source(BOOTSTRAP))
    nav = compact(source(NAV))
    static = compact(source(WORLD / "StaticActivityMarkerVisual.cs"))
    discovery = compact(source(WORLD / "DiscoveryMarkerVisual.cs"))
    underground = compact(source(WORLD / "UndergroundMarkerVisual.cs"))
    professions = compact(source(GAMEPLAY / "CityProfessionSystem.cs"))
    activity = compact(source(GAMEPLAY / "ActivityManager.cs"))

    check("publicboolIsBusy=>!string.IsNullOrEmpty(ActiveId)" in activity,
          "ActivityManager busy state changed; review marker filtering")
    check("publicboolIsActive(stringid)=>ActiveId==id" in activity,
          "ActivityManager owner check changed")

    # Ordinary race/activity markers already know their ActivityManager owner.
    race = {
        "StreetSprintMarkerVisual.cs": "sprint",
        "CircuitRaceMarkerVisual.cs": "circuit",
        "DriftChallengeMarkerVisual.cs": "drift",
    }
    for name, activity_id in race.items():
        code = compact(source(WORLD / name))
        check(f'activityManager.IsActive("{activity_id}")' in code,
              f"{name}: should display only during its own activity")
        check("!activityManager.IsBusy" in code,
              f"{name}: free roam should restore marker")
    delivery = compact(source(GAMEPLAY / "RouteMarkerVisual.cs"))
    check('activityManager.IsActive("delivery")' in delivery and
          "!activityManager.IsBusy" in delivery,
          "Delivery marker must show only during own activity")

    for token in (
        "ActivityManageractivityManager",
        "stringmarkerActivityId",
        "privatevoidUpdate(){RefreshVisibility();}",
        "activityManager.IsActive(markerActivityId)",
        "!activityManager.IsBusy",
        "SetVisible(visible)",
    ):
        check(token in static, f"Static job markers missing: {token}")

    for token in (
        "ActivityManageractivityManager",
        "activityManager.IsBusy",
        "!discoveries.IsFound(",
    ):
        check(token in discovery, f"Discoveries must hide during jobs: {token}")

    for token in (
        "ActivityManageractivityManager",
        "!activityManager.IsBusy",
        'activityManager.IsActive("underground")',
        "underground.HasActiveInvitation",
    ):
        check(token in underground, f"Underground invitation visibility missing: {token}")

    for token in (
        "publicstringGetStartActivityId(intindex)",
        'ActivityId(definitions[index].Id)',
    ):
        check(token in professions, f"Per-profession activity owner ID missing: {token}")

    for token in (
        "CreateCarWashMarker(carWash,activityManager)",
        "CreateTowTruckMarker(towTruck,activityManager)",
        "CreateProfessionMarkers(professions,activityManager)",
        "CreateDiscoveryMarkers(discoveries,activityManager)",
        "CreateUndergroundMarker(underground,activityManager)",
        '"profession_carwash"',
        '"profession_tow"',
        "professions.GetStartActivityId(i)",
        "visual.Bind(discoveries,i,activityManager)",
        "visual.Bind(underground,activityManager)",
    ):
        check(token in bootstrap, f"Bootstrap activity marker binding missing: {token}")

    # Dynamic checkpoints for active work must NOT be hidden by this change.
    active_checkpoint = compact(source(WORLD / "DynamicActivityCheckpointVisual.cs"))
    for token in ("profession.IsActive", "towTruck.IsActive",
                  "underground.IsActive", "underground.IsCountingDown"):
        check(token in active_checkpoint,
              f"Current dynamic checkpoint must remain visible: {token}")

    # The only minimap target blip must point to the active activity. A waiting
    # underground invitation must never override delivery/sprint/race/tow/etc.
    conditional = ('activityManager==null||!activityManager.IsBusy||'
                   'activityManager.IsActive("underground")')
    check(nav.count(conditional) >= 2,
          "Both underground icon and target must be gated by busy owner")
    check("if((activityManager==null||!activityManager.IsBusy)&&manualNavigationActive" in nav,
          "Manual destination must not override the active marker icon")
    check("if(activityManager!=null&&activityManager.IsBusy)"
          "{target=car.transform.position;label=string.Empty;"
          "hasTarget=false;showRoadRoute=false;return;}" in nav,
          "Unknown busy activity must not fall back to unrelated POI")

    for code in (
        "delivery.IsActive||delivery.IsCountingDown",
        "streetSprint.IsActive||streetSprint.IsCountingDown",
        "circuitRace.IsActive||circuitRace.IsCountingDown",
        "driftChallenge.IsActive||driftChallenge.IsCountingDown",
        "towTruck.IsActive", "carWash.IsActive", "professions.IsActive",
    ):
        check(code in nav, f"Minimap active target or countdown lost: {code}")

    # No new costly scene-wide scan/update: markers receive the same manager
    # which is already created by MotorCityBootstrap.
    check("FindObjectsOfType" not in static and
          "FindAnyObjectByType" not in static,
          "Static marker must not scan scene every frame")

    if ERRORS:
        print("Phase 12 exclusive activity markers FAILED:")
        for error in ERRORS:
            print(" -", error)
        return 1

    print("Phase 12 exclusive activity markers source contract PASS.")
    print(" World: 4 route markers + all job/discovery/underground markers;")
    print(" miniblap: owner-only target and icon, active checkpoints preserved.")
    print(" Unity Editor and GitHub Pages browser smoke still required.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
