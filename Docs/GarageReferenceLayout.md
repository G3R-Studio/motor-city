# Garage reference layout

The garage HUD is constructed directly by `BuildGarage` in
`GarageReferenceLayout.cs`, using the supplied 1672 x 941 composition mapped to
the 1920 x 1080 canvas. `GarageView.cs` only refreshes live game data and passport
content. Vehicle selection, upgrades, customization and progression retain their
gameplay actions.

The garage has no legacy SpriteLess surfaces, separate glow frames, shadow or
outline effects, texture decoration methods, hidden descriptions or offscreen
controls. `GarageReferenceGraphic` draws each rounded gradient panel and its
border in one mesh using the same contour. Navigation surfaces have their own
matching contours. Icons are vector graphics without old sprite fallback layers.
Each button targets its visible graphic. Decorative graphics ignore pointer
input; the passport action uses a transparent hit area over the mastery section.

The main menu button calls `ShowMainMenuFromGarage`; continuing resumes loaded
gameplay. This change concerns the garage UI, not scene meshes, lighting or camera.

Validation: production builder and icon code compiled against Unity API stubs.
Checks passed for the fresh hierarchy, absence of old surfaces and outlines,
single graphic per object, ten gameplay action bindings, hidden initial passport,
stat track dimensions, mastery formatting and geometry for all 26 symbols.
C# syntax and git whitespace checks passed. Unity Play mode was unavailable.

In Unity, verify desktop/touch interaction, Russian/English labels, locked cars,
affordability, maxed upgrades, onboarding, passport and main menu return.
`Motor City > Debug > Validate Garage UI` audits renderer state, dimensions,
button targets and unexpected legacy surfaces or shadow/outline effects.