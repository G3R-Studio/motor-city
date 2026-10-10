#!/usr/bin/env python3
"""Phase 9 conservative city/runtime performance contracts."""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
installer = (root / "Assets/Scripts/World/CityAssetRuntimeInstaller.cs").read_text(encoding="utf-8-sig")
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
print("Phase 9 city source check: " + ("PASS" if not errors else "FAIL"))
for error in errors:
    print("ERROR:", error)
raise SystemExit(bool(errors))
