# Motor City

**Version 1.1 · Unity 6.6 · WebGL / Playgama**

Motor City is an open-world driving game by G3R Studio. Explore the city, complete missions, earn credits and reputation, and upgrade your cars.

## Features

- 10 vehicles, garage upgrades and customization.
- Delivery, racing, drifting and other city activities.
- Dynamic day/night cycle, traffic and navigation.
- Keyboard, gamepad and touch controls.
- Persistent progress through Playgama Bridge Storage.

## Update 1.1

- Playgama QA initial download reduced to 21 MB with deferred city loading (3.02 s in the confirmed QA run).
- Improved rendering performance with distance-based culling.
- Streamed music, optimized textures and Brotli compression.
- Playgama Bridge integration for storage, ads and audio.

## Development

1. Install Unity `6000.6.1f1` with Web Build Support and Git LFS.
2. Clone the repository and run `git lfs pull`.
3. Open the project and `Assets/Scenes/Prototype.unity`.

For the current split-city test/release candidate, select **Web - Desktop - Release**, disable Development Build, use **Check and Set Brotli**, then **Tools > Motor City > Build > Build Split City Test**. A normal Build still embeds the city in Resources. Do not upload the diagnostic no-city player. Follow the roadmap QA gates. Test with `python Tools/serve_webgl.py --directory PATH_TO_BUILD` before uploading the complete ZIP to Playgama.

## Documentation

[Current technical roadmap and handoff](Docs/Roadmap-1.1.md) · [Technical audit](Docs/Audit-1.1.md)

Third-party notices: [font licenses](Docs/Licenses) and [asset license](Assets/ThirdParty/LICENSE).
