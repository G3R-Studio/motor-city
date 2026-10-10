#!/usr/bin/env python3
"""Phase 9 conservative city/runtime performance contracts."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
installer = (root / "Assets/Scripts/World/CityAssetRuntimeInstaller.cs").read_text(encoding="utf-8-sig")
gate = (root / "Assets/Editor/MotorCityPhase2BatchGate.cs").read_text(encoding="utf-8-sig")
audit = root / "Assets/Editor/MotorCityPhase9CityMaterialAudit.cs"
errors = []
if not audit.is_file() or "MotorCityPhase9CityMaterialAudit.Validate(folder)" not in gate:
    errors.append("Missing read-only city prefab Editor audit integration.")
print("Phase 9 city source check: " + ("PASS" if not errors else "FAIL"))
for error in errors:
    print("ERROR:", error)
raise SystemExit(bool(errors))
