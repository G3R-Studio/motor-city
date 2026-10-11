#!/usr/bin/env python3
"""Source-only guard for UX-2 catalog; Unity Play Mode is still a separate gate."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parent.parent


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


hud = read("Assets/Scripts/UI/PrototypeHud.cs")
loc = read("Assets/Scripts/Localization/MotorCityLocalization.cs")
sections = read("Assets/Scripts/UI/MotorCityFrontEndExperienceSections.cs")
activity = read("Assets/Scripts/Gameplay/ActivityManager.cs")

checks = {
    "one read-only catalog owner": "return BuildPlayerExperienceActivityCatalog();" in hud,
    "common labeled cards": "AppendPlayerExperienceCard(" in hud,
    "races": "AppendPlayerExperienceRaceCards(sb);" in hud,
    "drift challenges and zones": "AppendPlayerExperienceDriftCards(sb);" in hud,
    "all job types": "AppendPlayerExperienceJobCards(sb);" in hud,
    "radars, discoveries and photos": "AppendPlayerExperienceCityCards(sb);" in hud,
    "special events and nightclub": "AppendPlayerExperienceSpecialCards(sb);" in hud,
    "tutorial/story source policy": "CatalogAccessState(string id)" in activity
        and "CanBeginDuringOnboarding(id)" in activity
        and "CanBeginDuringStory(id)" in activity,
    "no catalog route buttons": "Catalog Route " not in sections
        and "SelectPlayerExperienceRoute(" not in sections,
    "profession rewards are read-only":
        "GetStartNextRewardCredits" in hud and "CatalogNextRewardCredits" in hud,
    "one-time zone rewards": "GetCatalogMedalCreditReward" in hud,
    "unlocked nighttime conditions": "CatalogIsNight" in hud
        and "HasActiveInvitation" in hud,
    "old achievement and progress owners untouched":
        "MotorCitySaveService" not in hud[
            hud.index("private string BuildPlayerExperienceActivityCatalog()"):
            hud.index("private string PlayerExperienceActiveActivityLine()")
        ],
}

# Literal localization calls must resolve to RU/EN entries.
entries = set(re.findall(r'\{\s*"([^"]+)"\s*,\s*E\(', loc))
refs = set(re.findall(
    r'MotorCityLocalization\.(?:Text|Format)\(\s*"([^"]+)"', hud))
missing = sorted(k for k in refs if k not in entries)
checks["all literal UI localization keys exist"] = not missing

for name, ok in checks.items():
    print(("[PASS] " if ok else "[FAIL] ") + name)
if missing:
    print("Missing localization keys:", ", ".join(missing))
if not all(checks.values()):
    raise SystemExit(1)
print(f"UX-2 catalog source checks: {len(checks)}/{len(checks)} passed")
