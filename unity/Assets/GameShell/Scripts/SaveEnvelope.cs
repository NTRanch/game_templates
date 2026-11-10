#nullable enable
using System;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// Flat, versioned envelope for one save slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The envelope is deliberately flat because
    /// <c>UnityEngine.JsonUtility</c> has no dictionary support and
    /// only handles a narrow, predictable shape reliably. The game
    /// serializes its own state however it likes and stores the result
    /// as the opaque string <see cref="payload_json"/>; the shell
    /// never parses the payload.
    /// </para>
    /// <para>
    /// Prefer adding new optional fields over bumping
    /// <see cref="CurrentFormatVersion"/>: an optional field that an
    /// older build ignores is compatible in both directions, while a
    /// version bump forces every older save through the migration
    /// chain in <see cref="SaveMigrations"/>. Bump the version only
    /// when the meaning or the required shape of an existing field
    /// changes.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class SaveEnvelope
    {
        /// <summary>The format version this build writes and accepts.</summary>
        public const int CurrentFormatVersion = 1;

        /// <summary>Envelope format version, written by the saving build.</summary>
        public int format_version = CurrentFormatVersion;

        /// <summary>Save time as Unix seconds (UTC).</summary>
        public long saved_at_unix = 0;

        /// <summary>Accumulated play time at save time, in seconds.</summary>
        public long playtime_seconds = 0;

        /// <summary>
        /// Player-facing slot label. Never null in a validated envelope:
        /// <see cref="SaveMigrations"/> repairs a missing label to the
        /// empty string, and writers should do the same.
        /// </summary>
        public string label = "";

        /// <summary>
        /// Opaque JSON document owned by the game, not by the shell.
        /// The shell stores and returns it verbatim.
        /// </summary>
        public string payload_json = "";
    }
}
