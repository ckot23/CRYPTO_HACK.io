extends Control

## Экран загрузки NEON BIOS — порт BootScreen из App.tsx.
## Строки появляются по одной (setInterval 320 мс в веб-версии), каждая — с бипом.
## После последней строки переходим на рабочий стол.

const BOOT_LINES := [
	"NEON BIOS v3.7 — проверка памяти ... OK",
	"Загрузка ядра NeonOS ... OK",
	"Подключение к TOR-сети ... OK (3 ретранслятора)",
	"Монтирование /dev/blackvault ... OK",
	"Запуск ghost-драйверов ... OK",
	"Обход телеметрии ... OK",
	"Добро пожаловать, ghost. Доступ ROOT подтверждён.",
]

const LINE_DELAY := 0.32
const AFTER_DELAY := 0.7

var _column: VBoxContainer
var _cursor: Label


func _ready() -> void:
	Neon.apply(self)
	set_anchors_preset(Control.PRESET_FULL_RECT)
	var bg := ColorRect.new()
	bg.color = Neon.BLACK
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	var holder := MarginContainer.new()
	holder.set_anchors_preset(Control.PRESET_CENTER)
	holder.offset_left = -320
	holder.offset_right = 320
	holder.offset_top = -140
	holder.offset_bottom = 140
	add_child(holder)

	_column = NeonUI.vbox(2)
	holder.add_child(_column)

	_cursor = NeonUI.label("▊", 14, Neon.GREEN)
	_column.add_child(_cursor)

	_play()


func _play() -> void:
	for i in range(BOOT_LINES.size()):
		await get_tree().create_timer(LINE_DELAY).timeout
		if not is_inside_tree():
			return
		var color := Neon.GREEN if i == BOOT_LINES.size() - 1 else Neon.with_alpha(Neon.GREEN, 0.45)
		var line := NeonUI.label("> " + BOOT_LINES[i], 13, color)
		_column.add_child(line)
		_column.move_child(line, _column.get_child_count() - 1)
		if i == BOOT_LINES.size() - 1:
			line.add_theme_font_override("font", Neon.font_bold())
		if Game.sound_on:
			Sfx.boot_line(i)
		_column.move_child(_cursor, _column.get_child_count() - 1)

	# мигающий курсор
	var blink := _cursor.create_tween().set_loops()
	blink.tween_property(_cursor, "modulate:a", 0.2, 0.5)
	blink.tween_property(_cursor, "modulate:a", 1.0, 0.5)

	await get_tree().create_timer(AFTER_DELAY).timeout
	if is_inside_tree():
		get_tree().change_scene_to_file("res://scenes/desktop.tscn")
