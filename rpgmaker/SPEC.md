# SPEC — RPG Maker MZ Game Shell

This is the deep contract for the RPG Maker MZ template in
`NTRanch/game_templates`. Read it before changing anything in this
directory. License: Apache-2.0. Credit: Nice Thoughts Ranch.

Target: **RPG Maker MZ**, JavaScript plugins, vanilla JS (ES6 classes
and syntax are fine — MZ runs on NW.js / Chromium). No external
dependencies.

**Deliberate per-engine deviations from the shared six-engine shell
contract** (recorded here, per repo law):

- MZ does not reimplement scenes. The title screen, `Scene_Options`,
  and the save/load scenes are engine-provided; the estate layer is a
  settings **model** plus a save **envelope**, not a shell UI.
- Save atomicity and corrupt-file quarantine are engine-owned
  (`StorageManager`). The estate layer's corruption duty is narrower
  and is met in full: corrupt-isolation of the `estate` block, so a
  bad block degrades to defaults and never blocks the game's contents.
- Settings validation is explicit control flow (clamp / type repair in
  plain code), never `assert` — consistent with the shared contract.

---

## 1. File manifest

| File | Purpose | Contract | Bounds |
|---|---|---|---|
| `js/plugins/NTR_EstateSettings.js` | Estate settings model | Owns estate keys with defaults + validation; persists through `ConfigManager.makeData` / `applyData`; bridges estate volumes onto the four native volumes; exposes `window.EstateSettings`; provides `SetVolume`, `SetFullscreen`, `OpenOptions` plugin commands | Aliases listed below only; no scene code; no `Scene_Options` code |
| `js/plugins/NTR_EstateSave.js` | Save envelope | Writes / validates the versioned `estate` metadata block in save contents; corrupt-isolation; migration chain; `DataManager.estateInfo()`; savefile-info label fields | Aliases listed below only; no window code; estate block is metadata only |
| `js/plugins/NTR_EstateAgeGate.js` | Optional age gate | When enabled by parameter and unconfirmed, routes boot → gate scene → title; confirmation flag in `localStorage` | One alias (`Scene_Boot.prototype.start`); disabled by default |
| `README.md` | Orientation + honest mapping table | States what MZ provides vs. what the plugins add; install steps; verification status | No claims beyond the gates actually run |
| `SPEC.md` | This contract | Manifest, add / do-not-add, gates, Neovim workflow | — |
| `SKILLS.md` | Tool map | Skills / MCP / apps / tools this template uses; Bloch lane law | — |
| `AGENTS.md` | Agent working rules for this directory | Read SPEC first; gates; refusals condensed | Repo `AGENTS.md` wins on conflict, except engine specifics here |
| `docs/bloch-lesson-template.md` | Lesson record template | Birth metadata: `origin_lane` + `content_label` + `evidence` | No record without all three |
| `book.toml` | mdBook config | Title "RPG Maker MZ Game Shell"; `build-dir = "book-out"`; `create-missing = false`; theme `../theme-torogao`; src `book/src`; authors `["Nice Thoughts Ranch"]`; language `en` | Book output (`book-out/`) is a build artifact, never committed |
| `book/src/SUMMARY.md` | Book table of contents | Links the four chapter stubs | — |
| `book/src/readme.md` | Book stub | Includes `../../README.md` | Include only; no copied prose |
| `book/src/spec.md` | Book stub | Includes `../../SPEC.md` | Include only |
| `book/src/skills.md` | Book stub | Includes `../../SKILLS.md` | Include only |
| `book/src/agents.md` | Book stub | Includes `../../AGENTS.md` | Include only |

### Aliasing bound (applies to every plugin)

A plugin **aliases, never replaces**, core methods. The original is
captured and called:

```js
const _ConfigManager_makeData = ConfigManager.makeData;
ConfigManager.makeData = function () {
    const config = _ConfigManager_makeData.call(this);
    // ... estate additions ...
    return config;
};
```

Complete alias inventory:

**NTR_EstateSettings.js**

- `ConfigManager.makeData` — adds the estate keys
  (`estateMasterVolume`, `estateMusicVolume`, `estateSfxVolume`,
  `estateReduceFlashing`, `estateScreenShake`, `estateSubtitles`,
  `estateWindowWidth`, `estateWindowHeight`, `estateVsync`,
  `estateFullscreen`) from the validated live state.
- `ConfigManager.applyData` — original runs first (native keys),
  then estate keys are read with `typeof` checks, repaired through
  the validator, stored, and re-applied, so the estate volume bridge
  wins over the native volume values the original just set. That
  ordering is the bridge contract.

No other core method is touched. In particular: no `AudioManager`
method is aliased — the bridge writes `ConfigManager` values only,
and the engine's own audio code reads them.

**NTR_EstateSave.js** (`ESTATE_FORMAT_VERSION = 1`)

- `DataManager.makeSaveContents` — original runs; the returned
  contents gain `contents.estate = { formatVersion, savedAtUnix,
  playtimeSeconds, label }`, where `savedAtUnix` is
  `floor(Date.now() / 1000)`, `playtimeSeconds` comes from
  `$gameSystem.playtime()` (0 when unavailable), and `label` is
  `$gameMap.displayName()` or `"New Game"`.
- `DataManager.extractSaveContents` — the estate block is validated
  **before** the original runs, on the estate field only: missing →
  default block attached; wrong types → repaired field by field;
  `formatVersion < 1` → `EstateSaveMigrations.migrate` chain first;
  `formatVersion > 1` → `console.warn` and the block is treated as
  absent (`DataManager.estateInfo()` returns `null`). The game's own
  contents are passed to the original exactly as stored; validation
  never throws and never blocks the load.
- `DataManager.makeSavefileInfo` — original runs; the returned info
  gains `estateLabel` and `estateFormatVersion`. In MZ the global
  savefile info is written at save time alongside the contents, which
  is what the load screen reads.
- `DataManager.estateInfo()` — a **new** accessor added by the plugin
  (not an alias): returns the validated block from the most recent
  load, a default block after a load whose save had no block, or
  `null` before any load / when the block was treated as absent.
  Callers must null-check.
- Window display: the plugin ships **no** `Window_SavefileList`
  code. Draw signatures vary between projects; a fragile window alias
  would risk the load screen. The data is available: a game may read
  a savefile's `estateLabel` from the engine's savefile info and draw
  it in its own `Window_SavefileList` customisation.

**NTR_EstateAgeGate.js**

- `Scene_Boot.prototype.start` — original runs to completion first
  (databases load, title scene is queued); only then, if the gate is
  enabled and unconfirmed, `SceneManager.goto(Scene_EstateAgeGate)`.
- New classes only otherwise: `Scene_EstateAgeGate` (a `Scene_Base`
  subclass) and `Window_EstateAgeCommand` (a `Window_Command`
  subclass) with two commands — confirm and exit
  (`SceneManager.exit()`).
- Confirmation state: `localStorage` key `estate_age_confirmed`,
  value `"1"`. Storage errors are treated as "not confirmed" on read
  and ignored on write; the gate never throws.

### Plugin surface contracts

**Estate settings** (`window.EstateSettings`)

- `DEFAULTS` / `SPEC` — every estate key appears once in each.
  Keys: `master_volume` (100), `music_volume` (100), `sfx_volume`
  (100), `reduce_flashing` (false), `screen_shake` (100, percent
  intensity), `subtitles` (true), `window_width` (0 = no override),
  `window_height` (0 = no override), `vsync` (true, engine no-op),
  `fullscreen` (false).
- `validate(candidate)` — total function: complete repaired state
  out, unknown fields dropped, never throws.
- `get(key)` / `set(key, value)` — validated reads/writes; `set`
  re-applies and persists through `ConfigManager.save()` when present.
- `setVolume(channel, value)` — channel in `master | music | sfx`,
  value clamped 0..100; unknown channel warns and changes nothing.
- `apply()` — writes `ConfigManager.estate*` properties and
  recomputes the four native volumes per the bridge in README.md.
  It deliberately does not toggle fullscreen or resize the window.
- `state` — the live validated object; games read
  `reduce_flashing` / `screen_shake` / `subtitles` from here and
  decide themselves what those values do in a scene.

**Plugin commands** (NTR_EstateSettings)

- `SetVolume` — args: `channel` (select: `master` / `music` / `sfx`),
  `value` (number 0..100). Applies immediately and persists.
- `SetFullscreen` — arg: `enabled` (boolean). Desktop route, guarded:
  `typeof nw !== "undefined"` → `nw.Window.get().enterFullscreen()` /
  `leaveFullscreen()`. Web deployments have no MZ core fullscreen
  API: the command logs a warning and changes only the stored value.
- `OpenOptions` — pushes the native `Scene_Options`. Note only: the
  scene is native; see "What not to add" about its volume rows.

**Display honesty**

- `window_width` / `window_height` are read at plugin load and
  applied to `Graphics.width` / `Graphics.height`. In MZ, resizing
  therefore applies at the **next launch**, not mid-session.
- `vsync` is stored for contract parity and is an **engine no-op**:
  MZ renders via `requestAnimationFrame`, vsync-bound by NW.js / the
  browser.
- Where things live: `ConfigManager` is **global** (one config for
  the whole game, all saves). The `$game*` objects are **save-scoped**
  (MZ saves and loads them natively). The estate block lives **inside
  the save contents** and is metadata about the save, nothing more.

---

## 2. What to add

Ordered checklist for extending this template in a real project:

1. **Enable in order.** Plugin Manager order is load order:
   `NTR_EstateSettings`, then `NTR_EstateSave`, then
   `NTR_EstateAgeGate`. Do not reorder; later plugins assume the
   earlier namespaces exist.
2. **Add an estate setting key — exactly three places.**
   1. `DEFAULTS` + `SPEC` in `NTR_EstateSettings.js` (default, type,
      bounds, and its `estate*` `configKey`).
   2. Nothing hand-written in `makeData` / `applyData` — they iterate
      the key list — but verify the new `configKey` round-trips by
      saving config, restarting, and reading the value back.
   3. Wherever the game reads it (via `window.EstateSettings.state`
      or `get(key)`), in the scene / system that owns the behaviour.
   MZ has no estate settings UI: the model is the deliverable.
3. **Scene_Options integration (guidance only).** If a project wants
   estate keys visible in the options scene, that is project code,
   not template code: alias `Window_Options.prototype.makeCommandList`
   and `statusText` / `processOk` in the project's own plugin,
   following the alias pattern in §1, and read/write through
   `window.EstateSettings`. Keep the alias in the project plugin so
   this template's surface stays tight.
4. **Hide the native volume rows once the estate model owns volumes**
   (text guidance only — deliberately not shipped code). In a project
   plugin, alias `Window_Options.prototype.makeCommandList` so the
   four native volume commands are not added:

   ```js
   // Project plugin sketch — guidance, not template code.
   const _Window_Options_makeCommandList = Window_Options.prototype.makeCommandList;
   Window_Options.prototype.makeCommandList = function () {
       _Window_Options_makeCommandList.call(this);
       this._list = this._list.filter(function (cmd) {
           return ["bgmVolume", "bgsVolume", "meVolume", "seVolume"].indexOf(cmd.symbol) < 0;
       });
   };
   ```

   The sketch is unverified template documentation, not a gate-passed
   plugin: test it in the adopting project before shipping it.
5. **Game state joins saves natively.** Progress, switches, variables,
   party, and position belong in the `$game*` objects, which MZ saves
   itself. The estate block stays metadata-only — that is the rule,
   not a suggestion.
6. **Version bump + migration step rule.** Any change to the estate
   block shape: bump `ESTATE_FORMAT_VERSION` by one **and** add
   exactly one step to `EstateSaveMigrations.steps` keyed by the old
   version, which upgrades a block of that version and sets
   `block.formatVersion` to the new version. No shape change without
   both halves.
7. **Events / plugin commands for intro flow.** Use `SetVolume` for
   opening mixes, `SetFullscreen` for a desktop intro option, and
   `OpenOptions` from a custom title command — all from ordinary
   events or the project's own title plugin. The template ships no
   intro scene.
8. **Age gate flip.** Set the `NTR_EstateAgeGate` parameter
   `AgeGateEnabled` to `true` in the Plugin Manager. Nothing else is
   required; to re-test, clear the `estate_age_confirmed` key from
   the deployment's localStorage.

---

## 3. What not to add

- **Never replace a core method.** Alias only, original preserved and
  called. A replacement silently breaks every other plugin that
  aliases the same method.
- **Never store game-critical state only in the estate block.** It is
  metadata. A treated-as-absent block must cost the player nothing
  but estate metadata.
- **No unversioned changes to the estate block shape.** Shape change
  = version bump + migration step (§2.6). No exceptions.
- **No settings key without a default + validation.** A key missing
  from `DEFAULTS` / `SPEC` does not exist.
- **No direct ConfigManager file edits.** `config.rpgsave` is
  engine-owned. Settings change through `ConfigManager` at runtime
  (or the estate model), never by writing the file.
- **Don't fight Scene_Options.** Adopt the estate volume model
  (and hide the native rows, §2.4) or don't adopt it. Half-adopting —
  estate applies in some scenes, native rows live in others — gives
  the player two volume controls that overwrite each other.
- **No new scenes for shell features MZ already has.** Title,
  options, save, and load stay native.
- **No external dependencies.** Vanilla JS against MZ core only.

---

## 4. Gates

Syntax gate (run at delivery; run again after any plugin edit):

```sh
node --check js/plugins/NTR_EstateSettings.js
node --check js/plugins/NTR_EstateSave.js
node --check js/plugins/NTR_EstateAgeGate.js
```

**Delivery result: `node --check` clean on all three plugins.**
The in-editor cases below were **not** run for this delivery; they
are the first-load checklist for the adopting project.

LSP gate: theodolite bridge server **`typescript-language-server`**
covers JavaScript — zero error diagnostics on every touched `.js`
file before the file is called done. Formatter: **prettier** (Diver
wires prettierd / prettier for JavaScript), with the MZ-style
4-space indent used throughout these plugins.

### First-load checklist (in editor, half validation / half adversarial)

Validation — expected outcomes:

1. Fresh project, plugins enabled in order, playtest: no console
   errors; `window.EstateSettings.state` equals `DEFAULTS`.
2. Event runs **Set Volume** (music, 40) with master at 100:
   `ConfigManager.bgmVolume` and `bgsVolume` read 40;
   `ConfigManager.seVolume` / `meVolume` unchanged. Set master to 50:
   all four recompute (bgm/bgs 20, se/me halved).
3. Save the game on a named map: the save succeeds natively; reloading
   it, `DataManager.estateInfo()` returns a block with
   `formatVersion` 1, a plausible `savedAtUnix` and
   `playtimeSeconds`, and `label` equal to the map display name.
   The savefile's info carries the same label as `estateLabel`.
4. Quit and relaunch: estate settings persist (config round-trip
   through `config.rpgsave`); native settings persist as before.

Adversarial — expected outcomes:

5. Hand-corrupt the estate block inside a save (scramble field types,
   e.g. via a script call writing garbage into the stored contents,
   or by editing an exported save in a test harness): the game
   **loads**, its own contents intact; the estate block degrades to
   repaired defaults.
6. Set the stored block's `formatVersion` to 99: the game **loads**;
   a warning is logged; `DataManager.estateInfo()` returns `null`;
   the game's own contents are unaffected.
7. Corrupt or delete `config.rpgsave`: MZ regenerates defaults on
   next launch, and the estate keys fall back to their `DEFAULTS`
   through the same `applyData` repair path; the game boots.
8. Enable the age gate with the `estate_age_confirmed` flag cleared:
   the gate shows before the title; choosing **Exit** exits the game;
   relaunching and choosing **I am 18 or older** reaches the title,
   and the next launch skips the gate.

Label every result honestly: `node --check` was run at delivery;
the in-editor cases are a checklist until an adopting project runs
and records them.

---

## 5. Neovim workflow

- **LSP:** `ts_ls` in the editor; the theodolite bridge route for
  gates is **`typescript-language-server`** (it covers JavaScript).
  Zero error diagnostics before a file is done.
- **Syntax gate from the terminal:**

  ```sh
  node --check js/plugins/NTR_EstateSettings.js
  node --check js/plugins/NTR_EstateSave.js
  node --check js/plugins/NTR_EstateAgeGate.js
  ```

- **Formatting:** prettier (Diver wires prettierd, then prettier, for
  the JavaScript filetype); keep the 4-space MZ plugin indent.
- **Playtest debugging:** playtest launches from the MZ editor and is
  an NW.js app; launching it is editor-side — there is no terminal
  playtest command in this workflow. Node DAP attaches to plain Node
  processes, not to the editor's playtest; MZ's own playtest console
  (F8 / F12 in a playtest) is the first debugging surface. An
  NW.js remote-debug session is an advanced, optional route and is
  not part of this template's gates.
- **Gotchas:**
  - `js/plugins.js` (the project's plugin registry) is
    **editor-generated**. Do not hand-edit it in the template; the
    install steps in README.md are the registration route.
  - **Plugin order is load order.** The Plugin Manager list order
    (§2.1) is a runtime contract, not a cosmetic preference.
  - **Scope split:** `ConfigManager` is global; `$game*` objects are
    save-scoped; the estate block is inside one save's contents.
    Putting a value in the wrong scope is the classic MZ bug.
  - Plugin headers (`/*:` blocks) are parsed by the editor: a
    malformed `@param` / `@command` block can silently drop a
    parameter or command from the Plugin Manager. After editing a
    header, re-open the Plugin Manager and confirm the parameters
    and commands still appear.
  - `DataManager` / `ConfigManager` / `Scene_*` globals exist because
    MZ core scripts load before plugins. The same file is not valid
    standalone Node code — `node --check` verifies syntax only, by
    design of the gate.
