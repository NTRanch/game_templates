# estate_options.rpy -- Estate options and constants for the Ren'Py shell.
#
# Copyright 2026 Nice Thoughts Ranch
# Licensed under the Apache License, Version 2.0.
#
# Contract:
#   - This file runs first (init offset -10) and owns only build identity
#     and estate-wide constants. It does not create screens, does not read
#     save data, and does not mutate persistent data.
#   - A game built on this shell edits the placeholders in this file and
#     flips estate_age_gate_enabled here. Everything else lives in the
#     other estate_*.rpy files.
#
# Ren'Py mapping note:
#   Ren'Py natively provides the main menu, the game menu (the pause
#   equivalent), file slots (the save / load screens), and preferences
#   (mixer volumes, fullscreen, text speed). This shell does NOT
#   reimplement any of those. See README.md for the full mapping table.

init -10 python:
    # -- Build identity (placeholders -- replace in your game) -----------
    #
    # config.name is shown on the main menu and in the window title.
    config.name = "Game Title"

    # config.version is the human-facing build version string.
    config.version = "1.0.0"

    # config.save_directory:
    #   Ren'Py derives the per-project save directory (which also holds
    #   the persistent file) from config.name by default. Set this
    #   explicitly in a real game so a title change does not orphan the
    #   player's saves and persistent data, for example:
    #
    #       config.save_directory = "GameTitle-20261109"
    #
    #   It is intentionally left unset in the template: the default
    #   derived name is the honest behaviour until a game picks a stable
    #   identifier. Once a game ships, never change this value.

    # -- Audio channels ---------------------------------------------------
    # The template ships no audio assets, but declares the standard
    # channels as present so preference screens show their sliders.
    config.has_sound = True
    config.has_music = True
    config.has_voice = False

    # -- Estate constants -------------------------------------------------
    #
    # ESTATE_FORMAT_VERSION is the current version of estate-managed data:
    #   * the persistent settings dict (see estate_settings.rpy), and
    #   * the per-save estate version stamp (see estate_saves.rpy).
    # Bump it only when the SHAPE of estate-managed data changes, and add
    # the matching migration step in the same change. Content-only changes
    # (new lines, new images) do not bump it.
    ESTATE_FORMAT_VERSION = 1

    # estate_age_gate_enabled:
    #   Store variable set at init time. Default False: the 18+ age gate
    #   is present but inert, and label splashscreen returns immediately.
    #   A game that ships adult content sets this to True here:
    #
    #       estate_age_gate_enabled = True
    #
    #   When True, the gate runs from label splashscreen before the main
    #   menu is reached, unless the player has already confirmed (the
    #   confirmation is kept in persistent.estate_age_confirmed).
    estate_age_gate_enabled = False
