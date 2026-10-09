#!/usr/bin/env python3
"""Phase 4: ensure exact, vehicle-scoped OBJ materials remain in the explicit role manifest."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "Assets/Scripts/Vehicle/VehicleMaterialRoleCatalog.cs"
SOURCE = ROOT / "Assets/VehicleAssets"
VEHICLES = {
    "AmgGT": "amggt",
    "Beatall": "beatall",
    "Bus": "bus",
    "Camaro": "camaro",
    "Delorean": "delorean",
    "Hybrid": "hybrid",
    "Peugeot306": "peugeot306",
    "Porsche996": "porsche996",
    "ToyotaAE86": "toyotaae86",
}
EXPECTED = {
    "AmgGT": {"Glass", "FrontLamp", "RearLamp"},
    "Beatall": {"Glass", "FrontLamp", "RearLamp"},
    "Bus": {"Glass", "FrontLamp", "RearLamp"},
    "Camaro": {"Glass", "FrontLamp", "RearLamp"},
    "Delorean": {"Glass", "FrontLamp", "RearLamp"},
    "Hybrid": {"Glass"},
    "Peugeot306": {"Body", "Glass", "FrontLamp", "RearLamp"},
    "Porsche996": {"Body", "Glass", "FrontLamp", "RearLamp"},
    "ToyotaAE86": {"Body", "Glass", "RearLamp"},
}

def normalize(value):
    return value.strip().lower().replace(" ", "_").replace(".", "_")

def main():
    source = CATALOG.read_text(encoding="utf-8-sig")
    roles = {}
    blocks = re.findall(
        r'((?:\s*case\s+"[^"]+":)+)\s*return\s+VehicleMaterialRole\.(\w+);',
        source)
    for cases, role in blocks:
        for name in re.findall(r'case\s+"([^"]+)":', cases):
            if name in roles and roles[name] != role:
                raise ValueError(f"Conflicting catalog role: {name}")
            roles[name] = role
    errors = []
    for vehicle, basename in VEHICLES.items():
        mtl = SOURCE / vehicle / (basename + ".mtl")
        if not mtl.is_file():
            errors.append(f"{vehicle}: missing source MTL")
            continue
        material_names = re.findall(
            r'^newmtl\s+(.+?)\s*$', mtl.read_text(encoding="utf-8-sig"), re.MULTILINE)
        actual = {roles.get(normalize(name)) for name in material_names}
        actual.discard(None)
        missing = EXPECTED[vehicle] - actual
        if missing:
            errors.append(f"{vehicle}: missing explicit MTL roles {sorted(missing)}")
        print(f"{vehicle}: authored source roles: {', '.join(sorted(actual)) or 'none'}")
    if errors:
        print("Phase 4 source catalog validation FAILED:")
        for error in errors:
            print(" -", error)
        return 1
    print("Phase 4 authored MTL role catalog checks passed for nine vehicle bodies.")
    print("Ambiguous paint, mirror and wheel materials intentionally remain unclassified.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
