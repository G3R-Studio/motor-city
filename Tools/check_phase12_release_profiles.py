#!/usr/bin/env python3
"""Phase 12 static release profile/runner contract audit (no Unity build)."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
PROFILES = {
    "Desktop": ("Web - Desktop - Release.asset", "1"),
    "Mobile": ("Web - Mobile - Release.asset", "4"),
}
errors = []


def require(text, expression, description):
    if not re.search(expression, text, re.MULTILINE):
        errors.append(description)


for name, (filename, texture_target) in PROFILES.items():
    path = ROOT / "Assets/Settings/Build Profiles" / filename
    if not path.is_file():
        errors.append(f"Missing {name} release profile: {path}")
        continue
    raw = path.read_text(encoding="utf-8-sig")
    require(raw, rf"^  m_Name: Web - {name} - Release$", f"{name}: profile name")
    require(raw, r"^  m_BuildTarget: 20$", f"{name}: not a WebGL profile")
    require(raw, r"^  m_OverrideGlobalSceneList: 0$", f"{name}: unexpected scene override")
    require(raw, r"^  m_HasScriptingDefines: 0$", f"{name}: unexpected custom defines")
    require(raw, r"^\s+ - line: '\|   activeInputHandler: 1'$", f"{name}: Input System New missing")
    for flag in (
        "m_Development", "m_ConnectProfiler", "m_BuildWithDeepProfilingSupport",
        "m_BuildWithCodeCoverage", "m_AllowDebugging", "m_WaitForManagedDebugger",
    ):
        require(raw, rf"^\s+{flag}: 0$", f"{name}: {flag} must be disabled")
    require(
        raw, r"^\s+m_CodeOptimization: 3$",
        f"{name}: release code optimization setting changed",
    )
    require(
        raw, rf"^\s+m_WebGLTextureSubtarget: {texture_target}$",
        f"{name}: texture subtarget differs from authored baseline",
    )

editor = ROOT / "Assets/Editor/MotorCityPhase12ReleaseBuild.cs"
wrapper = ROOT / "Tools/run_unity_phase12_release.ps1"
workflow = ROOT / ".github/workflows/motor-city-source-gates.yml"
if not editor.is_file() or not (editor.with_suffix(".cs.meta")).is_file():
    errors.append("Missing Phase 12 profile build entrypoint or Unity .meta")
else:
    source = editor.read_text(encoding="utf-8-sig")
    for fragment in (
        "BuildProfile.GetActiveBuildProfile()",
        "BuildPlayerWithProfileOptions",
        "BuildPipeline.BuildPlayer(",
        "BuildOptions.None",
        "Assets/Settings/Build Profiles/Web - ",
        "MotorCityPhase12ReleaseBuild",
    ):
        if fragment not in source:
            errors.append(f"Phase 12 build entrypoint missing: {fragment}")
if not wrapper.is_file():
    errors.append("Missing Phase 12 release runner")
else:
    source = wrapper.read_text(encoding="utf-8-sig")
    for fragment in ("-activeBuildProfile", "MOTORCITY_PHASE12_PROFILE", "Builds/Phase12",
                     "MotorCity.EditorTools.MotorCityPhase12ReleaseBuild.Build"):
        if fragment not in source:
            errors.append(f"Phase 12 PowerShell runner missing: {fragment}")
ci = workflow.read_text(encoding="utf-8-sig")
for fragment in (
    "Tools/check_phase12_release_profiles.py",
    "run_unity_phase12_release.ps1 -Profile Desktop",
    "run_unity_phase12_release.ps1 -Profile Mobile",
    "[phase12-release]",
):
    if fragment not in ci:
        errors.append(f"Phase 12 GitHub CI wiring missing: {fragment}")

if errors:
    print("Phase 12 release profile preflight FAILED:")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("Phase 12 release profile/source preflight PASS.")
print("Desktop and Mobile WebGL Build Profiles remain non-development.")
print("Distinct texture targets and explicit -activeBuildProfile build paths verified.")
print("Source preflight is not a WebGL build, binary QA audit, or browser test.")
