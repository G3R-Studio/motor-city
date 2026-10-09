#!/usr/bin/env python3
"""Phase 6 lifecycle contracts (read-only source validation)."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / "Assets" / "Scripts"
REPORT = ROOT / "Temp" / "MotorCityAudit" / "phase6-lifecycle.json"

BOOTSTRAP = "Bootstrap/MotorCityBootstrap.cs"
PERSISTENT = {
    "UI/MotorCityUiEventSystem.cs": "UI event system",
    "Input/MotorCityVirtualInputRuntime.cs": "virtual input",
    "Audio/MotorCitySfxRuntime.cs": "sound effects",
    "Audio/MotorCityMusicRuntime.cs": "music",
    "Platform/YandexPlatformService.cs": "platform",
}
CHECKS = {
    "reset_hook": r"RuntimeInitializeLoadType\.SubsystemRegistration",
    "startup_hook": r"RuntimeInitializeLoadType\.AfterSceneLoad",
    "scene_hook": r"SceneManager\.sceneLoaded\s*\+=",
    "scene_dedup": r"SceneManager\.sceneLoaded\s*-=",
    "scene_guard": r"scene\.name\s*!=\s*\"Prototype\"",
    "platform_guard": r"platformBootstrapPending",
    "gameplay_guard": r"gameplayBuildRequested",
    "gameplay_started_guard": r"gameplayBuildStarted",
    "platform_ready": r"platformBootstrapReady",
    "front_end": r"MotorCityFrontEndFlow",
}

def main() -> int:
    errors = []
    data = {}
    for rel in [BOOTSTRAP, *PERSISTENT]:
        path = SCRIPTS / rel
        if not path.is_file():
            errors.append(f"Missing lifecycle owner: {rel}")
            continue
        data[rel] = path.read_text(encoding="utf-8-sig")

    bootstrap = data.get(BOOTSTRAP, "")
    found = {name: bool(re.search(pattern, bootstrap))
             for name, pattern in CHECKS.items()}
    for name, present in found.items():
        if not present:
            errors.append(f"Bootstrap invariant absent: {name}")
    # Phase 6 stage extraction must preserve order: runtime world first,
    # player rig next, gameplay systems after that.
    stage_calls = [
        "InitializeCoreAndWorld();",
        "InitializePlayerVehicle(",
        'new("Gameplay Systems")',
    ]
    offsets = [bootstrap.find(token, bootstrap.find("private static void TryBuildPrototype("))
               for token in stage_calls]
    if any(offset < 0 for offset in offsets) or offsets != sorted(offsets):
        errors.append("Core/world, player and gameplay systems stage order changed")
    for signature in ("private static void InitializeCoreAndWorld()",
                      "private static ArcadeCarController InitializePlayerVehicle("):
        if signature not in bootstrap:
            errors.append(f"Missing Phase 6 extracted stage: {signature}")
    # A sceneLoaded subscription without a matching unsubscription risks
    # duplicate callbacks on domain reload or another bootstrap installation.
    if len(re.findall(CHECKS["scene_hook"], bootstrap)) != len(re.findall(CHECKS["scene_dedup"], bootstrap)):
        errors.append("sceneLoaded add/remove counts differ")

    persistent = {}
    for rel, purpose in PERSISTENT.items():
        source = data.get(rel, "")
        present = "DontDestroyOnLoad(" in source
        persistent[rel] = {"purpose": purpose, "persistent_host": present}
        if not present:
            errors.append(f"Persistent host protection absent: {rel}")

    # Inventory only. Many event handlers are lambdas or static events;
    # simplistic +/- counting would produce false cleanup claims.
    event_inventory = []
    for file in sorted(SCRIPTS.rglob("*.cs")):
        src = file.read_text(encoding="utf-8-sig", errors="replace")
        subscriptions = len(re.findall(r"(?<![+])\+=\s*(?![=])", src))
        unsubscriptions = len(re.findall(r"(?<![-])-=\s*(?![=])", src))
        if subscriptions or unsubscriptions:
            event_inventory.append({
                "path": file.relative_to(ROOT).as_posix(),
                "plus_assignments": subscriptions,
                "minus_assignments": unsubscriptions,
                "manual_review_required": True,
            })

    report = {
        "phase": 6,
        "bootstrap_invariants": found,
        "persistent_hosts": persistent,
        "event_assignment_inventory": event_inventory,
        "event_inventory_note": "Operators are only candidate locations, not proof of event leaks.",
        "safe_to_restructure_without_scene_tests": False,
        "errors": errors,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Phase 6 lifecycle audit: {len(found)} bootstrap invariants, "
          f"{len(persistent)} persistent hosts, {len(event_inventory)} event candidate files.")
    for error in errors:
        print("ERROR:", error)
    return 1 if errors else 0

if __name__ == "__main__":
    raise SystemExit(main())
