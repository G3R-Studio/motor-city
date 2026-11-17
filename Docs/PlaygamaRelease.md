# Motor City — Playgama release

## Build
- Use Unity **6000.6.1f1**, Web Build Support and Git LFS.
- In File > Build Profiles select **Web - Desktop - Release** (DXT) for initial desktop-only submission, or validate **Web - Mobile - Release** (ASTC) separately before promising mobile support. Do not combine two builds in one game archive.
- Build to a clean directory, e.g. Builds/Playgama. Keep Development Build disabled.
- The postprocessor automatically places the official stable Bridge CDN script first in index.html, initializes it and reads storage before starting the Unity loader, then copies the config and bootstrap. No manual SDK package or template changes are required.
- Check index.html, motor-city-bridge.js, playgama-bridge-config.json, Build/ and TemplateData/ in the output. ZIP the contents, not the enclosing directory.
- The uploaded archive must meet Playgama's current 300 MB limit and startup peak less than 2 GB. If too large, optimize assets; do not upload the Git repository.
- Test with a server (Build And Run, or Tools/serve_webgl.py) and in the Playgama QA tool. Local mock ads are not proof of live ads.
- Upload via https://developer.playgama.com/ and select Add Game. Submit a desktop-only, landscape draft initially unless mobile has passed QA.

## Verification checklist
- Bridge initialized before Unity asset download, config loaded, one game_ready after interactive menu.
- RU and EN, English fallback, desktop keyboard/mouse (Backspace replaces Esc), reload restores coins, cars, settings and mission state.
- No actual direct PlayerPrefs access in WebGL: the same game-facing methods use the preloaded Bridge cache. Editor continues to use Unity PlayerPrefs; old saves on other domains are not migrated.
- Interstitial requested on natural breaks (before an explicit activity); Bridge controls frequency. Rewarded ads grant only after rewarded state and close. Closing without reward grants nothing.
- Pause and sound while ads open, tab hidden, and host requests pause/mute; verify no unintended resume.
- Shop and external leaderboard submissions are disabled until the Playgama catalog and receipt/leaderboard mappings are integrated and tested. Local progression is unaffected. No direct Yandex SDK remains in runtime.
- Check sizing at square/narrow/landscape, control labels, no external links or warnings, no startup errors, no debug tools in release.

## Scope
Implementation uses official Bridge JS Core through the existing WebGL native interoperability boundary. No Unity compilation or live host QA is implied by a source change. Verify in Unity and Playgama before submission.
