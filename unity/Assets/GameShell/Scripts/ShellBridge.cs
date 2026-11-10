#nullable enable
using System;
using UnityEngine;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// The one integration seam between the shell and the game.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shell owns slots, files, and menus; it does not know the
    /// game's state shape. The game registers three delegates here:
    /// one that serializes its current state to a JSON string, one
    /// that reports accumulated play time in seconds, and one that
    /// restores state from a JSON string previously produced by the
    /// first delegate. <see cref="GameFlow"/> calls through this
    /// bridge when the player saves or loads a slot.
    /// </para>
    /// <para>
    /// Registration is a single static slot, replaced wholesale by
    /// each call to
    /// <see cref="RegisterPayloadProvider"/>. Register once, early
    /// (for example from the gameplay scene's bootstrap), and call
    /// <see cref="ClearPayloadProvider"/> when the gameplay scene is
    /// torn down so a stale provider cannot outlive its scene.
    /// </para>
    /// <para>
    /// Play mode note: Unity does not reset statics between Play
    /// sessions unless domain reload is on, so a provider registered
    /// in a previous Play session can survive into the next one.
    /// Re-registering on scene load (rather than only once) keeps the
    /// seam honest in the editor.
    /// </para>
    /// </remarks>
    public static class ShellBridge
    {
        private static Func<string>? _getPayloadJson;
        private static Func<long>? _getPlaytimeSeconds;
        private static Action<string>? _restorePayload;

        /// <summary>Whether a complete payload provider is registered.</summary>
        public static bool HasProvider
        {
            get
            {
                return _getPayloadJson != null
                    && _getPlaytimeSeconds != null
                    && _restorePayload != null;
            }
        }

        /// <summary>
        /// Registers the game's payload provider, replacing any
        /// previous registration.
        /// </summary>
        /// <param name="getPayloadJson">
        /// Serializes the game's current state to a JSON string. Must
        /// not return null; an empty game state should return an empty
        /// JSON object (<c>"{}"</c>).
        /// </param>
        /// <param name="getPlaytimeSeconds">
        /// Returns accumulated play time in seconds.
        /// </param>
        /// <param name="restorePayload">
        /// Restores game state from a payload string written earlier by
        /// <paramref name="getPayloadJson"/>.
        /// </param>
        public static void RegisterPayloadProvider(
            Func<string> getPayloadJson,
            Func<long> getPlaytimeSeconds,
            Action<string> restorePayload
        )
        {
            if (getPayloadJson == null)
            {
                throw new ArgumentNullException(nameof(getPayloadJson));
            }

            if (getPlaytimeSeconds == null)
            {
                throw new ArgumentNullException(nameof(getPlaytimeSeconds));
            }

            if (restorePayload == null)
            {
                throw new ArgumentNullException(nameof(restorePayload));
            }

            _getPayloadJson = getPayloadJson;
            _getPlaytimeSeconds = getPlaytimeSeconds;
            _restorePayload = restorePayload;
        }

        /// <summary>Removes the registered provider, if any.</summary>
        public static void ClearPayloadProvider()
        {
            _getPayloadJson = null;
            _getPlaytimeSeconds = null;
            _restorePayload = null;
        }

        /// <summary>
        /// Reads the current payload and play time from the game.
        /// </summary>
        /// <param name="payloadJson">The serialized game state.</param>
        /// <param name="playtimeSeconds">Accumulated play time.</param>
        /// <returns>
        /// True when a provider is registered and returned a non-null
        /// payload; false otherwise. A provider exception is logged
        /// and reported as false — saving must fail closed, never
        /// crash the menu.
        /// </returns>
        public static bool TryGetPayload(out string payloadJson, out long playtimeSeconds)
        {
            payloadJson = "";
            playtimeSeconds = 0;

            if (_getPayloadJson == null || _getPlaytimeSeconds == null)
            {
                Debug.LogWarning(
                    "ShellBridge: no payload provider is registered; "
                        + "the game must call RegisterPayloadProvider before saving."
                );
                return false;
            }

            try
            {
                string? payload = _getPayloadJson();
                if (payload == null)
                {
                    Debug.LogWarning(
                        "ShellBridge: the payload provider returned null; "
                            + "treating the save as failed."
                    );
                    return false;
                }

                payloadJson = payload;
                playtimeSeconds = _getPlaytimeSeconds();
                if (playtimeSeconds < 0)
                {
                    playtimeSeconds = 0;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "ShellBridge: the payload provider threw (" + exception.Message + ")."
                );
                return false;
            }
        }

        /// <summary>
        /// Hands a loaded payload back to the game for restoration.
        /// </summary>
        /// <param name="payloadJson">A payload string from a save slot.</param>
        /// <returns>
        /// True when a restore handler is registered and accepted the
        /// payload; false when no handler is registered or the handler
        /// threw. The shell reports the load as failed when this
        /// returns false, even though the file itself was valid.
        /// </returns>
        public static bool TryRestore(string? payloadJson)
        {
            if (_restorePayload == null)
            {
                Debug.LogWarning(
                    "ShellBridge: no restore handler is registered; "
                        + "the game must call RegisterPayloadProvider before loading."
                );
                return false;
            }

            try
            {
                _restorePayload(payloadJson ?? "");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "ShellBridge: the restore handler threw (" + exception.Message + ")."
                );
                return false;
            }
        }
    }
}
