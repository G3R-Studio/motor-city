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
    # Inspect committed importer metadata before considering the legacy fixer obsolete.
    legacy_location = []
    for meta in (ROOT / "Assets").rglob("*.fbx.meta"):
        content = meta.read_text(encoding="utf-8-sig", errors="replace")
        if re.search(r"(?m)^\s*materialLocation:\s*0\s*$", content):
            legacy_location.append(str(meta.relative_to(ROOT)).replace("\\", "/"))

    # Detect duplicate material GUID declarations, a destructive cleanup blocker.
    material_guids = {}
    duplicate_material_guids = []
    for meta in (ROOT / "Assets").rglob("*.mat.meta"):
        found = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$",
                          meta.read_text(encoding="utf-8-sig", errors="replace"))
        if not found:
            errors.append(f"Material .meta missing GUID: {meta.relative_to(ROOT)}")
            continue
        guid = found.group(1)
        if guid in material_guids:
            duplicate_material_guids.append((material_guids[guid], str(meta.relative_to(ROOT))))
        material_guids[guid] = str(meta.relative_to(ROOT))
    for a, b in duplicate_material_guids:
        errors.append(f"Duplicate material GUID: {a} and {b}")

    # These editor utilities still expose live repair/build commands. In
    # particular, URP repair can regenerate FCG runtime materials and traffic
    # prefabs; deleting the tool based only on the absence of direct code calls
    # would remove a supported recovery path.
    migration_uses = {
        "FantasticCityGeneratorLegacyImporterFixer.cs": "legacy FBX materialLocation recovery",
        "FantasticCityGeneratorUrpFixer.cs": "FCG materials and traffic prefab repair",
    }
    for name, purpose in migration_uses.items():
        if migrators[name]["menu_commands"] == 0:
            errors.append(f"{name}: expected manual recovery menu missing ({purpose})")

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
        "editor_migration_tool_recovery_roles": migration_uses,
        "classification": "No deletion candidates certified; Resources and editor-generated assets remain protected.",
        "legacy_fbx_importers_needing_migration": legacy_location,
        "material_guid_duplicates": duplicate_material_guids,
        "orphan_asset_deletions": 0,
        "warnings": [
            "An unused GUID in text is not proof of an orphan: Resources.Load and editor generation are dynamic.",
            "FCG migration/fixer tools are retained until real Unity importer state confirms they are obsolete.",
        ],
        "errors": errors,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, indent=2, ensure_ascii=False)+"\n", encoding="utf-8")
    print("Legacy FBX importer entries needing migration:", len(legacy_location))
    print("Phase 5 cleanup audit:", len(IMPORTERS), "dead constants removed;",
          len(materials), "materials protected; 0 unproven orphan deletions.")
    for error in errors:
        print("ERROR:", error)
    return 1 if errors else 0

if __name__ == "__main__":
    raise SystemExit(main())
