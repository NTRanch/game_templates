# SKILLS — Godot Game Shell

Tools and skills for work in this template. Load the skill before
doing the work it covers; the theodolite route is the diagnostics
gate for every `.gd` file (see SPEC.md §4).

| Name | Kind | Where it lives | When to use |
| --- | --- | --- | --- |
| tiger-style-gdscript | skill | `~/workspace/custom-skills/local-tiger-style-gdscript/` | Writing or reviewing any GDScript: static types, control-flow validation, bounded work, official file layout. |
| tiger-style-godot | skill | `~/workspace/custom-skills/local-tiger-style-godot/` | Godot CLI work: headless import, check-only, scene/resource discipline, exit-code + sentinel + stderr gates. |
| tiger-style-aseprite | skill | `~/workspace/custom-skills/local-tiger-style-aseprite/` | Sprite and sprite-sheet work for the game (Aseprite CLI/Lua). |
| local-blender-rigify | skill | `~/workspace/custom-skills/local-blender-rigify/` | Rigging 3D characters in Blender (Rigify) for import into the game. |
| tiger-style-blender | skill | `~/workspace/custom-skills/local-tiger-style-blender/` | Driving Blender headlessly: census, conversion, renders, rigging gates for 3D assets. |
| theodolite | MCP | bridge id `gdscript` | Headless LSP diagnostics for `.gd` files; zero error diagnostics before a file is done. `.tscn` files have no route (see SPEC.md). |
| Godot 4.7.2 binary | app/tool | `/usr/bin/godot` | Import, check-only gates, and the headless smoke run. |
| godot-gdscript-toolkit (gdformat / gdlint) | tool | where installed | Formatting and linting GDScript; not installed on the reference machine, so format by hand per the official style until it is. |
| mdBook | tool | `mdbook` on PATH, config `book.toml` | Building this template's book from README/SPEC/SKILLS/AGENTS. |

## Bloch lane law

Lesson sharing is governed by content label, not origin:

- Every lesson record is born with `origin_lane` +
  `content_label` + `evidence` — the birth-metadata gate. No
  unlabeled promotions, ever. Use
  `docs/bloch-lesson-template.md` for the record shape.
- Unclassified lessons are treated as `nsfw`, fail-closed:
  when in doubt, the stricter label wins.
- NSFW-pure lessons never flow to SFW / qompassai surfaces.
- SFW lessons may flow into NTRanch.
- Reclassification `nsfw` → `sfw` is a gated Owner/Pax act, never
  an author's self-service edit.
