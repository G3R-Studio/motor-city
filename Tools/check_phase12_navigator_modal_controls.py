#!/usr/bin/env python3
"""Guard navigator modal controls layout, actions and WebGL-safe arrow icons.

Source/geometry regression only. Real Unity Editor and Pages visual QA pending.
"""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
NAV = ROOT / "Assets/Scripts/UI/NavigatorView.cs"
HUD = ROOT / "Assets/Scripts/UI/PrototypeHud.cs"
TOUCH = ROOT / "Assets/Scripts/UI/TouchControlsView.cs"
GRAPHIC = ROOT / "Assets/Scripts/UI/GarageReferenceGraphic.cs"
ERRORS: list[str] = []


def ensure(condition: bool, detail: str) -> None:
    if not condition:
        ERRORS.append(detail)


def compact(text: str) -> str:
    return re.sub(r"\s+", "", text)


def method(source: str, name: str) -> str:
    match = re.search(rf"\b{re.escape(name)}\s*\([^)]*\)\s*\{{", source)
    if match is None:
        ERRORS.append(f"Method not found: {name}")
        return ""
    start = match.end() - 1
    depth = 0
    for i in range(start, len(source)):
        if source[i] == "{":
            depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0:
                return source[start : i + 1]
    ERRORS.append(f"Unclosed method: {name}")
    return ""


def main() -> int:
    nav = NAV.read_text(encoding="utf-8-sig")
    hud = HUD.read_text(encoding="utf-8-sig")
    touch = TOUCH.read_text(encoding="utf-8-sig")
    graphic = GRAPHIC.read_text(encoding="utf-8-sig")
    menu = compact(method(nav, "BuildNavigatorMenu"))
    buttons = compact(method(nav, "BuildNavigatorTouchControls"))
    arrow = compact(method(nav, "AddNavigatorArrowGraphic"))
    handler = compact(method(nav, "HandleNavigatorMenu"))

    ensure("privateRectTransformnavigatorMenuPanelRect;" in compact(hud),
           "Navigator panel reference must be stored with its modal")
    ensure("newVector2(640f,420f)" in menu,
           "Navigator modal panel must reserve room for its controls")
    ensure("navigatorMenuPanelRect=panel;" in menu,
           "Navigator modal panel reference must be captured")
    ensure('newVector2(0f,28f)' in menu and
           'newVector2(478f,140f)' in menu,
           "Destination card size/position must be stable")
    ensure("newVector2(0f,98f)" in menu,
           "Distance row must not overlap bottom actions")
    ensure("navigatorMenuPanelRect!=null?navigatorMenuPanelRect:canvas" in buttons,
           "Navigator controls must parent to modal, not screen-bottom canvas")
    ensure("root.anchorMin=newVector2(0.5f,0.5f)" in buttons and
           "root.anchorMax=newVector2(0.5f,0.5f)" in buttons,
           "Navigator controls must anchor to modal center")
    ensure("root.anchoredPosition=Vector2.zero" in buttons,
           "Navigator buttons must stay aligned with modal center")
    ensure("root.sizeDelta=newVector2(640f,420f)" in buttons,
           "Navigator controls should share modal dimensions")

    for name, action, pos, size in [
        ("Prev", "PreviousVehicle", "-282f,238f", "54f,62f"),
        ("Next", "NextVehicle", "282f,238f", "54f,62f"),
        ("Action", "Retry", "-99f,42f", "182f,50f"),
        ("Close", "ToggleNavigator", "99f,42f", "182f,50f"),
    ]:
        # compact() also removes whitespace within quoted UI object names.
        literal = f'"NavigatorTouchControls{name}"'
        ensure(literal in buttons, f"Missing modal control {name}")
        ensure(f"MotorCityInputAction.{action}" in buttons,
               f"Wrong action for Navigator {name}")
        ensure(f"newVector2({pos})" in buttons and
               f"newVector2({size})" in buttons,
               f"Navigator {name} geometry differs from modal layout")

    # Numerical clearance checks in authored Canvas units, not screenshots.
    panel_width, panel_height = 640, 420
    card_width, card_y = 478, 28
    card_bottom = card_y - 140 / 2
    left_center, right_center, arrow_width = -282, 282, 54
    ensure(left_center - arrow_width / 2 >= -panel_width / 2 and
           right_center + arrow_width / 2 <= panel_width / 2,
           "Arrow hitboxes extend outside navigator panel")
    ensure(left_center + arrow_width / 2 < -card_width / 2 and
           right_center - arrow_width / 2 > card_width / 2,
           "Arrow buttons overlap destination card")
    # Modal root anchored center, while child buttons anchor to bottom edge.
    button_top = -panel_height / 2 + 42 + 50 / 2
    distance_bottom = -panel_height / 2 + 98 - 28 / 2
    ensure(button_top < distance_bottom and
           distance_bottom < card_bottom,
           "Action buttons, distance line and destination card overlap")

    ensure('"◀"' not in buttons and '"▶"' not in buttons,
           "Do not use font-dependent Unicode triangle glyphs in WebGL")
    ensure("string.Empty" in buttons, "Arrow buttons must not render glyph text")
    ensure("GarageReferenceGraphic.Symbol.Left" in buttons and
           "GarageReferenceGraphic.Symbol.Right" in buttons,
           "Missing independent vector left and right icons")
    ensure("typeof(GarageReferenceGraphic)" in arrow and
           'button.Find("Label")' not in arrow and
           "icon.raycastTarget=false" in arrow,
           "Vector arrow icons must render without blocking click/touch")
    # The graphic now renders curated Tabler sprites rather than
    # procedural switch-case triangles. Assert both mappings AND the
    # sprite mesh path so neither arrow silently disappears in WebGL.
    visual = compact(graphic)
    ensure('Symbol.Left=>"arrow-left"' in visual and
           'Symbol.Right=>"arrow-right"' in visual and
           "MotorCityIconLibrary.Get(TablerIconName)" in visual and
           "PopulateTablerMesh(vh,icon)" in visual,
           "Navigator left/right arrows must use rendered Tabler sprites")
    icon_dir = ROOT / "Assets/Resources/MotorCity/UI/Icons/Tabler"
    for name in ("arrow-left", "arrow-right"):
        ensure((icon_dir / (name + ".png")).is_file() and
               (icon_dir / (name + ".png.meta")).is_file(),
               f"Missing imported WebGL navigator Tabler sprite: {name}")

    for token in (
        "MotorCityInput.PreviousVehiclePressed",
        "MotorCityInput.NextVehiclePressed",
        "MotorCityInput.RetryPressed",
        "MotorCityInput.ToggleNavigatorPressed",
    ):
        ensure(token in handler, f"Navigator click action not handled: {token}")
    ensure("BuildNavigatorTouchControls(canvas)" in compact(touch),
           "Touch modal must construct navigator controls")
    ensure("navigatorTouchControlsRoot,navigatorMenuOpen" in compact(touch),
           "Navigator controls must follow modal open/close state")

    if ERRORS:
        print("Phase 12 navigator modal layout contract FAILED:")
        for error in ERRORS:
            print(" -", error)
        return 1

    print("Phase 12 navigator modal controls PASS:")
    print(" Vector arrows on both sides of card; Select and Close within")
    print(" the modal beneath distance row; no Unicode font dependency.")
    print(" Desktop/mobile Unity and GitHub Pages visual QA still pending.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
