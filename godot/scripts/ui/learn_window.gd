class_name LearnWindowView
extends HBoxContainer

## Окно «Школа Python» — порт LearnWindow из App.tsx.
## Теория + мини-тест с автопроверкой и наградой (XP + «стипендия» $40).

const LIST_WIDTH := 210.0

var lesson: Lesson
var _list_box: VBoxContainer
var _right: VBoxContainer
var _answers := {}
var _selected := 1


func _ready() -> void:
	add_theme_constant_override("separation", 0)
	_build()
	Game.state_changed.connect(_refresh_list)
	_select(Game.data.lessons[0].id if not Game.data.lessons.is_empty() else 1)


func _build() -> void:
	var left := PanelContainer.new()
	left.custom_minimum_size = Vector2(LIST_WIDTH, 0)
	left.add_theme_stylebox_override("panel", Neon.sb(Neon.PANEL_DEEP))
	var left_v := NeonUI.vbox(0)
	left.add_child(left_v)
	left_v.add_child(NeonUI.margin(
		NeonUI.label("ШКОЛА PYTHON", 10, Neon.TEXT_MUTED), 10, 6, 10, 6))
	_list_box = NeonUI.vbox(0)
	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.custom_minimum_size = Vector2(0, 470)
	_list_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(_list_box)
	left_v.add_child(scroll)
	add_child(left)

	_right = NeonUI.vbox(8)
	_right.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_right.add_child(NeonUI.margin(null, 14, 12, 14, 12))
	add_child(_right)


func _refresh_list() -> void:
	NeonUI.clear(_list_box)
	for l in Game.data.lessons:
		var done := Game.lesson_completed(l.id)
		var b := Button.new()
		b.custom_minimum_size = Vector2(0, 42)
		var selected := l.id == _selected
		b.add_theme_stylebox_override("normal", Neon.sb(
			Neon.with_alpha(Neon.PINK, 0.10) if selected else Color(0, 0, 0, 0),
			Color(0, 0, 0, 0), 0))
		b.add_theme_stylebox_override("hover", Neon.sb(Neon.WHITE_05, Color(0, 0, 0, 0), 0))
		b.add_theme_stylebox_override("focus", Neon.sb_empty())
		var row := NeonUI.hbox(8)
		row.mouse_filter = Control.MOUSE_FILTER_IGNORE
		row.add_child(NeonUI.bold_label("✓" if done else str(l.id), 11,
			Neon.GREEN if done else Neon.PINK))
		var texts := NeonUI.vbox(0)
		texts.mouse_filter = Control.MOUSE_FILTER_IGNORE
		texts.add_child(NeonUI.bold_label(l.title, 11, Color.WHITE))
		texts.add_child(NeonUI.label("%s · +%d XP" % [l.duration, l.xp], 10, Neon.TEXT_MUTED))
		row.add_child(texts)
		b.add_child(NeonUI.margin(row, 8, 4, 6, 4))
		b.pressed.connect(func() -> void: _select(l.id))
		_list_box.add_child(b)


func _select(id: int) -> void:
	_selected = id
	lesson = Game.data.lesson(id)
	_answers = {}
	_refresh_list()
	_rebuild()


func _rebuild() -> void:
	NeonUI.clear(_right)
	if lesson == null:
		return

	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.custom_minimum_size = Vector2(0, 470)
	var content := NeonUI.vbox(8)
	content.size_flags_horizontal = Control.SIZE_EXPAND_FILL

	content.add_child(NeonUI.bold_label(lesson.title, 16, Color.WHITE))
	content.add_child(NeonUI.label(lesson.subtitle, 11, Neon.PINK))

	for block in lesson.content:
		var box := NeonUI.vbox(4)
		box.add_child(NeonUI.bold_label(str(block["heading"]), 12, Neon.CYAN))
		var text := NeonUI.label(str(block["text"]), 12, Neon.TEXT_SOFT)
		text.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		text.custom_minimum_size = Vector2(520, 0)
		box.add_child(text)
		var code := str(block["code"])
		if code != "":
			var code_label := NeonUI.label(code, 12, Neon.GREEN)
			code_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
			box.add_child(NeonUI.card_with(Neon.WHITE_10, code_label, Neon.BLACK, 8))
		content.add_child(NeonUI.card_with(Neon.WHITE_10, box, Color(0, 0, 0, 0.4), 10))

	content.add_child(_build_quiz())
	scroll.add_child(content)
	_right.add_child(scroll)


func _build_quiz() -> Control:
	var correct := _correct_count()
	var all_answered := _answers.size() >= lesson.quiz.size()
	var passed := correct == lesson.quiz.size()
	var done := Game.lesson_completed(lesson.id)

	var box := NeonUI.vbox(8)
	box.add_child(NeonUI.bold_label("✎ ПРОВЕРКА ЗНАНИЙ (%d/%d)" % [correct, lesson.quiz.size()],
		12, Neon.YELLOW))

	for qi in range(lesson.quiz.size()):
		var q: Dictionary = lesson.quiz[qi]
		var qbox := NeonUI.vbox(4)
		var qtext := NeonUI.label("%d. %s" % [qi + 1, str(q["q"])], 12, Color.WHITE)
		qtext.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		qbox.add_child(qtext)

		var options_row := NeonUI.hbox(6)
		var options: Array = q["options"]
		for oi in range(options.size()):
			var picked: bool = _answers.get(qi, -1) == oi
			var is_right: bool = oi == int(q["answer"])
			var show_right: bool = _answers.has(qi) and is_right
			var show_wrong: bool = picked and not is_right
			var color := Neon.TEXT_DIM
			if show_right:
				color = Neon.GREEN
			elif show_wrong:
				color = Neon.PINK
			var b := NeonUI.button(str(options[oi]), color,
				NeonUI.KIND_SOLID if picked else NeonUI.KIND_OUTLINE, 11)
			b.disabled = done
			b.pressed.connect(func() -> void:
				_answers[qi] = oi
				_rebuild())
			options_row.add_child(b)
		options_row.add_child(NeonUI.stretch())
		qbox.add_child(options_row)
		box.add_child(qbox)

	if done:
		box.add_child(NeonUI.bold_label("✓ Урок пройден! +%d XP получено" % lesson.xp, 12, Neon.GREEN))
	else:
		var label_text := "ОТВЕТЬ НА ВСЕ ВОПРОСЫ"
		if all_answered and not passed:
			label_text = "ЕСТЬ ОШИБКИ — ПОПРОБУЙ ЕЩЁ"
		elif all_answered and passed:
			label_text = "ЗАБРАТЬ +%d XP" % lesson.xp
		var claim := NeonUI.button(label_text, Neon.YELLOW, NeonUI.KIND_SOLID, 12)
		claim.disabled = not (all_answered and passed)
		claim.custom_minimum_size = Vector2(0, 34)
		claim.pressed.connect(func() -> void: Game.complete_lesson(lesson.id))
		box.add_child(claim)

	return NeonUI.card_with(Neon.with_alpha(Neon.YELLOW, 0.4), box,
		Neon.with_alpha(Neon.YELLOW, 0.04), 12)


func _correct_count() -> int:
	var count := 0
	for qi in range(lesson.quiz.size()):
		if _answers.has(qi) and int(_answers[qi]) == int(lesson.quiz[qi]["answer"]):
			count += 1
	return count
