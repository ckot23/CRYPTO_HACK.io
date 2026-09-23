class_name NeonWindow
extends PanelContainer

## Перетаскиваемое окно «рабочего стола» — аналог компонента WindowFrame из App.tsx.
##
## В вебе окно рисовалось как <motion.div> с position:fixed и drag-обработчиками.
## В Godot это обычный Control, который двигается вручную по InputEventMouseMotion,
## а порядок наложения задаётся через move_to_front().

signal focus_requested(window: NeonWindow)
signal close_requested(window: NeonWindow)

const DRAG_BLOCK_Y := 44.0        ## не заезжать под верхнюю панель
const HEADER_HEIGHT := 30

var title_text: String = ""
var accent: Color = Neon.GREEN
var wide: bool = false

var _header: PanelContainer
var _title_label: Label
var _body: MarginContainer
var _dragging := false
var _drag_offset := Vector2.ZERO
var _closing := false


func setup(title: String, color: Color, is_wide: bool = false) -> void:
	title_text = title
	accent = color
	wide = is_wide
	_build()
	_neon_style()


func _build() -> void:
	custom_minimum_size = Vector2(880.0 if wide else 660.0, 0.0)
	mouse_filter = Control.MOUSE_FILTER_STOP

	var root := VBoxContainer.new()
	root.add_theme_constant_override("separation", 0)
	add_child(root)

	# --- заголовок окна ---
	_header = PanelContainer.new()
	var head_style := StyleBoxFlat.new()
	head_style.bg_color = Neon.PANEL_HEAD
	head_style.border_color = Neon.WHITE_10
	head_style.border_width_bottom = 1
	head_style.content_margin_left = 10
	head_style.content_margin_right = 6
	head_style.content_margin_top = 5
	head_style.content_margin_bottom = 5
	_header.add_theme_stylebox_override("panel", head_style)
	_header.custom_minimum_size = Vector2(0, HEADER_HEIGHT)
	_header.mouse_filter = Control.MOUSE_FILTER_STOP
	_header.gui_input.connect(_on_header_input)
	root.add_child(_header)

	var head_row := HBoxContainer.new()
	head_row.add_theme_constant_override("separation", 8)
	_header.add_child(head_row)

	var marker := ColorRect.new()
	marker.color = accent
	marker.custom_minimum_size = Vector2(3, 14)
	head_row.add_child(marker)

	_title_label = NeonUI.bold_label(title_text, 12, accent)
	head_row.add_child(_title_label)
	head_row.add_child(NeonUI.stretch())

	var dots := NeonUI.label("● ● ○", 9, Neon.TEXT_FAINT)
	head_row.add_child(dots)

	var close_btn := NeonUI.icon_button("X", Neon.PINK, 12)
	close_btn.pressed.connect(request_close)
	head_row.add_child(close_btn)

	# --- тело окна ---
	# Обёрнуто в ScrollContainer: если контента больше, чем помещается на экране,
	# окно не «разъезжается», а прокручивается мышью.
	var body_scroll := ScrollContainer.new()
	body_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	body_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body_scroll.add_theme_stylebox_override("panel", Neon.sb(Color(0, 0, 0, 0), Color(0, 0, 0, 0), 0))
	root.add_child(body_scroll)

	_body = MarginContainer.new()
	_body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body_scroll.add_child(_body)


func _neon_style() -> void:
	var sb := Neon.sb(Neon.PANEL_DARK, Neon.with_alpha(accent, 0.25), 1)
	sb.shadow_color = Color(0, 0, 0, 0.65)
	sb.shadow_size = 24
	add_theme_stylebox_override("panel", sb)


## Положить содержимое в окно.
func set_body(control: Control) -> void:
	NeonUI.clear(_body)
	control.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_body.add_child(control)


## Установить заголовок (например, с количеством выполненных миссий).
func set_title(text: String) -> void:
	title_text = text
	if _title_label != null:
		_title_label.text = text


func _gui_input(event: InputEvent) -> void:
	# клик по любому месту окна поднимает его наверх
	if event is InputEventMouseButton and event.pressed:
		focus_requested.emit(self)


# ---------- перетаскивание ----------
func _on_header_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		var mb := event as InputEventMouseButton
		if mb.button_index == MOUSE_BUTTON_LEFT:
			_dragging = mb.pressed
			if _dragging:
				_drag_offset = get_global_mouse_position() - global_position
				focus_requested.emit(self)
			accept_event()
	elif event is InputEventMouseMotion and _dragging:
		var parent_size := get_parent_area_size()
		var target := get_global_mouse_position() - _drag_offset
		target.x = clampf(target.x, -size.x + 220.0, maxf(parent_size.x - 120.0, 220.0))
		target.y = clampf(target.y, DRAG_BLOCK_Y, maxf(parent_size.y - 60.0, DRAG_BLOCK_Y + 10.0))
		global_position = target
		accept_event()


func request_close() -> void:
	if _closing:
		return
	_closing = true
	close_requested.emit(self)
	NeonUI.fade_out_and_free(self, 0.16, 12.0)


func bring_to_front() -> void:
	move_to_front()


## Начальная позиция «со смещением» как в веб-версии (seeds по id окна).
static func seeded_position(index: int) -> Vector2:
	var s := float(index % 7) * 36.0
	return Vector2(48.0 + s, 72.0 + s)
