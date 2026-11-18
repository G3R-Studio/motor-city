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

- Initial WebGL download reduced from 65 MB to 43 MB in local QA.
- Improved rendering performance with distance-based culling.
- Streamed music, optimized textures and Brotli compression.
- Playgama Bridge integration for storage, ads and audio.

## Development

1. Install Unity `6000.6.1f1` with Web Build Support and Git LFS.
2. Clone the repository and run `git lfs pull`.
3. Open the project and `Assets/Scenes/Prototype.unity`.

For release, select **Web - Desktop - Release**, disable Development Build, use **Check and Set Brotli**, and build into a clean directory. Test with `python Tools/serve_webgl.py --directory PATH_TO_BUILD` before uploading the complete ZIP to Playgama.

## Documentation

[Roadmap](Docs/Roadmap-1.1.md) · [Technical audit](Docs/Audit-1.1.md)
