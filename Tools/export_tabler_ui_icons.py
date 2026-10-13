#!/usr/bin/env python3
"""Export a curated subset of the checked-in Tabler outline SVGs as Unity sprites.

Usage: python Tools/export_tabler_ui_icons.py
Requires: pip install cairosvg
Source: Assets/ThirdParty/outline/*.svg (MIT, notice in Assets/ThirdParty/LICENSE)
Output: Assets/Resources/MotorCity/UI/Icons/Tabler/*.png
"""
from pathlib import Path
import argparse
import re
import sys

# Distinct icon names used by Motor City's loading, HUD, activities, garage
# and career. Keep this selection small instead of importing 5k textures.
ICON_NAMES = tuple("""
flag-3 tools car-garage map-pin bulb trophy camera settings coins star gift
lock lock-open check plus player-play player-pause layout-grid shopping-cart
crown wheel steering-wheel car-suspension engine car-turbine palette car-4wd
truck-delivery car-crane car pizza mail spray briefcase moon-stars medal
history certificate sparkles clipboard-list sun users-group calendar-event
building car-suv tool manual-gearbox automatic-gearbox gauge brand-speedtest
map compass home arrows-left-right arrow-left arrow-right keyboard engine-off
car-lifter gas-station target route speedboat
""".split())

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true",
                        help="Only check source names; don't export PNGs")
    parser.add_argument("--size", type=int, default=192,
                        help="Output image width and height (default: 192)")
    args = parser.parse_args()

    project = Path(__file__).resolve().parents[1]
    source_dir = project / "Assets/ThirdParty/outline"
    target_dir = project / "Assets/Resources/MotorCity/UI/Icons/Tabler"
    notice = project / "Assets/ThirdParty/LICENSE"
    missing = [name for name in ICON_NAMES
               if not (source_dir / (name + ".svg")).is_file()]
    if not notice.is_file():
        sys.exit("Tabler MIT LICENSE is missing at Assets/ThirdParty/LICENSE")
    if missing:
        sys.exit("Missing Tabler SVG files: " + ", ".join(missing))

    if args.check:
        print(f"Tabler icons OK: {len(ICON_NAMES)} source SVGs + license")
        return

    try:
        import cairosvg
    except ImportError:
        sys.exit("Install converter first: python -m pip install cairosvg")

    target_dir.mkdir(parents=True, exist_ok=True)
    for name in ICON_NAMES:
        svg = (source_dir / (name + ".svg")).read_text(encoding="utf-8")
        # Tabler SVG uses currentColor; bake white pixels so Unity UI Image
        # tinting controls cyan / violet / disabled state at runtime.
        svg = svg.replace("currentColor", "#ffffff")
        # An imported SVG may include metadata comments; CairoSVG handles it.
        cairosvg.svg2png(bytestring=svg.encode("utf-8"),
                        write_to=str(target_dir / (name + ".png")),
                        output_width=args.size, output_height=args.size)
    print(f"Exported {len(ICON_NAMES)} PNG sprites to {target_dir}")

if __name__ == "__main__":
    main()
