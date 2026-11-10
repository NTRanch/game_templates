//! Typed errors for the shell core.
//!
//! Hand-rolled on purpose: the dependency budget for this crate is
//! `serde` + `ron`, so there is no `thiserror` / `anyhow` here. Every
//! expected failure on a data path is one of these variants; programmer
//! errors in tests may still panic.

use std::fmt;

/// Result alias used across the shell core.
pub type ShellResult<T> = Result<T, ShellError>;

/// Every failure the shell core reports to its caller.
#[derive(Debug)]
pub enum ShellError {
    /// An underlying I/O failure (read, write, rename, directory create).
    Io(std::io::Error),
    /// Serializing a settings or save file to RON failed.
    RonSerialize(ron::Error),
    /// Deserializing RON failed at the value level.
    ///
    /// Note: `ron::from_str` reports [`ron::error::SpannedError`]; the
    /// `From` impl below keeps the inner [`ron::Error`] code, which is
    /// what callers match on.
    RonDeserialize(ron::Error),
    /// A save file's `format_version` is newer than this build knows.
    ///
    /// This is a *data-level refusal*: the caller (the game) decides
    /// what refusal UX to show. It is the one load/save failure that is
    /// an error rather than a quarantine, because silently discarding a
    /// newer save would destroy player data.
    UnsupportedVersion {
        /// Version found in the file.
        found: u32,
        /// Highest version this build can read.
        current: u32,
    },
    /// A slot name that is not a canonical slot and not a `__` test
    /// slot. Slot names are untrusted input; anything else — above all
    /// anything containing a path separator — is rejected here so a
    /// slot can never escape the saves directory.
    InvalidSlot(String),
}

impl fmt::Display for ShellError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            ShellError::Io(err) => write!(f, "io error: {err}"),
            ShellError::RonSerialize(err) => write!(f, "ron serialize error: {err}"),
            ShellError::RonDeserialize(err) => write!(f, "ron deserialize error: {err}"),
            ShellError::UnsupportedVersion { found, current } => write!(
                f,
                "unsupported save format version {found} (this build supports up to {current})"
            ),
            ShellError::InvalidSlot(slot) => write!(f, "invalid save slot name: {slot:?}"),
        }
    }
}

impl std::error::Error for ShellError {
    fn source(&self) -> Option<&(dyn std::error::Error + 'static)> {
        match self {
            ShellError::Io(err) => Some(err),
            ShellError::RonSerialize(err) => Some(err),
            ShellError::RonDeserialize(err) => Some(err),
            ShellError::UnsupportedVersion { .. } => None,
            ShellError::InvalidSlot(_) => None,
        }
    }
}

impl From<std::io::Error> for ShellError {
    fn from(err: std::io::Error) -> Self {
        ShellError::Io(err)
    }
}

impl From<ron::error::SpannedError> for ShellError {
    fn from(err: ron::error::SpannedError) -> Self {
        ShellError::RonDeserialize(err.code)
    }
}
