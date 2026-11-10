extends Node
## Versioned JSON save system for the Game Shell template.
##
## Envelope: {format_version, saved_at_unix, playtime_seconds,
## label, payload}. Save data is untrusted input: every load
## validates shape by control flow, quarantines corrupt files to a
## .corrupt sibling, refuses newer format versions, and migrates
## older versions through _migrate_envelope.
## Prefer additive optional payload fields with defaults over a
## format_version bump; bump only when the envelope shape changes.

signal save_completed(slot: String)
signal load_completed(slot: String)

const SAVE_DIR: String = "user://saves"
const FORMAT_VERSION: int = 1
const SLOT_NAMES: Array = ["slot1", "slot2", "slot3", "auto"]
const MAX_LABEL_LENGTH: int = 128
const MAX_PAYLOAD_KEYS: int = 512


func _ready() -> void:
	_ensure_save_dir()


func _ensure_save_dir() -> Error:
	var absolute: String = ProjectSettings.globalize_path(SAVE_DIR)
	if DirAccess.dir_exists_absolute(absolute):
		return OK
	return DirAccess.make_dir_recursive_absolute(absolute)


func _slot_path(slot: String) -> String:
	return SAVE_DIR + "/" + slot + ".json"


func _slot_absolute_path(slot: String) -> String:
	return ProjectSettings.globalize_path(_slot_path(slot))


## A slot name is a single path component: no separators, no dots
## that could escape SAVE_DIR. Invalid names are rejected by callers.
func _is_valid_slot(slot: String) -> bool:
	if slot.is_empty():
		return false
	if slot.contains("/") or slot.contains("\\") or slot.contains(".."):
		return false
	if slot.length() > 64:
		return false
	return true


## Save payload to a slot atomically: write a .tmp file, then rename
## over the target so a crash mid-write never truncates a good save.
func save_game(slot: String, payload: Dictionary, label: String, playtime_seconds: int) -> Error:
	if not _is_valid_slot(slot):
		push_warning("SaveSystem: invalid slot name rejected: %s" % slot)
		return ERR_INVALID_PARAMETER
	if payload.size() > MAX_PAYLOAD_KEYS:
		push_warning("SaveSystem: payload exceeds MAX_PAYLOAD_KEYS.")
		return ERR_INVALID_PARAMETER
	var ensure_err: Error = _ensure_save_dir()
	if ensure_err != OK:
		return ensure_err
	var safe_label: String = label.substr(0, MAX_LABEL_LENGTH)
	var safe_playtime: int = maxi(0, playtime_seconds)
	var envelope: Dictionary = {}
	envelope["format_version"] = FORMAT_VERSION
	envelope["saved_at_unix"] = int(Time.get_unix_time_from_system())
	envelope["playtime_seconds"] = safe_playtime
	envelope["label"] = safe_label
	envelope["payload"] = payload
	var text: String = JSON.stringify(envelope)
	var target: String = _slot_path(slot)
	var temp_path: String = target + ".tmp"
	var file: FileAccess = FileAccess.open(temp_path, FileAccess.WRITE)
	if file == null:
		return FileAccess.get_open_error()
	file.store_string(text)
	file.close()
	var rename_err: Error = DirAccess.rename_absolute(ProjectSettings.globalize_path(temp_path), _slot_absolute_path(slot))
	if rename_err != OK:
		push_warning("SaveSystem: atomic rename failed for slot %s (error %d)." % [slot, rename_err])
		return rename_err
	save_completed.emit(slot)
	return OK


## Load a slot. Returns the full envelope Dictionary, or {} when
## the slot is empty, corrupt (quarantined), or refused (newer
## format version; file left in place).
func load_game(slot: String) -> Dictionary:
	if not _is_valid_slot(slot):
		push_warning("SaveSystem: invalid slot name rejected: %s" % slot)
		return {}
	var path: String = _slot_path(slot)
	if not FileAccess.file_exists(path):
		return {}
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_warning("SaveSystem: could not open slot %s." % slot)
		return {}
	var text: String = file.get_as_text()
	file.close()
	var parsed: Variant = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		push_warning("SaveSystem: corrupt save in slot %s; quarantining." % slot)
		_quarantine_slot(slot)
		return {}
	var envelope: Dictionary = parsed
	if not envelope.has("format_version"):
		push_warning("SaveSystem: save in slot %s lacks format_version; quarantining." % slot)
		_quarantine_slot(slot)
		return {}
	var version_raw: Variant = envelope["format_version"]
	if typeof(version_raw) != TYPE_FLOAT and typeof(version_raw) != TYPE_INT:
		push_warning("SaveSystem: save in slot %s has a non-integer format_version; quarantining." % slot)
		_quarantine_slot(slot)
		return {}
	var version: int = int(envelope["format_version"])
	if version > FORMAT_VERSION:
		push_warning("SaveSystem: slot %s has format_version %d, newer than supported %d; refusing." % [slot, version, FORMAT_VERSION])
		return {}
	if version < FORMAT_VERSION:
		envelope = _migrate_envelope(envelope, version)
		if envelope.is_empty():
			push_warning("SaveSystem: migration failed for slot %s." % slot)
			return {}
	if not _validate_envelope(envelope):
		push_warning("SaveSystem: save in slot %s failed envelope validation; quarantining." % slot)
		_quarantine_slot(slot)
		return {}
	load_completed.emit(slot)
	return envelope


func _validate_envelope(envelope: Dictionary) -> bool:
	if typeof(envelope.get("saved_at_unix", null)) != TYPE_FLOAT and typeof(envelope.get("saved_at_unix", null)) != TYPE_INT:
		return false
	if typeof(envelope.get("playtime_seconds", null)) != TYPE_FLOAT and typeof(envelope.get("playtime_seconds", null)) != TYPE_INT:
		return false
	if typeof(envelope.get("label", null)) != TYPE_STRING:
		return false
	if typeof(envelope.get("payload", null)) != TYPE_DICTIONARY:
		return false
	return true


## Migration chain. Each older version steps forward one version at
## a time. Add a new match arm when FORMAT_VERSION is bumped; never
## edit a shipped step. Returns {} on an unknown version.
## Example v0 -> v1: v0 envelopes could omit "label"; fill it with
## the default "" and stamp the new format_version.
func _migrate_envelope(envelope: Dictionary, from_version: int) -> Dictionary:
	var current: Dictionary = envelope.duplicate(true)
	var version: int = from_version
	var guard: int = 0
	while version < FORMAT_VERSION and guard < 16:
		guard += 1
		match version:
			0:
				if not current.has("label") or typeof(current["label"]) != TYPE_STRING:
					current["label"] = ""
				if not current.has("saved_at_unix"):
					current["saved_at_unix"] = 0
				if not current.has("playtime_seconds"):
					current["playtime_seconds"] = 0
				if not current.has("payload") or typeof(current["payload"]) != TYPE_DICTIONARY:
					current["payload"] = {}
				current["format_version"] = 1
				version = 1
			_:
				push_warning("SaveSystem: no migration step from version %d." % version)
				return {}
	if version != FORMAT_VERSION:
		return {}
	return current


func _quarantine_slot(slot: String) -> void:
	var source: String = _slot_absolute_path(slot)
	var destination: String = source + ".corrupt"
	var err: Error = DirAccess.rename_absolute(source, destination)
	if err != OK:
		push_warning("SaveSystem: quarantine rename failed for slot %s (error %d)." % [slot, err])


func has_save(slot: String) -> bool:
	if not _is_valid_slot(slot):
		return false
	return FileAccess.file_exists(_slot_path(slot))


func has_any_save() -> bool:
	for slot: String in SLOT_NAMES:
		if has_save(slot):
			return true
	return false


## Light metadata for menus: label, saved_at_unix, playtime_seconds.
## Returns {} for empty/corrupt/refused slots. Parses the file
## without migrating or quarantining on version refusal; corrupt
## JSON still quarantines via the same rule as load_game.
func slot_metadata(slot: String) -> Dictionary:
	var envelope: Dictionary = _peek_envelope(slot)
	if envelope.is_empty():
		return {}
	var meta: Dictionary = {}
	meta["label"] = str(envelope.get("label", ""))
	meta["saved_at_unix"] = int(envelope.get("saved_at_unix", 0))
	meta["playtime_seconds"] = int(envelope.get("playtime_seconds", 0))
	meta["format_version"] = int(envelope.get("format_version", 0))
	return meta


func _peek_envelope(slot: String) -> Dictionary:
	if not _is_valid_slot(slot):
		return {}
	var path: String = _slot_path(slot)
	if not FileAccess.file_exists(path):
		return {}
	var file: FileAccess = FileAccess.open(path, FileAccess.READ)
	if file == null:
		return {}
	var text: String = file.get_as_text()
	file.close()
	var parsed: Variant = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		return {}
	var envelope: Dictionary = parsed
	if not envelope.has("format_version"):
		return {}
	var version: int = int(envelope["format_version"])
	if version > FORMAT_VERSION:
		return {}
	if version < FORMAT_VERSION:
		envelope = _migrate_envelope(envelope, version)
	return envelope


func delete_save(slot: String) -> Error:
	if not _is_valid_slot(slot):
		return ERR_INVALID_PARAMETER
	var absolute: String = _slot_absolute_path(slot)
	if not FileAccess.file_exists(_slot_path(slot)):
		return OK
	return DirAccess.remove_absolute(absolute)


## Names of slots whose files currently exist, in SLOT_NAMES order
## plus any other .json files found in SAVE_DIR (bounded scan).
func list_slots() -> Array[String]:
	var result: Array[String] = []
	for slot: String in SLOT_NAMES:
		if has_save(slot):
			result.append(slot)
	var absolute: String = ProjectSettings.globalize_path(SAVE_DIR)
	var dir: DirAccess = DirAccess.open(absolute)
	if dir == null:
		return result
	dir.list_dir_begin()
	var entry: String = dir.get_next()
	var scanned: int = 0
	while entry != "" and scanned < 256:
		scanned += 1
		if entry.ends_with(".json"):
			var name: String = entry.trim_suffix(".json")
			if not result.has(name) and _is_valid_slot(name):
				result.append(name)
		entry = dir.get_next()
	dir.list_dir_end()
	return result
