# SPEC — Torogao Game Shell Template

The contract for this template. `README.md` is the map, `SKILLS.md`
is the tool map, `AGENTS.md` is the working rules; this file is the
deep contract. Where another document disagrees with this one, this
one wins for this template's files.

The template is a Rust workspace of two crates: `shell-core`, the
engine-agnostic shell core, and `shell-app`, a headless demo binary
that proves the flow end to end. Edition 2021, `rust-version` 1.74,
license Apache-2.0, credit Nice Thoughts Ranch. External dependencies
are exactly two: `serde` (with `derive`) and `ron` 0.8, declared once
in the workspace manifest. That dependency budget is a bound, not a
suggestion (see "What not to add").

## 1. File manifest

### Workspace root

| File | Purpose / contract |
| --- | --- |
| `Cargo.toml` | Workspace: `resolver = "2"`, members `crates/shell-core` + `crates/shell-app`; `[workspace.package]` pins edition 2021, license Apache-2.0, rust-version 1.74; `[workspace.dependencies]` holds the only external deps (`serde`, `ron`). |
| `README.md` | Orientation: what the template is, layout, how to run the demo, verification status. |
| `SPEC.md` | This contract. |
| `SKILLS.md` | The tool/skill map and the bloch lane law in full. |
| `AGENTS.md` | Working rules for agents: gates, refusals, etiquette. |
| `docs/bloch-lesson-template.md` | The lesson record format. Every lesson is born with `origin_lane` + `content_label` + `evidence`, or it does not exist. |
| `book.toml`, `book/src/*` | mdBook config and chapter stubs that `{{#include}}` the four root documents. Build output is `book-out/` (generated; never committed). |

### `crates/shell-core` (library, `shell_core`)

| File | Purpose / contract / bounds |
| --- | --- |
| `Cargo.toml` | Package `shell-core`; lib target name `shell_core`; deps only the workspace `serde` + `ron`. |
| `src/lib.rs` | Crate docs (what this is, the contract summary, how a game grows it), `#![forbid(unsafe_code)]`, module declarations, and re-exports of the main types (`ShellError`, `ShellResult`, `Settings`, `SETTINGS_FORMAT`, `SaveEnvelope`, `SaveMetadata`, `SaveStore`, `CURRENT_FORMAT_VERSION`, `SAVE_FORMAT`, `Shell`, `Screen`, `SettingsReturnTo`, `ShellEvent`, `ShellAction`). The public surface is exactly these re-exports plus each module's own public items. |
| `src/error.rs` | `ShellError` with variants `Io`, `RonSerialize`, `RonDeserialize`, `UnsupportedVersion { found, current }`, `InvalidSlot(String)`; `Display` + `std::error::Error` + `From` impls for `std::io::Error` and `ron::error::SpannedError`. Hand-rolled: no `thiserror`, no `anyhow`. `ShellResult<T>` alias. Bounds: error text carries bounded context (a version number, a slot name) — never file contents, never payloads. |
| `src/settings.rs` | `Settings` record + load/save. Invariants below. |
| `src/save.rs` | `SaveStore` + `SaveEnvelope` + migration chain. Invariants below. |
| `src/shell.rs` | `Shell` state machine. Pure and total. Invariants below. |
| `tests/roundtrip.rs` | Integration tests, half validation / half adversarial (listed in §4). Uses only std + the crate + `ron` values; temp dirs are hand-rolled (no `tempfile` dev-dependency). |

#### `settings.rs` invariants

- Every field has a default; the struct carries `#[serde(default)]`,
  so a file missing fields repairs instead of failing.
- `SETTINGS_FORMAT` is `"ntranch-shell-settings/1"`. The file on disk
  is a `SettingsFile { format, settings }` wrapper; the string marker
  exists because settings files in the estate predate the numeric
  envelope pattern used by saves.
- Bounds are named constants: volumes `0.0..=1.0`
  (`MIN_VOLUME`/`MAX_VOLUME`), screen shake `0.0..=1.0`, window width
  `640..=7680`, window height `360..=4320`, defaults width 1280,
  height 720, volumes 1.0.
- `validate(&mut self)` clamps every bounded field and is applied to
  every value that leaves the module. Non-finite floats repair to the
  field default via an explicit `is_finite()` check — NaN comparisons
  are always false, so a bare `clamp` cannot be trusted with NaN.
- `load` never panics and never errors on bad content: missing file →
  defaults; unparseable, invalid-UTF-8, or wrong-marker file →
  quarantine (rename to `<name>.corrupt`, best-effort) + defaults;
  any other I/O failure → `Err(Io)`.
- `save` is atomic (write `<path>.tmp`, rename), creates parent
  directories, and writes a validated copy.

#### `save.rs` invariants

- A save file is a `SaveFile { format, envelope }` wrapper with
  `SAVE_FORMAT = "ntranch-shell-save/1"`; the envelope carries
  `format_version`, with `CURRENT_FORMAT_VERSION = 1`.
- `SaveEnvelope { format_version, saved_at_unix, playtime_seconds,
  label, payload }`. `label` carries `#[serde(default)]` — that is
  the v0→v1 story (see migration).
- This module never reads the clock. `saved_at_unix` (and
  `playtime_seconds`) are caller-supplied parameters of `save()`.
- Slot validation is the security boundary: `SLOT_NAMES` is exactly
  `["slot1", "slot2", "slot3", "auto"]`, plus test slots prefixed
  `__`. Anything else returns `InvalidSlot` *before* any path is
  built, so a slot name (untrusted input — it may come from a file
  listing, a mod, or a UI) can never escape the saves directory.
  `exists()` on an invalid slot returns `false` rather than erroring.
- `load`: empty slot → `Ok(None)`; corrupt/invalid-UTF-8/wrong-marker
  → quarantine + `Ok(None)`; older version → `migrate()` +
  `Ok(Some)`; newer version → `Err(UnsupportedVersion)` with the
  file left untouched (data-level refusal is an error; refusal UX is
  the game's); other I/O failure → `Err(Io)`.
- `slots()` returns present canonical slots in `SLOT_NAMES` order;
  test slots are never listed.
- Migration: `migrate()` steps `while version < CURRENT`. The rule is
  additive-fields-first: prefer `#[serde(default)]` on a new field
  over a version bump; bump and add a step only when the *meaning* of
  existing data changes. `migrate_v0_to_v1` is the worked example: it
  only advances the stamp, because `label`'s serde default already
  did the work.

#### `shell.rs` invariants

- Pure: no I/O, no clock, no reads of the environment. Total:
  `handle()` is defined for every (state, event) pair; invalid
  transitions change nothing and return `ShellAction::None`. It never
  panics. This purity is the testability contract — the whole flow is
  testable in a unit test with no fixtures.
- Screens: `AgeGate`, `Title`, `Game`, `Pause`. Settings are an
  overlay, not a screen: `OpenSettings` records the caller
  (`SettingsReturnTo::Title` / `::Pause`) and does not change
  `screen()`. Read `settings_return_to()` *before* sending
  `CloseSettings`; the close clears the record, and the return screen
  is simply `screen()`, which never moved.
- Transition table (anything not listed is ignored → `None`):
  `Boot` → age gate if enabled and not yet confirmed (`ShowAgeGate`),
  else `Title` (`ShowTitle`); `AgeConfirmed` at the gate → `Title`;
  `AgeDeclined` at the gate → `Quit`; `NewGame` at title → `Game`
  (`StartGame`); `ContinueGame` at title, only when
  `continue_available` → `Game` (`StartGame`); `OpenPause` from game
  only → `Pause` (action `None` by design — the host renders pause
  from `screen()`); `ClosePause` → `Game` (`ResumeGame`);
  `OpenSettings` from title or pause → `ShowSettings`;
  `CloseSettings` when open → `HideSettings`; `QuitToTitle` from
  pause or game → `Title` (`ShowTitle`); `QuitRequested` anywhere →
  `Quit`.
- `AGE_GATE_DEFAULT_ENABLED = false`: a template must not gate a game
  that never asked.
- The shell tracks screens only. What the game state is when the
  player quits to title is the game's problem.

### `crates/shell-app` (binary, `shell-app`)

| File | Purpose / contract |
| --- | --- |
| `Cargo.toml` | Package `shell-app`; bin target name `shell-app`; deps: `shell-core` (path) + workspace `ron`. |
| `src/main.rs` | The scripted headless smoke. Work dir from argv[1] or a per-process temp dir. Sequence: load settings (defaults) → set `music_volume = 0.5` → save → reload → assert; `SaveStore` writes `slot1` with a small RON map payload, label `"Demo start"`, fixed `saved_at_unix = 0` (the core never reads the clock; the demo passes a constant for determinism); shell walk `Boot` → `NewGame` → `OpenPause` → `OpenSettings` (caller asserted `Pause`) → `CloseSettings` → `ClosePause` → `QuitToTitle` → `ContinueGame` (continue availability taken from the store). Success prints `OK shell-app` and exits 0; any failure prints `FAIL shell-app: ..` to stderr and exits 1. No `unwrap`/`expect` on data paths; failures travel through a checked helper. |

## 2. What to add (growing a real game)

In this order:

1. **Grow the payload.** Replace `ron::Value` in `SaveEnvelope` with
   the game's own state struct (`Serialize`/`Deserialize`). New state
   fields are additive first: give each a `#[serde(default)]` so old
   saves still load. Only when the *meaning* of existing data changes
   do you bump `CURRENT_FORMAT_VERSION`, add a step to `migrate()`,
   and add a migration test modeled on `version_zero_migrates_to_one`.
2. **Register a new setting — three places, all of them:** (a) the
   field on `Settings` plus its entry in the `Default` impl, (b) its
   clamp/repair in `validate()` with named bound constants, (c) the
   game's settings UI. A setting that exists in fewer than three
   places is a bug in progress.
3. **Wire the real session.** `shell-app`'s scripted walk is a stand-
   in: a real game drives `Shell` from the `torogao-session` host,
   feeding real input events and acting on the returned
   `ShellAction`s (starting/resuming the session on `StartGame` /
   `ResumeGame`, and so on). The state machine does not change; only
   the driver does.
4. **Audio.** The volume fields are consumed by the engine's audio
   layer; the shell only stores and clamps them. Estate rule (see
   `AGENTS.md`): audio is generated and verified as files only, never
   played aloud on primo.
5. **Content/data layout.** A real game keeps its data as RON "packs"
   beside the crate (for example `packs/` at the game root): content
   records in the canonical RON form, loaded through the same
   validate-with-control-flow discipline as saves. New content kinds
   get a versioned format marker of their own, following the settings
   / save pattern.

## 3. What not to add

- No clock, I/O, or environment reads inside `shell.rs`. Purity is
  the testability contract.
- No unversioned save writes, ever. Every save is an envelope with a
  marker and a `format_version`.
- No settings field without a default *and* a `validate()` clamp.
- No `unwrap`/`expect` on data paths (files, parsing, user input).
  Assertions are for programmer errors, and in tests only.
- No `unsafe` — the crate is `#![forbid(unsafe_code)]`.
- No new external dependencies without a stated reason recorded in
  the change. The budget (serde + ron) is a bound.
- No second serialization format for state. RON is canonical.

## 4. Gates

A change here is done when all of these ran, with real numbers
reported:

```sh
cargo build
cargo test
cargo run -p shell-app        # expect: OK shell-app
cargo clippy -- -D warnings
cargo fmt --check
```

Plus the estate IDE gate: every touched file validated through the
theodolite bridge with the language server for its type — bridge ids
`bacon-ls` (diagnostics owner) + `rust-analyzer` (semantics) for
`.rs`, `ron-lsp` for `.ron`, `taplo` for `.toml` — zero error
diagnostics before a file is called done. A file type with no bridge
route is named as not-LSP-validated, never silently skipped.
Formatter: rustfmt.

### Test split (`crates/shell-core/tests/roundtrip.rs`)

Validation (expected outcome: the documented happy path):

| Test | Proves |
| --- | --- |
| `settings_round_trip` | Save then load returns the same settings, through a nested path (parent creation works). |
| `settings_defaults_on_missing_file` | Missing file → exact defaults, no error. |
| `save_load_round_trip` | Payload, label, times, and version survive a round trip; `slots()` lists the slot. |
| `save_metadata_read` | Metadata returns label/times without the payload; empty slots read `None`. |
| `shell_flow_transitions` | The full transition table: boot with gate off/on, decline quits, confirm titles, pause only from game, settings caller tracked as Title vs Pause, continue refused until available. |

Adversarial (expected outcome: the failure is contained):

| Test | Proves |
| --- | --- |
| `corrupt_settings_quarantined` | Garbage bytes → defaults, `<name>.corrupt` exists, original path vacated. |
| `wrong_format_settings_quarantined` | A valid-RON file with the wrong marker → defaults + quarantine. |
| `nan_volume_repaired_by_validate` | NaN and +∞ volumes repair to default; a finite 0.25 survives; an out-of-range width clamps. |
| `nan_volume_in_handwritten_ron_repaired` | A hand-written RON file with `master_volume: NaN` loads as the default 1.0, never NaN. |
| `corrupt_save_quarantined_other_slot_intact` | Corrupt slot1 → `Ok(None)` + quarantine; slot2 loads untouched. |
| `unsupported_version_errors_and_file_untouched` | A hand-written `format_version: 99` envelope → `Err(UnsupportedVersion { found: 99, current: 1 })`; the file is *not* quarantined. |
| `version_zero_migrates_to_one` | A hand-written v0 envelope with no `label` field loads at version 1 with an empty label; `migrate()` exercised directly. |
| `invalid_slot_rejected_and_never_escapes` | `"../evil"` → `Err(InvalidSlot)` on save and load, no file written outside or inside the saves dir; `__probe` test slot works and is never listed. |

## 5. Neovim workflow

- Diver's Rust wiring is the editor surface: rust-analyzer for
  semantics, bacon-ls as the diagnostics owner (the same pair the
  theodolite bridge routes). Debugging uses lldb-dap / codelldb per
  Diver's Rust config.
- The one terminal command you need open beside the editor:
  `cargo test` while iterating, `cargo run -p shell-app` as the
  final smoke.
- Gotchas:
  - Edition and rust-version are pinned by the workspace manifest;
    do not add per-crate edition overrides.
  - RON canonical formatting in real Torogao records is one field
    per line via `ron::ser::PrettyConfig`. The template's writers
    use compact `ron::ser::to_string` for machine-managed files
    (settings/saves are rewritten whole, never hand-diffed); adopt
    the PrettyConfig pattern for any RON file a human is expected to
    read or edit.
  - `target/` is generated. So is `book-out/`. Neither is committed;
    both are cleaned at task end.
