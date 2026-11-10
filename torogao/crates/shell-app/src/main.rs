//! shell-app — a scripted, headless demo of the shell core.
//!
//! This binary is the template's smoke test: it proves the whole flow
//! end to end in a terminal, with no engine and no display.
//!
//! ```text
//! cargo run -p shell-app [-- <work-dir>]
//! ```
//!
//! It loads settings (defaults), changes and persists one, reloads
//! it; writes a demo save; then walks the shell state machine through
//! boot → new game → pause → settings (opened from pause) → resume →
//! quit to title → continue. Every step is checked; the run prints
//! `OK shell-app` and exits 0, or prints `FAIL shell-app: ..` to
//! stderr and exits 1.
//!
//! The work directory comes from argv[1], or defaults to a per-process
//! directory under the system temp dir. The demo passes a fixed
//! `saved_at_unix` (0) because the shell core never reads the clock —
//! a real game passes its own timestamp.

#![forbid(unsafe_code)]

use std::path::PathBuf;
use std::process::ExitCode;

use shell_core::{SaveStore, Screen, Settings, SettingsReturnTo, Shell, ShellAction, ShellEvent};

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

fn check(condition: bool, step: &str) -> Result<(), String> {
    if condition {
        Ok(())
    } else {
        Err(format!("check failed at step: {step}"))
    }
}

fn run(work_dir: PathBuf) -> Result<(), String> {
    println!("shell-app: work dir {}", work_dir.display());

    // --- Settings: defaults, change, persist, reload. ---
    let settings_path = work_dir.join("config").join("settings.ron");
    let mut settings = Settings::load(&settings_path).map_err(|e| e.to_string())?;
    println!(
        "settings: loaded (master_volume={})",
        settings.master_volume
    );
    check(
        (settings.master_volume - 1.0).abs() < f32::EPSILON,
        "default master volume",
    )?;

    settings.music_volume = 0.5;
    settings.save(&settings_path).map_err(|e| e.to_string())?;
    println!("settings: saved music_volume=0.5");

    let reloaded = Settings::load(&settings_path).map_err(|e| e.to_string())?;
    check(
        (reloaded.music_volume - 0.5).abs() < f32::EPSILON,
        "reloaded music volume",
    )?;
    println!("settings: reload verified");

    // --- Saves: one demo slot. ---
    let store = SaveStore::new(work_dir.join("saves"));
    store
        .save("slot1", demo_payload(), "Demo start", 0, 0)
        .map_err(|e| e.to_string())?;
    println!("saves: wrote slot1 (label \"Demo start\")");
    check(store.exists("slot1"), "slot1 exists after save")?;

    // --- Shell flow walk. ---
    let mut shell = Shell::new(shell_core::shell::AGE_GATE_DEFAULT_ENABLED);
    shell.set_continue_available(store.exists("slot1"));

    let action = shell.handle(ShellEvent::Boot);
    println!("shell: Boot -> {action:?} (screen {:?})", shell.screen());
    check(action == ShellAction::ShowTitle, "boot to title")?;
    check(
        shell.screen() == Screen::Title,
        "screen is title after boot",
    )?;

    let action = shell.handle(ShellEvent::NewGame);
    println!("shell: NewGame -> {action:?} (screen {:?})", shell.screen());
    check(action == ShellAction::StartGame, "new game starts")?;

    let action = shell.handle(ShellEvent::OpenPause);
    println!(
        "shell: OpenPause -> {action:?} (screen {:?})",
        shell.screen()
    );
    check(shell.screen() == Screen::Pause, "pause opens from game")?;

    let action = shell.handle(ShellEvent::OpenSettings);
    println!("shell: OpenSettings -> {action:?}");
    check(
        action == ShellAction::ShowSettings,
        "settings open from pause",
    )?;
    check(
        shell.settings_return_to() == Some(SettingsReturnTo::Pause),
        "settings caller is pause",
    )?;

    let action = shell.handle(ShellEvent::CloseSettings);
    println!(
        "shell: CloseSettings -> {action:?} (screen {:?})",
        shell.screen()
    );
    check(action == ShellAction::HideSettings, "settings close")?;
    check(shell.screen() == Screen::Pause, "settings return to pause")?;

    let action = shell.handle(ShellEvent::ClosePause);
    println!(
        "shell: ClosePause -> {action:?} (screen {:?})",
        shell.screen()
    );
    check(action == ShellAction::ResumeGame, "pause closes to game")?;

    let action = shell.handle(ShellEvent::QuitToTitle);
    println!(
        "shell: QuitToTitle -> {action:?} (screen {:?})",
        shell.screen()
    );
    check(action == ShellAction::ShowTitle, "quit to title")?;

    check(shell.continue_available(), "continue is available")?;
    let action = shell.handle(ShellEvent::ContinueGame);
    println!(
        "shell: ContinueGame -> {action:?} (screen {:?})",
        shell.screen()
    );
    check(action == ShellAction::StartGame, "continue starts the game")?;

    Ok(())
}

fn main() -> ExitCode {
    let work_dir = std::env::args()
        .nth(1)
        .map(PathBuf::from)
        .unwrap_or_else(|| {
            std::env::temp_dir().join(format!("ntranch-shell-app-{}", std::process::id()))
        });
    match run(work_dir) {
        Ok(()) => {
            println!("OK shell-app");
            ExitCode::from(0)
        }
        Err(message) => {
            eprintln!("FAIL shell-app: {message}");
            ExitCode::from(1)
        }
    }
}
