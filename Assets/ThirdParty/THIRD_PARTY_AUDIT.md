# Motor City third-party release audit

This file tracks release-readiness of third-party source documentation.
It is an audit checklist, not a replacement for the original package/license terms.

## Documented in this repository

### PROMETEO: Car Controller
- Project source note: `Assets/ThirdParty/Prometeo_SOURCE.md`
- Imported source root: `Assets/PROMETEO - Car Controller`
- Runtime dependency: vehicle controller / touch-input bridge.
- Release action: verify the original package terms used for this project remain satisfied.

### Kenney UI / icons
- Project source note: `Assets/ThirdParty/KenneyUI_SOURCE.md`
- Runtime/project artwork is used by Motor City UI and marker systems.
- The existing source note records these assets as CC0.
- Release action: keep the source note with the project.

### Haon SD Series Free Bundle
- Project source note: `Assets/ThirdParty/HaonSDByte_SOURCE.md`
- Imported source root: `Assets/Haons SD series Pack`
- Runtime dependency: Byte/Pixie character visual and spring-bone components.
- Release action: verify the original Asset Store / bundled licensing terms referenced by the source note.

## License files present but not independently cleared

- `Packages/com.unity.springbone/LICENSE` and `LICENSE.md` contain the
  embedded SpringBone package's MIT licence text; the package manifest is
  `Packages/com.unity.springbone/package.json`.
- `Assets/Fantastic City Generator/Documentation/License.pdf` is retained
  with the FCG source assets. Its mere presence and `.meta` entry do not
  establish the terms applicable to Motor City's current source or distribution.
- `Assets/Fantasy Skybox FREE/Readme.txt` identifies the free Asset Store
  listing (18353), but does not by itself complete a licensing review.
- The Git-based SpriteLess UI package is tracked in
  `Packages/packages-lock.json`; its applicable upstream licence still needs
  explicit confirmation before release.

## Source documentation still required before release

The project currently uses or retains the following third-party roots, but no
project-owned source/license note equivalent to the three files above was found
under `Assets/ThirdParty` during the release audit.

### ARCADE: FREE Racing Car
- Source root: `Assets/ARCADE - FREE Racing Car`
- Used by the player-vehicle pipeline.
- Required action: record package/source identifier and the applicable original license/Asset Store terms.

### Fantastic City Generator
- Source root: `Assets/Fantastic City Generator`
- Runtime dependency: baked city, traffic and authoring pipeline.
- Required action: record package/source identifier and the applicable original license/Asset Store terms.

### Gudamore - Free Sports Car
- Source root: `Assets/Gudamore`
- Used by generated/runtime vehicle content.
- Required action: record package/source identifier and applicable redistribution/use terms.

### Eric VFX Studio Magic Circle
- Source root: `Assets/Eric VFX Studio`
- Used by activity/world marker VFX.
- Required action: record package/source identifier and applicable original terms.

### Ubuntu fonts — license present; binary provenance still pending
- Runtime fonts: `Assets/Resources/MotorCity/Fonts/Ubuntu-Regular.ttf` and
  `Assets/Resources/MotorCity/Fonts/Ubuntu-Bold.ttf`.
- The repository **already includes** `ThirdParty/UbuntuFont-LICENSE.txt`
  (Ubuntu Font Licence Version 1.0). Do not add a second potentially
  conflicting copy merely because it lives outside `Assets/ThirdParty`.
- Remaining action: verify that the tracked TTF binaries correspond to the
  identified upstream font release and preserve the licence/notice when
  distributing the font. This audit does not certify their exact provenance.

### Imported vehicle source assets
- Source roots include `Assets/VehicleAssets` and generated player-vehicle prefabs
  under `Assets/Resources/MotorCity/Vehicles/Player`.
- Required action: preserve the source/license information for every imported
  vehicle model/texture pack that contributes to the shipped runtime prefabs.

## Release rule

Do not infer a license from an asset name, a "free" label, or the fact that it
was obtainable from an Asset Store/package page. Before publishing, every
third-party runtime dependency should have a traceable original source and the
applicable terms recorded or retained alongside the project documentation.

Demo/sample content that is not referenced by the current runtime or rebuild
tooling should remain excluded where safe, but source files that are still
required to rebuild tracked runtime assets should not be deleted merely to
reduce repository size.
