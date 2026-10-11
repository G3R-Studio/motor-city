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
        "bounds.center.x,\n                bounds.max.y + 0.01f,\n                bounds.center.z" in SOURCE
    ),
    "skin-specific calibrated offsets in runtime": (
        "StandardRoofHeightOffset =\n            0.05f;" in SOURCE and
        "ExclusiveRoofHeightOffset =\n            0.10f;" in SOURCE and
        "roofCenter.y += ActiveSkinRoofHeightOffset;" in SOURCE and
        "ActiveSkinRoofHeightOffset + editorRoofHeightAdjustment" in SOURCE
    ),
    "Editor reset returns to each skin baseline": (
        "public void ResetEditorRoofHeightOffset()" in SOURCE and
        "totalOffsetMeters - ActiveSkinRoofHeightOffset" in SOURCE and
        "pixie.ResetEditorRoofHeightOffset();" in
        (ROOT / "Assets/Scripts/Gameplay/AdminDebugPanel.cs").read_text(encoding="utf-8-sig") and
        "editorRoofHeightAdjustment = 0f;\n#endif\n\n            if (car != null)" in SOURCE
    ),
    "no hover or bobbing": (
        "hoverPhase" not in SOURCE and
        "Mathf.Sin(" not in SOURCE
    ),
    "central roof collider proxies": (
        "BodyCollider_" in SOURCE and
        "GetComponentsInChildren<BoxCollider>" in SOURCE and
        "surfaceY > bestRoofY" in SOURCE and
        "Physics.RaycastAll(" not in SOURCE
    ),
    "actual feet aligned to roof": (
        "AlignPixieFeetToRoof();" in SOURCE and
        "HumanBodyBones.LeftFoot" in SOURCE and
        "HumanBodyBones.RightFoot" in SOURCE and
        "SoleBelowAnkle" in SOURCE
    ),
    "Editor skin preview without granting purchase": (
        'public void SetEditorRoofHeightOffset(float totalOffsetMeters)' in SOURCE and
        "editorRoofHeightAdjustment" in SOURCE and
        "EditorRoofHeightOffset" in
          (ROOT / "Assets/Scripts/Gameplay/AdminDebugPanel.cs").read_text(encoding="utf-8-sig") and
        'pixie?.SetSupporterPackSkin(false);' in
          (ROOT / "Assets/Scripts/Gameplay/AdminDebugPanel.cs").read_text(encoding="utf-8-sig") and
        'pixie?.SetSupporterPackSkin(true);' in
          (ROOT / "Assets/Scripts/Gameplay/AdminDebugPanel.cs").read_text(encoding="utf-8-sig")
    ),
    "exact vehicle attachment": (
        "visualRoot.transform.position =\n                car.transform.TransformPoint(visualAnchorLocal);" in SOURCE and
        "visualRoot.transform.rotation =\n                car.transform.rotation;" in SOURCE and
        "followBlend" not in SOURCE and
        "rotationBlend" not in SOURCE
    ),
    "body bound exclusion": "ResolveCarLocalBounds()" in SOURCE and
        'car.transform.Find(\n                    "MotorCityVehicleVisual_Runtime")' in SOURCE,
    "normal three poses": all('"' + state + '"' in SOURCE for state in (
        "Pixie Idle", "Pixie Roof Pose", "Pixie Idle Alt"
    )),
    "no walking/running map": '"Pixie Follow"' not in SOURCE and
        'mappedState == "running"' not in SOURCE and
        '"running"' not in SOURCE,
    "exclusive run disabled": '"param_idletorunning",\n                    false' in SOURCE,
    "exclusive avoids T-pose-like default state": (
        '"Pixie Idle Alt" => "default"' not in SOURCE and
        '"Pixie Roof Pose" or "Pixie Idle Alt" or' in SOURCE and
        '"Pixie Idle" => "idle"' in SOURCE and
        'ambientPoseIndex = useSupporterPixieSkin' in SOURCE and
        '"param_idletodefault",\n                    false' in SOURCE and
        '"param_idletowalk",\n                    false' in SOURCE
    ),
    "Editor names current clip for diagnosis": (
        'GetCurrentAnimatorClipInfo(0)' in SOURCE and
        "pixie.EditorCurrentAnimationClip" in
        (ROOT / "Assets/Scripts/Gameplay/AdminDebugPanel.cs").read_text(encoding="utf-8-sig")
    ),
    "random poses": (
        "Random.Range(6f, 12f)" in SOURCE and
        "ambientPoseIndex + UnityEngine.Random.Range(1, 3)" in SOURCE
    ),
    "rare separate reactions": (
        "Random.Range(15f, 25f)" in SOURCE and
        'PlayPixieReaction("Pixie Clap", 1.8f)' in SOURCE and
        'PlayPixieReaction("Pixie Victory", 2.0f)' in SOURCE
    ),
    "rare supporter playful jump without boost": (
        'PlayPixieReaction("Pixie Playful Jump", 1.25f)' in SOURCE and
        '"Pixie Boost" or "Pixie Playful Jump" => "jump"' in SOURCE
    ),
    "reactions take priority": (
        "if (animationReactionTimer > 0f)" in SOURCE and
        "animationReactionTimer - Time.unscaledDeltaTime" in SOURCE
    ),
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
