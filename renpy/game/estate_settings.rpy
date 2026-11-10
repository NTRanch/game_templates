# estate_settings.rpy -- Estate settings model for the Ren'Py shell.
#
# Copyright 2026 Nice Thoughts Ranch
# Licensed under the Apache License, Version 2.0.
#
# Contract:
#   - Ren'Py preferences own master / music / sfx volumes, fullscreen,
#     window size, and text speed. This file does NOT duplicate them as
#     writable estate keys. It mirrors the mixer volumes read-only (see
#     estate_refresh_volume_mirror) for games that want one dict to read.
#   - Estate-owned keys are a FIXED set (ESTATE_SETTING_DEFAULTS).
#     Unknown keys are rejected: estate_set logs a warning and changes
#     nothing; estate_get returns None for an unknown key.
#   - Every value that enters persistent.estate_settings is validated.
#     A corrupt or wrong-typed stored value is replaced with its default
#     and the repair is logged with renpy.log. This reset-the-bad-key
#     behaviour is the engine equivalent of quarantining bad data:
#     persistent is engine-managed storage, there is no estate-owned
#     file to move aside, so the honest mapping is repair + log.
#   - Settings live at the persistent level, not the save level: a
#     settings change applies to the whole game, survives relaunch, and
#     is not rolled back with the story. estate_set is therefore safe to
#     call during gameplay (it never touches saved story variables).
#
# Bounds:
#   - Fixed key set, fixed types. Float keys are clamped to their
#     declared [minimum, maximum] range. No unbounded work: validation
#     iterates the fixed spec dict only.

init python:
    # Canonical defaults for every estate-owned key. Adding a key takes
    # THREE edits: this dict, ESTATE_SETTING_SPEC below, and the panel
    # in estate_screens.rpy. See SPEC.md, "WHAT TO ADD".
    ESTATE_SETTING_DEFAULTS = {
        "reduce_flashing": False,
        "screen_shake": 1.0,
        "subtitles": True,
    }

    # Validation spec: key -> (kind, minimum, maximum).
    #   kind is "bool" or "float". minimum / maximum are only used for
    #   "float" keys; bool keys must hold a real bool (an int or a
    #   string is treated as corrupt and repaired to the default).
    ESTATE_SETTING_SPEC = {
        "reduce_flashing": ("bool", None, None),
        "screen_shake": ("float", 0.0, 1.0),
        "subtitles": ("bool", None, None),
    }

    def estate_validate_value(key, value):
        """Return a valid value for key, repairing corrupt input.

        Typed intent: key is a str present in ESTATE_SETTING_SPEC,
        value is untrusted (it may come from a hand-edited or damaged
        persistent file). The return value always satisfies the spec:
        wrong types fall back to the default, floats are clamped.
        """
        kind, minimum, maximum = ESTATE_SETTING_SPEC[key]
        default = ESTATE_SETTING_DEFAULTS[key]

        if kind == "bool":
            # bool is a subclass of int in Python, so check bool first
            # and reject plain ints: 1 is not a valid bool setting.
            if isinstance(value, bool):
                return value
            return default

        if kind == "float":
            if isinstance(value, bool):
                return default
            if isinstance(value, (int, float)):
                result = float(value)
                if result < minimum:
                    return minimum
                if result > maximum:
                    return maximum
                return result
            return default

        # Unreachable while the spec only declares the kinds above.
        return default

    def estate_validate_settings():
        """Repair pass over persistent.estate_settings.

        Ensures the dict exists, holds exactly the known keys, and that
        every value satisfies its spec. Repairs are logged. Returns the
        repaired dict. Safe to call at init and after any bulk write.
        """
        stored = getattr(persistent, "estate_settings", None)
        if not isinstance(stored, dict):
            if stored is not None:
                renpy.log(
                    "estate_settings: stored settings were not a dict; "
                    "reset to defaults."
                )
            stored = {}

        repaired = {}
        for key in ESTATE_SETTING_DEFAULTS:
            if key in stored:
                value = estate_validate_value(key, stored[key])
                if value != stored[key] or type(value) is not type(
                    stored[key]
                ):
                    renpy.log(
                        "estate_settings: repaired key %r." % (key,)
                    )
                repaired[key] = value
            else:
                repaired[key] = ESTATE_SETTING_DEFAULTS[key]

        for key in stored:
            if key not in ESTATE_SETTING_DEFAULTS:
                renpy.log(
                    "estate_settings: dropped unknown key %r." % (key,)
                )

        persistent.estate_settings = repaired
        return repaired

    def estate_get(key):
        """Return the validated value for an estate key, or None.

        Unknown keys return None (and are the caller's bug to fix);
        known keys always return a spec-valid value because the stored
        dict only ever holds validated values.
        """
        if key not in ESTATE_SETTING_DEFAULTS:
            renpy.log("estate_settings: estate_get on unknown key %r."
                      % (key,))
            return None
        settings = getattr(persistent, "estate_settings", None)
        if not isinstance(settings, dict) or key not in settings:
            return ESTATE_SETTING_DEFAULTS[key]
        return settings[key]

    def estate_set(key, value):
        """Validate and store one estate setting, then apply it.

        Unknown keys are rejected with a logged warning and no change.
        Known keys are validated / clamped before they are stored, so
        persistent.estate_settings can never hold an out-of-spec value
        through this route.
        """
        if key not in ESTATE_SETTING_SPEC:
            renpy.log(
                "estate_settings: estate_set rejected unknown key %r."
                % (key,)
            )
            return
        settings = estate_validate_settings()
        settings[key] = estate_validate_value(key, value)
        persistent.estate_settings = settings
        estate_apply_settings()

    def estate_refresh_volume_mirror():
        """Refresh the read-only mixer-volume mirror dict.

        Ren'Py preferences own the real volume values. Games that want
        a single source of truth can read
        persistent.estate_volume_mirror instead of querying the mixer
        themselves. The mirror is refreshed, never written by games.

        Implementation note (honesty): the values are read from the
        preferences object Ren'Py keeps for the running game. The read
        is guarded: if a future Ren'Py version changes that surface,
        the mirror keeps its last values and the failure is logged
        rather than crashing the game. Verify against the Ren'Py 8
        preferences documentation when adopting a new SDK.
        """
        mirror = {
            "master": 1.0,
            "music": 1.0,
            "sfx": 1.0,
        }
        try:
            prefs = renpy.game.preferences
            get_volume = getattr(prefs, "get_volume", None)
            if get_volume is not None:
                # "main" is the master mixer name in Ren'Py; the game
                # mixers are "music" and "sfx" (the sound channel).
                mirror["master"] = float(get_volume("main"))
                mirror["music"] = float(get_volume("music"))
                mirror["sfx"] = float(get_volume("sfx"))
        except Exception:
            renpy.log(
                "estate_settings: volume mirror refresh failed; "
                "keeping previous mirror values."
            )
            existing = getattr(persistent, "estate_volume_mirror", None)
            if isinstance(existing, dict):
                return existing
        persistent.estate_volume_mirror = mirror
        return mirror

    def estate_apply_settings():
        """Push estate settings into the places that consume them.

        Current consumers:
          * Subtitles: the default is copied to the store flag
            estate_subtitles_enabled. A game's dialogue layer reads
            that flag to decide whether subtitle text is shown. This
            is a pattern hook, not a subtitle system: Ren'Py has no
            built-in subtitle track, so the game owns the rendering.
          * Volumes: the read-only mirror is refreshed from Ren'Py
            preferences (preferences stay the writable source).
        """
        renpy.store.estate_subtitles_enabled = estate_get("subtitles")
        estate_refresh_volume_mirror()

    def estate_cycle_screen_shake():
        """Cycle screen_shake through 0.0 -> 0.5 -> 1.0 -> 0.0.

        Used by the settings panel, which deliberately avoids a custom
        BarValue class. See estate_screens.rpy for the rationale.
        """
        current = estate_get("screen_shake")
        if current < 0.25:
            estate_set("screen_shake", 0.5)
        elif current < 0.75:
            estate_set("screen_shake", 1.0)
        else:
            estate_set("screen_shake", 0.0)


init -5 python:
    # Ensure the settings dict exists and is valid before any screen or
    # label can read it. Runs after estate_options.rpy (offset -10) and
    # before default-priority init blocks.
    estate_validate_settings()
    estate_apply_settings()
