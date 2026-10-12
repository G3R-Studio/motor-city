#!/usr/bin/env python3
"""Read-only UX-3 journal source regression guard; Unity smoke is separate."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

def source(path):
    return (ROOT / path).read_text(encoding="utf-8")

journal = source("Assets/Scripts/UI/PrototypeHudPlayerExperienceJournal.cs")
hud = source("Assets/Scripts/UI/PrototypeHud.cs")
ui = source("Assets/Scripts/UI/MotorCityFrontEndExperienceSections.cs")
loc = source("Assets/Scripts/Localization/MotorCityLocalization.cs")

checks = {
    "seven UI filters": "new Button[7]" in ui
        and "SelectPlayerExperienceJournalFilter(filter)" in ui,
    "seven read-only sources": all(
        "AppendJournal" + title + "(sb)" in journal
        for title in ["Story", "Season", "Today", "Contracts",
                      "Career", "Legends", "NightClub"]),
    "one journal owner": "PlayerExperienceJournalLineForFilter(" in ui
        and "PlayerExperienceJournalLineForFilter(-1)" in hud,
    "daily and weekly reset": "CatalogSecondsUntilReset" in journal
        and "CatalogSecondsUntilWeeklyReset" in journal,
    "seasonal cycle": "season.DaysRemaining" in journal
        and "season.IsComplete" in journal,
    "automatically tracked contract": "CatalogProgressLine" in journal
        and "ux.journal.contract_passive" in journal,
    "per-task daily progress": "CatalogTaskProgress(i)" in journal
        and "CatalogTaskComplete(i)" in journal,
    "onboarding block": "SecondaryProgressionAllowed" in journal
        and "ux.journal.story_tutorial" in journal,
    "activity delta before completion": "ActivityResultShown +=" in hud
        and "CaptureJournalBeforeActivityResult" in journal,
    "delta covers all seven categories": "for (int i = 0; i < 7; i++)" in journal
        and "PlayerExperienceJournalProgressSignature(i)" in journal,
    "no reward or save writes": all(token not in journal
        for token in ["MotorCitySaveService", ".AddCredits(",
                      ".AddReputation(", ".AddXp(", ".TryBegin(",
                      "StartCoroutine("]),
    "no journal route controls": "SelectPlayerExperienceRoute" not in journal
        and "BuildRoute" not in journal,
}
entries = set(re.findall(r'\{\s*"([^"]+)"\s*,\s*E\(', loc))
references = set(re.findall(
    r'MotorCityLocalization\.(?:Text|Format)\(\s*"([^"]+)"', journal))
missing = sorted(references - entries)
checks["localized literals"] = not missing

for label, passed in checks.items():
    print(("[PASS] " if passed else "[FAIL] ") + label)
if missing:
    print("Missing localization:", ", ".join(missing))
if not all(checks.values()):
    raise SystemExit(1)
print(f"UX-3 journal source checks: {len(checks)}/{len(checks)} PASS")
