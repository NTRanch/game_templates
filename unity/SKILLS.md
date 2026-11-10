# SKILLS — Unity Game Shell

The skills, tools, and services that apply to work on this
template. Load the skill before doing the kind of work it covers;
do not work from memory of a skill's contents.

| Name | Kind | Where it lives | When |
| --- | --- | --- | --- |
| tiger-style-unity | skill | `~/workspace/custom-skills/local-tiger-style-unity/` | Any Unity work: CLI and batchmode routes, project and Editor handling, `.meta` discipline, URP-only content. Load before touching project structure or proposing an editor run. |
| tiger-style-csharp | skill | `~/workspace/custom-skills/local-tiger-style-csharp/` | Every `.cs` file written or reviewed — contracts, nullable discipline, bounded work, CSharpier formatting. Prerequisite to tiger-style-unity. |
| theodolite | MCP | Estate theodolite bridge; server id `csharp-language-server` | The validation gate for every edited `.cs` file: zero error diagnostics before the file is called done (see `SPEC.md` §4.2). |
| Unity editor + unity CLI | app / tool | The project's pinned Editor (read `ProjectSettings/ProjectVersion.txt`) and the `unity` CLI | In-editor smoke, batchmode compile checks, builds. GUI work is the last resort, after CLI and batchmode routes. |
| Aseprite skill (tiger-style-aseprite) | skill | `~/workspace/custom-skills/local-tiger-style-aseprite/` | 2D sprite and UI art pipelines for a project built on this shell, where applicable. |
| Blender skill (tiger-style-blender) | skill | `~/workspace/custom-skills/local-tiger-style-blender/` | 3D asset work for a project built on this shell, where applicable. |
| mdBook | tool | `book.toml` in this directory | Building the rendered docs: `mdbook build .` from this directory; output goes to `book-out/` (generated, git-ignored). |

## Bloch lane law

Every lesson learned while working on this template — a gotcha, a
fix, a measured behaviour — is recorded as a lesson, and every
lesson record is governed by the lane law:

- **Birth-metadata gate:** every lesson record is born with
  `origin_lane` + `content_label` + `evidence`, or it does not
  exist. There are no unlabeled promotions.
- **Fail-closed default:** an unclassified lesson is treated as
  `nsfw`, never as safe.
- **Flow rules by content label:** NSFW-pure lessons never flow to
  SFW / qompassai surfaces. SFW lessons may flow into NTRanch.
  Lesson sharing is governed by content label, not by origin.
- **Reclassification is gated:** changing a lesson from `nsfw` to
  `sfw` is a gated Owner/Pax act, never an editor's quiet edit.

Use the record template at `docs/bloch-lesson-template.md` for
every lesson; fill in the birth metadata first.
