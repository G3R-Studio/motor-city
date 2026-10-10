#!/usr/bin/env python3
"""Phase 9 conservative city/runtime performance contracts."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
installer = (root / "Assets/Scripts/World/CityAssetRuntimeInstaller.cs").read_text(encoding="utf-8-sig")
day_night = (root / "Assets/Scripts/World/DayNightCycleController.cs").read_text(encoding="utf-8-sig")
gate = (root / "Assets/Editor/MotorCityPhase2BatchGate.cs").read_text(encoding="utf-8-sig")
front_end = (root / "Assets/Scripts/UI/MotorCityFrontEndFlow.cs").read_text(encoding="utf-8-sig")
audit = root / "Assets/Editor/MotorCityPhase9CityMaterialAudit.cs"
errors = []
if not audit.is_file() or "MotorCityPhase9CityMaterialAudit.Validate(folder)" not in gate:
    errors.append("Missing read-only city prefab Editor audit integration.")

# The async prefetch may change when loading happens, but it must never
# change the authored city prefab or the synchronous compatibility fallback.
if "Resources.LoadAsync<GameObject>(ResourcePath)" not in installer:
    errors.append("Missing asynchronous city prefab prefetch.")
if "Resources.Load<GameObject>(ResourcePath)" not in installer:
    errors.append("Missing synchronous city prefab compatibility fallback.")
if "cityPrefabPreload.isDone" not in installer:
    errors.append("City installer must only consume a completed prefab request.")
if "cityPrefabPreload =\n                null;" not in installer:
    errors.append("Domain-reload reset for city prefab prefetch is missing.")

# The front end should animate while Resources.LoadAsync is pending, and only
# request the original gameplay bootstrap after it has completed.
prefetch = front_end.find("CityAssetRuntimeInstaller.BeginPrefabPreload();")
ready = front_end.find("CityAssetRuntimeInstaller.IsPrefabPreloadReady")
invoke = front_end.find("gameplayLoadRequested?.Invoke();")
if min(prefetch, ready, invoke) < 0 or not prefetch < ready < invoke:
    errors.append("Loading-screen city prefetch / bootstrap ordering changed.")
# Release must not execute the expensive full-city WebGL material scan.
# Development players and the explicit audit symbol keep the QA path.
webgl_section = installer.find("#if UNITY_WEBGL && !UNITY_EDITOR", installer.find("BindAuthoredWindowEmission("))
debug_flag = installer.find("bool runMaterialDiagnostics = Debug.isDebugBuild;", webgl_section)
audit_flag = installer.find("#if MOTORCITY_CITY_MATERIAL_AUDIT", debug_flag)
run_guard = installer.find("if (runMaterialDiagnostics)", audit_flag)
scan_call = installer.find("MotorCityWebMaterialDiagnostics.Run(activeCity);", run_guard)
if min(webgl_section, debug_flag, audit_flag, run_guard, scan_call) < 0 or not (
    webgl_section < debug_flag < audit_flag < run_guard < scan_call
):
    errors.append("WebGL city material audit must remain restricted to development or explicit audit builds.")

# The 489/288 authored lamp counts are a diagnostic, not a release
# requirement. Do not suppress actual lamp registration or switching.
lamp_qa_guard = re.search(
    r"if\s*\(\s*\(Application\.isEditor\s*\|\|\s*Debug\.isDebugBuild\)"
    r"\s*&&\s*\(streetLightSourceCount\s*!=\s*489\s*\|\|"
    r"\s*parkLampSourceCount\s*!=\s*288\)\s*\)\s*\{"
    r"\s*Debug\.LogWarning\(",
    day_night,
)
if lamp_qa_guard is None:
    errors.append("Authored lamp-count warning must be QA-only in Editor/development builds.")
if "GetComponentsInChildren<Light>(true)" not in day_night or "ApplyStreetLights();" not in day_night:
    errors.append("Runtime authored lamp registration / switching must remain intact.")

print("Phase 9 city source check: " + ("PASS" if not errors else "FAIL"))
for error in errors:
    print("ERROR:", error)
raise SystemExit(bool(errors))
