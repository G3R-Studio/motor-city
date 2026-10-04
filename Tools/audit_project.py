"""Read-only Unity source/dependency audit; never deletes assets.

Usage: python Tools/audit_project.py --baseline <commit> --output <report.json>
GUID reachability is not proof that an asset is unused: imported FBX content,
Resources, Inspector events and editor tools also provide dependencies.
"""
import argparse
import collections
import json
from pathlib import Path
import re
import subprocess

GUID = re.compile(r"guid: ([0-9a-f]{32})")
SERIALIZED = {".meta", ".prefab", ".unity", ".asset", ".mat", ".controller", ".overridecontroller", ".anim", ".playable"}


def git(*args):
    return subprocess.check_output(["git", *args], text=True, encoding="utf-8")


def audit(root, baseline):
    paths = sorted(set(git("ls-files", "--cached", "--others", "--exclude-standard").splitlines()))
    paths = [path for path in paths if (root / path).is_file()]
    guid_paths = {}
    duplicate_guids = []
    for path in paths:
        if not path.endswith(".meta"):
            continue
        with (root / path).open(encoding="utf-8-sig", errors="replace") as stream:
            for line in stream:
                if line.startswith("guid: "):
                    value = line.split()[1]
                    asset = path[:-5]
                    if value in guid_paths:
                        duplicate_guids.append([guid_paths[value], asset])
                    guid_paths[value] = asset
                    break
    references = collections.defaultdict(set)
    callback_counts = collections.Counter()
    scripts = []
    for path in paths:
        file = root / path
        if file.suffix == ".cs":
            source = file.read_text(encoding="utf-8-sig", errors="replace")
            callbacks = re.findall(r"\bvoid\s+(Update|LateUpdate|FixedUpdate)\s*\(", source)
            callback_counts.update(callbacks)
            scripts.append({"path": path, "lines": len(source.splitlines()), "frame_callbacks": callbacks})
        if file.suffix not in SERIALIZED:
            continue
        with file.open(encoding="utf-8-sig", errors="replace") as stream:
            for line in stream:
                # A meta's own identity is not an incoming dependency.
                if file.suffix == ".meta" and line.startswith("guid: "):
                    continue
                for value in GUID.findall(line):
                    references[value].add(path)
    deleted = {}
    if baseline:
        for line in git("diff", "--name-status", "--no-renames", baseline, "--", "Assets/**/*.meta").splitlines():
            if not line.startswith("D\t"):
                continue
            path = line[2:]
            old = git("show", baseline + ":" + path)
            match = re.search(r"^guid: (\w+)", old, re.M)
            if match and match[1] not in guid_paths:
                deleted[match[1]] = path[:-5]
    dangling_deleted = [{"asset": path, "referenced_by": sorted(references[value])}
                        for value, path in deleted.items() if references[value]]
    candidates = sorted(path for value, path in guid_paths.items()
                        if not references[value] and (root / path).is_file()
                        and "/Resources/" not in path and Path(path).suffix not in {".cs", ".unity"})
    return {
        "warning": "Unreferenced candidates are NOT a safe deletion list. Validate dynamic loads and imported dependencies in Unity.",
        "tracked_and_untracked_files": len(paths),
        "asset_guids": len(guid_paths),
        "serialized_files": sum((root / path).suffix in SERIALIZED for path in paths),
        "script_count": len(scripts),
        "frame_callbacks": dict(callback_counts),
        "scripts": sorted(scripts, key=lambda item: item["lines"], reverse=True),
        "duplicate_guids": duplicate_guids,
        "deleted_assets": sorted(deleted.values()),
        "dangling_deleted_assets": dangling_deleted,
        "unreferenced_candidates": candidates,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", help="Compare removed GUIDs against this Git revision")
    parser.add_argument("--output", default="Temp/MotorCityAudit/static-audit.json")
    args = parser.parse_args()
    root = Path(git("rev-parse", "--show-toplevel").strip())
    report = audit(root, args.baseline)
    output = root / args.output
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Audited {report['script_count']} scripts, {report['serialized_files']} serialized files, {report['asset_guids']} GUIDs")
    print(f"Deleted-asset dangling references: {len(report['dangling_deleted_assets'])}; report: {output}")
    return 1 if report["dangling_deleted_assets"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
