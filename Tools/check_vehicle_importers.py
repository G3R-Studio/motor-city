#!/usr/bin/env python3
"""Read-only Phase 3 importer source integrity audit."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
editor = root / "Assets/Editor"
vehicles = ("Beatall", "Peugeot306", "ToyotaAE86", "Hybrid", "Porsche996", "AmgGT", "Camaro", "Delorean", "Bus")

def main():
    errors = []
    for name in vehicles:
        path = editor / (name + "VehicleImporter.cs")
        if not path.exists():
            errors.append(name + ": missing importer")
            continue
        text = path.read_text(encoding="utf-8-sig")
        if "SaveAsPrefabAsset" not in text:
            errors.append(name + ": no prefab save")
    hybrid = (editor / "HybridVehicleImporter.cs").read_text(encoding="utf-8-sig")
    for name in ("hybrid.obj", "wheels1.obj", "wheels2.obj"):
        if not (root / "Assets/VehicleAssets/Hybrid" / name).exists() or name not in hybrid:
            errors.append("Hybrid: missing source " + name)
    for term in ("InstantiateSource(", '"Body"', "StripImportedRuntimeComponents("):
        if term not in hybrid:
            errors.append("Hybrid: missing body contract " + term)
    if errors:
        print("\n".join(errors))
        return 1
    print("Vehicle importer source check passed: 9 importers, Hybrid body and source assets.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
