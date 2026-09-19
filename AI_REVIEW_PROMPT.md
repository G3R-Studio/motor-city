# Prompt for an external AI reviewer

Copy the prompt below into the other AI after giving it access to this repository.

---

You have access to the GitHub repository for **Motor City**, a Unity 6.6.1 browser-first open-world driving game targeting Yandex Games.

First read:

1. `AI_PROJECT_CONTEXT.md`
2. `README.md`
3. `ProjectSettings/ProjectVersion.txt`
4. `Packages/manifest.json`

Then inspect the actual project rather than only summarizing those files.

I want a **full technical review of the current game**.

Study all Motor City-owned code under:

- `Assets/Scripts/`
- `Assets/Editor/`
- `Assets/Shaders/`

Also inspect:

- `Assets/Scenes/Prototype.unity`
- `ProjectSettings/`
- generated files under `Assets/Resources/MotorCity/`
- `Assets/LocalGenerated/FCG_Workbench.unity` when present
- `MotorCity_FCGSceneReport.txt` when present
- `MotorCity_CityAssetReport.txt` when present

Important: original third-party Asset Store packages such as Fantastic City Generator, Prometeo Car Controller and ARCADE: FREE Racing Car may intentionally be absent. Generated scenes/prefabs can still reference them. Do not invent the contents of missing packages.

Please review the project in depth for:

- compile/runtime bugs;
- hidden logic errors;
- architecture problems;
- Unity 6.6 / URP 17.6 issues;
- WebGL and Yandex Games compatibility;
- performance bottlenecks;
- GC allocations and expensive per-frame work;
- excessive realtime lights;
- MeshCollider complexity;
- generated-city performance;
- connecting-highway collision correctness;
- camera obstacle/collision behavior;
- vehicle physics and WheelCollider setup;
- drift scoring/effects consistency;
- gameplay activity conflicts;
- save/persistence problems;
- UI/input issues;
- shader/material problems;
- missing/null asset handling;
- editor tooling safety;
- generated snapshot reproducibility.

Pay special attention to these current systems:

- `CityAssetRuntimeInstaller`
- `CityCollisionUtility`
- `ChaseCamera`
- `DayNightCycleController`
- `FantasticCityGeneratorRuntimeBuilder`
- `FantasticCityGeneratorUrpFixer`
- the special connecting-highway sections `HW-F-400-01`, `HW-F-400-02`, `HW-F-400-04`.

The highway must remain visually hilly; do not suggest flattening it as a collision fix.

Traffic lights are intentionally static. Decorative street furniture is intentionally non-blocking. Game audio is intentionally removed.

For each problem you find, provide:

- exact file/class/method;
- why it is a problem;
- likely player-visible symptom;
- severity;
- concrete code-level fix;
- whether the fix is safe for WebGL/Yandex Games.

Separate definite bugs from hypotheses that require testing.

After the audit, give a prioritized implementation plan starting with crashes/compile issues, then severe gameplay bugs, then performance, then polish.

Do not rewrite the whole project or propose a new engine. Review and improve the current architecture.
