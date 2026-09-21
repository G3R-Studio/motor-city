# Motor City — Development Plan

This document is the implementation checklist for the current Motor City production direction.
README is intentionally not used as a development roadmap.

## Product direction

Motor City is a bright open-world driving adventure for Yandex Games.

Core fantasy:
- the player has a favorite car, Turbo pet, garage and city;
- short driving adventures give useful rewards;
- cars enable different play styles rather than only bigger speed numbers;
- long-term retention comes from customization, collections, favorite vehicles, seasons and clubs;
- no violence, open chat, loot boxes or pay-to-win.

## Development rules

- Finish and verify each phase before moving to the next.
- Preserve working gameplay systems instead of rewriting them without a reason.
- Authored FCG city is the source of truth; gameplay code must not decorate or mutate it.
- FCG native traffic remains the only traffic system.
- Motor City owns day/night.
- Prometeo remains the vehicle controller.
- README is updated only to describe the current released state, never future steps.
- WebGL/mobile performance is a requirement, not a final polish task.
- Player-facing UI remains simple even when underlying progression is deep.

---

## Phase 0 — Refactor the existing prototype

### 0.1 Save and data foundation
- [x] Add centralized JSON save service.
- [x] Add deferred local flush runtime.
- [x] Preserve legacy PlayerPrefs values through automatic migration-on-read.
- [x] Add JSON export/import entry points for future cloud save.
- [x] Migrate runtime gameplay systems away from direct PlayerPrefs access.
- [ ] Validate migration in Unity with an existing local save.

### 0.2 Platform abstraction
- [x] Add platform service interface.
- [x] Add local/browser fallback implementation.
- [x] Separate gameplay from Yandex SDK calls.
- [x] Define Game Ready / Gameplay Start / Gameplay Stop lifecycle.
- [x] Define cloud save/load bridge.
- [x] Define leaderboard, rewarded ad and purchase bridge.
- [x] Define platform language and server-time bridge.

### 0.3 Localization architecture
- [x] Move player-facing text out of gameplay logic.
- [x] Add localization keys/table.
- [x] Russian is the default language.
- [x] Prepare English table without changing the main language.
- [x] Keep technical IDs independent from localized names.

### 0.4 Unified input
- [x] Add MotorCityInput facade.
- [x] Replace direct Keyboard.current checks in gameplay systems.
- [x] Keep keyboard controls through the facade.
- [x] Prepare touch actions for mobile.
- [x] Prepare contextual action input instead of hard-coded E/Shift combinations.

### 0.5 HUD and UX cleanup
- [x] Keep speed, credits, REP, minimap, one objective and contextual prompt.
- [x] Hide secondary progression from permanent HUD.
- [x] Add a single notification queue.
- [x] Add touch-safe scalable layout.
- [ ] Replace runtime minimap camera with a cheaper solution if profiling confirms it is expensive.

### 0.6 Mission / Adventure architecture
- [x] Add a shared mission definition and step model.
- [x] Add AdventureDirector.
- [x] Existing Delivery/Drift/Sprint/Circuit remain reusable activity executors.
- [x] Career, contracts, live events, legends and secret events become mission sources instead of competing HUD systems.
- [x] Add reusable steps: go to point, checkpoints, drift score, race result, delivery, discovery, photo, parking.

### 0.7 Repackage existing progression
- [x] Simplify player-facing progression to KR + REP.
- [x] Keep mastery, discipline reputation and collection as secondary systems.
- [x] Reframe Underground as secret night car-club events.
- [x] Reframe police risk around friendly Inspector Bublik presentation.
- [x] Remove criminal/dark presentation that conflicts with the 6+ tone.

### 0.8 Vehicle ownership and economy
- [x] REP unlocks access to a vehicle.
- [x] KR purchases ownership.
- [x] Starter vehicle is always owned and useful.
- [x] Collection score counts owned cars, not REP-unlocked cars.
- [x] Vehicle mastery/history stays attached to the specific car.
- [x] Balance upgrade and vehicle prices around 4–8 minute sessions.

### 0.9 Existing-runtime optimization
- [x] Cache repeated object lookups.
- [x] Reduce unnecessary runtime material/object creation.
- [ ] Profile physics tick and solver settings.
- [x] Keep full physics quality for player car.
- [x] Reduce distant traffic update cost.
- [x] Traffic shadows/probes/motion-vector cost audit.
- [ ] WebGL memory/build-size audit.
- [x] Low/Medium/High quality presets.

---

## Phase 1 — Yandex Games foundation

- [ ] Production Boot scene.
- [ ] Yandex SDK adapter implementation.
- [ ] Guest mode.
- [ ] Game Ready lifecycle.
- [ ] Local + cloud save conflict strategy.
- [ ] Pause/resume integration.
- [ ] Platform language.
- [ ] Basic analytics.
- [ ] Leaderboard adapter.
- [ ] Rewarded ad adapter.
- [ ] Purchase adapter.
- [ ] Remote config adapter.
- [ ] Mobile device quality selection.

---

## Phase 2 — Retention MVP

### Turbo pet
- [ ] Robo-cat Turbo MVP.
- [ ] Level and XP.
- [ ] Mood/reunion messages.
- [ ] Daily task giver.
- [ ] Hint ability.
- [ ] Collection magnet ability.
- [ ] Short PvE boost ability.
- [ ] Cosmetic skins.

### First-session onboarding
- [ ] Understand steering in under 60 seconds.
- [ ] First drive.
- [ ] First reward.
- [ ] Meet Turbo.
- [ ] First garage customization.
- [ ] First daily task.

### Daily Adventures
- [ ] Three short daily tasks.
- [ ] 2–5 minute task duration.
- [ ] Soft streak.
- [ ] 3/7/14/30-day milestone rewards.
- [ ] Cosmetic milestone rewards.
- [ ] No hard punishment for missed days.

### Story missions
- [ ] 10 introductory missions.
- [ ] Uncle Vitya.
- [ ] Nika.
- [ ] Inspector Bublik.
- [ ] Turbo.
- [ ] Final city festival race.

### Basic customization
- [ ] Body colors.
- [ ] Stickers.
- [ ] Simple vinyls.
- [ ] Wheels.
- [ ] Neon.
- [ ] Plates.
- [ ] Cosmetic presets.
- [ ] Photo button.

### Photo hunt
- [ ] City album.
- [ ] Landmarks.
- [ ] Rare cars.
- [ ] Secrets.
- [ ] Seasonal collection slots.

---

## Phase 3 — Automotive life expansion

- [ ] Pizza courier.
- [ ] Taxi.
- [ ] Mail.
- [ ] Ice cream route.
- [ ] Car wash mini-game.
- [ ] Tow-truck profession after suitable vehicle exists.
- [ ] Vehicle passport/history screen.
- [ ] Better contract mini-stories.
- [ ] Profession progression without adding extra currencies.

---

## Phase 4 — Alpha

- [ ] Season framework.
- [ ] Season 1 content.
- [ ] Character presentation.
- [ ] Achievement system.
- [ ] Garage presets.
- [ ] Expanded photo album.
- [ ] Async club prototype.
- [ ] Preset club names/emblems.
- [ ] No open chat.

---

## Phase 5 — Beta

- [ ] Yandex leaderboards.
- [ ] Club weekly goals.
- [ ] Weekend events.
- [ ] Rewarded ads.
- [ ] Cosmetic purchases.
- [ ] Free + cosmetic premium season path.
- [ ] Remote Config live balance.
- [ ] English localization.
- [ ] Economy/retention analytics dashboards.

---

## Phase 6 — Release

- [ ] Tutorial funnel polish.
- [ ] Economy balance.
- [ ] Crash/error pass.
- [ ] Weak-device FPS pass.
- [ ] WebGL memory pass.
- [ ] Build-size pass.
- [ ] Mobile UX pass.
- [ ] Loading-time pass.
- [ ] Yandex moderation checklist.
- [ ] Final 6+ tone/content pass.

---

## Phase 7 — Live Ops

Every 4–6 weeks:
- 10–15 missions;
- one visual/event theme;
- a short character story;
- Turbo cosmetic;
- 2–4 vehicle cosmetics;
- one rare reward;
- collection additions;
- weekend event variation.

Large expansions such as off-road regions, businesses, auctions, second cities and deeper player economy are postponed until retention data proves the core loop.
