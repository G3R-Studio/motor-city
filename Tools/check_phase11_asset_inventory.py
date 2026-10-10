#!/usr/bin/env python3
"""Phase 11 read-only Git asset/package inventory; never deletes project files.

A duplicate Git blob is evidence of identical *tracked Git content*, not of
interchangeable Unity assets. Different .meta GUIDs, importer state,
Resources.Load names and generated assets must be audited before any deletion.
"""
from __future__ import annotations

from collections import Counter, defaultdict
from pathlib import Path
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
REPORT = ROOT / "Temp/MotorCityAudit/phase11-asset-inventory.json"
MEDIA_EXTENSIONS = {
    ".png", ".jpg", ".jpeg", ".tga", ".exr", ".psd",
    ".fbx", ".blend", ".obj", ".mtl", ".wav", ".ogg", ".mp3",
    ".mp4", ".ttf", ".otf", ".asset", ".mat", ".prefab",
}
PROTECTED = (
    "Assets/Resources/MotorCity/Environment/CityVisual.prefab",
    "Assets/LocalGenerated/FCG_Workbench.unity",
    "Assets/Eric VFX Studio/Resource/Textures/circle2.PNG",
    "Assets/Resources/MotorCity/UI/Loading/circle2.PNG",
    "Assets/VehicleAssets/AmgGT/all.png",
    "Assets/VehicleAssets/Beatall/all.png",
    "Assets/VehicleAssets/Delorean/all.png",
    "Assets/VehicleAssets/Porsche996/rear_wheels.mtl",
    "Assets/VehicleAssets/ToyotaAE86/front_wheels.mtl",
)
THIRD_PARTY_ROOTS = (
    "Assets/ARCADE - FREE Racing Car/",
    "Assets/Eric VFX Studio/",
    "Assets/Fantastic City Generator/",
    "Assets/Fantasy Skybox FREE/",
    "Assets/Haons SD series Pack/",
    "Assets/PROMETEO - Car Controller/",
    "Assets/SapphiArt/",
    "Assets/VehicleAssets/",
    "Packages/com.unity.springbone/",
)


def git(*args: str) -> bytes:
    return subprocess.check_output(["git", *args], cwd=ROOT)


def tracked_blobs() -> dict[str, str]:
    result = {}
    for record in git("ls-files", "-s", "-z").split(b"\0"):
        if not record:
            continue
        header, raw_path = record.split(b"\t", 1)
        mode, sha, stage = header.decode("ascii").split()
        if stage == "0" and mode in ("100644", "100755"):
            result[raw_path.decode("utf-8", "surrogateescape")] = sha
    return result


def meta_guid(path: str) -> str | None:
    metadata = ROOT / (path + ".meta")
    if not metadata.is_file():
        return None
    match = re.search(
        r"(?m)^guid:\s*([0-9a-f]{32})\s*$",
        metadata.read_text(encoding="utf-8-sig", errors="replace"),
    )
    return match.group(1) if match else None


def main() -> int:
    errors = []
    blobs = tracked_blobs()
    tracked = set(blobs)

    # Unity treats the source Workbench and baked Resources prefab as different
    # roles. Their separation is deliberate, even though both are large.
    for path in PROTECTED:
        if path not in tracked or not (ROOT / path).is_file():
            errors.append(f"Protected source or runtime asset missing: {path}")
        if (path + ".meta") not in tracked or not meta_guid(path):
            errors.append(f"Protected Unity asset metadata/GUID missing: {path}.meta")

    by_blob = defaultdict(list)
    for path, sha in blobs.items():
        if path.startswith("Assets/") and Path(path).suffix.lower() in MEDIA_EXTENSIONS:
            by_blob[sha].append(path)
    duplicate_blobs = {
        sha: paths for sha, paths in by_blob.items() if len(paths) > 1
    }
    duplicates = []
    for sha, paths in sorted(duplicate_blobs.items(), key=lambda kv: kv[1]):
        size = int(git("cat-file", "-s", sha).decode("ascii"))
        guids = {path: meta_guid(path) for path in sorted(paths)}
        duplicates.append({
            "git_blob_sha": sha,
            "tracked_blob_bytes": size,
            "paths": sorted(paths),
            "meta_guids": guids,
            "all_have_distinct_meta_guids": (
                all(guids.values()) and len(set(guids.values())) == len(guids)
            ),
            "deletion_certified": False,
        })
        # For tracked Unity source copies, losing individual importer identity
        # is a regression even when tracked media bytes match.
        if not all(guids.values()):
            errors.append(f"Duplicate source missing importer GUID: {', '.join(paths)}")
        if len(set(guids.values())) != len(guids):
            errors.append(f"Duplicate source shares importer GUID: {', '.join(paths)}")

    manifest = json.loads((ROOT / "Packages/manifest.json").read_text(encoding="utf-8-sig"))
    lock = json.loads((ROOT / "Packages/packages-lock.json").read_text(encoding="utf-8-sig"))
    direct = manifest.get("dependencies", {})
    resolved = lock.get("dependencies", {})
    for name, value in direct.items():
        record = resolved.get(name)
        if not record:
            errors.append(f"Package missing from lockfile: {name}")
            continue
        if record.get("depth") != 0 or record.get("version") != value:
            errors.append(f"Manifest/lockfile direct dependency mismatch: {name}")
        if value.startswith("file:"):
            location = "Packages/" + value[len("file:"):].strip("/") + "/package.json"
            if record.get("source") != "embedded" or location not in tracked:
                errors.append(f"Embedded package missing/incorrect: {name} -> {location}")
        elif value.startswith(("https://", "http://")):
            if record.get("source") != "git":
                errors.append(f"Git package source mismatch: {name}")
        elif record.get("source") not in ("builtin", "registry"):
            errors.append(f"Unexpected registry/builtin package source: {name}")

    protected_sizes = {}
    for path in PROTECTED[:2]:
        if path in blobs:
            protected_sizes[path] = int(git("cat-file", "-s", blobs[path]).decode("ascii"))
    root_counts = {root: sum(path.startswith(root) for path in tracked) for root in THIRD_PARTY_ROOTS}
    docs = "Assets/ThirdParty/THIRD_PARTY_AUDIT.md"
    if docs not in tracked:
        errors.append(f"Third-party source/license audit document missing: {docs}")

    report = {
        "phase": 11,
        "scope": "tracked Git blobs and manifest/lockfile; no Unity dependency graph",
        "tracked_files": len(tracked),
        "tracked_media_model_material_files": sum(len(paths) for paths in by_blob.values()),
        "identical_git_blob_groups": len(duplicates),
        "identical_git_blob_files": sum(len(d["paths"]) for d in duplicates),
        "duplicates": duplicates,
        "protected_authoring_runtime_blob_bytes": protected_sizes,
        "third_party_root_tracked_counts": root_counts,
        "packages": {
            "direct": direct,
            "resolved_count": len(resolved),
            "resolved_sources": dict(sorted(Counter(
                record.get("source", "unknown") for record in resolved.values()
            ).items())),
        },
        "warnings": [
            "Git blob identity does not make two Unity asset GUIDs interchangeable.",
            "Git LFS pointer identity does not verify hydrated large-binary payloads.",
            "Runtime Resources.Load, importer/builder behavior, shaders, licenses and WebGL must be checked before deletion.",
            "Scene-only dependency graphs omit dynamically loaded Resources and Editor-generated objects.",
        ],
        "deletions_performed": 0,
        "deletion_candidates_certified": 0,
        "errors": errors,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(
        "Phase 11 asset/package inventory:",
        len(tracked), "tracked files,",
        report["tracked_media_model_material_files"], "media/model/material files,",
        len(duplicates), "identical Git blob groups,",
        len(direct), "direct /", len(resolved), "resolved packages.",
    )
    print("No assets removed; duplicate files are NOT certified for deletion.")
    for duplicate in duplicates:
        print("IDENTICAL Git blob:", " | ".join(duplicate["paths"]))
    for error in errors:
        print("ERROR:", error)
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
