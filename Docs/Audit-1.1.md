# Аудит Motor City 1.1 — этап 1 (18.11.2026)

Исходный снимок main: 63ef1745ae7be2cacae0bd83fd52634b5a17b628.

## Исходные данные

Архив 1.0: 103,4 МБ; загрузка 20,3 с. Исходные размеры Git не равны размеру импортированных или сжатых ресурсов.

| Группа | Наблюдение |
|---|---|
| CityVisual.prefab | 83 271 022 байт YAML |
| Intro | 7 PNG, 15 821 959 байт исходников |
| Music | 16 OGG, Git LFS; размер указателей не отражает размер аудио |
| Audio | 2 файла, 290 043 байт исходников |
| Fonts | 2 229 013 байт исходников, включая лицензию |

Пример Music/menu_01.ogg: Vorbis quality 0.55, loadType 0, preloadAudioData 1. Не менять массово настройки до измерений. Оба Release профиля: development 0, stripEngineCode 1, WebGL compression 0 (сборка 1.0 gzip), initial memory 32 MB, maximum 2048 MB. Desktop texture subtarget 1, Mobile 4.

## Измерение

Расширен существующий Tools > Motor City > Build > Generate Dependency Report: учитывает Resources и фактический PackedAssetInfo из последней сборки; автоматически выполняется после сборки. Результат Library/MotorCityBuildDependencyReport.txt. Сначала собрать чистый Desktop Release, затем прислать отчёт. Если packed content отсутствует, повторить с подробным BuildReport. Отчёт показывает сериализованные байты, не размер ZIP. Не удалять ресурсы по одному лишь отсутствию ссылок: Resources.Load может использовать динамические имена.

## Следующая работа

1. Ранжировать фактический packed content по типу и исходному пути.
2. Оценить звук, вступительные изображения, текстуры и меши города.
3. Сохранить контрольную сборку и выполнить изменения по одному с измерениями.
4. Проверить Brotli на Playgama до смены релизного формата.

На этом этапе игровые ресурсы и настройки сборки не изменены. Unity здесь не запускалась; компиляция расширенного отчёта и измерение требуют локального Editor.

## Фактический отчёт Unity от 18.11.2026

Packed content: 218,31 MiB; Texture2D 89,79 MiB; Mesh 69,13 MiB; AudioClip 37,91 MiB. Tabler: 65 PNG, 28,06 MiB; Music: 16 OGG, 37,79 MiB; Intro: 7 PNG, 7,33 MiB; FCG: 97,07 MiB без CityVisual (11,03 MiB). Не складывать группы с типами объектов. Это несжатые сериализованные байты, не размер ZIP. Summary сообщает 0 B и нулевую длительность, поэтому фактический размер архива измеряем отдельно.

Первый эксперимент: WebGL Tabler max 256, RGBA32, mipmaps off; исходники сохранены. Требуется Tools > Motor City > Build > Reimport Optimized UI Icons, повторная сборка, проверка прозрачности и читаемости всех иконок. Остальные ресурсы не менялись.

## UI experiment — measurement 15:03:17

Packed content 202.56 MiB; Tabler 65 icons 12.31 MiB (previous 28.06, reduction 15.75 MiB). Music unchanged at 37.79 MiB. User reports output folder 98.4 MB; ZIP-to-ZIP comparison not yet available. Visual verification pending. No new blocking error reported. Existing importer-version and collision warnings tracked separately.

## Music experiment

WebGL override: CompressedInMemory, quality 0.35, preserve sample rate and existing preload behavior. Applies only to Music OGG files, not engine or other effects. No source tracks removed or edited. Run Tools > Motor City > Build > Reimport Optimized Music, build to a fresh folder, compare packed AudioClip bytes and ZIP size. Verify all tracks, menu/game transitions, mute and ads in browser. Actual WebGL encoding and size savings must be measured; Unity may choose platform encoding. Runtime music loading intentionally unchanged to isolate the experiment.

## Music experiment result and deferred loading

15:10:07: music packed sizes and reported folder size 98.4 MB unchanged. Unity Web imports AudioClips as AAC; the override did not reduce measured size. Move the 14 city OGG files (preserving Git LFS blobs and GUIDs) to Assets/StreamingAssets/MotorCity/Music. Two menu tracks remain in Resources. City music downloads on demand via UnityWebRequest; the next track is prefetched, failed downloads retry with a delay and do not gate gameplay. No external hosting required. Total distribution size may differ from initial network payload. Verify OGG decode on Chrome/Firefox/Safari, 14 files in StreamingAssets, no city tracks in packed report, and no music downloads before gameplay. Compare .data.gz and cold-load network timing. Editor/runtime compilation and browser QA pending.

## Deferred music browser QA

Build folder 66.0 MB, StreamingAssets 33.2 MB, total 99.3 MB (user measurement). WebGL console rejected Unity OGG streaming. Web player now uses browser HTMLAudioElement for city tracks only; Unity retains menu music and native/editor audio path. Verify playback, next track, mute, platform pause and ads on actual browsers. OGG browser support must be checked, especially Safari. Separate console issues: missing SphereCollider and zero-width RenderTextureDesc; investigate independently.

## Browser music verification — 18.11.2026

User confirmed city music works after the browser audio fix. Build folder before this fix: 66.0 MB; StreamingAssets: 33.2 MB. Full ad/pause regression and cross-browser QA remain pending. Next: inspect city texture imports and measure targeted reductions.

## City texture trial A

Eight existing WebGL overrides reduced from 1024 to 512: Wins_N.tga, WinGlass-01_N.tga, WinGlass-03_N.tga, WinGlass-04_N.tga, Grade1.psd, Grade2.psd, Metal.psd, Water.png. All other importer fields (including mipmaps, normal-map type, alpha, color space and automatic platform compression), GUIDs and source files retained. Main building atlas, roads, foliage and night/emission textures unchanged. Baseline packed size for these eight paths: about 10.67 MiB; theoretical reduction around 8 MiB if dimensions halve and format remains unchanged. Actual ZIP and network savings require a new build. Compare city closeups, railings, window normals, water, and day/night transitions on Desktop Release before accepting. Unity import/build and visual QA pending.

## City texture A result and WebGL error triage

User verified startup after bridge refresh; Build folder 64.6 MB (previous 66.0 MB). Packed report unavailable in incremental build. Browser log: three missing SphereCollider messages, two zero-width RenderTextureDesc exceptions. Three procedural fog spheres are a strong candidate for the former; preserve UnityEngine.SphereCollider explicitly and reference it in the existing removal code. Keep engine stripping enabled. For RenderTextureDesc, add a Web-only bounded diagnostic dump of active camera sizes, rectangles, target textures and URP render scale on the first two matching errors. SSAO downsampling is a hypothesis, not established cause. No rendering settings changed. Rebuild clean and inspect console; both fixes require browser verification.

## QA Tool verification and viewport guard

QA Tool: initial download 65 MB, local loading 2.85 s; warns game will be hidden from mobile catalog due to high crash risk. Local timing is not a production cold-load benchmark. New browser log no longer contains missing SphereCollider; RenderDiagnostic shows screen=1x1, camera=1x1, renderScale=0.85 at zero-width RenderTextureDesc. Existing viewport script explicitly clamps to 1px. Raise canvas CSS and JS minimum dimensions to 64px while keeping actual viewport fit at normal sizes; no change to URP effects or quality. Clean output required if old viewport patch is present. Retest pause/resume, resize and QA Tool. Exact mobile size threshold requires verification; shrinking startup payload remains a priority.

## Viewport regression and Brotli trial

QA Tool retest: 1.37 s local, initial download 65 MB; neither SphereCollider nor RenderTextureDesc recurred in supplied log. Mobile catalog warning persists. Switch both release profiles from gzip to Brotli (fallback remains disabled), matching default ProjectSettings. Playgama platform-specific requirements explicitly permit Brotli for Facebook; verify actual QA and hosting Content-Encoding: br before release. Existing Tools/serve_webgl.py supports .br. Compare cold-cache size/time; preserve previous gzip output. No gameplay or texture changes in this trial. Mobile catalog cutoff remains unverified.

## Compression correction

Unity's WebGLCompressionFormat enum is Brotli=0, Gzip=1, Disabled=2. Previous trial accidentally selected gzip. Correct all three Web profiles and global settings. Uploaded index.html confirms the startup barrier is present; recurrent prefs failure remains unexplained. Add explicit initialized save-state check before starting Unity in newly generated HTML; do not substitute empty saves. Inspect browser Network/Console if it recurs. Clean build and browser test pending.

## Brotli verified, city texture trial B

Using the Unity API preflight, Desktop Release changed from Gzip to Brotli. Actual output: data.br 50972 KiB, wasm.br 6156 KiB, framework.js.br 67 KiB, loader.js 28 KiB; total approximately 55.9 MiB versus 63.3 MiB gzip. QA Tool local: game_ready, 1.61 s, initial download 56 MB; mobile-catalog size warning persists. Supplied log confirms .br download; no recurrence of prefs, missing SphereCollider or zero-width RenderTextureDesc in that run. Hosted production Brotli and mobile QA still pending.

Trial B: WebGL max size 1024 -> 512 for WinGlass-01.tga, WinGlass-03.tga, WinGlass-04.tga, Wins-02.tga, Wins-02_N.tga, Grass-01.tga. Keep existing mipmaps, compression, alpha, texture type and source GUIDs. No changes to primary atlas, roads, trees, lighting, meshes or physics. Run Check and Set Brotli before building, retain prior 56 MB QA baseline, compare cold-start payload and window/grass visuals day and night. Obtain packed report if available; no predicted compressed savings claimed.

## Texture trial B and recurring startup failure

Data.br 50059 KiB (previous 50972), approximately 0.89 MiB saved. Build And Run first navigation again starts Unity without bridge state; supplied console has no initialization message and fails in PrefHas. Root cause not established. Previously re-opening the tab worked and generated index.html contained the gate. Isolate possible editor auto-run timing by using Build only and serving completed output via Tools/serve_webgl.py --directory PATH. Server checks entrypoint markers and bridge files before serving; no save fallback or blank progress writes. Retest fresh navigation and QA Tool. Do not advance asset experiments until startup is reliable.

## Mesh trial result and UI texture trial

Build-only local server verified texture trial B: data.br 50059 KiB, QA 55 MB, 1.22 s. Mesh Low trial BB-001–012: data.br 50088 KiB, QA 55 MB, 1.29 s; no benefit, reverted in 302bd99b. Tabler importer changes persisted separately. Handbrake WebGL max 256 RGBA32, mipmaps off: data.br 50036 KiB, QA 55 MB, 1.32 s; user pushed the change. Mobile warning remains. Next trial: Tabler WebGL max 128 instead of 256, explicit importer order after base importer. Reimport all icons, verify readability at desktop/mobile UI sizes, compare packed icon bytes and data.br against 50036 KiB. No Unity build or visual validation performed remotely.

## Batched texture trial C

Tabler 128 trial: data.br 50001 KiB vs 50036 KiB, QA 55 MB, 1.31 s, game_ready; user pushed importer settings. Batch C: three night-window textures and Trees-01 WebGL 1024 -> 512; six intro slides 1536 -> 1024; preserve formats, alpha, mipmaps and GUIDs. Compare against 50001 KiB; inspect night windows, foliage and intro text. Main menu, roads, primary atlas and SDK unchanged. Unity build and visual QA pending.

## Batch C result and batch D

Batch C: data.br 46223 KiB vs 50001 KiB, QA 51 MB vs 55 MB, local 1.36 s, game_ready. Visual QA not explicitly confirmed. Mobile warning persists. Batch D: four FS013 panoramas and MainMenu WebGL max 1024; Vegetation, WinGlass-01-Night, WinGlass-05 and Volumetric3D max 512. Existing/default platform format and compression retained; new WebGL overrides where absent. Check sky gradient, menu, foliage alpha and streetlight/night appearance. Baseline 46223 KiB. Music deferral postponed to separate runtime trial. Unity verification pending.
