//=============================================================================
// NTR_EstateSettings.js
//=============================================================================
/*:
 * @target MZ
 * @plugindesc Estate settings model: estate-owned keys, validated defaults, and a volume bridge onto ConfigManager's native volumes.
 * @author Nice Thoughts Ranch
 *
 * @command SetVolume
 * @text Set Volume
 * @desc Set an estate volume channel (master, music, or sfx) to a value from 0 to 100 and apply it immediately.
 *
 * @arg channel
 * @text Channel
 * @type select
 * @option Master
 * @value master
 * @option Music
 * @value music
 * @option SFX
 * @value sfx
 * @default master
 * @desc Which estate volume channel to set.
 *
 * @arg value
 * @text Value
 * @type number
 * @min 0
 * @max 100
 * @default 100
 * @desc Volume value from 0 (silent) to 100 (full).
 *
 * @command SetFullscreen
 * @text Set Fullscreen
 * @desc Request or leave fullscreen on desktop (NW.js) deployments. Has no effect on web deployments.
 *
 * @arg enabled
 * @text Enabled
 * @type boolean
 * @on Fullscreen
 * @off Windowed
 * @default true
 * @desc True to enter fullscreen, false to leave it.
 *
 * @command OpenOptions
 * @text Open Options
 * @desc Open the native options scene (Scene_Options). Note: if the estate model owns volumes, see SPEC.md for hiding the native volume rows.
 *
 * @help
 * NTR Estate Settings
 * ---------------------------------------------------------------------------
 * This plugin does NOT replace the title screen, the options scene, the
 * save/load scenes, or ConfigManager persistence. RPG Maker MZ provides
 * all of those natively. This plugin adds only the estate settings MODEL:
 *
 *   - Estate-owned keys that ConfigManager does not own:
 *       reduce_flashing, screen_shake, subtitles
 *   - Estate volume channels (master / music / sfx) that compose with
 *     MZ's four native volumes through a documented bridge (see below).
 *   - Display fields for contract parity: window_width, window_height,
 *     vsync, fullscreen.
 *
 * Every key has a default and validation (clamp / type repair). Estate
 * values persist through ConfigManager's makeData / applyData, so they
 * are stored in the engine-owned config file (config.rpgsave in file
 * mode) together with the native settings. Never edit that file by hand.
 *
 * Volume bridge (the template's chosen mapping — implement no other):
 *   estate music_volume  -> ConfigManager.bgmVolume and bgsVolume
 *   estate sfx_volume    -> ConfigManager.seVolume and meVolume
 *   estate master_volume -> scales all four native values at apply time:
 *       bgmVolume = round(music * master / 100)
 *       bgsVolume = round(music * master / 100)
 *       meVolume  = round(sfx   * master / 100)
 *       seVolume  = round(sfx   * master / 100)
 * Changing any estate volume recomputes all four native values. Once a
 * game adopts this model, the four native volume rows in Scene_Options
 * fight the model (native edits are overwritten on the next estate
 * apply). SPEC.md documents, as text guidance only, how to hide those
 * rows. This plugin intentionally ships no Scene_Options code.
 *
 * Reading state from game code / events:
 *   window.EstateSettings.state.reduce_flashing  // boolean
 *   window.EstateSettings.state.screen_shake     // 0..100 intensity
 *   window.EstateSettings.state.subtitles        // boolean
 *   window.EstateSettings.get("music_volume")    // validated value
 *   window.EstateSettings.setVolume("music", 80) // set + apply + save
 * The game, not this plugin, decides what reduce_flashing, screen_shake,
 * and subtitles DO in a scene. This plugin owns the values only.
 *
 * Display notes (honest engine limits):
 *   - window_width / window_height are read at plugin load and applied
 *     to Graphics.width / Graphics.height. In MZ, resizing therefore
 *     takes effect at the next launch, not mid-session.
 *     A value of 0 means "leave the engine / project setting alone".
 *   - vsync is stored for cross-engine contract parity and is an
 *     engine no-op in MZ: MZ renders through requestAnimationFrame,
 *     which is vsync-bound by NW.js / the browser.
 *   - fullscreen applies through the NW.js window API on desktop
 *     deployments only, guarded at runtime. On web deployments the
 *     command logs a warning and changes only the stored value.
 *
 * Plugin commands:
 *   Set Volume     — set master / music / sfx to 0..100 and apply.
 *   Set Fullscreen — enter or leave fullscreen (desktop only, see above).
 *   Open Options   — push the native Scene_Options scene.
 */

(() => {
    "use strict";

    const PLUGIN_NAME = "NTR_EstateSettings";

    // -----------------------------------------------------------------
    // Contract: defaults + validation spec.
    //
    // Every estate key appears exactly once in DEFAULTS and once in
    // SPEC. SPEC fields:
    //   configKey — the property name used on ConfigManager and in the
    //               object returned by ConfigManager.makeData.
    //   type      — "volume" | "percent" | "bool" | "dimension"
    //   min / max — inclusive numeric bounds for numeric types.
    // Adding a key also requires a makeData / applyData entry below
    // and a reader in game code. See SPEC.md, "What to add".
    // -----------------------------------------------------------------
    const DEFAULTS = {
        master_volume: 100,
        music_volume: 100,
        sfx_volume: 100,
        reduce_flashing: false,
        screen_shake: 100,
        subtitles: true,
        window_width: 0,
        window_height: 0,
        vsync: true,
        fullscreen: false
    };

    const SPEC = {
        master_volume:   { configKey: "estateMasterVolume",   type: "volume",    min: 0, max: 100 },
        music_volume:    { configKey: "estateMusicVolume",    type: "volume",    min: 0, max: 100 },
        sfx_volume:      { configKey: "estateSfxVolume",      type: "volume",    min: 0, max: 100 },
        reduce_flashing: { configKey: "estateReduceFlashing", type: "bool" },
        screen_shake:    { configKey: "estateScreenShake",    type: "percent",   min: 0, max: 100 },
        subtitles:       { configKey: "estateSubtitles",      type: "bool" },
        window_width:    { configKey: "estateWindowWidth",    type: "dimension", min: 0, max: 7680 },
        window_height:   { configKey: "estateWindowHeight",   type: "dimension", min: 0, max: 4320 },
        vsync:           { configKey: "estateVsync",          type: "bool" },
        fullscreen:      { configKey: "estateFullscreen",     type: "bool" }
    };

    const ESTATE_KEYS = Object.keys(DEFAULTS);

    // Contract: clampNumber(value, min, max) returns a finite number in
    // [min, max], or min when value is not a finite number.
    // Bounds: no iteration; pure.
    function clampNumber(value, min, max) {
        const n = Number(value);
        if (!Number.isFinite(n)) {
            return min;
        }
        return Math.min(max, Math.max(min, n));
    }

    // Contract: validateValue(key, value) returns a repaired value for
    // key: integers clamped to SPEC bounds for numeric types, strict
    // booleans for bool types (only true / false survive; anything else
    // falls back to the default), and the default for unknown keys.
    // Bounds: no iteration over unbounded data; pure.
    function validateValue(key, value) {
        const spec = SPEC[key];
        if (!spec) {
            return undefined;
        }
        if (spec.type === "bool") {
            return typeof value === "boolean" ? value : DEFAULTS[key];
        }
        return Math.floor(clampNumber(value, spec.min, spec.max));
    }

    // Contract: validate(candidate) returns a complete, valid state
    // object. Missing or wrongly-typed fields are repaired from
    // DEFAULTS; unknown fields are dropped. A non-object candidate
    // yields a copy of DEFAULTS. Never throws.
    // Bounds: iterates ESTATE_KEYS only (fixed size: 10 keys).
    function validate(candidate) {
        const source = candidate && typeof candidate === "object" ? candidate : {};
        const result = {};
        for (const key of ESTATE_KEYS) {
            result[key] = Object.prototype.hasOwnProperty.call(source, key)
                ? validateValue(key, source[key])
                : DEFAULTS[key];
        }
        return result;
    }

    // The live estate state. Games read window.EstateSettings.state.
    // It is always a validated object (see validate).
    const state = validate(null);

    // Contract: readFromConfigManager() copies ConfigManager.estate*
    // properties into state, repairing each through validateValue.
    // Missing properties keep the current (initially default) value.
    // Bounds: iterates ESTATE_KEYS only.
    function readFromConfigManager() {
        const candidate = {};
        for (const key of ESTATE_KEYS) {
            const configKey = SPEC[key].configKey;
            if (typeof ConfigManager[configKey] !== "undefined") {
                candidate[key] = ConfigManager[configKey];
            }
        }
        const repaired = validate(Object.assign({}, state, candidate));
        for (const key of ESTATE_KEYS) {
            state[key] = repaired[key];
            ConfigManager[SPEC[key].configKey] = repaired[key];
        }
    }

    // Contract: applyVolumes() recomputes the four native ConfigManager
    // volumes from the estate channels per the documented bridge:
    //   bgm / bgs = round(music * master / 100)
    //   me  / se  = round(sfx   * master / 100)
    // Bounds: pure arithmetic; no iteration.
    function applyVolumes() {
        const music = Math.floor(state.music_volume * state.master_volume / 100);
        const sfx = Math.floor(state.sfx_volume * state.master_volume / 100);
        ConfigManager.bgmVolume = music;
        ConfigManager.bgsVolume = music;
        ConfigManager.meVolume = sfx;
        ConfigManager.seVolume = sfx;
    }

    // Contract: applyWindowSize() applies a non-zero window_width /
    // window_height to Graphics. Called once at plugin load; in MZ the
    // change is therefore visible from the next launch onward when the
    // value was changed mid-session and saved. Zero means no override.
    // Bounds: pure; no iteration.
    function applyWindowSize() {
        if (typeof Graphics === "undefined") {
            return;
        }
        if (state.window_width > 0) {
            Graphics.width = state.window_width;
        }
        if (state.window_height > 0) {
            Graphics.height = state.window_height;
        }
    }

    // Contract: applyFullscreen(enabled) enters or leaves fullscreen
    // through the NW.js window API when running in a desktop (NW.js)
    // deployment. On web deployments there is no MZ core fullscreen
    // API; this function warns and returns false. Returns true when a
    // native fullscreen call was made. Never throws.
    // Bounds: single guarded native call.
    function applyFullscreen(enabled) {
        state.fullscreen = enabled === true;
        if (typeof nw !== "undefined" && nw.Window && typeof nw.Window.get === "function") {
            const win = nw.Window.get();
            if (state.fullscreen) {
                win.enterFullscreen();
            } else {
                win.leaveFullscreen();
            }
            return true;
        }
        console.warn("[NTR_EstateSettings] Fullscreen is only available in desktop (NW.js) deployments.");
        return false;
    }

    // Contract: apply() pushes the whole validated state to the engine:
    // native volumes via the bridge, ConfigManager.estate* properties,
    // and the readable window.EstateSettings.state (the same object).
    // Fullscreen and window size are NOT applied here; they are applied
    // by their own commands / at load, so a routine volume change never
    // toggles the window. Bounds: iterates ESTATE_KEYS only.
    function apply() {
        for (const key of ESTATE_KEYS) {
            ConfigManager[SPEC[key].configKey] = state[key];
        }
        applyVolumes();
    }

    // Contract: saveConfig() persists ConfigManager (native + estate
    // keys together) through the engine's own save route, when present.
    // Bounds: single call; failure is logged, never thrown.
    function saveConfig() {
        if (typeof ConfigManager !== "undefined" && typeof ConfigManager.save === "function") {
            try {
                ConfigManager.save();
            } catch (e) {
                console.warn("[NTR_EstateSettings] ConfigManager.save() failed:", e);
            }
        }
    }

    // Contract: setVolume(channel, value) sets one estate volume
    // channel ("master" | "music" | "sfx") to a clamped 0..100 value,
    // re-applies the bridge, and persists the config. Unknown channels
    // are rejected with a warning and no state change. Returns the
    // applied value, or null when the channel was rejected.
    // Bounds: pure plus one apply + one save.
    function setVolume(channel, value) {
        const keyByChannel = {
            master: "master_volume",
            music: "music_volume",
            sfx: "sfx_volume"
        };
        const key = keyByChannel[channel];
        if (!key) {
            console.warn("[NTR_EstateSettings] Unknown volume channel:", channel);
            return null;
        }
        state[key] = validateValue(key, value);
        apply();
        saveConfig();
        return state[key];
    }

    // Contract: get(key) returns the validated live value for an estate
    // key, or undefined for an unknown key.
    function get(key) {
        return Object.prototype.hasOwnProperty.call(state, key) ? state[key] : undefined;
    }

    // Contract: set(key, value) validates and stores one estate key,
    // re-applies, and persists. Unknown keys are rejected with a
    // warning and no state change. Returns the stored value, or null
    // when the key was rejected. Fullscreen and window size take their
    // engine effect only through Set Fullscreen / the next launch;
    // see applyFullscreen and applyWindowSize.
    function set(key, value) {
        if (!SPEC[key]) {
            console.warn("[NTR_EstateSettings] Unknown estate setting key:", key);
            return null;
        }
        state[key] = validateValue(key, value);
        apply();
        saveConfig();
        return state[key];
    }

    // Public estate namespace. This is the plugin's only global.
    const EstateSettings = {
        DEFAULTS: Object.freeze(Object.assign({}, DEFAULTS)),
        SPEC: Object.freeze(SPEC),
        state: state,
        validate: validate,
        get: get,
        set: set,
        setVolume: setVolume,
        apply: apply,
        applyFullscreen: applyFullscreen,
        loadFromConfig: readFromConfigManager
    };
    window.EstateSettings = EstateSettings;

    // Seed ConfigManager with the estate defaults so a first run (no
    // config file yet) already carries valid estate values. The native
    // ConfigManager.load() at boot overwrites these through applyData
    // when a saved config exists.
    for (const key of ESTATE_KEYS) {
        ConfigManager[SPEC[key].configKey] = DEFAULTS[key];
    }
    applyWindowSize();

    //------------------------------------------------------------------
    // ConfigManager persistence — alias, never replace.
    // The originals run first / are fully preserved; the estate keys
    // ride the same makeData / applyData round-trip as native keys.
    //------------------------------------------------------------------
    const _ConfigManager_makeData = ConfigManager.makeData;
    ConfigManager.makeData = function () {
        const config = _ConfigManager_makeData.call(this);
        for (const key of ESTATE_KEYS) {
            config[SPEC[key].configKey] = state[key];
        }
        return config;
    };

    const _ConfigManager_applyData = ConfigManager.applyData;
    ConfigManager.applyData = function (config) {
        _ConfigManager_applyData.call(this, config);
        // Estate keys: read with typeof checks, repair through the
        // validator, and re-apply the volume bridge so estate volumes
        // win over the native values the original applyData just set.
        // That ordering is the bridge contract documented in @help.
        const candidate = {};
        for (const key of ESTATE_KEYS) {
            const configKey = SPEC[key].configKey;
            if (config && typeof config[configKey] !== "undefined") {
                candidate[key] = config[configKey];
            }
        }
        const repaired = validate(Object.assign({}, DEFAULTS, candidate));
        for (const key of ESTATE_KEYS) {
            state[key] = repaired[key];
        }
        apply();
        applyWindowSize();
    };

    //------------------------------------------------------------------
    // Plugin commands
    //------------------------------------------------------------------
    PluginManager.registerCommand(PLUGIN_NAME, "SetVolume", function (args) {
        const channel = String(args.channel || "master").toLowerCase();
        const value = Number(args.value);
        setVolume(channel, value);
    });

    PluginManager.registerCommand(PLUGIN_NAME, "SetFullscreen", function (args) {
        const enabled = args.enabled === true || args.enabled === "true";
        state.fullscreen = enabled;
        apply();
        saveConfig();
        applyFullscreen(enabled);
    });

    PluginManager.registerCommand(PLUGIN_NAME, "OpenOptions", function () {
        if (typeof Scene_Options !== "undefined" && typeof SceneManager !== "undefined") {
            SceneManager.push(Scene_Options);
        } else {
            console.warn("[NTR_EstateSettings] Scene_Options is not available in this context.");
        }
    });
})();
