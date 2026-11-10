#nullable enable
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// Binds every canonical setting to a UI control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Control map: sliders for master volume, music volume, SFX
    /// volume, and screen shake (each with a value label showing a
    /// percentage); toggles for fullscreen, vsync, reduce flashing,
    /// and subtitles; input fields for window width and height
    /// (integer input, clamped on end edit). Back returns through
    /// <see cref="GameFlow.CloseSettings"/>; Reset Defaults restores
    /// <see cref="SettingsData.Default"/> and repopulates the panel.
    /// </para>
    /// <para>
    /// The panel populates from
    /// <see cref="SettingsManager.Current"/> on enable using the
    /// controls' <c>WithoutNotify</c> setters, so opening the panel
    /// never writes settings. After population, every control change
    /// writes through the manager immediately — the manager applies,
    /// persists, and announces each change, so there is no separate
    /// Apply button and no unsaved state.
    /// </para>
    /// </remarks>
    public sealed class SettingsUI : MonoBehaviour
    {
        [SerializeField]
        private GameFlow? gameFlow;

        [SerializeField]
        private Slider? masterVolumeSlider;

        [SerializeField]
        private Text? masterVolumeValue;

        [SerializeField]
        private Slider? musicVolumeSlider;

        [SerializeField]
        private Text? musicVolumeValue;

        [SerializeField]
        private Slider? sfxVolumeSlider;

        [SerializeField]
        private Text? sfxVolumeValue;

        [SerializeField]
        private Slider? screenShakeSlider;

        [SerializeField]
        private Text? screenShakeValue;

        [SerializeField]
        private Toggle? fullscreenToggle;

        [SerializeField]
        private Toggle? vsyncToggle;

        [SerializeField]
        private Toggle? reduceFlashingToggle;

        [SerializeField]
        private Toggle? subtitlesToggle;

        [SerializeField]
        private InputField? windowWidthInput;

        [SerializeField]
        private InputField? windowHeightInput;

        [SerializeField]
        private Button? backButton;

        [SerializeField]
        private Button? resetDefaultsButton;

        private bool _populating;

        private void Awake()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (screenShakeSlider != null)
            {
                screenShakeSlider.onValueChanged.AddListener(OnScreenShakeChanged);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (vsyncToggle != null)
            {
                vsyncToggle.onValueChanged.AddListener(OnVsyncChanged);
            }

            if (reduceFlashingToggle != null)
            {
                reduceFlashingToggle.onValueChanged.AddListener(OnReduceFlashingChanged);
            }

            if (subtitlesToggle != null)
            {
                subtitlesToggle.onValueChanged.AddListener(OnSubtitlesChanged);
            }

            if (windowWidthInput != null)
            {
                windowWidthInput.onEndEdit.AddListener(OnWindowSizeEndEdit);
            }

            if (windowHeightInput != null)
            {
                windowHeightInput.onEndEdit.AddListener(OnWindowSizeEndEdit);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (resetDefaultsButton != null)
            {
                resetDefaultsButton.onClick.AddListener(OnResetDefaultsClicked);
            }
        }

        private void OnDestroy()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            }

            if (screenShakeSlider != null)
            {
                screenShakeSlider.onValueChanged.RemoveListener(OnScreenShakeChanged);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            }

            if (vsyncToggle != null)
            {
                vsyncToggle.onValueChanged.RemoveListener(OnVsyncChanged);
            }

            if (reduceFlashingToggle != null)
            {
                reduceFlashingToggle.onValueChanged.RemoveListener(
                    OnReduceFlashingChanged
                );
            }

            if (subtitlesToggle != null)
            {
                subtitlesToggle.onValueChanged.RemoveListener(OnSubtitlesChanged);
            }

            if (windowWidthInput != null)
            {
                windowWidthInput.onEndEdit.RemoveListener(OnWindowSizeEndEdit);
            }

            if (windowHeightInput != null)
            {
                windowHeightInput.onEndEdit.RemoveListener(OnWindowSizeEndEdit);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
            }

            if (resetDefaultsButton != null)
            {
                resetDefaultsButton.onClick.RemoveListener(OnResetDefaultsClicked);
            }
        }

        private void OnEnable()
        {
            Populate();
        }

        /// <summary>
        /// Re-reads the manager's current settings into every control
        /// without triggering a write-back.
        /// </summary>
        public void Populate()
        {
            SettingsManager? manager = SettingsManager.Instance;
            if (manager == null)
            {
                return;
            }

            SettingsData data = manager.Current;
            _populating = true;
            try
            {
                if (masterVolumeSlider != null)
                {
                    masterVolumeSlider.SetValueWithoutNotify(data.master_volume);
                }

                if (musicVolumeSlider != null)
                {
                    musicVolumeSlider.SetValueWithoutNotify(data.music_volume);
                }

                if (sfxVolumeSlider != null)
                {
                    sfxVolumeSlider.SetValueWithoutNotify(data.sfx_volume);
                }

                if (screenShakeSlider != null)
                {
                    screenShakeSlider.SetValueWithoutNotify(data.screen_shake);
                }

                if (fullscreenToggle != null)
                {
                    fullscreenToggle.SetIsOnWithoutNotify(data.fullscreen);
                }

                if (vsyncToggle != null)
                {
                    vsyncToggle.SetIsOnWithoutNotify(data.vsync);
                }

                if (reduceFlashingToggle != null)
                {
                    reduceFlashingToggle.SetIsOnWithoutNotify(data.reduce_flashing);
                }

                if (subtitlesToggle != null)
                {
                    subtitlesToggle.SetIsOnWithoutNotify(data.subtitles);
                }

                if (windowWidthInput != null)
                {
                    windowWidthInput.SetTextWithoutNotify(
                        data.window_width.ToString(CultureInfo.InvariantCulture)
                    );
                }

                if (windowHeightInput != null)
                {
                    windowHeightInput.SetTextWithoutNotify(
                        data.window_height.ToString(CultureInfo.InvariantCulture)
                    );
                }

                RefreshValueLabels(data);
            }
            finally
            {
                _populating = false;
            }
        }

        private void RefreshValueLabels(SettingsData data)
        {
            SetPercentLabel(masterVolumeValue, data.master_volume);
            SetPercentLabel(musicVolumeValue, data.music_volume);
            SetPercentLabel(sfxVolumeValue, data.sfx_volume);
            SetPercentLabel(screenShakeValue, data.screen_shake);
        }

        private static void SetPercentLabel(Text? label, float value)
        {
            if (label != null)
            {
                int percent = Mathf.RoundToInt(
                    Mathf.Clamp(value, 0.0f, 1.0f) * 100.0f
                );
                label.text = percent.ToString(CultureInfo.InvariantCulture) + "%";
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (_populating || SettingsManager.Instance == null)
            {
                return;
            }

            SettingsManager.Instance.SetMasterVolume(value);
            SetPercentLabel(masterVolumeValue, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (_populating || SettingsManager.Instance == null)
            {
                return;
            }

            SettingsManager.Instance.SetMusicVolume(value);
            SetPercentLabel(musicVolumeValue, value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (_populating || SettingsManager.Instance == null)
            {
                return;
            }

            SettingsManager.Instance.SetSfxVolume(value);
            SetPercentLabel(sfxVolumeValue, value);
        }

        private void OnScreenShakeChanged(float value)
        {
            if (_populating || SettingsManager.Instance == null)
            {
                return;
            }

            SettingsManager.Instance.SetScreenShake(value);
            SetPercentLabel(screenShakeValue, value);
        }

        private void OnFullscreenChanged(bool value)
        {
            if (_populating || SettingsManager.Instance == null)
            {
                return;
            }

            SettingsManager.Instance.SetFullscreen(value);
        }

        private void OnVsyncChanged(bool value)
        {
            if (_populating || SettingsManager.Instance == null)
            {
                return;
            }

            SettingsManager.Instance.SetVsync(value);
        }

        private void OnReduceFlashingChanged(bool value)
        {
            if (_populating || SettingsManager.Instance == null)
            {
                return;
            }

            SettingsManager.Instance.SetReduceFlashing(value);
        }

        private void OnSubtitlesChanged(bool value)
        {
            if (_populating || SettingsManager.Instance == null)
            {
                return;
            }

            SettingsManager.Instance.SetSubtitles(value);
        }

        private void OnWindowSizeEndEdit(string ignored)
        {
            if (_populating || SettingsManager.Instance == null)
            {
                return;
            }

            SettingsData data = SettingsManager.Instance.Current;
            int width = ParseOrFallback(
                windowWidthInput != null ? windowWidthInput.text : null,
                data.window_width
            );
            int height = ParseOrFallback(
                windowHeightInput != null ? windowHeightInput.text : null,
                data.window_height
            );
            // SetWindowSize validates: out-of-range input is clamped
            // by SettingsData.Validate, and Populate shows the result.
            SettingsManager.Instance.SetWindowSize(width, height);
            Populate();
        }

        private static int ParseOrFallback(string? text, int fallback)
        {
            if (
                int.TryParse(
                    text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int parsed
                )
            )
            {
                return parsed;
            }

            return fallback;
        }

        private void OnBackClicked()
        {
            if (gameFlow != null)
            {
                gameFlow.CloseSettings();
            }
        }

        private void OnResetDefaultsClicked()
        {
            if (SettingsManager.Instance == null)
            {
                return;
            }

            SettingsManager.Instance.ResetDefaults();
            Populate();
        }
    }
}
