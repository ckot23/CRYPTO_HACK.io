class_name HackWindowView
extends HBoxContainer

## Окно «Хак-терминал» — порт компонента HackWindow из App.tsx.
##
## Что где лежит:
##   слева  — список целей (Mission)
##   справа — шапка цели, вкладки (брифинг/теория/подсказки),
##            редактор exploit.py (CodeEdit) и консоль вывода
##
## Режимы выполнения кода:
##   СИМУЛЯТОР      — PySim.simulate() (как в веб-версии, работает всегда)
##   РЕАЛЬНЫЙ PYTHON — PyRunner.run_async() запускает настоящий интерпретатор

const LIST_WIDTH := 218.0
const PANE_HEIGHT := 232.0

const TAB_BRIEF := "brief"
const TAB_THEORY := "theory"
const TAB_HINTS := "hints"

static var LOG_COLORS := {
	"cmd": Color.WHITE,
	"ok": Neon.GREEN,
	"info": Color("#7d93b0"),
	"warn": Neon.YELLOW,
	"err": Neon.PINK,
	"code": Neon.GREEN,
}

var mission: Mission

var _list_box: VBoxContainer
var _list_head: Label
var _list_sig := ""
var _target_bar: HBoxContainer
var _tab_row: HBoxContainer
var _tab_box: MarginContainer
var _editor: CodeEdit
var _console: RichTextLabel
var _play_button: Button
var _reset_button: Button
var _mode_button: Button
var _result_bar: PanelContainer
var _result_label: Label
var _stars_label: Label
var _locked_box: CenterContainer
var _work_box: VBoxContainer
var _tab := TAB_BRIEF
var _running := false
var _show_solution := false
var _stars := 0


func _ready() -> void:
	add_theme_constant_override("separation", 0)
	_build()
	Game.state_changed.connect(_on_state_changed)
	_select(Game.selected_mission)


# ==================== ПОСТРОЕНИЕ ====================
func _build() -> void:
	# ---------- левая колонка: список целей ----------
	var left := PanelContainer.new()
	left.custom_minimum_size = Vector2(LIST_WIDTH, 0)
	left.add_theme_stylebox_override("panel", Neon.sb(Neon.PANEL_DEEP))
	var left_v := NeonUI.vbox(0)
	left.add_child(left_v)

	var list_head := PanelContainer.new()
	list_head.add_theme_stylebox_override("panel",
		Neon.sb(Neon.with_alpha(Neon.BLACK, 0.0), Neon.WHITE_10, 1))
	_list_head = NeonUI.label("ЦЕЛИ", 10, Neon.TEXT_MUTED)
	list_head.add_child(NeonUI.margin(_list_head, 10, 6, 10, 6))
	left_v.add_child(list_head)

	_list_box = NeonUI.vbox(0)
	var list_scroll := ScrollContainer.new()
	list_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	list_scroll.custom_minimum_size = Vector2(0, 430)
	_list_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	list_scroll.add_child(_list_box)
	left_v.add_child(list_scroll)
	add_child(left)

	# ---------- правая колонка ----------
	var right := NeonUI.vbox(0)
	right.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	add_child(right)

	# запертая миссия / рабочая область — переключаем видимость
	_locked_box = CenterContainer.new()
	_locked_box.size_flags_vertical = Control.SIZE_EXPAND_FILL
	right.add_child(_locked_box)

	_work_box = NeonUI.vbox(0)
	_work_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	right.add_child(_work_box)

	# шапка цели
	var bar_panel := NeonUI.card(Neon.WHITE_10, Neon.PANEL_BAR)
	_target_bar = NeonUI.hbox(14)
	bar_panel.add_child(NeonUI.margin(_target_bar, 12, 6, 12, 6))
	_work_box.add_child(bar_panel)

	# вкладки
	var tabs_panel := NeonUI.card(Neon.WHITE_10, Neon.PANEL_DARK)
	_tab_row = NeonUI.hbox(0)
	tabs_panel.add_child(_tab_row)
	_work_box.add_child(tabs_panel)

	# содержимое вкладки
	var tab_panel := NeonUI.card(Neon.WHITE_10, Neon.PANEL_DEEP)
	_tab_box = NeonUI.margin(null, 12, 10, 12, 10)
	_tab_box.custom_minimum_size = Vector2(0, 140)
	tab_panel.add_child(_tab_box)
	_work_box.add_child(tab_panel)

	# редактор + консоль
	var panes := NeonUI.hbox(0)
	panes.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_work_box.add_child(panes)
	panes.add_child(_build_editor_pane())
	panes.add_child(_build_console_pane())


func _build_editor_pane() -> Control:
	var pane := NeonUI.vbox(0)
	pane.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	var head := NeonUI.card(Neon.with_alpha(Neon.BLACK, 0.0), Color(0, 0, 0, 0.4))
	var head_row := NeonUI.hbox(8)
	head_row.add_child(NeonUI.label("⌁ exploit.py", 10, Neon.TEXT_MUTED))
	head_row.add_child(NeonUI.stretch())
	_mode_button = NeonUI.button("СИМУЛЯТОР", Neon.CYAN, NeonUI.KIND_GHOST, 10)
	_mode_button.pressed.connect(_toggle_mode)
	_mode_button.tooltip_text = "Переключить способ выполнения кода"
	head_row.add_child(_mode_button)
	_reset_button = NeonUI.button("сбросить", Neon.TEXT_MUTED, NeonUI.KIND_GHOST, 10)
	_reset_button.pressed.connect(func() -> void:
		if mission != null:
			_editor.text = mission.starter_code)
	head_row.add_child(_reset_button)
	head.add_child(NeonUI.margin(head_row, 10, 4, 10, 4))
	pane.add_child(head)

	_editor = PyHighlighter.make_editor()
	_editor.custom_minimum_size = Vector2(0, PANE_HEIGHT)
	_editor.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	pane.add_child(_editor)

	var foot := NeonUI.card(Neon.WHITE_10, Neon.PANEL_BAR)
	var foot_row := NeonUI.hbox(8)
	_play_button = NeonUI.button("▶ ЗАПУСТИТЬ", Neon.GREEN, NeonUI.KIND_SOLID, 12)
	_play_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_play_button.custom_minimum_size = Vector2(0, 34)
	_play_button.pressed.connect(_run)
	foot_row.add_child(_play_button)
	foot.add_child(NeonUI.margin(foot_row, 8, 6, 8, 6))
	pane.add_child(foot)
	return pane


func _build_console_pane() -> Control:
	var pane := NeonUI.vbox(0)
	pane.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	var head := NeonUI.card(Neon.with_alpha(Neon.BLACK, 0.0), Color(0, 0, 0, 0.4))
	head.add_child(NeonUI.margin(NeonUI.label("⌁ вывод терминала", 10, Neon.TEXT_MUTED), 10, 4, 10, 4))
	pane.add_child(head)

	var console_bg := NeonUI.card(Neon.with_alpha(Neon.BLACK, 0.0), Neon.BLACK)
	_console = RichTextLabel.new()
	_console.bbcode_enabled = true
	_console.scroll_following = true
	_console.custom_minimum_size = Vector2(0, PANE_HEIGHT)
	_console.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_console.add_theme_font_override("normal_font", Neon.font_main())
	_console.add_theme_font_size_override("normal_font_size", 12)
	console_bg.add_child(NeonUI.margin(_console, 8, 8, 8, 8))
	pane.add_child(console_bg)

	# полоса результата
	_result_bar = NeonUI.card(Neon.WHITE_10, Neon.PANEL_BAR)
	var res_row := NeonUI.hbox(6)
	_result_label = NeonUI.bold_label("", 11, Neon.TEXT_MUTED)
	res_row.add_child(_result_label)
	res_row.add_child(NeonUI.stretch())
	_stars_label = NeonUI.label("", 12, Neon.YELLOW)
	res_row.add_child(_stars_label)
	_result_bar.add_child(NeonUI.margin(res_row, 10, 6, 10, 6))
	_result_bar.visible = false
	pane.add_child(_result_bar)

	var spacer := NeonUI.spacer(0, 4)
	pane.add_child(spacer)
	return pane


# ==================== СПИСОК ЦЕЛЕЙ ====================
func _list_signature() -> String:
	var parts: Array[String] = []
	for m in Game.data.missions:
		parts.append("%d:%s:%s" % [
			m.id,
			"1" if Game.mission_completed(m.id) else "0",
			"1" if Game.mission_unlocked(m) else "0"])
	return "%s|%d" % ["".join(parts), Game.selected_mission]


func _on_state_changed() -> void:
	var sig := _list_signature()
	if sig != _list_sig:
		_refresh_list()
	_refresh_target_bar()
	_refresh_mode_button()


func _refresh_list() -> void:
	_list_sig = _list_signature()
	NeonUI.clear(_list_box)
	if _list_head != null:
		_list_head.text = "ЦЕЛИ // %d/%d" % [Game.completed_missions.size(), Game.data.missions.size()]
	for m in Game.data.missions:
		_list_box.add_child(_mission_button(m))


func _mission_button(m: Mission) -> Button:
	var done := Game.mission_completed(m.id)
	var unlocked := Game.mission_unlocked(m)
	var selected := m.id == Game.selected_mission

	var b := Button.new()
	b.custom_minimum_size = Vector2(0, 44)
	b.add_theme_stylebox_override("normal", Neon.sb(
		Neon.with_alpha(Neon.GREEN, 0.08) if selected else Color(0, 0, 0, 0),
		Neon.with_alpha(Neon.GREEN, 0.0), 0))
	b.add_theme_stylebox_override("hover", Neon.sb(Neon.WHITE_05, Color(0, 0, 0, 0), 0))
	b.add_theme_stylebox_override("pressed", Neon.sb(Neon.WHITE_10, Color(0, 0, 0, 0), 0))
	b.add_theme_stylebox_override("focus", Neon.sb_empty())

	var row := NeonUI.hbox(8)
	row.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var badge := NeonUI.label("✓" if done else ("✗" if not unlocked else str(m.id)),
		11, Neon.BLACK if done else (Neon.TEXT_MUTED if not unlocked else Neon.CYAN))
	badge.custom_minimum_size = Vector2(20, 0)
	badge.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	row.add_child(badge)

	var texts := NeonUI.vbox(0)
	texts.mouse_filter = Control.MOUSE_FILTER_IGNORE
	texts.add_child(NeonUI.bold_label(m.title, 11, Neon.GREEN if done else Neon.TEXT))
	texts.add_child(NeonUI.label("%s %s · %s" % [
		Neon.fmt_crypto(m.reward_amount), m.reward_crypto, m.difficulty], 10, Neon.TEXT_MUTED))
	row.add_child(texts)
	b.add_child(NeonUI.margin(row, 8, 4, 6, 4))

	b.pressed.connect(func() -> void: _select(m.id))
	return b


func _select(id: int) -> void:
	mission = Game.data.mission(id)
	if mission == null:
		return
	Game.selected_mission = id
	if _editor != null:
		_editor.text = mission.starter_code
	_show_solution = false
	_stars = 0
	_console_clear()
	_set_result("", 0)
	_refresh_stars()
	_refresh_list()
	_refresh_target_bar()
	_refresh_tab()
	_refresh_locked()
	_refresh_mode_button()
	Game.save()


func _refresh_locked() -> void:
	if mission == null:
		return
	var unlocked := Game.mission_unlocked(mission)
	_work_box.visible = unlocked
	_locked_box.visible = not unlocked
	NeonUI.clear(_locked_box)
	if unlocked:
		return

	var box := NeonUI.vbox(8)
	box.add_child(NeonUI.display_label("ДОСТУП ЗАБЛОКИРОВАН", 18, Neon.PINK))
	var lines: Array[String] = []
	if Game.level < mission.required_level:
		lines.append("Требуется уровень %d (у тебя %d). Качай опыт на взломах и обучении."
			% [mission.required_level, Game.level])
	if Game.upgrade_level("codeLib") < mission.required_code_lib:
		lines.append("Требуется «Библиотека кода» ур. %d — купи апгрейд во вкладке «Апгрейды»."
			% mission.required_code_lib)
	for l in lines:
		var text := NeonUI.label(l, 12, Neon.TEXT_SOFT)
		text.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		text.custom_minimum_size = Vector2(420, 0)
		box.add_child(text)
	box.add_child(NeonUI.chip("Концепт миссии: " + mission.concept, Neon.YELLOW))
	_locked_box.add_child(box)


func _refresh_target_bar() -> void:
	if mission == null:
		return
	NeonUI.clear(_target_bar)
	_target_bar.add_child(NeonUI.bold_label(mission.target_name, 12, Color.WHITE))
	_target_bar.add_child(NeonUI.label(mission.target_ip, 11, Neon.CYAN))
	_target_bar.add_child(NeonUI.label(mission.os_name, 11, Neon.TEXT_MUTED))
	_target_bar.add_child(NeonUI.stretch())
	_target_bar.add_child(NeonUI.label("Защита %d%%" % mission.security, 11, Neon.YELLOW))


# ==================== ВКЛАДКИ ====================
func _refresh_tab() -> void:
	NeonUI.clear(_tab_row)
	var tabs := [
		[TAB_BRIEF, "БРИФИНГ"],
		[TAB_THEORY, "ТЕОРИЯ"],
		[TAB_HINTS, "ПОДСКАЗКИ"],
	]
	for t in tabs:
		var key: String = t[0]
		var b := NeonUI.button(t[1], Neon.GREEN if _tab == key else Neon.TEXT_MUTED,
			NeonUI.KIND_GHOST, 11)
		b.custom_minimum_size = Vector2(112, 28)
		b.add_theme_stylebox_override("normal", Neon.sb(
			Neon.with_alpha(Neon.GREEN, 0.10) if _tab == key else Color(0, 0, 0, 0),
			Color(0, 0, 0, 0), 0))
		b.pressed.connect(func() -> void:
			_tab = key
			_refresh_tab())
		_tab_row.add_child(b)
	_tab_row.add_child(NeonUI.stretch())
	_tab_row.add_child(NeonUI.label("PYTHON 3.12 · NeonHack FW", 10, Neon.TEXT_MUTED))

	NeonUI.clear(_tab_box)
	if mission == null:
		return

	var content: Control
	match _tab:
		TAB_THEORY:
			content = _build_theory()
		TAB_HINTS:
			content = _build_hints()
		_:
			content = _build_brief()

	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.custom_minimum_size = Vector2(0, 140)
	content.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(content)
	_tab_box.add_child(scroll)


func _build_brief() -> Control:
	var box := NeonUI.vbox(8)
	var brief := NeonUI.label(mission.briefing, 12, Neon.TEXT_SOFT)
	brief.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	brief.custom_minimum_size = Vector2(560, 0)
	box.add_child(brief)

	var task_box := NeonUI.vbox(2)
	task_box.add_child(NeonUI.bold_label("ЗАДАЧА:", 11, Neon.GREEN))
	var task := NeonUI.label(mission.task, 12, Color.WHITE)
	task.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	task.custom_minimum_size = Vector2(520, 0)
	task_box.add_child(task)
	box.add_child(NeonUI.card_with(Neon.with_alpha(Neon.GREEN, 0.4), task_box,
		Neon.with_alpha(Neon.GREEN, 0.05), 8))

	var rewards := NeonUI.hbox(6)
	rewards.add_child(NeonUI.chip("+%s %s" % [Neon.fmt_crypto(mission.reward_amount), mission.reward_crypto], Neon.YELLOW))
	rewards.add_child(NeonUI.chip("+%s" % Neon.fmt_dollars(mission.reward_dollars), Neon.GREEN))
	rewards.add_child(NeonUI.chip("+%d XP" % mission.reward_xp, Neon.CYAN))
	if Game.mission_completed(mission.id):
		rewards.add_child(NeonUI.chip("✓ пройдено — повтор без награды", Neon.TEXT_MUTED))
	rewards.add_child(NeonUI.stretch())
	box.add_child(rewards)
	return box


func _build_theory() -> Control:
	var box := NeonUI.vbox(6)
	box.add_child(NeonUI.bold_label("Тема: " + mission.concept, 12, Neon.CYAN))
	var desc := NeonUI.label(mission.concept_desc, 12, Neon.TEXT_DIM)
	desc.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	desc.custom_minimum_size = Vector2(560, 0)
	box.add_child(desc)
	for i in range(mission.theory.size()):
		var row := NeonUI.hbox(6)
		row.add_child(NeonUI.bold_label("%d." % (i + 1), 12, Neon.GREEN))
		var text := NeonUI.label(mission.theory[i], 12, Neon.TEXT_SOFT)
		text.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		text.custom_minimum_size = Vector2(520, 0)
		row.add_child(text)
		box.add_child(row)
	return box


func _build_hints() -> Control:
	var box := NeonUI.vbox(6)
	for h in mission.hints:
		var row := NeonUI.hbox(6)
		row.add_child(NeonUI.label("●", 11, Neon.YELLOW))
		var text := NeonUI.label(h, 12, Neon.TEXT_SOFT)
		text.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		text.custom_minimum_size = Vector2(520, 0)
		row.add_child(text)
		box.add_child(row)

	if not _show_solution:
		var show := NeonUI.button("показать готовое решение (без штрафа, ты же учишься)",
			Neon.TEXT_MUTED, NeonUI.KIND_GHOST, 11)
		show.pressed.connect(func() -> void:
			_show_solution = true
			_refresh_tab())
		box.add_child(show)
	else:
		var sol := NeonUI.vbox(4)
		var head := NeonUI.hbox(8)
		head.add_child(NeonUI.bold_label("РЕШЕНИЕ:", 10, Neon.PINK))
		head.add_child(NeonUI.stretch())
		var insert := NeonUI.button("вставить в редактор", Neon.GREEN, NeonUI.KIND_GHOST, 10)
		insert.pressed.connect(func() -> void: _editor.text = mission.solution)
		head.add_child(insert)
		sol.add_child(head)
		sol.add_child(NeonUI.label(mission.solution, 12, Neon.TEXT))
		box.add_child(NeonUI.card_with(Neon.with_alpha(Neon.PINK, 0.45), sol, Neon.BLACK, 8))
	return box


# ==================== КОНСОЛЬ ====================
func _console_clear() -> void:
	if _console != null:
		_console.clear()


func _console_line(text: String, kind: String) -> void:
	var color: Color = LOG_COLORS.get(kind, Neon.TEXT)
	_console.push_color(color)
	_console.add_text(text + "\n")
	_console.pop()


func _set_result(text: String, stars: int, ok: bool = true) -> void:
	if text == "":
		_result_bar.visible = false
		return
	_result_bar.visible = true
	_result_label.text = text
	_result_label.add_theme_color_override("font_color", Neon.GREEN if ok else Neon.PINK)
	_result_bar.add_theme_stylebox_override("panel", Neon.sb(
		Neon.PANEL_BAR, Neon.with_alpha(Neon.GREEN if ok else Neon.PINK, 0.35), 1))
	_stars = stars
	_refresh_stars()


func _refresh_stars() -> void:
	if _stars <= 0:
		_stars_label.text = ""
		return
	_stars_label.text = "★".repeat(_stars) + "☆".repeat(maxi(0, 3 - _stars))


func _refresh_mode_button() -> void:
	if _mode_button == null:
		return
	_mode_button.text = "РЕЖИМ: " + ("PYTHON" if Game.real_python else "СИМУЛЯТОР")
	if not PyRunner.available():
		_mode_button.disabled = true
		_mode_button.tooltip_text = "Python не найден в системе — доступен только симулятор"
	else:
		_mode_button.tooltip_text = Game.python_mode_text()


func _toggle_mode() -> void:
	Game.real_python = not Game.real_python
	Game.save()
	_refresh_mode_button()
	_console_clear()
	_console_line("Режим выполнения: " + Game.python_mode_text(), "info")


# ==================== ЗАПУСК КОДА ====================
func _run() -> void:
	if _running or mission == null or not Game.mission_unlocked(mission):
		return
	_running = true
	_play_button.disabled = true
	_play_button.text = "ВЫПОЛНЯЕТСЯ..."
	_console_clear()
	_set_result("", 0)
	if Game.sound_on:
		Sfx.beep_square(440.0, 0.07, 0.3)

	var code := _editor.text
	if Game.real_python and PyRunner.available():
		await _run_real(code)
	else:
		await _run_simulator(code)

	_running = false
	_play_button.disabled = false
	_play_button.text = "▶ ЗАПУСТИТЬ"


func _run_simulator(code: String) -> void:
	var result := PySim.simulate(code, mission, Game.upgrade_level("hackSpeed"))
	var logs: Array = result["logs"]
	for entry in logs:
		var delay: float = float(entry["delay"]) / 1000.0
		await get_tree().create_timer(delay).timeout
		if not is_inside_tree():
			return
		_console_line(str(entry["text"]), str(entry["kind"]))
		if Game.sound_on and str(entry["kind"]) == "ok":
			Sfx.beep_sine(700.0 + randf() * 300.0, 0.03, 0.2)
	_finish_run(bool(result["success"]), result["missing"], int(result["style_score"]))


func _run_real(code: String) -> void:
	_console_line("$ " + PyRunner.describe() + " exploit.py --target " + mission.target_ip, "cmd")
	var res: Dictionary = await PyRunner.run_async(get_tree(), code, mission)
	var lines: Array = res.get("lines", [])
	for line in lines:
		_console_line(str(line), _guess_kind(str(line)))
	var raw_lines: Array[String] = []
	for line in lines:
		raw_lines.append(str(line))
	var text := "\n".join(PackedStringArray(raw_lines))
	var syntax_failed := text.contains("SyntaxError")
	var missing := PySim.check_patterns(code, mission)
	if bool(res.get("timed_out", false)):
		_console_line("Превышено время выполнения — вероятно, бесконечный цикл.", "warn")
	var success := missing.is_empty() and not syntax_failed
	_finish_run(success, missing, PySim.style_score(code, mission, missing))


func _guess_kind(line: String) -> String:
	if line.begins_with("$"):
		return "cmd"
	if line.contains("Error") or line.contains("Ошибка") or line.begins_with("  File"):
		return "err"
	if line.begins_with("[") and (line.contains("OK") or line.contains("drained")
			or line.contains("внедрён") or line.contains("извлечены") or line.contains("подобран")
			or line.contains("ослеплён") or line.contains("расшифрована")):
		return "ok"
	return "info"


func _finish_run(success: bool, missing: Array, stars: int) -> void:
	if success:
		if not Game.mission_completed(mission.id):
			Game.hack_success(mission, stars)
		else:
			_console_line("Цель уже взломана — награда не начисляется.", "info")
		_set_result("ВЗЛОМ УСПЕШЕН", stars, true)
		if Game.sound_on:
			Sfx.hack_success()
	else:
		if not missing.is_empty():
			_console_line("Не хватает шагов: %d. Открой вкладку «Подсказки»." % missing.size(), "warn")
		_set_result("ВЗЛОМ ПРОВАЛЕН — смотри подсказки и пробуй ещё", 0, false)
		if Game.sound_on:
			Sfx.hack_fail()
	Game.state_changed.emit()
