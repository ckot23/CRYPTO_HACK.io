extends Control

## Стартовый экран (главное меню) — порт StartScreen из App.tsx.
## Гигантский заголовок CRYPTO_HACK с glitch-эффектом, карточки фич,
## цитата хакера и модалка «Как играть».

const FEATURES := [
	{"icon": "⌁", "t": "8 миссий-взломов", "color_key": "green"},
	{"icon": "⚙", "t": "Пассивный майнинг", "color_key": "cyan"},
	{"icon": "▲", "t": "Живая биржа", "color_key": "yellow"},
	{"icon": "✎", "t": "Школа Python", "color_key": "pink"},
]

const HELP_STEPS := [
	["1. Взламывай.", "Открой «Хак-терминал», выбери миссию и пиши Python-код по обучению. Каждая миссия учит новой конструкции языка."],
	["2. Ставь майнеры.", "Взломанные компы — твои. Установи на них майнер, и крипта будет капать даже пока ты читаешь обучение."],
	["3. Торгуй.", "Продавай крипту за доллары на бирже, когда цена высокая. Следи за графиком!"],
	["4. Прокачивайся.", "За доллары покупай ускорение взлома, мощность майнинга и новые функции кода."],
	["5. Расти.", "Опыт поднимает уровень и открывает новые монеты: BTC → ETH → XMR → SOL."],
]

var _title: Label
var _help_layer: Control
var _quote: Label


func _color_for(key: String) -> Color:
	match key:
		"green": return Neon.GREEN
		"cyan": return Neon.CYAN
		"yellow": return Neon.YELLOW
		"pink": return Neon.PINK
		_: return Neon.TEXT


func _ready() -> void:
	Neon.apply(self)
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_background()
	_build_topbar()
	_build_center()
	_build_footer()
	NeonUI.fade_in(_title, 0.7, 24.0)

	var quote_timer := Timer.new()
	quote_timer.wait_time = 8.0
	quote_timer.autostart = true
	quote_timer.timeout.connect(_refresh_quote)
	add_child(quote_timer)
	_refresh_quote()


func _build_background() -> void:
	var bg := ColorRect.new()
	bg.color = Neon.BG
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)
	var grid := GridBg.new()
	grid.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(grid)
	var matrix := MatrixBg.new()
	matrix.set_anchors_preset(Control.PRESET_FULL_RECT)
	matrix.set_opacity(0.30)
	add_child(matrix)


func _build_topbar() -> void:
	var bar := NeonUI.card(Neon.with_alpha(Neon.GREEN, 0.2), Neon.with_alpha(Neon.BG, 0.85))
	bar.set_anchors_preset(Control.PRESET_TOP_WIDE)
	bar.offset_bottom = 30
	var row := NeonUI.hbox(12)
	row.add_child(NeonUI.label("● NEON_NET :: ЗАЩИЩЁННОЕ СОЕДИНЕНИЕ", 11, Neon.with_alpha(Neon.GREEN, 0.75)))
	row.add_child(NeonUI.stretch())
	row.add_child(NeonUI.label("TOR-узел: 185.220.70.4 :: ping 12ms", 11, Neon.TEXT_MUTED))
	bar.add_child(NeonUI.margin(row, 12, 4, 12, 4))
	add_child(bar)


func _build_center() -> void:
	var center := VBoxContainer.new()
	center.set_anchors_preset(Control.PRESET_CENTER)
	center.offset_left = -420
	center.offset_right = 420
	center.offset_top = -230
	center.offset_bottom = 240
	center.add_theme_constant_override("separation", 14)
	center.alignment = BoxContainer.ALIGNMENT_CENTER
	add_child(center)

	var badge := NeonUI.chip("★ СИМУЛЯТОР ХАКЕРА // УЧИ PYTHON ВЗЛАМЫВАЯ", Neon.PINK, 11)
	badge.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	center.add_child(badge)

	# заголовок с glitch-подсветкой
	var title_holder := Control.new()
	title_holder.custom_minimum_size = Vector2(840, 96)
	center.add_child(title_holder)

	var back_cyan := NeonUI.display_black_label("CRYPTO_HACK", 58, Color(Neon.CYAN.r, Neon.CYAN.g, Neon.CYAN.b, 0.55))
	var back_pink := NeonUI.display_black_label("CRYPTO_HACK", 58, Color(Neon.PINK.r, Neon.PINK.g, Neon.PINK.b, 0.55))
	_title = NeonUI.display_black_label("CRYPTO_HACK", 58, Color(0.92, 1.0, 0.96))
	for l in [back_cyan, back_pink, _title]:
		l.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		l.set_anchors_preset(Control.PRESET_FULL_RECT)
		title_holder.add_child(l)
	# глитч-дрожание подложек
	var g1 := back_cyan.create_tween().set_loops()
	g1.tween_property(back_cyan, "position", Vector2(-3, 2), 0.12)
	g1.tween_property(back_cyan, "position", Vector2(3, -2), 0.18)
	g1.tween_property(back_cyan, "position", Vector2.ZERO, 0.14)
	var g2 := back_pink.create_tween().set_loops()
	g2.tween_property(back_pink, "position", Vector2(3, -1), 0.16)
	g2.tween_property(back_pink, "position", Vector2(-2, 2), 0.12)
	g2.tween_property(back_pink, "position", Vector2.ZERO, 0.2)

	var subtitle := NeonUI.label(
		"Пиши настоящий Python-код, взламывай криптокошельки, ставь майнеры на чужие компы,\n"
		+ "торгуй на бирже и стань легендой даркнета.", 13, Neon.TEXT_DIM)
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	center.add_child(subtitle)

	var features := NeonUI.grid(4, 8)
	features.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	for f in FEATURES:
		var color := _color_for(str(f["color_key"]))
		var box := NeonUI.hbox(6)
		box.add_child(NeonUI.display_label(str(f["icon"]), 15, color))
		box.add_child(NeonUI.label(str(f["t"]), 11, Neon.TEXT))
		features.add_child(NeonUI.card_with(
			Neon.with_alpha(color, 0.25), box, Neon.with_alpha(Neon.PANEL_DARK, 0.9), 8))
	center.add_child(features)

	var buttons := NeonUI.hbox(10)
	buttons.alignment = BoxContainer.ALIGNMENT_CENTER
	var start := NeonUI.button("⌁ " + ("ПРОДОЛЖИТЬ ВЗЛОМ" if Game.has_save else "НАЧАТЬ ИГРУ"),
		Neon.GREEN, NeonUI.KIND_SOLID, 13)
	start.custom_minimum_size = Vector2(240, 46)
	start.pressed.connect(_start_game)
	buttons.add_child(start)
	NeonUI.pulse(start, 0.82, 1.0, 1.6)
	var help := NeonUI.button("КАК ИГРАТЬ", Neon.CYAN, NeonUI.KIND_OUTLINE, 13)
	help.custom_minimum_size = Vector2(180, 46)
	help.pressed.connect(_show_help)
	buttons.add_child(help)
	center.add_child(buttons)

	if Game.has_save:
		var reset := NeonUI.button("стереть сохранение и начать заново", Neon.TEXT_FAINT, NeonUI.KIND_GHOST, 10)
		reset.pressed.connect(func() -> void:
			Game.reset_progress()
			_start_game())
		center.add_child(reset)

	_quote = NeonUI.label("", 11, Neon.TEXT_MUTED)
	_quote.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	center.add_child(_quote)


func _build_footer() -> void:
	var footer := NeonUI.label("NEON_OS v3.7 :: сделано для будущих python-хакеров",
		10, Neon.TEXT_FAINT)
	footer.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	footer.offset_top = -24
	footer.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(footer)


func _refresh_quote() -> void:
	if Game.data.quotes.is_empty():
		return
	var idx := int(Time.get_ticks_msec() / 8000) % Game.data.quotes.size()
	_quote.text = "★ " + Game.data.quotes[idx]


func _start_game() -> void:
	if Game.sound_on:
		Sfx.ui_click()
	get_tree().change_scene_to_file("res://scenes/boot.tscn")


func _show_help() -> void:
	if _help_layer != null and is_instance_valid(_help_layer):
		_help_layer.queue_free()
		return
	_help_layer = ColorRect.new()
	_help_layer.color = Color(0, 0, 0, 0.8)
	_help_layer.set_anchors_preset(Control.PRESET_FULL_RECT)
	_help_layer.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(_help_layer)

	var box := NeonUI.vbox(8)
	box.add_child(NeonUI.glow_label("КАК ИГРАТЬ", 18, Neon.GREEN))
	for step in HELP_STEPS:
		var row := NeonUI.vbox(0)
		row.add_child(NeonUI.bold_label(str(step[0]), 12, Neon.CYAN))
		var text := NeonUI.label(str(step[1]), 12, Neon.TEXT_SOFT)
		text.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		text.custom_minimum_size = Vector2(460, 0)
		row.add_child(text)
		box.add_child(row)
	var go := NeonUI.button("ПОНЯЛ, ПОГНАЛИ!", Neon.GREEN, NeonUI.KIND_SOLID, 12)
	go.pressed.connect(_start_game)
	box.add_child(go)

	var card := NeonUI.card_with(Neon.with_alpha(Neon.GREEN, 0.45), box, Neon.PANEL_DARK, 20, 1.0)
	card.set_anchors_preset(Control.PRESET_CENTER)
	card.offset_left = -280
	card.offset_right = 280
	card.offset_top = -230
	card.offset_bottom = 230
	_help_layer.add_child(card)
	NeonUI.fade_in(card, 0.25, 16.0)
