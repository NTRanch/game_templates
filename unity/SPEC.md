# SPEC — Unity Game Shell

This is the contract for the Unity template of
`NTRanch/game_templates`. The code in `Assets/GameShell/Scripts/`
implements it; where code and this document disagree, stop and
reconcile them rather than guessing.

**Verification status: STATIC / template-grade.** The template was
not compiled in a Unity editor for this delivery and no gate below
is claimed as run. The gates are the checklist a project (or an
agent with an editor) runs to promote the template from static to
verified in that project.

Conventions: all types live in `NiceThoughtsRanch.GameShell`. One
public type per file. Expected failures (missing files, corrupt
files, full disks, refused versions) return `bool` / result values
and log a `Debug.LogWarning`; exceptions are reserved for contract
violations by the calling code, such as a null delegate passed to
`ShellBridge.RegisterPayloadProvider`. Runtime scripts use no
`UnityEditor` APIs.

---

## 1. File manifest

### `Assets/GameShell/Scripts/SettingsData.cs`

- **Purpose:** the settings payload POCO, serialized to
  `settings.json`.
- **Contract:** `[Serializable]`; snake_case field names are the
  on-disk JSON names. Fields: `master_volume`, `music_volume`,
  `sfx_volume` (float, default 1.0); `fullscreen` (bool, default
  false); `window_width` (int, default 1280), `window_height`
  (int, default 720); `vsync` (bool, default true);
  `reduce_flashing` (bool, default false); `screen_shake` (float,
  default 1.0); `subtitles` (bool, default true). `Default()`
  returns the canonical defaults. `Validate()` clamps volumes and
  screen shake to 0..1, width to 640..7680, height to 360..4320,
  and repairs NaN / infinity to the field default.
- **Bounds:** the min/max values are named constants on the type;
  no other code hard-codes them.

### `Assets/GameShell/Scripts/SettingsManager.cs`

- **Purpose:** loads, applies, and persists `SettingsData`;
  singleton `MonoBehaviour`.
- **Contract:** first instance wins (`Instance`,
  `DontDestroyOnLoad`); a duplicate destroys itself. File:
  `Path.Combine(Application.persistentDataPath, "settings.json")`,
  written atomically (`settings.json.tmp` then `File.Replace` /
  `File.Move`). A file that fails to parse is moved to
  `settings.json.corrupt`, a warning is logged, and defaults are
  used — loading never throws to the player. Apply: master volume
  to `AudioListener.volume`; music / SFX to the optional
  `[SerializeField]` `AudioMixer` references (exposed parameters
  `MusicVolume` / `SfxVolume`, linear-to-dB with a −80 dB floor);
  when no mixer is assigned the values are stored and exposed via
  `Current` for the game to consume — display to
  `Screen.SetResolution(w, h, fullscreen)` and
  `QualitySettings.vSyncCount` (1 or 0). Every `Set*` method
  validates, applies, saves, and raises `SettingsChanged`
  (`System.Action`).
- **Bounds:** one `DontDestroyOnLoad` object is created by the
  shell (this one); see "What not to add".

### `Assets/GameShell/Scripts/SaveEnvelope.cs`

- **Purpose:** the flat, versioned record stored in each slot file.
- **Contract:** `public const int CurrentFormatVersion = 1`;
  fields `format_version` (int), `saved_at_unix` (long, Unix
  seconds UTC), `playtime_seconds` (long), `label` (string, never
  null once validated), `payload_json` (string).
- **Why this shape:** `JsonUtility` has no dictionary support, so
  the envelope is flat and the game's own serialized state rides
  as the opaque string `payload_json`. The shell never parses the
  payload. Prefer additive optional fields over a version bump
  (see `SaveMigrations.cs`).

### `Assets/GameShell/Scripts/SaveManager.cs`

- **Purpose:** the static save-slot store.
- **Contract:** directory
  `Path.Combine(Application.persistentDataPath, "saves")`; slots
  are exactly `SlotNames = { "slot1", "slot2", "slot3", "auto" }`;
  unknown slot names are refused (never turned into file names).
  Methods: `Save(slot, payloadJson, label, playtimeSeconds) ->
  bool`, `TryLoad(slot, out SaveEnvelope) -> bool`,
  `HasSave(slot) -> bool`, `HasAnySave() -> bool`,
  `GetMetadata(slot, out SaveEnvelope) -> bool` (label, save time,
  play time for menus, without trusting the payload), `Delete(slot)
  -> bool`, `IsValidSlot(slot) -> bool`, `GetSlotPath(slot)`.
  Writes are atomic (`.tmp` then replace). All IO is wrapped in
  try/catch returning bool + `Debug.LogWarning`. `TryLoad` runs
  `SaveMigrations.Migrate`; a corrupt slot (JSON parses to null or
  throws) is quarantined to `<slot>.json.corrupt` and treated as
  empty, other slots untouched; a `format_version` above current
  is refused and treated as empty but **not** quarantined.
- **Bounds:** four slots, closed set; no enumeration of arbitrary
  files in the saves directory.

### `Assets/GameShell/Scripts/SaveMigrations.cs`

- **Purpose:** the format migration chain called from
  `SaveManager.TryLoad`.
- **Contract:** `Migrate(envelope) -> bool` (false = refuse).
  While `format_version < CurrentFormatVersion`, apply the step
  for that version; `StepV0ToV1` is implemented (v0 saves predate
  `label`, so a null label defaults to `""`; a null payload is
  likewise repaired). Negative versions and versions above current
  are refused. Steps mutate in place, one version increment each.
- **Rule:** additive optional fields first; a version bump only
  when an existing field changes meaning or shape.

### `Assets/GameShell/Scripts/ShellBridge.cs`

- **Purpose:** THE integration seam between shell and game.
- **Contract:** static class. The game calls
  `RegisterPayloadProvider(Func<string> getJson,
  Func<long> getPlaytimeSeconds, Action<string> restore)` once
  from its bootstrap; null delegates throw
  `ArgumentNullException` (a programmer contract violation, not an
  expected runtime failure). `TryGetPayload(out string, out long)
  -> bool` and `TryRestore(string) -> bool` catch provider
  exceptions, log warnings, and return false — a misbehaving
  provider fails the save/load, never the menu.
  `ClearPayloadProvider()` unregisters; `HasProvider` reports
  registration. Play-mode statics caveat is documented in the
  file: re-register on scene load.

### `Assets/GameShell/Scripts/GameFlow.cs`

- **Purpose:** the menu state machine and panel visibility owner.
- **Contract:** nested enums `FlowState { AgeGate, Title, Game,
  Paused }` and `SettingsReturnTo { Title, Pause }`;
  `public static bool AgeGateEnabled = false` (off by default;
  when true the age gate is the first panel shown). Methods:
  `Boot()`, `ConfirmAgeGate()`, `DeclineAgeGate()` (quits),
  `StartNewGame()`, `ContinueGame() -> bool` (loads the most
  recently written slot, `auto` preferred on ties by search
  order), `LoadSlot(slot) -> bool`, `SaveToSlot(slot, label) ->
  bool`, `OpenPause()`, `ClosePause()`, `OpenSettings(from)`,
  `CloseSettings()`, `QuitToTitle()`, `QuitGame()`
  (`Application.Quit`, with the editor note in the doc comment).
  Panels are `[SerializeField]` `GameObject` references:
  `titlePanel`, `pausePanel`, `settingsPanel`, `ageGatePanel`,
  `hudRoot`. Settings return target is tracked explicitly in
  `CurrentSettingsReturn`; `CloseSettings` returns there.
- **Pause contract:** `OpenPause` captures `Time.timeScale` and
  sets it to 0; `ClosePause` restores the captured value.
  Pausing does not destroy or reset game state. Coroutines driven
  by scaled time stop while paused (use unscaled time for menu
  animation); audio behaviour on pause is the game's choice, not
  a side effect of the shell.

### `Assets/GameShell/Scripts/MainMenuUI.cs`

- **Purpose:** title menu and load-mode slot panel.
- **Contract:** buttons wired in code in `Awake`
  (`onClick.AddListener`) and removed in `OnDestroy`: Continue,
  New Game, Load, Settings, Quit, slot panel close, and four slot
  buttons. Continue (and Load) `interactable` tracks
  `SaveManager.HasAnySave()`, refreshed on enable and via the
  public `RefreshContinue()`. Slot labels come from
  `SaveManager.GetMetadata`:
  `"<slot> — <label> — <yyyy-MM-dd HH:mm>"` (label shown as
  `Untitled` when empty) or `"<slot> — Empty"`. Load mode enables
  only non-empty slots. Slot order is `SaveManager.SlotNames`.

### `Assets/GameShell/Scripts/PauseMenuUI.cs`

- **Purpose:** pause overlay and the save/load slot panel.
- **Contract:** buttons Resume, Save, Load, Settings, Quit to
  Title, wired as in `MainMenuUI`. The slot panel has two modes:
  save mode enables every slot and calls
  `GameFlow.SaveToSlot(slot, "Manual save")`; load mode enables
  only non-empty slots and calls `GameFlow.LoadSlot(slot)`.
  Labels use the same format as the main menu. Saving pulls the
  payload through `ShellBridge` (via `GameFlow`); no provider
  means the save fails with a warning, never a crash.

### `Assets/GameShell/Scripts/SettingsUI.cs`

- **Purpose:** binds every canonical setting to a control.
- **Contract:** sliders for master / music / SFX / screen shake
  with percentage value labels; toggles for fullscreen, vsync,
  reduce flashing, subtitles; input fields for window width and
  height (integer parse on end edit, unparsable input falls back
  to the current value, result clamped by validation). Back calls
  `GameFlow.CloseSettings()`; Reset Defaults calls
  `SettingsManager.ResetDefaults()` and repopulates. Populates
  from `SettingsManager.Current` on enable using
  `SetValueWithoutNotify` / `SetIsOnWithoutNotify` /
  `SetTextWithoutNotify`, so opening the panel writes nothing;
  every later control change writes through the manager
  immediately (apply + save + event).

### `Assets/GameShell/Scripts/AgeGateUI.cs`

- **Purpose:** the optional age-gate panel.
- **Contract:** a `Text` message (`SetMessage(string)` replaces
  it), Confirm and Decline buttons routing to
  `GameFlow.ConfirmAgeGate()` / `GameFlow.DeclineAgeGate()`.
  Present and wired, but only ever shown when
  `GameFlow.AgeGateEnabled` is true. It collects and persists
  nothing; it is a menu seam, not a compliance mechanism (see the
  file's doc comment).

### `README.md`

- Orientation, the honest static verification status, and the
  drop-in summary. Kept short; this SPEC is the deep contract.

### `SPEC.md` (this file)

- The contract: manifest, build-out checklist, refusals, gates,
  and the Neovim workflow.

### `SKILLS.md`

- The skill / tool map for work on this template, and the Bloch
  lane law governing lesson records.

### `AGENTS.md`

- Agent working rules for this template: read this SPEC first,
  the theodolite gate, formatter, envelope rules, `.meta`
  discipline.

### `docs/bloch-lesson-template.md`

- The lesson-record template: `title`, `date`, `origin_lane`,
  `content_label`, `evidence`, and the lesson body. Every lesson
  is born with its birth metadata filled in.

### `book.toml`, `book/src/SUMMARY.md`, `book/src/*.md`

- The mdBook shell. `book/src/readme.md`, `spec.md`, `skills.md`,
  and `agents.md` include the top-level documents with
  \{{#include ...}} so the book never drifts from the repo text.
  Build output (`book-out/`) is generated and git-ignored.

### License

- Apache-2.0, credited to Nice Thoughts Ranch. The license text
  is the repo `LICENSE` at the repository root; this directory
  carries no separate license file.

---

## 2. What to add

Ordered checklist for turning the drop-in shell into a game's
menus. Do the steps in order; each step names the exact objects
and fields to wire.

### 2.1 Scenes

1. **Boot scene** (`Boot.unity`, first in Build Settings):
   - `SettingsManager` on a `GameShell` GameObject (assign the
     optional AudioMixers here if the project uses them; see 2.5).
   - `GameFlow` on the same or a sibling GameObject.
   - An `EventSystem` and the canvases below, all panels present in
     the scene with `GameFlow`'s serialized panel fields assigned:
     `titlePanel`, `pausePanel`, `settingsPanel`, `ageGatePanel`,
     `hudRoot`.
2. **Title canvas** (inside the Boot scene, or a scene loaded by
   it) containing the `titlePanel`:
   - `MainMenuUI` component with `gameFlow` assigned.
   - Buttons named `ContinueButton`, `NewGameButton`,
     `LoadButton`, `SettingsButton`, `QuitButton`, assigned to the
     matching serialized fields.
   - A `slotPanel` GameObject (starts inactive) with four buttons
     in an array field in `SlotNames` order — the buttons' child
     `Text` labels are rewritten at runtime — plus
     `slotPanelCloseButton`.
3. **HUD root** (`hudRoot`): the in-game overlay root; shown in
   the Game and Paused states, hidden in menus.
4. **Pause canvas** containing the `pausePanel`:
   - `PauseMenuUI` component with `gameFlow` assigned.
   - Buttons `ResumeButton`, `SaveButton`, `LoadButton`,
     `SettingsButton`, `QuitToTitleButton`, and a `slotPanel`
     with four slot buttons (`SlotNames` order) plus
     `slotPanelCloseButton`.
   - Pause input is the game's: call `GameFlow.OpenPause()` from
     the project's input handling (for example an Escape key check
     or a HUD pause button).
5. **Settings canvas** containing the `settingsPanel`:
   - `SettingsUI` component with `gameFlow` assigned and every
     control field wired: `masterVolumeSlider` +
     `masterVolumeValue`, `musicVolumeSlider` + `musicVolumeValue`,
     `sfxVolumeSlider` + `sfxVolumeValue`, `screenShakeSlider` +
     `screenShakeValue`, `fullscreenToggle`, `vsyncToggle`,
     `reduceFlashingToggle`, `subtitlesToggle`,
     `windowWidthInput`, `windowHeightInput`, `backButton`,
     `resetDefaultsButton`. Sliders are configured 0..1 in the
     inspector.
6. **Age gate canvas** containing the `ageGatePanel` (only if the
   project will ever enable the gate; harmless to build and leave
   unused):
   - `AgeGateUI` with `gameFlow`, `messageText`, `confirmButton`,
     `declineButton` assigned, and project-written message text.
7. **One gameplay scene**: the project's scene, loaded by project
   code. Its bootstrap registers the payload provider (2.3) and,
   on unload, calls `ShellBridge.ClearPayloadProvider()`.

### 2.2 Registering a new setting (three places, all required)

1. `SettingsData`: add the field with its default, and extend
   `Validate()` with its clamp or repair (named constants for the
   bounds).
2. `SettingsManager`: add a `Set<Name>` method that assigns,
   then flows through the existing validate → apply → save →
   announce path; extend `Apply()` if the setting has a live
   engine effect.
3. `SettingsUI`: add the control, wire it in `Awake` /
   `OnDestroy`, and populate it in `Populate()` with a
   `WithoutNotify` setter.

A setting missing any of the three is a defect: unpersisted,
unapplied, or unreachable.

### 2.3 Payload integration via ShellBridge

From the gameplay bootstrap, exactly one registration:

```csharp
ShellBridge.RegisterPayloadProvider(
    () => MyGame.SerializeState(),
    () => MyGame.PlaytimeSeconds,
    json => MyGame.RestoreState(json));
```

`SerializeState` returns the game's own JSON (the game owns its
shape; a dictionary-shaped state is the game's problem to flatten
or encode, because the shell payload is a single string precisely
so `JsonUtility`'s limits stop at the envelope). `RestoreState`
receives exactly the string a previous save stored.

### 2.4 Bumping the save version (only when forced)

1. Increment `SaveEnvelope.CurrentFormatVersion`.
2. Add `StepV{n}ToV{n + 1}` in `SaveMigrations` and extend the
   chain; the step mutates the envelope and sets `format_version`.
3. Add gate cases (section 4): a hand-written old-version slot
   file must load and migrate; a future-version file must be
   refused.

### 2.5 Audio mixer for music / SFX volumes

Without a mixer the music / SFX settings are stored values the
game reads from `SettingsManager.Current`. To route them live:

1. Create an `AudioMixer` asset; add `Music` and `SFX` groups.
2. Expose each group's volume parameter under the exact names
   `MusicVolume` and `SfxVolume`.
3. Route music sources to the Music group and effect sources to
   the SFX group.
4. Assign the mixer to `SettingsManager`'s `musicMixer` and
   `sfxMixer` fields in the Boot scene.

### 2.6 Build settings

- Build Settings scene list: `Boot` first, then the gameplay
  scene(s).
- Player settings: set the company and product names; the
  persistent data path (and therefore the settings / saves
  location) derives from them.
- No additional packages are required or permitted for the shell
  itself: it compiles against `UnityEngine`, `UnityEngine.UI`,
  and `UnityEngine.Audio` only.

---

## 3. What not to add

- **No `PlayerPrefs` for saves or settings.** The file-JSON
  contracts in this SPEC are inspectable, portable, and
  quarantinable; `PlayerPrefs` is none of those and its location
  varies by platform.
- **No unversioned envelopes.** Every slot file carries
  `format_version`; an envelope without one cannot be migrated or
  safely refused.
- **No settings without a default and a clamp.** Unbounded values
  from a hand-edited file become crashes or broken displays
  downstream.
- **No `DontDestroyOnLoad` sprawl beyond the two managers.**
  `SettingsManager` persists itself; `GameFlow` may be persisted
  by a project's scene design. A third persistent object needs a
  design reason recorded in this SPEC first.
- **No exceptions as control flow for expected failures.**
  Missing, corrupt, and refused saves are normal outcomes with
  `bool` results; exceptions are for contract violations by
  calling code.
- **No `JsonUtility` for dictionary-shaped game state.** It has
  no dictionary support; game state rides in the payload string
  (section 2.3) precisely so the game's serializer is its own
  choice.
- **No `UnityEditor` APIs in runtime scripts.** Editor-only code
  breaks player builds; editor tooling belongs in an `Editor`
  folder assembly, if a project adds any.
- **No hand-edited `.meta` files.** They are engine-owned
  identity; see section 5.

---

## 4. Gates

No gate in this section was run for the template's initial
delivery (see the verification status at the top). Run them in
the project that adopts the template.

### 4.1 In-editor smoke checklist

Setup: build the scenes per section 2.1, press Play on the Boot
scene.

Validation half:

1. **Title shows.** Play: the title panel is the first thing
   visible (or the age gate first, when
   `GameFlow.AgeGateEnabled` is set true for this check; set it
   back afterwards). Continue is disabled when no save exists.
2. **Settings round trip persists.** Open Settings, move the
   master volume slider, Back, stop Play, press Play again:
   `settings.json` exists under `Application.persistentDataPath`
   and the slider shows the moved value.
3. **Save/load round trip.** Start a game (with a payload
   provider registered per 2.3), change game state, Pause →
   Save → slot1, Quit to Title: Continue is now enabled and the
   slot panel shows slot1's label and date. Load slot1: the game
   state matches the saved state.
4. **Pause freezes, resume restores.** Pause during motion:
   scaled-time motion stops, game state is untouched; Resume
   continues from the same state. Settings opened from Pause
   returns to Pause (still frozen); Settings opened from Title
   returns to Title.

Adversarial half:

5. **Corrupt-file drill (settings).** Stop Play, hand-corrupt
   `settings.json` (truncate it mid-object), Play: the game
   reaches the title with default settings, a warning is logged,
   and `settings.json.corrupt` exists beside a freshly written
   valid `settings.json`.
6. **Corrupt-file drill (one slot).** Corrupt `saves/slot1.json`
   the same way, Play: slot1 is treated as empty and quarantined;
   other slots still load.
7. **Version-skew drill.** Hand-edit a slot's `format_version`
   to `99`: the slot is refused (treated as empty, warning
   logged) and the file is **not** quarantined or overwritten.
   Hand-write a `format_version: 0` envelope with no `label`:
   it loads, migrates, and shows an empty label.
8. **No-provider drill.** Without registering a payload
   provider, Pause → Save: the save fails with a `ShellBridge`
   warning and the menu stays usable; nothing throws.

### 4.2 Terminal routes

- **Unity batchmode compile check** (the command to run where an
  editor exists; **not run for this delivery**):

  ```sh
  unity -batchmode -projectPath <project> -quit -logFile <log-path>
  ```

  Judge the run by the log and the absence of compile errors in
  it, never by the exit code alone. Editor version is read from
  the project's `ProjectSettings/ProjectVersion.txt`.
- **Theodolite route (the agent gate):** every edited `.cs` file
  is validated through the theodolite bridge with server id
  **`csharp-language-server`**; zero error diagnostics before a
  file is called done. This is a real route: agents must use it
  rather than eyeballing syntax (see `AGENTS.md`). Note that LSP
  validation proves the file is coherent C# against the
  references the bridge resolves; it does not replace the
  in-editor smoke, because the Unity API surface is only fully
  present inside a project.
- **Formatter:** CSharpier. The template sources are formatted
  CSharpier-style by hand (4-space indent, 100-column target);
  after import, run CSharpier over `Assets/GameShell/Scripts` and
  treat a diff as a formatting fix, not a style debate.

---

## 5. Neovim workflow

- **LSP:** Roslyn via Diver's C# config (`lsp/roslyn_ls`) for
  in-editor diagnostics, and theodolite's
  `csharp-language-server` bridge route as the headless gate
  (section 4.2). Both speak to the same files; the gate result is
  the theodolite one.
- **DAP:** `netcoredbg` — attach to the Unity Editor or to a
  development player. Breakpoints in shell code hit the same
  process the editor runs; a standalone player needs a
  development build for symbols to resolve.
- **Gotchas:**
  - `.meta` files are engine-owned. Never hand-edit them, never
    invent a GUID; let the editor generate them on first import
    and commit them once generated. A missing `.meta` makes Unity
    mint a new GUID, which silently breaks serialized references.
  - `Library/` is generated state: git-ignore it, never commit
    it, delete it freely to force a reimport.
  - `JsonUtility` limits are load-bearing here: no dictionaries,
    no top-level arrays, fields not properties, and nulls can come
    back from damaged files despite non-null declarations —
    validate at the boundary (`Validate()`, `Migrate`) instead of
    trusting the annotations.
  - Statics survive Play sessions when domain reload is off:
    `ShellBridge` registrations and `SettingsManager.Instance`
    are re-established on scene load / `Awake`, and the template
    code is written to tolerate a stale registration being
    replaced.
