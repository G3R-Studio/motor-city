#!/usr/bin/env python3
"""Source regression for the shared Motor City gradient neon UI style.

Unity Game view is still required to judge the perceived glow and contrast.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Assets/Scripts/UI"
GRAPHIC = (ROOT / "GarageReferenceGraphic.cs").read_text(encoding="utf-8")
HUD = (ROOT / "HudUiFactory.cs").read_text(encoding="utf-8")
GARAGE = (ROOT / "GarageReferenceLayout.cs").read_text(encoding="utf-8")
BUTTONS = (ROOT / "MotorCityButtonVisuals.cs").read_text(encoding="utf-8")
FRONT = (ROOT / "MotorCityFrontEndFlow.cs").read_text(encoding="utf-8")
NAV = (ROOT / "NavigatorView.cs").read_text(encoding="utf-8")

checks = {
    "cyan matches loading": "new(0.06f, 0.85f, 1f, 1f)" in GRAPHIC,
    "violet matches loading": "new(0.62f, 0.40f, 1f, 1f)" in GRAPHIC,
    "gradient depends on horizontal position":
        "(x - bounds.xMin)" in GRAPHIC and "bounds.width" in GRAPHIC,
    "transparent outer halo": "NeonBand(r, radius, spread" in GRAPHIC,
    "soft core": "NeonBand(r, radius, 0.2f, -1.1f" in GRAPHIC,
    "minimap circular neon": "NeonCircleBand(x, y, radius" in GRAPHIC,
    "slanted navigation neon": "NeonEdge(a, b, bounds" in GRAPHIC,
    "HUD owns shared surfaces":
        "graphic.symbol = GarageReferenceGraphic.Symbol.Surface;" in HUD,
    "garage owns shared surfaces": "graphic.symbol = symbol;" in GARAGE,
    "modal buttons own shared surfaces":
        "vector.symbol = GarageReferenceGraphic.Symbol.Surface;" in BUTTONS,
    "front-end owns shared surfaces":
        "graphic.symbol = GarageReferenceGraphic.Symbol.Surface;" in FRONT,
    "navigator destination card no flat outline":
        "legacy.enabled = false;" in NAV and
        "ApplyReferenceHudSurface(card);" in NAV,
    "no extra raycast overlay": "raycastTarget =" not in GRAPHIC,
    "no original flat lilac border":
        "new Color(.65f,.58f,1f,.9f)" not in GRAPHIC,
}

failures = [name for name, passed in checks.items() if not passed]
if failures:
    print("FAIL: Neon UI style source contract")
    for name in failures:
        print(" - " + name)
    raise SystemExit(1)

print("PASS: unified gradient neon UI surface, buttons, garage, "
      "HUD, front end, navigator, minimap; no extra click targets.")
