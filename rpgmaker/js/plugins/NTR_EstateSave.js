//=============================================================================
// NTR_EstateSave.js
//=============================================================================
/*:
 * @target MZ
 * @plugindesc Estate save envelope: a versioned metadata block inside MZ save contents, corrupt-isolated so a bad block never blocks a load.
 * @author Nice Thoughts Ranch
 *
 * @help
 * NTR Estate Save
 * ---------------------------------------------------------------------------
 * This plugin does NOT replace the save or load scenes, DataManager's
 * game-state contents, or StorageManager. RPG Maker MZ provides all of
 * those natively. This plugin adds only a save ENVELOPE: a metadata
 * block stored inside the object DataManager.makeSaveContents returns.
 *
 * On save, the contents gain:
 *   contents.estate = {
 *       formatVersion:   1,
 *       savedAtUnix:     <integer, seconds since the Unix epoch>,
 *       playtimeSeconds: <integer, from $gameSystem.playtime()>,
 *       label:           <string, current map display name or "New Game">
 *   }
 *
 * On load, the block is validated with corrupt-isolation:
 *   - A missing block is repaired to a default block.
 *   - Wrongly-typed fields are repaired field by field.
 *   - A block from a NEWER formatVersion than this plugin understands
 *     is treated as absent (DataManager.estateInfo() returns null) and
 *     a warning is logged. The game's own contents are never touched.
 *   - A block from an OLDER formatVersion runs the migration chain in
 *     EstateSaveMigrations before validation.
 * In every case the game's own contents load exactly as the engine
 * made them. A bad estate block degrades estate features to defaults;
 * it NEVER blocks a load.
 *
 * The estate block is metadata only. Never store game-critical state
 * only in the estate block: game state belongs in the native $game*
 * objects, which MZ saves and loads itself.
 *
 * Savefile list data:
 *   DataManager.makeSavefileInfo is aliased to add, per save:
 *       info.estateLabel         — the same label as the envelope
 *       info.estateFormatVersion — the envelope format version
 *   A game may read DataManager.savefileInfo(fileId) — through the
 *   engine's global info — and draw estateLabel in its own
 *   Window_SavefileList customisation. This plugin intentionally ships
 *   no window code: Window_SavefileList draw signatures vary between
 *   projects, and a fragile window alias would risk the load screen.
 *   See SPEC.md for the data-availability note.
 *
 * Reading the loaded envelope from game code:
 *   const info = DataManager.estateInfo(); // validated block or null
 *
 * Requires: nothing. Recommended load order: after
 * NTR_EstateSettings.js (Plugin Manager order is load order).
 */

(() => {
    "use strict";

    // The current estate envelope format version. Bump this — and add
    // one migration step — whenever the block shape changes. Never
    // change the shape without a version bump. See SPEC.md.
    const ESTATE_FORMAT_VERSION = 1;

    // Contract: defaultEstateBlock() returns a fresh, valid block for
    // the current format version with neutral values. Used when a save
    // carries no estate block (for example a save written before this
    // plugin was installed). Pure; new object on every call.
    function defaultEstateBlock() {
        return {
            formatVersion: ESTATE_FORMAT_VERSION,
            savedAtUnix: 0,
            playtimeSeconds: 0,
            label: ""
        };
    }

    // Contract: currentLabel() returns the human label for a save made
    // right now: the current map's display name, or "New Game" when
    // there is no map or the map has no display name. Never throws.
    // Bounds: pure reads of $gameMap.
    function currentLabel() {
        try {
            if (typeof $gameMap !== "undefined" && $gameMap && typeof $gameMap.displayName === "function") {
                const name = $gameMap.displayName();
                if (typeof name === "string" && name.length > 0) {
                    return name;
                }
            }
        } catch (e) {
            console.warn("[NTR_EstateSave] Could not read map display name:", e);
        }
        return "New Game";
    }

    // Contract: currentPlaytimeSeconds() returns the engine playtime
    // in whole seconds, or 0 when $gameSystem is not available (for
    // example on the title screen). Never throws.
    function currentPlaytimeSeconds() {
        try {
            if (typeof $gameSystem !== "undefined" && $gameSystem && typeof $gameSystem.playtime === "function") {
                const seconds = Math.floor(Number($gameSystem.playtime()));
                return Number.isFinite(seconds) && seconds > 0 ? seconds : 0;
            }
        } catch (e) {
            console.warn("[NTR_EstateSave] Could not read playtime:", e);
        }
        return 0;
    }

    // Contract: makeEstateBlock() builds the block written at save
    // time from live engine values. Pure snapshot; no side effects.
    function makeEstateBlock() {
        return {
            formatVersion: ESTATE_FORMAT_VERSION,
            savedAtUnix: Math.floor(Date.now() / 1000),
            playtimeSeconds: currentPlaytimeSeconds(),
            label: currentLabel()
        };
    }

    //------------------------------------------------------------------
    // Migrations.
    //
    // Contract: EstateSaveMigrations.migrate(block) upgrades a block
    // from block.formatVersion to ESTATE_FORMAT_VERSION by applying
    // one step per version, in order: steps[v] upgrades a version-v
    // block to version v+1. Each step mutates and returns the block
    // and must set block.formatVersion to the new version. A missing
    // step stops the chain and returns what exists; validation after
    // migration repairs any remaining defects. When a step is absent
    // for a needed version, the block is returned unchanged apart from
    // steps that did run. Never throws on field values; a non-object
    // input is returned unchanged for the validator to reject.
    // Bounds: at most (ESTATE_FORMAT_VERSION) steps; fixed and small.
    //------------------------------------------------------------------
    const EstateSaveMigrations = {
        steps: {
            // Version 0 -> 1: pre-release blocks carried no label.
            // Give them the neutral default; all other fields are
            // repaired by validation after migration.
            0: function (block) {
                if (typeof block.label !== "string") {
                    block.label = "";
                }
                block.formatVersion = 1;
                return block;
            }
        },
        migrate: function (block) {
            if (!block || typeof block !== "object") {
                return block;
            }
            let version = Math.floor(Number(block.formatVersion));
            if (!Number.isFinite(version) || version < 0) {
                version = 0;
                block.formatVersion = 0;
            }
            while (version < ESTATE_FORMAT_VERSION) {
                const step = this.steps[version];
                if (typeof step !== "function") {
                    console.warn("[NTR_EstateSave] No estate migration step for version " + version + "; stopping migration.");
                    break;
                }
                step(block);
                const next = Math.floor(Number(block.formatVersion));
                if (!Number.isFinite(next) || next <= version) {
                    console.warn("[NTR_EstateSave] Estate migration step for version " + version + " did not advance the version; stopping migration.");
                    break;
                }
                version = next;
            }
            return block;
        }
    };
    window.EstateSaveMigrations = EstateSaveMigrations;

    // Contract: validateEstateBlock(raw) applies corrupt-isolation and
    // returns either a complete, valid block for the current version,
    // or null when the block must be treated as absent:
    //   - non-object input          -> a fresh default block
    //   - formatVersion > current   -> null (newer save, older plugin;
    //                                  logged; game contents untouched)
    //   - formatVersion < current   -> migrated, then field-repaired
    //   - current version           -> field-repaired
    // Field repair: non-finite / negative numbers become 0 (except
    // formatVersion, which migration/validation owns), a non-string
    // label becomes "". Never throws.
    // Bounds: fixed field set; at most one migration chain pass.
    function validateEstateBlock(raw) {
        if (!raw || typeof raw !== "object" || Array.isArray(raw)) {
            return defaultEstateBlock();
        }
        const version = Math.floor(Number(raw.formatVersion));
        if (Number.isFinite(version) && version > ESTATE_FORMAT_VERSION) {
            console.warn(
                "[NTR_EstateSave] Estate block formatVersion " + version +
                " is newer than supported version " + ESTATE_FORMAT_VERSION +
                "; treating the estate block as absent. Game contents are unaffected."
            );
            return null;
        }
        const working = Object.assign({}, raw);
        if (!Number.isFinite(version) || version < ESTATE_FORMAT_VERSION) {
            EstateSaveMigrations.migrate(working);
        }
        const repairedVersion = Math.floor(Number(working.formatVersion));
        if (Number.isFinite(repairedVersion) && repairedVersion > ESTATE_FORMAT_VERSION) {
            return null;
        }
        const savedAt = Math.floor(Number(working.savedAtUnix));
        const playtime = Math.floor(Number(working.playtimeSeconds));
        return {
            formatVersion: ESTATE_FORMAT_VERSION,
            savedAtUnix: Number.isFinite(savedAt) && savedAt > 0 ? savedAt : 0,
            playtimeSeconds: Number.isFinite(playtime) && playtime > 0 ? playtime : 0,
            label: typeof working.label === "string" ? working.label : ""
        };
    }

    // The validated block from the most recently extracted save, or
    // null before any load / when the loaded block was treated as
    // absent. Exposed through DataManager.estateInfo().
    let lastLoadedEstateInfo = null;

    //------------------------------------------------------------------
    // DataManager — alias, never replace.
    // Each original is preserved and runs; the estate block rides the
    // same contents / info objects the engine already moves around.
    //------------------------------------------------------------------
    const _DataManager_makeSaveContents = DataManager.makeSaveContents;
    DataManager.makeSaveContents = function () {
        const contents = _DataManager_makeSaveContents.call(this);
        contents.estate = makeEstateBlock();
        return contents;
    };

    const _DataManager_extractSaveContents = DataManager.extractSaveContents;
    DataManager.extractSaveContents = function (contents) {
        // Validate (and, when safe, repair) the estate block BEFORE
        // the original runs, on a shallow copy of the estate field
        // only. The game's own contents pass to the original exactly
        // as stored; validation never throws and never blocks a load.
        try {
            if (contents && typeof contents === "object") {
                const validated = validateEstateBlock(contents.estate);
                contents.estate = validated;
                lastLoadedEstateInfo = validated;
            } else {
                lastLoadedEstateInfo = null;
            }
        } catch (e) {
            console.warn("[NTR_EstateSave] Estate block validation failed; continuing without it:", e);
            lastLoadedEstateInfo = null;
        }
        return _DataManager_extractSaveContents.call(this, contents);
    };

    const _DataManager_makeSavefileInfo = DataManager.makeSavefileInfo;
    DataManager.makeSavefileInfo = function () {
        const info = _DataManager_makeSavefileInfo.call(this);
        info.estateLabel = currentLabel();
        info.estateFormatVersion = ESTATE_FORMAT_VERSION;
        return info;
    };

    // Contract: DataManager.estateInfo() returns the validated estate
    // block from the most recently loaded save, a default block after
    // a load whose save carried no block, or null when no save has
    // been loaded yet or the loaded block was from a newer format
    // version and is being treated as absent. Callers must null-check.
    DataManager.estateInfo = function () {
        return lastLoadedEstateInfo;
    };

    window.EstateSave = Object.freeze({
        FORMAT_VERSION: ESTATE_FORMAT_VERSION,
        makeBlock: makeEstateBlock,
        validateBlock: validateEstateBlock,
        defaultBlock: defaultEstateBlock
    });
})();
