#!/usr/bin/env python3
"""Phase 9 conservative city/runtime performance contracts."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
installer = (root / "Assets/Scripts/World/CityAssetRuntimeInstaller.cs").read_text(encoding="utf-8-sig")
gate = (root / "Assets/Editor/MotorCityPhase2BatchGate.cs").read_text(encoding="utf-8-sig")
audit = root / "Assets/Editor/MotorCityPhase9CityMaterialAudit.cs"
errors = []
expected = """#if UNITY_WEBGL && !UNITY_EDITOR
            ConvertUnsupportedCityMaterialsForWeb();
#if DEVELOPMENT_BUILD || MOTORCITY_CITY_MATERIAL_AUDIT
            MotorCityWebMaterialDiagnostics.Run(
                activeCity);
#endif
#endif"""
if expected not in installer:
    errors.append("Full city renderer diagnostic must remain disabled in WebGL release.")
if "RepairMissingPlantMaterials();" not in installer:
    errors.append("Do not remove runtime plant fallback before authored prefab validation.")
if "GetComponentsInChildren<Renderer>" not in installer:
    errors.append("Expected city renderer snapshot.")
if not audit.is_file() or "MotorCityPhase9CityMaterialAudit.Validate(folder)" not in gate:
    errors.append("Missing read-only city prefab Editor audit integration.")
print("Phase 9 city source check: " + ("PASS" if not errors else "FAIL"))
for error in errors:
    print("ERROR:", error)
raise SystemExit(bool(errors))
