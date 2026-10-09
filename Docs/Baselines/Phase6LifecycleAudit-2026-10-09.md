# Phase 6 — bootstrap/lifecycle source audit (2026-10-09)

**Scope:** source inspection of persistent gameplay hosts, registered scene callbacks and singleton disposal. This is not a claim that runtime reloads/WebGL have passed.

| Owner | Creation, persistence and duplication policy | Reset/disposal evidence | Remaining runtime gate |
| --- | --- | --- | --- |
| `MotorCityBootstrap` | `AfterSceneLoad` installs exactly one `sceneLoaded` handler; once-only flags control game creation | `SubsystemRegistration` clears flags/material cache and unregisters `OnSceneLoaded`; installer unregisters before re-register | Disable Domain Reload, re-enter Play Mode, load Prototype twice |
| `MotorCityVirtualInputRuntime` | `BeforeSceneLoad` + `DontDestroyOnLoad`, existing-instance query and `Awake` duplicate guard | No static instance to reset; virtual pressed/held frame cycles through Update/LateUpdate | Verify exactly one runtime after scene changes and valid virtual input |
| `HudVisualPolish` | `AfterSceneLoad` + `DontDestroyOnLoad`, existing-instance query and `Awake` duplicate guard | UI references are Unity objects, with `hudRoot == null` retry/rebind path | Verify one visual polish and no stale HUD binding |
| `MotorCitySfxRuntime` | lazy creation, `Awake` duplicate guard, persistent host | `OnDestroy` clears `instance` and generated clips | No duplicate SFX during repeated menu/city entry |
| `MotorCityMusicRuntime` | lazy creation, `Awake` duplicate guard, persistent host | `OnDestroy` clears `instance` | No duplicated music and pause/resume works |
| `MotorCityUiEventSystem` | `EnsureUiEventSystem` adopts existing `EventSystem`; otherwise creates persistent Input System UI host | No static instance/scene event subscription | Verify single active `EventSystem`, input module after repeated entry |
| `YandexPlatformInstaller` | WebGL-only `BeforeSceneLoad`; existing bridge guard and persistent object | Platform service remains static; initialization semantics require browser smoke | Build and exercise Yandex WebGL; do not claim tested in Editor |
| `Motor City Platform Systems` | Bootstrap finds or creates persistent platform/save/cloud/purchase/config components | Async sequence: platform → remote config → pending purchases → cloud → frontend/gameplay | Confirm callbacks and recovery across repeated scene navigation |

## Audited event binding

The targeted `SceneManager.sceneLoaded` registration currently appears only in `MotorCityBootstrap`. It has one addition and defensive removals in `ResetStaticState` and `InitializeBootstrap`. The Phase 6 CI gate enforces this targeted invariant and lists all files with potential `+=` / `-=` operations as *candidates only*.

The generic event inventory is **not** a sound proof of no leaks. Delegate subscriptions elsewhere require per-owner runtime examination before declaring the whole phase closed.

## Evidence and boundaries

- Phase 6 ordered bootstrap extraction was verified by CI #107 and a user-reported Play Mode PASS.
- The subsequent singleton hardening package passed CI #37994178789 and user-reported Play Mode PASS.
- The expanded source audit in `Tools/check_phase6_lifecycle.py` must pass a newer CI run.
- Separate WebGL Release smoke, disable-domain-reload scenario and instrumentation of actual live-instance counts are still outstanding.
