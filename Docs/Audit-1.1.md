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
