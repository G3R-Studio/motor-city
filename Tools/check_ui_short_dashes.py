#!/usr/bin/env python3
"""Reject long dashes in Motor City's player-facing UI and C# text.

Covers authored runtime/Editor scripts, authored MotorCity UI assets and
the manually generated WebGL test title. Third-party content, licenses and
long-form project docs are intentionally out of scope.
"""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
LOCALIZATION = ASSETS / "Scripts/Localization/MotorCityLocalization.cs"
ui_extensions = {".uxml", ".uss", ".json", ".csv", ".txt", ".html", ".prefab", ".asset"}

paths = [
    *sorted((ASSETS / "Scripts").rglob("*.cs")),
    *sorted((ASSETS / "Editor").rglob("*.cs")),
    *(p for p in (ASSETS / "Resources/MotorCity").rglob("*")
      if p.is_file() and p.suffix.lower() in ui_extensions
      and "license" not in p.name.lower()),
    *(p for p in (ASSETS / "Scenes").rglob("*.unity")
      if p.is_file()),
    ROOT / "Tools/prepare_pages_webgl.py",
]
banned = ("\u2014", "\u2013", "\u2212")  # em dash, en dash, Unicode minus
failures: list[str] = []

for path in sorted(set(paths)):
    if not path.is_file():
        continue
    content = path.read_text(encoding="utf-8-sig", errors="replace")
    for index, line in enumerate(content.splitlines(), 1):
        if any(symbol in line for symbol in banned):
            failures.append(f"{path.relative_to(ROOT)}:{index}: use '-' instead of a long dash")

locale = LOCALIZATION.read_text(encoding="utf-8-sig")
expected = (
    '{ "ux.inspector.hint", E('
    '"Быстрые заезды и дрифт повышают внимание. '
    'Спокойная езда и посещение гаража помогают его снизить.", '
    '"Fast runs and drifting increase attention. '
    'Calm driving and visiting the garage can reduce it.") }'
)
if expected not in locale:
    failures.append("Inspector hint RU/EN is absent or unexpectedly changed")
for stale in ("Физических погонь пока нет.", "Physical police chases are not implemented."):
    if stale in locale:
        failures.append(f"Outdated inspector status copy: {stale}")

if failures:
    print("UX-1 short dash / inspector text gate FAILED")
    print("\n".join(" - " + item for item in failures[:80]))
    raise SystemExit(1)
print(f"UX-1 short dash / inspector text gate PASS ({len(set(paths))} text files checked)")
print("Source-only check; Unity Editor Play Mode still requires manual QA.")
