# Phase 8 — Save/progression baseline (2026-10-10)

## Data boundaries

- Local document is stored in the existing `MotorCity.Save.Json.v1` PlayerPrefs key. The payload schema is version 2. **Do not rename the storage key**: the suffix denotes the original slot and is intentionally not the current document schema number.
- `MotorCity.Save.CorruptBackup.v1` is the recovery slot for malformed local JSON on startup. A parse exception backs up the raw content before deleting the active slot.
- `MotorCitySaveService.Normalize` sanitizes typed lists and bounds revision metadata. Older keys are read through typed `GetInt`, `GetFloat`, `GetString` fallback paths.
- Explicit QA reset is a separate method. Do not run it to test migrations on an existing player save.
- `MotorCityCloudSaveRuntime.ShouldUseRemote` uses cloud revision/server metadata when available and falls back to legacy modified-time handling when not.
- On remote conflict import, paid `MotorCity.Purchase.SupporterPack` entitlement and its reward-claim state are preserved from local data.
- `MotorCityCloudSaveRuntime` snapshots `attemptedRevision` before upload and only marks that revision synchronized after a success callback; changes during an upload remain unsynced.
- `MotorCitySaveRuntime` flushes local data periodically and upon focus/pause/quit events.

## Automated protection

`Tools/check_phase8_save_progression.py` produces `Temp/MotorCityAudit/phase8-save-keys.json`. It guards the local slot, recovery slot, schema, JSON normalization and cloud metadata contracts. It also lists *literal string arguments* passed to save-service read/write/delete methods. Key names stored in constants, built dynamically, or accessed through direct legacy PlayerPrefs calls require follow-up inspection.

## Remaining gates (not yet proven)

1. Produce a complete registry including dynamic and direct legacy keys and distinguish permanent entitlements, QA-only and normal progress.
2. Test legacy v1 import and malformed JSON recovery using isolated disposable PlayerPrefs, never a user's real data.
3. Test cloud conflict cases: fresh device, offline edits, old cloud metadata, concurrent edits during upload, cloud unavailable/failure, and paid entitlement preservation.
4. Browser/device persistence smoke including restart, reload, and supported Yandex login lifecycle.

Passing a static gate is **not** evidence that migrations, device cloud sync or runtime recovery have passed.

## Isolated model fixtures

`Tools/test_phase8_save_fixtures.py` checks deterministic cases for: empty remote,
fresh device, newer/older cloud revision, offline unsynced edits, equal-revision
server time and revision tie-breaks, and legacy timestamps. It also parses a
sample v1 document and rejects deliberately malformed JSON using Python's JSON
parser. The workflow runs these cases without accessing any real PlayerPrefs,
login, player profile, or cloud save.

**Important:** these are contract/model fixtures, not Unity serialization,
cloud integration or runtime migration tests. Actual `JsonUtility` behavior,
data-merging interactions, and recovery must be tested in an isolated Unity
environment before final Phase 8 closure.
