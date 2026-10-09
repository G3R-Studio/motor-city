# Phase 2 — Unity Editor CI runner

The source/GUID/Roslyn/UI jobs run on GitHub-hosted Linux runners. The Unity Editor job is **opt-in**, because Unity 6000.6.1f1 and a valid activation are not provisioned on the default GitHub-hosted runners.

## Enable

1. Prepare a **trusted Windows x64 self-hosted GitHub Actions runner** with Unity Hub and **Unity Editor 6000.6.1f1** installed; activate Unity according to your license terms. Do not commit license files, serial numbers or credentials. Restrict who can invoke workflows on this runner.
2. Register the runner for this repository, giving it the additional label `unity6000`. Its runner labels must include `self-hosted`, `Windows`, `X64`, `unity6000`.
3. Ensure Unity is closed on the runner before the job starts and the runner account can access the Unity activation. Either install at the default Windows path in `Tools/run_unity_phase2.ps1` or configure `UNITY_EDITOR_PATH` in the runner account's environment.
4. In GitHub repository **Settings → Secrets and variables → Actions → Variables**, add `UNITY_CI_ENABLED` with value `true`. Until then, the Unity job will be **skipped**, not passed.
5. Run **Motor City Source Gates** from Actions (or push a commit). Confirm the `Unity 6 Editor batch validation` job is green and download `motor-city-unity-validation` containing the batch log, compile and dependency reports.

The job runs `Tools/run_unity_phase2.ps1` without `-BuildWebGL`; it verifies the editor/project, not a real player build. WebGL build validation is independently opt-in via `pwsh -NoProfile -File Tools/run_unity_phase2.ps1 -BuildWebGL` on a properly provisioned machine with the WebGL build module. Release-profile and browser matrix is still Phase 12. The runner is responsible for secure workspace cleanup and keeping project copies out of public locations.

Do not set `UNITY_CI_ENABLED=true` until the labeled runner is online: otherwise jobs remain queued.

## Manually triggered WebGL build (Phase 2)

The workflow also contains a separate `Unity 6 WebGL build and report` job on the same Windows runner. It only runs when **both** repository variables `UNITY_CI_ENABLED=true` and `UNITY_WEBGL_CI_ENABLED=true` are present **and** the workflow was launched with **Run workflow** (`workflow_dispatch`). Pushes and pull requests will not trigger the expensive WebGL build. First confirm that the **WebGL Build Support** module is installed for Unity 6000.6.1f1.

The job executes `Tools/run_unity_phase2.ps1 -BuildWebGL`, preserves `unity-build.txt`, `unity-dependencies.txt`, the compile marker and the batch log as artifact `motor-city-webgl-build-report`. It does **not** publish a game build and does **not** select the named Desktop/Mobile release Build Profiles; those profiles and browser tests are verified separately in Phase 12.

Turn `UNITY_WEBGL_CI_ENABLED` back to `false` to disable manual builds, or leave it set to `true` for future manual invocations.
