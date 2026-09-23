class_name MatrixBg
extends Control

## «Матричный дождь» на фоне — порт canvas-анимации MatrixBg из App.tsx.
##
## В вебе это был <canvas> с requestAnimationFrame. В Godot — обычный Control,
## который рисует символы в _draw() и обновляется по таймеру.

const CHARS := "01ABCDEF$#@%&Ξ₿<>+*"
const FONT_SIZE := 13
const STEP_SEC := 0.066      ## ~15 кадров в секунду, как setTimeout(draw, 66)

var columns: int = 0
var drops: Array[float] = []
var trail: int = 4           ## длина «хвоста» символа
var opacity: float = 0.35

var _font: Font
var _accum := 0.0
var _size_cache := Vector2.ZERO


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_font = Neon.font_main()
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_reset()


func set_opacity(value: float) -> void:
	opacity = value


func _reset() -> void:
	_size_cache = size
	columns = maxi(1, int(size.x / float(FONT_SIZE)))
	drops.clear()
	var rows := size.y / float(FONT_SIZE)
	for i in range(columns):
		drops.append(randf() * rows)


func _process(delta: float) -> void:
	if size != _size_cache:
		_reset()
	_accum += delta
	if _accum < STEP_SEC:
		return
	_accum = 0.0
	var rows := size.y / float(FONT_SIZE)
	for i in range(drops.size()):
		drops[i] = float(drops[i]) + 1.0
		if float(drops[i]) * FONT_SIZE > size.y and randf() > 0.975:
			drops[i] = 0.0
	queue_redraw()


func _draw() -> void:
	if _font == null or drops.is_empty():
		return
	for i in range(drops.size()):
		var head: float = drops[i]
		for t in range(trail):
			var row := head - float(t)
			if row < 0.0:
				continue
			var alpha := opacity * (1.0 - float(t) / float(trail + 1))
			var ch := CHARS[randi() % CHARS.length()]
			# редкие розовые символы, как в оригинале (Math.random() > 0.12)
			var color := Neon.GREEN if randf() > 0.12 else Neon.PINK
			color.a = alpha
			var pos := Vector2(float(i * FONT_SIZE), row * float(FONT_SIZE))
			_font.draw_string(get_canvas_item(), pos, ch,
				HORIZONTAL_ALIGNMENT_LEFT, -1, FONT_SIZE, color)
