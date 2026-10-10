# Phase 11 — Unity duplicate dependencies and license evidence (2026-10-11)

**Verification:** [Unity 6 CI run #247](https://github.com/G3R-Studio/motor-city/actions/runs/38093971566) at `20aa813` — **SUCCESS**, including source/UI and Unity Editor batch validation; **WebGL build skipped**. The read-only report is printed in the Unity job and saved as `unity-phase11-asset-dependencies.txt` in [the Unity artifact](https://github.com/G3R-Studio/motor-city/actions/runs/38093971566/artifacts/11685480816). No assets were deleted, moved, reimported, replaced, or given new GUIDs.

## Unity evidence for all seven byte-identical source files

The repository's [tracked-blob inventory](Phase11AssetPackageInventory-2026-10-11.md) previously identified three exact-duplicate groups (seven files). The new `Assets/Editor/MotorCityPhase11DependencyAudit.cs` combines `AssetDatabase` importers and dependency graphs with explicit resource/OBJ sidecar checks.

| Asset | Confirmed usage / why protected |
| --- | --- |
| `Assets/Eric VFX Studio/Resource/Textures/circle2.PNG` | `TextureImporter`, distinct GUID `add11dd29f8829e4fb55d705d38d6600`; serialized dependency of Eric VFX prefabs/materials and reachable through `Resources`-root dependency closure; **mipmapEnabled=true** |
| `Assets/Resources/MotorCity/UI/Loading/circle2.PNG` | `TextureImporter`, GUID `6f64f0b8780d4d448fd9a2ca9953bc58`; `MotorCityFrontEndFlow` explicitly loads `Resources.Load<Texture2D>("MotorCity/UI/Loading/circle2")` (dynamic path). **mipmapEnabled=false**; it is deliberately excluded from the other-Resource-roots graph because it is itself a root |
| `Assets/VehicleAssets/AmgGT/all.png` | GUID `a7ff144c69909fa4c93a0376851deae2`; present in player prefab and Resource dependency closures; `amggt.mtl` also refers to `map_Kd all.png` |
| `Assets/VehicleAssets/Beatall/all.png` | GUID `e48ac31a867505c4cbfcf39fe472c3ef`; present in player prefab and Resource dependency closures; direct `BeatallVehicleImporter.SourceTexture` plus `beatall.mtl` uses `map_Kd all.png` |
| `Assets/VehicleAssets/Delorean/all.png` | GUID `04604ef5086d61d4dad326fdcdb84ba6`; present in player prefab and Resource dependency closures; `delorean.mtl` uses `map_Kd all.png` |
| `Assets/VehicleAssets/Porsche996/rear_wheels.mtl` | GUID `ad0b624a0a1ba9c4fbde43cd8a10225f`; not in scanned serialized closures, **but** `Porsche996/rear_wheels.obj` directly states `mtllib rear_wheels.mtl` |
| `Assets/VehicleAssets/ToyotaAE86/front_wheels.mtl` | GUID `debb0c11d2e397349a1eb699e98d634c`; not in scanned serialized closures, **but** `ToyotaAE86/front_wheels.obj` directly states `mtllib front_wheels.mtl` |

**Scope and counts from the actual Editor report:** enabled build scenes **1** root / **1** transitive dependency; other Resources **213** roots / **831** transitive dependencies; player vehicle prefabs **9** roots / **41** dependencies; Eric VFX prefabs/materials **8** roots / **17** dependencies. These categories intentionally don't include every possible Editor code, importer, string-loaded runtime asset, scene variant, or Build Profile. Thus **NOT_IN_SERIALIZED_GRAPH never means safe-to-delete**.

**Decision:** keep **all seven** files and their GUIDs. Even identical bytes are not interchangeable without a planned importer/source migration and regression pass. The compiled Unity gate verifies the existing 7 paths, distinct GUIDs, two differing mipmap policies, `Resources.Load` name and five MTL/OBJ sidecar contracts. It performs **no repairs**.

## Third-party source/license review, repository evidence only

| Component | Evidence in repository | Status and remaining action |
| --- | --- | --- |
| Unity SpringBone | `Packages/com.unity.springbone/LICENSE`, `LICENSE.md` contain the MIT license and copyright line; embedded package manifest present | License text **present**; keep notices in distribution, verify included code/samples and downstream obligations |
| Ubuntu Regular/Bold fonts | `ThirdParty/UbuntuFont-LICENSE.txt` contains **Ubuntu Font Licence 1.0**; both runtime TTF and Unity `.meta` entries exist | The old TODO saying the license was absent was stale; verify exact font source/version and retain license notice; not a legal clearance |
| Fantastic City Generator | `Assets/Fantastic City Generator/Documentation/License.pdf` is tracked (with meta); source and bake pipeline remain necessary | Bundled PDF **exists**, but matching current authored assets and redistribution terms require independent manual license review; don't infer rights from `.meta` `licenseType` |
| Fantasy Skybox FREE | `Assets/Fantasy Skybox FREE/Readme.txt` identifies the free Asset Store listing (18353) | Source information present; applicable distribution terms still need review |
| Prometeo, Kenney, Haon | Project source notes in `Assets/ThirdParty`; Haon refers to Asset Store/source terms | Source evidence exists, not a blanket license certification |
| ARCADE Free Racing Car, Eric VFX, Gudamore, imported vehicle packs | Existing package roots + outstanding items in `Assets/ThirdParty/THIRD_PARTY_AUDIT.md` | Definitive original source/version and distribution terms **still open** |
| SpriteLess UI, Toon Shader | Git/Unity registry lockfile entries in `Packages/packages-lock.json` | Verify exact source license/version and WebGL compatibility separately; never remove based on unused-symbol search alone |

This audit did not change third-party binary content or license files. It reconciled existing license notices so release-blocking TODOs reflect what is actually present.

## Residual gates

1. Before any proposed asset deletion, review the specific original licence and importer/rebuild path; find all GUID consumers, including dynamic `Resources.Load` and player/build assets; run Unity reimport and visual regressions on an isolated branch.
2. No LFS conversion for `FCG_Workbench.unity` or `CityVisual.prefab` without explicit history/CI migration plan. Their source and generated-runtime roles are distinct.
3. WebGL Desktop/Mobile Release, real devices, material/shader/font visuals and full Release matrix remain Phase 12. Static/editor audits cannot certify them.

**Summary: exact duplicate identification + Unity serialized/dynamic/OBJ sidecar evidence complete for the seven known copies; 0 deletion candidates approved. Licence/source provenance and targeted build regressions remain open.**
