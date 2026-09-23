extends Control

## Рабочий стол NeonOS — главный игровой экран (порт «desktop»-части App.tsx).
##
## Здесь живут: топбар с курсами, иконки программ, менеджер окон, панель задач,
## уведомления, обучение и модалка повышения уровня.
## Каждое «приложение» — отдельный скрипт из scripts/ui/, окна строятся на NeonWindow.
##
## Важно про производительность: HUD обновляется каждый игровой тик (майнеры
## начисляют крипту раз в секунду), поэтому виджеты создаются один раз, а дальше
## меняется только текст — иначе узлы пересоздавались бы по 10 раз в секунду.

const TOPBAR_H := 34.0
const TASKBAR_H := 40.0
const ICON_COLUMN_W := 96.0

const WINDOWS := {
	"hack": {"title": "NEON_HACK // ТЕРМИНАЛ ВЗЛОМА", "color_key": "green", "icon": "⌁",
		"label": "Хак-терминал", "script": "res://scripts/ui/hack_window.gd", "wide": true},
	"miner": {"title": "МАЙНИНГ-ФЕРМА", "color_key": "cyan", "icon": "⚙",
		"label": "Майнеры", "script": "res://scripts/ui/miner_window.gd", "wide": false},
	"trade": {"title": "БИРЖА DARKEX", "color_key": "yellow", "icon": "▲",
		"label": "Биржа", "script": "res://scripts/ui/trade_window.gd", "wide": false},
	"upgrade": {"title": "ЧЁРНЫЙ РЫНОК // АПГРЕЙДЫ", "color_key": "orange", "icon": "✚",
		"label": "Апгрейды", "script": "res://scripts/ui/upgrade_window.gd", "wide": false},
	"learn": {"title": "ШКОЛА PYTHON", "color_key": "pink", "icon": "✎",
		"label": "Школа Python", "script": "res://scripts/ui/learn_window.gd", "wide": true},
	"files": {"title": "ФАЙЛЫ // /home/ghost", "color_key": "cyan", "icon": "▶",
		"label": "Файлы", "script": "res://scripts/ui/files_window.gd", "wide": false},
	"profile": {"title": "ПРОФИЛЬ ХАКЕРА", "color_key": "yellow", "icon": "★",
		"label": "Профиль", "script": "res://scripts/ui/profile_window.gd", "wide": false},
}

const ICON_ORDER := ["hack", "miner", "trade", "upgrade", "learn", "files", "profile"]

var _window_layer: Control
var _overlay_layer: Control
var _icon_column: VBoxContainer
var _taskbar_buttons: HBoxContainer
var _taskbar_right: HBoxContainer
var _topbar_tickers: HBoxContainer
var _topbar_right: HBoxContainer
var _toast_layer: ToastLayer
var _start_menu: PanelContainer
var _windows := {}

# ссылки на часто обновляемые подписи
var _ticker_labels := {}
var _badge_labels := {}
var _income_label: Label
var _btc_label: Label
var _xp_label: Label
var _money_label: Label
var _clock_label: Label


func _color_for(key: String) -> Color:
	match key:
		"green": return Neon.GREEN
		"cyan": return Neon.CYAN
		"yellow": return Neon.YELLOW
		"orange": return Neon.ORANGE
		"pink": return Neon.PINK
		_: return Neon.TEXT


func _ready() -> void:
	Neon.apply(self)
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_build_background()
	_build_topbar()
	_window_layer = Control.new()
	_window_layer.set_anchors_preset(Control.PRESET_FULL_RECT)
	_window_layer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_window_layer)
	_build_icon_column()
	_build_taskbar()
	_build_toasts()
	_overlay_layer = Control.new()
	_overlay_layer.set_anchors_preset(Control.PRESET_FULL_RECT)
	_overlay_layer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(_overlay_layer)

	Game.state_changed.connect(_refresh_hud)
	Game.prices_changed.connect(_refresh_hud)
	Game.toast_requested.connect(_on_toast)
	Game.level_up.connect(_on_level_up)

	_refresh_hud()
	var clock_timer := Timer.new()
	clock_timer.wait_time = 10.0
	clock_timer.autostart = true
	clock_timer.timeout.connect(_update_clock)
	add_child(clock_timer)

	_open_window("hack")
	if not Game.has_save:
		_show_onboarding()


# ==================== ФОН ====================
func _build_background() -> void:
	var bg := ColorRect.new()
	bg.color = Neon.BG
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)

	var grid := GridBg.new()
	grid.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(grid)

	var matrix := MatrixBg.new()
	matrix.set_anchors_preset(Control.PRESET_FULL_RECT)
	matrix.set_opacity(0.10)
	add_child(matrix)

	# «луч сканирования» — .scan-beam из index.css (ездит сверху вниз)
	var beam := ColorRect.new()
	beam.color = Neon.with_alpha(Neon.GREEN, 0.06)
	beam.set_anchors_preset(Control.PRESET_TOP_WIDE)
	beam.offset_top = -140.0
	beam.offset_bottom = -20.0
	beam.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(beam)
	var tween := beam.create_tween().set_loops()
	tween.tween_property(beam, "offset_top", -140.0, 0.01)
	tween.tween_property(beam, "offset_bottom", -20.0, 0.01)
	tween.tween_property(beam, "offset_top", 760.0, 6.0).set_trans(Tween.TRANS_LINEAR)
	tween.parallel().tween_property(beam, "offset_bottom", 880.0, 6.0).set_trans(Tween.TRANS_LINEAR)


# ==================== ТОПБАР ====================
func _build_topbar() -> void:
	var bar := PanelContainer.new()
	bar.set_anchors_preset(Control.PRESET_TOP_WIDE)
	bar.offset_bottom = TOPBAR_H
	bar.add_theme_stylebox_override("panel",
		Neon.sb(Color(0.016, 0.027, 0.058, 0.95), Neon.with_alpha(Neon.GREEN, 0.25), 1))
	add_child(bar)

	var row := NeonUI.hbox(12)
	bar.add_child(NeonUI.margin(row, 12, 4, 12, 4))
	row.add_child(NeonUI.bold_label("⌁ NEON_OS", 11, Neon.GREEN))

	_topbar_tickers = NeonUI.hbox(8)
	row.add_child(_topbar_tickers)
	for info in Game.data.cryptos:
		var box := NeonUI.hbox(4)
		box.add_child(NeonUI.bold_label(info.icon, 11, info.color))
		var price := NeonUI.label("", 11, Color.WHITE)
		var balance := NeonUI.label("", 11, Neon.TEXT_DIM)
		box.add_child(price)
		box.add_child(balance)
		var card := NeonUI.card_with(Neon.WHITE_10, box, Color(0, 0, 0, 0.5), 5)
		card.visible = false
		_topbar_tickers.add_child(card)
		_ticker_labels[info.id] = {"card": card, "price": price, "balance": balance,
			"level": info.unlock_level}

	row.add_child(NeonUI.stretch())
	_topbar_right = NeonUI.hbox(10)
	row.add_child(_topbar_right)
	var xp_chip := _chip_with_ref("", Neon.GREEN)
	var money_chip := _chip_with_ref("", Neon.YELLOW)
	_xp_label = xp_chip[1]
	_money_label = money_chip[1]
	_clock_label = NeonUI.label("--:--", 11, Neon.TEXT_MUTED)
	_topbar_right.add_child(xp_chip[0])
	_topbar_right.add_child(money_chip[0])
	_topbar_right.add_child(_clock_label)


func _refresh_tickers() -> void:
	for id in _ticker_labels:
		var rec: Dictionary = _ticker_labels[id]
		var info := Game.data.crypto(id)
		var card: Control = rec["card"]
		card.visible = Game.level >= info.unlock_level
		if not card.visible:
			continue
		rec["price"].text = Neon.fmt_price(float(Game.prices[id]))
		rec["balance"].text = Neon.fmt_crypto(float(Game.crypto.get(id, 0.0)))


## Чип с доступом к внутренней подписи (чтобы не искать её по дереву каждый тик).
func _chip_with_ref(text: String, color: Color) -> Array:
	var panel := PanelContainer.new()
	panel.add_theme_stylebox_override("panel",
		Neon.sb(Neon.with_alpha(color, 0.10), Neon.with_alpha(color, 0.45), 1))
	var label := NeonUI.bold_label(text, 11, color)
	panel.add_child(NeonUI.margin(label, 6, 2, 6, 2))
	return [panel, label]


func _refresh_topbar_right() -> void:
	_xp_label.text = "⚡ ур.%d · %d/%d XP" % [Game.level, Game.xp, Game.xp_for_level(Game.level)]
	_money_label.text = "$ %s" % Neon.fmt_dollars(Game.dollars)
	_update_clock()


func _update_clock() -> void:
	if _clock_label == null or not is_instance_valid(_clock_label):
		return
	var t := Time.get_time_dict_from_system()
	_clock_label.text = "%02d:%02d" % [int(t.get("hour", 0)), int(t.get("minute", 0))]


# ==================== ИКОНКИ ====================
func _build_icon_column() -> void:
	var holder := MarginContainer.new()
	holder.set_anchors_preset(Control.PRESET_LEFT_WIDE)
	holder.offset_left = 10
	holder.offset_top = TOPBAR_H + 14
	holder.offset_right = 10 + ICON_COLUMN_W
	holder.offset_bottom = -TASKBAR_H - 8
	add_child(holder)
	_icon_column = NeonUI.vbox(2)
	holder.add_child(_icon_column)

	for id in ICON_ORDER:
		var meta: Dictionary = WINDOWS[id]
		var color := _color_for(str(meta["color_key"]))
		var b := Button.new()
		b.custom_minimum_size = Vector2(ICON_COLUMN_W, 56)
		b.add_theme_stylebox_override("normal", Neon.sb(Color(0, 0, 0, 0), Color(0, 0, 0, 0), 0))
		b.add_theme_stylebox_override("hover", Neon.sb(Neon.WHITE_05, Color(0, 0, 0, 0), 0))
		b.add_theme_stylebox_override("pressed", Neon.sb(Neon.WHITE_10, Color(0, 0, 0, 0), 0))
		b.add_theme_stylebox_override("focus", Neon.sb_empty())

		var box := NeonUI.vbox(1)
		box.mouse_filter = Control.MOUSE_FILTER_IGNORE
		var glyph := NeonUI.display_label(str(meta["icon"]), 19, color)
		glyph.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		box.add_child(glyph)
		var caption := NeonUI.label(str(meta["label"]), 10, Neon.TEXT)
		caption.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		caption.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		caption.custom_minimum_size = Vector2(ICON_COLUMN_W - 6, 0)
		box.add_child(caption)

		var badge := NeonUI.bold_label("", 9, color)
		badge.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		box.add_child(badge)
		_badge_labels[id] = badge

		b.add_child(NeonUI.margin(box, 2, 4, 2, 4))
		var win_id: String = id
		b.pressed.connect(func() -> void: _open_window(win_id))
		_icon_column.add_child(b)


func _badge_for(id: String) -> String:
	match id:
		"hack":
			return "%d/%d" % [Game.completed_missions.size(), Game.data.missions.size()]
		"miner":
			return "" if Game.miners.is_empty() else str(Game.miners.size())
		"learn":
			return "%d/%d" % [Game.completed_lessons.size(), Game.data.lessons.size()]
		_:
			return ""


func _refresh_icons() -> void:
	for id in _badge_labels:
		_badge_labels[id].text = _badge_for(id)


# ==================== ПАНЕЛЬ ЗАДАЧ ====================
func _build_taskbar() -> void:
	var bar := PanelContainer.new()
	bar.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	bar.offset_top = -TASKBAR_H
	bar.add_theme_stylebox_override("panel",
		Neon.sb(Color(0.016, 0.027, 0.058, 0.95), Neon.with_alpha(Neon.GREEN, 0.25), 1))
	add_child(bar)

	var row := NeonUI.hbox(8)
	bar.add_child(NeonUI.margin(row, 10, 5, 10, 5))
	var start_btn := NeonUI.button("⌁ GHOST", Neon.GREEN, NeonUI.KIND_OUTLINE, 11)
	start_btn.pressed.connect(_toggle_start_menu)
	row.add_child(start_btn)

	_taskbar_buttons = NeonUI.hbox(6)
	row.add_child(_taskbar_buttons)
	row.add_child(NeonUI.stretch())

	_taskbar_right = NeonUI.hbox(10)
	_income_label = NeonUI.label("", 10, Neon.GREEN)
	_btc_label = NeonUI.label("", 10, Neon.TEXT_DIM)
	_taskbar_right.add_child(_income_label)
	_taskbar_right.add_child(_btc_label)
	row.add_child(_taskbar_right)


func _rebuild_taskbar_buttons() -> void:
	NeonUI.clear(_taskbar_buttons)
	for id in _windows.keys():
		var meta: Dictionary = WINDOWS[id]
		var b := NeonUI.button(str(meta["title"]).split("//")[0].strip_edges(),
			_color_for(str(meta["color_key"])), NeonUI.KIND_OUTLINE, 10)
		var win_id: String = id
		b.pressed.connect(func() -> void: _close_window(win_id))
		_taskbar_buttons.add_child(b)


func _refresh_taskbar_labels() -> void:
	if Game.miners.is_empty():
		_income_label.text = ""
	else:
		_income_label.text = "⚙ +%s/мин" % Neon.fmt_dollars(Game.miner_income_per_min())
	_btc_label.text = "%s BTC" % Neon.fmt_crypto(float(Game.crypto.get("BTC", 0.0)))


func _refresh_hud() -> void:
	_refresh_tickers()
	_refresh_topbar_right()
	_refresh_icons()
	_refresh_taskbar_labels()


func _toggle_start_menu() -> void:
	if _start_menu != null and is_instance_valid(_start_menu):
		_start_menu.queue_free()
		_start_menu = null
		return
	var menu := NeonUI.card(Neon.with_alpha(Neon.GREEN, 0.45), Neon.PANEL_DARK, 0.8)
	menu.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
	menu.offset_left = 10
	menu.offset_right = 242
	menu.offset_top = -TASKBAR_H - 8 - 136
	menu.offset_bottom = -TASKBAR_H - 8
	var box := NeonUI.vbox(0)
	box.add_child(_menu_item("Звук: " + ("ВКЛ" if Game.sound_on else "ВЫКЛ"), func() -> void:
		Game.sound_on = not Game.sound_on
		Game.save()
		_toggle_start_menu()))
	box.add_child(_menu_item("Показать обучение", func() -> void:
		_toggle_start_menu()
		_show_onboarding()))
	box.add_child(_menu_item("Сбросить прогресс", func() -> void:
		_toggle_start_menu()
		Game.reset_progress()
		_refresh_hud()))
	box.add_child(_menu_item("В главное меню", func() -> void:
		get_tree().change_scene_to_file("res://scenes/main_menu.tscn")))
	menu.add_child(NeonUI.margin(box, 6, 6, 6, 6))
	add_child(menu)
	_start_menu = menu


func _menu_item(text: String, action: Callable) -> Button:
	var b := NeonUI.button(text, Neon.TEXT, NeonUI.KIND_GHOST, 11)
	b.alignment = HORIZONTAL_ALIGNMENT_LEFT
	b.custom_minimum_size = Vector2(0, 30)
	b.pressed.connect(action)
	return b


# ==================== ТОСТЫ / МОДАЛКИ ====================
func _build_toasts() -> void:
	_toast_layer = ToastLayer.new()
	_toast_layer.set_anchors_preset(Control.PRESET_FULL_RECT)
	_toast_layer.offset_top = TOPBAR_H
	_toast_layer.offset_bottom = -TASKBAR_H
	add_child(_toast_layer)


func _on_toast(title: String, text: String, kind: String) -> void:
	if _toast_layer != null:
		_toast_layer.push_toast(title, text, kind)


func _modal_root() -> ColorRect:
	var root := ColorRect.new()
	root.color = Color(0, 0, 0, 0.72)
	root.set_anchors_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter = Control.MOUSE_FILTER_STOP
	_overlay_layer.add_child(root)
	return root


func _on_level_up(level: int) -> void:
	var modal := _modal_root()
	var box := NeonUI.vbox(10)
	box.add_child(NeonUI.display_label("LEVEL %d" % level, 26, Neon.YELLOW))
	var info := ""
	for c in Game.data.cryptos:
		if c.unlock_level == level:
			info = "Разблокирована %s! Новые миссии и майнеры ждут." % c.name
	if info == "":
		info = "Ты становишься сильнее. Продолжай взламывать!"
	var label := NeonUI.label(info, 12, Neon.TEXT_SOFT)
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	label.custom_minimum_size = Vector2(300, 0)
	box.add_child(label)
	var ok := NeonUI.button("ЗАБРАТЬ НАГРАДУ", Neon.YELLOW, NeonUI.KIND_SOLID, 12)
	ok.pressed.connect(func() -> void: modal.queue_free())
	box.add_child(ok)

	var card := NeonUI.card_with(Neon.with_alpha(Neon.YELLOW, 0.5), box, Neon.PANEL_DARK, 20, 1.0)
	card.set_anchors_preset(Control.PRESET_CENTER)
	card.offset_left = -180
	card.offset_right = 180
	card.offset_top = -120
	card.offset_bottom = 120
	modal.add_child(card)
	NeonUI.fade_in(card, 0.3, 18.0)


func _show_onboarding() -> void:
	var steps := [
		{"t": "Добро пожаловать, ghost!", "d": "Это NeonOS — твоя хакерская ОС. Слева — иконки программ, сверху — деньги и крипта, снизу — панель задач.", "go": ""},
		{"t": "Шаг 1: взломай первую цель", "d": "Открой «Хак-терминал». Там миссии, которые шаг за шагом учат Python. Следуй подсказкам и жми «Запустить».", "go": "hack"},
		{"t": "Шаг 2: ставь майнеры", "d": "Каждый взломанный комп — источник пассивного дохода. Открой «Майнеры» и установи риг: крипта капает каждую секунду.", "go": "miner"},
		{"t": "Шаг 3: торгуй и качайся", "d": "Продавай крипту за доллары на «Бирже», трать их на «Апгрейды». «Школа Python» даёт теорию и XP.", "go": "trade"},
	]
	var modal := _modal_root()
	var box := NeonUI.vbox(10)
	var step_label := NeonUI.label("", 10, Neon.TEXT_MUTED)
	var title := NeonUI.bold_label("", 15, Color.WHITE)
	var body := NeonUI.label("", 12, Neon.TEXT_SOFT)
	body.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	body.custom_minimum_size = Vector2(400, 90)
	box.add_child(step_label)
	box.add_child(title)
	box.add_child(body)

	var buttons := NeonUI.hbox(8)
	var back := NeonUI.button("Назад", Neon.TEXT_DIM, NeonUI.KIND_OUTLINE, 11)
	buttons.add_child(back)
	buttons.add_child(NeonUI.stretch())
	var action := NeonUI.button("Открыть", Neon.GREEN, NeonUI.KIND_OUTLINE, 11)
	buttons.add_child(action)
	var next := NeonUI.button("Далее", Neon.CYAN, NeonUI.KIND_SOLID, 11)
	buttons.add_child(next)
	box.add_child(buttons)
	var skip := NeonUI.button("пропустить обучение", Neon.TEXT_FAINT, NeonUI.KIND_GHOST, 10)
	box.add_child(skip)

	var card := NeonUI.card_with(Neon.with_alpha(Neon.CYAN, 0.5), box, Neon.PANEL_DARK, 20, 1.0)
	card.set_anchors_preset(Control.PRESET_CENTER)
	card.offset_left = -250
	card.offset_right = 250
	card.offset_top = -180
	card.offset_bottom = 180
	modal.add_child(card)

	var index := {"v": 0}
	var render := func() -> void:
		var i: int = index["v"]
		var s: Dictionary = steps[i]
		step_label.text = "ОБУЧЕНИЕ · %d/%d" % [i + 1, steps.size()]
		title.text = str(s["t"])
		body.text = str(s["d"])
		action.visible = str(s["go"]) != ""
		next.text = "НАЧАТЬ ВЗЛОМ!" if i == steps.size() - 1 else "Далее"

	back.pressed.connect(func() -> void:
		index["v"] = maxi(0, int(index["v"]) - 1)
		render.call())
	next.pressed.connect(func() -> void:
		if int(index["v"]) >= steps.size() - 1:
			modal.queue_free()
			return
		index["v"] = int(index["v"]) + 1
		render.call())
	action.pressed.connect(func() -> void:
		var go := str(steps[int(index["v"])]["go"])
		if go != "":
			_open_window(go))
	skip.pressed.connect(func() -> void: modal.queue_free())
	render.call()
	NeonUI.fade_in(card, 0.28, 18.0)


# ==================== МЕНЕДЖЕР ОКОН ====================
func _open_window(id: String) -> void:
	if _windows.has(id) and is_instance_valid(_windows[id]):
		_windows[id].bring_to_front()
		_windows[id].visible = true
		return
	var meta: Dictionary = WINDOWS[id]
	var win := NeonWindow.new()
	win.setup(str(meta["title"]), _color_for(str(meta["color_key"])), bool(meta["wide"]))
	win.position = NeonWindow.seeded_position(ICON_ORDER.find(id))
	win.focus_requested.connect(func(w: NeonWindow) -> void: w.bring_to_front())
	win.close_requested.connect(func(_w: NeonWindow) -> void:
		_windows.erase(id)
		_rebuild_taskbar_buttons())

	var script: GDScript = load(str(meta["script"]))
	var view: Control = script.new()
	win.set_body(view)
	_window_layer.add_child(win)
	_windows[id] = win
	NeonUI.fade_in(win, 0.22, 14.0)
	_rebuild_taskbar_buttons()
	_fit_window.call_deferred(win)


func _fit_window(win: NeonWindow) -> void:
	if not is_instance_valid(win):
		return
	await get_tree().process_frame
	if not is_instance_valid(win):
		return
	var max_h := maxf(size.y - TOPBAR_H - TASKBAR_H - 22.0, 240.0)
	win.size = Vector2(win.size.x, minf(win.get_combined_minimum_size().y, max_h))


func _close_window(id: String) -> void:
	if not _windows.has(id):
		return
	var win: NeonWindow = _windows[id]
	_windows.erase(id)
	win.request_close()
	_rebuild_taskbar_buttons()


func _input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and _start_menu != null:
		var mb := event as InputEventMouseButton
		if is_instance_valid(_start_menu) and not _start_menu.get_global_rect().has_point(mb.position):
			_start_menu.queue_free()
			_start_menu = null
