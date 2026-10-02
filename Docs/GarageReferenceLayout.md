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
Resize handling: `GarageCanvasRefresh` invalidates all garage text generators on
window/canvas scale changes and shared font atlas rebuilds, including static
labels. Best Fit is disabled. Completed upgrade actions use 26 reference pixels,
a full action-strip rectangle and centered alignment; non-maxed actions restore
the purchase layout. Vector surfaces/icons include transparent coverage fringes
scaled to one screen pixel, without additional frame/glow objects. Stub regression
checks also cover resize, atlas invalidation, idle stability and all AA meshes.

Garage labels explicitly allow vertical overflow so scale-dependent glyph line
height cannot discard a whole line. Short labels/numbers use horizontal overflow;
multiline descriptions retain wrapping. The level number has room for three
and four digits, with its progress track moved to avoid overlap. Removing Best Fit
alone had exposed the default Text truncation in tight header rectangles.

Upgrade tracks distribute five segments across their full width with equal, pixel-aligned gaps on resize. White chevrons are single joined polygons rather than two independent strokes.

All generated game UI uses MotorCityTypography: Roboto Condensed Regular/Bold from googlefonts/roboto-2 (Apache 2.0; license bundled). Garage uses the real Bold font with FontStyle.Normal. Reference similarity is a visual approximation, not confirmed identification.

Garage camera framing targets the center of the open area between navigation arrows, header and upgrades (633.5,394 reference pixels), using an aspect-aware view ray. Applied on entry and ongoing orbit updates; driving framing retains its existing behavior. Unity visual verification is still required.
