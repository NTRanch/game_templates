# AGENTS — working on the Unity Game Shell template

Read `SPEC.md` first. Facts about this template come from `SPEC.md`
and `SKILLS.md`, not from memory or from another engine's template:
the six engine templates share a shell contract, but the details
that bind are the ones written in this directory.

## Gates

- **Validation gate:** every edited `.cs` file passes the
  theodolite bridge route with server id `csharp-language-server`
  at **zero error diagnostics** before the file is called done.
- **Behaviour gate:** the in-editor smoke from `SPEC.md` §4.1 must
  pass in a real Unity project before menu or persistence work is
  called verified.
- **If no editor is available, the work is labelled static** —
  "static, LSP-validated" is an honest result; "verified" without
  the smoke is not. Never claim a compile, a Play run, or a drill
  that did not happen.
- **Formatter:** CSharpier. One formatter only; a formatting diff
  after import is fixed by running it, not debated.

## Rules that do not bend

- **Envelope rules:** saves always carry `format_version`; writes
  are atomic (`.tmp` then replace); corrupt files are quarantined
  to `.corrupt`, never silently deleted or overwritten in place; a
  newer `format_version` is refused, not quarantined. Settings are
  file JSON — never `PlayerPrefs`.
- **Settings rule:** a new setting lands in all three places
  (`SettingsData` + `Validate`, `SettingsManager` setter/apply,
  `SettingsUI` control) in the same change, with a default and a
  clamp.
- **Expected failures return values,** they do not throw: missing,
  corrupt, and refused saves are `bool` results with a warning.
- **`.meta` discipline:** `.meta` files are engine-owned. Never
  hand-edit one, never invent a GUID. Let the editor generate them
  on import and commit them once generated. `Library/` is
  generated and git-ignored.
- **No `UnityEditor` APIs** in the runtime scripts under
  `Assets/GameShell/Scripts`.
- **Top refusals** (full list with reasons in `SPEC.md` §3): no
  `PlayerPrefs` for saves/settings; no unversioned envelopes; no
  setting without default + clamp; no `DontDestroyOnLoad` beyond
  the two managers; no exceptions as control flow for expected
  failures; no `JsonUtility` for dictionary-shaped game state.

## Credit and language

Author and credit lines read "Nice Thoughts Ranch". No personal
names in any artifact. English only.
