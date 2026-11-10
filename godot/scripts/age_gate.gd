extends Control
## Age gate: a separate first screen, off by default.
##
## Shown first only when Shell.AGE_GATE_ENABLED is true. Confirm
## records Shell.age_confirmed and emits confirmed (Main shows the
## title); decline emits declined (Main quits). No game content is
## reachable before confirmation while the gate is enabled.

signal confirmed
signal declined

@onready var _confirm_button: Button = %ConfirmButton
@onready var _decline_button: Button = %DeclineButton


func _ready() -> void:
	_confirm_button.pressed.connect(_on_confirm_pressed)
	_decline_button.pressed.connect(_on_decline_pressed)


func _on_confirm_pressed() -> void:
	Shell.age_confirmed = true
	confirmed.emit()


func _on_decline_pressed() -> void:
	declined.emit()
