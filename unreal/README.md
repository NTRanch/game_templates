# Game Shell — Unreal Engine 5.8 Template

A reusable game shell for Unreal Engine 5.8, part of the
`NTRanch/game_templates` collection. Licensed under Apache-2.0 (see
the repository `LICENSE`). Credit: Nice Thoughts Ranch.

The shell gives a new game its frame on day one: persisted settings
with validation, a versioned save-game envelope with corruption
handling, and a title / pause / settings / age-gate flow driven by
C++ widget bases.

> **Verification status: STATIC.** This template is text-authored.
> It has **not** been compiled, opened in the Editor, or run. The
> C++ follows the estate's Tiger Style Unreal/C++ skills and uses
> documented UE5 APIs, but the first compile and the first
> play-in-editor session are bring-up work — SPEC.md's GATES section
> is the checklist, including the test split to run there. Do not
> treat anything in this README as a build result.

## The honest split

### Code-complete (in this directory, as text)

- `FShellSettings` — the shared settings struct: master/music/SFX
  volumes (0..1), fullscreen, window size (640..7680 × 360..4320),
  VSync, reduce-flashing, screen shake (0..1), subtitles. Every
  field has a default and a clamp/repair in `Validate()`.
- `UShellSettingsSubsystem` — loads settings at startup from the
  engine SaveGame slot `ShellSettings`, validates them, persists
  every change, applies display settings for real through
  `UGameUserSettings`, and applies volumes through
  `ApplyAudioVolumes()` when given the SoundClass assets. A slot
  that fails to load (or holds NaN values) is quarantined at the
  byte level to `ShellSettings_corrupt.sav` and defaults are used.
- `UShellSaveGame` + `UShellSaveLibrary` — the save envelope:
  `FormatVersion`, `SavedAtUnix`, `PlaytimeSeconds`, `Label`, and
  the game's state as a JSON string in `PayloadJson`. Canonical
  slots `slot1`..`slot3` and `auto`; step-wise migration (a trivial
  v0→v1 ships); newer-than-build formats are refused; corrupt slots
  are quarantined to `<slot>_corrupt.sav` and treated as empty.
- `UShellGameInstance` — the flow state machine
  (`AgeGate` / `Title` / `Game` / `Paused`), pause via
  `UGameplayStatics::SetGamePaused`, settings open/close routing,
  quit. The age gate is off by default (`bAgeGateEnabled = false`).
- Widget C++ bases — `UShellTitleWidget`, `UShellPauseWidget`,
  `UShellSettingsWidget`, `UShellAgeGateWidget`: all behaviour
  (actions, subsystem calls, save calls, button wiring for the
  documented control names) is implemented in C++.
- `AShellGameMode` — minimal title-map GameMode.

### Editor-pending (cannot exist as text; you create these)

Unreal maps and UMG layouts are binary assets. No honest template
can ship them as text files, so they are editor steps, listed in
full in SPEC.md ("WHAT TO ADD"):

1. **Maps** — `TitleMap` under `Content/Maps/` (the startup/default
   map already named in `Config/DefaultEngine.ini`), with its World
   Settings GameMode override set to `AShellGameMode`; plus one
   gameplay map of your own.
2. **UMG layouts** — four Widget Blueprints parented to the C++
   bases: `WBP_Title`, `WBP_Pause`, `WBP_Settings`, `WBP_AgeGate`.
   Name controls as documented in the base-class headers (for
   example `NewGameButton`, `MasterVolumeSlider`,
   `FullscreenCheckBox`, `ConfirmButton`) and they are wired
   automatically; anything else, wire in the graph to the same
   `BlueprintCallable` actions.
3. **Sound assets** — three Sound Classes (`SC_Master`, `SC_Music`,
   `SC_SFX` or your names) assigned in `WBP_Settings` class
   defaults, so volume settings reach the audio device. An optional
   Sound Mix is not required by the shell.
4. **Packaging settings** — your project's own Project Settings →
   Packaging choices; the template ships none.

Until steps 1–2 are done the project will warn about the missing
startup map. That warning is expected and documented, not a defect
in the code.

## Layout

```
GameShell.uproject          project file (EngineAssociation 5.8)
Config/                     DefaultGame.ini, DefaultEngine.ini
Source/GameShell/           the whole C++ shell (one module)
docs/                       bloch lesson template
book.toml, book/src/        mdBook sources (build: mdbook build unreal)
README.md  SPEC.md  SKILLS.md  AGENTS.md
```

## Where to go next

- **SPEC.md** — the contract: file manifest, the ordered
  editor+code checklist, what not to add and why, gates, and the
  Neovim workflow.
- **SKILLS.md** — the skill and tool map for working on this
  template, including the Bloch lane law.
- **AGENTS.md** — the short rules for any agent working here.
