#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// Age-gate panel: explanatory text plus Confirm and Decline
    /// buttons routed to <see cref="GameFlow"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gate exists only when
    /// <see cref="GameFlow.AgeGateEnabled"/> is true; it is off by
    /// default and most projects will never enable it. When enabled,
    /// <see cref="GameFlow.Boot"/> shows this panel first: Confirm
    /// advances to the title screen, Decline quits the application.
    /// </para>
    /// <para>
    /// The gate records nothing: no date of birth is collected, no
    /// answer is persisted, and the check runs on every launch. Any
    /// project with a legal obligation stronger than a self-declared
    /// confirm must replace this panel's behaviour with its own
    /// verified flow — this component is a menu seam, not a
    /// compliance mechanism.
    /// </para>
    /// </remarks>
    public sealed class AgeGateUI : MonoBehaviour
    {
        [SerializeField]
        private GameFlow? gameFlow;

        [SerializeField]
        [Tooltip("Explanatory text shown above the buttons. Set in the inspector or in code.")]
        private Text? messageText;

        [SerializeField]
        private Button? confirmButton;

        [SerializeField]
        private Button? declineButton;

        /// <summary>
        /// Replaces the panel's explanatory text. The default scene
        /// text is a placeholder; a project enabling the gate must
        /// write text that matches its content and its obligations.
        /// </summary>
        /// <param name="message">The text to show; null clears it.</param>
        public void SetMessage(string? message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? "";
            }
        }

        private void Awake()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (declineButton != null)
            {
                declineButton.onClick.AddListener(OnDeclineClicked);
            }
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }

            if (declineButton != null)
            {
                declineButton.onClick.RemoveListener(OnDeclineClicked);
            }
        }

        private void OnConfirmClicked()
        {
            if (gameFlow != null)
            {
                gameFlow.ConfirmAgeGate();
            }
        }

        private void OnDeclineClicked()
        {
            if (gameFlow != null)
            {
                gameFlow.DeclineAgeGate();
            }
        }
    }
}
