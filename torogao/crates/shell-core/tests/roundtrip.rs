//! Integration tests for shell-core.
//!
//! Half validation (the happy paths a game depends on), half
//! adversarial (corrupt files, hostile slot names, future versions,
//! NaN floats) — save and settings data is untrusted input, and the
//! adversarial half is what proves the quarantine and validation
//! contracts instead of just asserting them.
//!
//! Temp directories are hand-rolled from `std::env::temp_dir()` plus
//! the process id and a per-test tag: no dev-dependencies.

#![forbid(unsafe_code)]

use std::fs;
use std::path::{Path, PathBuf};

use shell_core::save::migrate;
use shell_core::{
    SaveEnvelope, SaveStore, Screen, Settings, SettingsReturnTo, Shell, ShellAction, ShellError,
    ShellEvent,
};

// ---------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------

fn test_dir(tag: &str) -> PathBuf {
    let dir = std::env::temp_dir().join(format!("ntranch-shell-test-{}-{tag}", std::process::id()));
    let _ = fs::remove_dir_all(&dir);
    fs::create_dir_all(&dir).expect("create test dir");
    dir
}

fn cleanup(dir: &Path) {
    let _ = fs::remove_dir_all(dir);
}

/// The path `path` is quarantined to: `<path>.corrupt`.
fn corrupt_path(path: &Path) -> PathBuf {
    let mut name = path.as_os_str().to_owned();
    name.push(".corrupt");
    PathBuf::from(name)
}

fn demo_payload() -> ron::Value {
    let mut map = ron::value::Map::new();
    map.insert(
        ron::Value::String("checkpoint".to_owned()),
        ron::Value::String("start".to_owned()),
    );
    map.insert(
        ron::Value::String("hardcore".to_owned()),
        ron::Value::Bool(false),
    );
    ron::Value::Map(map)
}

// ---------------------------------------------------------------------
// Validation: settings
// ---------------------------------------------------------------------

#[test]
fn settings_round_trip() {
    let dir = test_dir("settings-round-trip");
    let path = dir.join("nested").join("settings.ron");

    let mut settings = Settings::new();
    settings.music_volume = 0.5;
    settings.fullscreen = true;
    settings.window_width = 1920;
    settings.subtitles = false;
    settings.save(&path).expect("save settings");

    let loaded = Settings::load(&path).expect("load settings");
    assert_eq!(loaded, settings);
    cleanup(&dir);
}

#[test]
fn settings_defaults_on_missing_file() {
    let dir = test_dir("settings-missing");
    let path = dir.join("never-written.ron");

    let loaded = Settings::load(&path).expect("load missing settings");
    assert_eq!(loaded, Settings::default());
    cleanup(&dir);
}

// ---------------------------------------------------------------------
// Validation: saves
// ---------------------------------------------------------------------

#[test]
fn save_load_round_trip() {
    let dir = test_dir("save-round-trip");
    let store = SaveStore::new(dir.join("saves"));

    store
        .save("slot1", demo_payload(), "Demo start", 120, 1_700_000_000)
        .expect("save slot1");

    let envelope = store
        .load("slot1")
        .expect("load slot1")
        .expect("slot1 is present");
    assert_eq!(envelope.payload, demo_payload());
    assert_eq!(envelope.label, "Demo start");
    assert_eq!(envelope.playtime_seconds, 120);
    assert_eq!(envelope.saved_at_unix, 1_700_000_000);
    assert_eq!(envelope.format_version, shell_core::CURRENT_FORMAT_VERSION);
    assert_eq!(store.slots(), vec!["slot1".to_owned()]);
    cleanup(&dir);
}

#[test]
fn save_metadata_read() {
    let dir = test_dir("save-metadata");
    let store = SaveStore::new(dir.join("saves"));

    store
        .save("auto", demo_payload(), "Autosave", 45, 1_700_000_100)
        .expect("save auto");

    let metadata = store
        .metadata("auto")
        .expect("metadata auto")
        .expect("auto is present");
    assert_eq!(metadata.label, "Autosave");
    assert_eq!(metadata.playtime_seconds, 45);
    assert_eq!(metadata.saved_at_unix, 1_700_000_100);
    assert!(store.metadata("slot2").expect("metadata empty").is_none());
    assert!(store.load("slot2").expect("load empty").is_none());
    cleanup(&dir);
}

// ---------------------------------------------------------------------
// Validation: shell flow
// ---------------------------------------------------------------------

#[test]
fn shell_flow_transitions() {
    // Gate disabled: Boot lands on the title screen.
    let mut shell = Shell::new(false);
    assert_eq!(shell.handle(ShellEvent::Boot), ShellAction::ShowTitle);
    assert_eq!(shell.screen(), Screen::Title);

    // Pause is only reachable from the game.
    assert_eq!(shell.handle(ShellEvent::OpenPause), ShellAction::None);
    assert_eq!(shell.screen(), Screen::Title);

    // Settings from title: the caller is recorded as Title.
    assert_eq!(
        shell.handle(ShellEvent::OpenSettings),
        ShellAction::ShowSettings
    );
    assert_eq!(shell.settings_return_to(), Some(SettingsReturnTo::Title));
    assert_eq!(
        shell.handle(ShellEvent::CloseSettings),
        ShellAction::HideSettings
    );
    assert_eq!(shell.settings_return_to(), None);
    assert_eq!(shell.screen(), Screen::Title);

    // Continue is refused until the shell is told a save exists.
    assert_eq!(shell.handle(ShellEvent::ContinueGame), ShellAction::None);
    shell.set_continue_available(true);
    assert_eq!(
        shell.handle(ShellEvent::ContinueGame),
        ShellAction::StartGame
    );
    assert_eq!(shell.screen(), Screen::Game);

    // Back to title, then a new game, then pause + settings from pause.
    assert_eq!(
        shell.handle(ShellEvent::QuitToTitle),
        ShellAction::ShowTitle
    );
    assert_eq!(shell.handle(ShellEvent::NewGame), ShellAction::StartGame);
    assert_eq!(shell.handle(ShellEvent::OpenPause), ShellAction::None);
    assert_eq!(shell.screen(), Screen::Pause);
    assert_eq!(
        shell.handle(ShellEvent::OpenSettings),
        ShellAction::ShowSettings
    );
    assert_eq!(shell.settings_return_to(), Some(SettingsReturnTo::Pause));
    assert_eq!(
        shell.handle(ShellEvent::CloseSettings),
        ShellAction::HideSettings
    );
    assert_eq!(shell.screen(), Screen::Pause);
    assert_eq!(
        shell.handle(ShellEvent::ClosePause),
        ShellAction::ResumeGame
    );
    assert_eq!(shell.screen(), Screen::Game);

    // Settings cannot be opened straight from gameplay.
    assert_eq!(shell.handle(ShellEvent::OpenSettings), ShellAction::None);

    // The age gate, when enabled: decline quits, confirm titles.
    let mut gated = Shell::new(true);
    assert_eq!(gated.handle(ShellEvent::Boot), ShellAction::ShowAgeGate);
    assert_eq!(gated.screen(), Screen::AgeGate);
    assert_eq!(gated.handle(ShellEvent::AgeDeclined), ShellAction::Quit);

    let mut gated = Shell::new(true);
    assert_eq!(gated.handle(ShellEvent::Boot), ShellAction::ShowAgeGate);
    assert_eq!(
        gated.handle(ShellEvent::AgeConfirmed),
        ShellAction::ShowTitle
    );
    assert!(gated.age_confirmed());
    assert_eq!(gated.screen(), Screen::Title);
}

// ---------------------------------------------------------------------
// Adversarial: settings
// ---------------------------------------------------------------------

#[test]
fn corrupt_settings_quarantined() {
    let dir = test_dir("settings-corrupt");
    let path = dir.join("settings.ron");
    fs::write(&path, b"this is not ron {{{").expect("write corrupt settings");

    let loaded = Settings::load(&path).expect("load corrupt settings");
    assert_eq!(loaded, Settings::default());
    assert!(corrupt_path(&path).exists(), "corrupt file is quarantined");
    assert!(!path.exists(), "original path is vacated by quarantine");
    cleanup(&dir);
}

#[test]
fn wrong_format_settings_quarantined() {
    let dir = test_dir("settings-wrong-format");
    let path = dir.join("settings.ron");
    fs::write(
        &path,
        "(format: \"some-other-format/9\", settings: (music_volume: 0.25))",
    )
    .expect("write wrong-format settings");

    let loaded = Settings::load(&path).expect("load wrong-format settings");
    assert_eq!(loaded, Settings::default());
    assert!(corrupt_path(&path).exists());
    cleanup(&dir);
}

#[test]
fn nan_volume_repaired_by_validate() {
    let mut settings = Settings::new();
    settings.master_volume = f32::NAN;
    settings.music_volume = 0.25;
    settings.sfx_volume = f32::INFINITY;
    settings.window_width = 10;
    settings.validate();

    assert_eq!(settings.master_volume, 1.0, "NaN repairs to the default");
    assert_eq!(settings.sfx_volume, 1.0, "infinity repairs to the default");
    assert_eq!(settings.music_volume, 0.25, "finite values survive");
    assert_eq!(settings.window_width, 640, "width clamps to the minimum");
}

#[test]
fn nan_volume_in_handwritten_ron_repaired() {
    // RON spells the f32 NaN literal `NaN`. Whether this file is
    // repaired by validate() (parse succeeds) or by quarantine (a
    // parser that rejects NaN), the observable contract is the same:
    // the loaded master volume is the default, never NaN.
    let dir = test_dir("settings-nan-ron");
    let path = dir.join("settings.ron");
    fs::write(
        &path,
        "(format: \"ntranch-shell-settings/1\", settings: (master_volume: NaN))",
    )
    .expect("write NaN settings");

    let loaded = Settings::load(&path).expect("load NaN settings");
    assert_eq!(loaded.master_volume, 1.0);
    assert!(loaded.master_volume.is_finite());
    cleanup(&dir);
}

// ---------------------------------------------------------------------
// Adversarial: saves
// ---------------------------------------------------------------------

#[test]
fn corrupt_save_quarantined_other_slot_intact() {
    let dir = test_dir("save-corrupt");
    let saves = dir.join("saves");
    let store = SaveStore::new(saves.clone());
    store
        .save("slot1", demo_payload(), "One", 1, 10)
        .expect("save slot1");
    store
        .save("slot2", demo_payload(), "Two", 2, 20)
        .expect("save slot2");

    let slot1_path = saves.join("slot1.ron");
    fs::write(&slot1_path, b"garbage bytes, not a save").expect("corrupt slot1");

    let loaded = store.load("slot1").expect("load corrupt slot1");
    assert!(loaded.is_none(), "corrupt slot reads as empty");
    assert!(
        corrupt_path(&slot1_path).exists(),
        "corrupt save is quarantined"
    );

    let intact = store
        .load("slot2")
        .expect("load slot2")
        .expect("slot2 is intact");
    assert_eq!(intact.label, "Two");
    cleanup(&dir);
}

#[test]
fn unsupported_version_errors_and_file_untouched() {
    let dir = test_dir("save-version-99");
    let saves = dir.join("saves");
    fs::create_dir_all(&saves).expect("create saves dir");
    let path = saves.join("slot1.ron");
    // Hand-written, not serialized from our types: a save from the
    // future must be refused, not repaired.
    fs::write(
        &path,
        "(format: \"ntranch-shell-save/1\", envelope: (format_version: 99, \
         saved_at_unix: 0, playtime_seconds: 0, label: \"future\", payload: 0))",
    )
    .expect("write future save");

    let store = SaveStore::new(saves);
    match store.load("slot1") {
        Err(ShellError::UnsupportedVersion { found, current }) => {
            assert_eq!(found, 99);
            assert_eq!(current, shell_core::CURRENT_FORMAT_VERSION);
        }
        other => panic!("expected UnsupportedVersion, got {other:?}"),
    }
    assert!(path.exists(), "a newer save is refused, never quarantined");
    cleanup(&dir);
}

#[test]
fn version_zero_migrates_to_one() {
    let dir = test_dir("save-version-0");
    let saves = dir.join("saves");
    fs::create_dir_all(&saves).expect("create saves dir");
    // A v0 envelope: no `label` field at all. The serde default fills
    // it, and the migration step advances the version.
    fs::write(
        saves.join("slot2.ron"),
        "(format: \"ntranch-shell-save/1\", envelope: (format_version: 0, \
         saved_at_unix: 10, playtime_seconds: 5, payload: \"x\"))",
    )
    .expect("write v0 save");

    let store = SaveStore::new(saves);
    let envelope = store
        .load("slot2")
        .expect("load v0 save")
        .expect("v0 save is present");
    assert_eq!(envelope.format_version, 1);
    assert_eq!(envelope.label, "");
    assert_eq!(envelope.saved_at_unix, 10);

    // The migrate() function itself, exercised directly.
    let raw = SaveEnvelope {
        format_version: 0,
        saved_at_unix: 0,
        playtime_seconds: 0,
        label: String::new(),
        payload: ron::Value::Unit,
    };
    let migrated = migrate(raw).expect("migrate v0");
    assert_eq!(migrated.format_version, 1);
    cleanup(&dir);
}

#[test]
fn invalid_slot_rejected_and_never_escapes() {
    let dir = test_dir("save-invalid-slot");
    let saves = dir.join("saves");
    let store = SaveStore::new(saves.clone());

    match store.save("../evil", demo_payload(), "evil", 0, 0) {
        Err(ShellError::InvalidSlot(name)) => assert_eq!(name, "../evil"),
        other => panic!("expected InvalidSlot, got {other:?}"),
    }
    match store.load("../evil") {
        Err(ShellError::InvalidSlot(_)) => {}
        other => panic!("expected InvalidSlot, got {other:?}"),
    }
    assert!(!store.exists("../evil"));

    // Nothing was written outside (or inside) the saves directory.
    assert!(
        !dir.join("evil.ron").exists(),
        "no escape write one level up"
    );
    assert!(
        !std::env::temp_dir().join("evil.ron").exists(),
        "no escape write into the temp root"
    );
    assert!(store.slots().is_empty());

    // Test slots, by contrast, are accepted.
    store
        .save("__probe", demo_payload(), "probe", 0, 0)
        .expect("save test slot");
    assert!(store.exists("__probe"));
    assert!(store.slots().is_empty(), "test slots are never listed");
    cleanup(&dir);
}
