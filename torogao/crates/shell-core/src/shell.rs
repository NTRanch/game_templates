//! The screen flow state machine.
//!
//! Pure and total: no I/O, no clock, no allocation beyond the state
//! itself, and [`Shell::handle`] is defined for every (state, event)
//! pair — an invalid transition is ignored and answers
//! [`ShellAction::None`]. It never panics.
//!
//! The shell tracks *screens only*. Game state — what "the game" is
//! when the player quits to title — is the game's problem; the shell
//! just reports that the screen changed.
//!
//! Settings are an overlay, not a screen: opening settings records
//! which screen opened it ([`Shell::settings_return_to`]) and does not
//! change [`Shell::screen`]. Read `settings_return_to()` *before*
//! sending [`ShellEvent::CloseSettings`]: the close clears the record,
//! and the answer to "where do I go back to" after a close is simply
//! `screen()`, which never moved.

/// Whether the age gate is on when a game does not say otherwise.
/// Default off: a template must not gate a game that never asked.
pub const AGE_GATE_DEFAULT_ENABLED: bool = false;

/// The top-level screens of the shell.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Screen {
    /// Age confirmation, shown before anything else when enabled.
    AgeGate,
    /// Title / main menu.
    Title,
    /// Gameplay.
    Game,
    /// Pause menu, over gameplay.
    Pause,
}

/// The screens settings can be opened from, and returns to.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SettingsReturnTo {
    /// Settings opened from the title screen.
    Title,
    /// Settings opened from the pause menu.
    Pause,
}

/// Something that happened: a button press, a boot, a request.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ShellEvent {
    /// The shell is starting.
    Boot,
    /// The player confirmed their age at the gate.
    AgeConfirmed,
    /// The player declined at the age gate.
    AgeDeclined,
    /// Start a new game (from title).
    NewGame,
    /// Continue from an existing save (from title; needs
    /// `continue_available`).
    ContinueGame,
    /// Open the pause menu (from gameplay only).
    OpenPause,
    /// Close the pause menu, back to gameplay.
    ClosePause,
    /// Open the settings overlay (from title or pause).
    OpenSettings,
    /// Close the settings overlay.
    CloseSettings,
    /// Leave gameplay/the pause menu for the title screen.
    QuitToTitle,
    /// The player asked to quit the application.
    QuitRequested,
}

/// What the host should do in response to an event: render a screen,
/// start or resume the session, show or hide the overlay, or exit.
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ShellAction {
    /// Nothing to do. Either the transition was invalid (ignored), or
    /// it changed only shell state the host reads back — opening pause
    /// is the one such case: the screen becomes [`Screen::Pause`] and
    /// the host renders it from [`Shell::screen`].
    None,
    /// Show the age gate.
    ShowAgeGate,
    /// Show the title screen.
    ShowTitle,
    /// Start the game session (new game or continue; the event says
    /// which).
    StartGame,
    /// Resume the paused game session.
    ResumeGame,
    /// Show the settings overlay.
    ShowSettings,
    /// Hide the settings overlay. The screen underneath is unchanged.
    HideSettings,
    /// Exit the application.
    Quit,
}

/// The shell's state.
#[derive(Debug, Clone)]
pub struct Shell {
    screen: Screen,
    settings_open_from: Option<SettingsReturnTo>,
    age_gate_enabled: bool,
    age_confirmed: bool,
    continue_available: bool,
}

impl Shell {
    /// A fresh shell, pre-boot (resting at the title screen until
    /// [`ShellEvent::Boot`] routes it). Pass
    /// [`AGE_GATE_DEFAULT_ENABLED`] if the game has no policy yet.
    pub fn new(age_gate_enabled: bool) -> Self {
        Shell {
            screen: Screen::Title,
            settings_open_from: None,
            age_gate_enabled,
            age_confirmed: false,
            continue_available: false,
        }
    }

    /// The current screen. Settings are an overlay and never change
    /// this; see the module docs.
    pub fn screen(&self) -> Screen {
        self.screen
    }

    /// Where settings will return to, if settings are open. Read this
    /// *before* sending [`ShellEvent::CloseSettings`]; the close
    /// clears it.
    pub fn settings_return_to(&self) -> Option<SettingsReturnTo> {
        self.settings_open_from
    }

    /// Is the settings overlay open?
    pub fn settings_open(&self) -> bool {
        self.settings_open_from.is_some()
    }

    /// Has the player passed the age gate this run?
    pub fn age_confirmed(&self) -> bool {
        self.age_confirmed
    }

    /// Is a save available to continue from?
    pub fn continue_available(&self) -> bool {
        self.continue_available
    }

    /// Tell the shell whether a save exists. The game owns this fact
    /// (it asks its `SaveStore`); the shell only tracks it so
    /// [`ShellEvent::ContinueGame`] can be validated.
    pub fn set_continue_available(&mut self, available: bool) {
        self.continue_available = available;
    }

    /// Advance the machine. Total: every event in every state returns
    /// an action; invalid transitions change nothing and return
    /// [`ShellAction::None`].
    pub fn handle(&mut self, event: ShellEvent) -> ShellAction {
        match event {
            ShellEvent::Boot => {
                if self.age_gate_enabled && !self.age_confirmed {
                    self.screen = Screen::AgeGate;
                    ShellAction::ShowAgeGate
                } else {
                    self.screen = Screen::Title;
                    ShellAction::ShowTitle
                }
            }
            ShellEvent::AgeConfirmed => {
                if self.screen == Screen::AgeGate {
                    self.age_confirmed = true;
                    self.screen = Screen::Title;
                    ShellAction::ShowTitle
                } else {
                    ShellAction::None
                }
            }
            ShellEvent::AgeDeclined => {
                if self.screen == Screen::AgeGate {
                    ShellAction::Quit
                } else {
                    ShellAction::None
                }
            }
            ShellEvent::NewGame => {
                if self.screen == Screen::Title {
                    self.screen = Screen::Game;
                    ShellAction::StartGame
                } else {
                    ShellAction::None
                }
            }
            ShellEvent::ContinueGame => {
                if self.screen == Screen::Title && self.continue_available {
                    self.screen = Screen::Game;
                    ShellAction::StartGame
                } else {
                    ShellAction::None
                }
            }
            ShellEvent::OpenPause => {
                if self.screen == Screen::Game {
                    self.screen = Screen::Pause;
                }
                // No dedicated action exists by design: the host
                // renders the pause menu from `screen()`.
                ShellAction::None
            }
            ShellEvent::ClosePause => {
                if self.screen == Screen::Pause {
                    self.screen = Screen::Game;
                    ShellAction::ResumeGame
                } else {
                    ShellAction::None
                }
            }
            ShellEvent::OpenSettings => {
                if self.settings_open_from.is_some() {
                    ShellAction::None
                } else {
                    match self.screen {
                        Screen::Title => {
                            self.settings_open_from = Some(SettingsReturnTo::Title);
                            ShellAction::ShowSettings
                        }
                        Screen::Pause => {
                            self.settings_open_from = Some(SettingsReturnTo::Pause);
                            ShellAction::ShowSettings
                        }
                        _ => ShellAction::None,
                    }
                }
            }
            ShellEvent::CloseSettings => {
                if self.settings_open_from.is_some() {
                    self.settings_open_from = None;
                    ShellAction::HideSettings
                } else {
                    ShellAction::None
                }
            }
            ShellEvent::QuitToTitle => {
                if self.screen == Screen::Pause || self.screen == Screen::Game {
                    self.screen = Screen::Title;
                    ShellAction::ShowTitle
                } else {
                    ShellAction::None
                }
            }
            ShellEvent::QuitRequested => ShellAction::Quit,
        }
    }
}

impl Default for Shell {
    fn default() -> Self {
        Self::new(AGE_GATE_DEFAULT_ENABLED)
    }
}
