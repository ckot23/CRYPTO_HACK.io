class_name FilesWindowView
extends HBoxContainer

## Окно «Файлы» — порт FilesWindow из App.tsx.
##
## Виртуальная файловая система живёт прямо в коде: папки и файлы собираются из
## данных игры (взломанные миссии → exploit_*.py, монеты → *_wallet.dat).

const TREE_WIDTH := 186.0

const TYPE_ICON := {
	"folder": "▶",
	"txt": "•",
	"key": "◎",
	"exe": "⌁",
}

static var TYPE_COLOR := {
	"folder": Neon.YELLOW,
	"txt": Neon.CYAN,
	"key": Neon.GREEN,
	"exe": Neon.PINK,
}

var _tree_box: VBoxContainer
var _content_box: VBoxContainer
var _path := "home"
var _open_file := ""


func _ready() -> void:
	add_theme_constant_override("separation", 0)
	_build()
	Game.state_changed.connect(_refresh)
	_refresh()


func _build() -> void:
	var left := PanelContainer.new()
	left.custom_minimum_size = Vector2(TREE_WIDTH, 0)
	left.add_theme_stylebox_override("panel", Neon.sb(Neon.PANEL_DEEP))
	_tree_box = NeonUI.vbox(0)
	left.add_child(NeonUI.margin(_tree_box, 6, 8, 6, 8))
	add_child(left)

	_content_box = NeonUI.vbox(6)
	_content_box.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	add_child(NeonUI.margin(_content_box, 12, 10, 12, 10))


func _files() -> Dictionary:
	var docs := [
		{"name": "пароли.txt", "type": "txt", "content": _passwords_text()},
		{"name": "шпаргалка_python.txt", "type": "txt", "content": _cheatsheet_text()},
	]
	var exploits: Array = []
	for m in Game.data.missions:
		if Game.mission_completed(m.id):
			exploits.append({
				"name": "exploit_%d_%s.py" % [m.id, m.title.replace(" ", "_")],
				"type": "exe",
				"content": "# %s — успешно применён!\n# Цель: %s (%s)\n\n%s\n\n# Награда: %s %s + $%d" % [
					m.title, m.target_name, m.target_ip, m.solution,
					Neon.fmt_crypto(m.reward_amount), m.reward_crypto, int(m.reward_dollars)],
			})
	var wallets: Array = []
	for c in Game.data.cryptos:
		var unlocked := Game.level >= c.unlock_level
		wallets.append({
			"name": "%s_wallet.dat" % c.id,
			"type": "key",
			"content": "%s WALLET\nАдрес: %s1qxy...%d\nБаланс: %s %s\nСтатус: %s" % [
				c.full_name, c.id.to_lower(), 1000 + int(c.base_price) % 9000,
				Neon.fmt_crypto(float(Game.crypto.get(c.id, 0.0))), c.id,
				"АКТИВЕН" if unlocked else "ЗАБЛОКИРОВАН (нужен уровень %d)" % c.unlock_level],
		})
	return {
		"home": [
			{"name": "Документы", "type": "folder"},
			{"name": "Эксплойты", "type": "folder"},
			{"name": "Кошельки", "type": "folder"},
			{"name": "README.txt", "type": "txt", "content": _readme_text()},
			{"name": "план.txt", "type": "txt", "content": _plan_text()},
		],
		"Документы": docs,
		"Эксплойты": exploits,
		"Кошельки": wallets,
	}


func _refresh() -> void:
	_refresh_tree()
	_refresh_content()


func _refresh_tree() -> void:
	NeonUI.clear(_tree_box)
	for p in ["home", "Документы", "Эксплойты", "Кошельки"]:
		var b := NeonUI.button("/home/ghost" if p == "home" else p,
			Neon.CYAN if _path == p else Neon.TEXT_DIM,
			NeonUI.KIND_GHOST, 11)
		b.alignment = HORIZONTAL_ALIGNMENT_LEFT
		b.custom_minimum_size = Vector2(0, 28)
		if _path == p:
			b.add_theme_stylebox_override("normal",
				Neon.sb(Neon.with_alpha(Neon.CYAN, 0.12), Color(0, 0, 0, 0), 0))
		b.pressed.connect(func() -> void:
			_path = p
			_open_file = ""
			_refresh())
		_tree_box.add_child(b)


func _refresh_content() -> void:
	NeonUI.clear(_content_box)
	var files: Dictionary = _files()
	var list: Array = files.get(_path, [])

	var crumb := "/home/ghost/%s · %d объектов" % [
		"" if _path == "home" else _path + "/", list.size()]
	_content_box.add_child(NeonUI.label(crumb, 11, Neon.TEXT_MUTED))

	if _open_file == "":
		var columns := NeonUI.grid(2, 6)
		columns.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		if list.is_empty():
			columns.add_child(NeonUI.label("пусто... пока что", 11, Neon.TEXT_MUTED))
		for f in list:
			var type := str(f["type"])
			var b := NeonUI.button("%s  %s" % [TYPE_ICON.get(type, "•"), str(f["name"])],
				TYPE_COLOR.get(type, Neon.TEXT), NeonUI.KIND_OUTLINE, 11)
			b.alignment = HORIZONTAL_ALIGNMENT_LEFT
			b.custom_minimum_size = Vector2(0, 30)
			b.pressed.connect(func() -> void:
				if type == "folder":
					_path = str(f["name"])
					_open_file = ""
				else:
					_open_file = str(f["name"])
				_refresh())
			columns.add_child(b)
		_content_box.add_child(columns)
		return

	# просмотр файла
	var head := NeonUI.hbox(8)
	head.add_child(NeonUI.label(_open_file, 11, Neon.CYAN))
	head.add_child(NeonUI.stretch())
	var back := NeonUI.button("назад", Neon.TEXT_MUTED, NeonUI.KIND_GHOST, 10)
	back.pressed.connect(func() -> void:
		_open_file = ""
		_refresh())
	head.add_child(back)
	_content_box.add_child(head)

	var body := ""
	for f in list:
		if str(f["name"]) == _open_file:
			body = str(f.get("content", ""))
			break
	var text := NeonUI.label(body, 12, Neon.TEXT_SOFT)
	text.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	text.custom_minimum_size = Vector2(520, 0)
	var scroll := ScrollContainer.new()
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.custom_minimum_size = Vector2(0, 320)
	scroll.add_child(text)
	_content_box.add_child(NeonUI.card_with(Neon.WHITE_15, scroll, Color(0, 0, 0, 0.6), 10))


# ==================== ТЕКСТЫ ФАЙЛОВ (перенос из App.tsx) ====================
func _readme_text() -> String:
	return """NEON_OS v3.7 — система анонимного хакера.

Правила выживания:
1. Никогда не взламывай без VPN (у нас он встроен).
2. Майнеры — твой пассивный доход. Ставь на каждый взломанный комп.
3. Продавай крипту на пике, покупай на дне.
4. Учи Python. Код — единственное оружие, которое нельзя отобрать.

— ghost"""


func _plan_text() -> String:
	return """ПЛАН СТАНОВЛЕНИЯ ЛЕГЕНДОЙ:

[ ] Взломать 8 целей
[ ] Открыть все 4 монеты (BTC, ETH, XMR, SOL)
[ ] Поставить 5+ майнеров
[ ] Прокачать всё до MAX
[ ] Заработать $50,000

Удачи, будущий кит."""


func _passwords_text() -> String:
	return """Слитые пароли (не использовать для зла... ладно, использовать):

admin:123456
root:qwerty
trader:neon-77  <-- тот самый ключ из миссии 5!
miner:ferma2024"""


func _cheatsheet_text() -> String:
	return """ШПАРГАЛКА PYTHON:

print("текст")     — вывод
x = 5             — переменная
if x == 5:        — условие (два равно!)
for i in range(5): — цикл 5 раз
while x > 0:      — цикл пока верно
def f():          — своя функция
list = [1,2,3]    — список

# — комментарий
: — двоеточие перед блоком
4 пробела — отступ блока"""
