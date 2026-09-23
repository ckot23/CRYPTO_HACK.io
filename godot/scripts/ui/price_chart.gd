class_name PriceChart
extends Control

## График курса монеты. В веб-версии график был SVG-полилинией прямо в JSX
## (см. TradeWindow в App.tsx) — здесь это Control._draw() с draw_polyline().

var values: Array[float] = []
var line_color: Color = Neon.GREEN
var show_dots: bool = true


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	custom_minimum_size = Vector2(0, 110)


func set_values(points: Array[float], color: Color) -> void:
	values = points
	line_color = color
	queue_redraw()


func _draw() -> void:
	var w := size.x
	var h := size.y
	# сетка
	var grid_color := Color(1, 1, 1, 0.05)
	for i in range(1, 4):
		var y := h * float(i) / 4.0
		draw_line(Vector2(0, y), Vector2(w, y), grid_color, 1.0)

	if values.size() < 2:
		return

	var min_v := values[0]
	var max_v := values[0]
	for v in values:
		min_v = minf(min_v, v)
		max_v = maxf(max_v, v)
	var span := maxf(max_v - min_v, 0.000001)

	var points := PackedVector2Array()
	for i in range(values.size()):
		var x := w * float(i) / float(values.size() - 1)
		var y := h - ((values[i] - min_v) / span) * (h * 0.82) - h * 0.09
		points.append(Vector2(x, y))

	# мягкая «заливка» под линией
	var area := points.duplicate()
	area.append(Vector2(w, h))
	area.append(Vector2(0, h))
	draw_colored_polygon(area, Color(line_color.r, line_color.g, line_color.b, 0.10))

	draw_polyline(points, line_color, 2.0, true)

	if show_dots:
		for i in range(0, points.size(), 8):
			draw_circle(points[i], 2.5, line_color)
