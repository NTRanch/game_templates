# Torogao Game Shell — Template

By Nice Thoughts Ranch. Licensed Apache-2.0.

This is the shell starter for a new Torogao game: the menu, settings,
and save layer every game needs before the engine starts. The full
engine — rendering, session hosting, the works — lives in the estate's
own `torogao-engine` and `torogao-session` crates. This template is
the part a new game clones and grows: settings with defaults and
clamps, versioned save envelopes in named slots, and a pure screen
flow state machine (age gate → title → game ⇄ pause, settings as an
overlay).

If you are starting a game here, read `SPEC.md` first. It is the
contract; this file is only the map.

## Layout

```text
Cargo.toml                  workspace (resolver 2, edition 2021)
crates/
  shell-core/               the engine-agnostic shell core (library)
    src/lib.rs              crate docs, contract summary, re-exports
    src/error.rs            ShellError / ShellResult (hand-rolled)
    src/settings.rs         Settings: defaults, validate(), atomic
                            load/save, corrupt-file quarantine
    src/save.rs             SaveStore: versioned envelopes, slots,
                            migration chain, quarantine
    src/shell.rs            Shell: the pure screen state machine
    tests/roundtrip.rs      integration tests, half validation /
                            half adversarial
  shell-app/                headless demo binary (the smoke test)
    src/main.rs             scripted end-to-end walk of the flow
docs/
  bloch-lesson-template.md  the lesson record format (see SKILLS.md)
book.toml, book/            the mdBook for this template
SPEC.md                     the contract: manifest, growth, gates
SKILLS.md                   the tool and skill map for work here
AGENTS.md                   working rules for agents in this template
```

## Run the demo

```sh
cargo run -p shell-app            # optionally: -- <work-dir>
cargo test                        # the roundtrip suite
```

The demo loads settings, persists a change, writes a demo save, and
walks the shell flow (boot → new game → pause → settings from pause →
resume → title → continue). A good run ends with:

```text
OK shell-app
```

## Serialization

All on-disk state is RON, the canonical serialization form across the
estate — the same discipline `torogao-session` uses for its state
files (`torogao-editor-state/1`-style versioned formats). Settings
files carry a string format marker (`ntranch-shell-settings/1`); save
files carry a marker (`ntranch-shell-save/1`) plus a numeric envelope
`format_version` that drives migration. There is no JSON state, no
unversioned state, and no second format to keep in sync.

## Verification status

Verified at delivery on primo: `cargo build`, `cargo test` (13/13),
`cargo clippy -- -D warnings`, `cargo fmt --check`, and the
`shell-app` smoke run — see the verification table in the repo
README. Theodolite at delivery: `bacon-ls` zero diagnostics on all
seven `.rs` files, `taplo` zero on all three manifests. The standing gates for any change here are listed in
`SPEC.md` (build, test, clippy with warnings denied, rustfmt check,
the smoke run, and theodolite diagnostics at zero errors).
