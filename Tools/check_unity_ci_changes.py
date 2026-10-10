#!/usr/bin/env python3
"""Decide whether Unity CI is needed since the last *verified* Unity Editor run.

Unlike HEAD~1 filters this covers changes in cancelled intermediate pushes.
Fail closed: any uncertainty requests a Unity run.
"""
import json
import os
import subprocess
import urllib.request

PREFIXES = ("Assets/", "Packages/", "ProjectSettings/", "UserSettings/")
FILES = {"Tools/run_unity_phase2.ps1", "Tools/run_unity_phase2.py",
         ".github/workflows/motor-city-source-gates.yml"}

def api(url):
    request = urllib.request.Request(url, headers={
        "Authorization": "Bearer " + os.environ["GITHUB_TOKEN"],
        "Accept": "application/vnd.github+json",
        "User-Agent": "motor-city-unity-ci-filter",
    })
    with urllib.request.urlopen(request, timeout=20) as response:
        return json.load(response)

def main():
    should_run = True
    reason = "fallback: no verified Unity baseline"
    try:
        event = os.environ.get("GITHUB_EVENT_NAME", "")
        if event != "push":
            reason = "manual or pull request"
        else:
            repo = os.environ["GITHUB_REPOSITORY"]
            head = os.environ["GITHUB_SHA"]
            base_url = f"https://api.github.com/repos/{repo}"
            runs = api(f"{base_url}/actions/workflows/motor-city-source-gates.yml/runs"
                       "?branch=main&status=success&per_page=50")["workflow_runs"]
            baseline = None
            for run in runs:
                if run["head_sha"] == head:
                    continue
                jobs = api(f"{base_url}/actions/runs/{run['id']}/jobs?per_page=100")["jobs"]
                if any(job["name"] == "Unity 6 Editor batch validation" and
                       job["conclusion"] == "success" for job in jobs):
                    baseline = run["head_sha"]
                    break
            if baseline:
                diff = subprocess.run(
                    ["git", "diff", "--name-only", baseline, head],
                    capture_output=True, text=True, check=True,
                )
                changed = diff.stdout.splitlines()
                relevant = [p for p in changed
                            if p.startswith(PREFIXES) or p in FILES]
                should_run = bool(relevant)
                reason = (f"last successful Unity run {baseline[:12]}, "
                          f"{len(changed)} changed files, {len(relevant)} Unity-relevant")
            else:
                reason = "no successful Unity Editor job in last 50 completed successful runs"
    except Exception as error:
        reason = f"conservative Unity run on comparison failure: {error}"
    print(f"Run Unity: {should_run}; {reason}")
    output = os.environ.get("GITHUB_OUTPUT")
    if output:
        with open(output, "a", encoding="utf-8") as stream:
            stream.write(f"unity={'true' if should_run else 'false'}\n")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
