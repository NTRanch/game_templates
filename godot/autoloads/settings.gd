extends Node
## Persistent user settings for the Game Shell template.
##
## Canonical settings, defaults, and bounds live here. Every value is
## validated by control flow on load and on set (never assert alone),
## because save/settings data is untrusted input and asserts are
## stripped in release builds.
## Sections are written in a fixed order (meta, audio, display,
## gameplay) because ConfigFile byte output follows authoring order.

signal settings_changed

const SETTINGS_PATH: String = "user://settings.cfg"
const SETTINGS_VERSION: int = 1

const MIN_VOLUME: float = 0.0
const MAX_VOLUME: float = 1.0
const MIN_WINDOW_WIDTH: int = 640
const MAX_WINDOW_WIDTH: int = 7680
const MIN_WINDOW_HEIGHT: int = 360
const MAX_WINDOW_HEIGHT: int = 4320
const MIN_SCREEN_SHAKE: float = 0.0
const MAX_SCREEN_SHAKE: float = 1.0

const DEFAULT_MASTER_VOLUME: float = 1.0
const DEFAULT_MUSIC_VOLUME: float = 1.0
const DEFAULT_SFX_VOLUME: float = 1.0
const DEFAULT_FULLSCREEN: bool = false
const DEFAULT_WINDOW_WIDTH: int = 1280
const DEFAULT_WINDOW_HEIGHT: int = 720
const DEFAULT_VSYNC: bool = true
const DEFAULT_REDUCE_FLASHING: bool = false
const DEFAULT_SCREEN_SHAKE: float = 1.0
const DEFAULT_SUBTITLES: bool = true

var master_volume: float = DEFAULT_MASTER_VOLUME
var music_volume: float = DEFAULT_MUSIC_VOLUME
var sfx_volume: float = DEFAULT_SFX_VOLUME
var fullscreen: bool = DEFAULT_FULLSCREEN
var window_width: int = DEFAULT_WINDOW_WIDTH
var window_height: int = DEFAULT_WINDOW_HEIGHT
var vsync: bool = DEFAULT_VSYNC
var reduce_flashing: bool = DEFAULT_REDUCE_FLASHING
var screen_shake: float = DEFAULT_SCREEN_SHAKE
var subtitles: bool = DEFAULT_SUBTITLES


func _ready() -> void:
	load_settings()
	apply_all()


## Reset all values to their documented defaults (no save, no apply).
func reset_defaults() -> void:
	master_volume = DEFAULT_MASTER_VOLUME
	music_volume = DEFAULT_MUSIC_VOLUME
	sfx_volume = DEFAULT_SFX_VOLUME
	fullscreen = DEFAULT_FULLSCREEN
	window_width = DEFAULT_WINDOW_WIDTH
	window_height = DEFAULT_WINDOW_HEIGHT
	vsync = DEFAULT_VSYNC
	reduce_flashing = DEFAULT_REDUCE_FLASHING
	screen_shake = DEFAULT_SCREEN_SHAKE
	subtitles = DEFAULT_SUBTITLES


## Clamp a volume value into [MIN_VOLUME, MAX_VOLUME].
func _clamp_volume(value: float) -> float:
	return clampf(value, MIN_VOLUME, MAX_VOLUME)


## Clamp a screen-shake value into [MIN_SCREEN_SHAKE, MAX_SCREEN_SHAKE].
func _clamp_screen_shake(value: float) -> float:
	return clampf(value, MIN_SCREEN_SHAKE, MAX_SCREEN_SHAKE)


## Clamp a window width into [MIN_WINDOW_WIDTH, MAX_WINDOW_WIDTH].
func _clamp_window_width(value: int) -> int:
	return clampi(value, MIN_WINDOW_WIDTH, MAX_WINDOW_WIDTH)


## Clamp a window height into [MIN_WINDOW_HEIGHT, MAX_WINDOW_HEIGHT].
func _clamp_window_height(value: int) -> int:
	return clampi(value, MIN_WINDOW_HEIGHT, MAX_WINDOW_HEIGHT)


## Load settings from SETTINGS_PATH. Missing file yields defaults.
## A parse error quarantines the file to a .corrupt sibling, logs a
## warning, and falls back to defaults. Never crashes.
func load_settings() -> void:
	if not FileAccess.file_exists(SETTINGS_PATH):
		reset_defaults()
		save_settings()
		return
	var config: ConfigFile = ConfigFile.new()
	var err: Error = config.load(SETTINGS_PATH)
	if err != OK:
		push_warning("Settings: failed to parse %s (error %d); quarantining." % [SETTINGS_PATH, err])
		_quarantine_settings()
		reset_defaults()
		save_settings()
		return
	master_volume = _read_float(config, "audio", "master_volume", DEFAULT_MASTER_VOLUME, MIN_VOLUME, MAX_VOLUME)
	music_volume = _read_float(config, "audio", "music_volume", DEFAULT_MUSIC_VOLUME, MIN_VOLUME, MAX_VOLUME)
	sfx_volume = _read_float(config, "audio", "sfx_volume", DEFAULT_SFX_VOLUME, MIN_VOLUME, MAX_VOLUME)
	fullscreen = _read_bool(config, "display", "fullscreen", DEFAULT_FULLSCREEN)
	window_width = _clamp_window_width(_read_int(config, "display", "window_width", DEFAULT_WINDOW_WIDTH))
	window_height = _clamp_window_height(_read_int(config, "display", "window_height", DEFAULT_WINDOW_HEIGHT))
	vsync = _read_bool(config, "display", "vsync", DEFAULT_VSYNC)
	reduce_flashing = _read_bool(config, "gameplay", "reduce_flashing", DEFAULT_REDUCE_FLASHING)
	screen_shake = _clamp_screen_shake(_read_float(config, "gameplay", "screen_shake", DEFAULT_SCREEN_SHAKE, MIN_SCREEN_SHAKE, MAX_SCREEN_SHAKE))
	subtitles = _read_bool(config, "gameplay", "subtitles", DEFAULT_SUBTITLES)


func _read_float(config: ConfigFile, section: String, key: String, fallback: float, min_value: float, max_value: float) -> float:
	if not config.has_section_key(section, key):
		return fallback
	var raw: Variant = config.get_value(section, key, fallback)
	if typeof(raw) != TYPE_FLOAT and typeof(raw) != TYPE_INT:
		push_warning("Settings: %s/%s has wrong type; using default." % [section, key])
		return fallback
	return clampf(float(raw), min_value, max_value)


func _read_int(config: ConfigFile, section: String, key: String, fallback: int) -> int:
	if not config.has_section_key(section, key):
		return fallback
	var raw: Variant = config.get_value(section, key, fallback)
	if typeof(raw) != TYPE_INT:
		push_warning("Settings: %s/%s has wrong type; using default." % [section, key])
		return fallback
	return int(raw)


func _read_bool(config: ConfigFile, section: String, key: String, fallback: bool) -> bool:
	if not config.has_section_key(section, key):
		return fallback
	var raw: Variant = config.get_value(section, key, fallback)
	if typeof(raw) != TYPE_BOOL:
		push_warning("Settings: %s/%s has wrong type; using default." % [section, key])
		return fallback
	return bool(raw)


func _quarantine_settings() -> void:
	var source: String = ProjectSettings.globalize_path(SETTINGS_PATH)
	var destination: String = source + ".corrupt"
	var err: Error = DirAccess.rename_absolute(source, destination)
	if err != OK:
		push_warning("Settings: quarantine rename failed with error %d." % err)


## Persist current values. Sections and keys are set in a fixed
## order (meta, audio, display, gameplay) for byte-stable output.
func save_settings() -> Error:
	var config: ConfigFile = ConfigFile.new()
	config.set_value("meta", "version", SETTINGS_VERSION)
	config.set_value("audio", "master_volume", master_volume)
	config.set_value("audio", "music_volume", music_volume)
	config.set_value("audio", "sfx_volume", sfx_volume)
	config.set_value("display", "fullscreen", fullscreen)
	config.set_value("display", "window_width", window_width)
	config.set_value("display", "window_height", window_height)
	config.set_value("display", "vsync", vsync)
	config.set_value("gameplay", "reduce_flashing", reduce_flashing)
	config.set_value("gameplay", "screen_shake", screen_shake)
	config.set_value("gameplay", "subtitles", subtitles)
	var err: Error = config.save(SETTINGS_PATH)
	if err != OK:
		push_warning("Settings: failed to save %s (error %d)." % [SETTINGS_PATH, err])
	return err


## Apply every setting to the engine.
func apply_all() -> void:
	apply_audio()
	apply_display()


## Apply volume settings to the Master, Music, and SFX buses.
func apply_audio() -> void:
	_apply_bus_volume("Master", master_volume)
	_apply_bus_volume("Music", music_volume)
	_apply_bus_volume("SFX", sfx_volume)


func _apply_bus_volume(bus_name: String, volume: float) -> void:
	var index: int = AudioServer.get_bus_index(bus_name)
	if index < 0:
		push_warning("Settings: audio bus not found: %s" % bus_name)
		return
	var clamped: float = _clamp_volume(volume)
	if clamped <= 0.0:
		AudioServer.set_bus_mute(index, true)
		AudioServer.set_bus_volume_db(index, -80.0)
	else:
		AudioServer.set_bus_mute(index, false)
		AudioServer.set_bus_volume_db(index, linear_to_db(clamped))


## Apply display settings. No-op under the headless driver, where
## there is no window to configure (tests and CI run headless).
func apply_display() -> void:
	if DisplayServer.get_name() == "headless":
		return
	if fullscreen:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
	else:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
		DisplayServer.window_set_size(Vector2i(window_width, window_height))
	if vsync:
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_ENABLED)
	else:
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)


## Generic validated setter used by the settings screen.
## Unknown keys and wrong types are rejected with a warning and no
## state change. Valid changes emit settings_changed, save, and apply.
func set_value(key: String, value: Variant) -> void:
	var changed: bool = false
	match key:
		"master_volume":
			if typeof(value) == TYPE_FLOAT or typeof(value) == TYPE_INT:
				master_volume = _clamp_volume(float(value))
				changed = true
		"music_volume":
			if typeof(value) == TYPE_FLOAT or typeof(value) == TYPE_INT:
				music_volume = _clamp_volume(float(value))
				changed = true
		"sfx_volume":
			if typeof(value) == TYPE_FLOAT or typeof(value) == TYPE_INT:
				sfx_volume = _clamp_volume(float(value))
				changed = true
		"fullscreen":
			if typeof(value) == TYPE_BOOL:
				fullscreen = bool(value)
				changed = true
		"window_width":
			if typeof(value) == TYPE_INT or typeof(value) == TYPE_FLOAT:
				window_width = _clamp_window_width(int(value))
				changed = true
		"window_height":
			if typeof(value) == TYPE_INT or typeof(value) == TYPE_FLOAT:
				window_height = _clamp_window_height(int(value))
				changed = true
		"vsync":
			if typeof(value) == TYPE_BOOL:
				vsync = bool(value)
				changed = true
		"reduce_flashing":
			if typeof(value) == TYPE_BOOL:
				reduce_flashing = bool(value)
				changed = true
		"screen_shake":
			if typeof(value) == TYPE_FLOAT or typeof(value) == TYPE_INT:
				screen_shake = _clamp_screen_shake(float(value))
				changed = true
		"subtitles":
			if typeof(value) == TYPE_BOOL:
				subtitles = bool(value)
				changed = true
		_:
			push_warning("Settings: unknown key rejected: %s" % key)
			return
	if not changed:
		push_warning("Settings: value for %s has wrong type; rejected." % key)
		return
	settings_changed.emit()
	save_settings()
	apply_all()


## Read a setting by canonical name. Returns null for unknown keys.
func get_value(key: String) -> Variant:
	match key:
		"master_volume":
			return master_volume
		"music_volume":
			return music_volume
		"sfx_volume":
			return sfx_volume
		"fullscreen":
			return fullscreen
		"window_width":
			return window_width
		"window_height":
			return window_height
		"vsync":
			return vsync
		"reduce_flashing":
			return reduce_flashing
		"screen_shake":
			return screen_shake
		"subtitles":
			return subtitles
		_:
			return null
