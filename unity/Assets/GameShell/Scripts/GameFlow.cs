#nullable enable
using UnityEngine;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// Owns the shell's menu state machine and panel visibility.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Flow: <see cref="Boot"/> shows the age gate when
    /// <see cref="AgeGateEnabled"/> is true, otherwise the title
    /// screen. From the title the player starts a new game, continues
    /// or loads through a slot, opens settings, or quits. In game,
    /// <see cref="OpenPause"/> freezes play; the pause menu can save,
    /// load, open settings, resume, or quit to title.
    /// </para>
    /// <para>
    /// Settings remembers its caller: <see cref="OpenSettings"/>
    /// records a <see cref="SettingsReturnTo"/> value and
    /// <see cref="CloseSettings"/> returns to that panel instead of
    /// guessing from the current state.
    /// </para>
    /// <para>
    /// Pause contract: pausing sets <see cref="Time.timeScale"/> to
    /// zero and restores the value captured on entry. It does not
    /// destroy, unload, or reset game state. Two consequences the
    /// game must design for: coroutines and updates driven by scaled
    /// time stop while paused (use unscaled time for pause-menu
    /// animation), and audio sources without
    /// <c>AudioSource.ignoreListenerPause</c> handling keep their own
    /// behaviour — if the game wants music to duck or stop on pause,
    /// it does so in response to the state change, not by relying on
    /// the shell.
    /// </para>
    /// </remarks>
    public sealed class GameFlow : MonoBehaviour
    {
        /// <summary>Top-level shell states.</summary>
        public enum FlowState
        {
            /// <summary>The age gate is showing (only when enabled).</summary>
            AgeGate,

            /// <summary>The title / main menu is showing.</summary>
            Title,

            /// <summary>Gameplay is running and unpaused.</summary>
            Game,

            /// <summary>Gameplay is loaded but frozen by the pause overlay.</summary>
            Paused,
        }

        /// <summary>Where <see cref="CloseSettings"/> returns to.</summary>
        public enum SettingsReturnTo
        {
            /// <summary>Settings was opened from the title screen.</summary>
            Title,

            /// <summary>Settings was opened from the pause overlay.</summary>
            Pause,
        }

        /// <summary>
        /// Master switch for the age gate. Off by default: a project
        /// that needs a gate sets this to true from its own bootstrap
        /// before <see cref="Boot"/> runs. When enabled, the age gate
        /// is the first panel shown; confirming enters the title
        /// screen and declining quits.
        /// </summary>
        public static bool AgeGateEnabled = false;

        [SerializeField]
        private GameObject? titlePanel;

        [SerializeField]
        private GameObject? pausePanel;

        [SerializeField]
        private GameObject? settingsPanel;

        [SerializeField]
        private GameObject? ageGatePanel;

        [SerializeField]
        private GameObject? hudRoot;

        private bool _booted;
        private float _timeScaleBeforePause = 1.0f;

        /// <summary>The current top-level shell state.</summary>
        public FlowState CurrentState { get; private set; } = FlowState.Title;

        /// <summary>The panel settings will return to when closed.</summary>
        public SettingsReturnTo CurrentSettingsReturn { get; private set; } =
            SettingsReturnTo.Title;

        /// <summary>Whether the settings panel is currently open.</summary>
        public bool IsSettingsOpen { get; private set; }

        private void Start()
        {
            if (!_booted)
            {
                Boot();
            }
        }

        /// <summary>
        /// Entry point for the Boot scene. Decides between the age
        /// gate and the title screen. Idempotent: later calls re-show
        /// the correct first panel only when the game is not already
        /// in progress.
        /// </summary>
        public void Boot()
        {
            _booted = true;
            Time.timeScale = 1.0f;
            if (AgeGateEnabled)
            {
                CurrentState = FlowState.AgeGate;
            }
            else
            {
                CurrentState = FlowState.Title;
            }

            RefreshPanels();
        }

        /// <summary>
        /// Confirms the age gate and advances to the title screen.
        /// Called by <see cref="AgeGateUI"/>.
        /// </summary>
        public void ConfirmAgeGate()
        {
            if (CurrentState != FlowState.AgeGate)
            {
                return;
            }

            CurrentState = FlowState.Title;
            RefreshPanels();
        }

        /// <summary>
        /// Declines the age gate and quits. Called by
        /// <see cref="AgeGateUI"/>.
        /// </summary>
        public void DeclineAgeGate()
        {
            QuitGame();
        }

        /// <summary>
        /// Starts a new game: leaves the menus and shows the HUD.
        /// The game itself resets its state in response — the shell
        /// deliberately does not reach into game state. Games that
        /// need a reset hook should observe the transition from their
        /// own bootstrap or be started from a dedicated scene load
        /// wired into this call site in the project.
        /// </summary>
        public void StartNewGame()
        {
            EnterGame();
        }

        /// <summary>
        /// Continues from the most recently written loadable slot
        /// (the <c>auto</c> slot is preferred when present, then
        /// slot1, slot2, slot3 in that order).
        /// </summary>
        /// <returns>
        /// True when a slot was loaded and its payload restored
        /// through <see cref="ShellBridge"/>; false when no save
        /// exists or restoration failed. On failure the flow stays on
        /// the title screen.
        /// </returns>
        public bool ContinueGame()
        {
            string[] preference = { "auto", "slot1", "slot2", "slot3" };
            long newestSavedAt = long.MinValue;
            string? newestSlot = null;

            for (int index = 0; index < preference.Length; index++)
            {
                if (
                    SaveManager.GetMetadata(preference[index], out SaveEnvelope? metadata)
                    && metadata != null
                )
                {
                    if (metadata.saved_at_unix > newestSavedAt)
                    {
                        newestSavedAt = metadata.saved_at_unix;
                        newestSlot = preference[index];
                    }
                }
            }

            if (newestSlot == null)
            {
                Debug.LogWarning("GameFlow: Continue was requested with no save present.");
                return false;
            }

            return LoadSlot(newestSlot);
        }

        /// <summary>
        /// Loads a named slot and enters the game on success.
        /// </summary>
        /// <param name="slot">One of <see cref="SaveManager.SlotNames"/>.</param>
        /// <returns>True when the slot loaded and the game restored it.</returns>
        public bool LoadSlot(string? slot)
        {
            if (!SaveManager.TryLoad(slot, out SaveEnvelope? envelope) || envelope == null)
            {
                Debug.LogWarning("GameFlow: load failed for the requested slot.");
                return false;
            }

            if (!ShellBridge.TryRestore(envelope.payload_json))
            {
                return false;
            }

            EnterGame();
            return true;
        }

        /// <summary>
        /// Saves the game's current state (via
        /// <see cref="ShellBridge"/>) into a named slot.
        /// </summary>
        /// <param name="slot">One of <see cref="SaveManager.SlotNames"/>.</param>
        /// <param name="label">Player-facing slot label; null becomes empty.</param>
        /// <returns>True when the payload was obtained and written.</returns>
        public bool SaveToSlot(string? slot, string? label)
        {
            if (!ShellBridge.TryGetPayload(out string payloadJson, out long playtimeSeconds))
            {
                return false;
            }

            return SaveManager.Save(slot, payloadJson, label, playtimeSeconds);
        }

        /// <summary>
        /// Opens the pause overlay and freezes scaled time. Game
        /// state is left exactly as it was.
        /// </summary>
        public void OpenPause()
        {
            if (CurrentState != FlowState.Game || IsSettingsOpen)
            {
                return;
            }

            _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0.0f;
            CurrentState = FlowState.Paused;
            RefreshPanels();
        }

        /// <summary>Closes the pause overlay and restores scaled time.</summary>
        public void ClosePause()
        {
            if (CurrentState != FlowState.Paused)
            {
                return;
            }

            Time.timeScale = _timeScaleBeforePause;
            CurrentState = FlowState.Game;
            IsSettingsOpen = false;
            RefreshPanels();
        }

        /// <summary>
        /// Opens settings and records where to return to.
        /// </summary>
        /// <param name="from">The panel that opened settings.</param>
        public void OpenSettings(SettingsReturnTo from)
        {
            CurrentSettingsReturn = from;
            IsSettingsOpen = true;
            RefreshPanels();
        }

        /// <summary>
        /// Closes settings and returns to the recorded caller (title
        /// or pause). Scaled time is untouched: if the caller was the
        /// pause overlay, the game stays frozen.
        /// </summary>
        public void CloseSettings()
        {
            IsSettingsOpen = false;
            RefreshPanels();
        }

        /// <summary>
        /// Leaves gameplay (or pause) and returns to the title
        /// screen, restoring normal scaled time. Game state is not
        /// destroyed by the shell; the project's scene arrangement
        /// decides what unloading means.
        /// </summary>
        public void QuitToTitle()
        {
            Time.timeScale = 1.0f;
            IsSettingsOpen = false;
            CurrentState = FlowState.Title;
            RefreshPanels();
        }

        /// <summary>
        /// Quits the application. In the editor,
        /// <see cref="Application.Quit"/> does not stop Play mode;
        /// use the editor's Play button to stop, or guard a project
        /// specific editor stop behind <c>#if UNITY_EDITOR</c> in
        /// project code (never in this runtime template file).
        /// </summary>
        public void QuitGame()
        {
            Application.Quit();
        }

        private void EnterGame()
        {
            Time.timeScale = 1.0f;
            IsSettingsOpen = false;
            CurrentState = FlowState.Game;
            RefreshPanels();
        }

        private void RefreshPanels()
        {
            SetPanelActive(ageGatePanel, CurrentState == FlowState.AgeGate && !IsSettingsOpen);
            SetPanelActive(
                titlePanel,
                CurrentState == FlowState.Title && !IsSettingsOpen
            );
            SetPanelActive(
                pausePanel,
                CurrentState == FlowState.Paused && !IsSettingsOpen
            );
            SetPanelActive(settingsPanel, IsSettingsOpen);
            SetPanelActive(
                hudRoot,
                CurrentState == FlowState.Game || CurrentState == FlowState.Paused
            );
        }

        private static void SetPanelActive(GameObject? panel, bool active)
        {
            if (panel != null && panel.activeSelf != active)
            {
                panel.SetActive(active);
            }
        }
    }
}
