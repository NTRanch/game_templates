# game_templates

Game shell templates by **Nice Thoughts Ranch**: the main menu, pause,
settings, and save/load layer every new studio game starts from, in six
engine flavours that all implement one shared contract.

Licensed under the [Apache License 2.0](LICENSE).

## The shared contract

Every template in this repo provides the same shell, re-expressed in
its engine's own idioms (these are light-show's proven *patterns* —
settings discipline, save versioning, menu flow — never ported code):

- **Settings model.** Audio (master / music / sfx volumes), display
  (fullscreen, window size, vsync), and generic gameplay/accessibility
  toggles (reduce flashing, screen shake, subtitles), persisted in the
  engine's native store. Every setting has a default and a
  validation/clamp rule, and a corrupt settings file falls back to
  defaults — quarantined aside, never a crash.
- **Save discipline.** A versioned save envelope
  (`format_version` from day one), atomic writes (temp + rename or the
  engine equivalent), named slots (`slot1`, `slot2`, `slot3`, `auto`),
  corrupt-slot isolation (a bad slot never takes down the others or
  the game), and a migration stub showing where version steps live.
  Additive optional fields with defaults are preferred over version
  bumps; save data is untrusted input and is validated as such.
- **Menu flow.** Title → Continue (when a save exists) / New Game /
  Load / Settings / Quit. Pause overlays the game without destroying
  state. Settings is reachable from both title and pause and returns
  to whichever called it.
- **18+ gate pattern.** An includable first-screen age/consent gate
  for adult titles, present in every template, **off** in the default
  flow and documented where it is switched on.
- English-canonical text everywhere.

Each engine directory carries four documents beside the template
itself: `README.md` (short orientation + verification status),
`SPEC.md` (the exhaustive contract: file manifest, what to add,
what not to add, gates, Neovim workflow), `SKILLS.md` (the skills /
MCP / tool map for that engine, including the bloch lesson lane law),
and `AGENTS.md` (instructions for agents working in a project started
from the template). A `docs/bloch-lesson-template.md` in each
directory is the lesson-record stub with the mandatory birth metadata
(`origin_lane`, `content_label`, `evidence`).

## The templates

### [godot/](godot/) — Godot 4 (GDScript)

Autoload `Settings` + `SaveSystem` + `Shell` flow state, title /
settings / pause / age-gate / game-stub scenes, a reusable slot list,
ConfigFile settings, versioned JSON saves. **Real and runnable** —
the smoke scene exercises settings and save round-trips, corrupt-file
fallbacks, and version skew headlessly.

### [unity/](unity/) — Unity (C#)

A drop-in `Assets/GameShell` package: settings manager (JSON in
`persistentDataPath`), versioned save envelope with atomic writes and
a migration chain, menu / pause / settings / age-gate UI logic, and a
documented scene-wiring checklist. **Static / template-grade**: not
compiled in an editor for this delivery; see its SPEC for the
first-run in-editor smoke.

### [renpy/](renpy/) — Ren'Py 8

Drop-in `game/*.rpy` files. Ren'Py natively owns the main menu, game
menu, file slots, and most preferences, so this template adds what it
lacks: the estate settings model with validation in `persistent`,
save/persistent versioning with an `after_load` migration hook, and
the 18+ gate screen. Static-gated (SDK lint route in its SPEC).

### [unreal/](unreal/) — Unreal Engine 5.8

A `.uproject` + config + C++ shell: a settings subsystem on
`UGameInstanceSubsystem`, a versioned `USaveGame` envelope with a
save library, a flow-owning GameInstance, and UMG widget base classes.
Systems code is complete; maps, UMG layouts, and sound-class assets
are editor steps, listed exactly in its SPEC. Static-gated.

### [rpgmaker/](rpgmaker/) — RPG Maker MZ

Three JS plugins: estate settings model bridged onto `ConfigManager`,
a save-envelope plugin (versioned metadata block inside MZ save
contents, validated on load, never blocking the game's own data), and
an 18+ gate scene plugin. `node --check` clean; first-load checklist
in its SPEC.

### [torogao/](torogao/) — Torogao (Rust)

The estate engine's own starter: a `shell-core` crate (settings in
canonical RON, versioned save envelope, a pure screen-flow state
machine) plus a `shell-app` headless demo binary that walks the whole
flow in a terminal. Builds and tests with cargo.

## Verification status

Honesty rule: a template is only called verified for what was
actually run. Summary (details in each engine's SPEC):

| Template | Gate | Result |
| --- | --- | --- |
| godot | Godot 4.7.2 headless: import, `--check-only` (autoload scripts), smoke scene 9/9 | PASS — see godot/SPEC.md |
| unity | Static review against the estate C# style guide; theodolite (Roslyn) diagnostics | STATIC — not editor-compiled; zero error/warning diagnostics (style hints only) |
| renpy | Static review; theodolite (renpy-language-server) diagnostics | STATIC — not run under an SDK; zero LSP diagnostics on all five scripts |
| unreal | Static review; `.uproject` JSON validated | STATIC — not compiled; C++ has no theodolite route (named in SPEC, not skipped) |
| rpgmaker | `node --check` on all three plugins; theodolite (TypeScript LS) diagnostics | PASS (syntax + zero LSP errors) — not editor-loaded |
| torogao | `cargo build`, `cargo test` 13/13, clippy `-D warnings`, `cargo fmt --check`, `shell-app` smoke, theodolite diagnostics | PASS — zero LSP diagnostics; see torogao/SPEC.md |
| books | `mdbook build` for all six engine books | PASS — see below |

## The books

Every engine directory is an mdBook using the shared Torogao theme in
[theme-torogao/](theme-torogao/) (dark, spiral motif, original work).
Build any of them from the repo root:

```sh
mdbook build godot      # or: unity, renpy, unreal, rpgmaker, torogao
```

Output lands in `<engine>/book-out/` (git-ignored). Each book wires
the engine's README, SPEC, SKILLS, and AGENTS as chapters.

## Working in this repo

Agents: read [AGENTS.md](AGENTS.md) first — it carries the estate
operating rules for this repo, and each engine's own `AGENTS.md` /
`SPEC.md` is the deeper contract for that template.

No CI, by design: validation runs locally, from the terminal, with
the commands in each SPEC.
