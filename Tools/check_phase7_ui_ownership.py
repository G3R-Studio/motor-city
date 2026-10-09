#!/usr/bin/env python3
"""Phase 7: conservative source inventory of UI layout ownership (read-only)."""
from __future__ import annotations
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "Assets/Scripts/UI"
REPORT = ROOT / "Temp/MotorCityAudit/phase7-ui-ownership.json"
FILES = {
    "HudVisualPolish.cs": "post-layout responsive polish",
    "PrototypeHud.cs": "runtime HUD controller",
    "GarageReferenceLayout.cs": "garage layout builder",
    "NavigatorView.cs": "navigator layout builder",
    "TouchControlsView.cs": "touch controls builder and customization",
}
REQUIRED = {
    "HudVisualPolish.cs": ("ApplyDesktopOrTouchComposition", "ApplyModalComposition", "FindRect"),
    "GarageReferenceLayout.cs": ("BuildGarage",),
    "TouchControlsView.cs": ("RectTransform",),
    "NavigatorView.cs": ("RectTransform",),
}
def main() -> int:
    errors = []
    rows = []
    for file, role in FILES.items():
        path = UI / file
        if not path.is_file():
            errors.append(f"Missing UI ownership source: {file}")
            continue
        src = path.read_text(encoding="utf-8-sig")
        for token in REQUIRED.get(file, ()):
            if token not in src:
                errors.append(f"Expected UI owner marker absent: {file}: {token}")
        rows.append({
            "path": path.relative_to(ROOT).as_posix(),
            "role": role,
            "sizeDelta_assignments": len(re.findall(r"\.sizeDelta\s*=", src)),
            "anchoredPosition_assignments": len(re.findall(r"\.anchoredPosition\s*=", src)),
            "named_rect_lookups": len(re.findall(r"\bFindRect\s*\(", src)),
            "runtime_find_calls": len(re.findall(r"\bGameObject\.Find\s*\(", src)),
        })
    polish = (UI / "HudVisualPolish.cs").read_text(encoding="utf-8-sig")
    names = sorted(set(re.findall(r'FindRect\s*\(\s*"([^"]+)"', polish)))
    if not names:
        errors.append("HUD polish name-bound layout inventory is unexpectedly empty")
    # Dynamic UI builders can replace a child without replacing the HUD root.
    # A persistent post-layout pass must never reuse detached cached targets.
    if "cached.IsChildOf(hudRoot)" not in polish or "rectCache.Remove(objectName)" not in polish:
        errors.append("HUD polish must invalidate cached targets detached from the active HUD")

    # Driving controls are owned by TouchControlsView and the user's layout
    # customization. HudVisualPolish may style controls but must not take over
    # their transform geometry.
    touch_owned = (
        "Touch Throttle", "Touch Brake", "Touch Handbrake",
        "Touch Action", "Touch Steering Wheel",
    )
    for name in touch_owned:
        if re.search(r'FindRect\s*\(\s*"' + re.escape(name) + r'"', polish):
            errors.append(f"Polish must not claim a touch-control geometry target: {name}")
    # Utility action geometry is authored in TouchControlsView. Prevent
    # reintroducing a second position writer in HudVisualPolish.
    for name in ("HUD Utility Rail", "HUD Secondary Actions"):
        if re.search(r'FindRect\s*\(\s*"' + re.escape(name) + r'"', polish):
            errors.append(f"Duplicate utility action layout owner in HUD polish: {name}")
    touch_source = (UI / "TouchControlsView.cs").read_text(encoding="utf-8-sig")
    for name in ("HUD Utility Rail", "HUD Secondary Actions"):
        if f'"{name}"' not in touch_source:
            errors.append(f"Touch control layout owner missing: {name}")
    # NavigatorView builds the minimap and target strip using the exact
    # desktop geometry formerly repeated by the visual polish pass.
    for name in ("Minimap", "Navigation Target Strip"):
        if re.search(r'FindRect\s*\(\s*"' + re.escape(name) + r'"', polish):
            errors.append(f"Duplicate navigator geometry writer in HUD polish: {name}")
    navigator_source = (UI / "NavigatorView.cs").read_text(encoding="utf-8-sig")
    for name in ("Minimap", "Navigation Target Strip"):
        if f'"{name}"' not in navigator_source:
            errors.append(f"Navigator layout builder missing: {name}")

    report = {
        "phase": 7,
        "kind": "source inventory (does not prove overlapping RectTransform instances)",
        "owners": rows,
        "hud_polish_named_targets": names,
        "touch_geometry_owned_by": "TouchControlsView / player customization",
        "risk": "Polish may write sizes/positions after HUD, navigator and touch builders.",
        "rule": "Do not delete builders or move layout writes until per-object runtime ownership is mapped.",
        "errors": errors,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Phase 7 UI ownership inventory: {len(rows)} owners, {len(names)} named polish targets.")
    for error in errors:
        print("ERROR:", error)
    return bool(errors)

if __name__ == "__main__":
    raise SystemExit(main())
