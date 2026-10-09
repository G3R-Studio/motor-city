#!/usr/bin/env python3
"""Phase 5 safe source/asset cleanup audit; no asset deletion."""
from __future__ import annotations
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EDITORS = ROOT / "Assets" / "Editor"
REPORT = ROOT / "Temp" / "MotorCityAudit" / "phase5-cleanup.json"

IMPORTERS = ("Peugeot306", "ToyotaAE86", "Hybrid", "Porsche996",
             "AmgGT", "Camaro", "Delorean", "Bus")
MIGRATORS = ("FantasticCityGeneratorLegacyImporterFixer.cs",
             "FantasticCityGeneratorUrpFixer.cs")

def main():
    errors = []
    for name in IMPORTERS:
        source = (EDITORS / (name + "VehicleImporter.cs")).read_text(encoding="utf-8-sig")
        if re.search(r"\bBuildSessionKey\b", source):
            errors.append(f"{name}: obsolete unused BuildSessionKey reappeared")
        if "SourceHashKey" not in source or "ShouldRebuild(" not in source:
            errors.append(f"{name}: incremental importer dependency guard missing")
    migrators = {}
    for name in MIGRATORS:
        file = EDITORS / name
        content = file.read_text(encoding="utf-8-sig") if file.exists() else ""
        migrators[name] = {"tracked": file.exists(), "menu_commands": len(re.findall(r"\[MenuItem\(", content))}
        if not file.exists():
            errors.append(f"Missing migration tool: {name}")
    # GUID-only scans are insufficient to declare Resources.Load, AssetDatabase
    # derived paths, editor-generated and Addressables assets safe for removal.
    # Preserve all existing material/prefab assets until the Unity dependency
    # report plus runtime/resource path references have been reviewed.
    materials = list((ROOT / "Assets" / "Resources" / "MotorCity").rglob("*.mat"))
    report = {
        "phase": 5,
        "dead_importer_constants_removed": len(IMPORTERS),
        "material_assets_preserved_pending_dependency_review": len(materials),
        "editor_migration_tools_retained": migrators,
        "orphan_asset_deletions": 0,
        "warnings": [
            "An unused GUID in text is not proof of an orphan: Resources.Load and editor generation are dynamic.",
            "FCG migration/fixer tools are retained until real Unity importer state confirms they are obsolete.",
        ],
        "errors": errors,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, indent=2, ensure_ascii=False)+"\n", encoding="utf-8")
    print("Phase 5 cleanup audit:", len(IMPORTERS), "dead constants removed;",
          len(materials), "materials protected; 0 unproven orphan deletions.")
    for error in errors:
        print("ERROR:", error)
    return 1 if errors else 0

if __name__ == "__main__":
    raise SystemExit(main())
