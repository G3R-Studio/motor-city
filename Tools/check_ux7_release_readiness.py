#!/usr/bin/env python3
"""UX-7 preflight: source contracts only, not a Unity or browser run."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parent.parent


def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")


ui_dir = ROOT / "Assets/Scripts/UI"
sources = {p.name: p.read_text(encoding="utf-8-sig") for p in ui_dir.glob("*.cs")}
source_workflow = read(".github/workflows/motor-city-source-gates.yml")
pages_workflow = read(".github/workflows/motor-city-pages.yml")
release_script = read("Tools/run_unity_phase12_release.ps1")
build_script = read("Assets/Editor/MotorCityPhase12ReleaseBuild.cs")
pages_script = read("Tools/prepare_pages_webgl.py")
journal = sources["PrototypeHudPlayerExperienceJournal.cs"]
navigator = sources["NavigatorView.cs"]


def partial_count(class_name, method):
    # Only consider files belonging to the requested partial UI type.
    # Do not count a different UI component's OnDestroy.
    owner = re.compile(r"\\bpartial\\s+class\\s+" + re.escape(class_name) + r"\\b")
    declaration = re.compile(r"\\b(?:private|public|protected|internal)\\s+void\\s+"
                             + re.escape(method) + r"\\s*\\(")
    return sum(len(declaration.findall(content))
               for content in sources.values() if owner.search(content))


checks = {
    "single PrototypeHud OnDestroy with journal event cleanup":
        partial_count("PrototypeHud", "OnDestroy") == 1
        and "ActivityResultShown -= CaptureJournalBeforeActivityResult" in navigator
        and "OnDestroy()" not in journal,
    "single FrontEnd OnDestroy callback":
        partial_count("MotorCityFrontEndFlow", "OnDestroy") == 1,
    "input / rendering source ownership gates registered":
        "Tools/check_phase7_ui_ownership.py" in source_workflow
        and "Tools/check_ui_layout.ps1" in source_workflow,
    "C# Roslyn and QA exclusion gates registered":
        "./Tools/check_cleanup.ps1" in source_workflow
        and "Tools/check_release_qa.py" in source_workflow,
    "release source gate registered":
        "Tools/check_ux7_release_readiness.py" in source_workflow,
    "source workflow never builds WebGL without explicit opt-in":
        "workflow_dispatch:" in source_workflow
        and "inputs.build_webgl_release == true" in source_workflow
        and "  push:" not in source_workflow.split("jobs:", 1)[0],
    "webgl release requires source/UI/Unity Editor gates":
        "needs: [changes, source-gates, ui-layout, unity-editor]" in source_workflow
        and "needs.unity-editor.result == 'success'" in source_workflow
        and "needs.source-gates.result == 'success'" in source_workflow
        and "needs.ui-layout.result == 'success'" in source_workflow,
    "two distinct release build profiles from exact source":
        "-Profile Desktop" in source_workflow
        and "-Profile Mobile" in source_workflow
        and "-activeBuildProfile" in release_script
        and "BuildProfile.GetActiveBuildProfile()" in build_script,
    "release artifacts explicitly uploaded":
        all(name in source_workflow for name in (
            "motor-city-webgl-desktop-release",
            "motor-city-webgl-mobile-release",
            "motor-city-phase12-release-reports")),
    "Pages requires source workflow, dispatch, and exact SHA":
        all(item in pages_workflow for item in (
            "release_run_id:", "Motor City Source Gates",
            "workflow_dispatch", '".head_sha', '"$GITHUB_SHA"',
            '"$current_main"', '"$release_sha"')),
    "Pages requires both same-run release artifacts":
        all(item in pages_workflow for item in (
            'index("motor-city-webgl-desktop-release")',
            'index("motor-city-webgl-mobile-release")',
            "run-id: ${{ inputs.release_run_id }}",
            "release_sha")),
    "Pages staging prevents invalid compressed WebGL assets":
        "BR_SUFFIXES" in pages_script
        and "SITE_LIMIT_BYTES" in pages_script
        and "brotli.Decompressor()" in pages_script
        and "release.txt" in pages_script,
    "WebGL release not debug build":
        "BuildOptions.None" in build_script
        and "EditorUserBuildSettings.development" in build_script
        and "BuildTarget.WebGL" in build_script,
    "Pages remains manual, not push or automatic CI":
        "workflow_dispatch:" in pages_workflow
        and "  push:" not in pages_workflow.split("jobs:", 1)[0]
        and "  workflow_run:" not in pages_workflow.split("jobs:", 1)[0],
}

failed = 0
for name, passed in checks.items():
    print(("[PASS] " if passed else "[FAIL] ") + name)
    if not passed:
        failed += 1
print(f"UX-7 source readiness: {len(checks) - failed}/{len(checks)}")
if failed:
    raise SystemExit(1)
print("This is source preflight only; Editor/WebGL builds and browser QA are separate.")
