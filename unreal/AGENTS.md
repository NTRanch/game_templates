# AGENTS.md — working in unreal/

Read `SPEC.md` first; it is the contract. `SKILLS.md` is the tool
map. Repo-wide law is in `../AGENTS.md`; where it and this file
disagree on Unreal files, this file wins.

- **Editor-pending honesty.** Maps, UMG layouts, and SoundClass
  assets are binary Editor artifacts. This template ships none and
  claims none. Never claim the template was compiled, opened, or
  run without a build log or run record in hand — its delivered
  status is STATIC (text-authored).
- **Validation gates.** C++ here is **not-LSP-validated** via
  theodolite: the bridge has no C++ route, and that fact is stated,
  never skipped over. The real gates are clangd in-editor (Diver's
  clangd wiring) plus a UBT editor-target build with 0 errors in
  the log. `GameShell.Build.cs` routes to the bridge C# server
  (Roslyn / `csharp-language-server`). `.uproject` is gated by
  `python3 -m json.tool`. Formatters: clang-format (UE style) for
  C++, CSharpier for Build.cs.
- **Envelope rules.** Game saves are `UShellSaveGame` envelopes
  through `UShellSaveLibrary` only — no unversioned SaveGame
  subclasses, no Blueprint-side direct `UGameplayStatics` saves.
  Settings are `FShellSettings` fields with default + clamp, a
  subsystem setter, and a widget control — all three, or it is not
  a setting.
- **Binary-asset discipline.** Never text-edit, synthesize, or
  fabricate `.uasset` / `.umap` files; never edit
  `*.generated.h` / `*.gen.cpp` (build output). Assets are created
  in the Editor; live-editor work by agents goes through Seahawk,
  never raw GUI automation.
- **No hardcoded asset paths in C++.** Editor assets reach code
  through config or Editor-assigned class properties (the
  SoundClass pattern), never `ConstructorHelpers` path strings.
- **Refusals, condensed.** No GameInstance god-object growth; no
  silent failures (every failure path logs through
  `LogGameShell`); no weakening of the shared contract (defaults +
  clamps, versioned envelopes, quarantine) without an Owner ruling;
  no personal names — credit is "Nice Thoughts Ranch"; English
  only.
