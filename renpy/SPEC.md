# SPEC — Ren'Py Game Shell

The contract for this template. Read this before changing any file.
Companion files: `README.md` (orientation), `SKILLS.md` (tools and
lane law), `AGENTS.md` (agent instructions).

Target: Ren'Py 8.x. Language: English only. License: Apache-2.0.
Credit: Nice Thoughts Ranch.

Verification status of this delivery: **STATIC** — syntax-reviewed,
not run under a Ren'Py SDK. The gates below are how that changes.

## 1. FILE MANIFEST

| File | Purpose | Contract | Bounds |
| --- | --- | --- | --- |
| `game/estate_options.rpy` | Build identity + estate constants | `init -10 python` sets `config.name` / `config.version` placeholders, declares audio channels, defines `ESTATE_FORMAT_VERSION = 1` and `estate_age_gate_enabled = False` | No screens, no persistent writes, no save access |
| `game/estate_settings.rpy` | Estate settings model | Fixed key set with defaults + a validation spec; `estate_get` / `estate_set` / `estate_validate_settings` / `estate_apply_settings`; values stored validated in `persistent.estate_settings`; mixer volumes mirrored read-only | Fixed keys only; unknown keys rejected with a `renpy.log` warning; floats clamped to spec range; validation iterates the fixed spec only |
| `game/estate_saves.rpy` | Save-data versioning | `default estate_save_version = 1` travels inside each save; `label after_load` migrates older saves and refuses (log + notify, estate payload treated as absent) newer ones; `persistent.estate_persistent_version` migrated at init | Never touches the engine save format, file slots, or rollback; `after_load` must never raise |
| `game/estate_screens.rpy` | Estate-added screens | `screen estate_age_gate` (modal, Confirm / Exit) + `label estate_age_gate_flow`; `screen estate_settings_panel` for estate-owned keys via `Function(estate_set, ...)` actions | No redefinition of engine screens (`main_menu`, `save`, `load`, `preferences`, game menu); no custom `BarValue` in the template |
| `game/script.rpy` | Demo + flow wiring | `label splashscreen` hooks the gate; `label start` is the demo entry with a menu; `label estate_demo_pause_note` documents that the game menu is the pause | Demo only — replaced by the game's own opening |
| `README.md` | Orientation | Honest native-vs-estate mapping table, drop-in instructions, static verification status | Must keep the verification status honest |
| `SPEC.md` | This contract | Manifest, add / do-not-add rules, gates, editor workflow | — |
| `SKILLS.md` | Tool + skill table, Bloch lane law | Names the theodolite Ren'Py route, the SDK, and applicable skills | Only genuinely applicable entries |
| `AGENTS.md` | Agent instructions | Points at SPEC / SKILLS, states the gate and the envelope rules | Condensed; SPEC wins on conflict |
| `docs/bloch-lesson-template.md` | Lesson record template | Birth-metadata fields for shared lessons | — |
| `book.toml` + `book/src/*.md` | mdBook shell | Includes the root docs into a book; `create-missing = false`, theme `../theme-torogao` | Stubs only (\{{#include ...}}), no copied prose |

## 2. WHAT TO ADD

Ordered checklist for growing a game on this shell:

1. **Write content as labels.** Story content goes in the game's own
   `.rpy` files as labels reached from `label start`. Replace the demo
   `label start` in `game/script.rpy`; keep `label splashscreen` if
   the age gate is (or may become) enabled.
2. **Add an estate settings key — exactly three places:**
   1. `ESTATE_SETTING_DEFAULTS` in `game/estate_settings.rpy` (the
      default value),
   2. `ESTATE_SETTING_SPEC` in the same file (kind, and min / max for
      floats),
   3. `screen estate_settings_panel` in `game/estate_screens.rpy`
      (the control that changes it, via `Function(estate_set, ...)`).
   A key in fewer than three places is a defect.
3. **Add per-save estate data as saved store variables.** Use
   `default <name> = <value>` so new games get the default and loaded
   saves keep theirs. Rules:
   - Prefer additive changes: a new `default` variable is safe for old
     saves (Ren'Py fills in the default on load) and does **not** need
     a version bump.
   - When the *shape* of existing estate data changes (renamed field,
     changed meaning, changed type), bump `ESTATE_FORMAT_VERSION` in
     `game/estate_options.rpy` **and** add the matching ordered step in
     `estate_migrate_save` (per-save) and / or
     `estate_migrate_persistent` (persistent) in
     `game/estate_saves.rpy`, in the same change.
4. **Wire the age gate if the game needs it.** Set
   `estate_age_gate_enabled = True` in `game/estate_options.rpy`.
   Nothing else: `label splashscreen` already routes through
   `label estate_age_gate_flow`, and confirmation is persistent.
5. **Customise look and feel in `gui.rpy` and the standard screens.**
   Colours, fonts, menu layout, and the preferences screen layout are
   customised through the SDK-generated `gui.rpy` and `screens.rpy`
   of the game project — never by editing the estate files to restyle
   engine screens.
6. **Configure builds in the game's options.** Distribution definitions
   (the SDK's `build` configuration in the project's `options.rpy` or
   equivalent) decide what ships per platform. The estate files carry
   no build configuration beyond identity placeholders.

## 3. WHAT NOT TO ADD

- **Do not reimplement the main menu, game menu, or file slots.**
  Ren'Py owns them. Restyle via `gui.rpy`; do not redefine the engine
  screens inside estate files.
- **Do not store estate state in the wrong home.** Per-player machine
  state (settings, age confirmation) belongs in `persistent`. Per-story
  state belongs in saved store variables (`default ...`). Settings in a
  save, or story progress in `persistent`, are both defects.
- **No unversioned persistent shape changes.** If estate persistent
  data changes shape, `ESTATE_FORMAT_VERSION` bumps and a migration
  step lands in the same change. No exceptions.
- **No settings key without a default + validation.** Every key goes
  through the three places in §2.2. Code must never write
  `persistent.estate_settings` directly outside
  `game/estate_settings.rpy`.
- **Rollback discipline.** Ren'Py rollback restores store variables to
  earlier states; persistent data is *not* rolled back. Therefore:
  - `estate_set` during gameplay is safe precisely because settings are
    persistent-level — keep it that way; never make a settings change
    that story logic depends on being rolled back.
  - Do not perform rollback-unsafe mutations in `init` blocks: init
    runs once at startup, outside the rollback timeline, so mutating
    saved variables there corrupts the save/rollback contract. Init
    code in this shell only sets constants, ensures the settings dict,
    and migrates persistent data.
  - Migrations in `label after_load` run after the load; keep them
    total (never raise) and idempotent.

## 4. GATES

Run on a machine with the Ren'Py 8 SDK. This delivery ran none of
them (static verification only).

1. **SDK lint** — from the SDK directory:
   `renpy.sh <project> lint`
   (On Windows, `renpy.exe <project> lint`.) Zero errors required;
   warnings triaged, not silently accepted.
2. **Compile check** — launch the project from the SDK launcher (or
   `renpy.sh <project>`) and reach the main menu; a compile failure in
   any `.rpy` file blocks startup and is a hard fail.
3. **Theodolite (headless diagnostics)** — validate every touched
   `.rpy` file through theodolite with the bridge server id
   `renpy-language-server` (rockettap `renpy-language-server` v0.2.1,
   the community server adopted in the estate bridge). Zero error
   diagnostics before a file is called done.
   *Known caveat:* this is a community language server; a session may
   not report a quiescent / fully-settled state, so diagnostics can
   arrive flagged `possibly_incomplete`. Treat `possibly_incomplete`
   results as *unproven*, not as clean: re-probe once, and if the flag
   persists, record the file as "diagnostics possibly incomplete" in
   the work report and rely on the SDK lint gate as the decider. Never
   report a possibly-incomplete probe as a zero-error pass.
4. **Pure-Python helpers** — if a game adds real `.py` files, they
   route through theodolite's `jedi-language-server` instead, with the
   same zero-error rule. (Python inside `.rpy` blocks stays on the
   Ren'Py route.)
5. **Formatter** — none is catalogued for `.rpy`. Hold 4-space
   indentation and PEP8-ish shape inside Python blocks by hand.

### Test split (half validation / half adversarial)

Validation (expected behaviour):

- Fresh start reaches the main menu with the age gate disabled.
- With `estate_age_gate_enabled = True` and no prior confirmation, the
  gate appears before the main menu; Confirm reaches the menu; on
  relaunch the gate does not reappear.
- Change each estate setting in the panel, quit, relaunch: values
  persist.
- Save in the demo body, load it: the game resumes and
  `label after_load` runs without error.

Adversarial (hostile input / state):

- Hand-corrupt `persistent` values (set `screen_shake` to a string,
  `subtitles` to an int, add an unknown key): on launch, values are
  repaired to defaults / clamped, the unknown key is dropped, repairs
  are logged, and the game reaches the menu.
- Edit a save's `estate_save_version` to a future value
  (`ESTATE_FORMAT_VERSION + 1`) and load it: the refusal path logs,
  notifies "This save was created by a newer version of the game.",
  treats the estate payload as absent, and does not crash.
- Delete the project's persistent file entirely: defaults regenerate
  (settings defaults, gate unconfirmed) and the game reaches the menu.
- Decline the age gate (Exit): the game quits and no confirmation is
  recorded — relaunch shows the gate again.

## 5. NEOVIM WORKFLOW

- **Diagnostics surface:** theodolite with the `renpy-language-server`
  bridge route is the headless diagnostics surface for `.rpy` files
  (see gate 3 for the `possibly_incomplete` caveat). In-editor, Diver's
  Ren'Py wiring per the adopted stanza runs the same rockettap server
  (v0.2.1, pinned, community server) for live diagnostics.
- **DAP:** none catalogued for Ren'Py. Debugging is the SDK's own
  tools (developer console, lint, log.txt) — say so rather than
  inventing a debug route.
- **The one terminal command:** the SDK lint —
  `renpy.sh <project> lint`.
- **Gotchas:**
  - *Init order / offsets:* `estate_options.rpy` runs at offset `-10`
    and the settings bootstrap at `-5` so constants and the settings
    dict exist before default-priority init blocks and any screen use.
    New estate init work must respect those offsets.
  - *Persistent is per-project:* the persistent file lives in the
    project's save directory, which follows `config.save_directory`
    (or the derived name). Renaming the game or the save directory
    starts a fresh persistent file — settings and the age confirmation
    "reset" for the player. That is engine behaviour, not a bug.
  - *Rollback:* see §3. Settings are persistent-level and outside
    rollback by design; saved variables are inside it. Do not mix the
    two homes.
  - *Screens are not labels:* the settings panel is invoked with
    `call screen estate_settings_panel` because it ends in `Return()`;
    showing it with `show screen` from story flow leaves it on screen
    after the story moves on.
