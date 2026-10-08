#!/usr/bin/env python3
"""Reject QA/test entrypoints compiled into non-development WebGL release."""
from __future__ import annotations
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
SOURCES = ROOT / "Assets" / "Scripts"
METHOD = re.compile(
    r"^\s*(?:public|private|protected|internal)\s+"
    r"(?:(?:static|override|virtual|sealed|async|new)\s+)*"
    r"[\w<>\[\],?.]+\s+([A-Za-z_]\w*)\s*\("
)
QA_METHOD = re.compile(r"(?:ForTesting|ForTest)$")
TOKEN = re.compile(r"\s*(&&|\|\||!|\(|\)|[A-Za-z_]\w*)")

def condition(expr: str, symbols: set[str]) -> bool:
    tokens = TOKEN.findall(expr)
    if "".join(tokens).replace(" ", "") != "".join(expr.split()):
        raise ValueError("Invalid preprocessor expression: " + expr)
    index = 0
    def atom() -> bool:
        nonlocal index
        if index >= len(tokens):
            raise ValueError("Missing operand: " + expr)
        t = tokens[index]
        index += 1
        if t == "!":
            return not atom()
        if t == "(":
            val = or_expr()
            if index >= len(tokens) or tokens[index] != ")":
                raise ValueError("Missing ')': " + expr)
            index += 1
            return val
        if t in ("&&", "||", ")"):
            raise ValueError("Unexpected token: " + t)
        return t in symbols or t == "true"
    def and_expr() -> bool:
        nonlocal index
        val = atom()
        while index < len(tokens) and tokens[index] == "&&":
            index += 1
            next_val = atom()
            val = val and next_val
        return val
    def or_expr() -> bool:
        nonlocal index
        val = and_expr()
        while index < len(tokens) and tokens[index] == "||":
            index += 1
            next_val = and_expr()
            val = val or next_val
        return val
    result = or_expr()
    if index != len(tokens):
        raise ValueError("Unexpected tail: " + expr)
    return result

def check_source(path: pathlib.Path, lines: list[str], symbols: set[str]) -> list[str]:
    errors: list[str] = []
    stack: list[tuple[bool, bool, bool]] = []  # parent, branch_seen, current
    active = True
    for n, line in enumerate(lines, 1):
        token = line.strip()
        try:
            if token.startswith("#if "):
                yes = condition(token[4:], symbols)
                stack.append((active, yes, active and yes))
                active = active and yes
            elif token.startswith("#elif "):
                if not stack:
                    raise ValueError("Orphan #elif")
                parent, seen, _ = stack[-1]
                yes = condition(token[6:], symbols)
                active = parent and not seen and yes
                stack[-1] = (parent, seen or yes, active)
            elif token == "#else":
                if not stack:
                    raise ValueError("Orphan #else")
                parent, seen, _ = stack[-1]
                active = parent and not seen
                stack[-1] = (parent, True, active)
            elif token == "#endif":
                if not stack:
                    raise ValueError("Orphan #endif")
                parent, _, _ = stack.pop()
                active = parent
            elif active:
                match = METHOD.match(line)
                if match and QA_METHOD.search(match.group(1)):
                    errors.append(f"{path.relative_to(ROOT)}:{n}: {match.group(1)} is compiled in release")
        except ValueError as error:
            errors.append(f"{path.relative_to(ROOT)}:{n}: {error}")
    if stack:
        errors.append(f"{path.relative_to(ROOT)}: unclosed preprocessor branch")
    return errors

def self_test() -> None:
    for expr, expected in (
        ("UNITY_EDITOR || DEBUG", False),
        ("UNITY_EDITOR || UNITY_WEBGL", True),
        ("UNITY_WEBGL && !UNITY_EDITOR", True),
        ("(UNITY_EDITOR || DEBUG) && !UNITY_WEBGL", False),
        ("!DEBUG && UNITY_WEBGL", True),
    ):
        assert condition(expr, {"UNITY_WEBGL"}) == expected, expr
    example = ["#if UNITY_EDITOR || DEBUG", "public void ResetForTesting()", "#endif"]
    assert not check_source(ROOT / "Assets" / "Scripts" / "Dummy.cs", example, {"UNITY_WEBGL"})
    example[0] = "#if UNITY_EDITOR || UNITY_WEBGL"
    assert check_source(ROOT / "Assets" / "Scripts" / "Dummy.cs", example, {"UNITY_WEBGL"})

def main() -> int:
    self_test()
    errors = []
    files = list(sorted(SOURCES.rglob("*.cs")))
    if not files:
        print("No runtime C# sources found", file=sys.stderr)
        return 1
    for path in files:
        lines = path.read_text(encoding="utf-8-sig").splitlines()
        errors.extend(check_source(path, lines, {"UNITY_WEBGL"}))
    if errors:
        print("Motor City release QA check FAILED:")
        print("\n".join(" - " + item for item in errors))
        return 1
    print(f"Motor City release QA check passed ({len(files)} C# files, WebGL release symbols).")
    print("Unity compile, runtime smoke and complete call-site analysis are separate.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
