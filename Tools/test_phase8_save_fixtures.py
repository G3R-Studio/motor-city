#!/usr/bin/env python3
"""Offline Phase 8 fixtures: deterministic metadata conflict and JSON recovery cases.

These fixtures model the documented decisions; they do not execute Unity JsonUtility,
PlayerPrefs, or the live Yandex service.
"""
import json
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REPORT = ROOT / "Temp/MotorCityAudit/phase8-save-fixture-tests.json"

@dataclass
class Meta:
    has_data: bool = False
    revision: int = 0
    last_synced: int = 0
    cloud_revision: int = 0
    server_time: int = 0
    legacy_ticks: int = 0

    @property
    def trusted(self):
        return self.cloud_revision > 0

    @property
    def unsynced(self):
        return self.revision > self.last_synced

def remote_wins(local, remote):
    if not remote.has_data:
        return False
    if not local.has_data:
        return True
    if local.trusted or remote.trusted:
        if remote.cloud_revision != local.cloud_revision:
            return remote.cloud_revision > local.cloud_revision
        if local.unsynced:
            return False
        if remote.server_time != local.server_time:
            return remote.server_time > local.server_time
        return remote.revision > local.revision
    return remote.legacy_ticks > local.legacy_ticks

def main():
    cases = [
        ("empty_remote", Meta(True, 4, 4, 2), Meta(), False),
        ("fresh_device", Meta(), Meta(True, 2, 2, 3), True),
        ("newer_cloud", Meta(True, 6, 6, 3), Meta(True, 8, 8, 4), True),
        ("offline_local_changes_same_cloud", Meta(True, 9, 8, 4), Meta(True, 8, 8, 4), False),
        ("older_cloud", Meta(True, 8, 8, 5), Meta(True, 9, 9, 4), False),
        ("newer_server_time", Meta(True, 8, 8, 5, 100), Meta(True, 8, 8, 5, 110), True),
        ("newer_remote_revision", Meta(True, 8, 8, 5, 100), Meta(True, 9, 9, 5, 100), True),
        ("legacy_remote_newer", Meta(True, 1, 0, 0, 0, 10), Meta(True, 1, 0, 0, 0, 20), True),
        ("legacy_local_newer", Meta(True, 1, 0, 0, 0, 20), Meta(True, 1, 0, 0, 0, 10), False),
    ]
    results = [{"name": name, "pass": remote_wins(local, remote) == expected}
               for name, local, remote, expected in cases]
    malformed = '{"Version":2,"Ints":['
    try:
        json.loads(malformed)
        malformed_rejected = False
    except json.JSONDecodeError:
        malformed_rejected = True
    results.append({"name": "malformed_json_rejected_by_fixture_parser", "pass": malformed_rejected})
    legacy = json.loads('{"Version":1,"Ints":[{"Key":"fixture.credits","Value":123}]}')
    results.append({"name": "legacy_v1_fixture_fields", "pass": legacy["Version"] == 1 and legacy["Ints"][0]["Value"] == 123})
    report = {"scope": "Python model/fixtures only, no Unity PlayerPrefs or live cloud", "cases": results}
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Phase 8 fixture models: {sum(x['pass'] for x in results)}/{len(results)} pass")
    return 0 if all(x["pass"] for x in results) else 1

if __name__ == "__main__":
    raise SystemExit(main())
