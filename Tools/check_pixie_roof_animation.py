#!/usr/bin/env python3
"""Pixie roof position and animation source regression checks (manual/editor UX-1)."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = (ROOT / "Assets/Scripts/Gameplay/PixieCompanionSystem.cs").read_text(encoding="utf-8-sig")
IMPORTER = (ROOT / "Assets/Editor/HaonByteVisualImporter.cs").read_text(encoding="utf-8-sig")
CONTROLLER = (ROOT / "Assets/Resources/MotorCity/Byte/HaonPixie.controller").read_text(encoding="utf-8-sig")
EXCLUSIVE = (ROOT / "Assets/SapphiArt/SapphiArtchan/Animation/SapphiArtchanAnimController.controller").read_text(encoding="utf-8-sig")

checks = {
    "center roof X/Z": (
        "bounds.center.x,\n                bounds.max.y + 0.12f,\n                bounds.center.z" in SOURCE
    ),
    "small stationary hover": "0.055f;" in SOURCE and "1.65f;" in SOURCE,
    "body bound exclusion": "ResolveCarLocalBounds()" in SOURCE and
        'car.transform.Find(\n                    "MotorCityVehicleVisual_Runtime")' in SOURCE,
    "normal three poses": all('"' + state + '"' in SOURCE for state in (
        "Pixie Idle", "Pixie Roof Pose", "Pixie Idle Alt"
    )),
    "no walking/running map": '"Pixie Follow"' not in SOURCE and
        'mappedState == "running"' not in SOURCE and
        '"running"' not in SOURCE,
    "exclusive run disabled": '"param_idletorunning",\n                    false' in SOURCE,
    "exclusive default pose": '"Pixie Idle Alt" => "default"' in SOURCE,
    "cheer while cruising": 'PlayPixieReaction("Pixie Clap", 1.7f);' in SOURCE,
    "gameplay cheers kept": '"Pixie Victory"' in SOURCE and
        '"Pixie Boost"' in SOURCE,
    "normal StandB authoring": 'StandB.FBX' in IMPORTER and
        '"Pixie Roof Pose"' in IMPORTER and
        'jump ?? roofPose ?? idle' in IMPORTER and
        'jump ?? follow ?? idle' not in IMPORTER,
    "normal StandB deployed": 'm_Name: Pixie Roof Pose' in CONTROLLER and
        'guid: a34c9ff020908b44caff787e4b41bea7' in CONTROLLER,
    "normal walking absent": 'm_Name: Pixie Follow' not in CONTROLLER and
        'guid: b1ca9eb47697e8e4883ec4b3782d43bf' not in CONTROLLER,
    "exclusive controller intact": "m_Name: running" in EXCLUSIVE and
        "m_Name: idle" in EXCLUSIVE and
        "m_Name: default" in EXCLUSIVE and
        "m_Name: winpose" in EXCLUSIVE,
    "no root-motion movement": "externalAnimator.applyRootMotion =" in SOURCE and
        "false;" in SOURCE,
}
failed = [name for name, ok in checks.items() if not ok]
for name, ok in checks.items():
    print(f"{'PASS' if ok else 'FAIL'} {name}")
if failed:
    raise SystemExit(f"Pixie roof source gate failed: {', '.join(failed)}")
print("Pixie roof source gate PASS. Unity Editor visual QA still required.")
