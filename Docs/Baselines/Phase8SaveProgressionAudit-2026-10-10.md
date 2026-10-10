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

## Unity Editor isolated serialization gate

`MotorCityPhase8SaveSerializationGate.Validate` is invoked by the existing
`MotorCityPhase2BatchGate.Validate` CI step. It exercises Unity
`JsonUtility` on synthetic v1/v2 documents via
`MotorCitySaveService.TryReadCloudMetadata`, verifies dirty/revision flags,
rejects malformed/empty metadata and checks a JsonUtility roundtrip. A report
is written to `Temp/MotorCityAudit/UnityPhase2/unity-phase8-save.txt`.

This **does not** import the fixtures into live `MotorCitySaveService` and
**never writes PlayerPrefs**, invokes the QA reset, or accesses Yandex.
Corrupt-backup persistence and real cloud conflict replay still need a
separate disposable Unity environment before Phase 8 can close.

## Cloud upload failure protection

The Phase 8 cloud runtime now avoids an immediate queued re-upload after a
failed platform save callback. It resets the regular `UploadIntervalSeconds`
timer and leaves unsynchronized local revisions unchanged, so offline/failing
sessions do not spin through back-to-back requests. Successful uploads still
acknowledge only the attempted revision and replay queued changes.

The source contract checks for the delayed failure branch. Real platform
callback/failure timing and cross-device conflict resolution are **not**
covered by this static gate and remain in the Phase 8 integration QA.

## Phase 8 save envelope recovery guard

`MotorCitySaveService` now validates the parsed document envelope (`Version` field present and positive) before reading cloud metadata, importing a remote snapshot, or accepting a local PlayerPrefs JSON. Invalid local envelopes enter the existing corrupt-backup path; invalid remote JSON cannot overwrite the active local document. Unity Editor fixtures cover `{}`, a foreign object, and version zero. The gate does not write PlayerPrefs; real backup-slot and restart behavior still require isolated integration tests. The check is deliberately narrow, not full semantic validation of arbitrary progress values.

## Disposable Unity PlayerPrefs integration check

The Unity Editor serialization gate now calls `MotorCitySaveService.VerifyIsolatedPlayerPrefsRecovery` (editor-only). It creates GUID-namespaced temporary PlayerPrefs storage and backup keys, validates malformed local JSON backup, migrates one legacy integer on read, saves it, clears the in-memory document to simulate reloading, and verifies the saved value. A `finally` block deletes temporary keys and restores all previous service state. The production `MotorCity.Save.Json.v1` and `MotorCity.Save.CorruptBackup.v1` keys are never used by this test.

This is still not a full process restart test and does not exercise live Yandex cloud callbacks or actual user save files.
