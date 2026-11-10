# estate_screens.rpy -- Estate-added screens for the Ren'Py shell.
#
# Copyright 2026 Nice Thoughts Ranch
# Licensed under the Apache License, Version 2.0.
#
# Contract:
#   - This file adds only what Ren'Py does not provide: the 18+ age
#     gate and a panel for the estate-owned settings keys.
#   - The main menu, game menu, save / load screens, and the Ren'Py
#     preferences screen are engine screens. They are not redefined
#     here and must not be reimplemented by games using this shell;
#     customise them through gui.rpy and the standard screen names in
#     the game's own screens.rpy instead.
#   - Screens stay deliberately simple: no custom BarValue classes, no
#     animation. Values change through Function actions that call the
#     validated estate_set route, never by writing persistent directly.

init python:
    def estate_confirm_age():
        """Record the player's 18+ confirmation in persistent data.

        Persistent (not a saved variable) is the right home: the
        confirmation is a property of the player on this machine, not
        of one save file, so it must survive starting a new game.
        """
        persistent.estate_age_confirmed = True


screen estate_age_gate():
    # Modal: the player cannot click through to anything behind it.
    modal True

    frame:
        # Centre the gate panel on a plain dark backing.
        xalign 0.5
        yalign 0.5
        padding (40, 40)

        vbox:
            spacing 20

            text "Adults Only (18+)" size 40 xalign 0.5
            text (
                "This game contains adult content intended for "
                "players who are 18 years of age or older."
            ) xalign 0.5
            text (
                "By confirming, you state that you are at least 18 "
                "years old and that viewing adult content is legal "
                "where you live."
            ) xalign 0.5

            hbox:
                xalign 0.5
                spacing 30

                textbutton "I am 18 or older":
                    action [
                        Function(estate_confirm_age),
                        Return(True),
                    ]

                # Quit is the standard Ren'Py screen action for
                # leaving the game; confirm=False because the gate
                # itself is the confirmation step.
                textbutton "Exit":
                    action Quit(confirm=False)


screen estate_settings_panel():
    # A panel for the estate-owned keys only. Volumes, fullscreen, and
    # text speed belong to the Ren'Py preferences screen; they are not
    # duplicated here.
    #
    # Screen shake uses a cycling textbutton (0.0 / 0.5 / 1.0) instead
    # of a bar: a proper bar needs a custom BarValue class, and this
    # template prefers a simple, obviously valid screen. A real game
    # can swap the cycle button for a bar backed by its own BarValue
    # subclass that reads and writes through estate_get / estate_set.
    frame:
        xalign 0.5
        yalign 0.5
        padding (40, 40)

        vbox:
            spacing 16

            text "Estate Settings" size 32 xalign 0.5

            hbox:
                spacing 20
                text "Reduce flashing"
                textbutton "[estate_get('reduce_flashing')]":
                    action Function(
                        estate_set,
                        "reduce_flashing",
                        not estate_get("reduce_flashing"),
                    )

            hbox:
                spacing 20
                text "Screen shake"
                textbutton "[estate_get('screen_shake')]":
                    action Function(estate_cycle_screen_shake)

            hbox:
                spacing 20
                text "Subtitles"
                textbutton "[estate_get('subtitles')]":
                    action Function(
                        estate_set,
                        "subtitles",
                        not estate_get("subtitles"),
                    )

            textbutton "Close":
                xalign 0.5
                action Return()


label estate_age_gate_flow:
    # Flow contract for the gate:
    #   * Gate disabled, or already confirmed -> return at once.
    #   * Otherwise the gate screen runs modally. Confirm records the
    #     confirmation (estate_confirm_age, called by the screen) and
    #     returns; the caller continues to the main menu.
    #   * Decline quits the game from the screen itself (Quit action),
    #     so reaching the line after `call screen` without a
    #     confirmation means the screen returned abnormally; the safe
    #     response is to quit rather than fall through to the menu.
    if not estate_age_gate_enabled:
        return

    if persistent.estate_age_confirmed:
        return

    call screen estate_age_gate

    if not persistent.estate_age_confirmed:
        $ renpy.quit()

    return
