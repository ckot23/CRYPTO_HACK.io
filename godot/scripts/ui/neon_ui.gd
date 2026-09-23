class_name NeonUI
extends RefCounted

## Фабрика виджетов. Заменяет Tailwind-классы из веб-версии:
## вместо <div className="border border-[#00ff9d]/40 bg-[#00ff9d]/5 p-3">
## пишем NeonUI.card(Neon.GREEN, Neon.with_alpha(Neon.GREEN, 0.02)).
##
## Стили кнопок: SOLID (заливка цветом), OUTLINE (рамка + прозрачный фон),
## GHOST (только текст).

const KIND_SOLID := "solid"
const KIND_OUTLINE := "outline"
const KIND_GHOST := "ghost"


# ==================== БАЗОВЫЕ БЛОКИ ====================
static func label(text: String, size: int = 14, color: Color = Neon.TEXT) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", color)
	return l


static func bold_label(text: String, size: int = 14, color: Color = Neon.TEXT) -> Label:
	var l := label(text, size, color)
	l.add_theme_font_override("font", Neon.font_bold())
	return l


static func display_label(text: String, size: int = 28, color: Color = Neon.TEXT) -> Label:
	var l := label(text, size, color)
	l.add_theme_font_override("font", Neon.font_display())
	return l


static func display_black_label(text: String, size: int = 56, color: Color = Neon.TEXT) -> Label:
	var l := label(text, size, color)
	l.add_theme_font_override("font", Neon.font_display_black())
	l.add_theme_constant_override("outline_size", 0)
	return l


## Текст со свечением (аналог CSS text-shadow / классов glow-*).
static func glow_label(text: String, size: int = 14, color: Color = Neon.GREEN,
		bold: bool = true, glow: float = 0.45) -> Label:
	var l := bold_label(text, size, color) if bold else label(text, size, color)
	l.add_theme_constant_override("outline_size", maxi(2, int(size * 0.28)))
	l.add_theme_color_override("font_outline_color", Color(color.r, color.g, color.b, glow))
	return l


static func dim_label(text: String, size: int = 11, color: Color = Neon.TEXT_MUTED) -> Label:
	var l := label(text, size, color)
	return l


## Мелкий «бейдж» с рамкой: +0.0012 BTC, ур. 3 и т.п.
static func chip(text: String, color: Color, size: int = 11) -> PanelContainer:
	var p := PanelContainer.new()
	p.add_theme_stylebox_override("panel",
		Neon.sb(Neon.with_alpha(color, 0.10), Neon.with_alpha(color, 0.45), 1))
	var l := label(text, size, color)
	l.add_theme_font_override("font", Neon.font_bold())
	var m := MarginContainer.new()
	m.add_theme_constant_override("margin_left", 6)
	m.add_theme_constant_override("margin_right", 6)
	m.add_theme_constant_override("margin_top", 2)
	m.add_theme_constant_override("margin_bottom", 2)
	m.add_child(l)
	p.add_child(m)
	return p


## Карточка: рамка + полупрозрачный фон (+ опциональное свечение).
static func card(border: Color, bg: Color = Neon.PANEL_DARK, glow: float = 0.0,
		border_width: int = 1) -> PanelContainer:
	var p := PanelContainer.new()
	p.add_theme_stylebox_override("panel", Neon.sb(bg, border, border_width, glow))
	return p


## Карточка с внутренними отступами и содержимым.
static func card_with(border: Color, inner: Control, bg: Color = Neon.PANEL_DARK,
		pad: int = 10, glow: float = 0.0) -> PanelContainer:
	var p := card(border, bg, glow)
	var m := MarginContainer.new()
	m.add_theme_constant_override("margin_left", pad)
	m.add_theme_constant_override("margin_right", pad)
	m.add_theme_constant_override("margin_top", pad)
	m.add_theme_constant_override("margin_bottom", pad)
	m.add_child(inner)
	p.add_child(m)
	return p


static func margin(inner: Control, left: int = 0, top: int = 0, right: int = 0, bottom: int = 0) -> MarginContainer:
	var m := MarginContainer.new()
	m.add_theme_constant_override("margin_left", left)
	m.add_theme_constant_override("margin_right", right)
	m.add_theme_constant_override("margin_top", top)
	m.add_theme_constant_override("margin_bottom", bottom)
	if inner != null:
		m.add_child(inner)
	return m


# ==================== КОНТЕЙНЕРЫ ====================
static func hbox(sep: int = 6) -> HBoxContainer:
	var h := HBoxContainer.new()
	h.add_theme_constant_override("separation", sep)
	return h


static func vbox(sep: int = 6) -> VBoxContainer:
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", sep)
	return v


static func grid(columns: int = 2, sep: int = 6) -> GridContainer:
	var g := GridContainer.new()
	g.columns = columns
	g.add_theme_constant_override("h_separation", sep)
	g.add_theme_constant_override("v_separation", sep)
	return g


static func spacer(w: float = 0.0, h: float = 0.0) -> Control:
	var c := Control.new()
	c.custom_minimum_size = Vector2(w, h)
	return c


static func stretch() -> Control:
	var c := Control.new()
	c.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	return c


static func line(color: Color = Neon.WHITE_10, thickness: float = 1.0) -> ColorRect:
	var r := ColorRect.new()
	r.color = color
	r.custom_minimum_size = Vector2(0, thickness)
	return r


static func vline(color: Color = Neon.WHITE_10, thickness: float = 1.0) -> ColorRect:
	var r := ColorRect.new()
	r.color = color
	r.custom_minimum_size = Vector2(thickness, 0)
	return r


## Прокручиваемая область фиксированной высоты.
static func scroll(inner: Control, height: float = 200.0) -> ScrollContainer:
	var s := ScrollContainer.new()
	s.custom_minimum_size = Vector2(0, height)
	s.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	var theme_box := Neon.sb(Neon.PANEL_DEEP)
	s.add_theme_stylebox_override("panel", theme_box)
	inner.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	s.add_child(inner)
	return s


static func progress(color: Color = Neon.GREEN, height: float = 6.0) -> ProgressBar:
	var p := ProgressBar.new()
	p.custom_minimum_size = Vector2(0, height)
	p.show_percentage = false
	p.max_value = 1.0
	p.add_theme_stylebox_override("background", Neon.sb(Color("#0f1a2e"), Neon.WHITE_10, 1, 0.0))
	p.add_theme_stylebox_override("fill", Neon.sb(color))
	return p


# ==================== КНОПКИ ====================
static func button(text: String, color: Color = Neon.GREEN, kind: String = KIND_SOLID,
		size: int = 13) -> Button:
	var b := Button.new()
	b.text = text
	b.add_theme_font_override("font", Neon.font_bold())
	b.add_theme_font_size_override("font_size", size)
	b.add_theme_stylebox_override("focus", Neon.sb_empty())

	var normal_bg := Color(0, 0, 0, 0)
	var hover_bg := Color(0, 0, 0, 0)
	var pressed_bg := Color(0, 0, 0, 0)
	var border := Color(0, 0, 0, 0)
	var fg := color

	match kind:
		KIND_SOLID:
			normal_bg = color
			hover_bg = Neon.brightness(color, 1.12)
			pressed_bg = Neon.brightness(color, 0.85)
			fg = Neon.BLACK
		KIND_OUTLINE:
			normal_bg = Neon.with_alpha(color, 0.08)
			hover_bg = Neon.with_alpha(color, 0.20)
			pressed_bg = Neon.with_alpha(color, 0.30)
			border = Neon.with_alpha(color, 0.5)
		_:  # GHOST
			normal_bg = Color(0, 0, 0, 0)
			hover_bg = Neon.WHITE_05
			pressed_bg = Neon.WHITE_10
			fg = Neon.TEXT_DIM

	b.add_theme_stylebox_override("normal", Neon.sb(normal_bg, border, 1))
	b.add_theme_stylebox_override("hover", Neon.sb(hover_bg, border, 1))
	b.add_theme_stylebox_override("pressed", Neon.sb(pressed_bg, border, 1))
	b.add_theme_color_override("font_color", fg)
	b.add_theme_color_override("font_hover_color", Neon.BLACK if kind == KIND_SOLID else Color.WHITE)
	b.add_theme_color_override("font_pressed_color", Neon.BLACK if kind == KIND_SOLID else Color.WHITE)
	b.add_theme_color_override("font_disabled_color", Neon.TEXT_MUTED)
	return b


## Кнопка-иконка (закрыть окно и т.п.)
static func icon_button(text: String, color: Color = Neon.PINK, size: int = 14) -> Button:
	var b := button(text, color, KIND_OUTLINE, size)
	b.custom_minimum_size = Vector2(26, 24)
	return b


## Строка списка: подпись слева, значение справа.
static func key_value(key: String, value: String, key_color: Color = Neon.TEXT_MUTED,
		value_color: Color = Neon.TEXT, size: int = 12) -> HBoxContainer:
	var row := hbox(8)
	row.add_child(label(key, size, key_color))
	row.add_child(stretch())
	row.add_child(bold_label(value, size, value_color))
	return row


# ==================== УТИЛИТЫ ====================
## Очистить контейнер без мигания: queue_free() удаляет узел только в конце кадра,
## поэтому сначала убираем его из дерева, и лишь потом помечаем на удаление.
static func clear(container: Node) -> void:
	for child in container.get_children():
		container.remove_child(child)
		child.queue_free()


# ==================== АНИМАЦИИ (замена framer-motion) ====================
## Плавное появление: аналог initial={{opacity:0, y:20}} animate={{opacity:1, y:0}}
static func fade_in(node: Control, duration: float = 0.25, from_y: float = 16.0) -> void:
	node.modulate.a = 0.0
	var start := node.position.y
	node.position.y = start + from_y
	var tween := node.create_tween()
	tween.set_parallel(true)
	tween.tween_property(node, "modulate:a", 1.0, duration).set_trans(Tween.TRANS_QUAD)
	tween.tween_property(node, "position:y", start, duration).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)


## Плавное исчезновение с последующим удалением узла.
static func fade_out_and_free(node: Control, duration: float = 0.2, to_y: float = 10.0) -> void:
	var tween := node.create_tween()
	tween.set_parallel(true)
	tween.tween_property(node, "modulate:a", 0.0, duration)
	tween.tween_property(node, "position:y", node.position.y + to_y, duration)
	tween.chain().tween_callback(node.queue_free)


## Пульсация (btn-pulse из index.css).
static func pulse(node: Control, low: float = 0.75, high: float = 1.0, period: float = 1.1) -> void:
	var tween := node.create_tween().set_loops()
	tween.tween_property(node, "modulate:a", low, period * 0.5).set_trans(Tween.TRANS_SINE)
	tween.tween_property(node, "modulate:a", high, period * 0.5).set_trans(Tween.TRANS_SINE)
