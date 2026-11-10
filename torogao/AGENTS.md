# AGENTS.md — Torogao Game Shell Template

Working rules for any agent working in this template. The repo-wide
`AGENTS.md` at the repository root is the general law; this file is
the Torogao-specific layer. Where they disagree on this template's
files, this file wins.

## Before anything

- **Read `SPEC.md` first.** It is the contract: file manifest,
  invariants, growth checklist, gates. `SKILLS.md` is the tool map.
- **Facts come from `SPEC.md` / `SKILLS.md`, not from memory.** If a
  fact you need is not in them, verify it on the machine or in the
  primary source, then write it down in the document that should have
  had it.

## Gates — before claiming done

Run them, report real numbers, never claim a gate you did not run:

```sh
cargo build
cargo test                      # report the counts
cargo run -p shell-app          # expect: OK shell-app
cargo clippy -- -D warnings
cargo fmt --check
```

- **Theodolite:** every touched file validated through the bridge —
  `bacon-ls` (diagnostics owner) + `rust-analyzer` (semantics) for
  `.rs`, `ron-lsp` for `.ron`, `taplo` for `.toml` — zero error
  diagnostics before a file is called done. A file type with no
  bridge route is named in your report as not-LSP-validated, never
  silently skipped.
- **Formatter:** rustfmt is the mechanical authority.
- Tests stay half validation / half adversarial, as in
  `crates/shell-core/tests/roundtrip.rs`. New data paths get an
  adversarial case in the same change.

## Load-bearing rules (refusals, condensed)

- The save envelope is load-bearing: format marker +
  `format_version` + the migration chain in `migrate()`. No
  unversioned save writes. Additive fields get `#[serde(default)]`
  before anyone bumps a version.
- Slot validation is a security boundary. Do not widen `SLOT_NAMES`,
  do not accept separators, do not build a path from an unvalidated
  slot name.
- Settings always have a default and a `validate()` clamp — all
  three registration places (field + default, clamp, UI) or the
  setting does not ship.
- `shell.rs` stays pure: no clock, no I/O, no environment. Timestamps
  are caller-supplied everywhere in the core.
- No `unsafe` (`#![forbid(unsafe_code)]`), no `unwrap`/`expect` on
  data paths, no new external dependencies without a stated reason.

## MCP fleet etiquette

- **torogao** MCP for engine and session/host work.
- **fathom** for reverse-engineering intake of studied games —
  clean-room discipline: techniques and measurements transfer,
  copied assets, curves, and code never do.
- **lumen** for 2D puppet/sprite work and 2D animation evaluation.
- **atelier** for asset workshop work.
- **ardor** for NSFW game/app support work — NTRanch lanes only.
- **bloch** for lessons, under the lane law in `SKILLS.md`: birth
  metadata or the record does not exist.
- **theodolite** for diagnostics on every touched file.

## Estate rules that bite here

- **Audio silence rule.** Audio is generated and verified as files
  only, never played aloud on primo. Primo runs only on a null/dummy
  sink; if a physical-sink stream exists, the audio work does not
  run. This template's volume settings are data — nothing here plays
  sound — but any engine work grown from this template inherits the
  rule.
- **NSFW lane separation.** NSFW work stays in NTRanch lanes and
  repos, never in qompassai surfaces. Bloch lane law governs every
  lesson crossing between lanes: NSFW-pure lessons never reach
  qompassai; unclassified is `nsfw`, fail-closed.
- **No CI, no Docker/Podman.** Validation is local, with the commands
  above.
- **Cleanup.** `target/` and `book-out/` are generated; delete them
  at task end.
