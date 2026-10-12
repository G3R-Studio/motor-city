#!/usr/bin/env python3
"""UX-6 source regression: structural checks, not Unity Play Mode/WebGL QA."""
from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent

def read(path):
    return (root / path).read_text(encoding="utf-8")

sections = read("Assets/Scripts/UI/MotorCityFrontEndExperienceSections.cs")
details = read("Assets/Scripts/UI/PrototypeHudPlayerExperienceDetails.cs")
front = read("Assets/Scripts/UI/MotorCityFrontEndFlow.cs")
modal = read("Assets/Scripts/UI/MotorCityFrontEndModalUi.cs")
loc = read("Assets/Scripts/Localization/MotorCityLocalization.cs")
result = read("Assets/Scripts/UI/PrototypeHudResultChanges.cs")
checks = {
    "nearest REP hint comes from real catalog":
        "BuildPlayerExperienceNextVehicleRepHint()" in details
        and "CatalogVehicleRequiredRep(i) - currentRep" in details
        and "activityManager.TotalReputation" in details,
    "no false supporter or missing-visual unlock promise":
        "CatalogVehicleSupporterExclusive(i)" in details
        and "CatalogVehicleAvailable(i)" in details
        and "CatalogVehicleUnlocked(i)" in details,
    "club goal reflects owner and claim state":
        "club.WeeklyTarget - club.WeeklyContribution" in details
        and "!club.CatalogWeeklyRewardClaimed" in details
        and "if (!club.HasClub)" in details,
    "no permanent HUD guidance":
        "ux6.garage.rep_hint" in details
        and "ux6.club.remaining" in details
        and "ux6.home.first_hint" in front
        and "ux6.garage.rep_hint" not in result,
    "one small session-only first-visit tip":
        "playerExperienceHintWasShown = true;" in front
        and "!playerExperienceHintWasShown" in front
        and "playerExperienceVisitHintText.gameObject.SetActive(false)" in front,
    "value stamps, not catalogs each frame":
        "CapturePlayerExperienceSourceStamp()" in sections
        and "playerExperienceSourcePollTimer < 0.75f" in sections
        and "RefreshVisiblePlayerExperienceIfChanged();" in front,
    "poll only while open":
        "!playerExperienceRoot.activeInHierarchy" in sections
        and "canvas.gameObject.activeInHierarchy" in front,
    "changed balances, vehicle and result refresh":
        all(x in sections for x in (
            "credits == observedCredits", "rep == observedReputation",
            "vehicle == observedVehicleIndex", "result == observedResultSequence")),
    "day and week rollover are source clock-based":
        "MotorCityPlatform.ServerUnixTime" in sections
        and "now / 86400L" in sections and "now / 604800L" in sections,
    "fresh snapshot on every opening":
        "SwitchPlayerExperiencePage(null, true);" in front
        and "CapturePlayerExperienceSourceStamp();" in front,
    "live update preserves existing panel and scroll":
        "RefreshPlayerExperienceSections(active)" in sections
        and "RefreshFrontEndScrollCues();" in sections
        and "scroll.StopMovement();" in sections,
    "safe-area and compact geometry preserved":
        "Screen.safeArea" in modal and "ApplyCompactFrontEndModalFrames();" in modal
        and "ConfigurePlayerExperienceActionRow(" in modal,
    "texts grow rather than shrinking":
        "Mathf.Max(minimumHeight, text.preferredHeight + 12f)" in modal
        and "HorizontalWrapMode.Wrap" in front
        and "SetPlayerExperienceTextHeight(playerExperienceVisitHintText" in modal,
    "empty and completed stages from owner":
        'MotorCityLocalization.Text("ux.sections.not_ready")' in details
        and "CatalogSeriesComplete" in details
        and "CatalogAllClaimed" in details,
    "no new payout or save writes in helpers":
        not any(x in (sections + details) for x in (
            "MotorCitySaveService", ".AddCredits(", ".AddReputation(",
            ".AddXp(", ".Purchase(", ".SetInt(", ".TryBegin(")),
}
entries = set(re.findall(r'\{\s*"([^"]+)"\s*,\s*E\(', loc))
keys = {"ux6.home.first_hint", "ux6.garage.rep_hint", "ux6.club.remaining"}
checks["all new guidance has RU/EN localization"] = keys.issubset(entries)
for label, passed in checks.items():
    print(("[PASS] " if passed else "[FAIL] ") + label)
if not all(checks.values()):
    raise SystemExit(1)
print(f"UX-6 source regression: {sum(checks.values())}/{len(checks)} PASS")
