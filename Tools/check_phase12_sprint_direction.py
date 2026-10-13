#!/usr/bin/env python3
"""Regression contract for static Tabler sprint markers in world and minimap.

The historic filename is retained because existing source checks may invoke it.
Sprint gameplay can still progress between waypoints; its icon never points to
the next one. This source contract does not replace a Unity Editor smoke test.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WORLD = ROOT / "Assets/Scripts/World/StreetSprintMarkerVisual.cs"
MAP = ROOT / "Assets/Scripts/UI/NavigatorView.cs"
ICONS = ROOT / "Assets/Scripts/UI/MotorCityIconLibrary.cs"
BUILDER = ROOT / "Assets/Editor/MotorCityActivityMarkerBuilder.cs"


def compact(source: str) -> str:
    return re.sub(r"\s+", "", source)


def main() -> int:
    world = WORLD.read_text(encoding="utf-8-sig")
    nav = MAP.read_text(encoding="utf-8-sig")
    icons = ICONS.read_text(encoding="utf-8-sig")
    builder = BUILDER.read_text(encoding="utf-8-sig")
    errors = []

    checks = (
        ('"flag-3.png"' in builder, "World Sprint prefab must use Tabler flag"),
        ('"sprint" => "flag-3"' in icons, "Minimap must use Tabler sprint flag"),
        ("UpdateSprintChevronDirection" not in world, "World icon still rotates"),
        ("sprintChevrons" not in world, "World icon still has arrow tracking"),
        ("TryGetNextTarget" not in world, "World marker still samples route direction"),
        ("SetDirection" not in world, "Fallback beacon still receives directions"),
        ("CheckpointBeaconStyle.Sprint" in world, "Sprint fallback style missing"),
        ("false,CheckpointBeaconStyle.Sprint" in compact(world),
         "Sprint fallback must not create a direction arrow"),
        ('sprintCheckpoint' not in nav, "Minimap still calculates sprint direction"),
        ("streetSprint.TryGetNextTarget" not in nav,
         "Minimap still samples the next checkpoint"),
        ("nextCheckpoint -" not in nav, "Minimap still uses a route vector"),
        ("minimapTargetIcon.rectTransform.localRotation=Quaternion.identity;"
         in compact(nav), "Minimap activity icon must keep fixed rotation"),
        ("minimapTargetIcon.sprite=minimapMaskSprite;" not in compact(nav),
         "Minimap must not substitute a dot at the last sprint checkpoint"),
    )
    for passed, reason in checks:
        if not passed:
            errors.append(reason)

    if errors:
        print("Static Sprint icon regression FAILED:")
        for message in errors:
            print(" -", message)
        return 1

    print("Static Sprint Tabler marker contract PASS (world, minimap, fallback).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
