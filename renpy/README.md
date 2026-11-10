# Ren'Py Game Shell — Nice Thoughts Ranch

A drop-in shell template for Ren'Py 8.x visual novel projects: the
estate settings model, save-data versioning discipline, and an optional
18+ age gate, layered on top of what Ren'Py already provides.

Licensed under the Apache License, Version 2.0.
Copyright 2026 Nice Thoughts Ranch.

## Verification status: STATIC

This delivery was **syntax-reviewed only**. It was not run under a
Ren'Py SDK, not linted by the SDK, and not launched. Treat every file as
unexecuted until you run the gates in `SPEC.md` on a machine with the
Ren'Py 8 SDK installed (the headline gate is
`renpy.sh <project> lint`). Nothing in this README claims otherwise.

One real check WAS run at delivery: theodolite diagnostics via the
`renpy-language-server` bridge over all five `.rpy` files — zero
diagnostics. That is an LSP analysis result, not an SDK run.

## The honest mapping

Ren'Py is a complete engine. Most of a generic "game shell" contract is
already native to it, and this template deliberately does **not**
reimplement any of it.

| Shell feature | Ren'Py native | Estate-added |
| --- | --- | --- |
| Main menu / title screen | Yes — engine `main_menu` screen | Nothing |
| Pause | Yes — the game menu (Esc / right-click) is the pause equivalent | Nothing |
| Save / load file slots | Yes — engine `save` / `load` screens | A version stamp + migration hook (see below) |
| Volumes (master / music / sfx) | Yes — mixer preferences | Read-only mirror dict for games that want one source of truth |
| Fullscreen / window size | Yes — preferences | Nothing (documented, not duplicated) |
| Text speed | Yes — preferences | Nothing |
| Accessibility / estate settings (`reduce_flashing`, `screen_shake`, subtitles default) | No | Settings model with defaults + validation, stored in `persistent` |
| Save-data versioning | No | `estate_save_version` saved per save + `label after_load` migrations; `persistent.estate_persistent_version` + init-time migration |
| 18+ age gate | No | `screen estate_age_gate` + `label estate_age_gate_flow`, off by default |

What the estate layer adds, in one line each:

- **`game/estate_options.rpy`** — build identity placeholders and
  estate constants (`ESTATE_FORMAT_VERSION`, `estate_age_gate_enabled`).
- **`game/estate_settings.rpy`** — the validated settings model.
  Estate-owned keys only: `reduce_flashing` (bool, default `False`),
  `screen_shake` (float, default `1.0`, clamped 0.0–1.0), `subtitles`
  (bool, default `True`). Stored in `persistent.estate_settings`.
  Corrupt or wrong-typed values are repaired to defaults and logged —
  `persistent` is engine-managed storage, so reset-the-bad-key + log is
  the honest engine equivalent of quarantining bad data.
- **`game/estate_saves.rpy`** — versioning discipline. Ren'Py owns the
  save format; the estate versions only its own data: a store variable
  that is itself saved (`default estate_save_version = 1`) plus
  `label after_load` for migrations, and a version stamp for persistent
  data migrated at init.
- **`game/estate_screens.rpy`** — the age gate screen and the estate
  settings panel. The panel deliberately uses a cycling button for
  screen shake instead of a custom `BarValue`; a real game can swap in
  a proper bar.
- **`game/script.rpy`** — a small demo of the flow contract, including
  `label splashscreen` hooking the age gate before the main menu.

## Flow contract

- **Title / Continue / New Game / Settings / Quit** map onto Ren'Py's
  built-in main menu, preferences, and file screens. New Game and game
  start both enter at `label start`.
- **Pause** is the game menu. There is no pause screen to write.
- **Age gate** (only when enabled): `label splashscreen` calls the gate
  flow before the main menu. Confirm sets
  `persistent.estate_age_confirmed` and the menu is reached; decline
  quits the game. With the gate disabled (the default), the splash
  label returns immediately.
- **Settings changes are persistent-level, not save-level**: they apply
  across the whole game, survive relaunch, and are not rolled back with
  the story. That is what makes `estate_set` safe during gameplay.

## Drop-in instructions

1. Create a new project with the Ren'Py 8 SDK, or open an existing
   Ren'Py 8 project.
2. Copy `game/*.rpy` from this template into the project's `game/`
   directory. If the project already has the SDK-generated
   `options.rpy` / `screens.rpy` / `gui.rpy`, keep them — the estate
   files use distinct names (`estate_*.rpy`) and redefine no engine
   screen. Replace the generated `script.rpy` demo with this template's
   `script.rpy`, or merge `label splashscreen` into yours.
3. Edit the placeholders in `game/estate_options.rpy`: `config.name`,
   `config.version`, and (before shipping) an explicit
   `config.save_directory`.
4. To enable the age gate, set `estate_age_gate_enabled = True` in
   `game/estate_options.rpy` (the constant is marked there).
5. Run the gates from `SPEC.md` (SDK lint + compile check) before
   treating the project as working.

## Where to go next

- `SPEC.md` — file manifest, what to add, what not to add, gates,
  Neovim workflow.
- `SKILLS.md` — the tools and skills this template assumes, plus the
  Bloch lane law for sharing lessons.
- `AGENTS.md` — short instructions for agents working in this repo.
