extends Node
## Headless smoke test for the Game Shell template.
##
## Run: godot --headless --path . res://tests/smoke.tscn
## Prints PASS/FAIL per case, then "OK smoke" and quit(0) when all
## cases pass, or quit(1) otherwise. Half the cases are validation
## (the contract working) and half are adversarial (corrupt data,
## version skew, isolation between slots).
## Every path reaches quit: run() is deferred one frame so the
## autoloads have settled, and no case awaits or can hang.

const SMOKE_SLOT: String = "__smoke__"
const SCENE_PATHS: Array = [
	"res://scenes/title_screen.tscn",
	"res://scenes/settings_screen.tscn",
	"res://scenes/pause_menu.tscn",
	"res://scenes/age_gate.tscn",
	"res://scenes/game_stub.tscn",
	"res://components/slot_list.tscn",
]

var _failures: Array[String] = []
var _slot1_created_by_smoke: bool = false


func _ready() -> void:
	call_deferred("_run")


func _check(case_name: String, condition: bool, why: String = "") -> bool:
	if condition:
		print("PASS ", case_name)
	else:
		print("FAIL ", case_name, ": ", why)
		_failures.append(case_name)
	return condition


func _write_text(path: String, text: String) -> Error:
	var file: FileAccess = FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		return FileAccess.get_open_error()
	file.store_string(text)
	file.close()
	return OK


func _remove_if_exists(path: String) -> void:
	var absolute: String = ProjectSettings.globalize_path(path)
	if FileAccess.file_exists(path):
		DirAccess.remove_absolute(absolute)


func _run() -> void:
	await get_tree().process_frame
	_case_settings_defaults()
	_case_settings_round_trip()
	_case_settings_corrupt()
	_case_save_load_round_trip()
	_case_corrupt_slot_isolation()
	_case_version_high()
	_case_version_low()
	_case_scenes_instantiate()
	_cleanup()
	if _failures.is_empty():
		print("OK smoke")
		get_tree().quit(0)
	else:
		printerr("FAIL smoke: %d case(s) failed" % _failures.size())
		get_tree().quit(1)


# --- Validation cases -------------------------------------------------

func _case_settings_defaults() -> void:
	Settings.reset_defaults()
	var ok: bool = true
	ok = ok and Settings.master_volume >= 0.0 and Settings.master_volume <= 1.0
	ok = ok and Settings.music_volume == 1.0
	ok = ok and Settings.sfx_volume == 1.0
	ok = ok and Settings.window_width >= 640 and Settings.window_width <= 7680
	ok = ok and Settings.window_height >= 360 and Settings.window_height <= 4320
	ok = ok and Settings.screen_shake >= 0.0 and Settings.screen_shake <= 1.0
	_check("settings_defaults_sane", ok, "a default is missing or out of bounds")


func _case_settings_round_trip() -> void:
	Settings.reset_defaults()
	Settings.set_value("music_volume", 0.5)
	Settings.music_volume = 0.9
	Settings.load_settings()
	_check("settings_round_trip", is_equal_approx(Settings.music_volume, 0.5), "music_volume reloaded as %f, expected 0.5" % Settings.music_volume)


func _case_save_load_round_trip() -> void:
	var err: Error = SaveSystem.save_game(SMOKE_SLOT, {"k": 7}, "smoke", 42)
	if not _check("save_load_round_trip_save", err == OK, "save_game returned %d" % err):
		return
	var envelope: Dictionary = SaveSystem.load_game(SMOKE_SLOT)
	var ok: bool = not envelope.is_empty()
	ok = ok and str(envelope.get("label", "")) == "smoke"
	ok = ok and int(envelope.get("playtime_seconds", -1)) == 42
	var payload: Variant = envelope.get("payload", {})
	ok = ok and typeof(payload) == TYPE_DICTIONARY
	if typeof(payload) == TYPE_DICTIONARY:
		ok = ok and int((payload as Dictionary).get("k", -1)) == 7
	_check("save_load_round_trip", ok, "envelope did not round-trip: %s" % str(envelope))


func _case_scenes_instantiate() -> void:
	var ok: bool = true
	var why: String = ""
	for path: String in SCENE_PATHS:
		var packed: Variant = load(path)
		if packed == null or not (packed is PackedScene):
			ok = false
			why = "load failed for " + path
			break
		var instance: Node = (packed as PackedScene).instantiate()
		if instance == null:
			ok = false
			why = "instantiate failed for " + path
			break
		add_child(instance)
		remove_child(instance)
		instance.free()
	_check("scenes_instantiate", ok, why)


# --- Adversarial cases ------------------------------------------------

func _case_settings_corrupt() -> void:
	Settings.reset_defaults()
	Settings.set_value("music_volume", 0.25)
	var write_err: Error = _write_text(Settings.SETTINGS_PATH, "[unclosed\nkey=1\n")
	if write_err != OK:
		_check("settings_corrupt_quarantine", false, "could not write corrupt settings fixture")
		return
	Settings.load_settings()
	var corrupt_exists: bool = FileAccess.file_exists(Settings.SETTINGS_PATH + ".corrupt")
	var ok: bool = corrupt_exists and is_equal_approx(Settings.music_volume, 1.0)
	_check("settings_corrupt_quarantine", ok, "corrupt=%s music_volume=%f" % [str(corrupt_exists), Settings.music_volume])


func _case_corrupt_slot_isolation() -> void:
	if not SaveSystem.has_save("slot1"):
		var marker_err: Error = SaveSystem.save_game("slot1", {"marker": 1}, "smoke-marker", 3)
		if marker_err == OK:
			_slot1_created_by_smoke = true
	SaveSystem.save_game(SMOKE_SLOT, {"k": 1}, "before-corrupt", 1)
	var slot_path: String = "user://saves/" + SMOKE_SLOT + ".json"
	_write_text(slot_path, "not json at all {{")
	var loaded: Dictionary = SaveSystem.load_game(SMOKE_SLOT)
	var meta: Dictionary = SaveSystem.slot_metadata("slot1")
	var ok: bool = loaded.is_empty()
	ok = ok and FileAccess.file_exists(slot_path + ".corrupt")
	ok = ok and not meta.is_empty()
	_check("corrupt_slot_isolation", ok, "loaded=%s meta=%s" % [str(loaded), str(meta)])


func _case_version_high() -> void:
	var slot_path: String = "user://saves/" + SMOKE_SLOT + ".json"
	var envelope: Dictionary = {"format_version": 99, "saved_at_unix": 1, "playtime_seconds": 0, "label": "future", "payload": {}}
	_write_text(slot_path, JSON.stringify(envelope))
	var loaded: Dictionary = SaveSystem.load_game(SMOKE_SLOT)
	var ok: bool = loaded.is_empty() and FileAccess.file_exists(slot_path)
	_check("version_skew_high_refused", ok, "loaded=%s exists=%s" % [str(loaded), str(FileAccess.file_exists(slot_path))])


func _case_version_low() -> void:
	var slot_path: String = "user://saves/" + SMOKE_SLOT + ".json"
	var envelope: Dictionary = {"format_version": 0, "saved_at_unix": 1, "playtime_seconds": 2, "payload": {"a": 1}}
	_write_text(slot_path, JSON.stringify(envelope))
	var loaded: Dictionary = SaveSystem.load_game(SMOKE_SLOT)
	var ok: bool = not loaded.is_empty() and str(loaded.get("label", "missing")) == ""
	ok = ok and int(loaded.get("format_version", -1)) == 1
	_check("version_skew_low_migrates", ok, "loaded=%s" % str(loaded))


func _cleanup() -> void:
	SaveSystem.delete_save(SMOKE_SLOT)
	_remove_if_exists("user://saves/" + SMOKE_SLOT + ".json.corrupt")
	_remove_if_exists("user://saves/" + SMOKE_SLOT + ".json.tmp")
	_remove_if_exists(Settings.SETTINGS_PATH + ".corrupt")
	if _slot1_created_by_smoke:
		SaveSystem.delete_save("slot1")
	Settings.reset_defaults()
	Settings.save_settings()
	Settings.apply_all()
