# Tabler Icons — original SVG source archive

This directory stores the complete, MIT-licensed Tabler outline SVG source set
outside Unity's `Assets/` directory. Unity must not import or index 5,184
unused SVG assets when only a curated set is needed.

- Source: https://github.com/tabler/tabler-icons
- License notice: `Assets/ThirdParty/LICENSE`
- Source files: `ThirdParty/TablerIcons/outline/`
- Unity-ready sprites: `Assets/Resources/MotorCity/UI/Icons/Tabler/`
- Export: `python Tools/export_tabler_ui_icons.py`

Only the exported PNGs are imported by Unity. The original `.svg.meta` files
are retained here for traceability but are not required by Unity.
