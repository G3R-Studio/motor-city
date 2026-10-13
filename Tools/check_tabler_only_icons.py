#!/usr/bin/env python3
"""Source audit: the only runtime icon pack is Tabler, with no legacy assets.

Run: python Tools/check_tabler_only_icons.py
This is a source/assets regression check, not a substitute for Unity Play Mode.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
UI = ASSETS / "Scripts/UI"
ICONS = ASSETS / "Resources/MotorCity/UI/Icons/Tabler"
EXPORT = ROOT / "Tools/export_tabler_ui_icons.py"
MARKERS = ASSETS / "Resources/MotorCity/Markers"

RETIRED = (
    "Assets/Resources/MotorCity/UI/Icons/Kenney",
    "Assets/Resources/MotorCity/UI/Icons/Kenney.meta",
    "Assets/Art/MotorCity/Markers",
    "Assets/Art/MotorCity/Markers.meta",
    "Assets/Scripts/UI/MotorCityLoadingIcons.cs",
    "Assets/Scripts/UI/MotorCityLoadingIcons.cs.meta",
    "Assets/Resources/MotorCity/Markers/flag.png",
    "Assets/Resources/MotorCity/Markers/flag.png.meta",
)
MARKER_ICONS = {
    "Drift": "car-turbine",
    "Delivery": "truck-delivery",
    "Sprint": "flag-3",
    "Circuit": "trophy",
    "Discovery": "map-pin",
    "Underground": "moon-stars",
    "Profession": "briefcase",
    "CarWash": "spray",
    "Tow": "car-crane",
}


def main():
    problems = []
    for path in RETIRED:
        if (ROOT / path).exists():
            problems.append("Old icon asset or system remains: " + path)

    exporter = EXPORT.read_text(encoding="utf-8")
    names = re.search(
        r'ICON_NAMES\s*=\s*tuple\("""([\s\S]+?)"""\.split\(\)\)',
        exporter,
    )
    if not names:
        problems.append("Cannot read curated Tabler export catalog")
        known = set()
    else:
        known = set(names.group(1).split())
        for name in sorted(known):
            for suffix in (".png", ".png.meta"):
                if not (ICONS / (name + suffix)).is_file():
                    problems.append("Missing Tabler sprite asset: " + name + suffix)

    library = (UI / "MotorCityIconLibrary.cs").read_text(encoding="utf-8")
    front = (UI / "MotorCityFrontEndFlow.cs").read_text(encoding="utf-8")
    garage = (UI / "GarageReferenceGraphic.cs").read_text(encoding="utf-8")
    importer = (ASSETS / "Editor/MotorCityIconPostprocessor.cs").read_text(
        encoding="utf-8"
    )
    for name, source in (
        ("MotorCityIconLibrary", library),
        ("MotorCityFrontEndFlow", front),
        ("GarageReferenceGraphic", garage),
        ("MotorCityIconPostprocessor", importer),
    ):
        for marker in ("Icons/Kenney", "KenneyGameIcons",
                       "MotorCityLoadingIcons", "TablerOrKenney"):
            if marker in source:
                problems.append(name + " references " + marker)

    if 'Resources.Load<GameObject>(\n                    "MotorCity/Markers/"' in library:
        problems.append("Icon library still extracts artwork from VFX prefabs")
    if "case Symbol.Engine:" in garage or "private void Arc(" in garage:
        problems.append("Procedural glyph painter still present")
    for required in ("private void Surface()", "private void NavigationSurface()",
                     "private void Ring("):
        if required not in garage:
            problems.append("Active decorative UI surface was removed: " + required)

    # Validate all literal Tabler names used by icon library and vector buttons.
    referenced = set(re.findall(r'TablerIcon\("([\w-]+)"\)', library))
    referenced.update(re.findall(r'Symbol\.\w+\s*=>\s*"([\w-]+)"', garage))
    world = library.split("string tablerName = normalized switch", 1)
    if len(world) == 2:
        referenced.update(re.findall(
            r'=>\s*"([\w-]+)"',
            world[1].split("return tablerName", 1)[0],
        ))
    else:
        problems.append("World marker Tabler lookup is missing")
    referenced.update(MARKER_ICONS.values())
    referenced.add("star")
    for missing in sorted(referenced - known):
        problems.append("Tabler name not in export list: " + missing)

    # Existing world-marker prefabs must reference Tabler GUIDs, not deleted PNGs.
    for marker, icon in MARKER_ICONS.items():
        prefab = MARKERS / (marker + "MarkerVfx.prefab")
        meta = ICONS / (icon + ".png.meta")
        if not prefab.exists() or not meta.exists():
            problems.append("Missing world marker or icon: " + marker)
            continue
        guid = re.search(r'^guid:\s*([0-9a-f]+)', meta.read_text(), re.M)
        assigned = re.findall(
            r'm_Sprite:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]+)',
            prefab.read_text(),
        )
        if not guid or not assigned or any(g != guid.group(1) for g in assigned):
            problems.append("Prefab has non-Tabler sprite reference: " + marker)

    if problems:
        print("FAIL: legacy icon cleanup\n" + "\n".join(" - " + p for p in problems))
        return 1

    print(
        "PASS: %d exported Tabler sprites; %d world marker prefabs; "
        "no legacy icon systems/assets; decorative UI preserved."
        % (len(known), len(MARKER_ICONS))
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
