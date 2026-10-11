#!/usr/bin/env python3
"""Guard complete authored FCG traffic roster in WebGL Unity player.

Do NOT reintroduce bus weighting/caps. Recovery should use the same 12 unique
authored FCG vehicle prefabs when the baked runtime-city IaCars is incomplete.

This is a source/asset contract; browser traffic variety still needs user QA.
"""
from __future__ import annotations

from collections import Counter
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
ORIGINAL = ROOT / "Assets/Fantastic City Generator/Traffic System/Traffic System.prefab"
CATALOG = ROOT / "Assets/Resources/MotorCity/Environment/TrafficVehicleCatalog.prefab"
CATALOG_META = CATALOG.with_suffix(".prefab.meta")
RUNTIME = ROOT / "Assets/Fantastic City Generator/Scripts/TrafficSystem.cs"
VEHICLES = ROOT / "Assets/Fantastic City Generator/Traffic System/Vehicles/Prefabs"
RESOURCE_FALLBACK = ROOT / "Assets/Resources/MotorCity/Environment/FCGTrafficCars"
ERRORS: list[str] = []

def check(value: bool, message: str) -> None:
    if not value:
        ERRORS.append(message)

def prefab_vehicle_guids(path: Path) -> list[str]:
    text = path.read_text(encoding="utf-8-sig")
    roster = re.search(r"(?m)^  IaCars:\s*\n(.*?)(?=^  nVehicles:)", text, re.S | re.M)
    if roster is None:
        ERRORS.append(f"{path}: serialized IaCars array missing")
        return []
    return re.findall(r"guid:\s*([0-9a-f]{32}), type: 3", roster.group(1))

def main() -> int:
    base = prefab_vehicle_guids(ORIGINAL)
    copy = prefab_vehicle_guids(CATALOG)
    check(bool(base) and base == copy,
          "Resources catalog must match original FCG Traffic System authored IaCars GUIDs in order")
    check(len(set(copy)) == 12,
          f"Expected 12 unique original vehicle models, got {len(set(copy))}")
    check(len(copy) == 23,
          f"Expected 23 serialized entries before deduplication, got {len(copy)}")
    check(CATALOG_META.exists() and
          bool(re.search(r"(?m)^guid: [0-9a-f]{32}$",
                         CATALOG_META.read_text(encoding="utf-8-sig"))),
          "Resources catalog .meta GUID missing")

    id_to_model = {}
    for meta in VEHICLES.glob("*.prefab.meta"):
        guid = re.search(
            r"(?m)^guid:\s*([0-9a-f]{32})",
            meta.read_text(encoding="utf-8-sig"),
        )
        if guid is not None:
            id_to_model[guid.group(1)] = meta.stem.removesuffix(".prefab")

    unknown = set(copy).difference(id_to_model)
    check(not unknown, f"Unresolvable FCG prefab GUIDs: {sorted(unknown)}")
    original_models = {id_to_model.get(guid, "") for guid in copy}
    buses = [name for name in original_models if name.lower().startswith("bus")]
    check(len(buses) == 1,
          f"Expected 1 bus among 12 original models, found {buses}")

    fallback = [p for p in RESOURCE_FALLBACK.glob("*.prefab")]
    fallback_buses = [p for p in fallback if p.name.lower().startswith("bus")]
    # The old incomplete Resources folder is left intact for legacy backup
    # compatibility; it must NEVER replace the complete authored roster.
    check(len(fallback) == 6 and len(fallback_buses) == 3,
          "Legacy Resources fallback makeup changed; revisit priority/roster investigation")

    runtime = RUNTIME.read_text(encoding="utf-8-sig")
    for marker in (
        'AuthoredTrafficCatalogPath',
        '"MotorCity/Environment/TrafficVehicleCatalog"',
        "ResolveAuthoredTrafficPool()",
        "if (authored.Length > serialized.Length)",
        "if (serialized.Length > 0 &&",
        "!repairedPartialTrafficRoster)",
        "IaCars = authored;",
        "return authored;",
        "if (serialized.Length > 0)",
        ".Distinct()",
        "spawnPool[Random.Range(0, spawnPool.Length)]",
    ):
        check(marker in runtime, f"TrafficSystem lost contract: {marker}")
    for forbidden in ("busSpawnPercent", "busTrafficCapPercent", "SelectTrafficPrefab("):
        check(forbidden not in runtime, f"Old bus weighting/cap has reappeared: {forbidden}")

    if ERRORS:
        print("Phase 12 authored FCG traffic roster guard FAILED:")
        for error in ERRORS:
            print(" -", error)
        return 1

    print("Phase 12 authored FCG traffic roster source/asset guard PASS.")
    print(f" Original models: {len(original_models)}; bus models: {len(buses)}.")
    print(f" Legacy fallback models: {len(fallback)}; bus models: {len(fallback_buses)}.")
    print(" Partial city prefab reference loss repairs to complete original FCG roster.")
    print(" Actual browser traffic variety and spawn counts still require smoke QA.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
