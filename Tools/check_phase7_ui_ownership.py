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
    "HudVisualPolish.cs": ("ApplyModalComposition", "FindRect"),
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

    # Each modal panel has one geometry owner: its builder. The visual
    # polish still handles inner text/details but cannot resize the root.
    modal_owners = {
        "Activity Result": ("ActivityResultView.cs", "650f", "520f"),
        "Navigator Menu": ("NavigatorView.cs", "640f", "420f"),
        "Club Panel": ("ClubView.cs", "580f", "400f"),
    }
    for name, (owner, width, height) in modal_owners.items():
        src = (UI / owner).read_text(encoding="utf-8-sig")
        if f'"{name}"' not in src or width not in src or height not in src:
            errors.append(f"Modal builder geometry contract missing: {owner}: {name}")
        if re.search(r'FindRect\s*\(\s*"' + re.escape(name) + r'"', polish):
            errors.append(f"Modal root has duplicate geometry owner in polish: {name}")

    # Legacy RacingUI modal sprites cannot return via a fallback builder.
    names = (
        "MotorCityButtonVisuals.cs", "MotorCityModalUiKit.cs",
        "TouchControlsView.cs", "NavigatorView.cs", "PauseMenuView.cs",
        "HudUiFactory.cs", "MotorCityFrontEndFlow.cs",
        "PrototypeHud.cs", "HudVisualPolish.cs",
    )
    sources = {
        name: (UI / name).read_text(encoding="utf-8-sig")
        for name in names
    }
    for name in ("MotorCityModalUiKit.cs", "TouchControlsView.cs",
                 "NavigatorView.cs", "PauseMenuView.cs"):
        if "MotorCityUiSkin.ApplyVectorButton(" not in sources[name]:
            errors.append(f"Button builder missing canonical vector owner: {name}")
    if "internal static GarageReferenceGraphic ApplyVectorButton(" not in sources["MotorCityButtonVisuals.cs"]:
        errors.append("Missing shared vector button factory")
    for name, code in sources.items():
        if any(token in code for token in (
            "GetModalButtonSprite", "modalButtonSprite",
            "frontEndPanelSprite", "frontEndButtonSprite",
            "uiThemeAssets.modalButton", "modalPanel;")):
            errors.append(f"Legacy modal sprite dependency: {name}")
    for retired in (
        ROOT / "Assets/Scripts/UI/MotorCityUiThemeAssets.cs",
        ROOT / "Assets/Resources/MotorCity/UI/MotorCityUiThemeAssets.asset",
    ):
        if retired.exists() or (retired.parent / (retired.name + ".meta")).exists():
            errors.append(f"Old UI theme returned: {retired.name}")
    if "button.targetGraphic is GarageReferenceGraphic" not in sources["HudUiFactory.cs"]:
        errors.append("HUD visual sweep can overwrite vector button owners")
    legacy_art = ROOT / "Assets/Art/MotorCity/UI/VilleSeppanen/RacingUI/Source"
    for obsolete in ("Skew Button 2.png", "Window 3.png",
                     "Слой 0.png", "POINTER.png"):
        if (legacy_art / obsolete).exists() or (legacy_art / (obsolete + ".meta")).exists():
            errors.append(f"Retired RacingUI texture returned: {obsolete}")

    controls_dir = ROOT / "Assets/Art/MotorCity/UI/VilleSeppanen/RacingHUD/Sprites"
    # Only these four original sprites are intentionally restored.
    for old_image in ("break.png", "handbreak.png", "throttle.png", "wheel.png"):
        if not (controls_dir / old_image).is_file() or not (controls_dir / (old_image + ".meta")).is_file():
            errors.append(f"Original driving control artwork missing: {old_image}")
    if "uiThemeAssets" in sources["TouchControlsView.cs"] or "uiThemeAssets" in sources["NavigatorView.cs"] or "uiThemeAssets" in sources["PrototypeHud.cs"]:
        errors.append("Current HUD still loads removed UI theme")
    touch_text = sources["TouchControlsView.cs"]
    if "Touch Pedal Surface" in touch_text or "Touch Wheel Icon" in touch_text:
        errors.append("User-restored pedals/steering must use original sprite artwork")
    for marker in ("touchArtAssets.touchThrottle", "touchArtAssets.touchBrake",
                   "touchArtAssets.touchHandbrake", "touchArtAssets.touchWheel",
                   "FitTouchArtSize(", "CreateTouchControlSprite("):
        if marker not in touch_text:
            errors.append(f"Original four driving control art path missing: {marker}")
    if 'Resources.Load<MotorCityTouchArtAssets>(' not in sources["PrototypeHud.cs"]:
        errors.append("Dedicated mobile driving art resource is not loaded")
    mobile_art = ROOT / "Assets/Resources/MotorCity/UI/MotorCityTouchArtAssets.asset"
    if not mobile_art.is_file():
        errors.append("Driving art reference asset is missing")
    else:
        mobile_yaml = mobile_art.read_text(encoding="utf-8-sig")
        for field in ("touchThrottle:", "touchBrake:", "touchHandbrake:", "touchWheel:"):
            if field not in mobile_yaml:
                errors.append(f"Original art reference missing: {field}")
    if "GarageReferenceGraphic.Symbol.Up" not in sources["NavigatorView.cs"]:
        errors.append("Minimap must keep its vector heading icon")
    if "vectorArt ? Color.clear : color" not in sources["HudVisualPolish.cs"]:
        errors.append("Touch raycast targets must remain transparent")

    # Builder coordinates match the final desktop layout; visual polish
    # should not re-assign these exact RectTransforms on every resize.
    geometry_owners = {
        "Speedometer": "DrivingHudView.cs",
        "Activity Status": "DrivingHudView.cs",
        "Minimap Target Label": "NavigatorView.cs",
    }
    for name, owner in geometry_owners.items():
        if re.search(r'FindRect\s*\(\s*"' + re.escape(name) + r'"', polish):
            errors.append(f"Polish reclaims builder-owned geometry: {name}")
        if f'"{name}"' not in (UI / owner).read_text(encoding="utf-8-sig"):
            errors.append(f"Expected geometry owner is missing {name}: {owner}")

    # Final Phase 7 ownership: builders own root HUD cards and result/text
    # geometry; the persistent visual pass must not resize them.
    final_owners = {
        "Player Card": "DrivingHudView.cs",
        "Character Card": "CharacterMissionCardView.cs",
        "Active Objective": "DrivingHudView.cs",
        "Status Text": "DrivingHudView.cs",
        "Result Details": "ActivityResultView.cs",
        "Result Reward": "ActivityResultView.cs",
        "Result Reward Icon": "ActivityResultView.cs",
    }
    for name, owner in final_owners.items():
        owner_src = (UI / owner).read_text(encoding="utf-8-sig")
        if f'"{name}"' not in owner_src:
            errors.append(f"Missing final geometry owner: {name} in {owner}")
        if name != "Result Reward" and re.search(
            r'FindRect\s*\(\s*"' + re.escape(name) + r'"', polish
        ):
            errors.append(f"Builder-owned HUD geometry reclaimed by polish: {name}")
    if "ApplyDesktopOrTouchComposition(" in polish:
        errors.append("HUD polish must not own player/character card root geometry")
    if "resultReward.anchoredPosition =" in polish or "resultReward.sizeDelta =" in polish:
        errors.append("Result reward geometry must be owned by ActivityResultView")

    # Result footer must fit even when mastery, secondary progress and
    # next-goal lines all appear. Coordinates are builder-local units.
    # Button row uses bottom pivot, so its top edge is -height+24+50.
    result_panel_height = 520
    actions_bottom = 24
    actions_height = 50
    next_goal_top = -390
    next_goal_height = 44
    footer_gap = (-result_panel_height + actions_bottom + actions_height) - (
        next_goal_top - next_goal_height
    )
    if footer_gap > -12:
        errors.append("Result footer has insufficient clearance between next goal and actions")
    result_view = (UI / "ActivityResultView.cs").read_text(encoding="utf-8-sig")
    for marker in ("new Vector2(650f, 520f)", "new Vector2(500f, 50f)", "new Vector2(0f, 24f)"):
        if marker not in result_view:
            errors.append(f"Result footer geometry contract missing: {marker}")
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
