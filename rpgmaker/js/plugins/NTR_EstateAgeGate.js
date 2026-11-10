//=============================================================================
// NTR_EstateAgeGate.js
//=============================================================================
/*:
 * @target MZ
 * @plugindesc Optional age-confirmation gate shown before the title screen. Disabled by default.
 * @author Nice Thoughts Ranch
 *
 * @param AgeGateEnabled
 * @text Enable Age Gate
 * @type boolean
 * @on Enabled
 * @off Disabled
 * @default false
 * @desc When enabled, players must confirm they are 18 or older once before reaching the title screen.
 *
 * @help
 * NTR Estate Age Gate
 * ---------------------------------------------------------------------------
 * An optional, self-contained age-confirmation gate. It ships DISABLED
 * (plugin parameter AgeGateEnabled = false) and changes nothing until
 * a project turns it on.
 *
 * When enabled and not yet confirmed on this machine, the gate scene
 * appears after boot, before the title screen, with two choices:
 *   "I am 18 or older" — records confirmation and continues to the
 *                        title screen.
 *   "Exit"             — exits the game (SceneManager.exit()).
 *
 * Confirmation is stored in the localStorage flag
 * "estate_age_confirmed" (value "1"). localStorage exists in both
 * NW.js desktop deployments and web deployments of MZ. Clearing the
 * browser / NW.js storage, or removing that flag, makes the gate show
 * again. The flag is a convenience record of a past confirmation on
 * this machine; it is not identity verification.
 *
 * This gate states an age threshold; it does not verify identity or
 * date of birth, and it is not a substitute for a storefront's own age
 * rating or a project's legal review.
 *
 * Integration: the plugin aliases Scene_Boot.prototype.start. After
 * the original boot completes (which loads the databases and moves to
 * the title screen), the alias redirects to the gate scene when the
 * gate applies. Confirming goes to Scene_Title. No core method is
 * replaced; the original boot always runs first.
 */

(() => {
    "use strict";

    const PLUGIN_NAME = "NTR_EstateAgeGate";
    const STORAGE_KEY = "estate_age_confirmed";

    const parameters = PluginManager.parameters(PLUGIN_NAME);
    const GATE_ENABLED = parameters.AgeGateEnabled === true || parameters.AgeGateEnabled === "true";

    // Contract: isConfirmed() returns true when the localStorage flag
    // records a past confirmation on this machine. Any storage error
    // (blocked storage, private mode) is treated as NOT confirmed, so
    // the gate errs toward showing. Never throws.
    function isConfirmed() {
        try {
            return typeof localStorage !== "undefined" && localStorage.getItem(STORAGE_KEY) === "1";
        } catch (e) {
            return false;
        }
    }

    // Contract: recordConfirmation() stores the flag. Storage failure
    // is logged and ignored: the player still proceeds this session,
    // and the gate will simply show again next launch. Never throws.
    function recordConfirmation() {
        try {
            if (typeof localStorage !== "undefined") {
                localStorage.setItem(STORAGE_KEY, "1");
            }
        } catch (e) {
            console.warn("[NTR_EstateAgeGate] Could not store the confirmation flag:", e);
        }
    }

    // Contract: gateApplies() is true exactly when the gate is enabled
    // by parameter and no confirmation is recorded on this machine.
    function gateApplies() {
        return GATE_ENABLED && !isConfirmed();
    }
    window.EstateAgeGate = Object.freeze({ applies: gateApplies, isConfirmed: isConfirmed });

    //------------------------------------------------------------------
    // Gate scene: a minimal Scene_Base with one command window.
    //------------------------------------------------------------------
    function Window_EstateAgeCommand() {
        this.initialize.apply(this, arguments);
    }
    Window_EstateAgeCommand.prototype = Object.create(Window_Command.prototype);
    Window_EstateAgeCommand.prototype.constructor = Window_EstateAgeCommand;

    Window_EstateAgeCommand.prototype.makeCommandList = function () {
        this.addCommand("I am 18 or older", "confirm");
        this.addCommand("Exit", "exit");
    };

    function Scene_EstateAgeGate() {
        this.initialize.apply(this, arguments);
    }
    Scene_EstateAgeGate.prototype = Object.create(Scene_Base.prototype);
    Scene_EstateAgeGate.prototype.constructor = Scene_EstateAgeGate;

    Scene_EstateAgeGate.prototype.create = function () {
        Scene_Base.prototype.create.call(this);
        this.createMessageWindow();
        this.createCommandWindow();
    };

    Scene_EstateAgeGate.prototype.createMessageWindow = function () {
        const rect = new Rectangle(0, 0, Graphics.boxWidth, Graphics.boxHeight);
        this._messageWindow = new Window_Base(rect);
        this._messageWindow.drawText(
            "This game contains adult content.",
            24, 24, Graphics.boxWidth - 48, "left"
        );
        this._messageWindow.drawText(
            "You must be 18 or older to play.",
            24, 60, Graphics.boxWidth - 48, "left"
        );
        this.addWindow(this._messageWindow);
    };

    Scene_EstateAgeGate.prototype.createCommandWindow = function () {
        const width = 320;
        // Two command rows: Window_Command item height (36) x 2 plus
        // the window padding (12 top + 12 bottom).
        const height = 2 * 36 + 24;
        const rect = new Rectangle(
            (Graphics.boxWidth - width) / 2,
            Graphics.boxHeight - height - 48,
            width,
            height
        );
        this._commandWindow = new Window_EstateAgeCommand(rect);
        this._commandWindow.setHandler("confirm", this.commandConfirm.bind(this));
        this._commandWindow.setHandler("exit", this.commandExit.bind(this));
        this._commandWindow.setHandler("cancel", this.commandExit.bind(this));
        this.addWindow(this._commandWindow);
    };

    Scene_EstateAgeGate.prototype.commandConfirm = function () {
        recordConfirmation();
        SceneManager.goto(Scene_Title);
    };

    Scene_EstateAgeGate.prototype.commandExit = function () {
        SceneManager.exit();
    };

    window.Scene_EstateAgeGate = Scene_EstateAgeGate;
    window.Window_EstateAgeCommand = Window_EstateAgeCommand;

    //------------------------------------------------------------------
    // Boot routing — alias, never replace. The original boot runs to
    // completion first (databases loaded, title scene queued); only
    // then does the gate redirect, when it applies.
    //------------------------------------------------------------------
    const _Scene_Boot_start = Scene_Boot.prototype.start;
    Scene_Boot.prototype.start = function () {
        _Scene_Boot_start.call(this);
        if (gateApplies()) {
            SceneManager.goto(Scene_EstateAgeGate);
        }
    };
})();
