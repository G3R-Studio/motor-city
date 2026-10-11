#!/usr/bin/env python3
"""Enforce separate Turbo navigator and Pixie companion identities.

Source-only check: gameplay/save compatibility still needs Unity Editor QA.
"""
from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "Assets" / "Scripts"
GAME = SCRIPTS / "Gameplay"
LOC = (SCRIPTS / "Localization/MotorCityLocalization.cs").read_text(encoding="utf-8-sig")
SOURCE = GAME / "PixieCompanionSystem.cs"
META = GAME / "PixieCompanionSystem.cs.meta"
BOOT = (SCRIPTS / "Bootstrap/MotorCityBootstrap.cs").read_text(encoding="utf-8-sig")
STORY = (GAME / "StoryMissionSystem.cs").read_text(encoding="utf-8-sig")
SEASON = (GAME / "SeasonSystem.cs").read_text(encoding="utf-8-sig")
FRONT = (SCRIPTS / "UI/MotorCityFrontEndFlow.cs").read_text(encoding="utf-8-sig")
ERRORS: list[str] = []


def require(condition: bool, message: str) -> None:
    if not condition:
        ERRORS.append(message)


def source(path: Path) -> str:
    if not path.is_file():
        ERRORS.append(f"Missing: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8-sig")


pixie = source(SOURCE)
meta = source(META)
vehicle = source(SCRIPTS / "Vehicle/ArcadeCarController.cs")
ux = source(SCRIPTS / "UI/MotorCityFrontEndExperienceSections.cs")
hud = source(SCRIPTS / "UI/PrototypeHud.cs")
navigator = source(GAME / "AdventureDirector.cs")
require(not (GAME / "TurboPetSystem.cs").exists(), "Old TurboPetSystem class/file still exists")
require(not (GAME / "TurboPetSystem.cs.meta").exists(), "Old TurboPetSystem .meta still exists")
require("class PixieCompanionSystem" in pixie, "Pixie component must match .cs filename")
require("guid: 0e0e7664bdba4a148995109992020ed5" in meta,
        "Pixie script must keep the original Unity MonoScript GUID")
require("AddComponent<PixieCompanionSystem>()" in BOOT,
        "Bootstrap must install Pixie component, not Turbo pet")
require("GetComponent<PixieCompanionSystem>()" in source(GAME / "AdminDebugPanel.cs"),
        "Debug QA must resolve Pixie component")
require("FindAnyObjectByType<PixieCompanionSystem>()" in source(GAME / "CosmeticStoreSystem.cs"),
        "Supporter Pixie skin must resolve Pixie component")

# Preserve existing saves verbatim. This is legacy persistence, NOT character lore.
for key in (
    "MotorCity.Turbo.Level", "MotorCity.Turbo.Xp",
    "MotorCity.Turbo.LastDay", "MotorCity.Turbo.Daily.Day",
    "MotorCity.Turbo.Daily.Progress", "MotorCity.Turbo.Daily.Claimed",
):
    require(f'"{key}"' in pixie, f"Existing Pixie save key changed: {key}")
require('story.character.turbo' in STORY and
        '{ "story.character.turbo", E("ТУРБО", "TURBO") }' in LOC,
        "Turbo the navigator must remain named TURBO (not PIXIE) in story and localization")
require('"onboarding.meet_turbo"' in LOC and "Я Турбо." in LOC,
        "Turbo narrator/onboarding identity lost")
require("CurrentPixieXpReward" in STORY and "CurrentPixieXpReward" in SEASON,
        "Mission and season XP rewards must explicitly name Pixie")
require("pixie?.AddXp(" in STORY and "pixie?.AddXp(" in SEASON,
        "Narrator mission XP should still be awarded to Pixie")
require("ActivatePixieBoost(" in vehicle and "ActivatePixieBoost(" in pixie,
        "Pixie driving boost contract broke")
require('{ "pixie.title", E("ПИКСИ", "PIXIE") }' in LOC and
        '{ "ux.pixie.title", E("ПИКСИ", "PIXIE") }' in LOC,
        "Pixie HUD and UX titles must explicitly show PIXIE in RU/EN")
require('MotorCityLocalization.Text("ux.pixie.title")' in FRONT,
        "Player Experience must show Pixie, not Turbo")
require('"ux.pixie.hint"' in LOC and "Пикси получает опыт" in LOC,
        "Pixie UX copy absent")
require('"source.turbo_daily"' in navigator,
        "Stable pre-existing mission source ID must be retained for compatibility")

for path in SCRIPTS.rglob("*.cs"):
    s = path.read_text(encoding="utf-8-sig")
    for token in ("TurboPetSystem", "CurrentTurboXpReward", "TurboXpReward", "SystemIcon.TurboXp", '"ux.turbo.', '"turbo.title"'):
        if token in s:
            ERRORS.append(f"Old Pixie identity {token} in {path.relative_to(ROOT)}")
require("XP ТУРБО" not in LOC and "TURBO XP" not in LOC and
        'E("НАГРАДА +{0:N0} КРЕДИТОВ • +{1:N0} РЕПУТАЦИИ • +{2} XP ПИКСИ"' in LOC,
        "User-facing season XP must belong to Pixie, not Turbo")

if ERRORS:
    print("Character canon check FAILED:")
    for error in ERRORS:
        print(" -", error)
    sys.exit(1)
print("Character canon source check PASS: Turbo = navigator, Pixie = companion.")
print("Legacy save keys and Unity MonoScript GUID preserved; Unity Editor QA still required.")
