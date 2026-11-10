extends Control
## Pause overlay: Resume / Save / Load / Settings / Quit to Title.
##
## The overlay never mutates game state: open() pauses the tree,
## close() unpauses it. This root runs with PROCESS_MODE_ALWAYS
## (set in the scene) so its buttons work while the tree is paused.
## Save payloads come from the Callable registered in Shell by the
## game scene; loads are delivered back through Shell.payload_loaded.

var _mode: String = ""

@onready var _resume_button: Button = %ResumeButton
@onready var _save_button: Button = %SaveButton
@onready var _load_button: Button = %LoadButton
@onready var _settings_button: Button = %SettingsButton
@onready var _quit_button: Button = %QuitToTitleButton
@onready var _slot_list = %SlotList


func _ready() -> void:
	_resume_button.pressed.connect(_on_resume_pressed)
	_save_button.pressed.connect(_on_save_pressed)
	_load_button.pressed.connect(_on_load_pressed)
	_settings_button.pressed.connect(_on_settings_pressed)
	_quit_button.pressed.connect(_on_quit_pressed)
	_slot_list.slot_chosen.connect(_on_slot_chosen)
	_slot_list.visible = false
	visible = false


## Open the overlay and pause the tree.
func open() -> void:
	_mode = ""
	_slot_list.visible = false
	visible = true
	Shell.set_paused(true)


## Close the overlay and unpause the tree.
func close() -> void:
	visible = false
	_slot_list.visible = false
	Shell.set_paused(false)


func _on_resume_pressed() -> void:
	close()


func _on_save_pressed() -> void:
	_mode = "save"
	_slot_list.set_mode("save")
	_slot_list.visible = true
	_slot_list.refresh()


func _on_load_pressed() -> void:
	_mode = "load"
	_slot_list.set_mode("load")
	_slot_list.visible = true
	_slot_list.refresh()


func _on_settings_pressed() -> void:
	get_tree().call_group("shell_main", "open_settings", "pause")


func _on_quit_pressed() -> void:
	close()
	get_tree().call_group("shell_main", "show_title")


func _on_slot_chosen(slot: String) -> void:
	if _mode == "save":
		var payload: Dictionary = Shell.collect_save_payload()
		var err: Error = SaveSystem.save_game(slot, payload, "Save", Shell.session_playtime_seconds)
		if err == OK:
			Shell.current_slot = slot
		_slot_list.visible = false
		_mode = ""
	else:
		var envelope: Dictionary = SaveSystem.load_game(slot)
		if envelope.is_empty():
			_slot_list.refresh()
			return
		Shell.current_slot = slot
		close()
		var payload: Dictionary = envelope.get("payload", {})
		if typeof(payload) == TYPE_DICTIONARY:
			Shell.restore_payload(payload)
