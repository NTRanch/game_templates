//! Player settings: defaults for everything, clamping, and a
//! forgiving on-disk format.
//!
//! Invariants:
//!
//! - Every field has a default, and the struct deserializes with
//!   `#[serde(default)]`, so a settings file missing fields *repairs*
//!   instead of failing.
//! - [`Settings::validate`] is applied to every value that leaves this
//!   module, whether it came from disk or from the game's UI.
//! - Loading never panics and never errors on bad *content*: a missing
//!   file yields defaults, and an unparseable or wrong-format file is
//!   quarantined (renamed to `<name>.corrupt`, best-effort) and also
//!   yields defaults. Only genuine I/O failures are errors.
//! - Saving is atomic: write `<path>.tmp`, then rename over `<path>`.
//!   Parent directories are created as needed.

use std::fs;
use std::path::{Path, PathBuf};

use serde::{Deserialize, Serialize};

use crate::error::{ShellError, ShellResult};

/// On-disk format marker for settings files. Bump the suffix only when
/// the *meaning* of the file changes; additive fields are absorbed by
/// serde defaults and need no bump.
pub const SETTINGS_FORMAT: &str = "ntranch-shell-settings/1";

/// Smallest accepted volume.
pub const MIN_VOLUME: f32 = 0.0;
/// Largest accepted volume.
pub const MAX_VOLUME: f32 = 1.0;
/// Default for every volume field (and the repair value for NaN).
pub const DEFAULT_VOLUME: f32 = 1.0;
/// Smallest accepted screen-shake multiplier.
pub const MIN_SCREEN_SHAKE: f32 = 0.0;
/// Largest accepted screen-shake multiplier.
pub const MAX_SCREEN_SHAKE: f32 = 1.0;
/// Default screen-shake multiplier (and the repair value for NaN).
pub const DEFAULT_SCREEN_SHAKE: f32 = 1.0;

/// Smallest accepted window width, in pixels.
pub const MIN_WINDOW_WIDTH: u32 = 640;
/// Largest accepted window width, in pixels.
pub const MAX_WINDOW_WIDTH: u32 = 7680;
/// Default window width, in pixels.
pub const DEFAULT_WINDOW_WIDTH: u32 = 1280;
/// Smallest accepted window height, in pixels.
pub const MIN_WINDOW_HEIGHT: u32 = 360;
/// Largest accepted window height, in pixels.
pub const MAX_WINDOW_HEIGHT: u32 = 4320;
/// Default window height, in pixels.
pub const DEFAULT_WINDOW_HEIGHT: u32 = 720;

/// Player settings. All fields are public: this is a plain data record
/// owned by the game, validated by [`Settings::validate`].
#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
#[serde(default)]
pub struct Settings {
    /// Master output volume, `MIN_VOLUME..=MAX_VOLUME`.
    pub master_volume: f32,
    /// Music volume, `MIN_VOLUME..=MAX_VOLUME`.
    pub music_volume: f32,
    /// Sound-effects volume, `MIN_VOLUME..=MAX_VOLUME`.
    pub sfx_volume: f32,
    /// Start in fullscreen.
    pub fullscreen: bool,
    /// Window width in pixels, `MIN_WINDOW_WIDTH..=MAX_WINDOW_WIDTH`.
    pub window_width: u32,
    /// Window height in pixels, `MIN_WINDOW_HEIGHT..=MAX_WINDOW_HEIGHT`.
    pub window_height: u32,
    /// Vertical sync enabled.
    pub vsync: bool,
    /// Accessibility: suppress flashing effects.
    pub reduce_flashing: bool,
    /// Screen-shake multiplier, `MIN_SCREEN_SHAKE..=MAX_SCREEN_SHAKE`.
    /// `0.0` disables shake entirely.
    pub screen_shake: f32,
    /// Show subtitles for voiced lines.
    pub subtitles: bool,
}

impl Default for Settings {
    fn default() -> Self {
        Settings {
            master_volume: DEFAULT_VOLUME,
            music_volume: DEFAULT_VOLUME,
            sfx_volume: DEFAULT_VOLUME,
            fullscreen: false,
            window_width: DEFAULT_WINDOW_WIDTH,
            window_height: DEFAULT_WINDOW_HEIGHT,
            vsync: true,
            reduce_flashing: false,
            screen_shake: DEFAULT_SCREEN_SHAKE,
            subtitles: true,
        }
    }
}

/// The serialized settings file: a format marker plus the settings.
/// The marker is a string (not a number) because settings files in the
/// estate predate the numeric-envelope pattern used by saves; see
/// `save.rs` for the other half of that history.
#[derive(Debug, Serialize, Deserialize)]
struct SettingsFile {
    format: String,
    settings: Settings,
}

impl Settings {
    /// Settings with every field at its default.
    pub fn new() -> Self {
        Self::default()
    }

    /// Clamp every field into its accepted range, in place.
    ///
    /// Non-finite floats (NaN, ±∞ — reachable from a hand-edited or
    /// corrupt file, since RON round-trips them) are repaired to that
    /// field's default: NaN comparisons are always false, so a plain
    /// `clamp` cannot be trusted to fix them; the finiteness check is
    /// explicit for exactly that reason.
    pub fn validate(&mut self) {
        self.master_volume = clamp_volume(self.master_volume);
        self.music_volume = clamp_volume(self.music_volume);
        self.sfx_volume = clamp_volume(self.sfx_volume);
        self.screen_shake = clamp_shake(self.screen_shake);
        self.window_width = self.window_width.clamp(MIN_WINDOW_WIDTH, MAX_WINDOW_WIDTH);
        self.window_height = self
            .window_height
            .clamp(MIN_WINDOW_HEIGHT, MAX_WINDOW_HEIGHT);
    }

    /// Load settings from `path`. See the module docs for the full
    /// contract: missing → defaults, corrupt/wrong-format →
    /// quarantine + defaults, other I/O failure → `Err`.
    pub fn load(path: &Path) -> ShellResult<Settings> {
        load(path)
    }

    /// Save these settings to `path`, atomically. A validated copy is
    /// written, so the file on disk always satisfies the clamps.
    pub fn save(&self, path: &Path) -> ShellResult<()> {
        save(path, self)
    }
}

fn clamp_volume(value: f32) -> f32 {
    if !value.is_finite() {
        DEFAULT_VOLUME
    } else {
        value.clamp(MIN_VOLUME, MAX_VOLUME)
    }
}

fn clamp_shake(value: f32) -> f32 {
    if !value.is_finite() {
        DEFAULT_SCREEN_SHAKE
    } else {
        value.clamp(MIN_SCREEN_SHAKE, MAX_SCREEN_SHAKE)
    }
}

/// The path a corrupt file is quarantined to: `<path>.corrupt`.
fn corrupt_path(path: &Path) -> PathBuf {
    let mut name = path.as_os_str().to_owned();
    name.push(".corrupt");
    PathBuf::from(name)
}

/// The path an in-progress atomic write goes to: `<path>.tmp`.
fn temp_path(path: &Path) -> PathBuf {
    let mut name = path.as_os_str().to_owned();
    name.push(".tmp");
    PathBuf::from(name)
}

/// Best-effort quarantine: a failed rename must not mask the recovery
/// (returning defaults), so the result is deliberately discarded.
fn quarantine(path: &Path) {
    let _ = fs::rename(path, corrupt_path(path));
}

/// Load settings from `path`; see [`Settings::load`].
pub fn load(path: &Path) -> ShellResult<Settings> {
    let bytes = match fs::read(path) {
        Ok(bytes) => bytes,
        Err(err) if err.kind() == std::io::ErrorKind::NotFound => {
            return Ok(Settings::default());
        }
        Err(err) => return Err(ShellError::Io(err)),
    };
    let text = match String::from_utf8(bytes) {
        Ok(text) => text,
        Err(_) => {
            quarantine(path);
            return Ok(Settings::default());
        }
    };
    let file = match ron::from_str::<SettingsFile>(&text) {
        Ok(file) => file,
        Err(_) => {
            quarantine(path);
            return Ok(Settings::default());
        }
    };
    if file.format != SETTINGS_FORMAT {
        quarantine(path);
        return Ok(Settings::default());
    }
    let mut settings = file.settings;
    settings.validate();
    Ok(settings)
}

/// Save `settings` to `path`, atomically; see [`Settings::save`].
pub fn save(path: &Path, settings: &Settings) -> ShellResult<()> {
    let mut validated = settings.clone();
    validated.validate();
    let file = SettingsFile {
        format: SETTINGS_FORMAT.to_owned(),
        settings: validated,
    };
    let text = ron::ser::to_string(&file).map_err(ShellError::RonSerialize)?;
    if let Some(parent) = path.parent() {
        if !parent.as_os_str().is_empty() {
            fs::create_dir_all(parent)?;
        }
    }
    let tmp = temp_path(path);
    fs::write(&tmp, text)?;
    fs::rename(&tmp, path)?;
    Ok(())
}
