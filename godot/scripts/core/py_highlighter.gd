class_name PyHighlighter
extends RefCounted

## Подсветка Python для редактора кода (CodeEdit).
##
## В веб-версии подсветка писалась руками: функция highlightPython() в src/engine.ts
## разбивала строку на токены. В Godot за это отвечает встроенный CodeHighlighter —
## код стал в 10 раз короче, а подсветка работает по мере ввода, а не постфактум.

const KEYWORDS := [
	"for", "in", "if", "elif", "else", "while", "def", "return", "and", "or",
	"not", "True", "False", "None", "import", "from", "as", "break", "continue",
	"pass", "with", "try", "except", "finally", "class", "lambda", "global",
]

## Функции-«заглушки» из мира игры: подсвечиваем их как встроенные.
const GAME_FUNCS := [
	"scan", "connect", "brute", "print", "range", "len", "str", "int", "float",
	"extract", "drain", "bypass", "decrypt", "install_miner", "wallets", "input",
]


static func build() -> CodeHighlighter:
	var ch := CodeHighlighter.new()

	# ключевые слова — розовый, как .token-keyword в index.css
	for kw in KEYWORDS:
		ch.add_keyword_color(kw, Neon.PINK)
	for fn in GAME_FUNCS:
		ch.add_keyword_color(fn, Neon.CYAN)

	ch.keyword_color = Neon.PINK
	ch.function_color = Neon.CYAN
	ch.number_color = Neon.ORANGE
	ch.symbol_color = Neon.TEXT_DIM
	ch.member_variable_color = Neon.GREEN

	# строки — жёлтый
	ch.add_color_region("\"", "\"", Neon.YELLOW, false)
	ch.add_color_region("'", "'", Neon.YELLOW, false)
	# комментарии — приглушённый серый, до конца строки
	ch.add_color_region("#", "", Neon.TEXT_MUTED, true)
	return ch


## Безопасно выставить свойство: молча пропускаем то, чего нет в этой версии Godot.
static func _set_if(node: Object, prop: String, value) -> void:
	if prop in node:
		node.set(prop, value)


## CodeEdit с нашей темой — используется в окне взлома.
static func make_editor() -> CodeEdit:
	var edit := CodeEdit.new()
	edit.syntax_highlighter = build()
	edit.gutters_draw_line_numbers = true
	_set_if(edit, "gutters_zero_padding", 2)
	edit.highlight_current_line = true
	edit.caret_blink = true
	edit.wrap_mode = TextEdit.LINE_WRAPPING_NONE
	_set_if(edit, "scroll_fit_content_height", false)
	edit.code_completion_enabled = false
	_set_if(edit, "indent_automatic", true)
	_set_if(edit, "indent_size", 4)
	edit.placeholder_text = "Пиши код здесь..."
	return edit
