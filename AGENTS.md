# AGENTS.md — working in game_templates

Operating rules for any agent (Pax, Claude Code, Codex, local models)
working in this repo or in a project cloned from one of its templates.
The per-engine `SPEC.md` is the deep contract for a template,
`SKILLS.md` is its tool map, and each engine directory has its own
`AGENTS.md` with the engine-specific rules. This file is the
repo-wide law; where it and an engine document disagree, the engine
document wins for that engine's files.

## Discipline

- **Tiger Style, per language.** Safety > performance > developer
  experience. Explicit contracts on every public surface; bounded
  work with named constants; no hidden behaviour. The style skill for
  the file's language (see the engine's `SKILLS.md`) is loaded before
  code is written or reviewed.
- **Verify before claiming.** A thing is done when its gate ran and
  produced numbers. Label every result honestly: a real run is a real
  run, a static review is "static", and a check that could not run is
  named as not run — never silently upgraded.
- **The shared contract is load-bearing.** Settings always have a
  default and a clamp; saves are always versioned envelopes with
  atomic writes and corrupt-file quarantine; save/settings data is
  untrusted input, validated with control flow that survives release
  builds (never `assert` alone). Do not weaken these in a template
  without an Owner ruling.
- **Theodolite rule.** Every file you edit is validated through the
  estate theodolite bridge (MCP) with the language server for its
  type: zero error diagnostics before the file is called done. A file
  type with no bridge route is named in your report as
  not-LSP-validated — never silently skipped. The bridge server id
  per engine is in that engine's `SPEC.md` / `AGENTS.md`.

## Repo law

- **No CI.** Absence of CI is by design; validation runs locally with
  the commands in each `SPEC.md`.
- **No Docker or Podman**, anywhere, for anything in this repo.
- **No personal names in artifacts.** Author and credit lines read
  "Nice Thoughts Ranch" (or "Owner" for the person). License and the
  main README are the standing exceptions pattern — in this repo the
  LICENSE carries the studio name and that is all.
- **English-canonical text** in every shipped file.
- **Bloch birth-metadata law.** Every lesson record is born with
  `origin_lane` + `content_label` + `evidence`, or it does not exist.
  Unclassified lessons are treated as `nsfw`, fail-closed. NSFW-pure
  lessons never flow to SFW/qompassai surfaces; SFW lessons may flow
  into NTRanch. Use `docs/bloch-lesson-template.md` in the engine's
  directory.
- **Push discipline.** No pushes to the Owner's repos without the
  Owner's explicit, per-scope confirmation; never force-push. The
  sole standing exception was this repo's own initial build, which
  the Owner commissioned directly; it does not extend to later work.
- **Task-end cleanup.** Build artifacts (`target/`, `book-out/`,
  `.godot/`, editor caches) and scratch trees are deleted at task
  end. Deliverables, sources, and gate evidence are kept. When in
  doubt about reuse, say so in the task report instead of silently
  keeping or deleting.

## Where things live

- One engine per directory; a change to the shared contract is made
  in all six templates or recorded as a deliberate per-engine
  deviation in that engine's `SPEC.md`.
- Books build with `mdbook build <engine-dir>` from the repo root;
  the shared theme is `theme-torogao/` and is edited once, centrally.
