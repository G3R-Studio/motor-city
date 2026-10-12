#!/usr/bin/env python3
"""UX-4 read-only detail pages: structural source regression, not Unity QA."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parent.parent

def read(path):
    return (ROOT / path).read_text(encoding="utf-8")

hud = read("Assets/Scripts/UI/PrototypeHud.cs")
details = read("Assets/Scripts/UI/PrototypeHudPlayerExperienceDetails.cs")
loc = read("Assets/Scripts/Localization/MotorCityLocalization.cs")
club = read("Assets/Scripts/Gameplay/ClubSystem.cs")
risk = read("Assets/Scripts/Gameplay/CityRiskSystem.cs")
pixie = read("Assets/Scripts/Gameplay/PixieCompanionSystem.cs")
garage = read("Assets/Scripts/Gameplay/GarageUpgradeSystem.cs")
achievements = read("Assets/Scripts/Gameplay/AchievementSystem.cs")
collection = read("Assets/Scripts/Gameplay/CollectionProgressionSystem.cs")
night = read("Assets/Scripts/Gameplay/UndergroundSceneSystem.cs")
legends = read("Assets/Scripts/Gameplay/CityLegendSystem.cs")

sections = [
    "Club", "Inspector", "Pixie", "Garage", "Achievements", "NightClub"
]
checks = {
    "all six existing pages are connected": all(
        ("PlayerExperience" + section + "Line => BuildPlayerExperience" +
         section + "Detail();") in hud and
        ("string BuildPlayerExperience" + section + "Detail(") in details
        for section in sections),
    "one read-only owner": "partial class PrototypeHud" in details,
    "club real focus and claimed weekly rewards":
        "CatalogCountedActivities" in club and
        "CatalogWeeklyRewardClaimed" in details and
        "CatalogWeeklyCreditsReward" in details,
    "risk from real attention and thresholds":
        "CatalogObservationStart" in risk and "CatalogObservationEnd" in risk
        and "CatalogAttentionMax" in details,
    "Pixie progression and only unlocked abilities":
        "CatalogDailyProgress" in pixie and
        "CatalogDailyClaimed" in details and
        "pixie.Level < requirement" in details,
    "garage shows saved roster and upgrade pricing":
        "CatalogVehicleUnlocked" in garage and
        "CatalogVehicleMasteryLevel" in details and
        "GetUpgradePrice(i)" in details and
        "CatalogVehicleSupporterExclusive(i) && !unlocked" in details,
    "achievements and collection are source-owned":
        "GetAchievementCondition" in achievements and
        "GetAchievementReward" in details and
        "CatalogClaimedTiers" in collection and
        "CatalogNextCreditsReward" in details,
    "night club and legends have real access":
        "CatalogIsNight" in night and
        "CatalogRequiredCred" in details and
        "CatalogCurrentRequirements" in legends and
        "AllLegendsDefeated" in details,
    "no gameplay mutation, purchase or save writes": all(
        token not in details for token in
        ["MotorCitySaveService", ".AddCredits(", ".AddReputation(",
         ".AddXp(", ".TryBegin(", ".Buy(", ".Purchase(", ".SetInt("]),
    "current page still owns Back": "BuildPlayerExperienceDetail(" in
        read("Assets/Scripts/UI/MotorCityFrontEndExperienceSections.cs")
        and "ShowPlayerExperienceHome" in
        read("Assets/Scripts/UI/MotorCityFrontEndExperienceSections.cs")
}
keys = set(re.findall(r'\{\s*"([^"]+)"\s*,\s*E\(', loc))
refs = set(re.findall(
    r'MotorCityLocalization\.(?:Text|Format)\(\s*"([^"]+)"', details))
missing = sorted(refs - keys)
checks["all directly referenced strings localized"] = not missing

for label, passed in checks.items():
    print(("[PASS] " if passed else "[FAIL] ") + label)
if missing:
    print("Missing localization keys:", ", ".join(missing))
if not all(checks.values()):
    raise SystemExit(1)
print(f"UX-4 detail source checks: {len(checks)}/{len(checks)} PASS")
