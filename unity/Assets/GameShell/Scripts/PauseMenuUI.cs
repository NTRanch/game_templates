#nullable enable
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// Pause overlay: Resume, Save, Load, Settings, Quit to Title,
    /// plus a slot panel shared by the save and load modes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Saving goes through <see cref="GameFlow.SaveToSlot"/>, which
    /// pulls the payload from the provider the game registered with
    /// <see cref="ShellBridge.RegisterPayloadProvider"/>. If no
    /// provider is registered the save fails closed with a warning
    /// from the bridge — wire the provider in the gameplay scene's
    /// bootstrap, before the player can pause.
    /// </para>
    /// <para>
    /// In save mode every slot is selectable (choosing a slot
    /// overwrites it; the template deliberately ships no confirmation
    /// dialog — add one in the project if overwriting silently is not
    /// acceptable for the game). In load mode only non-empty slots
    /// are selectable. A successful load closes the pause overlay by
    /// entering the game state; a successful save leaves the pause
    /// overlay open.
    /// </para>
    /// </remarks>
    public sealed class PauseMenuUI : MonoBehaviour
    {
        private enum SlotPanelMode
        {
            Save,
            Load,
        }

        [SerializeField]
        private GameFlow? gameFlow;

        [SerializeField]
        private Button? resumeButton;

        [SerializeField]
        private Button? saveButton;

        [SerializeField]
        private Button? loadButton;

        [SerializeField]
        private Button? settingsButton;

        [SerializeField]
        private Button? quitToTitleButton;

        [SerializeField]
        private GameObject? slotPanel;

        [SerializeField]
        private Button? slotPanelCloseButton;

        [SerializeField]
        [Tooltip("Exactly four buttons, in SaveManager.SlotNames order.")]
        private Button[] slotButtons = new Button[0];

        private SlotPanelMode _slotPanelMode = SlotPanelMode.Save;

        private void Awake()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSaveClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(OnLoadClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (quitToTitleButton != null)
            {
                quitToTitleButton.onClick.AddListener(OnQuitToTitleClicked);
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
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveListener(OnSaveClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveListener(OnLoadClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            }

            if (quitToTitleButton != null)
            {
                quitToTitleButton.onClick.RemoveListener(OnQuitToTitleClicked);
            }

            if (slotPanelCloseButton != null)
            {
                slotPanelCloseButton.onClick.RemoveListener(CloseSlotPanel);
            }

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
            CloseSlotPanel();
        }

        /// <summary>Opens the slot panel in save mode.</summary>
        public void OpenSavePanel()
        {
            _slotPanelMode = SlotPanelMode.Save;
            RefreshSlotPanel();
            SetSlotPanelVisible(true);
        }

        /// <summary>Opens the slot panel in load mode.</summary>
        public void OpenLoadPanel()
        {
            _slotPanelMode = SlotPanelMode.Load;
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
                button.interactable =
                    _slotPanelMode == SlotPanelMode.Save || hasSave;
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

        private void OnResumeClicked()
        {
            if (gameFlow != null)
            {
                gameFlow.ClosePause();
            }
        }

        private void OnSaveClicked()
        {
            OpenSavePanel();
        }

        private void OnLoadClicked()
        {
            OpenLoadPanel();
        }

        private void OnSettingsClicked()
        {
            if (gameFlow != null)
            {
                gameFlow.OpenSettings(GameFlow.SettingsReturnTo.Pause);
            }
        }

        private void OnQuitToTitleClicked()
        {
            if (gameFlow != null)
            {
                gameFlow.QuitToTitle();
            }
        }

        private void OnSlotClicked(int index)
        {
            if (index < 0 || index >= SaveManager.SlotNames.Length || gameFlow == null)
            {
                return;
            }

            string slot = SaveManager.SlotNames[index];
            if (_slotPanelMode == SlotPanelMode.Save)
            {
                if (gameFlow.SaveToSlot(slot, "Manual save"))
                {
                    RefreshSlotPanel();
                }
            }
            else if (gameFlow.LoadSlot(slot))
            {
                CloseSlotPanel();
            }
        }
    }
}
