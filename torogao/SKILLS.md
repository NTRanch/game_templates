# SKILLS — Torogao Game Shell Template

The tool map for work in this template: which skill, MCP server, or
tool fits which job, and where it lives. Load the skill for the
language of any file *before* writing or reviewing it. Facts here
outrank memory; if a row goes stale, fix the row.

## Map

| Name | Kind | Where | When |
| --- | --- | --- | --- |
| tiger-style-rust | skill | estate custom skills (`tiger-style-rust`) | Writing or reviewing any Rust in this template — contracts, bounds, error discipline, the review checklist. |
| local-tiger-style-mcp | skill | estate custom skills (`local-tiger-style-mcp`) | Writing or reviewing MCP servers and tools — bounded tool surfaces, complete schemas, annotations from birth. |
| theodolite | MCP | estate MCP fleet; bridge ids `bacon-ls` (diagnostics owner) + `rust-analyzer` (semantics) for `.rs`, `ron-lsp` for `.ron`, `taplo` for `.toml` | LSP validation of every touched file: zero error diagnostics before the file is called done. |
| torogao | MCP | estate MCP fleet (the engine's own MCP seam) | Engine and session/host work: wiring this shell to the real `torogao-session` host, engine state, build/runtime questions. |
| fathom | MCP | estate MCP fleet (clean-room reverse-engineering MCP) | Studying an existing game to feed Torogao: dynamics, constants, animation behaviour. Techniques and measurements transfer; copied assets, curves, and code never do. |
| lumen | MCP | estate MCP fleet | 2D puppet/sprite work: sprite-to-rig production and the puppet evaluator of record for 2D animation quality. |
| atelier | MCP | estate MCP fleet (asset workshop seam) | Asset intake and workshop work: screening, converting, and preparing models, rigs, and packs for a game built on this template. |
| ardor | MCP | estate MCP fleet (NSFW game/app maker support seam) | Support work for NSFW games and apps built on the estate's seams. NTRanch lanes only — never a qompassai surface. |
| bloch | MCP | estate MCP fleet (lesson sharing) | Recording or retrieving a lesson from any work here — under the lane law below, with birth metadata, or not at all. |
| rose / phlow | agent runtime | estate runtime (rose backend, phlow CLI/TUI/MCP) | Bounded local-model work: drafting, sweeping, and measuring with local lanes before spending cloud or human attention. |
| cargo / rustc | tool | the pinned Rust toolchain | Build, test, clippy, fmt — the gates in `SPEC.md` §4. |
| mdBook | tool | `mdbook build .` in this directory | Building this template's book from `book.toml` + `book/src/` (output `book-out/`, generated). |
| Aseprite skill | skill | estate custom skills (`tiger-style-aseprite`) | Sprite sheets, palettes, and scripted pixel work for a game's 2D assets. |
| Blender skill | skill | estate custom skills (`tiger-style-blender`) | `.blend`/GLB work, rigs, and shape keys for a game's 3D assets. |

## Bloch lane law (in full)

Lesson sharing is governed by *content label*, not origin. This is
the law for every bloch record created from work in this template:

- **Birth metadata or the record does not exist.** Every lesson is
  born carrying `origin_lane` + `content_label` + `evidence`.
  A record missing any of the three is refused at promotion — no
  unlabeled lessons, ever.
- **Unclassified defaults to `nsfw`, fail-closed.** A lesson whose
  content label is missing or unclear is treated as NSFW until it is
  classified.
- **NSFW-pure lessons never flow to qompassai surfaces.** Not to
  qompassai repos, not to qompassai callers, no exceptions.
- **NTRanch-origin lessons transfer to qompassai only when SFW**,
  and the origin is kept on the record when they do.
- **SFW lessons may flow into NTRanch** freely.
- **Reclassification `nsfw` → `sfw` is a gated act** — Pax/Owner
  only, never the recording agent's own call.

The record format is `docs/bloch-lesson-template.md` in this
directory. Use it verbatim.
