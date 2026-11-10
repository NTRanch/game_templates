extends Control
## Placeholder game scene. Replace this stub with the real game.
##
## It demonstrates the shell contracts a real game must honour:
## register a save payload provider with Shell, restore from
## Shell.payload_loaded, and request pause through the Shell
## pause_requested signal (ui_cancel). Its _process is pausable by
## default, so the counter freezes while the pause overlay is open,
## proving pause leaves game state untouched.

var stub_counter: int = 0

var _elapsed: float = 0.0

@onready var _counter_label: Label = %CounterLabel


func _ready() -> void:
	Shell.save_payload_provider = Callable(self, "get_save_payload")
	Shell.payload_loaded.connect(_on_payload_loaded)
	_refresh_label()


func _exit_tree() -> void:
	if Shell.save_payload_provider == Callable(self, "get_save_payload"):
		Shell.save_payload_provider = Callable()
	if Shell.payload_loaded.is_connected(_on_payload_loaded):
		Shell.payload_loaded.disconnect(_on_payload_loaded)


func _process(delta: float) -> void:
	_elapsed += delta
	if _elapsed >= 1.0:
		_elapsed -= 1.0
		stub_counter += 1
		_refresh_label()


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		Shell.pause_requested.emit()


## Payload provider registered in Shell: the Dictionary stored in
## the save envelope's payload field for this game.
func get_save_payload() -> Dictionary:
	return {"stub_counter": stub_counter}


func _on_payload_loaded(payload: Dictionary) -> void:
	if payload.has("stub_counter"):
		var raw: Variant = payload["stub_counter"]
		if typeof(raw) == TYPE_INT or typeof(raw) == TYPE_FLOAT:
			stub_counter = int(raw)
			_refresh_label()


func _refresh_label() -> void:
	if _counter_label != null:
		_counter_label.text = "Counter: %d" % stub_counter
