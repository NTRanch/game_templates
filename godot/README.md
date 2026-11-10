# Godot Game Shell

A runnable game shell for Godot 4.7: title / pause / settings menus,
a versioned JSON save system, and a settings store — the parts every
game rebuilds — with the game itself left as a clearly marked stub.

Credit: Nice Thoughts Ranch. License: Apache-2.0 (see the repo
LICENSE).

## Layout

- `project.godot` — Godot 4.7 project, GL Compatibility renderer,
  1280x720 window, three autoloads (Settings, SaveSystem, Shell).
- `autoloads/` — the only sanctioned singletons. `settings.gd`
  (canonical settings with defaults + clamps), `save_system.gd`
  (versioned save envelope, atomic writes, corrupt quarantine),
  `shell.gd` (flow state and signals).
- `scenes/` + `scripts/` — Main (screen switching), title screen,
  settings screen, pause menu, age gate (off by default), and the
  game stub that demonstrates the save/pause contracts.
- `components/` — the reusable slot list used by title and pause.
- `tests/` — a headless smoke scene (validation + adversarial).
- `default_bus_layout.tres` — Master / Music / SFX audio buses.
- `SPEC.md` — the full contract: file manifest, what to add, what
  not to add, and the gates. Read it before changing anything.
- `SKILLS.md`, `AGENTS.md`, `book.toml` + `book/` — tool map, agent
  rules, and the mdBook wiring for these docs.

## Run it

Open the folder in Godot 4.7 (or newer 4.x) and press Play, or:

```sh
godot --headless --path . --import
godot --headless --path . res://tests/smoke.tscn
```

The smoke run prints `PASS`/`FAIL` per case and `OK smoke` at the
end, and exits 0 only when every case passes.

Settings live at `user://settings.cfg`; saves live at
`user://saves/<slot>.json` (slots: `slot1`, `slot2`, `slot3`,
`auto`).

## Verification status

Verified on primo with Godot 4.7.2 headless: import + smoke
(9/9 cases) — see repo README verification table. Per-script
`--check-only` covers the three autoload scripts; on 4.7.2 it
cannot resolve autoload singletons in the scripts that consume
them, so the smoke is their gate (SPEC §4 has the detail).
