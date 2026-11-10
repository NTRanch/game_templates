extends Control
## Settings screen: immediate-apply controls for every canonical
## setting. There is no Apply button by design: each control writes
## through Settings.set_value on change, which validates, saves,
## and applies. Back emits settings_closed; Main owns the return.

signal settings_closed

var _populating: bool = false

@onready var _master_slider: HSlider = %MasterVolumeSlider
@onready var _master_value: Label = %MasterVolumeValue
@onready var _music_slider: HSlider = %MusicVolumeSlider
@onready var _music_value: Label = %MusicVolumeValue
@onready var _sfx_slider: HSlider = %SfxVolumeSlider
@onready var _sfx_value: Label = %SfxVolumeValue
@onready var _shake_slider: HSlider = %ScreenShakeSlider
@onready var _shake_value: Label = %ScreenShakeValue
@onready var _fullscreen_check: CheckBox = %FullscreenCheck
@onready var _vsync_check: CheckBox = %VsyncCheck
@onready var _flashing_check: CheckBox = %ReduceFlashingCheck
@onready var _subtitles_check: CheckBox = %SubtitlesCheck
@onready var _width_spin: SpinBox = %WindowWidthSpin
@onready var _height_spin: SpinBox = %WindowHeightSpin
@onready var _back_button: Button = %BackButton
@onready var _reset_button: Button = %ResetDefaultsButton


func _ready() -> void:
	_master_slider.value_changed.connect(_on_master_changed)
	_music_slider.value_changed.connect(_on_music_changed)
	_sfx_slider.value_changed.connect(_on_sfx_changed)
	_shake_slider.value_changed.connect(_on_shake_changed)
	_fullscreen_check.toggled.connect(_on_fullscreen_toggled)
	_vsync_check.toggled.connect(_on_vsync_toggled)
	_flashing_check.toggled.connect(_on_flashing_toggled)
	_subtitles_check.toggled.connect(_on_subtitles_toggled)
	_width_spin.value_changed.connect(_on_width_changed)
	_height_spin.value_changed.connect(_on_height_changed)
	_back_button.pressed.connect(_on_back_pressed)
	_reset_button.pressed.connect(_on_reset_pressed)
	_populate()


func _populate() -> void:
	_populating = true
	_master_slider.value = Settings.master_volume
	_music_slider.value = Settings.music_volume
	_sfx_slider.value = Settings.sfx_volume
	_shake_slider.value = Settings.screen_shake
	_fullscreen_check.button_pressed = Settings.fullscreen
	_vsync_check.button_pressed = Settings.vsync
	_flashing_check.button_pressed = Settings.reduce_flashing
	_subtitles_check.button_pressed = Settings.subtitles
	_width_spin.value = float(Settings.window_width)
	_height_spin.value = float(Settings.window_height)
	_refresh_labels()
	_populating = false


func _refresh_labels() -> void:
	_master_value.text = "%d%%" % int(round(Settings.master_volume * 100.0))
	_music_value.text = "%d%%" % int(round(Settings.music_volume * 100.0))
	_sfx_value.text = "%d%%" % int(round(Settings.sfx_volume * 100.0))
	_shake_value.text = "%d%%" % int(round(Settings.screen_shake * 100.0))


func _on_master_changed(value: float) -> void:
	if _populating:
		return
	Settings.set_value("master_volume", value)
	_refresh_labels()


func _on_music_changed(value: float) -> void:
	if _populating:
		return
	Settings.set_value("music_volume", value)
	_refresh_labels()


func _on_sfx_changed(value: float) -> void:
	if _populating:
		return
	Settings.set_value("sfx_volume", value)
	_refresh_labels()


func _on_shake_changed(value: float) -> void:
	if _populating:
		return
	Settings.set_value("screen_shake", value)
	_refresh_labels()


func _on_fullscreen_toggled(pressed: bool) -> void:
	if _populating:
		return
	Settings.set_value("fullscreen", pressed)


func _on_vsync_toggled(pressed: bool) -> void:
	if _populating:
		return
	Settings.set_value("vsync", pressed)


func _on_flashing_toggled(pressed: bool) -> void:
	if _populating:
		return
	Settings.set_value("reduce_flashing", pressed)


func _on_subtitles_toggled(pressed: bool) -> void:
	if _populating:
		return
	Settings.set_value("subtitles", pressed)


func _on_width_changed(value: float) -> void:
	if _populating:
		return
	Settings.set_value("window_width", int(value))


func _on_height_changed(value: float) -> void:
	if _populating:
		return
	Settings.set_value("window_height", int(value))


func _on_back_pressed() -> void:
	settings_closed.emit()


func _on_reset_pressed() -> void:
	Settings.reset_defaults()
	Settings.settings_changed.emit()
	Settings.save_settings()
	Settings.apply_all()
	_populate()
