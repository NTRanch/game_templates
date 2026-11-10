#nullable enable
using System;
using UnityEngine;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// Serializable settings payload stored as JSON in
    /// <c>settings.json</c> under <c>Application.persistentDataPath</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Field names are the on-disk JSON names. They use snake_case so the
    /// file stays readable and stable across engines in this template
    /// family. Do not rename a field without a settings migration.
    /// </para>
    /// <para>
    /// Contract: every field has a default, and <see cref="Validate"/>
    /// clamps or repairs every field. Callers must run
    /// <see cref="Validate"/> after deserialization and before applying
    /// or persisting the values. Null is never a valid state for this
    /// object once validated.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class SettingsData
    {
        /// <summary>Smallest accepted volume value (inclusive).</summary>
        public const float MinVolume = 0.0f;

        /// <summary>Largest accepted volume value (inclusive).</summary>
        public const float MaxVolume = 1.0f;

        /// <summary>Smallest accepted screen-shake value (inclusive).</summary>
        public const float MinScreenShake = 0.0f;

        /// <summary>Largest accepted screen-shake value (inclusive).</summary>
        public const float MaxScreenShake = 1.0f;

        /// <summary>Smallest accepted window width in pixels (inclusive).</summary>
        public const int MinWindowWidth = 640;

        /// <summary>Largest accepted window width in pixels (inclusive).</summary>
        public const int MaxWindowWidth = 7680;

        /// <summary>Smallest accepted window height in pixels (inclusive).</summary>
        public const int MinWindowHeight = 360;

        /// <summary>Largest accepted window height in pixels (inclusive).</summary>
        public const int MaxWindowHeight = 4320;

        /// <summary>Master output volume, in the range 0..1.</summary>
        public float master_volume = 1.0f;

        /// <summary>Music volume, in the range 0..1.</summary>
        public float music_volume = 1.0f;

        /// <summary>Sound-effect volume, in the range 0..1.</summary>
        public float sfx_volume = 1.0f;

        /// <summary>Whether the game starts in fullscreen mode.</summary>
        public bool fullscreen = false;

        /// <summary>Window width in pixels, in the range 640..7680.</summary>
        public int window_width = 1280;

        /// <summary>Window height in pixels, in the range 360..4320.</summary>
        public int window_height = 720;

        /// <summary>Whether vertical synchronisation is enabled.</summary>
        public bool vsync = true;

        /// <summary>
        /// Accessibility option: when true, the game should reduce or
        /// suppress flashing effects. The shell only stores the flag;
        /// the game owns the visual response.
        /// </summary>
        public bool reduce_flashing = false;

        /// <summary>Screen-shake intensity multiplier, in the range 0..1.</summary>
        public float screen_shake = 1.0f;

        /// <summary>Whether subtitles are shown where the game provides them.</summary>
        public bool subtitles = true;

        /// <summary>
        /// Creates a settings object holding every canonical default.
        /// </summary>
        /// <returns>A new, already-valid <see cref="SettingsData"/>.</returns>
        public static SettingsData Default()
        {
            return new SettingsData();
        }

        /// <summary>
        /// Clamps every numeric field to its documented range and
        /// repairs non-finite values to the field default.
        /// </summary>
        /// <remarks>
        /// Booleans need no repair. This method never throws: a settings
        /// file the player (or a text editor) damaged must degrade to
        /// usable values, not to a crash. It is safe to call repeatedly.
        /// </remarks>
        public void Validate()
        {
            master_volume = ClampFinite(master_volume, MinVolume, MaxVolume, 1.0f);
            music_volume = ClampFinite(music_volume, MinVolume, MaxVolume, 1.0f);
            sfx_volume = ClampFinite(sfx_volume, MinVolume, MaxVolume, 1.0f);
            screen_shake = ClampFinite(
                screen_shake,
                MinScreenShake,
                MaxScreenShake,
                1.0f
            );
            window_width = Mathf.Clamp(window_width, MinWindowWidth, MaxWindowWidth);
            window_height = Mathf.Clamp(
                window_height,
                MinWindowHeight,
                MaxWindowHeight
            );
        }

        private static float ClampFinite(
            float value,
            float minimum,
            float maximum,
            float fallback
        )
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return fallback;
            }

            return Mathf.Clamp(value, minimum, maximum);
        }
    }
}
