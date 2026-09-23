extends Node

## Синтезатор звуков (autoload Sfx).
##
## В веб-версии звук генерировался через WebAudio прямо в браузере
## (функция playBeep в App.tsx: осциллятор + гейн). В Godot аналогичный
## результат даёт AudioStreamWAV, который мы собираем «на лету» из PCM-сэмплов:
## получаются те же короткие квадратные/синусоидные бипы без внешних ассетов.

enum Wave { SQUARE, SINE, SAW }

const MIX_RATE := 22050
const POOL_SIZE := 8

var enabled: bool = true

var _players: Array[AudioStreamPlayer] = []
var _next: int = 0
## Кэш сгенерированных волн: "freq|dur|wave|vol" -> AudioStreamWAV
var _cache: Dictionary = {}


func _ready() -> void:
	# Пул плееров: звуки короткие и могут накладываться друг на друга.
	for i in range(POOL_SIZE):
		var p := AudioStreamPlayer.new()
		p.bus = "Master"
		p.volume_db = -6.0
		add_child(p)
		_players.append(p)


func _make_stream(freq: float, dur: float, wave: Wave, vol: float) -> AudioStreamWAV:
	var frames := maxi(1, int(MIX_RATE * dur))
	var data := PackedByteArray()
	data.resize(frames * 2)
	var period := 1.0 / maxf(freq, 1.0)
	for i in range(frames):
		var t := float(i) / float(MIX_RATE)
		var phase := fmod(t, period) / period
		var sample := 0.0
		match wave:
			Wave.SQUARE:
				sample = 1.0 if phase < 0.5 else -1.0
			Wave.SAW:
				sample = phase * 2.0 - 1.0
			_:
				sample = sin(TAU * freq * t)
		# мягкое затухание к концу, чтобы не было щелчков
		var fade := 1.0 - float(i) / float(frames)
		var value := int(clampf(sample * vol * fade, -1.0, 1.0) * 32767.0)
		data.encode_s16(i * 2, value)

	var stream := AudioStreamWAV.new()
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	stream.mix_rate = MIX_RATE
	stream.stereo = false
	stream.data = data
	return stream


## Короткий бип. freq — герцы, dur — секунды, wave — форма волны, vol — 0..1.
func beep(freq: float = 660.0, dur: float = 0.08, wave: Wave = Wave.SQUARE, vol: float = 0.35) -> void:
	if not enabled:
		return
	var key := "%d|%d|%d|%d" % [int(freq), int(dur * 1000.0), int(wave), int(vol * 1000.0)]
	var stream: AudioStreamWAV = _cache.get(key)
	if stream == null:
		stream = _make_stream(freq, dur, wave, vol)
		_cache[key] = stream
		if _cache.size() > 64:
			_cache.clear()
	var player := _players[_next]
	_next = (_next + 1) % _players.size()
	player.stream = stream
	player.play()


# --- Готовые «фирменные» звуки интерфейса ---
func ui_click() -> void:
	beep(550.0, 0.05, Wave.SINE, 0.25)


func ui_ok() -> void:
	beep(700.0, 0.03, Wave.SINE, 0.18)


func hack_success() -> void:
	beep(660.0, 0.10)
	_after(0.12, 880.0, 0.12)
	_after(0.24, 1320.0, 0.18)


func hack_fail() -> void:
	beep(180.0, 0.25, Wave.SAW, 0.35)


func level_up() -> void:
	beep(523.0, 0.12)
	_after(0.13, 659.0, 0.12)
	_after(0.26, 784.0, 0.20)


func boot_line(index: int) -> void:
	beep(300.0 + float(index) * 90.0, 0.05, Wave.SQUARE, 0.18)


func buy() -> void:
	beep(600.0, 0.08)
	_after(0.10, 900.0, 0.10)


func trade() -> void:
	beep(760.0, 0.07, Wave.SINE, 0.3)


# --- Удобные обёртки, чтобы не обращаться к enum снаружи (Sfx.Wave.X) ---
func beep_square(freq: float, dur: float = 0.08, vol: float = 0.35) -> void:
	beep(freq, dur, Wave.SQUARE, vol)


func beep_sine(freq: float, dur: float = 0.08, vol: float = 0.3) -> void:
	beep(freq, dur, Wave.SINE, vol)


func beep_saw(freq: float, dur: float = 0.08, vol: float = 0.3) -> void:
	beep(freq, dur, Wave.SAW, vol)


func _after(delay: float, freq: float, dur: float, wave: Wave = Wave.SQUARE, vol: float = 0.35) -> void:
	# небольшая отложенная нотка для «мелодий» из веб-версии
	var timer := get_tree().create_timer(delay)
	timer.timeout.connect(func() -> void: beep(freq, dur, wave, vol))
