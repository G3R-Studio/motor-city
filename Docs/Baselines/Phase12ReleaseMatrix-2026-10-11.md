# Phase 12 — WebGL Desktop/Mobile Release verification matrix (2026-10-11)

**State:** release validation in progress. Do not call either player build/browser smoke **PASS** merely because Unity Editor CI or static source checks succeeded.

## Exact-profile build pipeline

- Source CI: `Tools/check_phase12_release_profiles.py` validates both saved Build Profiles, release/debug flags, input backend and distinct Desktop/Mobile WebGL texture settings. This is a source-level assertion, not proof of final player binaries.
- Exact-profile builder: `Assets/Editor/MotorCityPhase12ReleaseBuild.cs` selects `BuildProfile.GetActiveBuildProfile()`, confirms the expected saved profile path and builds via `BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions { buildProfile = active, options = BuildOptions.None, ... })`.
- **Build output location:** `Builds/Phase12/` is Git-ignored and outside Unity's disposable `Temp/`. The first CI #257 Unity Desktop BuildPlayer reported success after ~16 min but its file report/output under `Temp/` disappeared before PowerShell could verify it. The original runner treated that as failure and skipped Mobile. The output location is corrected before re-testing.
- PowerShell entrypoint: `Tools/run_unity_phase12_release.ps1 -Profile Desktop` or `-Profile Mobile` launches Unity 6000.6.1f1 with `-activeBuildProfile "Assets/Settings/Build Profiles/Web - ... - Release.asset"` **before** compilation. It checks exit status, build report and `index.html`.
- CI: pushing a commit with `[phase12-release]` runs the two separate profile builds on a licensed Windows Unity runner. This is intentionally opt-in because two WebGL builds are expensive. The job uses the project-specific exact-commit read-only seed checkout so that LFS and large authored city assets are available. `GITHUB_SHA`, exact profile, Unity version, result, output bytes and scene list are written into separate text reports.
- CI artifacts: `motor-city-phase12-release-reports` (logs/reports), `motor-city-webgl-desktop-release`, `motor-city-webgl-mobile-release` (short retention for manual browser smoke, only on both successful builds). Artifact URLs/commits should be recorded after successful runs.

**Important distinction:** the older on-demand `MotorCityPhase2BatchGate.BuildWebGL` uses `BuildPlayerOptions` and global WebGL settings, not these saved Build Profiles. It must not be counted as either profile's Release acceptance gate.

## Release verification matrix

| Gate | Desktop Release | Mobile Release | Evidence needed |
| --- | --- | --- | --- |
| Exact profile, non-development flags, Input System New | source preflight | source preflight | `check_phase12_release_profiles.py` output and immutable SHA |
| Unity 6000.6.1f1 compilation/player build | pending | pending | separate CI build reports, logs and `index.html` for same commit |
| Load page, first launch and restart with existing save | pending | pending | browser/device, clean/fresh/existing saves, console/log notes |
| Choose controls: keyboard vs arrows/wheel | pending | pending | appropriate real browser/touch tests; steering release and pedals |
| All 9 player vehicles: spawn, collision, wheels, lights, tuning/paint | pending | pending | screenshots or per-vehicle smoke notes; no missing visual/physics |
| Garage → city → pause → main menu → Continue | pending | pending | control scheme selection and no stuck input after resume |
| Activities and rewarded-ad behavior | pending | pending | actual Yandex Games environment, skip gracefully when unavailable |
| Save persistence, browser reload, local/cloud conflict | pending | pending | Yandex login/device matrix as applicable, single rewards |
| Intro/day-night, materials, 0 missing scripts/shaders/slots | pending | pending | browser GPU, Unity Console/JS Console and visual confirmation |
| QA/Admin/reset/dev diagnostic entrypoints absent in Release | pending | pending | compiled-player inspection and explicit negative QA; source guard alone insufficient |

Do not mark WebGL browser results based on Desktop Editor, Device Simulator, build success or previously tested commits.

## Manual browser smoke after artifacts appear

1. Download a complete **Desktop** artifact and extract all files into one folder. Serve it via a compatible **HTTP** server (do not open `index.html` over `file://`). Record OS/browser, URL, git commit SHA, first-load console output and if the game enters the city.
2. Repeat with **Mobile** artifact using a real phone/mobile browser and a compatible server. Record device/browser, screen orientation, touch arrows/wheel/handbrake, control customization and slow-FPS steering response.
3. Test fresh save in an isolated profile, then existing save in the normal profile. Reload the page and verify both gameplay and persistence; cover game-entry, garage and activities.
4. Use **Yandex Games** staging for SDK-specific ads, cloud saves and platform functionality. A local HTTP server cannot certify these features.
5. If a bug appears, record which profile/commit, device, console line and minimal reproduction. Only after all rows pass should Phase 12 be closed.

## Unfinished/intentional work

Phase 10 measured vehicle physics at low FPS and Phase 11 license provenance/LFS migration decision remain separate. No third-party license or binary was removed. Do not mix asset deletion or physics tuning into the Release validation commits.
