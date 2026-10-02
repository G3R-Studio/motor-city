# Vehicle paint mesh contract

Use these object/mesh names when authoring vehicles:

| Role | Name |
| --- | --- |
| Paintable body | `body` |
| Paintable front rims | `front_wheels_misc` |
| Paintable rear rims | `rear_wheels_misc` |
| Paintable rims in a combined wheel source | `all_wheels_misc` |

Names are case insensitive. Spaces and dots are accepted in place of underscores;
Blender duplicate suffixes such as `.001` and Unity clone suffixes are accepted.
The renderer's mesh name and object name are both inspected, including skinned
meshes. `body_misc` is never selected as body paint.

Keep tires, glass, lights and trim in other meshes. Every material slot in a
designated paint mesh receives the selected tint, regardless of material name.
The texture is retained: paint UVs should sample white or neutral texture areas.
Black texels remain black under multiplication. Standard/URP Lit color properties
are supported; custom shaders must expose and use `_BaseColor` or `_Color`.

If any designated rim mesh exists, only designated rim meshes are painted.
Models without designated rim meshes retain the old wheel/material detection.
The ARCADE street car retains its five authored paint material variants.

OBJ imports containing these roles get explicit `g` declarations for each `o`
declaration when the export lacks groups. This is an idempotent source-file update
by `VehicleObjPaintPostprocessor`; geometry, UVs and material assignments are
unchanged. Existing groups are preserved. Model hierarchy preservation is enabled.
Export object groups directly when possible. Reimport existing OBJ assets after
installing these scripts, and rebuild generated vehicle prefabs if Unity reports
missing source references after the hierarchy change.

The current AMG GT front wheel OBJ contains only `front_wheels`, with no separate
rim mesh. Its rear wheel OBJ contains `front_wheels_misc` (despite the filename).
Only that designated mesh will be painted once imported; to enable front rim paint,
the artist must separate front rims from tires and name them `front_wheels_misc`.
