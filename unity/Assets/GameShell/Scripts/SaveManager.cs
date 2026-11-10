#nullable enable
using System;
using System.IO;
using UnityEngine;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// Static save-slot store: four slots, one JSON file per slot,
    /// under <c>Application.persistentDataPath/saves</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contract: every method validates the slot name against
    /// <see cref="SlotNames"/>; unknown slots are refused, never
    /// turned into file names, so a bad slot string cannot escape the
    /// saves directory. Every method reports expected failures
    /// (missing file, corrupt file, unwritable disk) by returning
    /// false and logging a warning — never by throwing to the player.
    /// </para>
    /// <para>
    /// Writes are atomic: the envelope is written to
    /// <c>&lt;slot&gt;.json.tmp</c> and then moved over
    /// <c>&lt;slot&gt;.json</c>. A crash mid-write leaves at worst a
    /// stale <c>.tmp</c> file, never a truncated slot.
    /// </para>
    /// <para>
    /// Corruption handling: a slot whose JSON does not parse is
    /// quarantined to <c>&lt;slot&gt;.json.corrupt</c> and treated as
    /// empty. Other slots are never touched. A slot written by a
    /// newer build (<c>format_version</c> above the current version)
    /// is refused and treated as empty but is <em>not</em>
    /// quarantined — it is valid data for that newer build.
    /// </para>
    /// </remarks>
    public static class SaveManager
    {
        /// <summary>Subdirectory of the persistent data path holding saves.</summary>
        public const string SavesDirectoryName = "saves";

        /// <summary>File extension for slot files, without a directory.</summary>
        public const string SlotFileExtension = ".json";

        /// <summary>
        /// The complete, closed set of slot names: three manual slots
        /// and one autosave slot. This array is the contract; treat it
        /// as read-only.
        /// </summary>
        public static readonly string[] SlotNames = { "slot1", "slot2", "slot3", "auto" };

        /// <summary>Full path of the saves directory.</summary>
        public static string SavesDirectory
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, SavesDirectoryName);
            }
        }

        /// <summary>
        /// Writes a payload into a slot as a current-version envelope.
        /// </summary>
        /// <param name="slot">One of <see cref="SlotNames"/>.</param>
        /// <param name="payloadJson">
        /// The game's serialized state. Null is stored as the empty
        /// string.
        /// </param>
        /// <param name="label">
        /// Player-facing slot label. Null is stored as the empty
        /// string.
        /// </param>
        /// <param name="playtimeSeconds">
        /// Accumulated play time in seconds; negative values are
        /// stored as zero.
        /// </param>
        /// <returns>True when the slot file was replaced.</returns>
        public static bool Save(
            string? slot,
            string? payloadJson,
            string? label,
            long playtimeSeconds
        )
        {
            if (!IsValidSlot(slot))
            {
                Debug.LogWarning("SaveManager: refusing to save to an unknown slot.");
                return false;
            }

            string path = GetSlotPath(slot!);
            string temporaryPath = path + ".tmp";
            try
            {
                Directory.CreateDirectory(SavesDirectory);
                var envelope = new SaveEnvelope
                {
                    format_version = SaveEnvelope.CurrentFormatVersion,
                    saved_at_unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    playtime_seconds = playtimeSeconds < 0 ? 0 : playtimeSeconds,
                    label = label ?? "",
                    payload_json = payloadJson ?? "",
                };
                string json = JsonUtility.ToJson(envelope, true);
                File.WriteAllText(temporaryPath, json);
                ReplaceFile(temporaryPath, path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "SaveManager: save to slot '" + slot + "' failed (" + exception.Message + ")."
                );
                TryDelete(temporaryPath);
                return false;
            }
        }

        /// <summary>
        /// Loads and migrates the envelope in a slot.
        /// </summary>
        /// <param name="slot">One of <see cref="SlotNames"/>.</param>
        /// <param name="envelope">
        /// The loaded envelope on success; null on any failure.
        /// </param>
        /// <returns>
        /// True when a usable envelope was loaded. False for a missing
        /// slot, an unknown slot, a corrupt slot (quarantined), or a
        /// refused format version.
        /// </returns>
        public static bool TryLoad(string? slot, out SaveEnvelope? envelope)
        {
            envelope = null;
            if (!IsValidSlot(slot))
            {
                Debug.LogWarning("SaveManager: refusing to load an unknown slot.");
                return false;
            }

            SaveEnvelope? loaded = ReadEnvelope(slot!, quarantineCorrupt: true);
            if (loaded == null)
            {
                return false;
            }

            if (!SaveMigrations.Migrate(loaded))
            {
                return false;
            }

            envelope = loaded;
            return true;
        }

        /// <summary>
        /// Whether a slot holds a loadable save. A corrupt slot counts
        /// as empty (and is quarantined as a side effect of the check,
        /// via the same read path as <see cref="TryLoad"/>).
        /// </summary>
        /// <param name="slot">One of <see cref="SlotNames"/>.</param>
        /// <returns>True when <see cref="TryLoad"/> would succeed.</returns>
        public static bool HasSave(string? slot)
        {
            if (!IsValidSlot(slot))
            {
                return false;
            }

            return TryLoad(slot, out SaveEnvelope? _);
        }

        /// <summary>Whether any of the four slots holds a loadable save.</summary>
        /// <returns>True when at least one slot loads.</returns>
        public static bool HasAnySave()
        {
            for (int index = 0; index < SlotNames.Length; index++)
            {
                if (HasSave(SlotNames[index]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Reads slot metadata (label, save time, play time, version)
        /// for menu display, without trusting or returning the payload
        /// to game code paths that only render a label.
        /// </summary>
        /// <param name="slot">One of <see cref="SlotNames"/>.</param>
        /// <param name="metadata">
        /// A migrated envelope whose metadata fields are populated on
        /// success; null on failure. Callers rendering a menu should
        /// read only <c>label</c>, <c>saved_at_unix</c>, and
        /// <c>playtime_seconds</c> from it.
        /// </param>
        /// <returns>True when the slot holds a usable save.</returns>
        public static bool GetMetadata(string? slot, out SaveEnvelope? metadata)
        {
            metadata = null;
            if (!IsValidSlot(slot))
            {
                return false;
            }

            return TryLoad(slot, out metadata);
        }

        /// <summary>
        /// Deletes a slot file. Deleting an absent slot succeeds:
        /// the postcondition (no save in the slot) already holds.
        /// </summary>
        /// <param name="slot">One of <see cref="SlotNames"/>.</param>
        /// <returns>True when the slot is empty after the call.</returns>
        public static bool Delete(string? slot)
        {
            if (!IsValidSlot(slot))
            {
                Debug.LogWarning("SaveManager: refusing to delete an unknown slot.");
                return false;
            }

            try
            {
                string path = GetSlotPath(slot!);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "SaveManager: delete of slot '" + slot + "' failed (" + exception.Message + ")."
                );
                return false;
            }
        }

        /// <summary>
        /// Whether a string names one of the four slots. Comparison is
        /// ordinal and case-sensitive: slot names are file names.
        /// </summary>
        /// <param name="slot">The candidate slot name.</param>
        /// <returns>True when the name is in <see cref="SlotNames"/>.</returns>
        public static bool IsValidSlot(string? slot)
        {
            if (string.IsNullOrEmpty(slot))
            {
                return false;
            }

            for (int index = 0; index < SlotNames.Length; index++)
            {
                if (string.Equals(slot, SlotNames[index], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Full path of a slot file. Callers must pass a name accepted
        /// by <see cref="IsValidSlot"/>; this method does not validate.
        /// </summary>
        /// <param name="slot">A valid slot name.</param>
        /// <returns>The slot's path inside the saves directory.</returns>
        public static string GetSlotPath(string slot)
        {
            return Path.Combine(SavesDirectory, slot + SlotFileExtension);
        }

        private static SaveEnvelope? ReadEnvelope(string slot, bool quarantineCorrupt)
        {
            string path = GetSlotPath(slot);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                SaveEnvelope? loaded = JsonUtility.FromJson<SaveEnvelope>(json);
                if (loaded == null)
                {
                    if (quarantineCorrupt)
                    {
                        Quarantine(path, slot);
                    }

                    return null;
                }

                return loaded;
            }
            catch (Exception exception)
            {
                if (quarantineCorrupt)
                {
                    Quarantine(path, slot);
                }

                Debug.LogWarning(
                    "SaveManager: slot '" + slot + "' was unreadable (" + exception.Message + ")."
                );
                return null;
            }
        }

        private static void Quarantine(string path, string slot)
        {
            try
            {
                string corruptPath = path + ".corrupt";
                if (File.Exists(corruptPath))
                {
                    File.Delete(corruptPath);
                }

                File.Move(path, corruptPath);
                Debug.LogWarning(
                    "SaveManager: slot '"
                        + slot
                        + "' was corrupt; its file was moved to "
                        + Path.GetFileName(corruptPath)
                        + " and the slot is treated as empty."
                );
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "SaveManager: could not quarantine slot '"
                        + slot
                        + "' ("
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
                // Best effort only: a leftover .tmp file is harmless
                // and is overwritten by the next save.
            }
        }
    }
}
