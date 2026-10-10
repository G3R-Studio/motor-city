# Phase 11 — tracked assets, binary duplicates and package inventory (2026-10-11)

**Scope:** read-only GitHub `main` tree and package metadata snapshot, supplemented by the pre-existing Editor dependency report and `Assets/ThirdParty/THIRD_PARTY_AUDIT.md`. This is **not** a Unity `AssetDatabase.GetDependencies` runtime certification, a source-license clearance, or a safe-to-delete list. Nothing was removed, moved, regenerated or converted to Git LFS.

## Repository snapshot

- Git tree: **4,516 tracked blobs** total, **4,271 under `Assets/`**, including **1,784** tracked model/texture/audio/font/material/prefab files in the extension scope of the new `Tools/check_phase11_asset_inventory.py`. Git object IDs identify identical repository bytes; hydrated LFS payloads and separate importer settings require additional checks.
- **Three identical Git blob groups (7 files)** in the tracked media/model/material scope:

| Paths | Git blob bytes | Why the files stay |
| --- | ---: | --- |
| `Assets/Eric VFX Studio/Resource/Textures/circle2.PNG` and `Assets/Resources/MotorCity/UI/Loading/circle2.PNG` | 68,456 each | Separate original-VFX and runtime Resources locations; removing one changes its GUID or path and may break serialized/dynamic consumers |
| `Assets/VehicleAssets/AmgGT/all.png`, `Beatall/all.png`, `Delorean/all.png` | 4,096 each | Same tracked pixels, separate vehicle import roots and Unity texture importer metadata; don't rewire model materials automatically |
| `Assets/VehicleAssets/Porsche996/rear_wheels.mtl` and `ToyotaAE86/front_wheels.mtl` | 607 each | Separate OBJ/MTL importer roots; same text does not prove identical import-time or runtime dependencies |

- `Assets/VehicleAssets/Hybrid/wheels1.mtl` and `wheels2.mtl` were inspected by Git object identity: **they are not exact tracked-blob duplicates**. Do not treat similarly named import sources as duplicates.
- Current authored-city files: `Assets/Resources/MotorCity/Environment/CityVisual.prefab` **103,001,717 bytes**, and `Assets/LocalGenerated/FCG_Workbench.unity` **97,978,478 bytes** as ordinary tracked blobs. Both are below GitHub's 100 MiB per-object limit (104,857,600 bytes) but close enough to warrant an explicit Git LFS *migration plan* before future growth. They are distinct authored/runtime layers and **neither should be deleted or automatically LFS-converted**. See `.gitattributes`.

## Third-party packages and licenses

- `Packages/manifest.json`: **13 direct dependencies**. `Packages/packages-lock.json`: **38 resolved entries** (29 builtin, 7 registry, 1 embedded, 1 git).
- The embedded package `com.unity.springbone` is sourced from `Packages/com.unity.springbone` with its local package manifest/license files; `com.unity.toonshader` remains a registry preview package; SpriteLess UI is a pinned lockfile entry from a Git URL. These are **inventory facts, not usage or compatibility verdicts**.
- Third-party roots retained in `Assets/` include ARCADE - FREE Racing Car, Eric VFX Studio, Fantastic City Generator, Fantasy Skybox FREE, Haons SD series Pack, PROMETEO - Car Controller, SapphiArt and vehicle source assets. Having a package root does not prove it ships in the player; absence of C# references does not prove that removal is safe.
- `Assets/ThirdParty/THIRD_PARTY_AUDIT.md` documents Prometeo, Kenney, Haon and identifies missing definitive source/license verification for ARCADE, FCG, Eric VFX, imported vehicle packs, and others. **No license conclusion** is made by this inventory.

## Verification boundary

`Assets/Editor/MotorCityBuildDependencyReport.cs` already supports a manual **Tools → Motor City → Build → Generate Dependency Report** and post-build report. Its scene graph and authored resource/vehicle sections are useful but cannot alone prove that string-based `Resources.Load`, Editor importer/rebuild flows, scene serialization and cloud/save effects have no consumers.

The new `Tools/check_phase11_asset_inventory.py` is a **source-only Git check**:
1. Inventories exact tracked Git blob duplicates without deleting or rewriting files.
2. Verifies that protected resources/city source and their unique Unity `.meta` GUIDs remain present.
3. Checks direct package entries against the lockfile, including embedded and Git-based dependencies.
4. Writes `Temp/MotorCityAudit/phase11-asset-inventory.json` in CI for review.
5. Emits explicit warnings that **zero candidates are approved for deletion**.

**Open gates:** Unity authoritative dependency/reimport trace for any proposed duplicate consolidation; licensing provenance per package/model; precise LFS migration scope and history impact; native + WebGL runtime regression before a separately revertable asset deletion. No physical files should be removed to force a Phase 11 completion checkbox.
