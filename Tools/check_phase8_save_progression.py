#!/usr/bin/env python3
"""Phase 8 save/progression contracts and key inventory. Read-only."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "Assets" / "Scripts"
REPORT = ROOT / "Temp" / "MotorCityAudit" / "phase8-save-keys.json"
SERVICE = SCRIPTS / "Persistence/MotorCitySaveService.cs"
CLOUD = SCRIPTS / "Persistence/MotorCityCloudSaveRuntime.cs"
RUNTIME = SCRIPTS / "Persistence/MotorCitySaveRuntime.cs"

def main() -> int:
    errors: list[str] = []
    sources = {}
    for path in (SERVICE, CLOUD, RUNTIME):
        if not path.is_file():
            errors.append(f"Missing persistence source: {path.relative_to(ROOT)}")
            sources[path] = ""
        else:
            sources[path] = path.read_text(encoding="utf-8-sig")
    service, cloud, runtime = (sources[p] for p in (SERVICE, CLOUD, RUNTIME))
    contracts = {
        "stable_v1_storage_key": '"MotorCity.Save.Json.v1"' in service,
        "corrupt_backup_key": '"MotorCity.Save.CorruptBackup.v1"' in service,
        "corrupt_backup_before_delete": (
            service.find("PlayerPrefs.SetString(\n                        CorruptBackupKey,") >= 0
            and service.find("CorruptBackupKey,\n                        json)") <
            service.find("PlayerPrefs.DeleteKey(\n                        StorageKey)")
        ),
        "schema_version_2": "CurrentVersion = 2;" in service,
        "json_import_normalized": bool(re.search(r"document\s*=\s*Normalize\s*\(\s*imported\s*\)", service)),
        "cloud_revision_conflict": "ShouldUseRemote(" in cloud and "HasUnsyncedChanges" in cloud,
        "upload_revision_snapshot": "ExportCloudJson(" in cloud and "attemptedRevision" in cloud,
        "upload_ack_revision": "MarkCloudUploadSucceeded(" in cloud,
        "paid_entitlement_retention": (
            '"MotorCity.Purchase.SupporterPack"' in cloud
            and '"MotorCity.Purchase.SupporterPack.RewardClaimed"' in cloud
        ),
        "local_flush_on_background": "OnApplicationPause(" in runtime and "OnApplicationFocus(" in runtime,
    }
    for key, ok in contracts.items():
        if not ok:
            errors.append(f"Save contract changed or missing: {key}")

    inventory = []
    key_regex = re.compile(
        r'MotorCitySaveService\.(GetInt|GetFloat|GetString|SetInt|SetFloat|SetString|HasKey|DeleteKey)\s*'
        r'\(\s*"([^"]+)"'
    )
    for file in sorted(SCRIPTS.rglob("*.cs")):
        src = file.read_text(encoding="utf-8-sig", errors="replace")
        for match in key_regex.finditer(src):
            inventory.append({
                "source": file.relative_to(ROOT).as_posix(),
                "method": match.group(1),
                "key": match.group(2),
                "dynamic_key_not_included": False,
            })
    key_summary = {}
    for entry in inventory:
        k = entry["key"]
        key_summary.setdefault(k, {"readers": [], "writers": [], "deletes": []})
        bucket = (
            "deletes" if entry["method"] == "DeleteKey"
            else "writers" if entry["method"].startswith("Set")
            else "readers"
        )
        key_summary[k][bucket].append(entry["source"])
    report = {
        "phase": 8,
        "scope": "literal save keys only; dynamic keys and legacy PlayerPrefs require manual audit",
        "contracts": contracts,
        "literal_keys": key_summary,
        "calls": inventory,
        "limitations": [
            "No live cloud account or migration was exercised.",
            "No player save was read, reset or modified.",
            "Key strings built by concatenation or constants are not covered by this source inventory.",
        ],
        "errors": errors,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Phase 8 save audit: {len(contracts)} contracts, {len(key_summary)} literal keys, {len(inventory)} call sites.")
    for e in errors:
        print("ERROR:", e)
    return 1 if errors else 0

if __name__ == "__main__":
    raise SystemExit(main())
