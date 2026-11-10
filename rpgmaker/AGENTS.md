# AGENTS.md — RPG Maker MZ template

Working rules for agents in this directory. **Read `SPEC.md` first**;
facts come from `SPEC.md` and `SKILLS.md`, not from memory of other
engines' templates. The repository root `AGENTS.md` is repo-wide law;
this file carries the MZ specifics and wins for this directory's
files where the two differ on engine detail.

## Gates

- `node --check` on every plugin you touch — the three commands are
  in `SPEC.md` §4. Clean means clean; report the actual result.
- **Theodolite:** every touched file is validated through the
  theodolite bridge with server id **`typescript-language-server`**
  (it covers JavaScript) — zero error diagnostics before the file is
  called done. A file with no bridge route is named in your report
  as not-LSP-validated, never silently skipped.
- **Formatter:** prettier (Diver wires prettierd / prettier for
  JavaScript); MZ-style 4-space indent.
- In-editor behaviour is gated by the first-load checklist in
  `SPEC.md` §4. Do not claim an in-editor result you did not run.

## Refusals (condensed — full text in `SPEC.md` §3)

- **Alias, never replace**, a core method. Original preserved and
  called, every time.
- The estate save block is **metadata only**. Never store
  game-critical state only there.
- **No unversioned shape change** to the estate block: version bump
  plus one migration step, together, or the change does not land.
- No settings key without a default + validation in `DEFAULTS` /
  `SPEC` (the settings plugin's tables).
- No direct edits to `config.rpgsave`; it is engine-owned.
- No new scenes for features MZ already provides (title, options,
  save, load stay native); don't fight `Scene_Options` — adopt the
  estate volume model or don't.

## Where things live

- `ConfigManager` is **global**: one config for the whole game,
  persisted by the engine to `config.rpgsave` in file mode. Estate
  keys persist by riding its `makeData` / `applyData`.
- The `$game*` objects are **save-scoped**: MZ saves and loads game
  state natively. Game progress never belongs in the estate layer.
- The **estate block lives inside a save's contents**
  (`contents.estate`, written by the `DataManager.makeSaveContents`
  alias in `NTR_EstateSave.js`) and is read back through
  `DataManager.estateInfo()` after a load.
- Estate settings live in `window.EstateSettings`
  (`NTR_EstateSettings.js`); the age gate's confirmation flag lives
  in `localStorage` under `estate_age_confirmed`
  (`NTR_EstateAgeGate.js`).
- `js/plugins.js` in an adopting project is editor-generated; the
  Plugin Manager is the registration route, in the order
  Settings → Save → AgeGate (plugin order is load order).
