# Motor City - WebGL visibility and mesh optimization

Motor City uses Unity 6000.6 / URP 17.6. It targets WebGL 2. Hardware mesh shaders,
GPU Resident Drawer and compute-shader-only approaches are **not** suitable
WebGL 2 deployment optimizations. The existing URP asset keeps SRP Batcher ON.

## Why the browser skyline was empty

The runtime camera used a 2200m far plane in Editor but only 280/350/450m
for WebGL Low/Medium/High. Additionally DayNightCycleController set linear
fog to fully opaque at 520m. Increasing the far plane alone cannot restore
a visible skyline if fog still hides it.

The WebGL visibility policy now uses:

| Quality | Camera far clip | Fog start | Fog end | Foliage hide/restore |
| --- | ---: | ---: | ---: | ---: |
| Low | 900m | 160m | 820m | 245/215m |
| Medium | 1250m | 230m | 1140m | 345/315m |
| High | 1700m | 310m | 1560m | 460/430m (auto culling OFF) |
| Default Editor | 2200m | 120m | 520m | 145/115m (manual A/B only) |

Visibility and fog respond to quality changes **after camera creation**.
Distant buildings are NOT disabled via layer culling. Long-range haze softens
the far-plane boundary. Colliders, authored meshes/materials, shadows,
prefabs, roads, traffic and UI are not rewritten by this change.

## Browser-oriented, conservative detail optimization

On WebGL **Low and Medium**, the existing foliage culler is enabled
automatically, but ONLY for MeshRenderers whose names match
`Plant-*`, `Plants-*`, `Fern-*`, `Vegetation-*`, `Flower-*`.
Renderers with an active LODGroup, missing mesh or material, inactive
objects, or unusually large bounds are excluded. Hiding affects rendering
only; colliders are untouched. A bounded 256-candidate-per-frame update
limits CPU spikes, and every hidden mesh is restored on disable.

The previous 145m hide / 165m restore thresholds were inverted: renderers
in the 145–165m band could oscillate ON/OFF every update. The restore
threshold is now 30m **closer** than the hide threshold to prevent this.
WebGL High leaves vegetation intact.

This is intentionally not a city-wide LOD/HLOD or instancing conversion.
The prefab is authored and interactive, and no validated simplified
building meshes / cluster impostors exist in the repository. Blind mesh
combination would risk lighting, material appearance, collider behavior,
streaming memory and build size. Do not enable static batching,
GPU instancing or aggressive city-wide culling without a representative
browser benchmark.

## Verify in Unity Editor (before making any build)

1. Pull the latest `main` changes. Open the `Prototype` scene,
   press Play and start driving after the city has loaded.
2. At a repeatable high-visibility street position, open Game view
   **Stats** and Window > Analysis > Profiler. Record FPS, frame time,
   Batches/Draw Calls, SetPass, Tris and CPU/GPU times. Leave foliage
   experiment OFF at first.
3. Use `Motor City > Optimization > 6 - Report Visibility (Play Mode)`.
   Baseline Editor should report camera 2200m and fog 120–520m.
4. Use `Motor City > Optimization > 4 - Preview Browser View Distance ON
   (Play Mode)`. Camera/fog should change together. It uses the current
   runtime quality preset. Check near and far buildings at day and night,
   and drive quickly toward the skyline.
5. Repeat Stats/Profiler measurements from the same position. Browser
   visibility can cost **more** Draw Calls than the short original WebGL
   view. This is expected until measured on the target browser.
6. Use `Motor City > Optimization > 1 - Report City Mesh Stats (Play Mode)`,
   then `2 - Enable Foliage A-B (Play Mode)` and compare; use
   `3 - Disable Foliage A-B (Play Mode)` to restore. The Editor toggle
   is separate from browser visibility preview.
7. Use `5 - Restore Editor View Distance (Play Mode)`; check that the
   Editor baseline returns. Quit Play Mode to reset non-persistent preview.
8. Switch Low/Medium/High in the game's Graphics/Quality settings
   **while playing** and repeat menu step 6 to confirm camera and fog
   update in the preview without restarting gameplay. High can look best,
   but may cost more.

The new Editor menu changes **only runtime state**; nothing is serialized.
If the menu is grey/unavailable, load the game and wait for the city.
Do not use an Editor FPS delta as proof of WebGL performance: the target
browser may differ substantially. Later, benchmark GitHub Pages WebGL
Desktop/Mobile builds at matching scene positions with browser profiling.

## Existing rendering baseline

Read-only audit before this change: about 21,778 city MeshRenderers
(9,926 enabled), 578 distinct meshes, 59 distinct materials and just
3 LODGroups. The prior foliage A/B reduced Draw Calls from 14,248 to
13,770 in Editor while frame time remained approximately 7.2–7.4ms.
Thus it is a limited draw-call saving, NOT a proven FPS improvement.
The number of visible triangles in Game view cannot be inferred from
the read-only total referenced-triangles audit.

## Next phase after browser benchmark

Use Frame Debugger + Profiler to identify actual large material/mesh
repetitions. Bake **validated** static, non-interactive per-chunk HLOD
meshes in the Editor (never at WebGL startup), then compare memory,
visuals and draw-call performance before integrating them. Only enable
GPU instancing on materials and shaders where it improves measured WebGL
performance versus SRP Batcher. No speculative HLOD/instancing baking was
shipped by this change.

## Distant tiny-mesh WebGL A/B experiment

Directional browser stalls can be caused by 3D scene complexity, CPU draw submission, overdraw or shadows, not necessarily only triangle count. On WebGL (including High), a reversible detail culler now hides only very small projected opaque MeshRenderers beyond a substantial camera distance. It excludes LODGroups, static batches, large objects, attached colliders/scripts, transparencies and architectural/gameplay names; it preserves the full building silhouettes, roads, traffic and physics. Work is spread over multiple frames and only revisited after the camera moves. It uses Renderer.forceRenderingOff and restores only its own flags. It is an A/B experiment, **not** a proven FPS boost or proper HLOD. Any visible pop-in is reason to disable it.

**Editor Play Mode:** enable browser visibility preview (menu 4), then use menus 7 / 8 to toggle distant detail and 9 to report hidden renderers and referenced triangles. Let scanning complete before comparing identical viewpoints. The editor's default is OFF.

**WebGL browser after a new release:** compare https://g3r-studio.github.io/motor-city/desktop/ (ON by default) against https://g3r-studio.github.io/motor-city/desktop/?motorcity_detail_cull=off (OFF). Test at the same save position and quality preset, after a few seconds of settling. A/B URLs work only on a *newly built* release, not the old published binaries. If this experiment is inadequate, prioritize validated offline building HLOD and captured browser profile data rather than hiding major city silhouettes.


## Offline city HLOD prototype (Editor A/B, no WebGL build required)

An opt-in HLOD baker and a renderer-only runtime swap have been added:

- `Assets/Editor/MotorCityHlodBaker.cs`: inspect and bake the imported
  `Assets/Resources/MotorCity/Environment/CityVisual.prefab` **in Edit Mode**.
- `Assets/Scripts/World/CityHlodData.cs`: stores proxy meshes/materials,
  source renderer indices/names/centers and triangle totals.
- `Assets/Scripts/World/CityHlodRuntime.cs`: initializes baked proxy meshes
  at world origin; swaps renderers only beyond 360m/450m/560m
  (Low/Medium/High), with +/-45m hysteresis and <=12 cell checks/frame.
- `MotorCityVisibilityRuntime.cs`: WebGL automatically uses HLOD only when
  a successfully baked data asset is included; no asset means no HLOD.
  To opt out in a future release, append `?motorcity_hlod=off` to the
  `/desktop/` browser URL. The tiny-detail culler remains separate.

### Editor test sequence

1. In Unity Editor, exit Play Mode. Pull the latest source commit and let
   Unity compile without red Console errors.
2. Run `Motor City > Optimization > HLOD > 1 - Audit Candidates (Edit Mode)`.
   Check counts in Console. Candidate selection is conservative and may
   return few objects depending on the authored FCG prefab hierarchy.
3. Run `Motor City > Optimization > HLOD > 2 - Bake Proxies (Edit Mode)`.
   The operation may take a few minutes and creates
   `Assets/Resources/MotorCity/Environment/HLOD/CityHlodData.asset`
   containing subasset meshes. **This step must run inside your Unity Editor**.
   Only the new HLOD asset is written; the city source prefab and colliders
   are not edited. Generated data remains *local* until committed separately.
4. Enter Play Mode, start city gameplay, enable browser visibility preview
   (main Optimization menu **4**) and use HLOD menu **3 - Enable A-B**.
   Keep tiny-detail culling A/B the same for both HLOD measurements.
5. In the same camera location/direction, compare Game View Stats, CPU/GPU
   frame time, Tris, Batches/Draw Calls. Use HLOD menu **5 - Report**,
   **4 - Disable A-B**, and **3 - Enable A-B** to compare.
   Wait for all cell switches to settle, and drive across a cell boundary
   to check popping, nighttime window illumination, road silhouettes.
6. If a proxy group looks wrong, turn HLOD menu **4** OFF. Original meshes
   are restored; no prefab or physics state is changed.
7. Do not schedule a WebGL build until Editor quality/performance improves,
   the game reopens cleanly, the baker-created `.asset` and all required
   Unity `.meta` files have been committed, and source gates pass.

### Safety limits and honest scope

The baker uses 120m spatial sectors of **opaque** MeshRenderers,
skipping roads, terrain, vehicles, foliage, night glass, attached scripts,
property blocks, LODGroups, lightmapped sources, and unusually large meshes.
Candidate MeshRenderers may have up to 8 source submeshes, provided every
material is safely opaque and not protected. Up to 8 distinct materials are
preserved per proxy: submeshes are combined by **original shared material**
and **original world matrices** inside each sector. A single proxy may have
multiple submesh/material slots, so the renderer is never switched off while
omitting a transparent material. Grouping by sector rather than individual
material allows buildings that have more than one opaque material to share
one far-distance proxy renderer. A very conservative vertex-cluster pass
may collapse small triangles when positional, normal AND UV neighborhoods
match; otherwise the exact combined geometry is retained. This is a
draw-call-oriented first HLOD, not guaranteed aggressive triangle decimation.
No third-party mesh decimator, compute/mesh shader or Unity runtime mesh
combination is used.

Each bake is capped at 320 proxy cells and ~1 million proxy triangle
references to prevent uncontrolled WebGL memory growth. The runtime validates
renderer count, indices, names and bounds centers before touching visibility;
a mismatched city revision fails closed. Original collider and gameplay
components continue to run even when render meshes use far proxies.
Distances are conservative to keep close-up appearance unchanged.

**Do not claim browser FPS improvement from Editor numbers.** If the proxy
replacement is barely faster, do not ship unnecessary mesh duplication.
A high-quality authored silhouette decimator or per-building reduced meshes
may be needed for meaningful GPU triangle reduction.

### HLOD candidate audit: imported FCG multi-material mesh support

The first candidate audit on the current CityVisual asset found only 33 of
25,634 MeshRenderers eligible, and zero proxy groups. Among the rejection
reasons were 10,022 disabled-in-prefab renderers, 5,890 protected names,
4,977 objects outside the architecture size limits, 2,749 alpha/cutout
materials and 1,880 multi-submesh renderers. The disabled objects are
intentionally preserved; do not activate hidden prefab hierarchies to raise
candidate count. Opaque multi-submesh renderer support and 120m sector
clustering were added to address a major source of false rejection, while
retaining protected glass, gameplay and environment exclusions. It does NOT
imply the previously rejected 1,880 renderers are all safe to bake.

**Rerun Audit Candidates after pulling the new code, BEFORE baking.** The
updated report shows eligibility, sector count and largest sector. Only
bake if enough sectors qualify and their likely draw-call savings justify
the extra proxy meshes; there is no requirement to keep HLOD if profiling
shows little benefit. The existing distance-detail culler targets tiny
renderers (bounds radius <=7m), while HLOD continues to require a radius
above 7.1m to avoid overlapping renderer-visibility ownership.


## CityVisual geometry census: optimize parked vehicles before architecture HLOD

The 2026-10-13 local CityVisual census found 25,634 mesh renderers,
15,612 active/enabled, and 4,168,191 triangle references (the sum for the
entire active prefab, NOT a camera frame). Of these, meshes with bounding
radius <7m contained about 3.01 million referenced triangles. Material
combinations FCG_Traffic-Car-Atlas (1,142,054), FCG_Tires (373,520),
FCG_Tires+FCG_Traffic-Car-Atlas (222,252), and Trees-01 (601,160)
together account for around 56% of referenced triangles.

Architecture HLOD Candidate Audit still returned only 34 eligible renderers,
1 bakeable chunk, and 3 potential draw slots -> 1. Do NOT bake or ship
architecture HLOD yet: its likely benefit is too small, and the source
windows are night-animated/protected. These are global prefab counts; actual
rendering costs need per-frame measurements.

An opt-in parked-vehicle rendering A/B test is now available via
`CityParkedVehicleCullingExperiment`. It only recognizes decorative
vehicle assemblies under `Park-*` within `MotorCity_FCGCity`,
using a `Car` or `Car ON` body mesh with the authored
`FCG_Traffic-Car-Atlas` material. It then hides the whole static vehicle
assembly at long camera distances, retaining original colliders and all
GameObjects. Ancestors `CarContainer`, scripted vehicle roots,
Rigidbody-owned vehicles, and existing LOD groups are excluded. No baked
asset is created and no gameplay scripts or materials are modified.

This experiment is OFF by default, even on WebGL. It is not yet validated
as a production feature and must not overlap with other culling A/B tests.

To test without a 20-minute WebGL build:

1. In Unity Editor Play Mode, load the full city and match the original
   high-density camera position. Use `Optimization > 4` to preview the
   browser view distance. Keep the existing distant detail and HLOD A/B
   experiments OFF for this comparison.
2. `Motor City > Optimization > 10 - Enable Parked Vehicles A-B`.
   Wait until scan-complete is True; note number of static parked vehicle
   groups discovered and reject counters (body meshes, under Park-*,
   scripted/dynamic excluded).
3. `Motor City > Optimization > 12 - Report Parked Vehicles`.
   Record hidden/eligible vehicle groups and estimated triangle references;
   these are not actual rendered triangles.
4. Compare Frame Debugger, Game view Stats (Tris, Batches/Draw Calls),
   Profiler CPU/GPU time and FPS while facing the same dense area.
5. `Motor City > Optimization > 11 - Disable Parked Vehicles A-B`.
   Record baseline at same viewpoint and verify all parked vehicles return.
   Drive toward parking lots and verify there is no pop-in nearby.
6. If the experiment meaningfully reduces frame time and looks correct,
   consider enabling it selectively in production in a later commit,
   separately from the existing small-detail culling. Otherwise keep it off.

No Unity compilation or browser FPS improvement has been asserted.

### Automated 4-phase parked-vehicle frame-time comparison

After the initial parked-vehicle A/B report of **305/455 hidden vehicle
groups** and **404,800/603,956 hidden triangle references**, use
`Motor City > Optimization > 13 - Benchmark Parked Vehicles A-B (Play Mode)`.
This is Editor-only and never runs in a WebGL build.

- Select the problem camera viewpoint in Play Mode and the intended graphics
  preset with browser view-distance preview enabled (menu 4).
- Disable foliage (menu 3), distant tiny-detail culling (menu 8) and
  HLOD (HLOD menu 4). The launcher checks for any that remain enabled.
- Activate menu 13 and keep the Game view focused with the camera stationary.
  The test waits for a culler scan, then automatically compares
  OFF / ON / ON / OFF. Each phase has a 2.5s warmup and 12s sampling,
  about 58 seconds overall. No input/movement during the test.
- If the camera moves more than 2m or rotates by more than 4 degrees
  from its start, the test aborts rather than claiming comparable results.
- Read the `[MotorCity][ParkedBenchmark] FINAL` Console entry. It combines
  two OFF periods and two ON periods; reports frame sample counts, mean
  frame interval, FPS derived from frame times, median/p95/p99 frame time,
  and frames above 25/50 ms. Positive improvement means the ON frame
  interval was shorter than OFF.
- The runner restores the A/B culler's original enabled state on completion
  (or abort) and deletes any temporary culler that it created itself.
  It never rewrites prefabs, materials, meshes or colliders.

These are **Unity Editor frame intervals**, not a measured browser
framerate or GPU-only cost. Graphics Editor panels, profiler recording,
vsync, dynamic simulation and app focus can affect the data. Require
consistent improvement in mean *and* p95/p99 and no visible parked-car
pop-in before considering enabling this experiment on WebGL.


### Automated combined-culling benchmark: no screenshots required

Run all relevant renderer A/B comparisons using one editor script:

1. Pull the latest `main`, wait for Unity Editor compilation, enter Play
   Mode, load the city and stop at the reproducible problematic viewpoint.
   Select **High** quality before starting; changing it mid-test could
   also change traffic budgets.
2. Choose `Motor City > Optimization > 14 - Run All Culling A-B Tests
   (Play Mode)`. You do NOT have to manually toggle menus 7/8/10/11,
   nor manually enable browser visibility preview.
3. Wait ~80-95 seconds. Do not move/rotate the camera or switch focus
   away from the Game view. The script tests eight phases:
   **baseline > tiny details only > parked cars only > both >
   both > parked cars only > tiny details only > baseline**.
   Every phase has 2s warmup followed by 8s frame-time measurement.
4. At completion, open the automatically generated Unity project file
   `Temp/MotorCity_CombinedCullingBenchmark.txt` and send the TXT
   file or paste the `[MotorCity][CombinedBenchmark] FINAL` log.
   No screenshots, manual FPS transcription, or WebGL build are needed.
5. The runner aborts if the camera moves >2m, rotates >4 degrees,
   High is changed or preview is turned off. It also ensures no
   simultaneous parked/detail discovery and automatically disables and
   restores preexisting foliage/HLOD during measurements.
6. Both A/B components and previous preview states are restored
   afterward; temporary helpers are destroyed, source geometry,
   colliders, materials, authored prefab and saved quality are unchanged.

The combined test also prevents culler-ownership conflicts: the tiny
detail culler explicitly skips parked vehicle wheels/body meshes inside
`Park-*` groups so only the parked-car culler can control those
renderers. Since its candidate list changes, previously reported
single-optimization results should not be directly subtracted from
the new script results.

**Interpretation:** Each mode reports two aggregate sample periods,
derived FPS (1000 / mean frame milliseconds), median, p95, p99 and
frames over 25/50ms, plus the number of hidden renderers/cars and
triangle references. Only comparing stable, same-camera frame-time
distributions can support Editor improvements. Even a successful Editor
test does not guarantee the same WebGL result.


## Distant Trees-01 A/B experiment (Editor, not production)

The CityVisual geometry census counted about 601,160 triangle references in
the `Trees-01` material group. Those are whole-city counts, not GPU triangle
counts or proof that every tree causes browser stalls.

`CityDistantTreeCullingExperiment` scans only active, unscripted,
non-static-batched `MeshRenderer` objects whose names begin with
`Tree` or `Palm` **and all renderer materials begin with `Trees-01`**.
Mixed-material trees are excluded rather than hiding half a tree. Also
excluded: colliders attached to the renderer, authored LODGroups, meshes
whose world-bounds radius exceeds 12m and renderers already hidden by
another system. Gameplay, meshes, road props, cars, source prefab, and
physics are unchanged. Uses only reversible `forceRenderingOff` flags.

The High preset allows hiding only beyond **500m** and when the
projected 1080p mesh diameter is below **12 pixels**; 50m distance and
35% projected-diameter hysteresis prevents rapid toggling. Medium and
Low have their own more aggressive experiment thresholds, but the
one-click A/B benchmark intentionally tests **High only**.

To avoid two cullers owning one `forceRenderingOff` flag, the tiny-detail
culler reserves these exact Trees-01 renderers for the tree experiment
**only in the Unity Editor**. WebGL's existing tiny-detail rules remain
unchanged; the separate tree experiment is NOT wired into WebGL startup.

### Automatic benchmark

1. Run `git pull origin main` and let Unity compile. Do not build WebGL.
2. Enter Play Mode, load the city, set graphics to **High**, stand at the
   reproducible dense-city location. Do not move the camera during test.
3. Choose `Motor City > Optimization > 15 - Benchmark Trees + Existing
   Optimizations (Play Mode)`.
4. The script turns on the browser-visibility Editor preview automatically,
   disables any previous HLOD or foliage A/B, and scans parked cars, small
   details and trees *sequentially* before starting.
5. It benchmarks four phases: **details+parked**, **details+parked+trees**,
   **details+parked+trees**, **details+parked**. Each phase has 3 seconds
   warmup plus 12 seconds of frame samples; runtime ~60s plus scanning.
6. Read `Temp/MotorCity_TreeCullingBenchmark.txt`. The report includes
   eligible/hidden trees, skipped triangle references, each phase's metrics,
   and aggregate mean frame time, p95, p99, derived FPS, >16.67ms,
   >25ms and >50ms counts. Send the TXT report for interpretation.
7. The benchmark aborts if camera moves over 2m or rotates over 4deg,
   or High quality/preview is changed, and restores all previous culler
   enabled states and the Editor preview after completion or abort.
   No Unity assets or build files are modified.

Even if the Editor benchmark improves, verify the road-level visual
quality and skyline before shipping. Exact WebGL behavior can only be
determined with a final browser build after Editor experiments conclude.


## Automated tree geometry diagnosis (READ ONLY, Play Mode)

The first Trees-01 benchmark at High returned only **4 eligible tree
renderers**, **1 hidden** and **138 triangle references hidden**. It did
not improve frame time: 5.49ms with small details+parked vehicles vs
5.57ms with the additional experimental tree culler. Do not ship the
tree experiment yet or keep blindly changing its distance thresholds.

To inspect the actual reasons for candidate rejection, choose
`Motor City > Optimization > 16 - Diagnose Tree Geometry (Play Mode)`.
Run in the loaded MotorCity game scene with a camera; it is not an
FPS benchmark and does not change enabled renderers, scene objects,
prefabs, meshes, materials, or build settings.

The command writes `Temp/MotorCity_TreeGeometryAudit.txt` in the Unity
project. The report contains:

- Total live MeshRenderers, and the union of renderers with a Tree/Palm
  name or at least one Trees-01 material.
- Exact first-failing filter stage of the current tree experiment for
  every inspected renderer, with sample full hierarchy paths, material
  names, dimensions, estimated distance and pixel size, and triangle
  reference counts.
- Independent totals for candidate name/material matches, active state,
  static batching, LODGroup and eligibility *hypothetically* without
  the restrictive Tree/Palm object-name filter.
- Material-family and object-name distributions and the most expensive
  tree-bearing meshes.
- Number of actual candidates and potential hidden meshes in the
  current camera position using the conservative High >500m and <12px
  projected-diameter thresholds.

No filter will be relaxed until the TXT reveals which classes of
renderers dominate cost; name-relaxation counts are only diagnostics
and do not imply those objects are safe to hide. This avoids risking
vegetation integrated into buildings, gameplay assets and skyline.
