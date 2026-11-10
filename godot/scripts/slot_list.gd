extends VBoxContainer
## Reusable slot picker for the canonical slots.
##
## Builds one Button row per slot (slot1, slot2, slot3, auto) from
## SaveSystem metadata. In "load" mode a row reads
## "slot1 - <label> - <date>" or "slot1 - Empty". In "save" mode
## non-empty rows gain an "(Overwrite)" affordance. Choosing a row
## emits slot_chosen; the caller performs the save or load.

signal slot_chosen(slot: String)

var mode: String = "load"


func _ready() -> void:
	refresh()


## Set the picker mode: "load" or "save". Unknown values fall back
## to "load" rather than leaving the picker in an unnamed state.
func set_mode(value: String) -> void:
	if value == "save":
		mode = "save"
	else:
		mode = "load"
	refresh()


## Rebuild rows from current save metadata.
func refresh() -> void:
	for child: Node in get_children():
		remove_child(child)
		child.queue_free()
	for slot: String in SaveSystem.SLOT_NAMES:
		var button: Button = Button.new()
		button.text = _row_text(slot)
		button.alignment = HORIZONTAL_ALIGNMENT_LEFT
		var captured: String = slot
		button.pressed.connect(func() -> void: slot_chosen.emit(captured))
		add_child(button)


func _row_text(slot: String) -> String:
	var meta: Dictionary = SaveSystem.slot_metadata(slot)
	if meta.is_empty():
		if mode == "save":
			return "%s - Empty (save here)" % slot
		return "%s - Empty" % slot
	var label: String = str(meta.get("label", ""))
	if label == "":
		label = "Untitled"
	var saved_at: int = int(meta.get("saved_at_unix", 0))
	var date_text: String = ""
	if saved_at > 0:
		date_text = Time.get_datetime_string_from_unix_time(saved_at)
	else:
		date_text = "unknown date"
	var text: String = "%s - %s - %s" % [slot, label, date_text]
	if mode == "save":
		text += " (Overwrite)"
	return text
