# SPEC — Unreal Engine 5.8 Game Shell

The deep contract for this template. `README.md` is the orientation;
this file is normative. Target: **Unreal Engine 5.8**. License:
Apache-2.0. Credit: Nice Thoughts Ranch.

**Delivery status: STATIC — text-authored, never compiled, never
opened in the Editor, never run.** Every gate below is written as
the checklist for the first machine that has a UE 5.8 install.

## 1. File manifest

### Project and config

| File | Purpose / contract / bounds |
| --- | --- |
| `GameShell.uproject` | FileVersion 3, EngineAssociation `5.8`, one Runtime module `GameShell`, no plugin dependencies. JSON; validated by `python3 -m json.tool` (see GATES). |
| `Config/DefaultGame.ini` | `GeneralProjectSettings` only: ProjectID, ProjectName "Game Shell", Description, ProjectVersion 1.0.0. No gameplay config lives here. |
| `Config/DefaultEngine.ini` | `GameInstanceClass=/Script/GameShell.ShellGameInstance`; `EditorStartupMap` and `GameDefaultMap` = `/Game/Maps/TitleMap.TitleMap` (an editor-created map; the header comment in the file says so). Deliberately no global GameMode: TitleMap carries `AShellGameMode` as a World Settings override, gameplay maps choose their own. |

### C++ module — `Source/GameShell/`

| File | Purpose / contract / bounds |
| --- | --- |
| `GameShell.Build.cs` | Module rules. Public deps: Core, CoreUObject, Engine, InputCore, UMG, Slate, SlateCore, Json (Json exists for the game layer's PayloadJson work). No private deps. |
| `GameShell.h` / `GameShell.cpp` | Module boilerplate (`FDefaultGameModuleImpl`) and the single log category `LogGameShell`. Every failure path in the template logs through it. |
| `ShellTypes.h` | `EShellFlowState`, `EShellSettingsReturnTo`, the `ShellSlots` canonical slot names + user index, `FShellSettings` (defaults + `Validate()` clamps/repairs + `HasInvalidFloats()` NaN check, implemented inline), `FShellSlotMetadata`. Window bounds are named constants (640..7680 × 360..4320). |
| `ShellFileUtil.h` | Internal, non-reflected helpers shared by the settings subsystem and the save library: slot `.sav` path under `ProjectSavedDir()/SaveGames`, and `QuarantineSlotFile()` — copies a bad slot file to `<slot>_corrupt.sav`, logs the outcome, never throws, never deletes the original. Relies on the engine's slot-name→file mapping; verifying that mapping is a bring-up check (GATES). |
| `ShellSettingsSave.h/.cpp` | `UShellSettingsSave` (USaveGame): `FShellSettings Settings` + `SettingsFormatVersion` (current = 1, its own version line, independent of the save envelope). Pure data; all behaviour in the subsystem. |
| `ShellSettingsSubsystem.h/.cpp` | `UGameInstanceSubsystem` owning settings: load-on-initialize with the corruption rules in its header comment (missing → defaults; unloadable/NaN → quarantine + defaults; newer format → defaults, slot untouched), validate+save on every setter, `ApplyDisplaySettings()` via `UGameUserSettings`, `ApplyAudioVolumes(USoundClass*, …)` via the main audio device with null guards. |
| `ShellSaveGame.h/.cpp` | `UShellSaveGame` (USaveGame), the envelope: `FormatVersion` (current = 1), `SavedAtUnix` (int64, UTC), `PlaytimeSeconds` (int64), `Label`, `PayloadJson`. Pure data; all behaviour in the library. |
| `ShellSaveLibrary.h/.cpp` | `UBlueprintFunctionLibrary`, the only save/load path: `SaveShellGame`, `LoadShellGame`, `DoesShellSaveExist`, `DeleteShellSave`, `GetShellSlotMetadata`, `GetExistingSlots` over the canonical slots. Payload bound `MaxPayloadChars` = 1,048,576. `MigrateSave()` steps versions one at a time, bounded at 8 steps; ships the trivial v0→v1 (default Label). Newer formats refused; corrupt slots quarantined via `ShellFileUtil` and treated as empty. Metadata note: there is no header-only read in the SaveGame system, so metadata loads the whole object — documented cost, fine for four slots on a title screen. |
| `ShellGameInstance.h/.cpp` | `UGameInstance`: flow state machine, `bAgeGateEnabled` (plain property, default false), pause/resume via `UGameplayStatics::SetGamePaused` with state guards, `TransitionToGame(LevelName)` (empty name = state only), settings open/close with `EShellSettingsReturnTo`, `QuitShellGame()`. Owns flow only — no settings or save storage (see §3). |
| `ShellGameMode.h/.cpp` | Minimal `AGameModeBase` for the title map; constructor sets `bUseSeamlessTravel = false`. |
| `ShellTitleWidget.h/.cpp` | Title base: `RefreshContinueState()` (`bHasAnySave`), Continue loads the newest readable save, fires `OnSaveLoaded(UShellSaveGame*)`, then transitions flow to Game; `OnNewGameRequested()` is the game layer's new-game seam. Optional binds: `NewGameButton`, `ContinueButton`, `SettingsButton`, `QuitButton`. |
| `ShellPauseWidget.h/.cpp` | Pause base: resume/settings/quit-to-title actions; save writes slot `auto` with label/payload/playtime from the `BuildSaveLabel()` / `BuildSavePayloadJson()` / `BuildPlaytimeSeconds()` BlueprintImplementableEvents. Optional binds: `ResumeButton`, `SaveButton`, `SettingsButton`, `QuitToTitleButton`. |
| `ShellSettingsWidget.h/.cpp` | Settings base: every control forwards to subsystem setters; `RefreshFromSettings()` populates bound controls; three `USoundClass*` class properties (assigned in WBP defaults) feed `ApplyAudioVolumes()` on volume changes. Optional binds: `MasterVolumeSlider`, `MusicVolumeSlider`, `SfxVolumeSlider`, `ScreenShakeSlider`, `FullscreenCheckBox`, `VSyncCheckBox`, `SubtitlesCheckBox`, `ReduceFlashingCheckBox`, `BackButton`. |
| `ShellAgeGateWidget.h/.cpp` | Age-gate base: Confirm → GameInstance `ConfirmAgeGate()`; Decline → `UKismetSystemLibrary::QuitGame`. Optional binds: `ConfirmButton`, `DeclineButton`. Shown only when the age gate is enabled. |

### Docs and book

| File | Purpose |
| --- | --- |
| `README.md` | Orientation + the code-complete vs editor-pending split. |
| `SPEC.md` | This file. |
| `SKILLS.md` | Skill/tool map + Bloch lane law summary. |
| `AGENTS.md` | Short working rules for agents in this directory. |
| `docs/bloch-lesson-template.md` | Lesson record template with birth metadata. |
| `book.toml`, `book/src/SUMMARY.md`, `book/src/{readme,spec,skills,agents}.md` | mdBook: title "Unreal Game Shell", authors ["Nice Thoughts Ranch"], language "en", `src = "book/src"`, build dir `book-out`, `create-missing = false`, theme `../theme-torogao`. The four chapter files are `{{#include}}` stubs over the root docs — one source of truth, no copies. |

## 2. What to add — ordered checklist

Editor steps come first because the code cannot be exercised
without them. Each step names its done-condition.

1. **First compile.** Open `GameShell.uproject` in UE 5.8, let it
   generate project files, build the `GameShell` editor target
   (GATES has the command-line form). Done when: the build log
   shows 0 errors. Fix API drift here, not later — this template
   was never compiled, so this step is a real gate, not a formality.
2. **Create the maps.** Content Browser → `Content/Maps/`: create
   `TitleMap` (empty level is fine) and one gameplay map (any name;
   the examples below call it `GameMap`). In `TitleMap` → World
   Settings → GameMode Override = `ShellGameMode`. Done when:
   opening the project no longer warns about a missing startup map
   and Play in Editor lands on TitleMap.
3. **Build the four WBP widgets.** For each, create a Widget
   Blueprint in `Content/UI/`, set its parent class, and lay out
   controls with the exact names from the base-class headers:
   - `WBP_Title` (parent `UShellTitleWidget`): buttons
     `NewGameButton`, `ContinueButton`, `SettingsButton`,
     `QuitButton`; bind `ContinueButton`'s enabled state to
     `bHasAnySave`; implement `OnNewGameRequested` (open your
     gameplay map, then call `TransitionToGame`) and `OnSaveLoaded`
     (restore state from the envelope payload).
   - `WBP_Pause` (parent `UShellPauseWidget`): buttons
     `ResumeButton`, `SaveButton`, `SettingsButton`,
     `QuitToTitleButton`; implement `BuildSavePayloadJson`,
     `BuildSaveLabel`, `BuildPlaytimeSeconds`,
     `OnQuitToTitleRequested` (open `TitleMap`).
   - `WBP_Settings` (parent `UShellSettingsWidget`): sliders
     `MasterVolumeSlider`, `MusicVolumeSlider`, `SfxVolumeSlider`,
     `ScreenShakeSlider` (range 0..1); checkboxes
     `FullscreenCheckBox`, `VSyncCheckBox`, `SubtitlesCheckBox`,
     `ReduceFlashingCheckBox`; button `BackButton`; any resolution
     control you add calls the base `SetResolution`.
   - `WBP_AgeGate` (parent `UShellAgeGateWidget`): buttons
     `ConfirmButton`, `DeclineButton`, plus your legal text.
   Done when: each WBP compiles in the Editor with no missing-bind
   errors and a HUD/level Blueprint can create and show `WBP_Title`
   on TitleMap.
4. **Show the widgets from the flow.** In TitleMap's Level
   Blueprint (or a small HUD class you add): on begin play, read
   the GameInstance flow state — `AgeGate` → show `WBP_AgeGate`
   (its Confirm event chain leads to Title), otherwise show
   `WBP_Title`. Pause input (an input action or key binding of your
   game) calls `PauseGame()` and shows `WBP_Pause`; Settings
   buttons show `WBP_Settings` as an overlay and Back returns.
   Done when: the GATES validation tests pass.
5. **Create the Sound Classes.** In `Content/Audio/`: Sound Class
   `SC_Master`; `SC_Music` and `SC_SFX` as its children. Route your
   music and SFX assets into their classes. In `WBP_Settings` class
   defaults, assign the three `*SoundClass` properties. Optionally
   add a Sound Mix later; the shell does not need one. Done when:
   moving the master volume slider audibly changes a playing sound
   in PIE (the only step where a human ear is the gate).
6. **Register a new setting (when you need one) — three places,
   all of them:**
   1. `FShellSettings`: field with default + clamp/repair in
      `Validate()` (and the NaN check if it is a float).
   2. `UShellSettingsSubsystem`: a setter that validates, saves,
      and applies (plus an apply path if it affects the engine).
   3. `UShellSettingsWidget`: a control that calls the setter.
   A setting in fewer than three places is a bug by contract.
7. **Payload discipline.** Game state → JSON string (engine Json
   module: `FJsonObject` + `FJsonSerializer`, or
   `FJsonObjectConverter` from a `USTRUCT`) → `PayloadJson` via
   `UShellSaveLibrary`. Rules: additive fields first (old saves
   keep loading); any shape change bumps
   `UShellSaveGame::CurrentFormatVersion` and adds the step to
   `MigrateSave()` in the same change; the game layer owns the
   payload schema, the shell never parses it.
8. **Input actions** (Enhanced Input) for pause/menu navigation are
   the game's to add; the shell exposes only the flow functions
   they should call.
9. **Packaging.** Project Settings → Packaging: pick platforms,
   maps to cook (include `TitleMap` and your gameplay maps), and
   build configuration. No packaging settings ship with the
   template.

## 3. What not to add

- **No GameInstance god-object growth.** Subsystems own domains:
  settings in `UShellSettingsSubsystem`, saves in
  `UShellSaveLibrary`. The GameInstance is a flow state machine.
  Reason: the moment flow, storage, and rules share one class,
  every change touches everything and nothing is testable alone.
- **No unversioned SaveGame subclasses for game saves.** The
  envelope (`UShellSaveGame`) or nothing. Reason: migration is
  impossible to retrofit; a version field you add "later" cannot
  describe saves already written without it.
- **No settings without default + clamp.** Every `FShellSettings`
  field is born with both, enforced in `Validate()` with real
  control flow (never `assert` alone — asserts vanish in release).
  Reason: settings files are untrusted input, including your own
  older ones.
- **No Blueprint-only save logic.** Blueprints call
  `UShellSaveLibrary`; they do not call `UGameplayStatics`
  save functions directly for game saves. Reason: the library is
  the contract — version checks, quarantine, and slot validation
  exist exactly once, or they effectively exist nowhere.
- **No editor-asset references hardcoded in C++.** No
  `ConstructorHelpers` paths to `/Game/...` assets in the shell.
  Assets reach C++ through config, class properties assigned in
  the Editor (the SoundClass pattern), or soft references the game
  layer resolves. Reason: hardcoded paths break silently on
  rename/move and make the template fork-hostile.
- **No fabricated binary assets.** `.uasset` / `.umap` files are
  created in the Editor, never written as text or synthesized.
  Reason: a synthesized asset is corrupt by construction.

## 4. Gates

All gates are **NOT RUN** for this delivery (no UE install was
used). Commands are given in the form to run at first bring-up;
judge Unreal builds by the log and the produced binaries, never by
exit code alone.

### Static gates (runnable anywhere)

```bash
# .uproject is JSON: (RUN for this delivery — passes)
python3 -m json.tool GameShell.uproject > /dev/null
```

### Build gate (first editor bring-up, NOT RUN)

```bash
# Editor target via UnrealBuildTool. <Engine> is the UE 5.8 root.
# Windows:
<Engine>/Engine/Build/BatchFiles/Build.bat GameShellEditor Win64 Development -Project="<path>/GameShell.uproject" -WaitMutex
# Linux:
<Engine>/Engine/Build/BatchFiles/Linux/Build.sh GameShellEditor Linux Development -Project="<path>/GameShell.uproject" -WaitMutex

# Package smoke via RunUAT (after the §2 editor steps):
<Engine>/Engine/Build/BatchFiles/RunUAT.sh BuildCookRun -project="<path>/GameShell.uproject" -noP4 -platform=<Platform> -cook -build -stage -pak -archive -archivedirectory=<out>
```

Gate: build log shows 0 errors and the editor target binary
exists. Warnings introduced by template code are fixed, not
waived.

### LSP / theodolite status (honest)

- **C++ (`.h` / `.cpp`): NOT-LSP-VALIDATED.** The estate
  theodolite bridge has **no C++ route**, so per estate rule these
  files are named here as not-LSP-validated rather than silently
  skipped. The in-editor surface is **clangd** (Diver's
  `lsp/clangd_ls` wiring, diagnostics + `lldb-dap` for debugging),
  and the real gate is the UBT build above.
- **`GameShell.Build.cs` (C#):** routes to the bridge's C# server
  (Roslyn, `csharp-language-server`) where the bridge is deployed;
  not run for this delivery.
- **`.ini` / `.uproject`:** no bridge route; the `.uproject` is
  covered by the `json.tool` gate above, the `.ini` files by
  review and by the Editor loading them at bring-up.
- **Formatter:** clang-format in UE style (tabs, braces on their
  own lines, 120-column soft limit as authored). This template was
  hand-formatted to that style; run clang-format with the project's
  UE `.clang-format` once one exists and treat the diff as review
  input, not gospel. `GameShell.Build.cs` formats with CSharpier
  where available.

### Test split — run at first editor bring-up (all NOT RUN)

Validation (the flow working as designed):

1. First launch with no save directory → settings slot is created
   on the first change with defaults; log shows "using defaults".
2. Save from pause → `auto` slot exists; Continue on the title
   screen lights up; Continue loads the save and `OnSaveLoaded`
   receives the same payload that was written (round trip).
3. Change a volume and the resolution → quit → relaunch → both
   persist; the resolution is actually applied to the window.

Adversarial (the shell under attack by its own files):

1. Delete `ShellSettings.sav` → defaults regenerate, no crash, no
   error spam beyond the "no settings slot" log line.
2. Hand-corrupt a slot `.sav` (overwrite with random bytes) →
   load treats the slot as empty, a `<slot>_corrupt.sav` copy
   appears in the same directory, a warning is logged.
3. Craft a save with `FormatVersion = 99` (save once, hex-edit is
   not viable on the binary format — instead write it from a debug
   Blueprint/console command calling the library internals, or a
   scratch C++ test) → load refuses it, logs the refusal, and the
   file is left in place un-quarantined.
4. Write a settings save, then corrupt one volume to NaN via a
   scratch writer → next launch quarantines the settings file and
   boots on defaults; separately, set a volume to 7.5 in the
   widget → `Validate()` clamps it to 1.0 everywhere it is read.

## 5. Neovim workflow

- **C++:** Diver's clangd (`lsp/clangd_ls`) for diagnostics,
  navigation, and code actions; `lldb-dap` for debugging the
  editor target. clangd needs a `compile_commands.json` — generate
  it from the Editor (Project → Generate Clang Database) or UBT
  (`-GenerateClangDatabase`) after the first successful build.
- **Build.cs:** the C# route (Roslyn language server,
  `csharp-language-server`) per the estate bridge.
- **Terminal build:** the UBT command in GATES, run from the
  project directory; read the log tail in the terminal, not just
  the exit status.
- **Gotchas:**
  - `.uasset` / `.umap` are binary. Never text-edit, never diff,
    never let a formatter near them.
  - `*.generated.h` and `*.gen.cpp` under `Intermediate/` are build
    output from UHT. Never edit them; fix the header they came
    from. A missing `.generated.h` include is always the *last*
    include in its header (UHT requirement — the template follows
    it everywhere).
  - Live Coding is for iteration on function bodies; any UCLASS /
    UPROPERTY / UFUNCTION shape change needs a full editor-target
    rebuild with the Editor closed. Do not debug a "my property
    does not show up" problem before checking this.
  - Opening the project runs project content (Editor startup,
    plugins). Treat an unfamiliar `.uproject` as executable:
    work on a copy, and for studied games stay static-only.
