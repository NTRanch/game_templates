# SKILLS — Ren'Py Game Shell

Tools and skills this template assumes. Only genuinely applicable
entries are listed.

| Name | Kind | Where | When |
| --- | --- | --- | --- |
| theodolite (bridge id `renpy-language-server`) | MCP | Estate theodolite bridge; rockettap `renpy-language-server` v0.2.1 (community server, pinned) | Headless diagnostics for every touched `.rpy` file — zero error diagnostics before a file is done; heed the `possibly_incomplete` caveat in `SPEC.md` §4 |
| Ren'Py SDK | App / tool | renpy.org — the Ren'Py 8 SDK (`renpy.sh` / launcher) | Lint gate (`renpy.sh <project> lint`), compile check, running and building the game |
| tiger-style-python | Skill | `~/workspace/custom-skills/local-tiger-style-python/` | Discipline for the Python inside `.rpy` blocks: explicit contracts, bounded work, validate untrusted data with control flow |
| Aseprite | App / tool | Sprite / asset pipeline tooling | Authoring and exporting sprite and UI image assets the game displays |
| mdBook | App / tool | `book.toml` in this repo | Building the documentation book from the root docs (`mdbook build`) |

## BLOCH LANE LAW

Lesson sharing across the estate is governed by **content label, not
origin**:

- Every lesson is born with three fields, from birth, no exceptions:
  `origin_lane`, `content_label`, and `evidence`. A lesson missing any
  of them does not get promoted or shared.
- An unclassified lesson defaults to **nsfw** — fail-closed. It is
  treated as NSFW until a gated reclassification says otherwise.
- **NSFW-pure lessons never reach SFW / qompassai surfaces.** No
  exceptions for usefulness.
- **SFW lessons may flow into NTRanch.** NTRanch-origin lessons transfer
  to qompassai surfaces **only** when their content label is SFW, with
  the origin kept on the record.
- Reclassification (especially nsfw → sfw) is a gated act by the Owner
  or Pax — never a drive-by edit, never inferred from context.

Use the template at `docs/bloch-lesson-template.md` for every new
lesson record.
