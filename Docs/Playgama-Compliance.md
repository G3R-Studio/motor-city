# Motor City — Playgama release compliance
19.11.2026. Source audit, not certification.
Official index: https://wiki.playgama.com/playgama/llms.txt
Requirements: https://wiki.playgama.com/playgama/game-requirements
Advertising: https://wiki.playgama.com/playgama/game-requirements/advertising-requirements
Technical: https://wiki.playgama.com/playgama/game-requirements/technical-requirements
UX: https://wiki.playgama.com/playgama/game-requirements/user-experience-requirements
Content: https://wiki.playgama.com/playgama/game-requirements/content-requirements
Partners: https://wiki.playgama.com/playgama/game-requirements/platform-specific-requirements
SDK: https://wiki.playgama.com/playgama/bridge-sdk/getting-started

## Source-level implementations
- Bridge v2 initialized before Unity, storage read before gameplay, game_ready guarded, level messages, language and platform pause/audio callbacks. Standard Build routes through district packaging. Historical Sandbox success is not proof of current build.
- Viewport wrapper fills iframe, Unity footer hidden (bbef0d1); safe area and responsive Canvas retained. No new fixed ad reservation.
- Interstitial at explicit activity boundary, minimum session duration 120 seconds and one completed activity, cooldown default 180 seconds (147b2bb); Bridge still controls availability.
- Save FlushNow before/after ads (58d0c3a); async storage acknowledgement is not guaranteed by synchronous C# flush. Existing reward callback requires rewarded state; failed/cancelled ads must not grant reward. Check failed requests and pause/resume.
- Modal pause owner cleared on HUD disposal (289c223); garage excluded from frozen blur.
- Real purchases disabled, no external payment path, optional shop rewards and temporary boost retained. Legacy stats adapter IncrementStat is a no-op in Playgama, not external analytics.
- Russian and English, audio mute/volume, onboarding/progression, keyboard and touch modes present.

## Release gates, not confirmed
- Build in empty folder using Web - Desktop - Release. Verify archive root index.html, Latin file paths, archive <=300 MB, all district bundles, no debug resources or exceptions. TMP Essentials must be available at build time.
- QA tools in actual hosted iframe: SDK initialization, game_ready, lifecycle messages, save across restart, ad success/failure/cancel, no audio or physics during ads, pause reasons overlap, input and resume, store cooldowns.
- Check safe areas, resize/orientation and all UI layouts including garage, language selector recognizable without knowing current language, all popups closable, no scroll or text overlap. Declare landscape-only if needed. Desktop/mobile Chrome, Firefox, Safari, Edge, Opera as applicable; no claim of support for obsolete OS versions merely from Unity code.
- Check real mobile memory, load times, FPS, district transitions, graphics quality, focus/visibility, sound autoplay. Historical initial load ~21 MB; no new measurement. No arbitrary compression/quality downgrade.
- Confirm rights to assets, fonts, music, artwork, screenshots and trademarks; source audit cannot establish licenses.
- Publication metadata and age/content rating, device support and orientation, and distribution settings checked by owner. Do not enable partners with unmet requirements (e.g. Facebook social features and archive limits, MSN size, Discord multiplayer/IAP, portrait-only platforms). Optional SDK APIs not required for core Playgama release.
- Confirm rewarded prompt clearly describes exact reward before opt-in; verify all localized variants including ordinary bonus. Verify no hidden third-party SDK in final archive.

## Scope
Reviewed accessible general requirements and SDK-related implementation. Some current documentation pages (engine-specific, self-check, platform/storage API markdown) were inaccessible during audit; do not claim exhaustive page-by-page verification. No Unity compiler or browser QA execution in this audit. Final certification remains with Playgama.

- Engine-specific requirement 5.1.1: WebGL Decompression Fallback enabled in project and both Web profiles; compressed payload inventory accepts .unityweb. Verify hosted startup before release.
