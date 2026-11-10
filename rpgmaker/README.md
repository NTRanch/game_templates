# RPG Maker MZ Game Shell

A public game-shell template for **RPG Maker MZ** (JavaScript plugins),
part of `NTRanch/game_templates`. It supplies the shared six-engine shell
contract in MZ idioms — and, just as deliberately, it does **not**
reimplement what MZ already provides.

- **License:** Apache-2.0
- **Credit:** Nice Thoughts Ranch
- **Language:** English only
- **Dependencies:** none beyond RPG Maker MZ core scripts

## What MZ provides vs. what this template adds

MZ natively provides the title screen, the options window
(`Scene_Options`), the save/load scenes, and `ConfigManager`
persistence (`config.rpgsave` in file mode). This template does not
reimplement any scene. It adds exactly three plugins:

| Shell feature | MZ native | Estate plugin (this template) |
|---|---|---|
| Title screen | `Scene_Title` | — (untouched) |
| Options screen | `Scene_Options` + `Window_Options` | Settings **model** only; no options UI ships |
| Settings persistence | `ConfigManager` + `config.rpgsave` | Estate keys ride `ConfigManager.makeData` / `applyData` |
| Accessibility / comfort keys | — | `reduce_flashing`, `screen_shake`, `subtitles` in the estate model |
| Volumes | `bgmVolume` / `bgsVolume` / `meVolume` / `seVolume` on `ConfigManager` | Estate `master` / `music` / `sfx` channels **bridged** onto the native four (mapping below) |
| Display size | `Graphics.width` / `Graphics.height` | `window_width` / `window_height` model fields, applied at plugin load (effect at next launch) |
| Vsync | requestAnimationFrame (vsync-bound by NW.js / browser) | `vsync` field stored for cross-engine parity; **engine no-op in MZ** |
| Fullscreen | No public MZ core API | Plugin command via the guarded NW.js window route (desktop only) |
| Save / load scenes | `Scene_Save` / `Scene_Load`, `DataManager`, `StorageManager` | Save **envelope**: a versioned `estate` metadata block inside save contents |
| Savefile list label | Native savefile info | `estateLabel` + `estateFormatVersion` added to savefile info for the game to draw |
| Age gate | — | Optional third plugin, **disabled by default** |

### Volume bridge (the chosen mapping)

Estate volumes are the source of truth in the estate model. On apply,
they are written into `ConfigManager`'s native keys, with master
scaling multiplicatively:

```text
bgmVolume = round(music_volume * master_volume / 100)
bgsVolume = round(music_volume * master_volume / 100)
meVolume  = round(sfx_volume   * master_volume / 100)
seVolume  = round(sfx_volume   * master_volume / 100)
```

Consequence, stated honestly: once a game adopts the estate model, the
four native volume rows in `Scene_Options` fight the model (native
edits are overwritten by the next estate apply). `SPEC.md` documents,
as text guidance only, how to hide those rows. This template ships no
`Scene_Options` code.

## Files

```text
js/plugins/NTR_EstateSettings.js  Settings model + volume bridge + plugin commands
js/plugins/NTR_EstateSave.js      Versioned estate block inside save contents
js/plugins/NTR_EstateAgeGate.js   Optional age-confirmation gate (off by default)
SPEC.md                           Full contract: manifest, add / do-not-add, gates
SKILLS.md                         Skills, tools, and services this template uses
AGENTS.md                         Working rules for agents in this directory
docs/bloch-lesson-template.md     Lesson-record template (birth-metadata law)
book.toml, book/src/              mdBook shell (shared theme: ../theme-torogao)
```

## Install

1. Create or open an RPG Maker MZ project.
2. Copy the three plugin files into the project's `js/plugins/`
   directory:
   - `NTR_EstateSettings.js`
   - `NTR_EstateSave.js`
   - `NTR_EstateAgeGate.js`
3. In the editor, open **Tools → Plugin Manager** and add the three
   plugins in this exact order (Plugin Manager order is load order):
   1. `NTR_EstateSettings`
   2. `NTR_EstateSave`
   3. `NTR_EstateAgeGate`
4. Leave the age gate disabled (`AgeGateEnabled = false`) unless the
   project needs it; see `SPEC.md` for flipping it on.
5. Playtest from the editor. Settings persist through the engine's own
   config file; saves carry the estate block automatically.

The plugins are plain files: a freshly generated MZ project's
`js/plugins.js` registry is editor-generated, so adding the files and
enabling them in the Plugin Manager is the whole registration step.

## Using it

- Read estate values from game code or event script calls via
  `window.EstateSettings.state` (for example
  `window.EstateSettings.state.subtitles`), or
  `window.EstateSettings.get("music_volume")`.
- Change volumes from an event with the **Set Volume** plugin command;
  toggle desktop fullscreen with **Set Fullscreen**; open the native
  options scene with **Open Options**.
- Read the loaded save's metadata via `DataManager.estateInfo()`
  (a validated block, or `null` — always null-check).

## Verification status

- `node --check` was run at delivery on all three plugins and was
  **clean on all three**.
- Theodolite diagnostics via `typescript-language-server` at
  delivery: **zero errors** on all three plugins. The age-gate
  plugin carries two Hint-level suggestions (constructor function
  may be a class) — kept deliberately: MZ plugins are constructor
  functions by convention.
- The plugins were **NOT loaded in a live MZ editor for this
  delivery.** The in-editor validation and adversarial cases in
  `SPEC.md` ("Gates") are the first-load checklist for the first
  project that adopts this template — run them before shipping.
