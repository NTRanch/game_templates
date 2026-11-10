# Unity Game Shell — Nice Thoughts Ranch

A drop-in game shell for Unity: settings with persistence, four
versioned save slots, a title / pause / settings menu flow, and an
optional age gate. Namespace `NiceThoughtsRanch.GameShell`, no
external packages, legacy `UnityEngine.UI` controls only.

Credit: Nice Thoughts Ranch. License: Apache-2.0 (see the repo
`LICENSE` at the repository root).

## Verification status — read this first

**STATIC / template-grade.** This template was **not compiled in a
Unity editor for this delivery**, and no Play-mode run is claimed.
The C# follows the estate C# style guide and is written to compile
under Unity 2022 LTS / Unity 6 with no external packages.

What WAS run at delivery: theodolite diagnostics via the Roslyn
bridge (`csharp-language-server`) over all eleven scripts —
**zero error and zero warning diagnostics**. The remaining
findings are style-level only (Hint/Information: IDE0005, IDE0031,
IDE0044, IDE0300), produced by a project-less analysis; several
IDE0031 "simplify null check" suggestions are deliberately not
applied — explicit null checks are the correct shape for
Unity-object lifetime semantics. Honest label, per `AGENTS.md`:
**static, LSP-validated**.

First-run checklist: import the shell, build the scenes per the
wiring list in `SPEC.md`, then run the in-editor smoke in `SPEC.md`
(settings round trip, save/load round trip, corrupt-file drill,
version-skew drill) before treating the template as verified in
your project.

## Drop-in summary

1. Copy `Assets/GameShell` into your project's `Assets` folder.
   Let Unity generate the `.meta` files on import; commit them once
   generated.
2. Create the scenes listed in `SPEC.md` (one Boot scene carrying
   `GameFlow` + `SettingsManager`, the menu canvases, and one
   gameplay scene) and wire the serialized fields by name as the
   SPEC lists them.
3. Register your game's state with the shell from your gameplay
   bootstrap:

   ```csharp
   ShellBridge.RegisterPayloadProvider(
       () => MyGame.SerializeState(),
       () => MyGame.PlaytimeSeconds,
       json => MyGame.RestoreState(json));
   ```

4. Optionally set `GameFlow.AgeGateEnabled = true` before the Boot
   scene loads if the game needs an age gate (off by default).

## Layout

- `Assets/GameShell/Scripts/` — the eleven shell scripts; one
  public type per file, each with its contract in XML doc comments.
- `SPEC.md` — the full contract: file manifest, what to add, what
  not to add, gates, and the Neovim workflow.
- `SKILLS.md` — the tools and skills map, plus the Bloch lane law.
- `AGENTS.md` — working rules for agents editing this template.
- `docs/bloch-lesson-template.md` — the lesson-record template.
- `book.toml` + `book/` — the mdBook shell that renders these docs.

Start with `SPEC.md`; it is the contract everything else points at.
