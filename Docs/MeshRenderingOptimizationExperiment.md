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
