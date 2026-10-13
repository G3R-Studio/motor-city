# Motor City — Mesh rendering optimization experiment

Motor City uses Unity 6.6 / URP 17.6 and targets browser builds, including
Yandex Games. Hardware **mesh shaders** are not supported by WebGL 2 and
cannot be a production optimization for this game. Mesh shaders are *not*
the same thing as ordinary Unity MeshRenderers, HLSL vertex/fragment
shaders, GPU instancing or the SRP Batcher.

## Current rendering baseline

- The `Assets/Settings/MotorCityURP.asset` pipeline already enables
  SRP Batcher (`m_UseSRPBatcher: 1`).
- GPU Resident Drawer is disabled; it requires a compute-capable graphics
  API and is not a WebGL 2 replacement.
- URP render scale is 0.92, MSAA 2, and 75m shadow draw distance.
- The authored FCG city contains roughly 26,000 renderers. Exact loaded
  counts are printed by the audit below.

Do not enable every batching mechanism blindly: some overlap, can increase
memory, or have little effect when SRP Batcher already handles materials.

## Zero-risk Editor A/B test

In Unity Editor:

1. Pull Git changes and open Play Mode; wait for the city to finish loading.
2. Choose `Motor City > Optimization > 1 - Report City Mesh Stats (Play Mode)`.
   This reports mesh reuse, materials, approximate triangles, static batches,
   LOD groups and missing mesh assignments (read-only).
3. In Game view, turn on `Stats`; note **Batches**, **SetPass**, **Tris** and
   FPS while looking at a street with lots of foliage. Use the Unity Profiler
   (Rendering, CPU and GPU) for reliable frame-time comparisons.
4. Select `Motor City > Optimization > 2 - Enable Foliage A-B (Play Mode)`.
   Drive the same camera route and repeat measurements.
5. Select `Motor City > Optimization > 3 - Disable Foliage A-B (Play Mode)`.
   Any hidden decorative renderers are restored, without changing colliders.
6. If it helps, capture *same-place/same-camera* profiler timings over
   several seconds in both states. Only enable any optimization by default
   after benchmarking.

The experiment affects **only renderers** named `Plant-*`, `Plants-*`,
`Fern-*`, `Vegetation-*`, or `Flower-*`, and skips missing meshes,
inactive renderers, active LODGroups and unusually large bounding boxes.
It hides those farther than 145m from the camera, restores within 165m,
and processes at most 256 candidates per frame. There is no modification
of `CityVisual.prefab`, roads, the garage, jobs, markers, collisions, input,
materials or runtime loading. Default state is OFF in all builds.

If FPS/CPU/GPU time does not improve or the city looks noticeably emptier,
leave it OFF. Do not confuse reduced triangle reference counts with real
on-screen GPU work: Unity's native frustum culling and SRP Batcher already
handle many objects.

See Unity docs: GPU optimization
https://unity.com/how-to/gpu-optimization
and GPU Resident Drawer limitations
https://docs.unity.com/en-us/engine/6000.0/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/reduce-rendering-work-on-cpu/gpu-resident-drawer
