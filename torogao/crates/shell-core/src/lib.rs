//! # shell-core — the Torogao game shell, engine-agnostic core
//!
//! This crate is the menu / settings / save layer a new Torogao game
//! starts from. It is deliberately *not* the whole engine: the full
//! engine lives in the estate's `torogao-engine` and `torogao-session`
//! crates. What lives here is the shell every game needs before the
//! engine starts:
//!
//! - [`settings`] — player settings with defaults for every field, a
//!   `validate()` clamp pass, a versioned on-disk format, atomic
//!   writes, and corrupt-file quarantine. Loading bad settings data
//!   never fails the boot: worst case the player gets defaults.
//! - [`save`] — versioned save envelopes in named slots. Writes are
//!   atomic (temp file + rename), corrupt files are quarantined and
//!   read as empty slots, and a save from a *newer* format version is
//!   refused with [`ShellError::UnsupportedVersion`] so the game — not
//!   the library — decides the refusal UX. The core never reads the
//!   clock: timestamps are passed in, keeping saves deterministic and
//!   testable.
//! - [`shell`] — the screen flow state machine (age gate → title →
//!   game ⇄ pause, settings as an overlay). Pure and total: no I/O, no
//!   clock, no panics; invalid transitions are ignored.
//!
//! ## Contract summary
//!
//! - `#![forbid(unsafe_code)]` — there is no unsafe code in this crate.
//! - All on-disk data is untrusted input. It is validated with ordinary
//!   control flow (never `assert!` alone) before it is used.
//! - Every bound is a named constant in the owning module.
//! - Serialization is RON, the canonical form across the estate (the
//!   same discipline `torogao-session` uses for its state files).
//!
//! ## How a game grows this
//!
//! Replace the save payload (`ron::Value`) with the game's own typed
//! state struct, register new settings in the three places listed in
//! the template `SPEC.md`, and drive [`Shell`] from the real engine
//! session instead of the scripted `shell-app` demo. The envelope,
//! slot, and quarantine rules are load-bearing — do not weaken them.

#![forbid(unsafe_code)]

pub mod error;
pub mod save;
pub mod settings;
pub mod shell;

pub use error::{ShellError, ShellResult};
pub use save::{SaveEnvelope, SaveMetadata, SaveStore, CURRENT_FORMAT_VERSION, SAVE_FORMAT};
pub use settings::{Settings, SETTINGS_FORMAT};
pub use shell::{Screen, SettingsReturnTo, Shell, ShellAction, ShellEvent};
