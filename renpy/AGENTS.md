# AGENTS — Ren'Py Game Shell

Read `SPEC.md` first. It is the contract; this file is the condensed
version. Facts about files, gates, and rules come from `SPEC.md` and
`SKILLS.md` — if this file and SPEC disagree, SPEC wins.

- **Gate:** every touched `.rpy` file passes theodolite with bridge
  server id `renpy-language-server` at zero error diagnostics, with
  the `possibly_incomplete` caveat handled exactly as `SPEC.md` §4
  describes (a possibly-incomplete probe is unproven, never a pass).
  Where a Ren'Py SDK exists, `renpy.sh <project> lint` must also pass.
  Where neither exists, label the work **static** — never claim a gate
  you did not run.
- **Refusals (condensed):** do not reimplement the main menu, game
  menu, or file slots; do not redefine engine screens in estate files;
  do not add a settings key anywhere except the three places in
  SPEC §2.2; do not change the shape of estate-managed data (per-save
  or persistent) without bumping `ESTATE_FORMAT_VERSION` and adding
  the migration step in the same change; do not write
  `persistent.estate_settings` outside `game/estate_settings.rpy`.
- **Envelope rules:** the estate version variables are load-bearing —
  `default estate_save_version` (per save) and
  `persistent.estate_persistent_version` (persistent), checked in
  `label after_load` and at init respectively. Migrations are ordered
  steps, total (never raise), and safe to re-run. A save from a newer
  version takes the refusal path (log + notify, estate payload treated
  as absent), never a guess at a future shape.
- **Rollback reminder:** settings are persistent-level — outside
  rollback, applied game-wide, safe to change mid-game via
  `estate_set`. Story state is saved store variables — inside
  rollback. No rollback-unsafe mutations in `init` blocks. Keep the
  two homes separate.
- **Style:** English only. Python inside `.rpy` follows the
  tiger-style-python discipline (see `SKILLS.md`). No formatter is
  catalogued for `.rpy`; hold 4-space indentation by hand. Credit
  "Nice Thoughts Ranch"; license Apache-2.0; no personal names in
  repo artifacts.
