class_name GridBg
extends Control

## Фон «сетка из точек» — порт классов .cyber-grid / .cyber-grid-faint из index.css.
## В CSS это была SVG-плитка, здесь — простая отрисовка в _draw().

var dot_color: Color = Neon.with_alpha(Neon.GREEN, 0.14)
var step: float = 28.0
var dot_radius: float = 1.0


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)
	# при изменении размера окна точки нужно перерисовать под новую ширину/высоту
	resized.connect(queue_redraw)


func _draw() -> void:
	var y := 0.0
	while y < size.y:
		var x := 0.0
		while x < size.x:
			draw_circle(Vector2(x, y), dot_radius, dot_color)
			x += step
		y += step
