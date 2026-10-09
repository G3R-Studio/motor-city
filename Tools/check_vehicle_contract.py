#!/usr/bin/env python3
"""Phase 3 read-only prefab and wheel-pivot baseline; never edits Unity assets."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / "Assets/Resources/MotorCity/Vehicles/Player"
PREFABS = ("Beatall", "Peugeot306", "ToyotaAE86", "Hybrid",
           "Porsche996", "AmgGT", "Camaro", "Delorean", "Bus")
WHEELS = ("front_left", "front_right", "rear_left", "rear_right")
ID_FILE = ROOT / "Assets/Scripts/Vehicle/VehicleIds.cs"
NAME = re.compile(r"^  m_Name: (.+)$", re.MULTILINE)
TRANSFORM = re.compile(r"^--- !u!4 &", re.MULTILINE)

def run():
    errors = []
    ids = ID_FILE.read_text(encoding="utf-8-sig")
    for prefab in PREFABS:
        path = FOLDER / (prefab + ".prefab")
        if not path.is_file():
            errors.append(f"Missing prefab: {path.relative_to(ROOT)}")
            continue
        if not (path.with_suffix(".prefab.meta")).is_file():
            errors.append(f"Missing .meta: {path.relative_to(ROOT)}")
        content = path.read_text(encoding="utf-8-sig")
        names = NAME.findall(content)
        for pivot in WHEELS:
            if names.count(pivot) != 1:
                errors.append(f"{prefab}: expected exactly one {pivot}, got {names.count(pivot)}")
        if len(TRANSFORM.findall(content)) < 4:
            errors.append(f"{prefab}: fewer than four Transform records")
        if not re.search(r"public const string " + re.escape(prefab) + r"\s*=", ids):
            errors.append(f"VehicleIds missing stable ID: {prefab}")
    if not re.search(r'public const string Street\s*=\s*"street"', ids):
        errors.append("Missing Street fallback ID")
    if errors:
        print("Vehicle contract baseline FAILED:")
        for err in errors:
            print(" -", err)
        return 1
    print(f"Vehicle contract baseline passed: {len(PREFABS)} prefabs, 4 unique named wheel pivots each.")
    print("Street is a stable fallback ID; not a separate authored Player prefab.")
    print("Visual alignment, mesh ownership and runtime handling still require Unity validation.")
    return 0

if __name__ == "__main__":
    sys.exit(run())
