# AGENTS — Godot Game Shell

Instructions for agents working in a project started from this
template. The repo-wide `AGENTS.md` also applies; this file is the
engine-specific layer and wins for Godot files.

- Read `SPEC.md` first. Facts about this template come from
  `SPEC.md` and `SKILLS.md`, not from memory or from other
  templates.
- Run the gates before claiming done (exact commands in
  `SPEC.md` §4): import
  (`godot --headless --path . --import`), `--check-only` for the
  three autoload scripts only (on 4.7.2 it cannot resolve
  autoload singletons in consuming scripts — `SPEC.md` §4 has
  the proven detail), and the smoke
  (`timeout 60 godot --headless --path . res://tests/smoke.tscn`,
  expecting exit 0 + `OK smoke` + no `SCRIPT ERROR` in stderr).
  Check exit code, sentinel, and stderr — all three.
- Theodolite: bridge server id `gdscript` for `.gd` files, zero
  error diagnostics before a file is done. `.tscn` / `.tres`
  files have no theodolite route; name them not-LSP-validated in
  reports, never silently skipped.
- Formatter: `gdformat` where installed; otherwise the official
  GDScript style by hand (tabs for indentation).

## Top refusals

- No extra autoloads. Only Settings, SaveSystem, and Shell are
  sanctioned singletons.
- No unversioned save writes. Every save is the envelope from
  `SPEC.md` with `format_version`, written atomically, with
  corrupt files quarantined — do not break the envelope rules.
- No settings without a default + clamp (and a `set_value` arm
  and a settings-screen control: three places, see SPEC.md §2).
- No assert-only validation. Save/settings data is untrusted
  input; validate with control flow that survives release builds.

## Data locations

- Settings: `user://settings.cfg` (corrupt → `settings.cfg.corrupt`).
- Saves: `user://saves/<slot>.json` (corrupt → `<slot>.json.corrupt`).

## Generated files

Never edit `.godot/` contents or `*.uid` sidecars by hand; they
are generated at import. Credit lines, where needed, read
"Nice Thoughts Ranch". No personal names in artifacts. English
only.
