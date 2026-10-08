#!/usr/bin/env python3
"""Static release-safety gate for Motor City QA entrypoints.

Run from repository root: py -3 Tools/check_release_qa.py
This checks source guards; Unity compilation and build smoke remain separate gates.
"""
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCES = ROOT / "Assets" / "Scripts"
METHOD = re.compile(
    r"^\s*(?:public|private|protected|internal)\s+"
    r"(?:(?:static|override|virtual|sealed|async|new)\s+)*"
    r"[\w<>\[\],?.]+\s+([A-Za-z_]\w*)\s*\(",
)
QA_METHOD = re.compile(r"(?:ForTesting|ForTest)$")
QA_GUARD = re.compile(r"\bUNITY_EDITOR\b|\bDEBUG\b")

def assert_qa_guard(path: pathlib.Path, lines: list[str]) -> list[str]:
    errors = []
    stack: list[str] = []
    for n, line in enumerate(lines, 1):
        token = line.strip()
        if token.startswith("#if "):
            stack.append(token[4:].strip())
        elif token.startswith("#elif "):
            if stack:
                stack[-1] = token[6:].strip()
        elif token == "#else":
            if stack:
                stack[-1] = "!(" + stack[-1] + ")"
        elif token == "#endif":
            if stack:
                stack.pop()
            else:
                errors.append(f"{path.relative_to(ROOT)}:{n}: unmatched #endif")
        m = METHOD.match(line)
        if m and QA_METHOD.search(m.group(1)):
            # A QA guard must exist in a surrounding #if. A release-positive
            # UNITY_WEBGL branch is not sufficient.
            if not any(
                QA_GUARD.search(g) and not g.startswith("!")
                and not ("UNITY_WEBGL" in g and "||" in g and
                         "DEBUG" not in g and "UNITY_EDITOR" not in g)
                for g in stack
            ):
                errors.append(
                    f"{path.relative_to(ROOT)}:{n}: {m.group(1)} is not behind a QA guard"
                )
    if stack:
        errors.append(f"{path.relative_to(ROOT)}: unclosed #if")
    return errors

def main() -> int:
    errors = []
    count = 0
    for path in sorted(SOURCES.rglob("*.cs")):
        lines = path.read_text(encoding="utf-8-sig").splitlines()
        count += 1
        errors += assert_qa_guard(path, lines)
    if errors:
        print("Motor City release QA guard check FAILED:")
        print("\n".join(" - " + x for x in errors))
        return 1
    print(f"Motor City release QA guard source check passed ({count} C# files).")
    print("Unity compilation, call-site audit and WebGL profile smoke remain separate.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
