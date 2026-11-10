#nullable enable
using UnityEngine;

namespace NiceThoughtsRanch.GameShell
{
    /// <summary>
    /// The save-format migration chain used by <see cref="SaveManager"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rule one is additive-first: prefer a new optional field with a
    /// sensible default over a version bump. A bump is justified only
    /// when an existing field changes meaning or shape.
    /// </para>
    /// <para>
    /// To add a step when the version does move: increment
    /// <see cref="SaveEnvelope.CurrentFormatVersion"/>, add a
    /// <c>StepV{n}ToV{n + 1}</c> method that rewrites the envelope in
    /// place and sets <c>format_version</c> to the new version, and
    /// extend the chain in <see cref="Migrate"/>. Each step handles
    /// exactly one version increment so an old save walks every
    /// intermediate shape. A step returns false when the data cannot
    /// be carried forward; the slot is then refused, never guessed at.
    /// </para>
    /// <para>
    /// A version newer than
    /// <see cref="SaveEnvelope.CurrentFormatVersion"/> is not a
    /// migration problem: it was written by a newer build, and the
    /// caller refuses it (see <see cref="SaveManager.TryLoad"/>).
    /// </para>
    /// </remarks>
    public static class SaveMigrations
    {
        /// <summary>
        /// Migrates an envelope in place up to the current format
        /// version.
        /// </summary>
        /// <param name="envelope">The envelope to migrate.</param>
        /// <returns>
        /// True when the envelope is now at the current version and may
        /// be used; false when it must be refused (null envelope,
        /// negative version, newer version, or a failed step).
        /// </returns>
        public static bool Migrate(SaveEnvelope? envelope)
        {
            if (envelope == null)
            {
                Debug.LogWarning("SaveMigrations: refusing a null envelope.");
                return false;
            }

            if (envelope.format_version < 0)
            {
                Debug.LogWarning(
                    "SaveMigrations: refusing an envelope with format_version "
                        + envelope.format_version
                        + "."
                );
                return false;
            }

            if (envelope.format_version > SaveEnvelope.CurrentFormatVersion)
            {
                Debug.LogWarning(
                    "SaveMigrations: refusing format_version "
                        + envelope.format_version
                        + "; this build understands up to "
                        + SaveEnvelope.CurrentFormatVersion
                        + "."
                );
                return false;
            }

            while (envelope.format_version < SaveEnvelope.CurrentFormatVersion)
            {
                bool stepped;
                switch (envelope.format_version)
                {
                    case 0:
                        stepped = StepV0ToV1(envelope);
                        break;
                    default:
                        Debug.LogWarning(
                            "SaveMigrations: no migration step exists from format_version "
                                + envelope.format_version
                                + "."
                        );
                        return false;
                }

                if (!stepped)
                {
                    return false;
                }
            }

            // A migrated (or current) envelope still passes through the
            // same null repair the v0 -> v1 step performs, so TryLoad
            // can rely on non-null strings at the current version.
            envelope.label ??= "";
            envelope.payload_json ??= "";
            return true;
        }

        /// <summary>
        /// Step v0 to v1: version 0 saves predate the
        /// <see cref="SaveEnvelope.label"/> field, so a label that
        /// deserializes as null defaults to the empty string.
        /// </summary>
        /// <param name="envelope">A version 0 envelope.</param>
        /// <returns>Always true; this step cannot fail.</returns>
        private static bool StepV0ToV1(SaveEnvelope envelope)
        {
            envelope.label ??= "";
            envelope.payload_json ??= "";
            envelope.format_version = 1;
            return true;
        }
    }
}
