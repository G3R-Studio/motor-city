# Garage reference layout

The garage HUD follows the supplied 1672 x 941 reference composition, mapped to
the existing 1920 x 1080 canvas. `GarageReferenceLayout` positions and styles the
existing controls once at construction. Vehicle selection, upgrade prices and
levels, customization actions and progression remain connected to live game data.

The layout includes the four header metrics, large right vehicle card with seven
stat rows and mastery, three lower upgrade cards, appearance controls, chevron
navigation, city button and main menu button. The passport action is retained as
an invisible button over the mastery section rather than an extra visible card.

`GarageReferenceGraphic` supplies rounded gradient fills, navigation shapes and
vector icons. All decorative graphics ignore pointer events. The original button
graphics continue to receive clicks, except arrow/city navigation: their visible
child graphics are the hit targets for the existing parent buttons. Colored padlock artwork follows vehicle
availability, and price icon colors follow affordability.

The main menu button calls `ShowMainMenuFromGarage`. Continuing after this return
resumes loaded gameplay without reloading the scene or replaying onboarding.

This change affects UI, not garage meshes, lighting, materials or the camera.
A rendered layout preview uses sample data and Unity API test stubs; it is not
an in-game screenshot. Production layout/icon code compiled against those stubs,
eight layout/state checks and mesh geometry checks for all 26 symbols passed,
four main-menu return/resume checks passed against extracted production methods,
and edited C# files passed syntax checks. A Unity editor/build was unavailable.

In Unity, verify desktop and touch interaction, Russian and English labels,
locked/unlocked cars, affordability and maxed upgrades, the rookie color step,
passport opening, and main-menu return/continue. The existing HUD scaler and safe
area handling provide screen scaling; compare the 16:9 game view to the reference.

The custom graphics explicitly require `CanvasRenderer` and use clockwise UI
triangle winding. Upgrade refreshes preserve the new action-label position and
display numeric cost without the legacy currency prefix. Regression checks cover
renderer presence and winding for all symbols. For runtime diagnosis, open the
garage in Play mode and use `Motor City > Debug > Validate Garage UI`; this audits
renderer presence, rect dimensions, depth, culling and navigation hit targets.
