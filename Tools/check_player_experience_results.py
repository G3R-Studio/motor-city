#!/usr/bin/env python3
"""UX-5 result source gate: no Unity compilation or Play Mode substitute."""
from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent

def read(path):
    return (root / path).read_text(encoding="utf-8")

manager = read("Assets/Scripts/Gameplay/ActivityManager.cs")
view = read("Assets/Scripts/UI/ActivityResultView.cs")
changes = read("Assets/Scripts/UI/PrototypeHudResultChanges.cs")
journal = read("Assets/Scripts/UI/PrototypeHudPlayerExperienceJournal.cs")
loc = read("Assets/Scripts/Localization/MotorCityLocalization.cs")
checks = {
    "one transaction per result": "if (HasResult)" in manager[
        manager.index("public void ShowResult("):
        manager.index("public void ReportCompletion(")],
    "dismiss idempotent": "if (!HasResult)" in manager[
        manager.index("public void DismissResult("):],
    "snapshot before owner completion":
        manager.index("ActivityResultShown?.Invoke(") <
        manager.index("ActivityCompleted?.Invoke(", manager.index("public void ShowResult("))
        and "CaptureResultChangesBefore(success)" in journal,
    "sequence-bound immutable delta": "resultChangesBefore.Sequence != sequence" in changes
        and "resultChangesRenderedSequence == sequence" in changes,
    "no theoretical balance": "after.Credits - before.Credits" in changes
        and "after.Reputation - before.Reputation" in changes,
    "records use result owner": "ResultIsNewRecord" in view,
    "seven secondary progress owners": "PlayerExperienceJournalProgressSignature(i)" in changes
        and "for (int i = 0; i < journalBeforeResult.Length; i++)" in changes,
    "mastery, discipline, Pixie, inspector and achievements":
        all(x in changes for x in (
            "ResultMasteryXp", "CatalogDisciplineReputation",
            "PixieLevel", "Attention", "Achievements", "CollectionScore")),
    "failures have no fabricated changes":
        "!activityManager.ResultSuccess" in changes,
    "scroll viewport and mask": "typeof(RectMask2D)" in changes
        and "ScrollRect.MovementType.Clamped" in changes
        and "resultChangesScroll.content = resultChangesContent" in changes,
    "full text wraps": "horizontalOverflow = HorizontalWrapMode.Wrap" in changes
        and "resizeTextForBestFit = false" in changes,
    "existing Retry and Continue stay wired":
        'MotorCityInputAction.Retry' in view
        and 'MotorCityInputAction.Cancel' in view
        and 'IsReplayableResult(' in view,
    "no result UI payout or save changes": not any(x in changes
        for x in ("MotorCitySaveService", ".AddCredits(",
                  ".AddReputation(", ".AddXp(", ".TryBegin(",
                  ".Purchase(", ".SetInt(")),
}
entries = set(re.findall(r'\{\s*"([^"]+)"\s*,\s*E\(', loc))
literal = set(re.findall(
    r'MotorCityLocalization\.(?:Text|Format)\(\s*"([^"]+)"', changes))
missing = sorted(literal - entries)
checks["all literal RU/EN keys exist"] = not missing

for title, passed in checks.items():
    print(("[PASS] " if passed else "[FAIL] ") + title)
if missing:
    print("Missing localization:", ", ".join(missing))
if not all(checks.values()):
    raise SystemExit(1)
print(f"UX-5 result source checks: {len(checks)}/{len(checks)} PASS")
