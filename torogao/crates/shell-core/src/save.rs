//! Versioned save files in named slots.
//!
//! Invariants:
//!
//! - A save file is a `SaveFile` wrapper: a string format marker
//!   plus a [`SaveEnvelope`]. The envelope carries the numeric
//!   `format_version`; the marker identifies *which kind of file* it
//!   is, the version drives migration. Both halves are checked.
//! - Writes are atomic (temp file + rename) and create the saves
//!   directory on demand.
//! - Corrupt or wrong-marker files are quarantined (renamed to
//!   `<name>.corrupt`, best-effort) and read as an empty slot. A file
//!   from a *newer* format version is the opposite case: it is refused
//!   with [`ShellError::UnsupportedVersion`] and left untouched,
//!   because the data is probably fine — this build is just too old.
//!   Data-level refusal is an error; the refusal *presentation* is the
//!   game's problem.
//! - This module never reads the clock. `saved_at_unix` is supplied by
//!   the caller, which keeps the core deterministic and testable.
//! - Slot names are untrusted input. Only the canonical
//!   [`SLOT_NAMES`] and test slots prefixed `__` are accepted; anything
//!   else — above all anything containing a path separator — fails
//!   with [`ShellError::InvalidSlot`] before any path is built, so a
//!   slot can never escape the saves directory.

use std::fs;
use std::path::{Path, PathBuf};

use serde::{Deserialize, Serialize};

use crate::error::{ShellError, ShellResult};

/// On-disk format marker for save files.
pub const SAVE_FORMAT: &str = "ntranch-shell-save/1";

/// The highest save `format_version` this build can read and write.
pub const CURRENT_FORMAT_VERSION: u32 = 1;

/// The canonical player-facing slots. [`SaveStore::slots`] reports
/// present slots in this order.
pub const SLOT_NAMES: [&str; 4] = ["slot1", "slot2", "slot3", "auto"];

/// Prefix marking a test slot. Test slots (for example `__probe`) are
/// accepted alongside the canonical names so tests and tooling can
/// write saves without touching player slots; they are never listed by
/// [`SaveStore::slots`].
pub const TEST_SLOT_PREFIX: &str = "__";

/// One save: bookkeeping plus the game's payload.
///
/// The payload is an untyped [`ron::Value`] in the template. A real
/// game replaces it with its own typed, `Serialize`/`Deserialize`
/// state struct — see the template `SPEC.md`.
#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
pub struct SaveEnvelope {
    /// Envelope format version; see [`CURRENT_FORMAT_VERSION`] and
    /// [`migrate`].
    pub format_version: u32,
    /// When the save was written, seconds since the Unix epoch.
    /// Supplied by the caller; the core never reads the clock.
    pub saved_at_unix: i64,
    /// Playtime at save time, in seconds.
    pub playtime_seconds: i64,
    /// Player- or game-supplied label shown in slot pickers.
    ///
    /// The `serde(default)` is the v0→v1 migration story: v0 envelopes
    /// had no label, and a missing label deserializes as empty instead
    /// of failing.
    #[serde(default)]
    pub label: String,
    /// The game's save payload.
    pub payload: ron::Value,
}

/// Just the bookkeeping half of an envelope, for slot pickers that
/// should not have to hold the payload.
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct SaveMetadata {
    /// Envelope format version.
    pub format_version: u32,
    /// When the save was written, seconds since the Unix epoch.
    pub saved_at_unix: i64,
    /// Playtime at save time, in seconds.
    pub playtime_seconds: i64,
    /// Player- or game-supplied label.
    pub label: String,
}

/// The serialized save file: format marker plus envelope.
#[derive(Debug, Serialize, Deserialize)]
struct SaveFile {
    format: String,
    envelope: SaveEnvelope,
}

/// Is `slot` acceptable? Exact canonical match, or a `__`-prefixed
/// test slot. Everything else is invalid.
pub fn is_valid_slot(slot: &str) -> bool {
    SLOT_NAMES.contains(&slot) || slot.starts_with(TEST_SLOT_PREFIX)
}

fn validate_slot(slot: &str) -> ShellResult<()> {
    if is_valid_slot(slot) {
        Ok(())
    } else {
        Err(ShellError::InvalidSlot(slot.to_owned()))
    }
}

fn corrupt_path(path: &Path) -> PathBuf {
    let mut name = path.as_os_str().to_owned();
    name.push(".corrupt");
    PathBuf::from(name)
}

fn temp_path(path: &Path) -> PathBuf {
    let mut name = path.as_os_str().to_owned();
    name.push(".tmp");
    PathBuf::from(name)
}

/// Best-effort quarantine: a failed rename must not mask the recovery
/// (reading the slot as empty), so the result is deliberately
/// discarded.
fn quarantine(path: &Path) {
    let _ = fs::rename(path, corrupt_path(path));
}

/// Bring an envelope up to [`CURRENT_FORMAT_VERSION`], step by step.
///
/// Rule (additive-fields-first): prefer `#[serde(default)]` on new
/// fields over a version bump — a field that deserializes when missing
/// needs no migration step at all. Bump the version, and add a step
/// here, only when the *meaning* of existing data changes.
///
/// An envelope newer than current is refused by the caller before
/// migration is attempted; an envelope with no step from its version
/// is refused here.
pub fn migrate(envelope: SaveEnvelope) -> ShellResult<SaveEnvelope> {
    let mut envelope = envelope;
    while envelope.format_version < CURRENT_FORMAT_VERSION {
        envelope = match envelope.format_version {
            0 => migrate_v0_to_v1(envelope),
            other => {
                return Err(ShellError::UnsupportedVersion {
                    found: other,
                    current: CURRENT_FORMAT_VERSION,
                });
            }
        };
    }
    Ok(envelope)
}

/// v0 → v1: v1 added the `label` field. Because `label` carries a
/// serde default, a v0 payload already deserialized with an empty
/// label by the time this step runs; the step itself only advances
/// the version stamp. It exists — and is exercised by tests — so the
/// migration chain has a real first link to copy when v2 arrives.
fn migrate_v0_to_v1(mut envelope: SaveEnvelope) -> SaveEnvelope {
    envelope.format_version = 1;
    envelope
}

/// A directory of save slots.
#[derive(Debug, Clone)]
pub struct SaveStore {
    dir: PathBuf,
}

impl SaveStore {
    /// A store rooted at `dir`. The directory is created lazily, on
    /// the first save.
    pub fn new(dir: PathBuf) -> Self {
        SaveStore { dir }
    }

    /// The directory this store is rooted at.
    pub fn dir(&self) -> &Path {
        &self.dir
    }

    fn slot_path(&self, slot: &str) -> PathBuf {
        // Only called after `validate_slot`, so `slot` is a canonical
        // or `__`-prefixed name and the join cannot escape `dir`.
        self.dir.join(format!("{slot}.ron"))
    }

    /// Write `payload` into `slot`, atomically, stamped with the
    /// caller-supplied `saved_at_unix` and `playtime_seconds`.
    pub fn save(
        &self,
        slot: &str,
        payload: ron::Value,
        label: &str,
        playtime_seconds: i64,
        saved_at_unix: i64,
    ) -> ShellResult<()> {
        validate_slot(slot)?;
        let envelope = SaveEnvelope {
            format_version: CURRENT_FORMAT_VERSION,
            saved_at_unix,
            playtime_seconds,
            label: label.to_owned(),
            payload,
        };
        let file = SaveFile {
            format: SAVE_FORMAT.to_owned(),
            envelope,
        };
        let text = ron::ser::to_string(&file).map_err(ShellError::RonSerialize)?;
        fs::create_dir_all(&self.dir)?;
        let path = self.slot_path(slot);
        let tmp = temp_path(&path);
        fs::write(&tmp, text)?;
        fs::rename(&tmp, &path)?;
        Ok(())
    }

    /// Read `slot`.
    ///
    /// - Empty slot → `Ok(None)`.
    /// - Corrupt or wrong-marker file → quarantine, then `Ok(None)`.
    /// - Older envelope → migrated, then `Ok(Some(..))`.
    /// - Newer envelope → `Err(UnsupportedVersion)`, file untouched.
    /// - Invalid slot name → `Err(InvalidSlot)`.
    pub fn load(&self, slot: &str) -> ShellResult<Option<SaveEnvelope>> {
        validate_slot(slot)?;
        let path = self.slot_path(slot);
        let bytes = match fs::read(&path) {
            Ok(bytes) => bytes,
            Err(err) if err.kind() == std::io::ErrorKind::NotFound => return Ok(None),
            Err(err) => return Err(ShellError::Io(err)),
        };
        let text = match String::from_utf8(bytes) {
            Ok(text) => text,
            Err(_) => {
                quarantine(&path);
                return Ok(None);
            }
        };
        let file = match ron::from_str::<SaveFile>(&text) {
            Ok(file) => file,
            Err(_) => {
                quarantine(&path);
                return Ok(None);
            }
        };
        if file.format != SAVE_FORMAT {
            quarantine(&path);
            return Ok(None);
        }
        if file.envelope.format_version > CURRENT_FORMAT_VERSION {
            return Err(ShellError::UnsupportedVersion {
                found: file.envelope.format_version,
                current: CURRENT_FORMAT_VERSION,
            });
        }
        let envelope = migrate(file.envelope)?;
        Ok(Some(envelope))
    }

    /// Does `slot` currently hold a save file? Invalid slot names
    /// report `false` rather than erroring: this is a predicate for UI
    /// code, and an untrusted name simply names nothing.
    pub fn exists(&self, slot: &str) -> bool {
        if !is_valid_slot(slot) {
            return false;
        }
        self.slot_path(slot).exists()
    }

    /// The bookkeeping for `slot`, without the payload, or `None` for
    /// an empty (or quarantined-corrupt) slot.
    pub fn metadata(&self, slot: &str) -> ShellResult<Option<SaveMetadata>> {
        match self.load(slot)? {
            Some(envelope) => Ok(Some(SaveMetadata {
                format_version: envelope.format_version,
                saved_at_unix: envelope.saved_at_unix,
                playtime_seconds: envelope.playtime_seconds,
                label: envelope.label,
            })),
            None => Ok(None),
        }
    }

    /// Remove `slot`'s save. Deleting an empty slot is fine.
    pub fn delete(&self, slot: &str) -> ShellResult<()> {
        validate_slot(slot)?;
        let path = self.slot_path(slot);
        match fs::remove_file(&path) {
            Ok(()) => Ok(()),
            Err(err) if err.kind() == std::io::ErrorKind::NotFound => Ok(()),
            Err(err) => Err(ShellError::Io(err)),
        }
    }

    /// The canonical slots that currently hold a save, in
    /// [`SLOT_NAMES`] order. Test slots are never listed.
    pub fn slots(&self) -> Vec<String> {
        let mut present = Vec::new();
        for name in SLOT_NAMES {
            if self.slot_path(name).exists() {
                present.push(name.to_owned());
            }
        }
        present
    }
}
