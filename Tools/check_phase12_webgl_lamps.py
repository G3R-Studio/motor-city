#!/usr/bin/env python3
"""Phase 12 WebGL day/night regression guard for authored FCG street lamps.

This is a source contract, not a browser visual or frame-time measurement.
"""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
CYCLE = ROOT / "Assets/Scripts/World/DayNightCycleController.cs"
QUALITY = ROOT / "Assets/Scripts/Platform/MotorCityQualityRuntime.cs"
ERRORS = []


def expect(condition: bool, message: str) -> None:
    if not condition:
        ERRORS.append(message)


def section(code: str, begin: str, end: str) -> str:
    start = code.find(begin)
    if start < 0:
        ERRORS.append(f"Missing section: {begin[:60]}")
        return ""
    stop = code.find(end, start + len(begin))
    if stop < 0:
        ERRORS.append(f"Unterminated section: {begin[:60]}")
        return ""
    return code[start:stop]


def main() -> int:
    cycle = CYCLE.read_text(encoding="utf-8-sig")
    quality = QUALITY.read_text(encoding="utf-8-sig")

    lamps = section(
        cycle, "private void ApplyStreetLights()", "private void DisableEnabledLampLights()"
    )
    budget_start = lamps.find("int lightBudget =")
    expect(budget_start >= 0, "Lamp light-budget selection missing")
    # The lightweight WebGL lamp limits are now shared by Editor, Android
    # and WebGL. Accept either the original platform gate or the shared gate,
    # while still validating the same bounded per-quality lighting policy.
    budget_code = lamps[budget_start:] if budget_start >= 0 else ""
    marker = (
        "#if UNITY_WEBGL && !UNITY_EDITOR"
        if "#if UNITY_WEBGL && !UNITY_EDITOR" in budget_code
        else "#if true"
    )
    webgl = section(budget_code, marker, "#endif")
    expect(bool(webgl), "Shared lightweight lamp budget must be explicit")
    expect("lampDistance = 0f;" not in webgl, "WebGL must not zero lamp range")
    expect("lightBudget = 0;" not in webgl, "WebGL must not turn off every lamp")

    # Quality-dependent, non-zero caps prevent all-city lighting stalls on weak
    # browsers while preserving a visible radius around the player.
    def require_budget(name: str, distance: int, count: int) -> None:
        n = re.escape(name)
        expect(
            bool(re.search(rf"{n}\s*=>\s*{distance}f", webgl)),
            f"{name} WebGL lamp radius must stay {distance}m",
        )
        expect(
            bool(re.search(rf"{n}\s*=>\s*{count}\b", webgl)),
            f"{name} WebGL lamp budget must stay {count}",
        )

    require_budget("MotorCityQualityPreset.Low", 65, 8)
    require_budget("MotorCityQualityPreset.High", 115, 24)
    expect("_ => 90f" in webgl, "WebGL Medium lamp range missing")
    expect("_ => 16" in webgl, "WebGL Medium lamp budget missing")

    expect("NightAmount >= 0.12f" in lamps, "Lamplights must turn on only after dusk")
    expect("if (!nightActive)" in lamps and "DisableEnabledLampLights();" in lamps,
           "Daytime and observer-null cases must turn off lamp lights")
    expect("lampCandidates.Sort(" in lamps and "DistanceSquared.CompareTo(" in lamps,
           "Nearest authored lamps must be selected first")
    expect("light.intensity = source.BaseIntensity * duskFade * distanceFade;" in lamps,
           "Lamp fade must preserve authored base intensity")
    expect("desiredLampLights.Add(light);" in lamps and
           bool(re.search(r"light\.enabled\s*=\s*true\s*;", lamps)),
           "Runtime must re-enable selected FCG lamps")
    expect(bool(re.search(r"light\.enabled\s*=\s*false\s*;", lamps)),
           "Runtime must turn lamps off again")

    register = section(
        cycle, "private void RefreshStreetLights()", "private void ApplyStreetLights()"
    )
    expect("GetComponentsInChildren<Light>(true)" in register,
           "FCG authored disabled spotlights must be discovered")
    expect("sourceLight.transform.position" in register,
           "Authored world light position must be used for proximity")
    expect("sourceLight.transform.rotation =" not in register and
           "sourceLight.transform.position =" not in register,
           "Registration must never overwrite authored lamp transforms")
    expect("sourceLight.spotAngle =" not in register and
           "sourceLight.range =" not in register and
           "sourceLight.shadows =" not in register,
           "Do not override authored Spot Light shape or shadows")
    expect("ResolveLampObserver();" in lamps or
           "lampObserver.position" in lamps, "Lamp observer must drive nearest lamps")
    expect("PresetChanged +=" in cycle and "HandleQualityPresetChanged()" in cycle,
           "Quality changes must refresh active lamp budget")
    expect("RuntimePlatform.WebGLPlayer" in quality,
           "WebGL must select a stable quality preset")

    if ERRORS:
        print("Phase 12 WebGL street lamp guard FAILED:")
        for message in ERRORS:
            print(" - " + message)
        return 1

    print("Phase 12 WebGL street lamp guard PASS:")
    print(" Non-zero per-quality nearest-Spot-Light budgets, day/night switching,")
    print(" authored transforms/shape preserved, no runtime lamp shadows override.")
    print(" Unity Play Mode and WebGL device/nighttime visuals still need testing.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
