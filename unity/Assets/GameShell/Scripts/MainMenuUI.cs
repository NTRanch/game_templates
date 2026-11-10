#nullable enable
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// Title-screen menu: Continue, New Game, Load, Settings, Quit,
    /// plus the load-mode slot panel.
    /// </summary>
    /// <remarks>
    /// Buttons are wired in code in <c>Awake</c> and unwired in
    /// <c>OnDestroy</c>, so the panel works without inspector event
    /// wiring and cannot leak a listener. Continue is interactable
    /// only when <see cref="SaveManager.HasAnySave"/> is true; that
    /// is refreshed every time the panel is enabled, because saves
    /// can appear or disappear while the title screen is up.
    /// The slot panel lists the four slots in
    /// <see cref="SaveManager.SlotNames"/> order; in load mode only
    /// non-empty slots are interactable.
    /// </remarks>
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField]
        private GameFlow? gameFlow;

        [SerializeField]
        private Button? continueButton;

        [SerializeField]
        private Button? newGameButton;

        [SerializeField]
        private Button? loadButton;

        [SerializeField]
        private Button? settingsButton;

        [SerializeField]
        private Button? quitButton;

        [SerializeField]
        private GameObject? slotPanel;

        [SerializeField]
        private Button? slotPanelCloseButton;

        [SerializeField]
        [Tooltip("Exactly four buttons, in SaveManager.SlotNames order.")]
        private Button[] slotButtons = new Button[0];

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(OnLoadClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (slotPanelCloseButton != null)
            {
                slotPanelCloseButton.onClick.AddListener(CloseSlotPanel);
            }

            for (int index = 0; index < slotButtons.Length; index++)
            {
                int capturedIndex = index;
                if (slotButtons[index] != null)
                {
                    slotButtons[index].onClick.AddListener(
                        () => OnSlotClicked(capturedIndex)
                    );
                }
            }
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(OnNewGameClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(OnLoadClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
            }

            if (slotPanelCloseButton != null)
            {
                slotPanelCloseButton.onClick.RemoveListener(CloseSlotPanel);
            }

            // Slot buttons use anonymous listeners; RemoveAllListeners
            // is correct here because this component owns those buttons'
            // click events by template contract.
            for (int index = 0; index < slotButtons.Length; index++)
            {
                if (slotButtons[index] != null)
                {
                    slotButtons[index].onClick.RemoveAllListeners();
                }
            }
        }

        private void OnEnable()
        {
            RefreshContinue();
            CloseSlotPanel();
        }

        /// <summary>
        /// Recomputes the Continue button's interactable state from
        /// the save store. Called on enable; the game may also call
        /// it after deleting saves from elsewhere.
        /// </summary>
        public void RefreshContinue()
        {
            if (continueButton != null)
            {
                continueButton.interactable = SaveManager.HasAnySave();
            }

            if (loadButton != null)
            {
                loadButton.interactable = SaveManager.HasAnySave();
            }
        }

        /// <summary>Opens the slot panel in load mode.</summary>
        public void OpenLoadPanel()
        {
            RefreshSlotPanel();
            SetSlotPanelVisible(true);
        }

        /// <summary>Closes the slot panel.</summary>
        public void CloseSlotPanel()
        {
            SetSlotPanelVisible(false);
        }

        private void RefreshSlotPanel()
        {
            int count = Math.Min(slotButtons.Length, SaveManager.SlotNames.Length);
            for (int index = 0; index < count; index++)
            {
                Button? button = slotButtons[index];
                if (button == null)
                {
                    continue;
                }

                string slot = SaveManager.SlotNames[index];
                bool hasSave = SaveManager.GetMetadata(
                    slot,
                    out SaveEnvelope? metadata
                );
                button.interactable = hasSave;
                SetButtonLabel(button, FormatSlotLabel(slot, hasSave, metadata));
            }
        }

        private static string FormatSlotLabel(
            string slot,
            bool hasSave,
            SaveEnvelope? metadata
        )
        {
            if (!hasSave || metadata == null)
            {
                return slot + " \u2014 Empty";
            }

            // saved_at_unix is untrusted file data: an out-of-range
            // value must not throw inside a menu label.
            long clampedSeconds = Math.Clamp(
                metadata.saved_at_unix,
                0,
                253402300799L
            );
            DateTime localTime = DateTimeOffset
                .FromUnixTimeSeconds(clampedSeconds)
                .LocalDateTime;
            string when = localTime.ToString(
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture
            );
            string label = string.IsNullOrEmpty(metadata.label)
                ? "Untitled"
                : metadata.label;
            return slot + " \u2014 " + label + " \u2014 " + when;
        }

        private static void SetButtonLabel(Button button, string text)
        {
            Text? label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private void SetSlotPanelVisible(bool visible)
        {
            if (slotPanel != null && slotPanel.activeSelf != visible)
            {
                slotPanel.SetActive(visible);
            }
        }

        private void OnContinueClicked()
        {
            if (gameFlow != null)
            {
                gameFlow.ContinueGame();
            }
        }

        private void OnNewGameClicked()
        {
            if (gameFlow != null)
            {
                gameFlow.StartNewGame();
            }
        }

        private void OnLoadClicked()
        {
            OpenLoadPanel();
        }

        private void OnSettingsClicked()
        {
            if (gameFlow != null)
            {
                gameFlow.OpenSettings(GameFlow.SettingsReturnTo.Title);
            }
        }

        private void OnQuitClicked()
        {
            if (gameFlow != null)
            {
                gameFlow.QuitGame();
            }
        }

        private void OnSlotClicked(int index)
        {
            if (index < 0 || index >= SaveManager.SlotNames.Length)
            {
                return;
            }

            if (gameFlow != null && gameFlow.LoadSlot(SaveManager.SlotNames[index]))
            {
                CloseSlotPanel();
            }
        }
    }
}
