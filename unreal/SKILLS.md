# SKILLS — Unreal Game Shell

The skill and tool map for working on this template. Skills live in
`~/workspace/custom-skills/`; load the matching skill before writing
or reviewing code in its language.

| Need | Use | When / notes |
| --- | --- | --- |
| Unreal Engine work (projects, commandlets, packaging, Editor discipline) | skill `tiger-style-unreal` (`local-tiger-style-unreal`) | Always for `.uproject` work. Governs where this file and the C++ skill disagree on UE code. Scratch copies for headless runs; project content is executable; judge UAT/commandlet runs by log + outputs, never exit code alone. |
| C++ (all `Source/GameShell/*.h/.cpp`) | skill `tiger-style-cpp` (`local-tiger-style-cpp`) | Contracts, bounded work, no silent failures. UE idiom (tabs, `F`/`U`/`A`/`E` prefixes, reflection macros) wins over generic C++ naming where they conflict. |
| C# (`GameShell.Build.cs`) | skill `tiger-style-csharp` (`local-tiger-style-csharp`) | Module-rules files are C#; keep dependency lists minimal and explicit. |
| LSP validation | MCP **theodolite** | Honest status: the bridge has **no C++ route**, so the shell's `.h`/`.cpp` are **not-LSP-validated** (named, per estate rule — never silently skipped); clangd in Neovim is the in-editor surface and a UBT build is the gate. C# (`Build.cs`) routes to the bridge's Roslyn server (`csharp-language-server`). `.ini` / `.uproject` have no route; `.uproject` is gated by `python3 -m json.tool`. |
| Live Editor GUI work | MCP **Seahawk** | The estate's broker for UE 5.8's built-in MCP: `gui.observe` → `gui.ground` → `gui.act_verified`, execute-class default-deny, append-only op log. Live-editor work goes through Seahawk — never raw GUI automation. Headless routes (commandlets, Editor Python on scratch copies) beat GUI work whenever they can do the job. |
| Headless engine runs / packaging | tool `UnrealEditor-Cmd`, tool `RunUAT` | Command forms in SPEC.md §4. Scratch copies only; log sentinel + declared outputs are the verdict. |
| Asset pipeline (models, sprites for the game built on this shell) | skills `tiger-style-blender`, `tiger-style-aseprite` | Assets are made with those pipelines and imported in the Editor; the shell itself ships no assets. |
| Book build | tool `mdbook` | `mdbook build unreal` from the repo root; output `book-out/` is a build artifact, deleted at task end. |

## Bloch lane law

Lesson sharing across the estate is governed by **content label,
not origin**. The rules, in substance:

- Every lesson record is born with **birth metadata**:
  `origin_lane` + `content_label` + `evidence`. A record lacking
  any of the three does not get promoted or shared — it does not
  exist, for sharing purposes.
- An **unclassified** lesson is treated as `nsfw`, **fail-closed**.
- **NSFW-pure lessons never** flow to SFW or qompassai surfaces.
- **SFW lessons may flow into NTRanch** (this repo); NTRanch-origin
  lessons flow outward only when their content label is SFW, with
  origin kept on the record.
- Reclassification (`nsfw` → `sfw`) is a gated Owner/Pax act, never
  an author's self-service edit.

Use `docs/bloch-lesson-template.md` for every new record.
