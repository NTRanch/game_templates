extends Node
## Flow state for the Game Shell template.
##
## State and signals only: Main performs scene switching. Keeping
## flow state here lets menus, the pause overlay, and the game stub
## coordinate without holding references to each other.

signal screen_changed(screen: int)
signal pause_requested
signal payload_loaded(payload: Dictionary)

enum Screen { AGE_GATE, TITLE, GAME }

## Master switch for the age gate. Off by default: most games built
## from this template are not age-restricted. When true, Main shows
## the age gate first; confirm goes to Title, decline quits.
const AGE_GATE_ENABLED: bool = false

## Per-session accumulator bound: 100 years of seconds. The bound
## exists so a forgotten session cannot overflow playtime maths.
const MAX_SESSION_PLAYTIME_SECONDS: float = 3155760000.0

var age_gate_enabled: bool = AGE_GATE_ENABLED
var age_confirmed: bool = false
var settings_return_to: StringName = &"title"
var current_slot: String = ""
var session_playtime_seconds: int = 0

## Registered by the game scene: returns the payload Dictionary to
## store in the save envelope. Empty/invalid means "no provider".
var save_payload_provider: Callable = Callable()

var _playtime_accumulator: float = 0.0
var _current_screen: int = Screen.TITLE


func _process(delta: float) -> void:
	if delta <= 0.0:
		return
	_playtime_accumulator += delta
	if _playtime_accumulator >= 1.0:
		var whole: int = int(_playtime_accumulator)
		_playtime_accumulator -= float(whole)
		session_playtime_seconds = mini(session_playtime_seconds + whole, int(MAX_SESSION_PLAYTIME_SECONDS))


func get_current_screen() -> int:
	return _current_screen


func set_current_screen(screen: int) -> void:
	_current_screen = screen
	screen_changed.emit(screen)


## Pause is a tree property, not game state: the game scene is left
## untouched under the overlay. Pausable nodes stop; the pause menu
## runs with PROCESS_MODE_ALWAYS.
func set_paused(value: bool) -> void:
	get_tree().paused = value


func is_paused() -> bool:
	return get_tree().paused


## Collect a save payload from the registered provider, if any.
func collect_save_payload() -> Dictionary:
	if save_payload_provider.is_valid():
		var result: Variant = save_payload_provider.call()
		if typeof(result) == TYPE_DICTIONARY:
			return result
		push_warning("Shell: save payload provider returned a non-Dictionary.")
	return {}


## Deliver a loaded payload to the game via payload_loaded.
func restore_payload(payload: Dictionary) -> void:
	payload_loaded.emit(payload)
