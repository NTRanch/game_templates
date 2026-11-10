# script.rpy -- Demo entry point for the Ren'Py shell template.
#
# Copyright 2026 Nice Thoughts Ranch
# Licensed under the Apache License, Version 2.0.
#
# This file is a demonstration, not content. A game built on the shell
# replaces label start with its own opening and deletes the demo labels.
# The estate_*.rpy files are the shell; this file only shows how the
# flow contract is wired.

label splashscreen:
    # Ren'Py runs label splashscreen, when defined, before the main
    # menu. The estate age gate hooks in here: when the gate is enabled
    # (estate_age_gate_enabled in estate_options.rpy) and the player has
    # not confirmed yet, the gate runs first and the main menu is only
    # reached after confirmation. When the gate is disabled or already
    # confirmed, the flow label returns immediately and this label adds
    # no delay and no screen.
    call estate_age_gate_flow
    return


label start:
    # Flow contract, as Ren'Py provides it:
    #   * Title / main menu: the engine main menu screen. "Start" jumps
    #     to label start (this label). Nothing to implement.
    #   * Continue: the engine loads the most recent save. Nothing to
    #     implement; label after_load (estate_saves.rpy) runs estate
    #     migrations after any load.
    #   * New Game: also label start -- a game distinguishes a fresh
    #     start from a load by its own variables, not by a second entry
    #     point.
    #   * Settings: the engine preferences screen for volumes,
    #     fullscreen, and text speed; the estate panel
    #     (screen estate_settings_panel) covers the estate-owned keys.
    #   * Quit: the engine main menu / window close. Nothing to
    #     implement.
    #   * Pause: the engine game menu, opened with Esc / right-click
    #     during play. There is no pause label to write; see
    #     label estate_demo_pause_note.

    scene black

    "This is the shell demo for the Nice Thoughts Ranch Ren'Py game template."
    "It exists to prove the flow contract: menus, settings, saves, and the optional age gate."
    "A real game replaces this label with its own opening scene."

    menu:
        "Begin the demo.":
            jump estate_demo_body

        "Open the estate settings panel.":
            call screen estate_settings_panel
            jump estate_demo_body

    return


label estate_demo_body:
    "You are in the demo body. Press Esc or right-click now: that opens the Ren'Py game menu."
    "The game menu is the pause equivalent -- save, load, preferences, and return all live there, provided by the engine."
    "From the game menu you can also save here and reload: label after_load in estate_saves.rpy will run its version check when you do."
    jump estate_demo_pause_note


label estate_demo_pause_note:
    "That is the whole demo. There is deliberately no custom pause screen, main menu, or save screen in this template."
    "Ren'Py provides those, and the shell only adds what the engine lacks: the settings model, save versioning, and the age gate."
    return
