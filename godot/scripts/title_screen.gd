extends Control
## Title screen: Continue / New Game / Load / Settings / Quit.
##
## Continue is enabled only when a save exists and loads the most
## recent slot by saved_at_unix. Load reveals the SlotList in load
## mode. Navigation goes through the "shell_main" group (Main).

@onready var _continue_button: Button = %ContinueButton
@onready var _new_game_button: Button = %NewGameButton
@onready var _load_button: Button = %LoadButton
@onready var _settings_button: Button = %SettingsButton
@onready var _quit_button: Button = %QuitButton
@onready var _slot_list = %SlotList


func _ready() -> void:
	_continue_button.pressed.connect(_on_continue_pressed)
	_new_game_button.pressed.connect(_on_new_game_pressed)
	_load_button.pressed.connect(_on_load_pressed)
	_settings_button.pressed.connect(_on_settings_pressed)
	_quit_button.pressed.connect(_on_quit_pressed)
	_slot_list.slot_chosen.connect(_on_slot_chosen)
	_slot_list.set_mode("load")
	_slot_list.visible = false
	refresh()


## Refresh Continue availability from the save system.
func refresh() -> void:
	_continue_button.disabled = not SaveSystem.has_any_save()
	if _slot_list.visible:
		_slot_list.refresh()


func _most_recent_slot() -> String:
	var best_slot: String = ""
	var best_time: int = -1
	for slot: String in SaveSystem.SLOT_NAMES:
		var meta: Dictionary = SaveSystem.slot_metadata(slot)
		if meta.is_empty():
			continue
		var saved_at: int = int(meta.get("saved_at_unix", 0))
		if saved_at > best_time:
			best_time = saved_at
			best_slot = slot
	return best_slot


func _load_slot(slot: String) -> void:
	var envelope: Dictionary = SaveSystem.load_game(slot)
	if envelope.is_empty():
		refresh()
		return
	Shell.current_slot = slot
	Shell.session_playtime_seconds = int(envelope.get("playtime_seconds", 0))
	get_tree().call_group("shell_main", "show_game")
	var payload: Dictionary = envelope.get("payload", {})
	if typeof(payload) == TYPE_DICTIONARY:
		Shell.restore_payload(payload)


func _on_continue_pressed() -> void:
	var slot: String = _most_recent_slot()
	if slot == "":
		refresh()
		return
	_load_slot(slot)


func _on_new_game_pressed() -> void:
	Shell.current_slot = ""
	Shell.session_playtime_seconds = 0
	get_tree().call_group("shell_main", "show_game")


func _on_load_pressed() -> void:
	_slot_list.visible = not _slot_list.visible
	if _slot_list.visible:
		_slot_list.refresh()


func _on_settings_pressed() -> void:
	get_tree().call_group("shell_main", "open_settings", "title")


func _on_quit_pressed() -> void:
	get_tree().quit()


func _on_slot_chosen(slot: String) -> void:
	_load_slot(slot)
