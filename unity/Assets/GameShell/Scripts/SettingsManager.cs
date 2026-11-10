#nullable enable
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// Loads, applies, and persists <see cref="SettingsData"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Singleton: the first instance in the Boot scene becomes
    /// <see cref="Instance"/>, marks itself
    /// <c>DontDestroyOnLoad</c>, and loads settings in
    /// <c>Awake</c>. A duplicate instance destroys itself. The
    /// template allows at most two <c>DontDestroyOnLoad</c> objects
    /// (this manager and, only if a project deliberately persists it,
    /// <see cref="GameFlow"/>); the shell as shipped creates exactly
    /// one. Do not add more.
    /// </para>
    /// <para>
    /// Persistence contract: JSON at
    /// <c>Application.persistentDataPath/settings.json</c>, written
    /// atomically (write <c>settings.json.tmp</c>, then replace).
    /// A file that fails to parse is quarantined to
    /// <c>settings.json.corrupt</c>, a warning is logged, and the
    /// defaults are used. Loading and saving never throw to the
    /// player.
    /// </para>
    /// <para>
    /// Apply contract: master volume goes to
    /// <see cref="AudioListener.volume"/>; music and SFX volumes go
    /// to the optional <see cref="AudioMixer"/> references below when
    /// assigned. When no mixer is assigned the values are still
    /// stored, persisted, and exposed through <see cref="Current"/>
    /// for the game to consume directly (for example by scaling
    /// <c>AudioSource.volume</c> at spawn time). Display settings go
    /// to <see cref="Screen.SetResolution(int, int, bool)"/> and
    /// <see cref="QualitySettings.vSyncCount"/>.
    /// </para>
    /// </remarks>
    public sealed class SettingsManager : MonoBehaviour
    {
        /// <summary>File name of the settings document.</summary>
        public const string SettingsFileName = "settings.json";

        /// <summary>Exposed mixer parameter for the music group.</summary>
        public const string MusicVolumeParameter = "MusicVolume";

        /// <summary>Exposed mixer parameter for the SFX group.</summary>
        public const string SfxVolumeParameter = "SfxVolume";

        /// <summary>Mixer level used for a linear volume of zero.</summary>
        private const float SilenceDecibels = -80.0f;

        [SerializeField]
        [Tooltip(
            "Optional. The AudioMixer whose exposed MusicVolume parameter "
                + "tracks the music setting. Leave empty to store the value only."
        )]
        private AudioMixer? musicMixer;

        [SerializeField]
        [Tooltip(
            "Optional. The AudioMixer whose exposed SfxVolume parameter "
                + "tracks the SFX setting. Leave empty to store the value only."
        )]
        private AudioMixer? sfxMixer;

        /// <summary>The live singleton, or null before the Boot scene runs.</summary>
        public static SettingsManager? Instance { get; private set; }

        /// <summary>
        /// The current settings. Never null after <c>Awake</c>; always
        /// validated. Mutate it only through the <c>Set*</c> methods so
        /// changes are applied, persisted, and announced.
        /// </summary>
        public SettingsData Current { get; private set; } = SettingsData.Default();

        /// <summary>
        /// Raised after a setting changed, was applied, and was saved.
        /// Subscribers should re-read <see cref="Current"/>.
        /// </summary>
        public event Action? SettingsChanged;

        /// <summary>Full path of the settings file.</summary>
        public static string SettingsPath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, SettingsFileName);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Loads settings from disk, falling back to defaults on any
        /// failure, then applies them. Called once from <c>Awake</c>;
        /// safe to call again to re-read the file.
        /// </summary>
        public void Load()
        {
            Current = LoadFromDisk();
            Current.Validate();
            Apply();
        }

        /// <summary>
        /// Persists the current settings atomically. Failures are
        /// logged as warnings and swallowed: a failed save loses the
        /// change on disk but never interrupts play.
        /// </summary>
        public void Save()
        {
            string path = SettingsPath;
            string temporaryPath = path + ".tmp";
            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(Current, true);
                File.WriteAllText(temporaryPath, json);
                ReplaceFile(temporaryPath, path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "SettingsManager: could not save settings ("
                        + exception.Message
                        + "); the in-memory values still apply."
                );
                TryDelete(temporaryPath);
            }
        }

        /// <summary>
        /// Applies the current settings to audio and display.
        /// </summary>
        public void Apply()
        {
            AudioListener.volume = Current.master_volume;

            if (musicMixer != null)
            {
                musicMixer.SetFloat(
                    MusicVolumeParameter,
                    LinearToDecibels(Current.music_volume)
                );
            }

            if (sfxMixer != null)
            {
                sfxMixer.SetFloat(SfxVolumeParameter, LinearToDecibels(Current.sfx_volume));
            }

            Screen.SetResolution(
                Current.window_width,
                Current.window_height,
                Current.fullscreen
            );
            QualitySettings.vSyncCount = Current.vsync ? 1 : 0;
        }

        /// <summary>Sets, validates, applies, and saves the master volume (0..1).</summary>
        /// <param name="value">Linear volume; clamped by validation.</param>
        public void SetMasterVolume(float value)
        {
            Current.master_volume = value;
            CommitChange();
        }

        /// <summary>Sets, validates, applies, and saves the music volume (0..1).</summary>
        /// <param name="value">Linear volume; clamped by validation.</param>
        public void SetMusicVolume(float value)
        {
            Current.music_volume = value;
            CommitChange();
        }

        /// <summary>Sets, validates, applies, and saves the SFX volume (0..1).</summary>
        /// <param name="value">Linear volume; clamped by validation.</param>
        public void SetSfxVolume(float value)
        {
            Current.sfx_volume = value;
            CommitChange();
        }

        /// <summary>
        /// Sets, validates, applies, and saves the screen-shake
        /// multiplier (0..1).
        /// </summary>
        /// <param name="value">Intensity multiplier; clamped by validation.</param>
        public void SetScreenShake(float value)
        {
            Current.screen_shake = value;
            CommitChange();
        }

        /// <summary>Sets, applies, and saves the fullscreen flag.</summary>
        /// <param name="value">True for fullscreen.</param>
        public void SetFullscreen(bool value)
        {
            Current.fullscreen = value;
            CommitChange();
        }

        /// <summary>Sets, applies, and saves the vsync flag.</summary>
        /// <param name="value">True to synchronise presentation to the display.</param>
        public void SetVsync(bool value)
        {
            Current.vsync = value;
            CommitChange();
        }

        /// <summary>Sets, applies, and saves the reduce-flashing flag.</summary>
        /// <param name="value">True to ask the game to reduce flashing effects.</param>
        public void SetReduceFlashing(bool value)
        {
            Current.reduce_flashing = value;
            CommitChange();
        }

        /// <summary>Sets, applies, and saves the subtitles flag.</summary>
        /// <param name="value">True to show subtitles where provided.</param>
        public void SetSubtitles(bool value)
        {
            Current.subtitles = value;
            CommitChange();
        }

        /// <summary>
        /// Sets, validates (clamped to 640..7680 by 360..4320),
        /// applies, and saves the window size.
        /// </summary>
        /// <param name="width">Window width in pixels.</param>
        /// <param name="height">Window height in pixels.</param>
        public void SetWindowSize(int width, int height)
        {
            Current.window_width = width;
            Current.window_height = height;
            CommitChange();
        }

        /// <summary>
        /// Restores every setting to its default, then applies, saves,
        /// and announces the change.
        /// </summary>
        public void ResetDefaults()
        {
            Current = SettingsData.Default();
            CommitChange();
        }

        private void CommitChange()
        {
            Current.Validate();
            Apply();
            Save();
            SettingsChanged?.Invoke();
        }

        private static SettingsData LoadFromDisk()
        {
            string path = SettingsPath;
            if (!File.Exists(path))
            {
                return SettingsData.Default();
            }

            try
            {
                string json = File.ReadAllText(path);
                SettingsData? loaded = JsonUtility.FromJson<SettingsData>(json);
                if (loaded == null)
                {
                    Quarantine(path);
                    Debug.LogWarning(
                        "SettingsManager: settings.json did not parse; "
                            + "it was moved to settings.json.corrupt and defaults are in use."
                    );
                    return SettingsData.Default();
                }

                return loaded;
            }
            catch (Exception exception)
            {
                Quarantine(path);
                Debug.LogWarning(
                    "SettingsManager: settings.json was unreadable ("
                        + exception.Message
                        + "); it was quarantined where possible and defaults are in use."
                );
                return SettingsData.Default();
            }
        }

        private static void Quarantine(string path)
        {
            try
            {
                string corruptPath = path + ".corrupt";
                if (File.Exists(corruptPath))
                {
                    File.Delete(corruptPath);
                }

                File.Move(path, corruptPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "SettingsManager: could not quarantine the corrupt settings file ("
                        + exception.Message
                        + ")."
                );
            }
        }

        private static void ReplaceFile(string temporaryPath, string finalPath)
        {
            if (File.Exists(finalPath))
            {
                File.Replace(temporaryPath, finalPath, null);
            }
            else
            {
                File.Move(temporaryPath, finalPath);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                // Best effort only: a leftover .tmp file is harmless and
                // is overwritten by the next save.
            }
        }

        private static float LinearToDecibels(float linearVolume)
        {
            float clamped = Mathf.Clamp(linearVolume, 0.0f, 1.0f);
            if (clamped <= 0.0f)
            {
                return SilenceDecibels;
            }

            return Mathf.Max(SilenceDecibels, 20.0f * Mathf.Log10(clamped));
        }
    }
}
