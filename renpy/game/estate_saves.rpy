# estate_saves.rpy -- Save-data versioning discipline for the shell.
#
# Copyright 2026 Nice Thoughts Ranch
# Licensed under the Apache License, Version 2.0.
#
# Honest scope:
#   Ren'Py owns the save format. File slots, serialisation, and rollback
#   are engine machinery and this file does not touch them. What the
#   estate CAN version is estate-managed data, in exactly two places:
#
#   1. Per-save: the store variable estate_save_version below. Because
#      it is a normal saved variable, it travels inside every save file.
#      label after_load compares it against ESTATE_FORMAT_VERSION and
#      runs migrations. This -- a saved store variable plus after_load --
#      is the documented Ren'Py route for per-save versioning.
#
#   2. Persistent: persistent.estate_persistent_version stamps the shape
#      of estate-managed persistent data (currently just the settings
#      dict). It is migrated at init time, before any label runs.
#
# Refusal policy (saves from a NEWER version):
#   Ren'Py will load its own data regardless; the estate cannot and
#   does not block the engine. The estate policy is: log the mismatch,
#   notify the player that the save came from a newer version, and
#   treat estate-managed payloads as absent (defaults) rather than
#   guessing at a future shape. No crash, no silent misread.

# Per-save estate version stamp. `default` means: a brand-new game gets
# the current version, and a loaded save keeps the version it was
# written with -- which is exactly what after_load needs to see.
default estate_save_version = 1

init python:
    def estate_migrate_save(from_version):
        """Migrate estate-managed per-save data to the current version.

        Typed intent: from_version is the estate_save_version found in
        the loaded save (an int, or None for a pre-versioning save).
        Steps run in order, one per version boundary, each written to
        be safe to re-run. After all steps, the stamp is set to
        ESTATE_FORMAT_VERSION. Add a step here whenever
        ESTATE_FORMAT_VERSION is bumped for a per-save shape change.
        """
        version = from_version

        # Step: (None or 0) -> 1.
        # Pre-versioning saves have no stamp and no estate label field.
        # The trivial real step for version 1 is to ensure the saved
        # estate display label exists, defaulting to the start label,
        # so later estate features can rely on the key being present.
        if version is None or version < 1:
            if getattr(renpy.store, "estate_save_label", None) is None:
                renpy.store.estate_save_label = "start"
            version = 1

        # Future steps go here, in ascending order, in this shape:
        #
        #   if version < 2:
        #       ... transform estate-managed saved data ...
        #       version = 2

        renpy.store.estate_save_version = ESTATE_FORMAT_VERSION
        renpy.log(
            "estate_saves: migrated save from version %r to %d."
            % (from_version, ESTATE_FORMAT_VERSION)
        )

    def estate_migrate_persistent(from_version):
        """Migrate estate-managed persistent data to the current version.

        Same step pattern as estate_migrate_save, applied to the
        persistent shape. Version 1 is the first stamped shape, so the
        (None or 0) -> 1 step only records the stamp; the settings dict
        itself is repaired by estate_validate_settings.
        """
        version = from_version
        if version is None or version < 1:
            version = 1
        persistent.estate_persistent_version = ESTATE_FORMAT_VERSION
        renpy.log(
            "estate_saves: migrated persistent data from version %r "
            "to %d." % (from_version, ESTATE_FORMAT_VERSION)
        )


init python:
    # Persistent versioning runs at init: if the stored stamp is older
    # than the current format, migrate before any label or screen reads
    # estate persistent data. A stamp from the future is logged and left
    # alone -- the settings repair pass still guarantees valid values.
    _estate_persistent_version = getattr(
        persistent, "estate_persistent_version", None
    )
    if _estate_persistent_version is None or (
        isinstance(_estate_persistent_version, int)
        and _estate_persistent_version < ESTATE_FORMAT_VERSION
    ):
        estate_migrate_persistent(_estate_persistent_version)
    elif isinstance(_estate_persistent_version, int) and (
        _estate_persistent_version > ESTATE_FORMAT_VERSION
    ):
        renpy.log(
            "estate_saves: persistent data is from a newer version "
            "(%d > %d); estate payloads treated as defaults."
            % (_estate_persistent_version, ESTATE_FORMAT_VERSION)
        )


label after_load:
    # Ren'Py jumps to label after_load after a save is loaded, with the
    # loaded store in place. This is the migration hook for per-save
    # estate data. Keep it fast and total: it must never raise, or the
    # player is left with a half-loaded game.
    python:
        _loaded_version = renpy.store.estate_save_version

        if isinstance(_loaded_version, int) and (
            _loaded_version > ESTATE_FORMAT_VERSION
        ):
            # Refusal path: newer save. Log, notify, and reset the
            # estate stamp so estate features fall back to defaults
            # instead of interpreting a future shape.
            renpy.log(
                "estate_saves: save version %d is newer than this "
                "build (%d); refusing estate payload."
                % (_loaded_version, ESTATE_FORMAT_VERSION)
            )
            renpy.notify(
                "This save was created by a newer version of the game."
            )
            renpy.store.estate_save_version = ESTATE_FORMAT_VERSION
        elif _loaded_version != ESTATE_FORMAT_VERSION:
            estate_migrate_save(_loaded_version)

    return
