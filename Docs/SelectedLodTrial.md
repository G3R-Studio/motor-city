# Motor City – isolated LOD trial (not shipped in WebGL)

This is an **opt-in Editor experiment** on branch
`experiment/safe-selected-mesh-lod-20261014`.
It does not change the runtime bootstrap, WebGL quality presets, existing
detail/parked/vegetation cullers, `CityVisual.prefab`, colliders or physics.
It is not yet a city-wide automatic LOD solution.

## How to test

1. Pull this branch and open Unity. Exit Play Mode.
2. Put a **copy of a static mesh object into a test scene**, outside the
   imported CityVisual prefab asset. Select its GameObject (not a parent
   containing other MeshRenderers). The city is instantiated at runtime,
   so you cannot select its generated objects in Edit Mode. You can drag
   a suitable mesh/prefab instance into a temporary test scene.
3. Choose **Motor City > Optimization > LOD Trial > 1 - Audit Selection**.
   The mesh must be readable, opaque, single-material/single-submesh,
   unbatched, not lightmapped, without tangents/vertex colors, scripts,
   Rigidbody or existing LODGroup. The filter deliberately rejects
   windows, foliage, vehicles and roads until materials are validated.
4. Choose **2 - Generate Selected LOD1** (1–8 objects). A coarsely
   quantized vertex-cluster mesh is generated under
   `Assets/MotorCityGenerated/LODTrials/` **only if at least 15% of
   triangles collapse**. Each successful object gets LOD0 (original,
   above 18% relative screen height) and LOD1 (proxy, down to 1%).
5. Compare near/far silhouettes, UVs, normals, shadow behaviour,
   Stats/Profiler, memory and camera-motion hitching. The generated
   LOD1 preview does not cast/receive shadows; this is a known visual
   difference to judge. Any visible holes or shading problems mean the
   proxy is unsuitable.
6. To revert, use Undo immediately or remove the LODGroup and the
   `__MotorCity_LOD1_Preview` child; delete any unused generated
   .asset files manually. Undo **does not delete saved mesh assets**.
   Do not save scene modifications intended only for the experiment.

## Limitations

This first version is a deliberately conservative geometry-clustering
experiment, **not** authored mesh decimation or guaranteed production
LOD. It often rejects geometry or fails to reduce triangles because UV
and normal seams are preserved. It cannot simplify protected multi-material
buildings and does not automate LOD on the runtime CityVisual instantiation.

Do **not** merge or deploy this experiment as a WebGL performance feature
until a visually correct representative mesh produces measurable results
in Editor and on the actual mobile browser. Existing WebGL optimizations
remain the established baseline.
