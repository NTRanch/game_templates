# SPEC — Godot Game Shell

Target: Godot 4.7.2, GDScript, GL Compatibility renderer.
Credit: Nice Thoughts Ranch. License: Apache-2.0.

The shared shell contract (identical across all six engine
templates): canonical settings with defaults + clamps, a versioned
JSON save envelope with atomic writes and corrupt-file quarantine,
title/pause/settings menu flow with settings returning to its
caller, and an age gate that is present, documented, and off by
default. Save and settings data is untrusted input, validated by
control flow — never by `assert` alone, because asserts are
stripped in release builds.

## 1. File manifest

Each entry: purpose / contract (inputs, outputs, invariants) /
bounds (what it must never do).

- `project.godot` — Project definition. / Declares the app name
  "Game Shell", main scene `scenes/main.tscn`, autoload order
  Settings → SaveSystem → Shell, 1280x720 window, the default bus
  layout, and the `gl_compatibility` renderer. Invariant: autoload
  order never changes (Shell and screens assume Settings and
  SaveSystem exist). / Never hand-add engine settings that a gate
  has not imported clean.
- `default_bus_layout.tres` — Audio buses. / Bus 0 Master, bus 1
  Music → Master, bus 2 SFX → Master; these names are the contract
  `Settings.apply_audio()` looks up. / Never rename a bus without
  updating `settings.gd` in the same change.
- `icon.svg` — Project icon: an original spiral motif on a dark
  ground. / Decorative only. / Never replace it with a copied or
  third-party asset in the template itself.
- `autoloads/settings.gd` — Settings singleton (autoload
  `Settings`). / Canonical settings: `master_volume`,
  `music_volume`, `sfx_volume` (float, clamp 0..1, default 1.0);
  `fullscreen` (bool, false); `window_width` (int, clamp
  640..7680, 1280); `window_height` (int, clamp 360..4320, 720);
  `vsync` (bool, true); `reduce_flashing` (bool, false);
  `screen_shake` (float, clamp 0..1, 1.0); `subtitles` (bool,
  true). Stored at `user://settings.cfg` (ConfigFile, sections in
  fixed order meta/audio/display/gameplay for byte-stable output).
  Missing file → defaults + save. Parse error → file renamed to
  `settings.cfg.corrupt`, warning logged, defaults used. Per-key
  reads check `typeof` and clamp. `set_value(key, value)` validates,
  emits `settings_changed`, saves, applies. `apply_display()` is a
  documented no-op under the headless driver. / Never crash on bad
  settings data; never add a setting here without a default, a
  clamp or type check, and a settings-screen control.
- `autoloads/save_system.gd` — Save singleton (autoload
  `SaveSystem`). / Envelope `{format_version: 1, saved_at_unix:
  int, playtime_seconds: int, label: String, payload: Dictionary}`
  as JSON at `user://saves/<slot>.json`. API: `save_game(slot,
  payload, label, playtime_seconds) -> Error` (atomic: write
  `.tmp`, rename over target; emits `save_completed`),
  `load_game(slot) -> Dictionary` (`{}` when empty, corrupt, or
  refused; emits `load_completed`), `has_save`, `has_any_save`,
  `slot_metadata`, `delete_save`, `list_slots`. Corrupt JSON →
  quarantined to `.json.corrupt`, that slot reads empty, others
  untouched. `format_version > 1` → refused (reads empty, logged,
  file left in place). `format_version < 1` → `_migrate_envelope`
  match chain (v0→v1 fills a missing `label`). Additive optional
  payload fields with defaults are preferred over version bumps.
  Slot names are validated as single path components. / Never
  write a non-envelope file; never load a path built from
  unvalidated data; never delete another slot's file.
- `autoloads/shell.gd` — Flow-state singleton (autoload `Shell`). /
  State + signals only: `Screen` enum (AGE_GATE, TITLE, GAME),
  `AGE_GATE_ENABLED := false`, `age_confirmed`,
  `settings_return_to` (`"title"` / `"pause"`), `current_slot`,
  `session_playtime_seconds` (accumulated in `_process`, bounded
  by `MAX_SESSION_PLAYTIME_SECONDS`), `save_payload_provider`
  Callable, signals `screen_changed`, `pause_requested`,
  `payload_loaded`. `set_paused(v)` sets `get_tree().paused`. /
  Never switch scenes, never hold game state beyond the payload
  hand-off.
- `scenes/main.tscn` + `scripts/main.gd` — Root scene (Main). /
  Children: `ScreenRoot` (Control, full rect), `PauseLayer`
  (CanvasLayer, layer 10) holding the pause instance,
  `SettingsLayer` (CanvasLayer, layer 20) for settings so it
  overlays both title and pause. `_ready` shows the age gate when
  enabled and unconfirmed, else the title; connects
  `Shell.screen_changed` and `Shell.pause_requested`. Functions:
  `show_title()`, `show_game()`, `show_age_gate()`,
  `open_settings(from)`, `close_settings()` (returns per
  `Shell.settings_return_to`), `toggle_pause()`. Reachable by
  screens through the `shell_main` group. / Never let two
  full-screen scenes coexist in ScreenRoot.
- `scenes/title_screen.tscn` + `scripts/title_screen.gd` — Title. /
  Title label ("GAME TITLE" placeholder), Continue (disabled
  unless `SaveSystem.has_any_save()`; loads the most recent slot
  by `saved_at_unix`), New Game, Load (toggles the SlotList in
  load mode; a chosen slot loads into the game), Settings
  (`open_settings("title")`), Quit. / Never write saves.
- `scenes/settings_screen.tscn` + `scripts/settings_screen.gd` —
  Settings. / One control per canonical setting: HSliders +
  value labels (master/music/sfx/screen_shake), CheckBoxes
  (fullscreen/vsync/reduce_flashing/subtitles), SpinBoxes
  (window width/height within the clamp bounds), Back,
  Reset Defaults. No Apply button: changes write through
  `Settings.set_value` immediately. Back emits `settings_closed`;
  Main owns the return navigation. / Never keep a local copy of
  settings state that can diverge from the Settings singleton.
- `scenes/pause_menu.tscn` + `scripts/pause_menu.gd` — Pause
  overlay. / Root `process_mode = 3` (PROCESS_MODE_ALWAYS, set in
  the scene) so it runs while the tree is paused; starts hidden.
  `open()` shows it and pauses via Shell, `close()` hides and
  unpauses; the game scene is never touched. Buttons: Resume,
  Save, Load (SlotList in the matching mode), Settings
  (`open_settings("pause")`; the overlay stays open underneath),
  Quit to Title. Save collects the payload from
  `Shell.save_payload_provider`; load delivers it through
  `Shell.restore_payload` → `payload_loaded`. / Never mutate
  game state directly; never unpause on Settings open.
- `scenes/age_gate.tscn` + `scripts/age_gate.gd` — Age gate. /
  Separate scene with 18+ text, Confirm ("I am 18 or older -
  Enter") and Decline ("Exit") buttons, signals `confirmed` /
  `declined`. Main shows it first only when
  `Shell.AGE_GATE_ENABLED` is true; confirm sets
  `Shell.age_confirmed` and goes to Title, decline quits. Off by
  default. / Never gate content when the flag is false; never
  store an age or birthdate — the confirmation is a session bool.
- `scenes/game_stub.tscn` + `scripts/game_stub.gd` — Placeholder
  game. / "Your game goes here." label plus a counter label that
  increments once per second in pausable `_process` (proves pause
  leaves state untouched). `ui_cancel` emits
  `Shell.pause_requested`. Registers
  `Shell.save_payload_provider` in `_ready` (payload
  `{"stub_counter": n}`) and restores from
  `Shell.payload_loaded`; unregisters on exit. / Never keep the
  stub in a shipped game; it exists to demonstrate the contracts.
- `components/slot_list.tscn` + `scripts/slot_list.gd` — Slot
  picker component. / A VBoxContainer building one Button row per
  canonical slot (`slot1`, `slot2`, `slot3`, `auto`) from
  `SaveSystem.slot_metadata`: `"<slot> - <label> - <date>"`, or
  `"<slot> - Empty"`; save mode appends "(Overwrite)" to
  non-empty rows. `set_mode("load" | "save")`, `refresh()`,
  signal `slot_chosen(slot)`. / Never perform the save or load
  itself; the caller owns the operation.
- `tests/smoke.tscn` + `tests/smoke.gd` — Headless smoke scene. /
  Runs the case list in §4, prints `PASS <name>` / `FAIL <name>:
  <why>`, then `OK smoke` + `quit(0)` on success or `quit(1)`.
  Cleans up its `__smoke__` slot, corrupt artifacts, and any
  `slot1` it created, and restores default settings. Every path
  reaches quit. / Never depend on a display, audio device, or
  pre-existing user saves.
- `README.md` — Orientation: layout, run commands, verification
  status. / Kept short. / Never claim a verification that has not
  run.
- `SKILLS.md` — Tool and skill map for this template, plus the
  Bloch lane law. / Points at skills and bridge ids, not copies.
- `AGENTS.md` — Agent rules for projects started from this
  template. / Condensed refusals and gate commands.
- `docs/bloch-lesson-template.md` — Lesson record template with
  the birth-metadata fields. / A lesson without `origin_lane`,
  `content_label`, and `evidence` does not exist.
- `book.toml` + `book/src/` — mdBook wiring: SUMMARY plus four
  include chapters (README, SPEC, SKILLS, AGENTS). / The includes
  are the single source; never fork the text inside `book/src/`.

## 2. What to add

Ordered checklist for growing a real game from this shell:

1. Replace the game stub: put the real game scene where
   `game_stub.tscn` sits (or repoint `GameScene` in `main.gd`),
   keep the contracts — register a payload provider with Shell,
   restore from `payload_loaded`, request pause via
   `pause_requested`.
2. Register each new setting in THREE places: the model in
   `autoloads/settings.gd` (typed var, default, bound constants,
   ConfigFile read + write in the fixed section order, a
   `set_value` arm), the ConfigFile section layout, and a control
   in `scenes/settings_screen.tscn` + its wiring in
   `scripts/settings_screen.gd`.
3. Extend save payloads additively: new payload fields are
   optional, read with defaults (`payload.get("field", default)`
   plus a `typeof` check). Bump `FORMAT_VERSION` only when the
   envelope shape itself changes, and then add one match arm per
   version step in `_migrate_envelope` — never edit a shipped
   step.
4. Add screens through Main's flow: preload the scene in
   `main.gd`, mount it in `ScreenRoot`, add a `Shell.Screen`
   value only if it is a top-level screen (overlays are layers,
   not screens).
5. Add audio buses in `default_bus_layout.tres` (send to Master)
   and apply their volumes from `settings.gd` if they are
   user-facing.
6. Add export presets (`export_presets.cfg`) when the game is
   ready to ship; exports need the matching export templates
   installed, which the template deliberately does not assume.

## 3. What not to add

- No new autoloads for game state — only Settings, SaveSystem,
  and Shell are sanctioned singletons; extra global state hides
  ownership and ordering bugs.
- No unversioned saves — an envelope without `format_version`
  cannot migrate, so it can only break later.
- No settings without a default + clamp — an unbounded value from
  untrusted storage is a crash or a corrupted display mode
  waiting for its input.
- No assert-only validation — asserts are stripped in release
  builds, so the check disappears exactly where it matters.
- No per-frame `load()` — it re-reads and re-parses resources on
  the frame path; preload at scene scope instead.
- No scene-tree access from threads — the tree is main-thread
  only; worker results return to the main thread before touching
  nodes.

## 4. Gates

Run from this directory. Every gate checks exit code, the success
sentinel where there is one, AND stderr for `SCRIPT ERROR` /
`ERROR:` lines:

```sh
# Import (also generates .godot/ and *.uid sidecars; never hand-edit those)
godot --headless --path . --import

# Per-script parse/type check — ONLY the three autoload scripts.
# On Godot 4.7.2, --check-only compiles a script WITHOUT the
# project's autoload registrations, so any script that references
# the Settings / SaveSystem / Shell singletons fails with
# "Identifier not found" even though the project itself is sound
# (proven at this template's delivery gate, 2026-11-09). The
# autoload scripts reference no singletons and check cleanly:
godot --headless --path . --check-only --script res://autoloads/settings.gd
godot --headless --path . --check-only --script res://autoloads/save_system.gd
godot --headless --path . --check-only --script res://autoloads/shell.gd

# Smoke: expect exit 0, "OK smoke" on stdout, no SCRIPT ERROR in
# stderr. The smoke is the parse + integration gate for every
# other script: it loads and instances all six scenes.
# One stderr line is EXPECTED during the run: the engine's JSON
# parser prints "ERROR: Parse JSON failed ..." while the
# corrupt-slot adversarial case feeds it garbage — that case
# passing is the verdict, not the line.
timeout 60 godot --headless --path . res://tests/smoke.tscn
```

Settings-corruption semantics, measured on 4.7.2: `ConfigFile` is
lenient — most garbage parses to zero sections (load returns OK),
and the per-key default repair in `settings.gd` is what restores
sanity; the `.corrupt` quarantine fires on a true parse error
(err 43, e.g. an unclosed section header), which is what the
smoke's `settings_corrupt_quarantine` case feeds it.

Theodolite route: bridge server id `gdscript` for `.gd` files —
zero error diagnostics before a file is done. `.tscn` / `.tres`
files have NO theodolite route: they are named here as
not-LSP-validated, covered instead by the smoke's
scene-instancing cases. Formatter: `gdformat` (godot-gdscript-
toolkit; not installed on the reference machine — format by hand
per the official GDScript style, tabs for indentation, until it
is).

Test split (the smoke's cases; half validation, half adversarial):

Validation — expected outcome in parentheses:

- `settings_defaults_sane` (every default present and in bounds).
- `settings_round_trip` (music_volume set to 0.5, saved, changed
  in memory, reloaded as 0.5).
- `save_load_round_trip` (envelope for `__smoke__` round-trips
  payload `{"k": 7}`, label, and playtime).
- `scenes_instantiate` (title, settings, pause, age gate, game
  stub, and slot list all load, instantiate, and enter/exit the
  tree without error).

Adversarial — expected outcome in parentheses:

- `settings_corrupt_quarantine` (garbage over `settings.cfg` →
  defaults in memory, `settings.cfg.corrupt` exists).
- `corrupt_slot_isolation` (garbage over the `__smoke__` slot →
  that slot loads `{}`, its `.corrupt` sibling exists, `slot1`
  metadata is unaffected).
- `version_skew_high_refused` (`format_version: 99` → loads `{}`,
  file left in place).
- `version_skew_low_migrates` (`format_version: 0` without a
  label → migrates to version 1 with `label == ""`).

## 5. Neovim workflow

- LSP: Godot's own language server via the running editor (TCP
  6005, Diver `lsp/gdscript_ls.lua`), plus the theodolite
  `gdscript` route for headless diagnostics when no editor is
  open.
- DAP: the editor's debug adapter on TCP 6006.
- One terminal command smokes the whole project:
  `timeout 60 godot --headless --path . res://tests/smoke.tscn`.
- Gotchas: `.godot/` and `*.uid` sidecars are generated — never
  hand-edit them; commit the sidecars after a first clean import.
  ConfigFile section order is byte-significant — `settings.gd`
  writes sections in the fixed order meta/audio/display/gameplay
  on purpose.
