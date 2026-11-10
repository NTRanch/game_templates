extends Node
## Root scene controller for the Game Shell template.
##
## Owns screen switching. Screens never reference each other: Title,
## Pause, and the game stub reach Main through the "shell_main"
## group or through Shell signals, and Settings returns through the
## settings_closed signal that Main connects on instancing.
## Children: ScreenRoot holds the current full-screen scene,
## PauseLayer (layer 10) holds the pause overlay, SettingsLayer
## (layer 20) holds the settings screen so it overlays both.

const TitleScene: PackedScene = preload("res://scenes/title_screen.tscn")
const SettingsScene: PackedScene = preload("res://scenes/settings_screen.tscn")
const AgeGateScene: PackedScene = preload("res://scenes/age_gate.tscn")
const GameScene: PackedScene = preload("res://scenes/game_stub.tscn")

var _current_node: Node = null
var _settings_node: Control = null

@onready var _screen_root: Control = $ScreenRoot
@onready var _pause_menu = $PauseLayer/PauseMenu
@onready var _settings_layer: CanvasLayer = $SettingsLayer


func _ready() -> void:
	add_to_group("shell_main")
	Shell.screen_changed.connect(_on_screen_changed)
	Shell.pause_requested.connect(toggle_pause)
	if _pause_menu != null:
		_pause_menu.visible = false
	Shell.set_paused(false)
	if Shell.age_gate_enabled and not Shell.age_confirmed:
		show_age_gate()
	else:
		show_title()


func _on_screen_changed(_screen: int) -> void:
	pass


func _clear_screen_root() -> void:
	if _current_node != null and is_instance_valid(_current_node):
		_screen_root.remove_child(_current_node)
		_current_node.free()
	_current_node = null


func _mount(node: Node) -> void:
	_clear_screen_root()
	_current_node = node
	_screen_root.add_child(node)


## Show the age gate (only used when Shell.age_gate_enabled).
func show_age_gate() -> void:
	var gate = AgeGateScene.instantiate()
	_mount(gate)
	gate.confirmed.connect(show_title)
	gate.declined.connect(_on_age_gate_declined)
	Shell.set_current_screen(Shell.Screen.AGE_GATE)


func _on_age_gate_declined() -> void:
	get_tree().quit()


## Show the title screen.
func show_title() -> void:
	Shell.set_paused(false)
	if _pause_menu != null:
		_pause_menu.visible = false
	var title = TitleScene.instantiate()
	_mount(title)
	Shell.set_current_screen(Shell.Screen.TITLE)


## Show the game scene (the stub until a real game replaces it).
func show_game() -> void:
	Shell.set_paused(false)
	if _pause_menu != null:
		_pause_menu.visible = false
	var game = GameScene.instantiate()
	_mount(game)
	Shell.set_current_screen(Shell.Screen.GAME)


## Open settings over the current screen. `from` is "title" or
## "pause" and is recorded in Shell.settings_return_to so
## close_settings returns to whichever screen opened it.
func open_settings(from: String) -> void:
	Shell.settings_return_to = StringName(from)
	if _settings_node != null and is_instance_valid(_settings_node):
		return
	var settings = SettingsScene.instantiate()
	_settings_node = settings
	settings.settings_closed.connect(close_settings)
	_settings_layer.add_child(settings)


## Close settings and return to the recorded caller. Returning to
## pause needs no work: the pause overlay stayed open underneath
## the settings layer. Returning to title: the title stayed mounted.
func close_settings() -> void:
	if _settings_node != null and is_instance_valid(_settings_node):
		_settings_layer.remove_child(_settings_node)
		_settings_node.free()
	_settings_node = null


## Toggle the pause overlay. Only meaningful on the game screen;
## the game scene itself is left untouched (overlay + tree pause).
func toggle_pause() -> void:
	if Shell.get_current_screen() != Shell.Screen.GAME:
		return
	if _pause_menu == null:
		return
	if Shell.is_paused():
		_pause_menu.close()
	else:
		_pause_menu.open()
