# SKILLS — RPG Maker MZ Game Shell

The skills, tools, and services this template uses. The style skill
for a file's language is loaded **before** the code is written or
reviewed. Engine specifics live in `SPEC.md`; repo-wide law lives in
the repository root `AGENTS.md`.

## Skills and tools

| Name | Kind | Used for |
|---|---|---|
| tiger-style-typescript | skill — `~/workspace/custom-skills/local-tiger-style-typescript/` | JavaScript discipline for the plugins. There is no separate tiger-style-javascript skill; TypeScript is the sibling discipline and its rules (explicit contracts, bounded work, no hidden behaviour, strict null handling) apply to this template's vanilla JS. |
| tiger-style-json | skill — `~/workspace/custom-skills/local-tiger-style-json/` | Any JSON the project adds around the template (for example editor data exports reviewed by hand). The plugins themselves ship no JSON. |
| theodolite | MCP (estate bridge) | LSP validation of every touched file. Server id for JavaScript: **`typescript-language-server`**. Zero error diagnostics before a file is called done. |
| RPG Maker MZ | app | The engine: editor, Plugin Manager, playtest (NW.js), and the core scripts the plugins alias. Playtest launching is editor-side. |
| node | tool | The syntax gate: `node --check` on each plugin (commands in `SPEC.md` §4). Syntax only — MZ core globals do not exist under plain Node. |
| prettier | tool (formatter) | Formatting for JavaScript; Diver wires prettierd, then prettier. Keep the MZ-style 4-space indent. |
| Aseprite | skill + app — `~/workspace/custom-skills/local-tiger-style-aseprite/` | The asset pipeline for sprites and tilesets a project adds around this shell (battlers, characters, tiles). Not used by the plugins themselves. |
| mdBook | tool | Builds this template's book: `mdbook build .` in this directory (output `book-out/`, a build artifact, never committed). Shared theme: `../theme-torogao`. |

## Bloch lane law

Lesson sharing is governed by **content label, not origin**.

- Every lesson record is born with three fields — `origin_lane`,
  `content_label`, and `evidence` — or it does not exist. Use
  `docs/bloch-lesson-template.md` in this directory.
- An unclassified lesson is treated as `nsfw`, **fail-closed**.
- **NSFW-pure lessons never** flow to SFW / qompassai surfaces.
- NTRanch-origin lessons transfer to qompassai surfaces **only** when
  labelled SFW (origin is kept on the record).
- SFW lessons may flow into NTRanch.
- `nsfw` → `sfw` reclassification is a gated Owner/Pax act, never an
  agent's unilateral edit.

Template: [`docs/bloch-lesson-template.md`](docs/bloch-lesson-template.md)
