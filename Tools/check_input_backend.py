"""Read-only guard for the new Input System backend in Motor City.

This guard checks repository settings and source references; it is NOT a
Unity compilation, input-device smoke test, or WebGL build.
Run: py -3 Tools/check_input_backend.py
"""
from pathlib import Path
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[1]

SETTINGS = [
    Path("ProjectSettings/ProjectSettings.asset"),
    Path("Assets/Settings/Build Profiles/Web - Mobile - Release.asset"),
    Path("Assets/Settings/Build Profiles/Web - Desktop - Release.asset"),
]

# Legacy input calls will throw when the Unity player only enables the new
# Input System. "Input." alone is not used: it would also match valid
# MotorCity.Input namespace references and virtual input code.
LEGACY_CALL = re.compile(
    r"(?<![\\w.])(?:UnityEngine\\.)?Input\\."
    r"(?:Get[A-Za-z0-9_]*|touchSupported|touchCount|touches|"
    r"mousePosition|mouseScrollDelta|anyKey|anyKeyDown|"
    r"acceleration|gyro|compass)\\b"
)
LEGACY_TYPE = re.compile(r"\\bUnityEngine\\.Input\\b")


def errors() -> list[str]:
    problems: list[str] = []

    manifest_path = ROOT / "Packages/manifest.json"
    if not manifest_path.is_file():
        problems.append(f"Missing manifest: {manifest_path}")
    else:
        try:
            manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
            if "com.unity.inputsystem" not in manifest.get("dependencies", {}):
                problems.append("Packages/manifest.json: com.unity.inputsystem absent")
        except (OSError, ValueError) as error:
            problems.append(f"Cannot parse Packages/manifest.json: {error}")

    for relative in SETTINGS:
        config = ROOT / relative
        if not config.is_file():
            problems.append(f"Missing input settings: {relative}")
            continue

        source = config.read_text(encoding="utf-8-sig")
        values = re.findall(r"\\bactiveInputHandler:\\s*([012])\\b", source)
        if values != ["1"]:
            problems.append(
                f"{relative}: expected exactly one activeInputHandler: 1 "
                f"(Input System New); found {values}"
            )

    scripts = sorted((ROOT / "Assets").rglob("*.cs"))
    if not scripts:
        problems.append("No C# scripts found under Assets/")
        return problems

    for file in scripts:
        relative = file.relative_to(ROOT)
        content = file.read_text(encoding="utf-8-sig", errors="replace")
        for number, line in enumerate(content.splitlines(), 1):
            if LEGACY_CALL.search(line) or LEGACY_TYPE.search(line):
                problems.append(
                    f"{relative}:{number}: legacy UnityEngine.Input reference: "
                    f"{line.strip()[:160]}"
                )

    return problems


def main() -> int:
    problems = errors()
    if problems:
        print("Motor City input backend check FAILED:")
        for item in problems:
            print("  - " + item)
        return 1

    print("Motor City input backend source/settings checks passed.")
    print("  Global PlayerSettings and both WebGL release profiles: Input System (New).")
    print("  Package dependency present; no legacy Input API references in Assets/*.cs.")
    print("  Unity Editor restart, Play Mode controls and WebGL smoke still required.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
