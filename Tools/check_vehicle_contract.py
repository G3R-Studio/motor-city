#!/usr/bin/env python3
"""Phase 3 read-only prefab and wheel-pivot baseline; never edits Unity assets."""
from pathlib import Path
import json
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
BLOCK = re.compile(r"^--- !u!(1|4) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)", re.MULTILINE)
POSE = re.compile(r"m_Local(Position|Rotation): \{([^}]+)\}")
SNAPSHOT = ROOT / "Docs/Baselines/VehicleWheelTransforms-2026-10-09.json"

def poses(content):
    blocks = list(BLOCK.finditer(content))
    transforms = {match[2]: match[3] for match in blocks if match[1] == "4"}
    found = {}
    for match in blocks:
        if match[1] != "1":
            continue
        name = re.search(r"^  m_Name: (.+)$", match[3], re.MULTILINE)
        if name is None or name[1] not in WHEELS:
            continue
        component = re.search(r"component: \{fileID: (\d+)\}", match[3])
        transform = transforms.get(component[1]) if component else None
        if transform is None:
            raise ValueError(f"{name[1]} missing referenced Transform")
        values = {}
        for kind, raw in POSE.findall(transform):
            values[kind.lower()] = [float(v.split(':', 1)[1]) for v in raw.split(',')]
        if 'position' not in values or 'rotation' not in values:
            raise ValueError(f"{name[1]} missing local pose")
        found[name[1]] = values
    return found

def run():
    errors = []
    ids = ID_FILE.read_text(encoding="utf-8-sig")
    snapshots = json.loads(SNAPSHOT.read_text(encoding="utf-8"))["vehicles"]
    for prefab in PREFABS:
        path = FOLDER / (prefab + ".prefab")
        if not path.is_file():
            errors.append(f"Missing prefab: {path.relative_to(ROOT)}")
            continue
        if not (path.with_suffix(".prefab.meta")).is_file():
            errors.append(f"Missing .meta: {path.relative_to(ROOT)}")
        content = path.read_text(encoding="utf-8-sig")
        names = NAME.findall(content)
        try:
            actual = poses(content)
            expected = snapshots[prefab]
            for wheel in WHEELS:
                if wheel not in actual or wheel not in expected:
                    errors.append(f"{prefab}: missing {wheel} pose snapshot")
                    continue
                for field in ('position', 'rotation'):
                    x, y = actual[wheel][field], expected[wheel][field]
                    if len(x) != len(y) or any(abs(a-b) > 0.000001 for a,b in zip(x,y)):
                        errors.append(f"{prefab}/{wheel}: {field} differs from snapshot")
        except (KeyError, ValueError) as exc:
            errors.append(f"{prefab}: invalid snapshot/pose: {exc}")
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
    print(f"Vehicle contract baseline passed: {len(PREFABS)} prefabs, 4 unique named wheel pivots and matching transform snapshots each.")
    print("Street is a stable fallback ID; not a separate authored Player prefab.")
    print("Visual alignment, mesh ownership and runtime handling still require Unity validation.")
    return 0

if __name__ == "__main__":
    sys.exit(run())
