#!/usr/bin/env python3
"""Run available Phase 0/1 static checks in a single pass.

Run from project root:
    py -3 Tools/check_phase_0_1.py
Unity Editor and WebGL runtime validation must still be performed separately.
"""
from __future__ import annotations
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
COMMANDS = (
    ("Input backend", [sys.executable, "Tools/check_input_backend.py"]),
    ("WebGL release QA guards", [sys.executable, "Tools/check_release_qa.py"]),
    ("Static project audit", [sys.executable, "Tools/audit_project.py", "--output", "Temp/MotorCityAudit/static-audit.json"]),
)

def main() -> int:
    failures = []
    for title, command in COMMANDS:
        print(f"\n=== {title} ===", flush=True)
        outcome = subprocess.run(command, cwd=ROOT, check=False)
        if outcome.returncode:
            failures.append(title)
    if failures:
        print("\nPhase 0/1 static checks FAILED:", ", ".join(failures))
        return 1
    print("\nPhase 0/1 Python static checks PASSED.")
    print("Separately run: pwsh -NoProfile -File Tools/check_cleanup.ps1")
    print("Unity compilation, Console snapshot and release profiles are not verified by this script.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
