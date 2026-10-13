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
SKIN = BUTTONS
FRONT = (ROOT / "MotorCityFrontEndFlow.cs").read_text(encoding="utf-8")
NAV = (ROOT / "NavigatorView.cs").read_text(encoding="utf-8")

checks = {
    "cyan matches loading": "new(0.06f, 0.85f, 1f, 1f)" in GRAPHIC,
    "dark cyan glass": "new(0.018f, 0.100f, 0.155f, 1f)" in GRAPHIC,
    "dark indigo glass": "new(0.077f, 0.069f, 0.148f, 1f)" in GRAPHIC,
    "dark surface horizontal gradient":
        "Color.Lerp(GlassCyan, GlassViolet, t)" in GRAPHIC,
    "button state affects glass brightness instead of hue":
        "float roleLuma = color.r * 0.2126f +" in GRAPHIC
        and "roleLift" in GRAPHIC
        and "fill.r += color.r" not in GRAPHIC,
    "glass alpha follows existing panel":
        "fill.a = color.a;" in GRAPHIC,
    "panels now use nearly opaque neutral surface tint":
        "Color Panel = new(0.11f, 0.16f, 0.22f, 0.99f)" in SKIN,
    "buttons no longer use violet role RGB tints":
        "new Color(0.20f, 0.16f, 0.34f, 0.96f)" not in SKIN,
    "garage uses common neutral surface palette":
        "graphic.color = MotorCityUiSkin.Panel;" in GARAGE,
    "front-end uses common neutral surface palette":
        "graphic.color = MotorCityUiSkin.Panel;" in FRONT,
    "surface fills its mesh with gradient":
        "mesh.AddVert(r.center, SurfaceFillAt(r.center, r)" in GRAPHIC
        and "mesh.AddVert(p, SurfaceFillAt(p, r)" in GRAPHIC,
    "navigation panels use same dark gradient":
        "glassPolygon" in GRAPHIC and
        "SurfaceFillAt(local, bounds)" in GRAPHIC,
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

print("PASS: unified cyan-to-violet glow and matching dark glass "
      "backgrounds for shared UI panels/buttons, garage, HUD and navigator; "
      "no extra click targets.")
