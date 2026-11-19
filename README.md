# Motor City

**G3R Studio · Version 1.1 · Unity 6.6 · WebGL 2**

Motor City is an open-world city driving game. Explore the streets, complete activities, earn credits and reputation, and build your collection of cars.

[Play Motor City on Playgama Sandbox](https://playgama.ai/play/nkvqxlch7s76z6)

> Version 1.1 is available in Sandbox. Full catalog release and mobile compatibility remain subject to validation.

## Gameplay

- 10 vehicles, garage upgrades and vehicle customization.
- Open city exploration, delivery jobs, racing, drifting and other activities.
- Dynamic traffic, day/night cycle, navigation and headlights.
- Keyboard, gamepad and touch controls.
- Persistent player progress through Playgama Bridge Storage.

## What's new in 1.1

This update focuses on loading speed, rendering performance and browser stability.

- Deferred city loading: the main menu starts before the city AssetBundle downloads.
- Streamed menu and city music, optimized textures and Brotli-compressed WebGL player.
- Distance-based culling for city details and parked vehicles.
- Graphics presets controlling render scale, shadows, draw distance and traffic density.
- Playgama Bridge integration for saves, advertising and audio.

### Measured improvements

| Metric | Before city split | Version 1.1 split build |
|---|---:|---:|
| Initial WebGL data + WASM (local build) | 45.08 MB | 22.11 MB |
| Player data (Sandbox Network) | 39.19 MB | 15.76 MB |
| Playgama QA initial download | 43 MB (earlier 1.1 QA) | 21 MB |

The confirmed QA run reported **3.02 seconds** of initial loading time. The split reduced the initial local player payload by approximately 51%. The city is delivered separately as a versioned LZ4 AssetBundle (41.07 MB); total downloaded data is not the same as initial download size. Network timings vary with connection speed and cache. Mobile stability still requires device testing.

## Technology

| Component | Technology |
|---|---|
| Engine | Unity `6000.6.1f1` |
| Rendering | Universal Render Pipeline 17.6.0 |
| Platform | WebGL 2 / Playgama |
| City | Fantastic City Generator, authored workbench and runtime prefab |
| Asset delivery | Brotli player, on-demand LZ4 city bundle, streamed music |
| Source control | Git and Git LFS |

## Development

Install Unity `6000.6.1f1` with Web Build Support, Git LFS and Python 3 for the local WebGL test server.

```bash
git clone https://github.com/G3R-Studio/motor-city.git
cd motor-city
git lfs pull
```

Open the project in Unity and load `Assets/Scenes/Prototype.unity`.

### Build and test

1. Activate **Web - Desktop - Release** and leave Development Build disabled.
2. Run **Tools > Motor City > Build > Check and Set Brotli**.
3. Run **Tools > Motor City > Build > Build Split City Test** to create the current split-city release candidate.
4. Find the output directory in `Library/MotorCitySplitCityTest.txt`.
5. Serve the completed build locally:

```bash
python Tools/serve_webgl.py --directory "PATH_TO_SPLIT_BUILD"
```

Open the local URL printed by the server. Check menu loading, city entry, gameplay, persistence, audio and browser console before any Sandbox update.

A normal Unity Build still embeds the city in Resources. **Measure City AssetBundle** produces diagnostic builds, not a playable release. Do not upload the stripped diagnostic player or experimental compressed bundle sidecars. If the split build is interrupted, use **Restore City After Experiment** and verify the original assets are restored.

## Development status

The startup optimization has been tested locally and in Playgama Sandbox. The next phase is a read-only render audit, followed by measured improvements to memory usage, geometry LOD, batching, lighting and mobile stability.

Use the [technical roadmap](Docs/Roadmap-1.1.md) as the current source of truth for priorities, completed experiments, safe asset handling, QA gates and handoff between development sessions.

## Documentation

- [Technical roadmap and handoff](Docs/Roadmap-1.1.md)
- [Historical technical audit](Docs/Audit-1.1.md)
- [Third-party font licenses](Docs/Licenses)
- [Asset license](Assets/ThirdParty/LICENSE)

---

Developed by **G3R Studio**.
