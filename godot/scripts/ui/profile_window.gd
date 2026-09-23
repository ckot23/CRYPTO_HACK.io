class_name ProfileWindowView
extends VBoxContainer

## Окно «Профиль хакера» — порт ProfileWindow из App.tsx:
## уровень, капитал, статистика и достижения.

static var ACHIEVEMENTS := [
	{"n": "Первый взлом", "d": "Взломай первую цель"},
	{"n": "Серийный хакер", "d": "Взломай 4 цели"},
	{"n": "Легенда даркнета", "d": "Взломай все 8 целей"},
	{"n": "Фермер", "d": "Установи первый майнер"},
	{"n": "Магнат", "d": "Держи 4+ майнера"},
	{"n": "Трейдер", "d": "Соверши 5 сделок"},
	{"n": "Студент", "d": "Пройди 3 урока Python"},
	{"n": "Кит", "d": "Капитал $10,000+"},
]

var _body: VBoxContainer


func _ready() -> void:
	add_theme_constant_override("separation", 10)
	_body = NeonUI.vbox(10)
	add_child(_body)
	Game.state_changed.connect(_refresh)
	Game.prices_changed.connect(_refresh)
	_refresh()


func _refresh() -> void:
	NeonUI.clear(_body)
	_body.add_child(_profile_card())
	_body.add_child(_stats_row())
	_body.add_child(_achievements())


func _status_text() -> String:
	if Game.level >= 5:
		return "ЛЕГЕНДА ДАРКНЕТА"
	if Game.level >= 3:
		return "ОПЫТНЫЙ ХАКЕР"
	return "СКРИПТ-КИДДИ"


func _profile_card() -> Control:
	var row := NeonUI.hbox(12)

	var avatar := NeonUI.display_label("⌁_", 26, Neon.GREEN)
	avatar.custom_minimum_size = Vector2(58, 0)
	avatar.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	row.add_child(avatar)

	var info := NeonUI.vbox(3)
	info.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var name_row := NeonUI.hbox(6)
	name_row.add_child(NeonUI.bold_label("ghost", 14, Color.WHITE))
	name_row.add_child(NeonUI.glow_label("[ур. %d]" % Game.level, 13, Neon.GREEN))
	info.add_child(name_row)
	info.add_child(NeonUI.label("статус: " + _status_text(), 11, Neon.TEXT_DIM))

	var bar := NeonUI.progress(Neon.GREEN, 7.0)
	bar.value = Game.xp_progress()
	bar.custom_minimum_size = Vector2(0, 7)
	info.add_child(bar)
	info.add_child(NeonUI.label("%d / %d XP" % [Game.xp, Game.xp_for_level(Game.level)],
		10, Neon.TEXT_MUTED))
	row.add_child(info)

	var money := NeonUI.vbox(1)
	money.add_child(NeonUI.label("КАПИТАЛ", 10, Neon.TEXT_MUTED))
	var total := Game.portfolio_value() + Game.dollars
	money.add_child(NeonUI.glow_label(Neon.fmt_dollars_full(total), 15, Neon.YELLOW))
	money.add_child(NeonUI.label("%s/мин майнинг" % Neon.fmt_dollars(Game.miner_income_per_min()),
		10, Neon.TEXT_DIM))
	row.add_child(money)

	return NeonUI.card_with(Neon.with_alpha(Neon.GREEN, 0.4), row,
		Neon.with_alpha(Neon.GREEN, 0.05), 12)


func _stats_row() -> Control:
	var grid := NeonUI.grid(4, 6)
	grid.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var stats := [
		[str(Game.total_hacked), "ВЗЛОМОВ", Neon.GREEN],
		[str(Game.miners.size()), "МАЙНЕРОВ", Neon.CYAN],
		[str(Game.total_trades), "СДЕЛОК", Neon.YELLOW],
		[str(Game.completed_lessons.size()), "УРОКОВ", Neon.PINK],
	]
	for s in stats:
		var box := NeonUI.vbox(0)
		var value := NeonUI.bold_label(str(s[0]), 18, s[2])
		value.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		box.add_child(value)
		var caption := NeonUI.label(str(s[1]), 10, Neon.TEXT_MUTED)
		caption.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		box.add_child(caption)
		grid.add_child(NeonUI.card_with(Neon.WHITE_10, box, Neon.PANEL_BAR, 10, 0.0))
	return grid


func _achievement_state() -> Array[bool]:
	var total := Game.portfolio_value() + Game.dollars
	return [
		Game.total_hacked >= 1,
		Game.total_hacked >= 4,
		Game.total_hacked >= 8,
		Game.miners.size() >= 1,
		Game.miners.size() >= 4,
		Game.total_trades >= 5,
		Game.completed_lessons.size() >= 3,
		total >= 10000.0,
	]


func _achievements() -> Control:
	var box := NeonUI.vbox(6)
	var states := _achievement_state()
	var done := 0
	for s in states:
		if s:
			done += 1
	box.add_child(NeonUI.bold_label("ДОСТИЖЕНИЯ · %d/%d" % [done, ACHIEVEMENTS.size()],
		11, Neon.TEXT_DIM))

	var grid := NeonUI.grid(2, 6)
	grid.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	for i in range(ACHIEVEMENTS.size()):
		var a: Dictionary = ACHIEVEMENTS[i]
		var ok: bool = states[i]
		var row := NeonUI.hbox(8)
		row.add_child(NeonUI.display_label("★" if ok else "☆", 15,
			Neon.YELLOW if ok else Neon.TEXT_MUTED))
		var texts := NeonUI.vbox(0)
		texts.add_child(NeonUI.bold_label(str(a["n"]), 11, Color.WHITE if ok else Neon.TEXT_MUTED))
		texts.add_child(NeonUI.label(str(a["d"]), 10, Neon.TEXT_MUTED))
		row.add_child(texts)
		grid.add_child(NeonUI.card_with(
			Neon.with_alpha(Neon.YELLOW, 0.4) if ok else Neon.WHITE_10,
			row, Neon.with_alpha(Neon.YELLOW, 0.05) if ok else Color(0, 0, 0, 0.4), 8))
	box.add_child(grid)
	return box
